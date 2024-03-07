namespace FileStorage.Services.Exceptions;

public class FileDoesNotExistExistException(Exception? innerException = null)
    : InvalidOperationException("Файл не существует", innerException);