#if UNITY_EDITOR
using System;
using UnityEditor;
using HookHorizon.Unity.Diagnostics;

namespace HookHorizon.Unity.EditorTools
{
    /// <summary>
    /// Tools ▸ Hook &amp; Horizon ▸ Logging — flip log channels and turn Verbose on/off from
    /// the menu (checkmarks show the current state). Choices persist in EditorPrefs and are
    /// re-applied to <see cref="GameLog"/> after every script recompile (static state resets
    /// on domain reload), so your logging setup sticks between sessions.
    /// </summary>
    [InitializeOnLoad]
    public static class LoggingMenu
    {
        private const string Menu = "Tools/Hook & Horizon/Logging/";
        private const string ChannelPrefix = "HH.Log.Channel.";
        private const string LevelKey = "HH.Log.MinLevel";

        static LoggingMenu() => ApplySavedSettings();

        private static void ApplySavedSettings()
        {
            foreach (LogChannel ch in Enum.GetValues(typeof(LogChannel)))
                GameLog.SetEnabled(ch, EditorPrefs.GetBool(ChannelPrefix + ch, true));
            GameLog.MinLevel = (LogLevel)EditorPrefs.GetInt(LevelKey, (int)LogLevel.Info);
        }

        // ---- Channel toggles ---------------------------------------------------------
        [MenuItem(Menu + "General", false, 0)] private static void TGeneral() => ToggleChannel(LogChannel.General);
        [MenuItem(Menu + "General", true)] private static bool VGeneral() => ShowChannel(LogChannel.General, Menu + "General");

        [MenuItem(Menu + "Fishing", false, 1)] private static void TFishing() => ToggleChannel(LogChannel.Fishing);
        [MenuItem(Menu + "Fishing", true)] private static bool VFishing() => ShowChannel(LogChannel.Fishing, Menu + "Fishing");

        [MenuItem(Menu + "Content", false, 2)] private static void TContent() => ToggleChannel(LogChannel.Content);
        [MenuItem(Menu + "Content", true)] private static bool VContent() => ShowChannel(LogChannel.Content, Menu + "Content");

        [MenuItem(Menu + "World", false, 3)] private static void TWorld() => ToggleChannel(LogChannel.World);
        [MenuItem(Menu + "World", true)] private static bool VWorld() => ShowChannel(LogChannel.World, Menu + "World");

        [MenuItem(Menu + "Economy", false, 4)] private static void TEconomy() => ToggleChannel(LogChannel.Economy);
        [MenuItem(Menu + "Economy", true)] private static bool VEconomy() => ShowChannel(LogChannel.Economy, Menu + "Economy");

        [MenuItem(Menu + "Player", false, 5)] private static void TPlayer() => ToggleChannel(LogChannel.Player);
        [MenuItem(Menu + "Player", true)] private static bool VPlayer() => ShowChannel(LogChannel.Player, Menu + "Player");

        [MenuItem(Menu + "UI", false, 6)] private static void TUI() => ToggleChannel(LogChannel.UI);
        [MenuItem(Menu + "UI", true)] private static bool VUI() => ShowChannel(LogChannel.UI, Menu + "UI");

        // ---- Verbose + bulk actions --------------------------------------------------
        [MenuItem(Menu + "Verbose", false, 20)] private static void ToggleVerbose()
        {
            LogLevel next = GameLog.MinLevel <= LogLevel.Verbose ? LogLevel.Info : LogLevel.Verbose;
            GameLog.MinLevel = next;
            EditorPrefs.SetInt(LevelKey, (int)next);
        }
        [MenuItem(Menu + "Verbose", true)] private static bool ShowVerbose()
        {
            UnityEditor.Menu.SetChecked(Menu + "Verbose", GameLog.MinLevel <= LogLevel.Verbose);
            return true;
        }

        [MenuItem(Menu + "Enable All Channels", false, 40)] private static void EnableAll() => SetAll(true);
        [MenuItem(Menu + "Mute All Channels", false, 41)] private static void MuteAll() => SetAll(false);

        // ---- Helpers -----------------------------------------------------------------
        private static void ToggleChannel(LogChannel ch)
        {
            bool on = !GameLog.IsEnabled(ch);
            GameLog.SetEnabled(ch, on);
            EditorPrefs.SetBool(ChannelPrefix + ch, on);
        }

        private static bool ShowChannel(LogChannel ch, string path)
        {
            UnityEditor.Menu.SetChecked(path, GameLog.IsEnabled(ch));
            return true;
        }

        private static void SetAll(bool on)
        {
            foreach (LogChannel ch in Enum.GetValues(typeof(LogChannel)))
            {
                GameLog.SetEnabled(ch, on);
                EditorPrefs.SetBool(ChannelPrefix + ch, on);
            }
        }
    }
}
#endif
