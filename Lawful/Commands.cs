using System;
using System.Net;
using System.Xml;
using System.Collections.Generic;

using Lawful.InputParser;
using Lawful.GameLibrary;

using static Lawful.GameLibrary.Session;

namespace Lawful
{
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

                if (Player.ConnectionInfo.PC.Address == TryIP.ToString())
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
                
                if (TryUser.Password.Length > 0)
				{
                PasswordPrompt:

                    Console.Write("Password: ");
                    string Password = Util.ReadLineSecret();

                    if (Password != TryUser.Password)
                        goto PasswordPrompt;
				}

                Util.WriteLineColor($"Logged in as user '{Username}' at the connected node '{TryIP}'", ConsoleColor.Green);

                Player.ConnectionInfo.PC = TryPC;

                Player.ConnectionInfo.User = TryUser;
                Player.ConnectionInfo.PathNode = Player.ConnectionInfo.PC.FileSystemRoot;

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

            UserAccount TryAccount = Player.ConnectionInfo.PC.GetUser(Username);

            if (TryAccount is null)
			{
				Console.WriteLine($"Could not find a user by the username '{Username}'");
                return;
			}

            if (TryAccount.Password.Length > 0)
			{
                // Login code
                string Input;

                do
                {
                    Console.Write("Password: ");
                    Input = Util.ReadLineSecret();
                }
                while (Input != TryAccount.Password);

				Console.WriteLine();
			}

            // Actually switch to the user now
            Player.ConnectionInfo.User = TryAccount;

