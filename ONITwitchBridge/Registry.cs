using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using Newtonsoft.Json;
using PeterHan.PLib.Core;

namespace enimaloc.onitb
{
    public class Registry
    {
        private static Registry _instance;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public TwitchRegistry TwitchRegistry { get; private set; }
        public GameRegistry GameRegistry { get; private set; }

        public static Registry Get() => _instance ?? new Registry();

        public Registry Initialize()
        {
            _instance = this;
            TwitchRegistry = TwitchRegistry.Initialize(_serializer);
            GameRegistry = GameRegistry.Initialize(_serializer);
            return this;
        }

        public void Save()
        {
            GameRegistry.Save(_serializer);
            TwitchRegistry.Save(_serializer);
        }
    }

    public abstract class RegistryBase<T> where T : class
    {
        [JsonProperty] public List<T> Items = new List<T>();

        public int Count => Items.Count;

        public void CopyTo(T[] array) => Items.CopyTo(array);

        public void Add(T item)
        {
            if (Items.All(x => x != item)) Items.Add(item);
        }

        public void Remove(T item) => Items.Remove(item);

        public T Get(Predicate<T> predicate) => Items.Find(predicate);

        public T GetOrCreate(Predicate<T> predicate, Func<T> createFunc)
        {
            var item = Get(predicate);
            if (item != null) return item;

            item = createFunc();
            Add(item);
            return item;
        }
        
        public bool Has(T item) => Items.Contains(item);
        
        public bool Has(Predicate<T> predicate) => Items.Exists(predicate);

        public abstract string GetFilePath();

        public void Save(JsonSerializer serializer)
        {
            PUtil.LogDebug($"Saving {GetType().Name} registry to {GetFilePath()}");
            var path = GetFilePath();
            if (!Directory.Exists(path.Substring(path.LastIndexOf("/", StringComparison.Ordinal)))) Directory.CreateDirectory(path.Substring(path.LastIndexOf("/", StringComparison.Ordinal)));

            using var textWriter = new StreamWriter(path);
            using var jsonWriter = new JsonTextWriter(textWriter);
            serializer.Serialize(jsonWriter, this);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameRegistry : RegistryBase<GameDup>
    {
        public static readonly string FilePath =
            $"{ONITwitchBridge.ModFolderPath}/Saves/{SaveLoader.Instance.GameInfo.baseName}.json";

        public GameDup Get(string name) => GetOrCreate(dup => dup.Name == name, () => new GameDup(name));
        
        public bool Has(string name) => Has(dup => dup.Name == name);

        public override string GetFilePath() => FilePath;

        public static GameRegistry Initialize(JsonSerializer serializer)
        {
            PUtil.LogDebug($"Loading GameRegistry registry from {FilePath}");
            var path = FilePath;
            if (!File.Exists(path)) return new GameRegistry();

            using var textReader = new StreamReader(path);
            using var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<GameRegistry>(jsonReader);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TwitchRegistry : RegistryBase<TwitchDup>
    {
        public static readonly string FilePath = $"{ONITwitchBridge.ModFolderPath}/global.json";

        public static TwitchRegistry Initialize(JsonSerializer serializer)
        {
            PUtil.LogDebug($"Loading TwitchRegistry registry from {FilePath}");
            var fullPath = $"{ONITwitchBridge.ModFolderPath}/global.json";
            if (!File.Exists(fullPath)) return new TwitchRegistry();

            using var textReader = new StreamReader(fullPath);
            using var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<TwitchRegistry>(jsonReader);
        }

        public TwitchDup Get(string name) => GetOrCreate(dup => dup.Name == name, () => new TwitchDup(name));
        
        public bool Has(string name) => Has(dup => dup.Name == name);

        public override string GetFilePath() => FilePath;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameDup
    {
        [JsonProperty] public string Name { get; private set; }
        [JsonProperty] public bool WantJoin { get; set; }
        [JsonProperty] public bool InGame { get; set; }

        public GameDup(string name) => Name = name;

        public TwitchDup GetGlobalScope() => Registry.Get().TwitchRegistry.Get(Name);

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

        public GameDup GetGameScope() => Registry.Get().GameRegistry.Get(Name);

        public bool IsMainSkilled(SkillGroup arg) => HasMainSkill() && arg.Id == MainSkill;
        public bool HasMainSkill() => MainSkill != null;

        public void SetMainSkill(SkillGroup skill) => MainSkill = skill.Id;

        public void SetGender(string gender) => Gender = gender switch
        {
            _ when gender == GENDER_MALE => GENDER_MALE,
            _ when gender == GENDER_FEMALE => GENDER_FEMALE,
            _ => GENDER_OTHER
        };
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