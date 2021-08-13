using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Reflection;
using System.Runtime.Loader;

using Lawful.InputParser;
using Lawful.GameObjects;

namespace Lawful
{
   public enum GameState
    {
        MainMenu,
        NewGame,
        LoadSave,
        LoadingAnimation,
        Console,
        Exiting
    }

    class Program
	{
        public static bool Active = true;
        public static GameState CurrentState = GameState.MainMenu;

        public static User Player;
        public static EventManager Events;
        public static ComputerStructure Computers;

        public static Assembly CurrentMissionAssembly;
        public static AssemblyLoadContext MissionAssemblyLoader;

        public static Story CurrentStory;
        public static StoryMission CurrentMission;

        // other objects will go here once I build a way to serialize them

        internal const string MissionAssemblyNamespace = "Lawful.Mission";
        internal const string MissionAssemblyEntryClassName = "Entry";
        internal const string MissionAssemblyDataClassName = "Data";

        static void Main()
        {
            // Init
            Directory.SetCurrentDirectory(@"C:\Users\Carson\source\repos\Lawful");

            while (Active)
            {
                while (CurrentState == GameState.MainMenu)
                {
                    Console.Clear();
                    PrintTitle(true);

                    Console.WriteLine();
					Console.WriteLine(" 1    New Game");
					Console.WriteLine(" 2    Load Saved Game");
					Console.WriteLine();
					Console.WriteLine(" 3    Exit");
					Console.WriteLine();

                    Console.Write("> ");

                    // New, handles save games
                    switch (Console.ReadLine())
					{
                        case "1":
                            CurrentState = GameState.NewGame;
                            continue;

                        case "2":
                            CurrentState = GameState.LoadSave;
                            continue;

                        case "3":
                            CurrentState = GameState.Exiting;
                            continue;
                    }

                    // Old
                    //  switch (Console.ReadLine())
                    //  {
                    //      case "1":
                    //          Console.Clear();
                    //  
                    //          // Replace this bit with code that loads save data determined by the user
                    //          Console.Write("Loading user data, ");
                    //              xUser.Load(@".\User.xml");
                    //          
                    //              Console.Write("creating user... ");
                    //              Player = User.FromFile(ref xUser);
                    //          Console.WriteLine("done.");
                    //  
                    //          Console.Write("Loading network structure... ");
                    //              xNetworkStructure.Load(@".\Computers.xml");
                    //              xNetworks = xNetworkStructure.SelectSingleNode("Networks");
                    //          Console.WriteLine("done.");
                    //  
                    //          Console.Write("Setting current mission...");
                    //              XmlDocument xMission = new XmlDocument();
                    //              xMission.Load(@".\Missions\shouldersofgiants\test_mission_precompiledassembly.xml");
                    //              Player.CurrentMissionNode = xMission.SelectSingleNode("DynamicMission");
                    //              Player.CurrentMission = new DynamicMission(Player.CurrentMissionNode);
                    //          Console.Write("done.");
                    //  
                    //          Player.ConnectToHome();
                    //  
                    //          Console.WriteLine();
                    //          Console.WriteLine();
                    //  
                    //          CurrentState = GameState.Console;
                    //          continue;
                    //  
                    //      case "2":
                    //          CurrentState = GameState.Exiting;
                    //          continue;
                    //  
                    //      default:
                    //          continue;
                    //  }
                }

                while (CurrentState == GameState.NewGame)
				{

                    Console.Clear();
                    PrintTitle(false);

					Console.WriteLine();

                    Console.WriteLine("Select a Storyline");
					Console.WriteLine();

                    foreach (string dir in Directory.GetDirectories($@".\Content\Story"))
					{
						Util.WriteLineColor(dir.Split('\\')[^1], ConsoleColor.Cyan);
					}

					Console.WriteLine();
                    Console.Write("> ");

                    string UserStorySelection = Console.ReadLine();

                    if (UserStorySelection.ToUpper() == "EXIT")
                    {
                        CurrentState = GameState.MainMenu;
                        continue;
                    }
                    else if (!Directory.Exists($@".\Content\Story\{UserStorySelection}"))
                        continue;

                    Console.WriteLine();
					Console.WriteLine("Enter User Info");
					Console.WriteLine();
                    Console.Write("Enter a PC Name      : ");
                    string UserPCName = Console.ReadLine();

					Console.Write("Enter a Profile Name : ");
                    string UserProfileName = Console.ReadLine();

                    (bool Succeeded, string PathToSave) = SaveAPI.InitSave(UserPCName, UserProfileName, UserStorySelection);

                    if (Succeeded)
					{
                        SaveAPI.LoadGameFromSave(PathToSave);
                        CurrentState = GameState.LoadingAnimation;
                        continue;
                    }
                    else
					{
						Console.WriteLine();
						Console.WriteLine("Failed to create save file");
                        Console.ReadKey(true);
                        CurrentState = GameState.MainMenu;
					}
                }

                while (CurrentState == GameState.LoadSave)
				{
                    Console.Clear();
                    PrintTitle(false);

					Console.WriteLine();
					Console.WriteLine("Load a saved game");
					Console.WriteLine();
                    foreach (string Dir in Directory.GetDirectories(@".\Content\Saves"))
					{
                        string SaveName = Dir.Split('\\')[^1];
                        Util.WriteColor($"{SaveName}".PadRight(20), ConsoleColor.Green);
                        Util.WriteLineColor($"Last accessed on {File.GetLastAccessTime($@".\Content\Saves\{SaveName}\User.xml")}", ConsoleColor.Yellow);
					}
					Console.WriteLine();

                    Console.Write("Save To Load ('exit' to cancel) : ");

                    ConsoleColor RevertTo = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    string Input = Console.ReadLine();
                    Console.ForegroundColor = RevertTo;

                    if (Input.ToUpper() == "EXIT")
					{
                        CurrentState = GameState.MainMenu;
                        continue;
					}

                    if (Directory.Exists($@".\Content\Saves\{Input}"))
					{
						Console.Write("Loading... ");
                        SaveAPI.LoadGameFromSave($@".\Content\Saves\{Input}\User.xml");
						Console.WriteLine("done.");
						
                        Console.WriteLine();
                        CurrentState = GameState.Console;
                        continue;
					}
                    else
					    continue;
				}

                while (CurrentState == GameState.LoadingAnimation)
				{
                    Events.FireBootupSequenceStarted(Player, Computers);

                    Console.CursorVisible = false;
                    Console.Clear();

                    Util.WriteLineColor("V Systems Company", ConsoleColor.Yellow);
                    Util.WriteLineColor("(C) 2018", ConsoleColor.Yellow);
                    Console.WriteLine();

                    Thread.Sleep(750);

                    Console.Write("Checking RAM");
                    for (int i = 0; i < 20; i++)
                    {
                        Console.Write('.');
                        Thread.Sleep(50);
                    }
                    Util.WriteLineColor(" 8192 MB OK", ConsoleColor.Green);
                    Console.WriteLine();

                    Thread.Sleep(500);

                    Console.WriteLine("Checking for storage devices...");
                    Thread.Sleep(250);
                    Util.WriteLineColor("    Found Device! :: 0x01 :: 2TB Toshiba Drive   (Mechanical)", ConsoleColor.Green);
                    Thread.Sleep(500);
                    Util.WriteLineColor("    Found Device! :: 0x02 :: 500GB Samsung Drive (Solid-state)", ConsoleColor.Green);
                    Thread.Sleep(250);
                    Console.WriteLine("Done!");
                    Console.WriteLine();

                    Thread.Sleep(500);

                    Util.WriteLineColor("Booting from EFI partition on device '0x02'...", ConsoleColor.Yellow);
                    Thread.Sleep(1000);

                    Console.Clear();
                    Console.CursorVisible = true;

                    Thread.Sleep(1500);

					Console.WriteLine();

                    Thread.Sleep(500);

                    Console.CursorVisible = false;
                    Console.Clear();

                    Console.WriteLine("Kennedy Computers Microprocessor Kernel");
                    Console.WriteLine("(C) 2020");
                    Console.WriteLine();

                    Thread.Sleep(750);

                    Console.WriteLine("Loading modules... ");
                    Thread.Sleep(500);

                    Util.WriteColor("  [fs.sys]      Common Filesystem Driver", ConsoleColor.Yellow);
                    for (int i = 0; i < 20; i++)
                    {
                        Console.Write('.');
                        Thread.Sleep(50);
                    }
                    Util.WriteLineColor(" loaded", ConsoleColor.Green);

                    Util.WriteColor("  [netman.sys]  Network Management Driver", ConsoleColor.Yellow);
                    for (int i = 0; i < 10; i++)
                    {
                        Console.Write('.');
                        Thread.Sleep(50);
                    }
                    Util.WriteLineColor(" loaded", ConsoleColor.Green);

                    Util.WriteColor("  [kcon.sys]    Kennedy Console Driver", ConsoleColor.Yellow);
                    for (int i = 0; i < 25; i++)
                    {
                        Console.Write('.');
                        Thread.Sleep(50);
                    }
                    Util.WriteLineColor(" loaded", ConsoleColor.Green);

                    Util.WriteColor("  [session.sys] User-Space Session Handler Driver", ConsoleColor.Yellow);
                    for (int i = 0; i < 15; i++)
                    {
                        Console.Write('.');
                        Thread.Sleep(50);
                    }
                    Util.WriteLineColor(" loaded", ConsoleColor.Green);
                    Console.WriteLine();

                    Thread.Sleep(250);

                    Util.WriteColor("  Module reliability checking... ", ConsoleColor.Yellow);
                    Thread.Sleep(500);
                    Util.WriteLineColor(" clean, no errors", ConsoleColor.Green);

                    Thread.Sleep(250);

                    Console.WriteLine("Module load finished");

                    Thread.Sleep(750);

                    Console.WriteLine();
                    Console.WriteLine();

                    Console.Write("Initiating user session... ");
                    Thread.Sleep(500);
                    Util.WriteLineColor("done", ConsoleColor.Green);
                    Console.WriteLine();

                    Thread.Sleep(250);

                    Console.WriteLine();

                    Thread.Sleep(500);

                    Console.WriteLine("Welcome.");
                    Console.WriteLine();

                    Thread.Sleep(1500);

					Console.Write( "[kcon]::AllocateConsole : Allocating console... ");

                    Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 16, 48, 50, Console.GetCursorPosition());

                    Console.CursorVisible = false;

                    Random random = new();
                    int num = random.Next(10000000, 99999999);
                    string HexString = num.ToString("X");

                    Console.WriteLine("done.");
                    Console.WriteLine($"kcon handle: 0x{HexString}");

					Console.WriteLine();

                    Thread.Sleep(500);

                    Console.CursorVisible = true;

                    Events.FireBootupSequenceCompleted(Player, Computers);

                    CurrentState = GameState.Console;
                }

                while (CurrentState == GameState.Console)
                {
					#region Handle User Input

					Util.PrintPrompt();
                    string UserInput = Console.ReadLine();
                    InputQuery UserQuery = Parser.Parse(UserInput);

                    Console.WriteLine();

                    Events.FireCommandEntered(Player, Computers, UserQuery);

                    if (!MissionAPI.GetMissionData<bool>("OverrideCommand"))
                        HandleQuery(UserQuery);
                    else
                        MissionAPI.SetMissionData("OverrideCommand", false);

                    Events.FireCommandExecuted(Player, Computers, UserQuery);

                    Console.WriteLine();

					#endregion

					// Has the mission assembly instructed us to change to another mission?
					if (MissionAPI.GetMissionData<bool>("ChangeMission"))
                    {
                        string NextMissionID = MissionAPI.GetMissionData<string>("NextMissionID");

                        MissionAPI.UnloadCurrentMission();
                        MissionAPI.LoadMission(NextMissionID);
                    }
                }

                if (CurrentState == GameState.Exiting) {
                    Console.WriteLine("Exiting...");
                    Active = false;
                }
            }
        }

