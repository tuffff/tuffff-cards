namespace TuffCards.Extensions;

public static class LoggerExtensions {
        public static void LogSuccess<T>(this ILogger<T> logger, EventId eventId, Exception? exception, string? message, params object?[] args) {
            logger.Log(LogLevel.Information, eventId, exception, message, args);
        }

        public static void LogSuccess<T>(this ILogger<T> logger, EventId eventId, string? message, params object?[] args) {
            logger.Log(LogLevel.Information, eventId, message, args);
        }

        public static void LogSuccess<T>(this ILogger<T> logger, Exception? exception, string? message, params object?[] args) {
            logger.Log(LogLevel.Information, exception, message, args);
        }

        public static void LogSuccess<T>(this ILogger<T> logger, string? message, params object?[] args) {
	        logger.Log(LogLevel.Information, message, args);
        }
}