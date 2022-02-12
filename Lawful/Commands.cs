using System.Net;
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
				Console.WriteLine("Insufficient arguments");
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
        if (Query.Arguments.Count == 0)
		{
			Console.WriteLine("Insufficient arguments");
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

        Util.BeginCharacterAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 16, 48, 50, Console.GetCursorPosition());

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

    #region TODO (Carson): Update SecureCopy

    //  private static List<UserSession> ActiveSessions = new();

    // TODO (Carson): Figure out a way to check for permissions on the origin and destination(s) via the repsective users accessing them

    // 1) Check the origin to the extent where the relevant user has MODIFY permissions for the parent of every node of the specified path
    // 2) Check all destinations to the extent where users have MODIFY permissions for the specified path

    //  public static void SecureCopy(InputQuery Query)
    //  {
    //      // No matter what, every new execution of the command will start with an empty list of active UserSessions
    //      ActiveSessions.Clear();
    //  
    //      if (Query.Arguments.Count == 0)
    //  	{
    //  		Util.WriteLineColor("Insufficent arguments", ConsoleColor.Red);
    //          return;
    //  	}
    //  
    //      string Path;
    //      UserSession Current;
    //  
    //      if (Remote.TryGetRSI(Query.Arguments[0], out UserAccount RemoteOriginUser, out Computer RemoteOriginHost, out string RemoteOriginPath) == RSIStatus.Complete)
    //  	{
    //          Util.WriteLineColor($"Connected to {RemoteOriginHost.Address}!", ConsoleColor.Green);
    //          // remote origin
    //          if (!Util.TryUserLogin(RemoteOriginUser, 3))
    //          {
    //              Util.WriteLineColor("Login cancelled or 3 invalid attempts were made", ConsoleColor.Red);
    //              return;
    //          }
    //  
    //          Path = RemoteOriginPath;
    //          Current = UserSession.FromConstituents(RemoteOriginHost, RemoteOriginUser);
    //          ActiveSessions.Add(Current);
    //      }
    //      else
    //  	{
    //          // local origin
    //          Path = Query.Arguments[0];
    //          Current = Player.CurrentSession;
    //      }
    //  
    //      dynamic Origin = FSAPI.Locate(Current, Path);
    //  
    //      if (Origin is null)
    //  	{
    //          Util.WriteLineColor($"Could not locate '{Path}'", ConsoleColor.Red);
    //          return;
    //      }
    //      
    //      switch (Origin)
    //  	{
    //          case XmlNode n:
    //              if (!FSAPI.UserHasPermissions(Current, n.ParentNode, PermissionType.Write))
    //  			{
    //                  Util.WriteLineColor($"Insufficient permissions for '{n.GetPath()}'", ConsoleColor.Red);
    //                  return;
    //              }
    //              break;
    //  
    //          case XmlNodeList nl:
    //              foreach (XmlNode n in nl)
    //  			{
    //                  if (!FSAPI.UserHasPermissions(Current, n.ParentNode, PermissionType.Write))
    //                  {
    //                      Util.WriteLineColor($"Insufficient permissions for '{n.GetPath()}'", ConsoleColor.Red);
    //                      return;
    //                  }
    //              }
    //              break;
    //  	}
    //  
    //      // At this point, we have a valid origin but we do not know where to put it
    //  
    //      if (Query.Arguments.Count >= 2)
    //  	{
    //          XmlNode Destination;
    //  
    //          // For every destination argument after the origin
    //          for (int i = 1; i < Query.Arguments.Count; i++)
    //  		{
    //              if (Remote.TryGetRSI(Query.Arguments[i], out UserAccount RemoteDestinationUser, out Computer RemoteDestinationHost, out string RemoteDestinationPath) == RSIStatus.Complete)
    //  			{
    //                  // If, for every session, it is true that this remote query does not reference it, we know it is not in the list of active sessions and must be validated
    //                  if (ActiveSessions.TrueForAll(session => session.User != RemoteDestinationUser && session.Host != RemoteDestinationHost))
    //                  {
    //                      // remote origin
    //                      if (!Util.TryUserLogin(RemoteDestinationUser, 3))
    //                      {
    //                          Util.WriteLineColor("Login cancelled or 3 invalid attempts were made", ConsoleColor.Red);
    //                          return;
    //                      }
    //  
    //                      Current = UserSession.FromConstituents(RemoteDestinationHost, RemoteDestinationUser);
    //                      ActiveSessions.Add(Current);
    //                  }
    //  
    //                  Current = ActiveSessions.Find(session => session.User == RemoteDestinationUser && session.Host == RemoteDestinationHost);
    //                  Destination = FSAPI.LocateDirectory(Current, RemoteDestinationPath);
    //  
    //                  if (Destination is null)
    //  				{
    //                      Util.WriteLineColor($"Could not find directory '{RemoteDestinationUser}@{RemoteDestinationHost}:{RemoteDestinationPath}'", ConsoleColor.Red);
    //                      return;
    //  				}
    //  
    //                  if (!FSAPI.UserHasPermissions(Current, Destination, PermissionType.Read))
    //  				{
    //                      Util.WriteLineColor($"Insufficient permissions for '{RemoteDestinationUser}@{RemoteDestinationHost}:{RemoteDestinationPath}'", ConsoleColor.Red);
    //                      return;
    //  				}
    //              }
    //              else
    //  			{
    //                  Current = Player.CurrentSession;
    //                  Destination = FSAPI.LocateDirectory(Current, Query.Arguments[i]);
    //  
    //                  if (Destination is null)
    //                  {
    //                      Util.WriteLineColor($"Could not find directory '{Query.Arguments[i]}'", ConsoleColor.Red);
    //                      return;
    //                  }
    //  
    //                  if (!FSAPI.UserHasPermissions(Current, Destination, PermissionType.Write))
    //  				{
    //                      Util.WriteLineColor($"Insufficient permissions for '{Query.Arguments[i]}'", ConsoleColor.Red);
    //                      return;
    //  				}
    //              }
    //                  
    //              // Copy the file(s) here
    //              switch (Origin)
    //  			{
    //                  case XmlNode n:
    //                      AnimatedFileTransfer(n, Destination);
    //                      break;
    //  
    //                  case XmlNodeList nl:
    //                      foreach (XmlNode n in nl)
    //                          AnimatedFileTransfer(n, Destination);
    //                      break;
    //  			}
    //  		}
    //  	}
    //  }

    #endregion

    public static void AnimatedFileTransfer(XmlNode Source, XmlNode Destination)
    {
        Console.CursorVisible = false;

        Util.WriteColor($"{(Source.Name == "Root" ? "/" : Source.Attributes["Name"].Value),-40} -> {Destination.GetPath()} ", ConsoleColor.Yellow);

        XmlNode ToCopy = Source.Clone();
        Destination.AppendChild(ToCopy);

        Util.BeginCharacterAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 25, 150, 20, Console.GetCursorPosition());
        Util.WriteLineColor("√", ConsoleColor.Green);

        Console.CursorVisible = true;
    }

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

            // Check for MODIFY permissions in the current directory here
            bool HasPermissions = FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, Player.CurrentSession.PathNode, DirectoryPermission.Modify);

            if (!HasPermissions)
			{
				Console.WriteLine("Insufficient permissions");
                return;
			}

            // We're all good at this point, create the directory

            if (Argument.Contains('/'))
			{
				Console.WriteLine("Name cannot contain slashes");
				Console.WriteLine("To create in a directory besides the CWD, specify the p= or path= named argument with the directory you wish to create in");
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
            if (!FSAPI.UserHasFilePermissions(Player.CurrentSession, NodeToList, FilePermission.Read))
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

        if ( !FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, NodeToList, DirectoryPermission.List) )
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
			Console.WriteLine("Insufficient arguments");
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

        if (!FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, TryDirectory, DirectoryPermission.Enter))
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
			Console.WriteLine("Insufficient arguments");
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

        // Both origin and destination need to have MODIFY permissions for the current user
        bool DestinationHasModifyPerms = FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, Destination, DirectoryPermission.Modify);
        if (!DestinationHasModifyPerms)
		{
			Console.WriteLine($"{Player.CurrentSession.User.Username} is not permitted to perform that action on {Destination.GetPath()}");
            return;
		}

        switch (Origin)
		{
            case XmlNode o:
                XmlNode Parent = o.ParentNode;
                if (!FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, Parent, DirectoryPermission.Modify))
				{
                    Console.WriteLine($"{Player.CurrentSession.User.Username} is not permitted to perform that action on {o.GetPath()}");
                    return;
                }

                Destination.AppendChild(o);
                break;

            case XmlNodeList nl:
                int count = nl.Count;

                for (int i = 0; i < count; i++)
                {
                    XmlNode CurrentParent = nl[i].ParentNode;
                    if (!FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, CurrentParent, DirectoryPermission.Modify))
					{
                        Console.WriteLine($"{Player.CurrentSession.User.Username} is not permitted to perform that action on {nl[i].GetPath()}");
                        continue;
                    }

                    //if (nl[i] == Player.CurrentShell.PathNode) // An origin query involving multiple items may include the current directory so we must check for that
                    //    Player.CurrentShell.PathNode = d;

                    Destination.AppendChild(nl[0]);
                }
                break;
		}
	}

    public static void Remove(InputQuery Query)
	{
        if (Query.Arguments.Count == 0)
		{
			Console.WriteLine("Insufficient arguments");
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
                
                if (!FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, n.ParentNode, DirectoryPermission.Modify))
				{
                    Console.WriteLine($"{Player.CurrentSession.User.Username} is not permitted to perform that action on {n.GetPath()}");
                    return;
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
                {
                    if (!FSAPI.UserHasDirectoryPermissions(Player.CurrentSession, nl[i].ParentNode, DirectoryPermission.Modify))
                    {
                        Console.WriteLine($"{Player.CurrentSession.User.Username} is not permitted to perform that action on {nl[i].GetPath()}");
                        continue;
                    }
                    Parent.RemoveChild(nl[i]);
                }

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
			Console.WriteLine("Insufficient arguments");
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

        if (!FSAPI.UserHasFilePermissions(Player.CurrentSession, File, FilePermission.Read))
		{
            Console.WriteLine($"{Player.CurrentSession.User.Username} is not permitted to perform that action on {File.GetPath()}");
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