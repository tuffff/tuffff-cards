using System;
using System.CodeDom;

namespace TuffCards;

public static class Log {
	public static void Info(string s) => Console.WriteLine(s);
	public static void Success(string s) {
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine(s);
		Console.ResetColor();
	}
	public static void Error(string s) {
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Write("Error:");
		Console.ResetColor();
		Console.Write(" ");
		Console.WriteLine(s);
	}
	public static void Warning(string s) {
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.Write("Warning:");
		Console.ResetColor();
		Console.Write(" ");
		Console.WriteLine(s);
	}
}