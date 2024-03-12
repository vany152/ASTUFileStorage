using ILogger = Serilog.ILogger;

namespace FileStorage.Infrastructure;

/// <summary>
/// Расширения для логгирования
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Логгирование предупреждения
    /// </summary>
    /// <param name="logger">Логгер</param>
    /// <param name="httpStatusCode">Http код, соответствующий предупреждению</param>
    /// <param name="operation">Название операции</param>
    /// <param name="additionalMessage">Дополнительная информация</param>
    public static void Warning(this ILogger logger, int httpStatusCode, string operation,
        string? additionalMessage = null) =>
        logger.Warning(
            "HTTP-код: {HttpStatusCode}; Операция: {Operation}; Дополнительная информация: {AdditionalMessage}",
            httpStatusCode, operation, additionalMessage);

    /// <summary>
    /// Логгирование ошибки
    /// </summary>
    /// <param name="logger">Логгер</param>
    /// <param name="err">Исключение, вызвавшее ошибку</param>
    /// <param name="httpStatusCode">Http код, соответствующий ошибке</param>
    /// <param name="operation">Название операции</param>
    public static void Error(this ILogger logger, Exception err, int httpStatusCode, string operation) =>
        logger.Error(
            err, "HTTP-код: {HttpStatusCode}; Операция: {Operation}}",
            httpStatusCode, operation);
}