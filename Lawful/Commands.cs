﻿using System.Net;
using System.Xml;

using Lawful.InputParser;
using Lawful.GameLibrary;

using static Lawful.GameLibrary.GameSession;

namespace Lawful;

public static class Commands
{
    public static void SSH(InputQuery Query)
	{
        if (Query.Arguments.Count > 0)
        {
            string[] LoginQuery = Query.Arguments[0].Split('@', StringSplitOptions.RemoveEmptyEntries);

            if (LoginQuery.Length < 2)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            string Username = LoginQuery[0];
            string Hostname = LoginQuery[1];

            // Error checking
            if (!IPAddress.TryParse(Hostname, out IPAddress TryIP))
            {
				Util.WriteLineColor("Invalid IP address specified", ConsoleColor.Red);
                return;
            }

            if (Player.CurrentSession.Host.Address == TryIP.ToString())
            {
                Util.WriteLineColor("Already connected to that machine, cannot perform SSH connection", ConsoleColor.Red);
                return;
            }

            Util.WriteColor($"Trying connection to '{TryIP}' ", ConsoleColor.Yellow);

            Util.BeginSpinningCursorAnimation(new string[4] { "▀", "■", "▄", "■" }, 8, 32, 75, Console.GetCursorPosition());

            Console.Write("- ");

            if (!Computers.HasComputer(TryIP.ToString()))
			{
				Util.WriteLineColor($"could not find a node at the IP address: '{TryIP}'", ConsoleColor.Red);
                return;
			}

            // Hostname is valid at this point, check Username

            Computer TryPC = Computers.GetComputer(TryIP.ToString());

            if (!TryPC.HasUser(Username))
			{
				Util.WriteLineColor($"node at IP '{TryIP}' does not contain user '{Username}'", ConsoleColor.Red);
                return;
			}


			Util.WriteLineColor("connected", ConsoleColor.Green);

            // Both are valid at this point, check if user has a password. If so, start login.
            UserAccount TryUser = TryPC.GetUser(Username);
                
            if (!Util.TryUserLogin(TryUser, 3))
			{
				Util.WriteLineColor("Connection closed by 3 invalid login attempts", ConsoleColor.Red);
                return;
			}

            Util.WriteLineColor($"Logged in as user '{Username}' at the connected node '{TryIP}'", ConsoleColor.Green);

            Player.CloseCurrentSession();
			TryPC.TryOpenSession(Username, out Player.CurrentSession);

            Events.FireSSHConnect(Player, Computers, Query, Events);
        }
	}

    public static void SwitchUser(InputQuery Query)
	{
        if (Query.Arguments.Count < 1)
		{
			Console.WriteLine("Insufficient parameters");
            return;
		}

        string Username = Query.Arguments[0];

        UserAccount TryAccount = Player.CurrentSession.Host.GetUser(Username);

        if (TryAccount is null)
		{
			Console.WriteLine($"Could not find a user by the username '{Username}'");
            return;
		}

        if (!Util.TryUserLogin(TryAccount, 3))
		{
			Console.WriteLine("Login cancelled or 3 invalid attempts were made");
            return;
		}

        // Actually switch to the user now
        Player.CurrentSession.User = TryAccount;

        Util.WriteColor("Success!", ConsoleColor.Green);
        Util.WriteLineColor($" :: Session now active for user '{Player.CurrentSession.User.Username}'", ConsoleColor.Yellow);
	}

    public static void Scan(InputQuery Query)
    {
        Console.Write("Scanning... ");

        Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 16, 48, 50, Console.GetCursorPosition());

        Util.WriteLineColor("done!", ConsoleColor.Green);
		Console.WriteLine();

