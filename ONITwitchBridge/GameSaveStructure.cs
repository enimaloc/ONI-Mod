using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ONITwitchBridge
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameSaveStructure
    {
        private static readonly JsonSerializer JSON_SERIALIZER = new JsonSerializer();
        [JsonProperty] public List<GameDup> SavedDups = new List<GameDup>();
        
        public GameDup GetGameDup(string username)
        {
            foreach (var dup in SavedDups)
            {
                if (dup.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    return dup;
                }
            }

            return null;
        }
        
        public GameDup AddGameDup(string username)
        {
            var dup = new GameDup {Username = username, CanJoin = true};
            SavedDups.Add(dup);
            return dup;
        }

        public static GameSaveStructure Load()
        {
            string currentSaveName = SaveLoader.Instance.GameInfo.baseName;
            string fullPath = $"{ONITwitchBridge.ModFolderPath}/Saves/{currentSaveName}.json";
            if (!File.Exists(fullPath))
            {
                return new GameSaveStructure();
            }
            using (var textReader = new StreamReader(fullPath))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                return JSON_SERIALIZER.Deserialize<GameSaveStructure>(jsonReader);
            }
        }
    }
}