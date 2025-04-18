using System.Collections.Generic;
using System.Linq;
using Database;
using HarmonyLib;
using KMod;
using ONITwitchBridge;
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
            ONITwitchBridge.Skills.AddRange(__instance.SkillGroups.resources);
        }

        [HarmonyPatch(typeof(Game), nameof(Game.Load))]
        [HarmonyPostfix]
        public static void GameLoad_Postfix(Game __instance)
        {
            ONITwitchBridge.Settings = POptions.ReadSettings<Settings>();

            ONITwitchBridge.IrcClient = new IrcClient();
            ONITwitchBridge.IrcClient.RegisterCommands();
            ONITwitchBridge.IrcClient.Connect(
                ONITwitchBridge.Settings.Username,
                ONITwitchBridge.Settings.OAuth,
                ONITwitchBridge.Settings.Channel
            );

            Registry.Get().Initialize();
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

        [HarmonyPatch(typeof(Telepad), "OnAcceptDelivery")]
        [HarmonyPostfix]
        public static void TelepadOnAcceptDelivery_Postfix(ITelepadDeliverable delivery)
        {
            if (!(delivery is MinionStartingStats minion)) return;

            var name = minion.Name;
            var dup = Registry.Get().GameRegistry.Get(name);
            dup.InGame = true;

            ONITwitchBridge.IrcClient.SendMessage($"{name} has been accepted as a Duplicant. Welcome to the colony!");
        }
    }

    // [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    // public static class PatchDbInitialize
    // {
    // public static void Postfix(Db __instance)
    // {
    // foreach (var skillGroup in __instance.SkillGroups.resources)
    // {
    // ONITwitchBridge.Skills.Add(skillGroup);
    // }
    // }
    // }

    // [HarmonyPatch(typeof(Game), nameof(Game.Load))]
    // public static class PatchGameLoad
    // {
    // public static void Postfix(Game __instance)
    // {
    // ONITwitchBridge.Settings = POptions.ReadSettings<Settings>();
    // ONITwitchBridge.IrcClient = new IrcClient();
    // ONITwitchBridge.IrcClient.RegisterCommands();
    // ONITwitchBridge.IrcClient.Connect(ONITwitchBridge.Settings.Username, ONITwitchBridge.Settings.OAuth,
    // ONITwitchBridge.Settings.Channel);
    // Registry.Get().Initialize();
    // }
    // }

    // [HarmonyPatch(typeof(Game), nameof(Game.Save))]
    // public static class PatchGameSave
    // {
    // public static void Postfix(Game __instance)
    // {
    // Registry.Get().Save();
    // }
    // }

    // [HarmonyPatch(typeof(Game), "DestroyInstances")]
    // public static class PatchGameDestroy
    // {
    // public static void Postfix()
    // {
    // if (ONITwitchBridge.IrcClient == null) return;
    // ONITwitchBridge.IrcClient.Disconnect();
    // ONITwitchBridge.IrcClient = null;
    // }
    // }

    // [HarmonyPatch(typeof(MinionStartingStats), "GenerateStats")]
    // public static class PatchGenerateStats
    // {
    // public static void Postfix(MinionStartingStats __instance)
    // {
    // PUtil.LogDebug($"Skills of {__instance.Name}:");
    // foreach (var (key, value) in __instance.skillAptitudes)
    // {
    // PUtil.LogDebug($"{((IListableOption)key).GetProperName()}: {value}");
    // }

    // __instance.Name = ONITwitchBridge.IrcClient
    // .GetRandomUser(new Dup(__instance.Name),
    // __instance.skillAptitudes.Where(apt => Mathf.Approximately(apt.Value, 1))
    // .ToDictionary(p => p.Key, p => p.Value).Keys,
    // __instance.GenderStringKey)
    // .Name;
    // }
    // }

    // [HarmonyPatch(typeof(Telepad), "OnAcceptDelivery")]
    // public static class PatchTelepadOnAcceptDelivery
    // {
    // public static void Postfix(ITelepadDeliverable delivery)
    // {
    // if (!(delivery is MinionStartingStats minion)) return;
    // var name = minion.Name;
    // var dup = Registry.Get().GameRegistry.Get(name);
    // dup.InGame = true;
    // ONITwitchBridge.IrcClient.SendMessage($"{name} has been accepted as a Duplicant. Welcome to the colony!");
    // }
    // }
}