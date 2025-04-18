using enimaloc.onitb;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ONITwitchBridge
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        private const string OAuthPrefix = "oauth:";
        private string _oauthToken = "oauth:kappa";

        [Option("Username", 
            "The username of the bot. You can set it to justinfan12345 for anonymous access.", 
            "Twitch")]
        [JsonProperty]
        public string Username { get; set; } = IrcClient.ANON_USERNAME;

        [Option("OAuth", 
            "The OAuth token of the bot. If username is justinfan12345 you can use any placeholder.", 
            "Twitch")]
        [JsonProperty]
        public string OAuth
        {
            get => _oauthToken;
            set => _oauthToken = value.StartsWith(OAuthPrefix) ? value : $"{OAuthPrefix}{value}";
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

        [Option("Disable main skill customization", 
            "Disable the ability for users to set their main skill.")]
        [JsonProperty]
        public bool DisableMainSkillCustomization { get; set; } = false;
    }

    public enum SaveType
    {
        Colony,
        Global,
        Session
    }
}