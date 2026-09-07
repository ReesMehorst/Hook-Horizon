using UnityEngine;

namespace HookHorizon.Unity.Diagnostics
{
    /// <summary>Which system a log line belongs to. Toggle each independently.</summary>
    public enum LogChannel
    {
        General = 0,
        Fishing = 1,
        Content = 2,
        World = 3,
        Economy = 4,
        Player = 5,
        UI = 6
    }

    /// <summary>Severity. Messages below <see cref="GameLog.MinLevel"/> are dropped.</summary>
    public enum LogLevel
    {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Off = 4
    }

    /// <summary>
    /// Lightweight, channel-based logging for Hook &amp; Horizon. One consistent prefix,
    /// a per-channel on/off switch and a global minimum level — so the console stays
    /// readable and you can silence noise while chasing a single system. Thin wrapper over
    /// UnityEngine.Debug; warnings and errors still show up in the usual places.
    ///
    /// In the editor everything is colour-coded and every channel is on at Info level;
    /// flip channels or turn on Verbose from Tools ▸ Hook &amp; Horizon ▸ Logging. Verbose
    /// calls are compiled out of player builds (unless HH_VERBOSE is defined), so leaving
    /// verbose traces in the code costs nothing once shipped.
    /// </summary>
    public static class GameLog
    {
        private const int ChannelCount = 7;
        private static readonly bool[] Enabled = new bool[ChannelCount];

        // Console colours per channel (editor rich text). General, Fishing, Content,
        // World, Economy, Player, UI.
        private static readonly string[] Colors =
            { "9aa0a6", "36c5f0", "d9a441", "6cc070", "e0b000", "c07cd0", "d06c8a" };

        /// <summary>Global floor: messages below this level are dropped on every channel.</summary>
        public static LogLevel MinLevel = LogLevel.Info;

        static GameLog()
        {
            for (int i = 0; i < ChannelCount; i++) Enabled[i] = true;
        }

        public static bool IsEnabled(LogChannel channel) => Enabled[(int)channel];
        public static void SetEnabled(LogChannel channel, bool on) => Enabled[(int)channel] = on;

        /// <summary>Fine-grained trace. Stripped from player builds unless HH_VERBOSE is set.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("HH_VERBOSE")]
        public static void Verbose(LogChannel channel, string message) => Write(channel, LogLevel.Verbose, message);

        public static void Info(LogChannel channel, string message) => Write(channel, LogLevel.Info, message);
        public static void Warn(LogChannel channel, string message) => Write(channel, LogLevel.Warning, message);
        public static void Error(LogChannel channel, string message) => Write(channel, LogLevel.Error, message);

        private static void Write(LogChannel channel, LogLevel level, string message)
        {
            if (level < MinLevel) return;
            if (!Enabled[(int)channel]) return;

            string text = Format(channel, message);
            switch (level)
            {
                case LogLevel.Warning: Debug.LogWarning(text); break;
                case LogLevel.Error: Debug.LogError(text); break;
                default: Debug.Log(text); break;
            }
        }

        private static string Format(LogChannel channel, string message)
        {
#if UNITY_EDITOR
            return $"<b>[HH:{channel}]</b> <color=#{Colors[(int)channel]}>{message}</color>";
#else
            return $"[HH:{channel}] {message}";
#endif
        }
    }
}
