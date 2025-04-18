using System.Collections.Generic;
using System.IO;
using Database;
using Newtonsoft.Json;

namespace ONITwitchBridge
{
    public class Registry
    {
        private static Registry _Instance;
        private JsonSerializer _Serializer = new JsonSerializer();
        public TwitchRegistry TwitchRegistry { get; private set; }
        public GameRegistry GameRegistry { get; private set; }

        public static Registry Get() => _Instance ?? new Registry();

        public Registry Initialize()
        {
            _Instance = this;
            TwitchRegistry = TwitchRegistry.Initialize(_Serializer);
            GameRegistry = GameRegistry.Initialize(_Serializer);
            return this;
        }

        public void Save()
        {
            GameRegistry.Save(_Serializer);
            TwitchRegistry.Save(_Serializer);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameRegistry
    {
        [JsonProperty] public List<GameDup> GameDups { get; private set; }
        public int Count => GameDups.Count;
        
        public void CopyTo(GameDup[] array) => GameDups.CopyTo(array);
        
        public GameRegistry()
        {
            GameDups = new List<GameDup>();
        }
        
        public void AddDup(GameDup dup)
        {
            if (!GameDups.Exists(x => x.Name == dup.Name)) GameDups.Add(dup);
        }
        
        public void RemoveDup(string name)
        {
            if (GameDups.Exists(x => x.Name == name)) GameDups.Remove(GameDups.Find(x => x.Name == name));
        }
        
        public GameDup GetDup(string name)
        {
            if (!GameDups.Exists(x => x.Name == name)) GameDups.Add(new GameDup(name));
            return GameDups.Find(x => x.Name == name);
        }

        public static GameRegistry Initialize(JsonSerializer serializer)
        {
            var currentSaveName = SaveLoader.Instance.GameInfo.baseName;
            var fullPath = $"{ONITwitchBridge.ModFolderPath}/Saves/{currentSaveName}.json";
            if (!File.Exists(fullPath))
            {
                return new GameRegistry();
            }

            using var textReader = new StreamReader(fullPath);
            using var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<GameRegistry>(jsonReader);
        }

        public void Save(JsonSerializer serializer)
        {
            var currentSaveName = SaveLoader.Instance.GameInfo.baseName;
            var folderPath = $"{ONITwitchBridge.ModFolderPath}/Saves";
            var fullPath = $"{folderPath}/{currentSaveName}.json";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using var textWriter = new StreamWriter(fullPath);
            using var jsonWriter = new JsonTextWriter(textWriter);
            serializer.Serialize(jsonWriter, this);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TwitchRegistry
    {
        [JsonProperty] public List<TwitchDup> TwitchDups { get; private set; }
        public int Count => TwitchDups.Count;
        
        public void CopyTo(TwitchDup[] array) => TwitchDups.CopyTo(array);

        public TwitchRegistry()
        {
            TwitchDups = new List<TwitchDup>();
        }
        
        public void AddDup(TwitchDup dup)
        {
            if (!TwitchDups.Exists(x => x.Name == dup.Name)) TwitchDups.Add(dup);
        }
        
        public void RemoveDup(string name)
        {
            if (TwitchDups.Exists(x => x.Name == name)) TwitchDups.Remove(TwitchDups.Find(x => x.Name == name));
        }
        
        public TwitchDup GetDup(string name)
        {
            if (!TwitchDups.Exists(x => x.Name == name)) TwitchDups.Add(new TwitchDup(name));
            return TwitchDups.Find(x => x.Name == name);
        }

        public static TwitchRegistry Initialize(JsonSerializer serializer)
        {
            var fullPath = $"{ONITwitchBridge.ModFolderPath}/global.json";
            if (!File.Exists(fullPath))
            {
                return new TwitchRegistry();
            }

            using var textReader = new StreamReader(fullPath);
            using var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<TwitchRegistry>(jsonReader);
        }

        public void Save(JsonSerializer serializer)
        {
            var fullPath = $"{ONITwitchBridge.ModFolderPath}/global.json";

            using var textWriter = new StreamWriter(fullPath);
            using var jsonWriter = new JsonTextWriter(textWriter);
            serializer.Serialize(jsonWriter, this);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameDup
    {
        [JsonProperty] public string Name { get; private set; }
        [JsonProperty] public bool WantJoin { get; set; }
        [JsonProperty] public bool InGame { get; set; } = true;
        
        public GameDup(string name) => Name = name;

        public TwitchDup GetGlobalScope() => Registry.Get().TwitchRegistry.GetDup(Name);
        
        public bool CanBeSelected() => WantJoin && !InGame;

        public bool Join()
        {
            if (InGame) return false;
            WantJoin = true;
            return true;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TwitchDup : Dup
    {
        [JsonProperty] public new string Name { get; private set; }
        [JsonProperty] public string MainSkill { get; set; }
        [JsonProperty] public string Gender { get; set; }
        
        public TwitchDup(string name) : base(name) => Name = name;
        
        public GameDup GetGameScope() => Registry.Get().GameRegistry.GetDup(Name);

        public bool IsMainSkilled(SkillGroup arg) => HasMainSkill() && arg.Id == MainSkill;
        public bool HasMainSkill() => MainSkill != null;

        public void SetMainSkill(SkillGroup skill) => MainSkill = skill.Id;

        public void SetGender(string gender)
        {
            if (!gender.Equals(GENDER_MALE) && !gender.Equals(GENDER_FEMALE))
            {
                gender = GENDER_OTHER;
            }

            Gender = gender;
        }
    }

    public class Dup
    {
        public static string GENDER_MALE = "Male";
        public static string GENDER_FEMALE = "Female";
        public static string GENDER_OTHER = "NB";
        public string Name { get; private set; }
        
        public Dup(string name) => Name = name;
    }
}