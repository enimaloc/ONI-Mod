using System;
using System.Net;
using Newtonsoft.Json;

namespace enimaloc.onitb.api
{
    public class AlejoPronounsAPI
    {
        public const string Uri = "https://api.pronouns.alejo.io";
        public const int Version = 1;
        public static string FullUri => $"{Uri}/v{Version}";
        public static string PronounsUri() => $"{FullUri}/pronouns/";
        public static string UserUri(string nickname) => $"{FullUri}/users/{nickname}";

        public static AlejoUser User(string nickname)
        {
            using var client = new WebClient();
            client.Headers.Add("User-Agent", "ONITwitchBridge");
            client.Headers.Add("Accept", "application/json");

            var response = client.DownloadString(UserUri(nickname));
            if (string.IsNullOrEmpty(response)) return null;

            return JsonConvert.DeserializeObject<AlejoUser>(response)
                   ?? throw new Exception("Failed to deserialize user data");
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AlejoUser
    {
        [JsonProperty("channel_id")] public string ChannelId;
        [JsonProperty("channel_login")] public string ChannelLogin;
        [JsonProperty("pronoun_id")] public string PronounId;
        [JsonProperty("alt_pronoun_id")] public string AltPronounId;
    }
}