namespace tuffCards.Extensions;

public static class TextWriterExtensions
{
	public static void WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
	{
		if (background.HasValue) {
			textWriter.Write(GetBackgroundColorEscapeCode(background.Value));
		}
		if (foreground.HasValue) {
			textWriter.Write(GetForegroundColorEscapeCode(foreground.Value));
		}
		textWriter.Write(message);
		if (foreground.HasValue) {
			textWriter.Write(DefaultForegroundColor);
		}
		if (background.HasValue) {
			textWriter.Write(DefaultBackgroundColor);
		}
	}

	private const string DefaultForegroundColor = "\x1B[39m\x1B[22m";
	private const string DefaultBackgroundColor = "\x1B[49m";

	private static string GetForegroundColorEscapeCode(ConsoleColor color) {
		return color switch {
			ConsoleColor.Black => "\x1B[30m",
			ConsoleColor.DarkRed => "\x1B[31m",
			ConsoleColor.DarkGreen => "\x1B[32m",
			ConsoleColor.DarkYellow => "\x1B[33m",
			ConsoleColor.DarkBlue => "\x1B[34m",
			ConsoleColor.DarkMagenta => "\x1B[35m",
			ConsoleColor.DarkCyan => "\x1B[36m",
			ConsoleColor.Gray => "\x1B[37m",
			ConsoleColor.Red => "\x1B[1m\x1B[31m",
			ConsoleColor.Green => "\x1B[1m\x1B[32m",
			ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
			ConsoleColor.Blue => "\x1B[1m\x1B[34m",
			ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
			ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
			ConsoleColor.White => "\x1B[1m\x1B[37m",
			_ => DefaultForegroundColor
		};
	}

	private static string GetBackgroundColorEscapeCode(ConsoleColor color) {
		return color switch {
			ConsoleColor.Black => "\x1B[40m",
			ConsoleColor.DarkRed => "\x1B[41m",
			ConsoleColor.DarkGreen => "\x1B[42m",
			ConsoleColor.DarkYellow => "\x1B[43m",
			ConsoleColor.DarkBlue => "\x1B[44m",
			ConsoleColor.DarkMagenta => "\x1B[45m",
			ConsoleColor.DarkCyan => "\x1B[46m",
			ConsoleColor.Gray => "\x1B[47m",
			_ => DefaultBackgroundColor
		};
	}
}