namespace Lawful.GameLibrary;

using static GameSession;

public static class Util
{
	public static void PrintPrompt()
	{
		Console.Write("/ [");
		WriteColor(Player.CurrentSession.User.Username, ConsoleColor.Green);
		Console.Write("] [");
		WriteColor(Player.CurrentSession.Host.Address, ConsoleColor.Green);
		Console.WriteLine(']');

		Console.Write($"\\ {Player.CurrentSession.PathNode.GetPath()} > ");

		//	Console.Write($"\\ [{Player.ConnectionInfo.Drive.Label}] {Player.ConnectionInfo.Path} > ");
		//	Console.WriteLine($"/ [{Player.ConnectionInfo.User.Username} @ {Player.ConnectionInfo.PC.Address}]");
	}

	public static void WriteColor(string text, ConsoleColor color)
	{
		ConsoleColor initialcolor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.Write(text);
		Console.ForegroundColor = initialcolor;
	}

	public static void WriteLineColor(string text, ConsoleColor color)
	{
		ConsoleColor initialcolor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.WriteLine(text);
		Console.ForegroundColor = initialcolor;
	}

	public static void WriteDynamic(string text, int delaymiliseconds)
	{
		foreach (char c in text)
		{
			Console.Write(c);
			Thread.Sleep(delaymiliseconds);
		}
	}

	public static void WriteDynamicColor(string text, int delaymiliseconds, ConsoleColor color)
	{
		ConsoleColor initialcolor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		WriteDynamic(text, delaymiliseconds);
		Console.ForegroundColor = initialcolor;
	}

	public static string ReadLineSecret()
	{
		ConsoleColor RevertTo = Console.ForegroundColor;
		Console.ForegroundColor = Console.BackgroundColor;

		Console.CursorVisible = false;
		string Input = Console.ReadLine();
		Console.CursorTop--;
		Console.CursorVisible = true;


		Console.ForegroundColor = RevertTo;

		return Input;
	}

	public static void BeginSpinningCursorAnimation(string[] Frames, int CycleCountLowerLimit, int CycleCountUpperLimit, int SleepIntervalMilliseconds, (int X, int Y) Position)
	{
		Console.CursorVisible = false;

		Random rand = new(DateTime.UtcNow.Second);

		int Count = 0;

		for (int i = 0; i < rand.Next(CycleCountLowerLimit, CycleCountUpperLimit); i++)
		{
			if (Count == Frames.Length)
			{
				Count = 0;
				Console.SetCursorPosition(Position.X, Position.Y);
			}

			Console.SetCursorPosition(Position.X, Position.Y);

			Console.Write(Frames[Count]);

			Count++;

			Thread.Sleep(SleepIntervalMilliseconds);
		}

		Console.SetCursorPosition(Position.X, Position.Y);

		Console.CursorVisible = true;
	}

	public static void BeginCharacterAnimation(char[] Frames, int CycleCountLowerLimit, int CycleCountUpperLimit, int SleepIntervalMilliseconds, (int X, int Y) Position)
	{
		Console.CursorVisible = false;

		Random rand = new(DateTime.UtcNow.Second);

		int Count = 0;

		for (int i = 0; i < rand.Next(CycleCountLowerLimit, CycleCountUpperLimit); i++)
		{
			if (Count == Frames.Length)
			{
				Count = 0;
				Console.SetCursorPosition(Position.X, Position.Y);
			}

			Console.SetCursorPosition(Position.X, Position.Y);

			Console.Write(Frames[Count]);

			Count++;

			Thread.Sleep(SleepIntervalMilliseconds);
		}

		Console.SetCursorPosition(Position.X, Position.Y);

		Console.CursorVisible = true;
	}

	// Basic login handler with a max tries counter
	// MaxTries set to 0 means infinite tries
	public static bool TryUserLogin(UserAccount Account, int MaxTries = 0)
	{
		if (Account.Password.Length == 0)
			return true;

		string Password;
		int Tries = 0;

		do
		{
			if (MaxTries > 0)
				if (Tries == MaxTries)
					return false;

			if (MaxTries > 0)
				Console.Write($"({MaxTries - Tries}) ");

			Console.WriteLine($"Password for '{Account.Username}': ");
			Password = ReadLineSecret();

			Tries++;

			if (Password == "$cancel")
				return false;
		}
		while (Password != Account.Password);

		return true;
	}
}