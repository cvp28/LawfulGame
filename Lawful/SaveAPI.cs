using System;
using System.IO;

using Lawful.GameObjects;
using static Lawful.Program;

namespace Lawful
{
	public static class SaveAPI
	{
		// We return a bool and a string determing if:
		// a. the save initialization was successful
		// and
		// b. the path to the user's save file
		public static (bool, string) InitSave(string UserPCName, string UserProfileName, string UserStorySelection)
		{
			Story UserStory = Story.DeserializeFromFile($@".\Content\Story\{UserStorySelection}\Story.xml");

			User User = new()
			{
				ProfileName = UserProfileName,
				StoryID = UserStorySelection,
				CurrentMissionID = UserStory.StartMissionID
			};

			// Define our path to the new user's save folder
			string PathToNewSaveFolder = Path.Combine(Directory.GetCurrentDirectory() + @$"\Content\Saves\{UserProfileName}");

			// Create our new save folder if it does not exist already
			if (!Directory.Exists(PathToNewSaveFolder))
				Directory.CreateDirectory(PathToNewSaveFolder);
			else
			{
				Console.WriteLine($"Save data folder for '{UserProfileName}' already exists");
				return (false, null);
			}

			try
			{
				// Load in the default ComputerStructure from our selected storyline, update it to the user's selected values for PC and Profile name, and re-serialize it to their save folder
				ComputerStructure Computers = ComputerStructure.DeserializeFromFile($@".\Content\Story\{UserStorySelection}\Computers.xml");

				Computer UserPC = Computers.GetComputer("REPLACETHIS");
				UserAccount UserAccount = UserPC.GetUser("REPLACETHIS");

				UserPC.Name = UserPCName;
				UserAccount.Username = UserProfileName;

				Computers.SerializeToFile($@"{PathToNewSaveFolder}\Computers.xml");

				// Do some initialization on the User object now that we have the ComputerStructure loaded in and then serialize that to the user's save folder
				User.HomePC = Computers.GetComputer(UserPCName);
				User.Account = User.HomePC.GetUser(UserProfileName);

				User.ConnectionInfo.PC = User.HomePC;
				User.ConnectionInfo.User = User.Account;
				User.ConnectionInfo.Drive = User.HomePC.GetSystemDrive();
				User.ConnectionInfo.PathNode = User.ConnectionInfo.Drive.Root;

				User.SerializeToFile($@"{PathToNewSaveFolder}\User.xml");

				// And we're done, report success and the path to the user's save file
				return (true, $@"{PathToNewSaveFolder}\User.xml");
			}
			catch (Exception e)
			{
				Console.WriteLine("Caught exception while writing save data");
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
				Console.WriteLine();
				Console.WriteLine("Send this information to the developer or post it online for help!");

				return (false, null);
			}
		}

		public static void LoadGameFromSave(string PathToSave)
		{
			// Initialize game objects
			Player = User.DeserializeFromFile(PathToSave);

			CurrentStory = Story.DeserializeFromFile($@".\Content\Story\{Player.StoryID}\Story.xml");
			CurrentMission = CurrentStory.GetMission(Player.CurrentMissionID);

			Computers = ComputerStructure.DeserializeFromFile($@".\Content\Saves\{Player.ProfileName}\Computers.xml");

			// Initialize some connection-related values given the computer structure for this save
			Player.PostDeserializationInit(Computers);

			Events = new();

			// Load current mission
			string PathToCurrentMissionAssembly = Path.GetFullPath(@$".\Content\Story\{Player.StoryID}\{CurrentMission.AssemblyPath}");

			if (!File.Exists(PathToCurrentMissionAssembly))
			{
				Console.WriteLine("Error loading current mission");
				Console.WriteLine($"Could not find mission assembly referenced by:\n'{PathToCurrentMissionAssembly}'");
				Console.ReadKey(true);
				Environment.Exit(-1);
			}

			MissionAPI.LoadMission(Player.CurrentMissionID);
		}
	}
}