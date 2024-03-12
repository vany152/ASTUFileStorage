namespace FileStorage.Models;

/// <summary>
/// Подробная информация о файле
/// </summary>
/// <param name="summary">Краткая информация о файле</param>
/// <param name="stream">Поток файла</param>
public class FileDetails(FileSummary summary, Stream stream)
    : FileSummary(summary.Id, summary.Hash, summary.AbsolutePath, summary.Name, summary.LinksCount)
{
    /// <summary>
    /// Поток файла
    /// </summary>
    public Stream Stream { get; } = stream;
}