namespace FileStorage.Models;

/// <summary>
/// Краткая информация о файле
/// </summary>
/// <param name="id">Идентификатор файла</param>
/// <param name="hash">Хэш файла</param>
/// <param name="absolutePath">Абсолютный путь файла в файловой системе</param>
/// <param name="name">Название файла</param>
/// <param name="linksCount">Количество ссылок на файл</param>
public class FileSummary(Guid id, string hash, string absolutePath, string name, int linksCount)
{
    /// <summary>
    /// Id файла
    /// </summary>
    public Guid Id { get; init; } = id;
    
    /// <summary>
    /// Хэш файла
    /// </summary>
    public string Hash { get; init; } = hash;
    
    /// <summary>
    /// Абсолютный путь файла в файловой системе
    /// </summary>
    public string AbsolutePath { get; init; } = absolutePath;
    
    /// <summary>
    /// Название файла
    /// </summary>
    public string Name { get; init; } = name;
    
    /// <summary>
    /// Количество ссылок на файл
    /// </summary>
    public int LinksCount { get; init; } = linksCount;
}