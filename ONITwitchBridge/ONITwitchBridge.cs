using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace ONITwitchBridge
{
    public class ONITwitchBridge : UserMod2
    {
        public static IrcClient IrcClient;
        public static Settings Settings;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(Settings));
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
        public static void Postfix(MinionStartingStats __instance) =>
            __instance.Name = ONITwitchBridge.IrcClient.GetRandomUser(__instance.Name);
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