using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Database;
using PeterHan.PLib.Core;
using TwitchIRC;
using UnityEngine;

namespace ONITwitchBridge
{
    public class IrcClient
    {
        public const string ANON_USERNAME = "justinfan12345";
        private List<TwitchDup> _nicknames = new List<TwitchDup>();
        public Dictionary<string, ICommand> Commands;
        private ChatListener _listener;
        private bool _anonymous;
        private string[] disallowedNicknames = new string[7];
        private int cursor = 0;

        public void Connect(string user, string oauth, string channel)
        {
            PUtil.LogDebug($"Attempting to connect to Twitch chat as {user} on channel {channel}.");
            _anonymous = user == ANON_USERNAME;
            _listener = new ChatListener(user, oauth, channel);
            _listener.OnChatMessage += OnMessage;

            if (_listener.Connect())
            {
                _listener.StartListening();
                PUtil.LogDebug($"Connected to Twitch chat as {user}.");
                SendMessage(
                    $"VoHiYo Twitch integration connected. Type {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.JOIN} to join the list of potential Dups. " +
                    $"Type {IrcCommand.COMMAND_PREFIX}{IrcCommand.Command.HELP} to see the list of commands.");
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
                if (attr != null)
                {
                    var constructor = type.GetConstructor(new[] { typeof(IrcClient) });
                    ICommand instance = constructor != null
                        ? (ICommand)Activator.CreateInstance(type, this)
                        : (ICommand)Activator.CreateInstance(type);

                    Commands[attr.Name] = instance;
                    PUtil.LogDebug($"Registered command: {attr.Name} - {attr.Help}");
                }
            }
        }

        private void OnMessage(string user, string message, string channel)
        {
            if (!message.StartsWith("!")) return;
            message = message.Substring(1);
            string command = (message.Contains(" ")
                ? message.Substring(0, message.IndexOf(" ", StringComparison.Ordinal))
                : message).ToLower();
            string arg = message.Contains(" ")
                ? message.Substring(message.IndexOf(" ", StringComparison.Ordinal) + 1)
                : "";
            string[] args = arg.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (Commands.TryGetValue(command, out ICommand cmd))
            {
                if (cmd.IsDisabled()) 
                {
                    SendMessage(cmd.GetDisabledReason());
                    return;
                }
                var reply = cmd.Execute(user, arg, args);
                if (!string.IsNullOrEmpty(reply))
                {
                    SendMessage(reply);
                }
            }
        }

        public void SendMessage(string message)
        {
            if (_anonymous) return;
            _listener.Irc.SendChatMessage(message);
        }

        public Dup GetRandomUser(Dup or, Dictionary<SkillGroup, float>.KeyCollection skillsHint)
        {
            var available = new TwitchDup[_nicknames.Count];
            _nicknames.CopyTo(available);
            available = available.Where(dup =>
                (skillsHint.Any(dup.IsMainSkilled) || dup.HasNoMainSkill())
                && !disallowedNicknames.Contains(dup.Username)
            ).ToArray();
            if (available.Length != 0)
            {
                Dup get = available.GetRandom();
                int attempts = 0;
                while (disallowedNicknames.Contains(get.Username) && attempts < 10)
                {
                    get = available.GetRandom();
                    attempts++;
                }

                if (attempts >= 10)
                {
                    PUtil.LogWarning("Could not find a valid random user after 10 attempts.");
                    get = or;
                }

                or = get;
            }

            disallowedNicknames[cursor] = or.Username;
            cursor = (cursor + 1) % disallowedNicknames.Length;
            return or;
        }

        public bool RemoveUser(string user)
        {
            if (!_nicknames.Any(dup => dup.Username.Equals(user))) return false;
            _nicknames.Remove(_nicknames.Find(dup => dup.Username.Equals(user)));
            PUtil.LogDebug($"Removed user: {user}.");
            return true;
        }

        public bool AddUser(string user)
        {
            if (_nicknames.Any(dup => dup.Username.Equals(user))) return false;
            _nicknames.Add(new TwitchDup(user));
            PUtil.LogDebug($"Added user: {user}.");
            return true;
        }

        public bool GetUser(string user, out TwitchDup dup)
        {
            dup = _nicknames.FirstOrDefault(d => d.Username.Equals(user));
            return dup != null;
        }

        public void Disconnect()
        {
            PUtil.LogDebug("Disconnecting from Twitch chat.");
            _listener.StopListening();
            _listener = null;
        }

        public bool IsConnected() => _listener is { Connected: true };
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class Command : Attribute
    {
        public string Name { get; }
        public string Help { get; }
        public DisabledState Disabled { get; }

        public Command(string name, string help, string disabledReason = null)
        {
            Name = name.ToLower();
            Help = help;
            Disabled = string.IsNullOrEmpty(disabledReason) ? DisabledState.FALSE : DisabledState.True(disabledReason);
        }
    }

    public abstract class ICommand
    {
        public abstract string Execute(string user, string arg, string[] args);

        public virtual string Help(string user, string arg, string[] args)
        {
            var type = GetType().GetCustomAttribute<Command>();
            return HelpHeader(user, arg, args, type.Help);
        }

        public string HelpHeader(string user, string arg, string[] args, string help)
        {
            var type = GetType().GetCustomAttribute<Command>();
            return $"Command: {(string.IsNullOrEmpty(arg) ? type.Name : type.Name + " " + arg)} - {help}";
        }
        
        public virtual DisabledState GetDisabledState() => GetType().GetCustomAttribute<Command>().Disabled;
        
        public virtual string GetDisabledReason() => $"This command is disabled because {GetDisabledState().Reason}";
        
        public virtual bool IsDisabled() => GetDisabledState().Disabled;
    }

    public class DisabledState
    {
        public static readonly DisabledState FALSE = False();
        public static readonly DisabledState TRUE = True();
        public readonly bool Disabled;
        public readonly string Reason;
        
        private DisabledState(bool disabled, string reason)
        {
            Disabled = disabled;
            Reason = reason;
        }
        
        public static DisabledState True(string reason) => new DisabledState(true, reason);
        public static DisabledState True() => True("unknown reason");
        public static DisabledState False() => new DisabledState(false, "");
    }
}