        public static void HandleQuery(InputQuery Query)
        {
            // process built-in commands first

            switch (Query.Command.ToUpper())
			{
                case "EXIT":
                    CurrentState = GameState.MainMenu;
                    break;

                case "QUERY":
                    if (Query.Arguments.Count > 0)
                        Console.WriteLine(NodeLocator.GetRSIStatus(Query.Arguments[0]));
                    return;

                case "CURRENTMISSION":
                    Console.WriteLine(CurrentMissionAssembly.FullName);
					Console.WriteLine(CurrentMissionAssembly.Location);
                    return;

                case "SCP":
                    Commands.SecureCopy(Query);
                    return;
            }

            XmlNode TryExecuteBin = Player.ConnectionInfo.PC.GetPrimaryDisk().GetNodeFromPath($"bin/{Query.Command}");
            XmlNode TryExecuteLocal = NodeLocator.LocalLocate(Query.Command, in Player.ConnectionInfo);
            XmlNode TryExecute;

            if (TryExecuteBin is not null)
			{
                TryExecute = TryExecuteBin;
                goto ExecuteCommand;
			}

            if (TryExecuteLocal is not null)
            {
                TryExecute = TryExecuteLocal;
                goto ExecuteCommand;
            }

            Console.WriteLine($"File not found '{Query.Command}'");
            return;

            ExecuteCommand:

            if (TryExecute.Attributes["Command"] is null)
            {
                Console.WriteLine($"File is not an executable '{Query.Command}'");
                return;
            }

            string Command = TryExecute.Attributes["Command"].Value.ToUpper();

            switch (Command)
            {
                //  case "TEST":
				//  	Console.WriteLine(CurrentStory.GetMission("mission_2").AssemblyPath);
                //      break;

                case "SSH":
                    Commands.SSH(Query);
                    break;

                case "SU":
                    Commands.SwitchUser(Query);
                    break;

                case "SCAN":
                    Commands.Scan(Query);
                    break;

                case "SCP":
                    Commands.SecureCopy(Query);
                    break;

                case "CAT":
                    Commands.Concatenate(Query);
                    break;

                case "LS":
                    Commands.List(Query);
                    break;

                case "CD":
                    Commands.CD(Query);
                    break;

                case "PWD":
					Console.WriteLine(Player.ConnectionInfo.Path);
                    break;

                case "RM":
                    Commands.Remove(Query);
                    break;

                case "MV":
                    Commands.Move(Query);
                    break;

                case "SAVE":
                    // Save network structure
                    //  XmlWriter NetworkStructureWriter = XmlWriter.Create($@".\Content\Saves\{Player.ProfileName}\Computers.xml", new XmlWriterSettings()
                    //  {
                    //      OmitXmlDeclaration = true,
                    //      ConformanceLevel = ConformanceLevel.Fragment,
                    //      Indent = true,
                    //      IndentChars = "    "
                    //  });
                    break;

                case "CHANGEROOT":
                case "CR":
                    Commands.ChangeRoot(Query);
                    break;

                //  case "LOGIN":
                //      Commands.Login(Query);
                //      break;

                case "DISCONNECT":
                case "DC":
                    if (Player.ConnectionInfo.PC is not null && Player.ConnectionInfo.PC.Name == Player.HomePC.Name)
                    {
                        Console.WriteLine("Not connected to an external node");
                        return;
                    }
                    Player.ConnectionInfo.PC = Player.HomePC;
                    Player.ConnectionInfo.User = Player.HomePC.GetUser(Player.ProfileName);
                    Player.ConnectionInfo.Disk = Player.HomePC.GetPrimaryDisk();
                    Player.ConnectionInfo.PathNode = Player.HomePC.GetPrimaryDisk().Root;
                    Console.WriteLine("Disconnected");
                    break;

                case "CLEAR":
                    Console.Clear();
                    break;

                case "SUS":
                    Commands.Sus();
                    break;
            }
        }

        public static void PrintTitle(bool ShowCreatorName)
		{
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                  "     __                           ____            __" + '\n' +
                  "    / /     ______   _      __   / __/  __  __   / /" + '\n' +
                  "   / /     / __  /  | | /| / /  / /_   / / / /  / / " + '\n' +
                  "  / /___  / /_/ /   | |/ |/ /  / __/  / /_/ /  / /  " + '\n' +
                 @" /_____/  \__/\_\   |__/|__/  /_/     \____/  /_/   "
            );
            if (ShowCreatorName)
			{
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(@" \ A game by Carson Ver Planck");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}