using System;
using UnityEngine;
using SchulteFlow.Core;

namespace SchulteFlow
{
    [Serializable]
    public sealed class Profile
    {
        public int version = 1;
        public int size = 4, mode, rounds;
        public bool sound = true, haptics = true, reducedMotion;
        public long[] bestMs = new long[6];
        public RoundSnapshot round;
    }

    public sealed class SaveStore
    {
        private const string Key = "schulte.unity.profile.v1";
        public Profile Data { get; private set; }
        public SaveStore()
        {
            try { Data = JsonUtility.FromJson<Profile>(PlayerPrefs.GetString(Key, "")); }
            catch (Exception) { Data = null; }
            if (Data == null || Data.version != 1) { Data = new Profile(); ImportLegacy(); }
            Data.size = Mathf.Clamp(Data.size, 3, 5); Data.mode = Mathf.Clamp(Data.mode, 0, 1);
            Data.rounds = Math.Max(0, Data.rounds);
            if (Data.bestMs == null || Data.bestMs.Length != 6) Data.bestMs = new long[6];
            for (int i = 0; i < 6; i++) if (Data.bestMs[i] < 0) Data.bestMs[i] = 0;
        }
        public long Best(Round g) { return Data.bestMs[(int)g.Mode * 3 + g.Size - 3]; }
        public bool Finish(Round g, long now)
        {
            Data.round = null;
            if (g.State != RoundState.Won) { Flush(); return false; }
            long elapsed = Math.Max(1, g.Elapsed(now)); int index = (int)g.Mode * 3 + g.Size - 3;
            bool improved = Data.bestMs[index] == 0 || elapsed < Data.bestMs[index];
            if (improved) Data.bestMs[index] = elapsed;
            Data.rounds++; Flush(); return improved;
        }
        public Round Restore()
        {
            try { return Data.round == null ? null : Round.Restore(Data.round); }
            catch (ArgumentException) { Data.round = null; Flush(); return null; }
        }
        public void SaveRound(Round g, long now)
        {
            Data.size = g.Size; Data.mode = (int)g.Mode; Data.round = g.Snapshot(now); Flush();
        }
        public void Flush() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); }

        // Android can read the old prefs only when upgrading the SAME package with the SAME signing key.
        // The default preview package intentionally coexists with v1 and has its own sandbox.
        private void ImportLegacy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var prefs = activity.Call<AndroidJavaObject>("getSharedPreferences", "schulte_flow", 0))
                {
                    if (!prefs.Call<bool>("contains", "size")) return;
                    Data.size = prefs.Call<int>("getInt", "size", 5);
                    Data.sound = prefs.Call<bool>("getBoolean", "sound", true);
                    Data.haptics = prefs.Call<bool>("getBoolean", "haptics", true);
                    Data.rounds = prefs.Call<int>("getInt", "rounds", 0);
                    for (int n = 3; n <= 5; n++) Data.bestMs[n - 3] = prefs.Call<long>("getLong", "best_" + n, 0L);
                    string json = prefs.Call<string>("getString", "round", "");
                    if (!string.IsNullOrEmpty(json))
                    {
                        var old = JsonUtility.FromJson<LegacyRound>(json);
                        if (old != null)
                        {
                            var snap = new RoundSnapshot { size = old.size, mode = 0, board = old.board,
                                next = old.next, mistakes = old.mistakes, elapsedMs = old.elapsed };
                            try { Round.Restore(snap); Data.round = snap; } catch (ArgumentException) { }
                        }
                    }
                }
            }
            catch (Exception) { Debug.LogWarning("Legacy local data could not be imported; Unity profile remains usable."); }
#endif
        }
        [Serializable] private sealed class LegacyRound
        {
            public int size = 0, next = 0, mistakes = 0;
            public long elapsed = 0;
            public int[] board = null;
        }
    }
}
