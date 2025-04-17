namespace ONITwitchBridge
{
    public class IrcCommand
    {
        public const string COMMAND_PREFIX = "!";

        public class Command
        {
            public const string HELP = "onitb";
            public const string JOIN = "join";
        }
    }

    [Command(IrcCommand.Command.HELP, "allow you to see the list of commands.")]
    public class OniTB : ICommand
    {
        private IrcClient _ircClient;

        public OniTB(IrcClient ircClient)
        {
            _ircClient = ircClient;
        }

        public override string Execute(string user, string arg, string[] args)
        {
            if (args.Length != 0)
            {
                string command = args[0].ToLower();
                if (_ircClient.Commands.TryGetValue(command, out ICommand cmd))
                {
                    string newArg = arg.Contains(" ")
                        ? arg.Substring(arg.IndexOf(" ", StringComparison.Ordinal) + 1)
                        : "";
                    string[] newArgs = new string[args.Length - 1];
                    Array.Copy(args, 1, newArgs, 0, args.Length - 1);
                    return cmd.Help(user, newArg, newArgs);
                }
            }

            string commands = "";
            foreach (var command in _ircClient.Commands)
            {
                commands += command.Key + ", ";
            }

            commands = commands.TrimEnd(',', ' ');
            return
                $"Available commands: {commands}. Tip: an argument can be provided to view the help for a specific command " +
                $"(example: {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.HELP} {IrcCommand.Command.HELP}).";
        }
    }

    [Command(IrcCommand.Command.JOIN, "allow you to join the list of potential Dups.")]
    public class Join : ICommand
    {
        private IrcClient _ircClient;

        public Join(IrcClient ircClient)
        {
            _ircClient = ircClient;
        }

        public override string Execute(string user, string arg, string[] args) => _ircClient.AddUser(user)
            ? $"{user} has joined the list to be a Dups."
            : $"{user}, you are already in the list of potential Duplicants.";
    }
}