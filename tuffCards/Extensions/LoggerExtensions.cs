using System.Diagnostics.CodeAnalysis;

namespace tuffCards.Extensions;

[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public static class LoggerExtensions {
        public static void LogSuccess<T>(this ILogger<T> logger, EventId eventId, Exception? exception, string? message, params object?[] args) {
            logger.Log(LogLevel.Information, eventId, exception, $"<success>{message}</success>", args);
        }

        public static void LogSuccess<T>(this ILogger<T> logger, EventId eventId, string? message, params object?[] args) {
            logger.Log(LogLevel.Information, eventId, $"<success>{message}</success>", args);
        }

        public static void LogSuccess<T>(this ILogger<T> logger, Exception? exception, string? message, params object?[] args) {
            logger.Log(LogLevel.Information, exception, $"<success>{message}</success>", args);
        }

        public static void LogSuccess<T>(this ILogger<T> logger, string? message, params object?[] args) {
	        logger.Log(LogLevel.Information, $"<success>{message}</success>", args);
        }
}