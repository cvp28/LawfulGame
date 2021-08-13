using System;

using Lawful.GameObjects;

namespace Lawful.Mission
{
	public static class Entry
	{
		public static void Load(EventManager e)
		{
			e.CommandEntered += Mission.OnCommandEntered;
		}

		public static void Unload(EventManager e)
		{
			e.CommandEntered -= Mission.OnCommandEntered;
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
		public static void OnCommandEntered(EventGlobalType e)
		{
			switch (e.Query.Command.ToUpper())
			{
				case "SUS":
					Data.OverrideCommand = true;
					Console.WriteLine("Sussy");
					break;
			}
		}
	}
}
