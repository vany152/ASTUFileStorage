namespace FileStorage.Services.Exceptions;

/// <summary>
/// Исключение генерируется, когда количество ссылок на файл становится отрицательным
/// </summary>
/// <param name="innerException">Исключение, являющееся причиной текущего исключения</param>
public class LinksCountCannotBeNegativeException(Exception? innerException = null)
    : InvalidOperationException("Количество ссылок не может быть отрицательным", innerException);