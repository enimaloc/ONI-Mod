using System.Collections.Generic;
using System.Linq;
using Database;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using UnityEngine;

namespace enimaloc.onitb
{
    public class ONITwitchBridge : UserMod2
    {
        public static IrcClient IrcClient;
        public static List<SkillGroup> Skills = new List<SkillGroup>();
        public static Settings Settings;
        public static string ModFolderPath;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(Settings));
            ModFolderPath = path;
        }
    }

    [HarmonyPatch]
    public static class ONITBPatches
    {
        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        [HarmonyPostfix]
        public static void DbInitialize_Postfix(Db __instance)
        {
            for (var i = 0; i < __instance.SkillGroups.Count; i++)
            {
                var skillGroup = __instance.SkillGroups[i];
                if (!DlcManager.FeatureClusterSpaceEnabled() && skillGroup.Id.Equals("Rocketry")) return;
                ONITwitchBridge.Skills.Add(skillGroup);
            }

        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        [HarmonyPostfix]
        public static void GameOnSpawn_Postfix(Game __instance)
        {
            PUtil.LogDebug("ONITwitchBridge: Launching...");
            ONITwitchBridge.Settings = POptions.ReadSettings<Settings>();

            ONITwitchBridge.IrcClient = new IrcClient();
            ONITwitchBridge.IrcClient.RegisterCommands();
            ONITwitchBridge.IrcClient.Connect(
                ONITwitchBridge.Settings.Username,
                ONITwitchBridge.Settings.UnmaskedOauth,
                ONITwitchBridge.Settings.Channel
            );
            Registry.Get().Initialize();
        }
        
        [HarmonyPatch(typeof(MinionIdentity), "ApplyCustomGameSettings")]
        [HarmonyPostfix]
        public static void MinionIdentityApplyCustomGameSettings_Postfix(MinionIdentity __instance)
        {
            var registry = Registry.Get().GameRegistry;
            if (registry.Has(__instance.name))
            {
                registry.Get(__instance.name).GameObject = __instance.gameObject;
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.Save))]
        [HarmonyPostfix]
        public static void GameSave_Postfix(Game __instance)
        {
            Registry.Get().Save();
        }

        [HarmonyPatch(typeof(Game), "DestroyInstances")]
        [HarmonyPostfix]
        public static void GameDestroy_Postfix()
        {
            ONITwitchBridge.IrcClient?.Disconnect();
            ONITwitchBridge.IrcClient = null;
        }

        [HarmonyPatch(typeof(MinionStartingStats), "GenerateStats")]
        [HarmonyPostfix]
        public static void GenerateStats_Postfix(MinionStartingStats __instance)
        {
            PUtil.LogDebug($"Skills of {__instance.Name}:");
            foreach (var (key, value) in __instance.skillAptitudes)
            {
                PUtil.LogDebug($"{((IListableOption)key).GetProperName()}: {value}");
            }

            var relevantSkills = __instance.skillAptitudes
                .Where(pair => Mathf.Approximately(pair.Value, 1f))
                .Select(pair => pair.Key)
                .ToList();

            var generatedDup = new Dup(__instance.Name);
            var twitchDup =
                ONITwitchBridge.IrcClient.GetRandomUser(generatedDup, relevantSkills, __instance.GenderStringKey);

            __instance.Name = twitchDup.Name;
        }

        [HarmonyPatch(typeof(MinionStartingStats), nameof(MinionStartingStats.Deliver))]
        [HarmonyPostfix]
        public static void MinionStartingStatsDeliver_Postfix(GameObject __result)
        {
            var name = __result.name;
            var dup = Registry.Get().GameRegistry.Get(name);
            dup.InGame = true;
            dup.GameObject = __result;

            ONITwitchBridge.IrcClient.SendMessage($"{name} has been accepted as a Duplicant. Welcome to the colony!");
        }
        
        [HarmonyPatch(typeof(MinionIdentity), "OnDied")]
        [HarmonyPostfix]
        public static void MinionIdentityOnDied_Postfix(MinionIdentity __instance)
        {
            var name = __instance.GetProperName();
            if (!Registry.Get().GameRegistry.Has(name)) return;
            var dup = Registry.Get().GameRegistry.Get(name);
            dup.InGame = false;
            dup.WantJoin = false;

            ONITwitchBridge.IrcClient.SendMessage($"{name} has died. Rest in peace.");
        }
    }
}