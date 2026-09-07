using UnityEngine;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// Player options that persist independently of the save game, in <see cref="PlayerPrefs"/>
    /// (they belong to the device, not a save slot). Applies itself to Unity — audio volume and
    /// fullscreen — and carries the autosave preference, which the menu pushes onto the
    /// <see cref="Persistence.GameSaveController"/>. Deliberately tiny; extend as real options
    /// appear.
    /// </summary>
    public sealed class GameSettings
    {
        private const string VolumeKey = "hh_opt_volume";
        private const string FullscreenKey = "hh_opt_fullscreen";
        private const string AutosaveKey = "hh_opt_autosave";

        public float MasterVolume;
        public bool Fullscreen;
        public bool AutosaveEnabled;

        public static GameSettings Load() => new GameSettings
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f)),
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1,
            AutosaveEnabled = PlayerPrefs.GetInt(AutosaveKey, 1) == 1
        };

        /// <summary>Push the current values onto Unity (audio + screen).</summary>
        public void Apply()
        {
            AudioListener.volume = MasterVolume;
            if (Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(AutosaveKey, AutosaveEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
