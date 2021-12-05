using System.Reflection;
using System.Runtime.Loader;

namespace Lawful.GameLibrary;

// Convenient place to store all session-specific data
public static class GameSession
{
	public static User? Player;
	public static EventManager? Events;
	public static ComputerStructure? Computers;

	public static Assembly? CurrentMissionAssembly;
	public static AssemblyLoadContext? MissionAssemblyLoader;

	public static Story? CurrentStory;
	public static StoryMission? CurrentMission;
}
