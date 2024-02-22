using BepInEx.Configuration;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.UIElements.UIR.Allocator2D;
using System;

namespace BetterSpectator
{
    internal static class Settings
    {
        public static ConfigEntry<bool> isClockEnabled;
        public static ConfigEntry<bool> isCauseOfDeathEnabled;
        public static ConfigEntry<bool> isChatEnabled;
        public static ConfigEntry<bool> isExtraInputEnabled;
        private static bool isLoaded = false;

        public static void Load(ConfigFile config)
        {
            if (!isLoaded)
            {
                isClockEnabled = config.Bind("General", "Show Clock", true, "Shows the time of day when spectating");
                isCauseOfDeathEnabled = config.Bind("General", "Show Cause Of Death", true, "Shows each players cause of death when spectating");
                isChatEnabled = config.Bind("General", "Show Chat", true, "Enable text chat when spectating, but only with other dead players(and see alive players text chat if you are spectating them, or if they are in range of the spectated player/using a walkie talkie)");
                isExtraInputEnabled = config.Bind("General", "Enable Extra Input", true, "Enables using the scroll wheel/arrow keys to scroll forwards/back between alive players when spectating");
                isLoaded = true;
            }
        }
    }
}
