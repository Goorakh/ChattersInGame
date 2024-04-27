using BepInEx;
using BepInEx.Configuration;
using ChattersInGame.Patches;
using ChattersInGame.Twitch;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

// TODO:
// display all boss chatters in boss health bar
// show a message in chat when ally chatters die
// * remove emote modification extensions from emotes before fetching (_BW, _HF, _SG, _SQ, _TK)
// Umbral p3 doesnt display chatter name anymore?

namespace ChattersInGame
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "ChattersInGame";
        public const string PluginVersion = "1.0.0";

        internal static Main Instance { get; private set; }

        public static ConfigEntry<int> ChatterMaxInactivityTime { get; private set; }

        public static ConfigEntry<bool> UseChatterColors { get; private set; }

        public static ConfigEntry<bool> ShowChatterEmotes { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            Instance = SingletonHelper.Assign(Instance, this);

            AllyCardChatUserOverride.Apply();
            ChatNameSelector.Apply();
            CombatHealthBarChatUserOverride.Apply();
            MithrixSpeechNameOverride.Apply();
            AllyChatterDeathMessagePatch.Apply();
            BossTitleOverridePatch.Apply();

            ChatterMaxInactivityTime = Config.Bind("General", "Maximum Inactivity Time", 30, "How long a user has to be inactive in chat (in minutes) for the mod to remove them from the list of chatters");
            ModSettingsManager.AddOption(new IntSliderOption(ChatterMaxInactivityTime, new IntSliderConfig
            {
                min = 1,
                max = 120
            }));

            UseChatterColors = Config.Bind("General", "Color Chatter Names", true, "If enabled, colors the in-game text displaying chatter names to match their set user color");
            ModSettingsManager.AddOption(new CheckBoxOption(UseChatterColors));

            ShowChatterEmotes = Config.Bind("General", "Display Emotes", true, "If enabled, the last emote used by a chatter will appear above an enemy with their name on it");
            ModSettingsManager.AddOption(new CheckBoxOption(ShowChatterEmotes));

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            ModSettingsManager.AddOption(new GenericButtonOption("Authenticate", "Authentication", () =>
            {
                Task.Run(() => AuthenticationAPI.GenerateNewAuthenticationTokenAsync());
            }));

            RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, () =>
            {
                Task.Run(() => TwitchDataStorage.LoadAndValidateAccessToken());
            });

            AsyncUtils.RecordMainThread();

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        void OnDestroy()
        {
            AllyCardChatUserOverride.Undo();
            ChatNameSelector.Undo();
            CombatHealthBarChatUserOverride.Undo();
            MithrixSpeechNameOverride.Undo();
            AllyChatterDeathMessagePatch.Undo();
            BossTitleOverridePatch.Undo();

            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

            Instance = SingletonHelper.Unassign(Instance, this);
        }

        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error_NoCallerPrefix($"{sender}: {e.Exception}");
        }
    }
}
