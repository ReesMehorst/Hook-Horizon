using UnityEngine;
using UnityEngine.SceneManagement;

namespace HookHorizon.Unity.Persistence
{
    /// <summary>
    /// Boot intent that survives a scene reload. "New Game" can't rebuild in place — the
    /// <c>GameBootstrap</c> creates fresh objects each run — so it reloads the active scene
    /// with this static flag set, and the bootstrap consumes it on the next Build to start
    /// clean instead of continuing the most recent save.
    /// </summary>
    public static class GameSession
    {
        public enum Intent { Continue, NewGame }

        public static Intent BootIntent = Intent.Continue;
        public static int? ForcedSeed;

        /// <summary>
        /// Start a fresh game: roll a new world seed and reload the scene. Existing manual save
        /// slots are kept; only the autosave is replaced by the new world. Requires the scene to
        /// be in Build Settings so it can reload (logs a clear warning if it isn't).
        /// </summary>
        public static void StartNewGame()
        {
            Time.timeScale = 1f; // a pause menu may have frozen time; reloading must run
            BootIntent = Intent.NewGame;
            ForcedSeed = new System.Random().Next(int.MinValue, int.MaxValue);

            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
            else
            {
                Debug.LogWarning("[Hook & Horizon] New Game reloads the scene, which needs it added to " +
                    "Build Settings (File > Build Settings > Add Open Scenes). Add it, then New Game works.");
                BootIntent = Intent.Continue;
                ForcedSeed = null;
            }
        }

        /// <summary>True if a New Game was requested; outputs its seed and resets the intent.</summary>
        public static bool ConsumeNewGameIntent(out int? seed)
        {
            if (BootIntent != Intent.NewGame) { seed = null; return false; }
            seed = ForcedSeed;
            BootIntent = Intent.Continue;
            ForcedSeed = null;
            return true;
        }
    }
}
