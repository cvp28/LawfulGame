#r "C:\Users\Carson\source\repos\LawfulGame\Lawful.GameObjects\bin\Release\net5.0\Lawful.GameObjects.dll"

using Lawful.GameObjects;
using System.Collections.Generic;

Story Lawful = new()
{
	StartMissionID = "shouldersofgiants",
	Name = "Lawful"
};

Lawful.Missions.Add(new()
{
	Name = "Stood Upon the Shoulders of Giants",
	ID = "shouldersofgiants",
	AssemblyPath = @"Missions\shouldersofgiants\net5.0\shouldersofgiants.dll"
});

Lawful.SerializeToFile(@"..\Content\Story\Lawful\Story.xml");