            Util.WriteColor("Success!", ConsoleColor.Green);
            Util.WriteLineColor($" :: Session now active for user '{Player.ConnectionInfo.User.Username}'", ConsoleColor.Yellow);
		}

        public static void Scan(InputQuery Query)
        {
            Console.Write("Scanning... ");

            Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 16, 48, 50, Console.GetCursorPosition());

            Util.WriteLineColor("done!", ConsoleColor.Green);
			Console.WriteLine();

            if (Player.ConnectionInfo.PC.ScanResults.Count > 0)
            {
				Console.WriteLine("Responses received:");

                foreach (ScanResult Result in Player.ConnectionInfo.PC.ScanResults)
                    Util.WriteLineColor($"    {Result.Name}@{Result.Address}", ConsoleColor.Yellow);

                
            }
            else
            {
				Console.WriteLine("Scan did not find any PCs open to the internet");
			}
        }

        private static List<IPAddress> ConnectedHosts = new();
        private static List<string> LoggedInUsers = new();

        private static List<Computer> ConnectedPCs = new();
        private static List<UserAccount> LoggedInAccounts = new();

        public static void SecureCopy(InputQuery Query)
		{
            if (Query.Arguments.Count == 0)
			{
				Console.WriteLine("Insufficient arguments");
                return;
			}

            if (Query.Arguments.Count == 1)
			{
                (bool Succeeded, dynamic Source) Arg1 = HandleOtherSCPArg(Query.Arguments[0]);

                XmlNode UserDir = Player.HomePC.GetNodeFromPath($"/home/{Player.ProfileName}");
                XmlNode UserBin = Player.HomePC.GetNodeFromPath("/bin");

                if (!Arg1.Succeeded) // The argument handler already prints our error messages for us so we just have to return
                    return;

                switch (Arg1.Source)
				{
                    case XmlNode n:
                        if (n.Attributes["Command"] is not null)
                            AnimatedFileTransfer(n, UserBin);
                        else
                            AnimatedFileTransfer(n, UserDir);
                        break;

                    case XmlNodeList nl:
                        foreach (XmlNode n in nl)
						{
                            if (n.Attributes["Command"] is not null)
                                AnimatedFileTransfer(n, UserBin);
                            else
                                AnimatedFileTransfer(n, UserDir);
                        }
                        break;
				}
			}
            else
			{
                (bool Succeeded, dynamic Source) Arg1 = HandleOtherSCPArg(Query.Arguments[0]);
                (bool Succeeded, dynamic Source) Arg2 = HandleOtherSCPArg(Query.Arguments[1]);

                if (!Arg1.Succeeded || !Arg2.Succeeded)
                    return;

                // Sort out the destination
                switch (Arg2.Source)
				{
                    case XmlNode n:
                        if (n.Name != "Directory" && n.Name != "Root")
                            goto default;       // Handly little feature, I must say
                        break;

                    default:
						Console.WriteLine("Invalid destination, must be a folder");
                        break;
				}

                // Sort out the source
                switch (Arg1.Source)
                {
                    case XmlNode n:
                        AnimatedFileTransfer(n, Arg2.Source);
                        break;

                    case XmlNodeList nl:
                        foreach (XmlNode n in nl)
                            AnimatedFileTransfer(n, Arg2.Source);
                        break;
                }
            }
            ConnectedPCs.Clear();
            LoggedInAccounts.Clear();
		}

        public static void AnimatedFileTransfer(XmlNode Source, XmlNode Destination)
		{
            Console.CursorVisible = false;

            Util.WriteColor($"{Source.Attributes["Name"].Value, -40} -> {Destination.GetPath()} ", ConsoleColor.Yellow);

            XmlNode ToCopy = Source.Clone();
            Destination.AppendChild(ToCopy);

            Util.BeginSpinningCursorAnimation(new char[8] { '|', '/', '-', '\\', '|', '/', '-', '\\' }, 25, 150, 20, Console.GetCursorPosition());
            Util.WriteLineColor("√", ConsoleColor.Green);

            Console.CursorVisible = true;
        }

        public static (bool, dynamic) HandleOtherSCPArg(string Argument)
		{
            // first we're going to determine the type of query
            RSIStatus QueryRSIStatus = NodeLocator.GetRSIStatus(Argument);

            dynamic Source;

            switch (QueryRSIStatus)
            {
                case RSIStatus.None:
                    // local query, use LocalLocate
                    Source = NodeLocator.LocalLocate(Argument, in Player.ConnectionInfo);
                    break;

                case RSIStatus.Complete:
                    // remote query, use RemoteLocate with new ConnectionInfo
                    string RSI = Argument.Split(':')[0];
                    string[] RSIElements = RSI.Split('@');

                    Computer PC = Computers.GetComputer(RSIElements[1]);

                    if (!ConnectedPCs.Contains(PC)) // If we do not already have an active session on this computer, create one.
                    {
                        ConnectedPCs.Add(PC);
						Console.WriteLine($"Connected to {PC.Name}@{PC.Address}");
                    }

                    UserAccount Account = PC.GetUser(RSIElements[0]);

                    if (!LoggedInAccounts.Contains(Account))    // If we are not already logged in to this particular account, log in.
					{
                        if (Account.Password.Length > 0)
                        {
                            string Password = String.Empty;

                            do
                            {
                                Console.WriteLine($"Password for '{Account.Username}': ");
                                Password = Util.ReadLineSecret();

                                if (Password == "$cancel")
                                    return (false, null);
                            }
                            while (Password != Account.Password);

                            LoggedInAccounts.Add(Account);
                        }
                    }

                    ConnectionInfo New = new() { PC = Computers.GetComputer(RSIElements[1]), User = Account };
                    Source = NodeLocator.RemoteLocate(Argument, in New);
                    break;

                default:
                    Console.WriteLine($"Error in query '{QueryRSIStatus}'");
                    return (false, null);
            }

            switch (Source)
            {
                case XmlNode n:
                    return (true, n);

                case XmlNodeList nl:
                    return (true, nl);

                default:
                    Console.WriteLine($"Unable to resolve source query '{Argument}'");
                    return (false, null);
            }
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
                if (NodeLocator.GetRSIStatus(Argument) > RSIStatus.None)
				{
					Console.WriteLine("Cannot create directories remotely");
                    return;
				}

                bool NameConflict = Player.ConnectionInfo.PathNode.GetNodeFromPath(Argument) != null;

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

                XmlNode NewDirectory = Player.ConnectionInfo.PathNode.OwnerDocument.CreateElement("Directory");

                XmlAttribute NewDirectoryAttribute = Player.ConnectionInfo.PathNode.OwnerDocument.CreateAttribute("Name");
                NewDirectoryAttribute.InnerText = Argument;

                NewDirectory.Attributes.Append(NewDirectoryAttribute);

                Player.ConnectionInfo.PathNode.AppendChild(NewDirectory);
			}
		}

        public static void List(InputQuery Query)
        {
            XmlNode NodeToList = Player.ConnectionInfo.PathNode;

            if (Query.Arguments.Count > 0)
            {
                RSIStatus ArgRSIStatus = NodeLocator.GetRSIStatus(Query.Arguments[0]);

                if (ArgRSIStatus > RSIStatus.None)
				{
					Console.WriteLine("Cannot list the contents of a remote directory");
                    return;
				}

                switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
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
				Console.WriteLine($"Listing details for file '{NodeToList.Attributes["Name"].Value}'");
				Console.WriteLine();

				Console.Write("Path".PadRight(10));
                Util.WriteLineColor(NodeToList.GetPath(), ConsoleColor.Yellow);

				Console.Write("Length".PadRight(10));

                int Length = NodeToList.InnerText.Trim().Length;
                Util.WriteLineColor(Length.ToString() + (Length == 1 ? " character" : " characters"), ConsoleColor.Yellow);

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
            if (NodeLocator.GetRSIStatus(Query.Arguments[0]) > RSIStatus.None)
			{
				Console.WriteLine("Cannot CD into a remote directory");
                return;
			}

            // Handle query
            switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
			{
                case XmlNode n:
                    if (n.Name == "File")
					{
						Console.WriteLine("Cannot CD into a file");
                        return;
					}
                    Player.ConnectionInfo.PathNode = n;
                    return;

                case XmlNodeList:
                    Console.WriteLine("Query must resolve to a single directory");
                    return;

                default:
					Console.WriteLine($"Directory '{Query.Arguments[0]}' not found");
                    return;
			}
		}

        public static void Move(InputQuery Query)
		{
            if (Query.Arguments.Count < 2)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            // Test for Remote System Identifiers (RSIs) on both the origin and destination queries
            RSIStatus OriginRSIStatus = NodeLocator.GetRSIStatus(Query.Arguments[0]);
            RSIStatus DestinationRSIStatus = NodeLocator.GetRSIStatus(Query.Arguments[1]);

            // Handle remote queries
            if (OriginRSIStatus > RSIStatus.None || DestinationRSIStatus > RSIStatus.None)
			{
				Console.WriteLine("Move does not support remote queries");
                return;
			}

            // Find each query
            dynamic Origin = NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo);
            dynamic Destination = NodeLocator.LocalLocate(Query.Arguments[1], in Player.ConnectionInfo);

            switch (Destination)
			{
                case XmlNode d:
                    if (d.Name == "File")
					{
						Console.WriteLine("Destination must be a directory");
                        return;
					}
                    switch (Origin)
					{
                        case XmlNode o:

                            d.AppendChild(o);

                            //if (o == Player.ConnectionInfo.PathNode)
                            //    Player.ConnectionInfo.PathNode = d;

                            break;

                        case XmlNodeList nl:
                            int count = nl.Count;

                            for (int i = 0; i < count; i++)
                            {
                                //if (nl[i] == Player.ConnectionInfo.PathNode) // An origin query involving multiple items may include the current directory so we must check for that
                                //    Player.ConnectionInfo.PathNode = d;

                                d.AppendChild(nl[0]);
                            }
                            break;

                        default:
							Console.WriteLine($"Could resolve origin query '{Query.Arguments[0]}', nothing was moved");
                            return;
					}
                    break;

                case XmlNodeList:
					Console.WriteLine("Cannot have more than one destination");
                    return;

                default:
					Console.WriteLine($"Could not resolve destination query '{Query.Arguments[1]}', nothing was moved");
                    return;
			}
		}

		public static XmlNode LocateNode(string Query)
		{
			if (Query.Length == 0) { return null; }

            if (Query[0] == '/')
                return Player.ConnectionInfo.PC.GetNodeFromPath(Query);
            else
                return Player.ConnectionInfo.PathNode.GetNodeFromPath(Query);
		}

        public static void Remove(InputQuery Query)
		{
            if (Query.Arguments.Count == 0)
			{
				Console.WriteLine("Insufficient parameters");
                return;
			}

            if (NodeLocator.GetRSIStatus(Query.Arguments[0]) > RSIStatus.None)
			{
                Console.WriteLine("Remove does not support remote queries");
                return;
            }

            XmlNode Node = Player.ConnectionInfo.PathNode;

            switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
            {
                case XmlNode n:
                    if (n.Name == "Root")
                    {
                        Console.WriteLine("Cannot delete root directory");
                        return;
                    }

                    while (Node.Name != "Root")
                    {
                        if (Node == n)
                        {
                            // error condition
                            Console.WriteLine("Cannot delete a parent of the current working directory");
                            return;
                        }
                        Node = Node.ParentNode;
                    }

                    n.ParentNode.RemoveChild(n);
                    break;

                case XmlNodeList nl:
                    XmlNode Parent = nl[0].ParentNode;

                    while (Node.Name != "Root")
                    {
                        foreach (XmlNode n in nl)
                            if (Node == n)
                            {
                                Console.WriteLine("Cannot delete a parent of the current working directory");
                                return;
                            }

                        Node = Node.ParentNode;
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

            if (NodeLocator.GetRSIStatus(Query.Arguments[0]) > RSIStatus.None)
			{
                Console.WriteLine("Concatenate does not support remote queries");
                return;
            }

            switch (NodeLocator.LocalLocate(Query.Arguments[0], in Player.ConnectionInfo))
            {
                case XmlNode n:
                    if (n.Name != "File")
                    {
                        Console.WriteLine("Query must resolve to a file");
                        return;
                    }
                    Console.WriteLine(n.InnerText.Trim());
                    Events.FireReadFile(Player, Computers, Query, Events, n);
                    break;

                case XmlNodeList:
                    Console.WriteLine("Cannot read more than one file at a time");
                    break;

                default:
                    Console.WriteLine($"Could not find file '{Query.Arguments[0]}'");
                    break;
            }
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
}