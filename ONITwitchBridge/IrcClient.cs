using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Database;
using PeterHan.PLib.Core;
using TwitchIRC;

namespace enimaloc.onitb
{
    public class IrcClient
    {
        public const string ANON_USERNAME = "justinfan12345";

        public Dictionary<string, ICommand> Commands { get; private set; }

        private ChatListener _listener;
        private bool _anonymous;
        private readonly string[] _disallowedNicknames = new string[7];
        private int _cursor = 0;

        public void Connect(string user, string oauth, string channel)
        {
            PUtil.LogDebug($"{nameof(Connect)}: user={user}, oauth={oauth}, channel={channel}");
            PUtil.LogDebug($"Connecting to Twitch as '{user}' on channel '{channel}'.");

            _anonymous = user == ANON_USERNAME;
            _listener = new ChatListener(user, oauth, channel);
            _listener.OnChatMessage += OnMessage;

            if (_listener.Connect())
            {
                _listener.StartListening();
                PUtil.LogDebug($"Connected successfully.");

                SendMessage(
                    $"VoHiYo Twitch integration ready! Use {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.JOIN} to join, " +
                    $"or {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.HELP} for commands."
                );
            }
            else
            {
                PUtil.LogWarning("Failed to connect to Twitch.");
            }
        }

        public void RegisterCommands()
        {
            Commands = new Dictionary<string, ICommand>();

            var commandTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in commandTypes)
            {
                var attr = type.GetCustomAttribute<Command>();
                if (attr == null) continue;

                var constructor = type.GetConstructor(new[] { typeof(IrcClient) });
                var instance = constructor != null
                    ? (ICommand)Activator.CreateInstance(type, this)
                    : (ICommand)Activator.CreateInstance(type);

                if (instance == null) continue;
                Commands[instance.Name()] = instance;
                PUtil.LogDebug($"Registered command: {instance.Name()} – {instance.Help()}");
            }
        }

        private void OnMessage(string user, string message, string channel)
        {
            if (!message.StartsWith(IrcCommand.COMMAND_PREFIX)) return;

            message = message.Substring(1);
            var command = (message.Contains(" ")
                ? message.Substring(0, message.IndexOf(" ", StringComparison.Ordinal))
                : message).ToLower();
            var arg = message.Contains(" ")
                ? message.Substring(message.IndexOf(" ", StringComparison.Ordinal) + 1)
                : "";
            var args = arg.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (!Commands.TryGetValue(command, out var cmd)) return;

            if (cmd.IsDisabled())
            {
                SendMessage(cmd.GetDisabledReason());
                return;
            }

            var result = cmd.Execute(user, arg, args);
            if (!string.IsNullOrEmpty(result))
            {
                SendMessage(result);
            }
        }

        public void SendMessage(string message)
        {
            if (_anonymous || _listener?.Irc == null) return;
            _listener.Irc.SendChatMessage(message);
        }

        public Dup GetRandomUser(Dup fallback, IEnumerable<SkillGroup> skillsHint, string genderKey)
        {
            var twitchRegistry = Registry.Get().TwitchRegistry;
            var dups = new TwitchDup[twitchRegistry.Count];
            twitchRegistry.CopyTo(dups);

            var candidates = dups
                .Where(dup =>
                    dup.GameScope.CanBeSelected() &&
                    (skillsHint.Any(dup.IsMainSkilled) || !dup.HasMainSkill()) &&
                    string.Equals(dup.Gender, genderKey, StringComparison.OrdinalIgnoreCase) &&
                    !_disallowedNicknames.Contains(dup.Name)
                )
                .ToArray();

            if (candidates.Length == 0)
            {
                PUtil.LogWarning("No suitable candidate found for random user.");
                _disallowedNicknames[_cursor] = fallback.Name;
                _cursor = (_cursor + 1) % _disallowedNicknames.Length;
                return fallback;
            }

            var selected = SelectNonDisallowed(candidates, fallback);
            _disallowedNicknames[_cursor] = selected.Name;
            _cursor = (_cursor + 1) % _disallowedNicknames.Length;

            return selected;
        }

        private Dup SelectNonDisallowed(TwitchDup[] candidates, Dup fallback)
        {
            var attempts = 0;
            Dup selected = candidates.GetRandom();

            while (_disallowedNicknames.Contains(selected.Name) && attempts++ < 10)
            {
                selected = candidates.GetRandom();
            }

            if (attempts >= 10)
            {
                PUtil.LogWarning("Too many attempts to find a non-disallowed user.");
                return fallback;
            }

            return selected;
        }

        public void Disconnect()
        {
            PUtil.LogDebug("Disconnecting from Twitch...");
            _listener?.StopListening();
            _listener = null;
        }

        public bool IsConnected() => _listener?.Connected == true;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class Command : Attribute
    {
        public string Name { get; }
        public string Help { get; }
        public DisabledState Disabled { get; }

        public Command(string name, string help, string disabledReason = null)
        {
            Name = name;
            Help = help;
            Disabled = string.IsNullOrWhiteSpace(disabledReason)
                ? DisabledState.False()
                : DisabledState.True(disabledReason);
        }
    }

    public abstract class ICommand
    {
        public abstract string Execute(string user, string arg, string[] args);

        public virtual string Help(string user, string arg, string[] args) =>
            HelpHeader(user, arg, args, Help());

        protected string HelpHeader(string user, string arg, string[] args, string help) =>
            $"Command: {Name()}{(string.IsNullOrEmpty(arg) ? " " + arg : "")} - {help}";

        public virtual DisabledState GetDisabledState()
            => GetType().GetCustomAttribute<Command>()?.Disabled ?? DisabledState.False();

        public virtual string GetDisabledReason()
            => $"This command is disabled. Reason: {GetDisabledState().Reason}";

        public virtual bool IsDisabled()
            => GetDisabledState().Disabled;
        
        public virtual string Name()
            => GetType().GetCustomAttribute<Command>().Name.ToLower();
        
        public virtual string Help()
            => GetType().GetCustomAttribute<Command>().Help;
    }

    public class DisabledState
    {
        public static readonly DisabledState FALSE = new DisabledState(false, string.Empty);

        public bool Disabled { get; }
        public string Reason { get; }

        private DisabledState(bool disabled, string reason)
        {
            Disabled = disabled;
            Reason = reason;
        }

        public static DisabledState True(string reason = "Unknown reason") => new DisabledState(true, reason);
        public static DisabledState False() => FALSE;
    }
}