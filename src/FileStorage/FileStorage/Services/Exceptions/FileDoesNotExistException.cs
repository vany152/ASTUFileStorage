namespace FileStorage.Services.Exceptions;

/// <summary>
/// Генерируется, когда запрошенный файл не существует
/// </summary>
/// <param name="innerException">Исключение, являющееся причиной текущего исключения</param>
public class FileDoesNotExistException(Exception? innerException = null)
    : InvalidOperationException("Файл не существует", innerException);