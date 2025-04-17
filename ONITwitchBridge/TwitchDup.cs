using System;
using Database;
using Newtonsoft.Json;

namespace ONITwitchBridge
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameDup
    {
        [JsonProperty] public string Username;
        [JsonProperty] public bool WantJoin;
        [JsonProperty] public bool CanJoin;
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class TwitchDup : Dup
    {
        public const string GENDER_MALE = "Male";
        public const string GENDER_FEMALE = "Female";
        public const string GENDER_OTHER = "NB";
        [JsonProperty] public new string Username;
        [JsonProperty] public string MainSkill { get; private set; }
        [JsonProperty] public string Gender { get; private set; }

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

        public void SetGender(string gender)
        {
            if (!gender.Equals(GENDER_MALE, StringComparison.OrdinalIgnoreCase) && !gender.Equals(GENDER_FEMALE, StringComparison.OrdinalIgnoreCase))
            {
                gender = GENDER_OTHER;
            }
            Gender = gender;
        }

        public GameDup GetGameDup()
        {
            var save = GameSaveStructure.Load();
            return save.GetGameDup(Username) ?? save.AddGameDup(Username);
        }
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