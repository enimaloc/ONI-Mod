using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace ONITwitchBridge
{
    public class ONITwitchBridge : UserMod2
    {
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
            Settings settings = POptions.ReadSettings<Settings>();
            IrcClient irc = new IrcClient();
            irc.Connect(settings.Username, settings.OAuth, settings.Channel);
        }
    }
}