using System.Security.Cryptography;
using FileStorage.DataAccess;
using FileStorage.Infrastructure;
using FileStorage.Models;
using FileStorage.Services.Exceptions;
using Npgsql;
using Serilog;
using ILogger = Serilog.ILogger;

namespace FileStorage.Services;

/// <summary>
/// Сервис для управления файлами
/// </summary>
public class FileService
{
    private readonly FileSqlRepository _repository;
    private readonly ApplicationConfig _appConfig;

    private readonly ILogger _logger = Log.ForContext<FileService>();



    /// <summary>
    /// Сервис для управления файлами
    /// </summary>
    /// <param name="repository">Репозиторий файлов</param>
    /// <param name="appConfig">Конфигурация приложения</param>
    public FileService(ApplicationConfig appConfig, FileSqlRepository repository)
    {
        _appConfig = appConfig;
        _repository = repository;
    }



    /// <summary>
    /// Получение идентификатора файла по его хэшу 
    /// </summary>
    /// <param name="hash">Закодированный в base-64 хэш файла</param>
    /// <returns>Идентификатор файла</returns>
    public Guid? GetIdByHash(string hash) =>
        _repository.GetIdByHash(hash);

    /// <summary>
    /// Получение подробной информации о файле
    /// </summary>
    /// <param name="id">Id файла</param>
    /// <returns>Подробности о файле, включая поток чтения файла</returns>
    /// <exception cref="FileDoesNotExistException">Файл с указанным id не существует</exception>
    public FileDetails GetFileDetails(Guid id)
    {
        var fileSummary = GetFileSummary(id);
        if (!File.Exists(fileSummary.AbsolutePath))
            throw new FileDoesNotExistException();

        var stream = new FileStream(fileSummary.AbsolutePath, FileMode.Open, FileAccess.Read);
        return new FileDetails(fileSummary, stream);
    }

    /// <summary>
    /// Загрузка файла в файловое хранилище
    /// </summary>
    /// <param name="fileStream">Поток, из которого читается содержимое файла</param>
    /// <param name="fileName">Название, под которые следует сохранить файл</param>
    /// <returns>Идентификатор сохраненного файла</returns>
    /// <exception cref="FileDoesNotExistException">Загружаемый файл уже был сохранен в хранилище, но не удалось получить его id из БД</exception>
    public Guid Upload(Stream fileStream, string fileName)
    {
        var fileId = Guid.NewGuid();
        var filePath = ConstructAbsolutePath(fileId.ToString());
        var fileHash = CalculateHash(fileStream);
        fileStream.Seek(0, SeekOrigin.Begin);

        try
        {
            SaveFileSummaryToDatabase(fileId, fileName, filePath, fileHash, linksCount: 1);
            SaveFileToFilesystem(fileStream, filePath);
            _logger.Information("Файл {FileId} сохранен в хранилище", fileId);
        }
        catch (PostgresException err) when (err is { SqlState: "23505", ConstraintName: "files_hash_key" })
        {
            fileId = GetIdByHash(fileHash) ?? throw new FileDoesNotExistException();
            IncreaseLinksCount(fileId);
            _logger.Information("Запрошена загрузка уже существующего в хранилище файла с хешем {Hash}", fileHash);
        }

        return fileId;
    }

    /// <summary>
    /// Декрементирование количества ссылок на файл
    /// </summary>
    /// <param name="fileId">Идентификатор файла</param>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    /// <exception cref="LinksCountCannotBeNegativeException">После уменьшения количество ссылок стало отрицательным</exception>
    public void DecreaseLinksCount(Guid fileId)
    {
        var linksCount = TryDecreaseLinksCount(fileId);
        if (linksCount == 0)
            TryDeleteFile(fileId);
    }



    /// <summary>
    /// Загрузка краткой информации о файле из БД
    /// </summary>
    /// <param name="id">Id файла</param>
    /// <returns>Краткая информация о файле</returns>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    private FileSummary GetFileSummary(Guid id) =>
        _repository.GetFileSummary(id);

    /// <summary>
    /// Попытка уменьшения количества ссылок на файл
    /// </summary>
    /// <param name="fileId">Id файла</param>
    /// <returns>Количество обновленных в процессе выполнения запроса строк в БД</returns>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    /// <exception cref="LinksCountCannotBeNegativeException">После уменьшения количество ссылок стало отрицательным</exception>
    private int TryDecreaseLinksCount(Guid fileId) =>
        _repository.TryDecreaseLinksCount(fileId);

