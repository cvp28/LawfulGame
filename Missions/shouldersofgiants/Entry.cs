using System;

using Lawful.GameLibrary;

namespace Lawful.Mission
{
	public static class Entry
	{
		public static void Load(EventManager e)
		{
			e.CommandEntered += Mission.oce1;
		}

		public static void Unload(EventManager e)
		{
			e.CommandEntered -= Mission.oce1;
		}
	}

	public static class Data
	{
		public static bool OverrideCommand = false;

		public static bool ChangeMission = false;
		public static string NextMissionID = string.Empty;
	}

	public static class Mission
	{
		public static void oce1(EventGlobalType e)
		{
			switch (e.Query.Command.ToUpper())
			{
				case "COMMAND1":
					Data.OverrideCommand = true;    // Tells the game not to handle the user's query when we return from this event handler.
													// Basically, this is a way to override commands
					Util.WriteDynamicColor("First objective done, run \"command2\" to progress", 50, ConsoleColor.Green);
					e.EventManager.CommandEntered -= oce1;
					e.EventManager.CommandEntered += oce2;
					Console.WriteLine();
					break;
			}
		}

		public static void oce2(EventGlobalType e)
		{
			switch(e.Query.Command.ToUpper())
			{
				case "COMMAND2":
					Data.OverrideCommand = true;
					Util.WriteDynamicColor("Second objective done, run \"command3\" to progress", 50, ConsoleColor.Green);
					e.EventManager.CommandEntered -= oce2;
					e.EventManager.CommandEntered += oce3;
					Console.WriteLine();
					break;
			}
		}

		public static void oce3(EventGlobalType e)
		{
			switch (e.Query.Command.ToUpper())
			{
				case "COMMAND3":
					Data.OverrideCommand = true;
					Util.WriteDynamicColor("Mission complete", 50, ConsoleColor.Green);
					e.EventManager.CommandEntered -= oce3;
					Console.WriteLine();
					break;
			}
		}
	}
}
