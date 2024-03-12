using System.Data;
using Dapper;
using FileStorage.Models;
using FileStorage.Services.Exceptions;
using Npgsql;

namespace FileStorage.DataAccess;

/// <summary>
/// Репозиторий файлов
/// </summary>
public class FileSqlRepository : IDisposable
{
    private readonly IDbConnection _connection;

    
    
    /// <summary>
    /// Репозиторий файлов
    /// </summary>
    /// <param name="connection">Подключение к базе данных</param>
    public FileSqlRepository(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
    }



    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Получение идентификатора файла по его хэшу
    /// </summary>
    /// <param name="hash">Закодированный в base-64 хэш файла</param>
    /// <returns>Идентификатор файла или null, если файл не найден</returns>
    public Guid? GetIdByHash(string hash)
    {
        var id = _connection.ExecuteScalar<Guid?>(
            "select id from files where hash = @hash",
            new { hash });

        return id;
    }
    
    /// <summary>
    /// Загрузка краткой информации о файле из БД
    /// </summary>
    /// <param name="id">Id файла</param>
    /// <returns>Краткая информация о файле</returns>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    public FileSummary GetFileSummary(Guid id)
    {
        var res = _connection.QuerySingleOrDefault(
            "select id, hash, absolute_path, name, links_count from files where id = @id",
            new { id });
        if (res is null)
            throw new FileDoesNotExistException();

        return new FileSummary(res.id, res.hash, res.absolute_path, res.name, res.links_count);
    }
    
    /// <summary>
    /// Попытка уменьшения количества ссылок на файл
    /// </summary>
    /// <param name="fileId">Id файла</param>
    /// <returns>Количество обновленных в процессе выполнения запроса строк в БД</returns>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    /// <exception cref="LinksCountCannotBeNegativeException">После уменьшения количество ссылок стало отрицательным</exception>
    public int TryDecreaseLinksCount(Guid fileId)
    {
        try
        {
            var linksCount = _connection.QuerySingleOrDefault<int?>(
                "update files set links_count = links_count - 1 where id = @fileId returning links_count",
                new { fileId });
            
            if (linksCount is null)
                throw new FileDoesNotExistException();

            return linksCount.Value;
        }
        catch (PostgresException err) when (err is
                                            {
                                                SqlState: "23514",
                                                ConstraintName: "non_negative_links_count"
                                            })
        {
            throw new LinksCountCannotBeNegativeException(err);
        }
    }
    
    /// <summary>
    /// Сохранение основной информации о файле в БД
    /// </summary>
    /// <param name="id">Id файла</param>
    /// <param name="name">Название файла</param>
    /// <param name="absolutePath">Абсолютный путь к файлу</param>
    /// <param name="hash">Хэш файла</param>
    /// <param name="linksCount">Количество ссылок на файл</param>
    public void SaveFileSummaryToDatabase(Guid id, string name, string absolutePath, string hash, int linksCount)
    {
        _connection.Execute(
            """
            insert into files (id, hash, absolute_path, name, links_count)
            values (@id, @hash, @absolutePath, @name, @linksCount)
            """,
            new { id, hash, absolutePath, name, linksCount }
        );
    }
    
    /// <summary>
    /// Инкрементирование количества ссылок на файл
    /// </summary>
    /// <param name="fileId">Идентификатор файла</param>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    public void IncreaseLinksCount(Guid fileId)
    {
        var updated = _connection.Execute(
            "update files set links_count = links_count + 1 where id = @fileId",
            new { fileId });

        if (updated < 1)
            throw new FileDoesNotExistException();
    }
    
    /// <summary>
    /// Удаление файла из БД
    /// </summary>
    /// <param name="fileId"></param>
    /// <returns>Абсолютный путь удаленного из БД файла в файловой системе</returns>
    /// <exception cref="FileDoesNotExistException">Файл не существует в БД</exception>
    public string DeleteFile(Guid fileId)
    {
        var filePath = _connection.QuerySingleOrDefault<string?>(
            "delete from files where id = @fileId returning absolute_path", 
            new { fileId });
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new FileDoesNotExistException();

        return filePath;
    }
}