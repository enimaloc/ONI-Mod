using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace enimaloc.onitb
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        private const string OAuthPrefix = "oauth:";
        [JsonProperty]
        public string UnmaskedOauth { get; private set; }  = "oauth:kappa";

        [Option("Username",
            "The username of the bot. You can set it to justinfan12345 for anonymous access.",
            "Twitch")]
        [JsonProperty]
        public string Username { get; set; } = IrcClient.ANON_USERNAME;

        [Option("OAuth",
            "The OAuth token of the bot. If username is justinfan12345 you can use any placeholder.",
            "Twitch")]
        public string OAuth
        {
            get => OAuthPrefix + new string('*', UnmaskedOauth.Length - OAuthPrefix.Length);
            set => UnmaskedOauth = value.StartsWith(OAuthPrefix) ? value : $"{OAuthPrefix}{value}";
        }

        [Option("Channel",
            "The channel the bot will join.",
            "Twitch")]
        [JsonProperty]
        public string Channel { get; set; } = "enimaloc";

        [Option("Save type",
            "Select how registered users are saved.")]
        [JsonProperty]
        public SaveType SaveType { get; set; } = SaveType.Colony;

        [Option("Enable main skill customization",
            "Enable the ability for users to set their main skill.",
            "Viewer Customization")]
        [JsonProperty]
        public bool EnableMainSkillCustomization { get; set; } = false;
        
        [Option("Prefix",
            "The prefix for commands.",
            "Commands")]
        [JsonProperty]
        public string CommandPrefix { get; set; } = "!";
        
        [Option("Join command",
            "The command to join the game.",
            "Commands")]
        [JsonProperty]
        public string JoinCommand { get; set; } = "join";
    }

    public enum SaveType
    {
        Colony,
        Global,
        Session
    }
}