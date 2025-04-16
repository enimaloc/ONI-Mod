using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PeterHan.PLib.Core;
using TwitchIRC;
using UnityEngine;

namespace ONITwitchBridge
{
    public class IrcClient
    {
        public const string ANON_USERNAME = "justinfan12345";
        private List<string> _nicknames = new List<string>();
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

        public string GetRandomUser(string or)
        {
            if (_nicknames.Count != 0)
            {
                var get = _nicknames.GetRandom();
                int attempts = 0;
                while (disallowedNicknames.Contains(get) && attempts < 10)
                {
                    get = _nicknames.GetRandom();
                    attempts++;
                }
                if (attempts >= 10)
                {
                    PUtil.LogWarning("Could not find a valid random user after 10 attempts.");
                    get = or;
                }
                or = get;
            }

            disallowedNicknames[cursor] = or;
            cursor = (cursor + 1) % disallowedNicknames.Length;
            return or;
        }

        public bool RemoveUser(string user)
        {
            if (!_nicknames.Contains(user)) return false;
            _nicknames.Remove(user);
            PUtil.LogDebug($"Removed user: {user}.");
            return true;
        }

        public bool AddUser(string user)
        {
            if (_nicknames.Contains(user)) return false;
            _nicknames.Add(user);
            PUtil.LogDebug($"Added user: {user}.");
            return true;
        }

        public void Disconnect()
        {
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

        public Command(string name, string help)
        {
            Name = name.ToLower();
            Help = help;
        }
    }

    public abstract class ICommand
    {
        public abstract string Execute(string user, string arg, string[] args);

        public string Help(string user, string arg, string[] args)
        {
            var type = GetType().GetCustomAttribute<Command>();
            return $"Command: {type.Name} - {type.Help}";
        }
    }
}