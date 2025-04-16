using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ONITwitchBridge
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        [Option("Username", "The username of the bot. You can set to justinfan12345 for anonymous",
            "Twitch")]
        [JsonProperty]
        public string Username { get; set; } = IrcClient.ANON_USERNAME;

        private string _OAuth = "oauth:kappa";

        [Option("OAuth",
            "The OAuth token of the bot. If username is justinfan12345 you can fill with a placeholder value",
            "Twitch")]
        [JsonProperty]
        public string OAuth
        {
            get => _OAuth;
            set
            {
                if (value.StartsWith("oauth:"))
                {
                    _OAuth = value;
                }
                else
                {
                    _OAuth = "oauth:" + value;
                }
            }
        }
        
        [Option("Channel", "The channel to join.", "Twitch")]
        [JsonProperty]
        public string Channel { get; set; } = "enimaloc";

        [Option("Save type", "Select how registered users are saved.")]
        [JsonProperty]
        public SaveType SaveType { get; set; } = SaveType.Colony;
    }

    public enum SaveType
    {
        Colony,
        Global,
        Session
    }
}