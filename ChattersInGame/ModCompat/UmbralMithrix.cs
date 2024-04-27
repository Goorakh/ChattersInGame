using BepInEx.Bootstrap;

namespace ChattersInGame.ModCompat
{
    static class UmbralMithrix
    {
        public static bool IsActive => Chainloader.PluginInfos.ContainsKey("com.Nuxlar.UmbralMithrix");
    }
}
