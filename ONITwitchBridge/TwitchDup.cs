using Database;
using Newtonsoft.Json;

namespace ONITwitchBridge
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TwitchDup : Dup
    {
        [JsonProperty] public new string Username;
        [JsonProperty] public string MainSkill { get; private set; }

        public TwitchDup(string username) : base(username)
        {
            Username = username;
            MainSkill = "";
        }

        public TwitchDup(string username, string mainSkill) : base(username)
        {
            Username = username;
            MainSkill = mainSkill;
        }
        
        public bool IsMainSkilled(SkillGroup skill) => MainSkill == skill.Id;
        
        public void SetMainSkill(SkillGroup skill)
        {
            MainSkill = skill.Id;
        }

        public bool HasNoMainSkill() => string.IsNullOrEmpty(MainSkill);
    }

    public class Dup
    {
        public string Username;

        public Dup(string name)
        {
            Username = name;
        }
    }
}