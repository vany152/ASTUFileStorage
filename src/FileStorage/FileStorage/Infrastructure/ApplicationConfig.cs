namespace FileStorage.Infrastructure;

/// <summary>
/// Конфигурация приложения
/// </summary>
/// <param name="storageDirectoryPath">Путь к директории файлового хранилища</param>
public class ApplicationConfig(string storageDirectoryPath)
{
    /// <summary>
    /// Путь к директории файлового хранилища
    /// </summary>
    public string StorageDirectoryPath { get; } = storageDirectoryPath;
}