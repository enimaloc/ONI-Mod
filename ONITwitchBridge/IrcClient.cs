using System;
using System.Collections.Generic;
using System.Linq;
using PeterHan.PLib.Core;
using TwitchIRC;
using UnityEngine;

namespace ONITwitchBridge
{
    public class IrcClient
    {
        public const string ANON_USERNAME = "justinfan12345";
        private List<string> _nicknames;
        private ChatListener _listener;
        private bool _anonymous;
        
        public IrcClient()
        {
            _nicknames = new List<string>();
        }

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
                SendMessage("VoHiYo Twitch integration connected. Type !join to join the list of potential Dups. Type !onitb to see the list of commands.");
            }
        }

        private void OnMessage(string user, string message, string channel)
        {
            string command = (message.Contains(" ") ? message.Substring(0, message.IndexOf(" ", StringComparison.Ordinal) + 1) : message).ToLower();
            string[] args = message.Contains(" ")
                ? message.Substring(message.IndexOf(" ", StringComparison.Ordinal) + 1)
                    .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            switch (command)
            {
            }
        }

        private void SendMessage(string message)
        {
            if (_anonymous) return;
            _listener.Irc.SendChatMessage(message);
        }
    }
}