using BepInEx.Configuration;

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
                isChatEnabled = config.Bind("General", "Show Chat", true, "Shows the chat box and allows dead players to talk to each other");
                isExtraInputEnabled = config.Bind("General", "Enable Extra Input", true, "Enables using the scroll wheel/arrow keys to scroll forwards/back between alive players when spectating");
                isLoaded = true;
            }
        }
    }
}
