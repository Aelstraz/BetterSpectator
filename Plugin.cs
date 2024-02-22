using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BetterSpectator
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("Aelstraz.BetterSpectator");
        private static ManualLogSource logger = null;

        private void Awake()
        {
            logger = Logger;
            Settings.Load(Config);
            harmony.PatchAll(typeof(HUDManager_Patch));
            Log($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static void Log(string info)
        {
            logger.LogInfo(info);
        }
    }
}
