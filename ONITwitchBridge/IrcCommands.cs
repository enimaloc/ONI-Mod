using System;
using System.Linq;

namespace ONITwitchBridge
{
    public class IrcCommand
    {
        public const string COMMAND_PREFIX = "!";

        public class Command
        {
            public const string HELP = "onitb";
            public const string JOIN = "join";
            public const string SET = "set";
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
                    if (cmd.IsDisabled())
                        return $"{command} is disabled. Reason: {cmd.GetDisabledReason()}";
                    string[] newArgs = new string[args.Length - 1];
                    Array.Copy(args, 1, newArgs, 0, args.Length - 1);
                    return cmd.Help(user, arg.Contains(" ")
                        ? arg.Substring(arg.IndexOf(" ", StringComparison.Ordinal) + 1)
                        : "", newArgs);
                }
            }

            return
                "Available commands: " +
                $"{_ircClient.Commands.Where(command => !command.Value.IsDisabled()).Aggregate("", (current, command) => current + command.Key + ", ").TrimEnd(',', ' ')}" +
                ". Tip: an argument can be provided to view the help for a specific command " +
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

    [Command(IrcCommand.Command.SET, "allow you to customize your Dup. Available subcommands: mainSkill.")]
    public class Set : ICommand
    {
        private IrcClient _ircClient;

        public Set(IrcClient ircClient)
        {
            _ircClient = ircClient;
        }

        public override string Execute(string user, string arg, string[] args)
        {
            if (args.Length == 0)
                return
                    $"Set what? Use {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.HELP} {IrcCommand.Command.SET} for help.";

            string subCommand = args[0].ToLower();
            switch (subCommand)
            {
                case "mainskill":
                    if (ONITwitchBridge.Settings.DisableMainSkillCustomization)
                        return "Main skill customization was disabled by the streamer.";
                    if (args.Length < 2) return Help(user, arg, args);
                    if (!ONITwitchBridge.Skills.Any(s =>
                            ((IListableOption)s).GetProperName().Equals(args[1], StringComparison.OrdinalIgnoreCase)))
                        return
                            $"Invalid skill: {args[1]}. Possible values: {string.Join(", ", ONITwitchBridge.Skills)}";
                    if (!_ircClient.GetUser(user, out var dup))
                        return
                            $"{user}, you are not in the list of potential Duplicants. Use {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.JOIN} to join.";
                    dup.SetMainSkill(ONITwitchBridge.Skills.FirstOrDefault(s =>
                        ((IListableOption)s).GetProperName().Equals(args[1], StringComparison.OrdinalIgnoreCase)));
                    return $"Main skill set to {args[1]} for {user}.";
                default:
                    return Help(user, arg, args);
            }
        }

        public override string Help(string user, string arg, string[] args)
        {
            if (args.Length == 0) return base.Help(user, arg, args);

            string subCommand = args[0].ToLower();
            switch (subCommand)
            {
                case "mainskill":
                    return HelpHeader(user, arg, args,
                        $"Set the main skill of your Dup. Possible values: {string.Join(", ", ONITwitchBridge.Skills)}");
                default: return base.Help(user, arg, args);
            }
        }
    }
}