        if (Player.CurrentSession.Host.ScanResults.Count > 0)
        {
			Console.WriteLine("Responses received:");

            foreach (ScanResult Result in Player.CurrentSession.Host.ScanResults)
                Util.WriteLineColor($"    {Result.Name}@{Result.Address}", ConsoleColor.Yellow);
        }
        else
        {
			Console.WriteLine("Scan did not find any PCs open to the internet");
		}
    }

    private static List<UserSession> ActiveSessions = new();

    // TODO (Carson): Figure out a way to check for permissions on the origin and destination(s) via the repsective users accessing them

    public static void SecureCopy(InputQuery Query)
	{
        if (Query.Arguments.Count == 0)
		{
			Util.WriteLineColor("Insufficent arguments", ConsoleColor.Red);
            return;
		}

        string Path;
        UserSession Current;

        if (Remote.TryGetRSI(Query.Arguments[0], out UserAccount RemoteOriginUser, out Computer RemoteOriginHost, out string RemoteOriginPath) == RSIStatus.Complete)
		{
            Util.WriteLineColor($"Connected to {RemoteOriginHost.Address}!", ConsoleColor.Green);
            // remote origin
            if (!Util.TryUserLogin(RemoteOriginUser, 3))
            {
                Util.WriteLineColor("Login cancelled or 3 invalid attempts were made", ConsoleColor.Red);
                return;
            }

            Path = RemoteOriginPath;
            Current = UserSession.FromConstituents(RemoteOriginHost, RemoteOriginUser);
            ActiveSessions.Add(Current);
        }
        else
		{
            // local origin
            Path = Query.Arguments[0];
            Current = Player.CurrentSession;
        }

        dynamic Origin = FSAPI.Locate(Current, Path);

        switch (Origin)
		{
            case XmlNode n:

                break;

            case XmlNodeList nl:

                break;

            default:
                Util.WriteLineColor($"Could not locate '{Path}'", ConsoleColor.Red);
                return;
		}

        // At this point, we have an origin but we do not know where to put it

        if (Query.Arguments.Count >= 2)
		{
            XmlNode Destination;

            // For every argument after the origin
            for (int i = 1; i < Query.Arguments.Count; i++)
			{
                if (Remote.TryGetRSI(Query.Arguments[i], out UserAccount RemoteDestinationUser, out Computer RemoteDestinationHost, out string RemoteDestinationPath) == RSIStatus.Complete)
				{
                    // If, for every session, it is true that this remote query does not remote query does not reference it, we know it is not in the list of active sessions and must be validated
                    if (ActiveSessions.TrueForAll(session => session.User != RemoteDestinationUser && session.Host != RemoteDestinationHost))
                    {
                        // remote origin
                        if (!Util.TryUserLogin(RemoteDestinationUser, 3))
                        {
                            Util.WriteLineColor("Login cancelled or 3 invalid attempts were made", ConsoleColor.Red);
                            return;
                        }

                        Current = UserSession.FromConstituents(RemoteDestinationHost, RemoteDestinationUser);
                        ActiveSessions.Add(Current);
                    }

                    Current = ActiveSessions.Find(session => session.User == RemoteDestinationUser && session.Host == RemoteDestinationHost);
                    Destination = FSAPI.LocateDirectory(Current, RemoteDestinationPath);

                    if (Destination is null)
					{
                        Util.WriteLineColor($"Could not find directory '{RemoteDestinationPath}' at {RemoteDestinationHost.Address}", ConsoleColor.Red);
                        return;
					}
                }
                else
				{
                    Destination = FSAPI.LocateDirectory(Player.CurrentSession, Query.Arguments[i]);

                    if (Destination is null)
                    {
                        Util.WriteLineColor($"Could not find directory '{Query.Arguments[i]}'", ConsoleColor.Red);
                        return;
                    }
                }
                    
                // Copy the file(s) here (check for permissions first!)
                switch (Origin)
				{
                    case XmlNode n:
                        // Check for permissions here

                        AnimatedFileTransfer(n, Destination);
                        break;

                    case XmlNodeList nl:
                        foreach (XmlNode n in nl)
						{

						}
                        break;
				}
			}
		}
	}

    public static void AnimatedFileTransfer(XmlNode Source, XmlNode Destination)
    {
        Console.CursorVisible = false;

        Util.WriteColor($"{Source.Attributes["Name"].Value,-40} -> {Destination.GetPath()} ", ConsoleColor.Yellow);

        XmlNode ToCopy = Source.Clone();
        Destination.AppendChild(ToCopy);

        Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 25, 150, 20, Console.GetCursorPosition());
        Util.WriteLineColor("√", ConsoleColor.Green);

        Console.CursorVisible = true;
    }

    //  private static List<Computer> ConnectedPCs = new();
    //  private static List<UserAccount> LoggedInAccounts = new();
    //  
    //  public static void SecureCopy(InputQuery Query)
    //  {
    //      if (Query.Arguments.Count == 0)
    //  	{
    //  		Console.WriteLine("Insufficient arguments");
    //          return;
    //  	}
    //  
    //      if (Query.Arguments.Count == 1)
    //  	{
    //          (bool Succeeded, dynamic Source) Arg1 = HandleOtherSCPArg(Query.Arguments[0]);
    //  
    //          XmlNode UserDir = Player.HomePC.GetNodeFromPath($"/home/{Player.ProfileName}");
    //          XmlNode UserBin = Player.HomePC.GetNodeFromPath("/bin");
    //  
    //          if (!Arg1.Succeeded) // The argument handler already prints our error messages for us so we just have to return
    //          {
    //              ConnectedPCs.Clear();
    //              LoggedInAccounts.Clear();
    //              return;
    //          }
    //  
    //          switch (Arg1.Source)
    //  		{
    //              case XmlNode n:
    //                  if (n.Attributes["Command"] is not null)
    //                      AnimatedFileTransfer(n, UserBin);
    //                  else
    //                      AnimatedFileTransfer(n, UserDir);
    //                  break;
    //  
    //              case XmlNodeList nl:
    //                  foreach (XmlNode n in nl)
    //  				{
    //                      if (n.Attributes["Command"] is not null)
    //                          AnimatedFileTransfer(n, UserBin);
    //                      else
    //                          AnimatedFileTransfer(n, UserDir);
    //                  }
    //                  break;
    //  		}
    //  	}
    //      else
    //  	{
    //          (bool Succeeded, dynamic Source) Arg1 = HandleOtherSCPArg(Query.Arguments[0]);
    //          (bool Succeeded, dynamic Source) Arg2 = HandleOtherSCPArg(Query.Arguments[1]);
    //  
    //          if (!Arg1.Succeeded || !Arg2.Succeeded)
    //          {
    //              ConnectedPCs.Clear();
    //              LoggedInAccounts.Clear();
    //              return;
    //          }
    //  
    //          // Sort out the destination
    //          switch (Arg2.Source)
    //  		{
    //              case XmlNode n:
    //                  if (n.Name != "Directory" && n.Name != "Root")
    //                      goto default;       // Handly little feature, I must say
    //                  break;
    //  
    //              default:
    //  				Console.WriteLine("Invalid destination, must be a folder");
    //                  ConnectedPCs.Clear();
    //                  LoggedInAccounts.Clear();
    //                  return;
    //  		}
    //  
    //          // Sort out the source
    //          switch (Arg1.Source)
    //          {
    //              case XmlNode n:
    //                  AnimatedFileTransfer(n, Arg2.Source);
    //                  break;
    //  
    //              case XmlNodeList nl:
    //                  foreach (XmlNode n in nl)
    //                      AnimatedFileTransfer(n, Arg2.Source);
    //                  break;
    //          }
    //      }
    //      ConnectedPCs.Clear();
    //      LoggedInAccounts.Clear();
    //  }
    //

    //  
    //  public static (bool, dynamic) HandleOtherSCPArg(string Argument)
    //  {
    //      // first we're going to determine the type of query
    //      RSIStatus QueryRSIStatus = Remote.GetRSIStatus(Argument);
    //  
    //      dynamic Source;
    //  
    //      switch (QueryRSIStatus)
    //      {
    //          case RSIStatus.None:
    //              // local query, use LocalLocate
    //              Source = NodeLocator.LocalLocate(Argument, in Player.ConnectionInfo);
    //              break;
    //  
    //          case RSIStatus.Complete:
    //              // remote query, use RemoteLocate with new ConnectionInfo
    //              string RSI = Argument.Split(':')[0];
    //              string[] RSIElements = RSI.Split('@');
    //  
    //              Computer PC = Computers.GetComputer(RSIElements[1]);
    //  
    //              if (!ConnectedPCs.Contains(PC)) // If we do not already have an active session on this computer, create one.
    //              {
    //                  ConnectedPCs.Add(PC);
    //  				Console.WriteLine($"Connected to {PC.Name}@{PC.Address}");
    //              }
    //  
    //              UserAccount Account = PC.GetUser(RSIElements[0]);
    //  
    //              if (!LoggedInAccounts.Contains(Account))    // If we are not already logged in to this particular account, log in.
    //  			{
    //                  Util.DoUserLogin(Account, 3);
    //                  LoggedInAccounts.Add(Account);
    //              }
    //  
    //              ConnectionInfo New = new() { PC = Computers.GetComputer(RSIElements[1]), User = Account };
    //              Source = NodeLocator.RemoteLocate(Argument, in New);
    //              break;
    //  
    //          default:
    //              Console.WriteLine($"Error in query '{QueryRSIStatus}'");
    //              return (false, null);
    //      }
    //  
    //      switch (Source)
    //      {
    //          case XmlNode n:
    //              return (true, n);
    //  
    //          case XmlNodeList nl:
    //              return (true, nl);
    //  
    //          default:
    //              Console.WriteLine($"Unable to resolve source query '{Argument}'");
    //              return (false, null);
    //      }
    //  }

    public static void MakeDirectory(InputQuery Query)
	{
        if (Query.Arguments.Count == 0)
		{
			Console.WriteLine("Insufficient arguments");
            return;
		}

        foreach (string Argument in Query.Arguments)
		{
            if (Remote.GetRSIStatus(Argument) > RSIStatus.InvalidIP)
			{
				Console.WriteLine("Cannot create directories remotely");
                return;
			}

            bool NameConflict = Player.CurrentSession.PathNode.GetNodeFromPath(Argument) != null;

            if (NameConflict)
			{
				Console.WriteLine("An object with that name already exists");
                return;
			}

            // We're all good at this point, create the directory

            if (Argument.Contains('/'))
			{
				Console.WriteLine("Name cannot contain slashes");
				Console.WriteLine("To create in a directory besides the CWD, specify the [p] or [path] named parameter with the directory you wish to create in");
				Console.WriteLine();
				Console.WriteLine("(that last bit is not implemented yet, sorry)");
                return;
			}

            XmlNode NewDirectory = Player.CurrentSession.PathNode.OwnerDocument.CreateElement("Directory");

            XmlAttribute NewDirectoryAttribute = Player.CurrentSession.PathNode.OwnerDocument.CreateAttribute("Name");
            NewDirectoryAttribute.InnerText = Argument;

            NewDirectory.Attributes.Append(NewDirectoryAttribute);

            Player.CurrentSession.PathNode.AppendChild(NewDirectory);
		}
	}

    public static void List(InputQuery Query)
    {
        XmlNode NodeToList = Player.CurrentSession.PathNode;

        if (Query.Arguments.Count > 0)
        {
            if (Remote.GetRSIStatus(Query.Arguments[0]) > RSIStatus.InvalidIP)
			{
				Console.WriteLine("Cannot list the contents of a remote directory");
                return;
			}

            switch (FSAPI.Locate(Player.CurrentSession, Query.Arguments[0]))
			{
                case XmlNode n:
                    NodeToList = n;
                    break;

                case XmlNodeList nl:
                    NodeToList = nl[0];
                    break;

                default:
					Console.WriteLine($"Could not resolve query: '{Query.Arguments[0]}'");
                    break;
			}
        }

		if (NodeToList.Name == "File")
		{
            if (!FSAPI.UserHasPermissions(Player.CurrentSession, NodeToList, PermissionType.Read))
            {
                Console.WriteLine($"'{Player.CurrentSession.User.Username}' is not permitted to perform that action");
                return;
            }

            Console.WriteLine($"Listing details for file '{NodeToList.Attributes["Name"].Value}'");
			Console.WriteLine();

			Console.Write("Path".PadRight(10));
            Util.WriteLineColor(NodeToList.GetPath(), ConsoleColor.Yellow);

			Console.Write("Length".PadRight(10));

            int Length = NodeToList.InnerText.Trim().Length;
            Util.WriteLineColor(Length.ToString() + (Length == 1 ? " character" : " characters"), ConsoleColor.Yellow);

            return;
		}

        if ( !FSAPI.UserHasPermissions(Player.CurrentSession, NodeToList, PermissionType.Read, PermissionType.Execute) )
        {
            Console.WriteLine($"'{Player.CurrentSession.User.Username}' is not permitted to perform that action");
            return;
        }

        Console.WriteLine($"Listing for '{NodeToList.GetPath()}'");
        Console.WriteLine();

        XmlNodeList Folders = NodeToList.SelectNodes("Directory");
        XmlNodeList Files = NodeToList.SelectNodes("File");
            
        if (Folders != null) {

            foreach (XmlNode Folder in Folders)
                Util.WriteLineColor(Folder.Attributes["Name"].Value, ConsoleColor.Yellow);
		}

        if (Files != null) {

            foreach (XmlNode File in Files)
                if (File.Attributes["Command"] is not null)
                    Util.WriteLineColor(File.Attributes["Name"].Value, ConsoleColor.Green);
                else
                    Console.WriteLine(File.Attributes["Name"].Value);
        }
    }

    public static void CD(InputQuery Query)
    {
        // Handle no arguments
        if (Query.Arguments.Count == 0)
        {
			Console.WriteLine("Insufficient parameters");
            return;
		}

        // Handle remote queries
        if (Remote.GetRSIStatus(Query.Arguments[0]) > RSIStatus.InvalidIP)
		{
			Console.WriteLine("Cannot CD into a remote directory");
            return;
		}

        // Handle query
        if (!FSAPI.TryGetNode(Player.CurrentSession, Query.Arguments[0], FSNodeType.Directory, out XmlNode TryDirectory))
		{
            Console.WriteLine($"Directory '{Query.Arguments[0]}' not found");
            return;
        }

        if (!FSAPI.UserHasPermissions(Player.CurrentSession, TryDirectory, PermissionType.Execute))
		{
            Console.WriteLine($"'{Player.CurrentSession.User.Username}' is not permitted to perform that action");
            return;
        }

        Player.CurrentSession.PathNode = TryDirectory;
	}

    public static void Move(InputQuery Query)
	{
        if (Query.Arguments.Count < 2)
		{
			Console.WriteLine("Insufficient parameters");
            return;
		}

        // Test for Remote System Identifiers (RSIs) on both the origin and destination queries
        if (Remote.GetRSIStatus(Query.Arguments[0]) > RSIStatus.InvalidIP || Remote.GetRSIStatus(Query.Arguments[1]) > RSIStatus.InvalidIP)
		{
			Console.WriteLine("Move does not support remote queries");
            return;
		}

        // Find each query

        if (!FSAPI.TryGetNode(Player.CurrentSession, Query.Arguments[0], out dynamic Origin))
		{
			Console.WriteLine($"Could not resolve origin query '{Query.Arguments[0]}'");
            return;
		}

        if (!FSAPI.TryGetNode(Player.CurrentSession, Query.Arguments[1], FSNodeType.Directory, out XmlNode Destination))
		{
            Console.WriteLine($"Directory '{Query.Arguments[1]}' not found");
            return;
        }

        switch (Origin)
		{
            case XmlNode o:
                Destination.AppendChild(o);
                break;

            case XmlNodeList nl:
                int count = nl.Count;

                for (int i = 0; i < count; i++)
                {
                    //if (nl[i] == Player.CurrentShell.PathNode) // An origin query involving multiple items may include the current directory so we must check for that
                    //    Player.CurrentShell.PathNode = d;

                    Destination.AppendChild(nl[0]);
                }
                break;
		}
	}

	public static XmlNode LocateNode(string Query)
	{
		if (Query.Length == 0) { return null; }

        if (Query[0] == '/')
            return Player.CurrentSession.Host.GetNodeFromPath(Query);
        else
            return Player.CurrentSession.PathNode.GetNodeFromPath(Query);
	}

    public static void Remove(InputQuery Query)
	{
        if (Query.Arguments.Count == 0)
		{
			Console.WriteLine("Insufficient parameters");
            return;
		}

        if (Remote.GetRSIStatus(Query.Arguments[0]) > RSIStatus.InvalidIP)
		{
            Console.WriteLine("Remove does not support remote queries");
            return;
        }

        XmlNode Traverser = Player.CurrentSession.PathNode;

        switch (FSAPI.Locate(Player.CurrentSession, Query.Arguments[0]))
        {
            case XmlNode n:
                if (n.Name == "Root")
                {
                    Console.WriteLine("Cannot delete the root directory");
                    return;
                }

                while (Traverser.Name != "Root")
                {
                    if (Traverser == n)
                    {
                        // error condition
                        Console.WriteLine("Cannot delete a parent of the current working directory");
                        return;
                    }
                    Traverser = Traverser.ParentNode;
                }

                n.ParentNode.RemoveChild(n);
                break;

            case XmlNodeList nl:
                XmlNode Parent = nl[0].ParentNode;

                while (Traverser.Name != "Root")
                {
                    foreach (XmlNode n in nl)
                        if (Traverser == n)
                        {
                            Console.WriteLine("Cannot delete a parent of the current working directory");
                            return;
                        }

                    Traverser = Traverser.ParentNode;
                }

                for (int i = nl.Count - 1; i >= 0; i--)
                    Parent.RemoveChild(nl[i]);

                break;

            default:
                Console.WriteLine($"Could not resolve query '{Query.Arguments[0]}'");
                break;
        }
	}

    public static void Concatenate(InputQuery Query)
    {
        if (Query.Arguments.Count == 0)
		{
			Console.WriteLine("Insufficient parameters");
            return;
		}

        if (Remote.GetRSIStatus(Query.Arguments[0]) > RSIStatus.InvalidIP)
		{
            Console.WriteLine("Concatenate does not support remote queries");
            return;
        }

        if (!FSAPI.TryGetNode(Player.CurrentSession, Query.Arguments[0], FSNodeType.File, out XmlNode File))
		{
            Console.WriteLine($"Could not find file '{Query.Arguments[0]}'");
            return;
        }

        Console.WriteLine(File.InnerText.Trim());
        Events.FireReadFile(Player, Computers, Query, Events, File);
    }

    public static void Sus() {
        Console.WriteLine($".   ....../#########((#(*,. ,...,,,,,......,..........,**((((/#(/////////////////");
        Console.WriteLine($"........../(######(/*,,.,......,.......................,,***/(#(#(///////////////");
        Console.WriteLine($"          ,(###(//,,,,...,*.............,,....,,,.....,,.,*///((((///////////////");
        Console.WriteLine($"           *,,,,,,..  . .,*,...,,,,*,*****///(///////*****,,,*,,,*///////////////");
        Console.WriteLine($"          .,,,....   ...,,,,,,,*////((((####%%%%%%######(((/*,,,....,**//////////");
        Console.WriteLine($".....  ...,,,.... ...,,,****,,/((((((####%%%%%%%%%%%%%####((((**,,...,/*/////////");
        Console.WriteLine($" .....,*,,,,..   .,,*******/,*(((((########%%%%%&%%%##########(/**,......////////");
        Console.WriteLine($",.***/,,*,,..   ,,****/*****,/((((((#####%%%%%&&&&%%%##########(/*,,.....,///////");
        Console.WriteLine($"*,****//...   .,**///*****,*((((((((#######%%%%%%%%%##########(((/*,,.....,//////");
        Console.WriteLine($"%%%%#(/*,..  ,,************((((############%%%%%&&%%%%%########((/**,....,*///(//");
        Console.WriteLine($"%%%%%%,,,....,**********/(((((((((##(#######%#%%%%%%%%%########(((**,.....*/////(");
        Console.WriteLine($"%%%%%%.,. ...*******//(((((((((((((((((##########%&&%%%#########((*,,.....*(/((/(");
        Console.WriteLine($"%%%%%%*.. ...********/(((((((((((((((((((##((#(#################((/*,.....//(((((");
        Console.WriteLine($"%%%%%%%*....,****,,,,,,,,**///////((((((((((((///*,,,,*,*,***//(##(/,,....(((((((");
        Console.WriteLine($"%%%%%%%#,..,*,.,,*//**,.,,,,,*****/////((////***,,.,,..,********/(((*,..  (((((((");
        Console.WriteLine($"%%%%%%%&. .,,,**/*,.  ..,....,,,*,***(((((/*/**,...***/#/....,*/(&%#(....,(((((((");
        Console.WriteLine($"%%%%%%%&,. ,,***,..,,**,   .*,..,,**/(#%%#(/*,,,,,*,.,,,*/***,/((//##,.///((((/((");
        Console.WriteLine($"%%%%%%#%*,.*//****,,,,*... .....,,,((((#%%(/.,..,,,,*,*/(#(/(##%%%###,,/(/(/(((((");
        Console.WriteLine($"%%%%%%%#*..*/***,,,,,****,,,,,,,,,,/(###%##(((/**,,*,,,**/(#(#((*/(((*,//((((((((");
        Console.WriteLine($"%%%%%%%%*..********,,,,,,,,*//**,,,*(##%%#((((((((((/////((######*/((*.*/(#((((((");
        Console.WriteLine($"%%%%%%%#,..*****///((((((((///******/(###((((((((((((////(((((#####/*///###(((/((");
        Console.WriteLine($"%%%%%%%#...,***///(((((((//********//(#%#(((/((((/////(((##(((((###((*##%%#(((/((");
        Console.WriteLine($"%%%%%%%**,.****////////*****,*****//((((#((((((((((((**,,**///////(/((###%%((((((");
        Console.WriteLine($"%%%%%%%%(,.******/**,,,,**,*******//(((((#(((((((((((///*,,,,**/##*(/(((((//(((((");
        Console.WriteLine($"%%%%%%%%%#.*******.,,,,,,****,*****/(#(((//**///(((((////*,,,,.,/(/*/(((///(/((((");
        Console.WriteLine($"%%%%%%%%%%%(*****.,,..,,,*****,,,,,,,******////(((//***,,...,, ,*/*//(((//(//((((");
        Console.WriteLine($"%%%%%%%%%%%%/***/,/***. ...*****,,,,,,,,*((///****,,,**...,*,,.///*/*/*///////(((");
        Console.WriteLine($"%%%%%%%%%%%(%****/*/****.. ,*****,************((#%(####.,(///,/*(,/*/////////*(((");
        Console.WriteLine($"%%%%%%##%%#,#(****/*(/***,.(%#%#%&/(##(/(&@##&@#&&%&&*,/(////,//*//(((////////(((");
        Console.WriteLine($"###%%%##%##.#%%/***/**/**,,. #%%&&%&&&&(&&@@&&&&&&%..*/((((//(/**/(((///*/////((/");
        Console.WriteLine($"###########*/%%%%/**//*/***,,...,/*%%%%%%&&%%,//,/#***/((((/((//(/////*/*.       ");
        Console.WriteLine($"###########(,%%###(*,*/*/**,,,,../*.,,,,,,,,,,#(&%//*/(##((((*/*/////*/**.    ...");
        Console.WriteLine($"###########%,######((**/******,,*/*,/((#(###((((////(((##((/***/*//**//*,.     ..");
        Console.WriteLine($"############((#(,,**,,,,//****//*,,**/////(//(////((((####(/(((%*#%%(((*..      .");
        Console.WriteLine($"########(/*,**///***./,,,*********/////(((((((((((((((###(///((((.*((#%%%* . ....");
        Console.WriteLine($"((*,,**,****,**///, ***,,,,*****/*///////(/((/(((((#(#(#(*///((((/.#####((##%%%%(");
        Console.WriteLine($"/*,*,,,,***,**///* ****,,,,,***/**/////(((/((((###((#(#*,,**/((((/,######%%%%#((#");
        Console.WriteLine($"**,,,,***,***********,,,*,,,,,,//////(/((((((((#((#((,*//((*,,(((**##############");
        Console.WriteLine($"*,,,,****/****/*/*.,,,,,**,**,,,/*///(((((((((((((((,*////((/,,,((###############");
        Console.WriteLine($"**,,****//*,,,****. *,,,,*****,,,,**///(/(///////,,**/////((((((*#(((############");
    }
}