    /// <summary>
    /// Конструирование абсолютного пути к сохраняемому файлу
    /// </summary>
    /// <param name="fileName">Название файла</param>
    /// <returns>Абсолютный путь файла</returns>
    private string ConstructAbsolutePath(string fileName) =>
        Path.Combine(_appConfig.StorageDirectoryPath, fileName);

    /// <summary>
    /// Вычисление хеша файла
    /// </summary>
    /// <param name="stream">Поток файла</param>
    /// <returns>Хэш файла, закодированный в Base64</returns>
    private static string CalculateHash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var rawHash = sha256.ComputeHash(stream);
        var hash = Convert.ToBase64String(rawHash);

        return hash;
    }

    /// <summary>
    /// Сохранение основной информации о файле в БД
    /// </summary>
    /// <param name="id">Id файла</param>
    /// <param name="name">Название файла</param>
    /// <param name="absolutePath">Абсолютный путь к файлу</param>
    /// <param name="hash">Хэш файла</param>
    /// <param name="linksCount">Количество ссылок на файл</param>
    private void SaveFileSummaryToDatabase(Guid id, string name, string absolutePath, string hash,
        int linksCount) =>
        _repository.SaveFileSummaryToDatabase(id, name, absolutePath, hash, linksCount);

    /// <summary>
    /// Сохранение файла в файловую систему
    /// </summary>
    /// <param name="stream">Поток файла</param>
    /// <param name="absolutePath">Абсолютный путь файла, по которому его требуется сохранить</param>
    private static void SaveFileToFilesystem(Stream stream, string absolutePath)
    {
        using var outStream = File.Create(absolutePath);
        stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(outStream);
        outStream.Close();
    }

    /// <summary>
    /// Инкрементирование количества ссылок на файл
    /// </summary>
    /// <param name="fileId">Идентификатор файла</param>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    private void IncreaseLinksCount(Guid fileId) =>
        _repository.IncreaseLinksCount(fileId);

    /// <summary>
    /// Метод пытается удалить файл из БД и файловой системы и логгирует исключения в случае их возникновения
    /// </summary>
    /// <param name="fileId">Id файла</param>
    private void TryDeleteFile(Guid fileId)
    {
        try
        {
            DeleteFile(fileId);
            _logger.Information("Файл {FileId} удален из хранилища", fileId);
        }
        catch (FileDoesNotExistException err)
        {
            _logger.Error("Файл {FileId} не существует в базе данных. Дополнительно: {Message}", fileId,
                err.Message);
        }
        catch (FileNotFoundException err)
        {
            _logger.Error("Файл {FileId} не существует в файловой системе. Дополнительно: {Message}", fileId,
                err.Message);
        }
        catch (Exception err)
        {
            _logger.Error("Непредвиденная ошибка при удалении файла {FileId}. Дополнительно: {Message}", fileId,
                err.Message);
        }
    }

    /// <summary>
    /// Удаление файла
    /// </summary>
    /// <param name="fileId">Id удаляемого файла</param>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    /// <exception cref="FileNotFoundException">Файл не существует в файловой системе</exception>
    private void DeleteFile(Guid fileId)
    {
        var filePath = DeleteFileFromDatabase(fileId);
        DeleteFileFromFilesystem(filePath);
    }

    /// <summary>
    /// Удаление файла из БД
    /// </summary>
    /// <param name="fileId"></param>
    /// <returns>Абсолютный путь удаленного из БД файла в файловой системе</returns>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    private string DeleteFileFromDatabase(Guid fileId) =>
        _repository.DeleteFile(fileId);

    /// <summary>
    /// Удаление файла из файловой системы
    /// </summary>
    /// <param name="absolutePath">Абсолютный путь файла в файловой системе</param>
    /// <exception cref="FileNotFoundException">Файл не существует в файловой системе</exception>
    private static void DeleteFileFromFilesystem(string absolutePath)
    {
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException(
                $"Файл не существует в файловой системе. Абсолютный путь: {absolutePath}");

        File.Delete(absolutePath);
    }
}