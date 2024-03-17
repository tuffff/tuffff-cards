using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace TuffCards.Extensions;

public class TuffCardsConsoleFormatter : ConsoleFormatter
{
	public TuffCardsConsoleFormatter() : base(nameof(TuffCardsConsoleFormatter)) { }

	public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
	{
		var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
		var logLevelColors = GetLogLevelConsoleColors(logEntry.LogLevel);

		textWriter.Write(DateTime.Now.ToString("HH:mm:ss"));
		if (logEntry.LogLevel > LogLevel.Information) {
			textWriter.Write(" ");
			textWriter.WriteColoredMessage(logEntry.LogLevel.ToString(), logLevelColors.Background, logLevelColors.Foreground);
		}
		textWriter.Write(" ");
		CreateDefaultLogMessage(textWriter, logEntry, message, scopeProvider);
	}

	private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, IExternalScopeProvider? scopeProvider)
	{
		var exception = logEntry.Exception;

		textWriter.Write(logEntry.Category.AsSpan(logEntry.Category.LastIndexOf('.') + 1));
		WriteScopeInformation(textWriter, scopeProvider);
		textWriter.Write(": ");

		WriteMessage(textWriter, message);
		if (exception != null) {
			WriteMessage(textWriter, exception.ToString());
		}

		textWriter.Write(Environment.NewLine);
	}

	private void WriteMessage(TextWriter textWriter, string message)
	{
			var newMessage = message.Replace(Environment.NewLine, "\\ ");
			textWriter.Write(newMessage);
	}

	private static ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
			LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
			LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
			LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
			LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
			LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
			_ => new ConsoleColors(null, null)
		};
	}

	private static void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider)
	{
		scopeProvider?.ForEachScope((scope, state) =>
		{
			state.Write(" | ");
			state.Write(scope);
		}, textWriter);
	}

	private readonly struct ConsoleColors
	{
		public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
		{
			Foreground = foreground;
			Background = background;
		}

		public ConsoleColor? Foreground { get; }

		public ConsoleColor? Background { get; }
	}
}

public class TuffCardsConsoleFormatterOptions : ConsoleFormatterOptions {}