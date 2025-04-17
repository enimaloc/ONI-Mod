using System.Collections.Generic;
using System.Linq;
using Database;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using UnityEngine;

namespace ONITwitchBridge
{
    public class ONITwitchBridge : UserMod2
    {
        public static IrcClient IrcClient;
        public static List<SkillGroup> Skills = new List<SkillGroup>();
        public static Settings Settings;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(Settings));
        }
    }

    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class PatchDbInitialize
    {
        public static void Postfix(Db __instance)
        {
            foreach (SkillGroup skillGroup in __instance.SkillGroups.resources)
            {
                ONITwitchBridge.Skills.Add(skillGroup);
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Load))]
    public static class PatchGameLoad
    {
        public static void Postfix(Game __instance)
        {
            ONITwitchBridge.Settings = POptions.ReadSettings<Settings>();
            ONITwitchBridge.IrcClient = new IrcClient();
            ONITwitchBridge.IrcClient.RegisterCommands();
            ONITwitchBridge.IrcClient.Connect(ONITwitchBridge.Settings.Username, ONITwitchBridge.Settings.OAuth, ONITwitchBridge.Settings.Channel);
        }
    }

    [HarmonyPatch(typeof(Game), "DestroyInstances")]
    public static class PatchGameDestroy
    {
        public static void Postfix()
        {
            if (ONITwitchBridge.IrcClient == null) return;
            ONITwitchBridge.IrcClient.Disconnect();
            ONITwitchBridge.IrcClient = null;
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateStats")]
    public static class PatchGenerateStats
    {
        public static void Postfix(MinionStartingStats __instance)
        {
            PUtil.LogDebug($"Skills of {__instance.Name}:");
            foreach (var (key, value) in __instance.skillAptitudes)
            {
                PUtil.LogDebug($"{((IListableOption) key).GetProperName()}: {value}");
            }

            __instance.Name = ONITwitchBridge.IrcClient
                .GetRandomUser(new Dup(__instance.Name),
                    __instance.skillAptitudes.Where(apt => Mathf.Approximately(apt.Value, 1)).ToDictionary(p => p.Key, p => p.Value).Keys,
                    __instance.GenderStringKey)
                .Username;
        }
    }

    [HarmonyPatch(typeof(Telepad), "OnAcceptDelivery")]
    public static class PatchTelepadOnAcceptDelivery
    {
        public static void Postfix(ITelepadDeliverable delivery)
        {
            if (!(delivery is MinionStartingStats minion)) return;
            var name = minion.Name;
            if (ONITwitchBridge.IrcClient.RemoveUser(name))
                ONITwitchBridge.IrcClient.SendMessage($"{name} has been accepted as a Duplicant. Welcome to the colony!");
        }
    }
}