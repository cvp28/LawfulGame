using System.Reflection;
using System.Runtime.Loader;

namespace Lawful.GameLibrary
{
	public static class Session
	{
		public static User Player;
		public static EventManager Events;
		public static ComputerStructure Computers;

		public static Assembly CurrentMissionAssembly;
		public static AssemblyLoadContext MissionAssemblyLoader;

		public static Story CurrentStory;
		public static StoryMission CurrentMission;
	}
}
