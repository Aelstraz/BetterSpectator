using BepInEx;
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
            logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            harmony.PatchAll(typeof(HUDManager_Patch));
        }

        public static void LogInfo(string info)
        {
            logger.LogInfo(info);
        }
    }
}
