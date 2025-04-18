using System;
using System.Linq;

namespace enimaloc.onitb
{
    public static class IrcCommand
    {
        public const string COMMAND_PREFIX = "!";

        public static class Command
        {
            public const string HELP = "onitb";
            public const string JOIN = "join";
            public const string SET = "set";
            public const string INFO = "info";
        }
    }

    [Command(IrcCommand.Command.HELP, "Allows you to see the list of available commands or help for a specific one.")]
    public class OniTB : ICommand
    {
        private readonly IrcClient _ircClient;

        public OniTB(IrcClient ircClient) => _ircClient = ircClient;

        public override string Execute(string user, string arg, string[] args)
        {
            if (args.Length > 0)
            {
                var command = args[0].ToLower();
                if (_ircClient.Commands.TryGetValue(command, out var cmd))
                {
                    if (cmd.IsDisabled())
                        return $"{command} is disabled. Reason: {cmd.GetDisabledReason()}";

                    var subArgs = args.Skip(1).ToArray();
                    var subArg = string.Join(" ", subArgs);
                    return cmd.Help(user, subArg, subArgs);
                }
            }

            var available = _ircClient.Commands
                .Where(kv => !kv.Value.IsDisabled())
                .Select(kv => kv.Key);

            return $"Available commands: {string.Join(", ", available)}.\n" +
                   $"Tip: use {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.HELP} <command> for help.";
        }
    }

    [Command(IrcCommand.Command.JOIN, "Join the list of potential Duplicants.")]
    public class Join : ICommand
    {
        public override string Execute(string user, string arg, string[] args)
        {
            var dup = Registry.Get().GameRegistry.Get(user);
            return dup.Join()
                ? $"{user} has joined the list to become a Duplicant!"
                : $"{user}, you are already a Duplicant or queued to be one.";
        }
    }

    [Command(IrcCommand.Command.SET, "Customize your Dup. Subcommands: mainskill, gender.")]
    public class Set : ICommand
    {
        private static readonly string[] VALID_GENDERS =
        {
            Dup.GENDER_MALE, Dup.GENDER_FEMALE, "Other"
        };

        public override string Execute(string user, string arg, string[] args)
        {
            if (args.Length < 1)
                return Help(user, arg, args);

            var subCommand = args[0].ToLower();
            var dup = Registry.Get().TwitchRegistry.Get(user);

            switch (subCommand)
            {
                case "mainskill":
                    return HandleMainSkill(dup, user, args);
                case "gender":
                    return HandleGender(dup, user, args);
                default:
                    return Help(user, arg, args);
            }
        }

        private string HandleMainSkill(TwitchDup dup, string user, string[] args)
        {
            if (ONITwitchBridge.Settings.DisableMainSkillCustomization)
                return "Main skill customization is disabled by the streamer.";

            if (args.Length < 2)
                return Help(user, string.Join(" ", args), args);

            var skillName = args[1];
            var skill = ONITwitchBridge.Skills
                .FirstOrDefault(s => ((IListableOption)s).GetProperName()
                    .Equals(skillName, StringComparison.OrdinalIgnoreCase));

            if (skill == null)
                return
                    $"Invalid skill: {skillName}. Possible values: {string.Join(", ", ONITwitchBridge.Skills.Select(s => ((IListableOption)s).GetProperName()))}";

            dup.SetMainSkill(skill);
            return $"Main skill for {user} set to {skillName}.";
        }

        private string HandleGender(TwitchDup dup, string user, string[] args)
        {
            if (args.Length < 2)
                return Help(user, string.Join(" ", args), args);

            var gender = args[1];
            if (!VALID_GENDERS.Any(g => g.Equals(gender, StringComparison.OrdinalIgnoreCase)))
                return $"Invalid gender: {gender}. Valid options: {string.Join(", ", VALID_GENDERS)}";

            dup.SetGender(gender);
            return $"Gender for {user} set to {gender}.";
        }

        public override string Help(string user, string arg, string[] args)
        {
            if (args.Length < 1)
                return HelpHeader(user, arg, args,
                    "Customize your Dup. Use one of the subcommands: mainskill, gender.");

            var subCommand = args[0].ToLower();
            return subCommand switch
            {
                "mainskill" => HelpHeader(user, arg, args,
                    $"Set your Dup's main skill. Options: {string.Join(", ", ONITwitchBridge.Skills.Select(s => ((IListableOption)s).GetProperName()))}"),
                "gender" => HelpHeader(user, arg, args,
                    $"Set your Dup's gender. Options: {string.Join(", ", VALID_GENDERS)}"),
                _ => base.Help(user, arg, args)
            };
        }
    }

    [Command(IrcCommand.Command.INFO, "Get info about your Dup.")]
    public class Info : ICommand
    {
        public override string Execute(string user, string arg, string[] args)
        {
            var twitchDup = Registry.Get().TwitchRegistry.Get(user);
            var gameDup = twitchDup.GetGameScope();

            var genderPart = string.IsNullOrEmpty(twitchDup.Gender)
                ? ""
                : $"a {(twitchDup.Gender.Equals(Dup.GENDER_OTHER) ? "X" : twitchDup.Gender)} ";
            var skillPart = string.IsNullOrEmpty(twitchDup.MainSkill) ? "" : $"specialized in {twitchDup.MainSkill}";
            var gameInfo = SaveLoader.Instance.GameInfo.baseName;
            var status = gameDup.InGame
                ? $"You live in {gameInfo}."
                : gameDup.WantJoin
                    ? $"You are in the queue to be printed in {gameInfo}."
                    : "";

            return $"Your Dup is {genderPart}named {user} {skillPart}. {status}";
        }
    }
}