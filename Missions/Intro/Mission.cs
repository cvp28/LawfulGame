using static System.Threading.Thread;

using Lawful.GameLibrary;
using static Lawful.GameLibrary.Util;

namespace Lawful.Mission
{
	public class Entry
	{
		public static void Load(EventManager e)
		{
			e.BootupSequenceStarted += IntroMission.OnBootupSequenceStarted;
		}

		public static void Unload(EventManager e)
		{
			e.BootupSequenceStarted -= IntroMission.OnBootupSequenceStarted;
		}
	}

	public class Data
	{
		public static bool OverrideCommand = false;
		public static bool ChangeMission = false;
		public static string NextMissionID = string.Empty;
	}

	public class IntroMission
	{
		public static void OnBootupSequenceStarted(EventGlobalType egt)
		{
			Sleep(2000);

			WriteDynamic("End of Sophomore Year, 2018\n", 50);
			Sleep(750);
			WriteDynamic("Lawful Estate\n", 50);
			Sleep(750);
			WriteDynamic("10:34 AM\n", 50);
			Console.WriteLine();
			Sleep(1000);

			Console.WriteLine("Dad: Alex! Can you come out here for a second?");
			BetterReadKey(); ;
			Console.WriteLine("Alex: Yeah?");
			BetterReadKey();
			Console.WriteLine("Dad: Report card came.");
			BetterReadKey();
			Console.WriteLine("Alex: Don't bother. Nothing but F's on that paper.");
			Sleep(2000);
			Console.WriteLine();
			Console.WriteLine("Dad: ...");
			Sleep(1250);
			Console.WriteLine("Alex: ...");
			Sleep(1250);
			WriteLineColor("(dramatic envelope ripping noises)", ConsoleColor.Yellow);
			Sleep(1000);
			Console.WriteLine();
			Console.WriteLine("Dad: Let's see here... WOW. All A's, huh?");
			BetterReadKey();
			Console.WriteLine("Alex: Hmm, must have been sent to the wrong address.");
			BetterReadKey();
			Console.WriteLine("Dad: Don't sell yourself short, bud. Remember our deal?");
			BetterReadKey();
			Console.WriteLine("Alex: I do actually, I'll go get ready!");
			BetterReadKey();

			Sleep(750);
			Console.WriteLine();
			WriteDynamic("Wavetronics Retail Outlet, Aisle 10\n", 50);
			Sleep(750);
			WriteDynamic("11:42 AM\n", 50);
			Console.WriteLine();
			Sleep(1000);

			Console.WriteLine("Dad: Alex, you have been staring at that shelf for 10 minutes.");
			BetterReadKey();
			Console.WriteLine("Alex: wait wha-");
			BetterReadKey();
			Console.WriteLine("Dad: Don't tell me you were daydreaming about a computer of all things.");
			BetterReadKey();
			Console.WriteLine("Alex: ");
			BetterReadKey();
			Console.WriteLine("Dad: Just hurry up and pick a computer already, the A/C in this place blows too cold.");
			BetterReadKey();
			Console.Write("Alex: ");
			Sleep(600);
			WriteColor("Points", ConsoleColor.Yellow);
			Console.Write(' ');
			Sleep(600);
			WriteDynamic("That one.\n", 25);

			Sleep(1000);
			Console.WriteLine();
			WriteDynamic("Law Estate\n", 50);
			Sleep(750);
			WriteDynamic("1:28 PM\n", 50);
			Console.WriteLine();
			Sleep(750);

			Console.WriteLine("Alex: Alright, cables are hooked up and everything looks to be in order.");
			BetterReadKey();
			Console.WriteLine("Alex: Let's see what this button does...");

			Sleep(1500);
			Console.Clear();
			Sleep(2000);
		}

		public static void BetterReadKey()
		{
			while (Console.KeyAvailable) { Console.ReadKey(true); }
			Console.ReadKey(true);
		}
	}
}