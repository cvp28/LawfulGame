using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lawful.InputParser
{
    public struct InputQuery
    {
        public string Command;

        public List<string> Arguments;
        public Dictionary<string, string> NamedArguments;
        public List<string> Flags;

        public static InputQuery Empty()
        {
            return new InputQuery()
            {
                Command = String.Empty,
                Arguments = null,
                NamedArguments = null,
                Flags = null
            };
        }
    }


    public static class Parser
    {
        public static InputQuery Parse(string Input)
        {
            string Command;
            Dictionary<string, string> NamedArgs = new Dictionary<string, string>();
            List<string> Flags = new List<string>();
            List<string> Args = new List<string>();

            // We begin parsing here
            // Big clusterfuck regexes to follow

            if (Input.Split(' ').Length > 1)
            {
                Command = Input.Split(' ')[0];
                Input = Input.Substring(Command.Length + 1) + " ";


                // Get named arguments (with quoted values)
                foreach (Match mNamedArgQ in Regex.Matches(Input, "\\w*=\"[\\w\\s`~!@#$%^&*()-=+\\\\/|,.<>;':\\[\\]{}]*\"\\s"))
                {
                    string Parameter = mNamedArgQ.Value.Split('=')[0];
                    string Value = mNamedArgQ.Value.Split('=')[1].Replace("\"", "");
                    if (!NamedArgs.ContainsKey(Parameter))
                    {
                        NamedArgs.Add(Parameter, Value);
                    }
                    Input = Input.Replace(mNamedArgQ.Value, "");
                }


                // Get named arguments (no quoted values)
                foreach (Match mNamedArg in Regex.Matches(Input, @"\w*=[\w`~!@#$%^&*()-=+/\\|,.<>;':\[\]{}]*"))
                {
                    string Parameter = mNamedArg.Value.Split('=')[0];
                    string Value = mNamedArg.Value.Split('=')[1];
                    if (!NamedArgs.ContainsKey(Parameter))
                    {
                        NamedArgs.Add(Parameter, Value);
                    }
                    Input = Input.Replace(mNamedArg.Value, "");
                }


                // Get quoted bits
                foreach (Match mQuotedString in Regex.Matches(Input, "\"[\\w\\s`~!@#$%^&*()-=+\\,.<>;':\\[\\]{}]*\"\\s"))
                {
                    string match = mQuotedString.Value;
                    match = match.Trim();
                    match = match.Trim('"');
                    Args.Add(match);
                    Input = Input.Replace(mQuotedString.Value, "");
                }


                // Get flags
                foreach (Match mFlag in Regex.Matches(Input, @"--\w*\s"))
                {
                    string match = mFlag.Value;
                    match = match.Substring(2).Trim();
                    Flags.Add(match);
                    Input = Input.Replace(mFlag.Value, "");
                }


                // Get single-word arguments
                Input.Trim();
                foreach (string arg in Input.Split(' '))
                {
                    if (arg.Length > 0)
                    {
                        Args.Add(arg);
                    }
                }
            }
            else
            {
                Command = Input;
            }

            return new InputQuery() { Command = Command, Arguments = Args, NamedArguments = NamedArgs, Flags = Flags };
        }
    }
}
