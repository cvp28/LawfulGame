using System;
using System.IO;
using System.Reflection;

using static Lawful.GameLibrary.Session;
using static Lawful.GameLibrary.Constants;

namespace Lawful.GameLibrary
{
	public static class MissionAPI
	{
        public static void LoadMission(string MissionID)
        {
            MissionAssemblyLoader = new(null, true);

            StoryMission TryMission = CurrentStory.GetMission(MissionID);

            if (TryMission is null)
			{
				Console.WriteLine($"Could not load mission by ID '{MissionID}'");
				Console.WriteLine($"'{CurrentStory.Name}' does not contain mission by ID '{MissionID}'");
                Console.ReadKey(true);
                Environment.Exit(-1);
			}

            string PathToMissionAssembly = Path.GetFullPath(@$".\Content\Story\{CurrentStory.Name}\{TryMission.AssemblyPath}");

            try
            {
                // Load the mission assembly into a new context, nab its Load method, and then invoke it
                CurrentMissionAssembly = MissionAssemblyLoader.LoadFromAssemblyPath(PathToMissionAssembly);

                Delegate LoadHandler = CurrentMissionAssembly
                    .GetType($"{MissionAssemblyNamespace}.{MissionAssemblyEntryClassName}")
                    .GetMethod("Load")
                    .CreateDelegate<Action<EventManager>>();

                LoadHandler.DynamicInvoke(Events);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Loading the current mission assembly");
                Console.WriteLine($"Assembly: {PathToMissionAssembly}");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();
            }
        }

        public static void UnloadCurrentMission()
        {
            try
            {
                // Get the current mission's Unload method, invoke it, and then unload the mission assembly
                Delegate UnloadHandler = CurrentMissionAssembly
                    .GetType($"{MissionAssemblyNamespace}.{MissionAssemblyEntryClassName}")
                    .GetMethod("Unload")
                    .CreateDelegate<Action<EventManager>>();

                UnloadHandler.DynamicInvoke(Events);

                MissionAssemblyLoader.Unload();
                MissionAssemblyLoader = null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error unloading the current mission assembly");
                Console.WriteLine($"Assembly: {CurrentMission.AssemblyPath}");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();
            }
        }

        public static T GetMissionData<T>(string Name)
        {
            Type TryType = CurrentMissionAssembly.GetType($"{MissionAssemblyNamespace}.{MissionAssemblyDataClassName}");
            FieldInfo TryField = TryType?.GetRuntimeField(Name);

            if (TryType is not null && TryField is not null)
                return (T)TryField.GetValue(0);
            else
                return default;
        }

        public static void SetMissionData(string Name, object Value)
        {
            Type TryType = CurrentMissionAssembly.GetType($"{MissionAssemblyNamespace}.{MissionAssemblyDataClassName}");
            FieldInfo TryField = TryType?.GetRuntimeField(Name);

            if (TryType is not null && TryField is not null)
                TryField.SetValue(null, Value);
        }
    }
}