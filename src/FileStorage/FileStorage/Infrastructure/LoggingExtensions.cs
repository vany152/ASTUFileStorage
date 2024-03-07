using ILogger = Serilog.ILogger;

namespace FileStorage.Infrastructure;

public static class LoggingExtensions
{
    public static void Warning(this ILogger logger, int httpStatusCode, string operation,
        string? additionalMessage = null) =>
        logger.Warning(
            "HTTP-код: {HttpStatusCode}; Операция: {Operation}; Дополнительная информация: {AdditionalMessage}",
            httpStatusCode, operation, additionalMessage);

    public static void Error(this ILogger logger, Exception err, int httpStatusCode, string operation) =>
        logger.Error(
            err, "HTTP-код: {HttpStatusCode}; Операция: {Operation}}",
            httpStatusCode, operation);
}