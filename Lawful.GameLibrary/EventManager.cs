using System.Xml;

using Lawful.InputParser;

namespace Lawful.GameLibrary;

public enum EventType
{
	ReadFile,
	DeleteFile,
	ChangeDirectory,
	DeleteDirectory,
	LoginNetwork,
	SSHConnect,
	CommandEntered,
	CommandExecuted,
	BootupSequenceStarted,
	BootupSequenceCompleted
}

public class EventManager
{
	public event Action<EventGlobalType> ReadFile;
	public event Action<EventGlobalType> DeleteFile;
	public event Action<EventGlobalType> ChangeDirectory;
	public event Action<EventGlobalType> DeleteDirectory;
	public event Action<EventGlobalType> SSHConnect;
	public event Action<EventGlobalType> CommandEntered;
	public event Action<EventGlobalType> CommandExecuted;
	public event Action<EventGlobalType> BootupSequenceStarted;
	public event Action<EventGlobalType> BootupSequenceCompleted;

	#region Event Triggers

	public void FireSSHConnect(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events)
	{
		SSHConnect?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = null
		});
	}

	public void FireReadFile(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events, XmlNode FileNode)
	{
		ReadFile?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = FileNode
		});
	}

	public void FireDeleteFile(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events, string PathToFile)
	{
		DeleteFile?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = PathToFile
		});
	}

	public void FireChangeDirectory(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events, XmlNode Directory)
	{
		ChangeDirectory?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = Directory
		});
	}

	public void FireDeleteDirectory(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events, XmlNode Directory)
	{
		DeleteDirectory?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = Directory
		});
	}

	public void FireCommandEntered(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events)
	{
		CommandEntered?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = null
		});
	}

	public void FireCommandExecuted(User Player, ComputerStructure Computers, InputQuery Query, EventManager Events)
	{
		CommandExecuted?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = Query,
			EventManager = Events,
			Argument = null
		});
	}

	public void FireBootupSequenceStarted(User Player, ComputerStructure Computers, EventManager Events)
	{
		BootupSequenceStarted?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = InputQuery.Empty(),
			EventManager = Events,
			Argument = null
		});
	}

	public void FireBootupSequenceCompleted(User Player, ComputerStructure Computers, EventManager Events)
	{
		BootupSequenceCompleted?.Invoke(new EventGlobalType()
		{
			Player = Player,
			ComputerStructure = Computers,
			Query = InputQuery.Empty(),
			EventManager = Events,
			Argument = null
		});
	}

	#endregion
}
