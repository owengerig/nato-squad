using System;
using System.Collections.Generic;
using UnityEngine;

namespace OGSoftware.Platform
{
    /// <summary>One row of a leaderboard.</summary>
    public struct OGScore
    {
        public long rank;
        public string name;
        public long score;
        public bool isMe;
    }

    /// <summary>
    /// OG Software's shared game services layer: Apple Game Center on iOS and Google Play Games Services (v2) on
    /// Android, behind one API. Covers sign-in, achievements, leaderboards and cloud saves (see OGCloudSave).
    ///
    /// - Every call is safe to make at any time. When the player isn't signed in (or in the editor / on PC),
    ///   achievement progress and best scores are kept on the device and sent as soon as they sign in.
    /// - Achievements and leaderboards are declared once with a short key plus the store IDs
    ///   (see the game's GameServices.cs). The game only ever uses the keys.
    /// - Native code: Assets/Plugins/iOS/OGPlatform/OGGameCenter.mm and
    ///   Assets/Plugins/Android/OGPlatform/OGPlayGames.java. Build setup (GameKit, iCloud, the Play Games
    ///   dependency and App ID) is added automatically by Assets/OGPlatform/Editor/OGPlatformBuild.cs.
    /// - Same file in every OG Software Unity game; change it in one and copy it to the others.
    /// </summary>
    public static class OGPlatform
    {
        public const string Version = "1.0.0";
        public const string ObjectName = "OGPlatform"; // the native side sends messages to this GameObject

        class AchievementDef { public string ios, android; public int steps; }
        class LeaderboardDef { public string ios, android; public bool lowerIsBetter; }

        static readonly Dictionary<string, AchievementDef> achievements = new Dictionary<string, AchievementDef>();
        static readonly Dictionary<string, LeaderboardDef> leaderboards = new Dictionary<string, LeaderboardDef>();
        static readonly Dictionary<string, Action<List<OGScore>>> scoreRequests = new Dictionary<string, Action<List<OGScore>>>();
        static readonly List<Action<bool, string>> loadWaiters = new List<Action<bool, string>>();
        static Action<bool> saveWaiter;
        static int nextRequest;
        static bool initialized;

        public static bool IsSignedIn { get; private set; }
        public static string PlayerName { get; private set; } = "";
        public static string PlayerId { get; private set; } = "";

        /// <summary>Raised on the main thread whenever sign-in finishes or changes.</summary>
        public static event Action SignInChanged;

        /// <summary>True on an iOS or Android device build; everywhere else the calls only store progress locally.</summary>
        public static bool IsSupported
        {
            get
            {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>"Game Center", "Google Play Games", or a neutral name elsewhere.</summary>
        public static string ServiceName
        {
            get
            {
#if UNITY_IOS
                return "Game Center";
#elif UNITY_ANDROID
                return "Google Play Games";
#else
                return "Game services";
#endif
            }
        }

        // ---------------------------------------------------------------- setup

        /// <summary>
        /// Declares an achievement. steps = 1 for a normal one; more than 1 for a progress achievement (iOS shows a
        /// percentage; on Google Play it must be created as "incremental" with the same number of steps).
        /// </summary>
        public static void DefineAchievement(string key, string iosId, string androidId, int steps = 1)
        {
            achievements[key] = new AchievementDef { ios = iosId, android = androidId, steps = Mathf.Max(1, steps) };
        }

        /// <summary>Declares a leaderboard. Set lowerIsBetter for golf-style scores (must match the store setting).</summary>
        public static void DefineLeaderboard(string key, string iosId, string androidId, bool lowerIsBetter = false)
        {
            leaderboards[key] = new LeaderboardDef { ios = iosId, android = androidId, lowerIsBetter = lowerIsBetter };
        }

        /// <summary>Starts the service and signs in silently (iOS shows Apple's own sign-in sheet the first time).</summary>
        public static void Init()
        {
            if (initialized) return;
            initialized = true;
            OGPlatformDispatcher.Ensure();
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_Init();
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("init", Native.Activity, new Native.Callback());
#else
                Debug.Log("[OGPlatform] " + ServiceName + " isn't available here; progress is kept locally.");
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] init failed: " + e.Message); }
        }

        /// <summary>
        /// Interactive sign-in, for a "Sign in" button. Android shows the Play Games sign-in. On iOS Apple only shows
        /// its sign-in sheet once; after that the player signs in from Settings → Game Center.
        /// </summary>
        public static void SignIn()
        {
            Init();
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_Init();
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("signIn");
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] sign-in failed: " + e.Message); }
        }

        // ---------------------------------------------------------------- achievements

        /// <summary>Unlocks an achievement. Safe to call repeatedly; it is only sent once.</summary>
        public static void Unlock(string key)
        {
            if (!achievements.TryGetValue(key, out var a)) { Debug.LogWarning("[OGPlatform] unknown achievement " + key); return; }
            SetProgress(key, a.steps);
        }

        /// <summary>Sets a progress achievement to `current` steps (out of the steps it was declared with). Never goes down.</summary>
        public static void SetProgress(string key, int current)
        {
            if (!achievements.TryGetValue(key, out var a)) { Debug.LogWarning("[OGPlatform] unknown achievement " + key); return; }
            current = Mathf.Clamp(current, 0, a.steps);
            string bestKey = "og.ach." + key;
            if (current <= PlayerPrefs.GetInt(bestKey, 0)) { FlushAchievement(key); return; }
            PlayerPrefs.SetInt(bestKey, current);
            PlayerPrefs.Save();
            FlushAchievement(key);
        }

        /// <summary>True once the achievement is fully earned on this device (whether or not it has been sent yet).</summary>
        public static bool IsUnlocked(string key)
        {
            return achievements.TryGetValue(key, out var a) && PlayerPrefs.GetInt("og.ach." + key, 0) >= a.steps;
        }

        static void FlushAchievement(string key)
        {
            if (!IsSignedIn || !achievements.TryGetValue(key, out var a)) return;
            int best = PlayerPrefs.GetInt("og.ach." + key, 0);
            if (best <= PlayerPrefs.GetInt("og.achSent." + key, 0)) return;
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                if (string.IsNullOrEmpty(a.ios)) return;
                Native.OGGC_ReportAchievement(key, a.ios, 100.0 * best / a.steps, best);
#elif UNITY_ANDROID && !UNITY_EDITOR
                if (string.IsNullOrEmpty(a.android) || a.android.StartsWith("TODO")) return;
                Native.Android.CallStatic("reportAchievement", key, a.android, best, a.steps);
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] achievement " + key + " failed: " + e.Message); }
        }

        /// <summary>Opens the platform's achievements screen.</summary>
        public static void ShowAchievements()
        {
            if (!IsSignedIn) { SignIn(); return; }
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_ShowAchievements();
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("showAchievements");
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] " + e.Message); }
        }

        // ---------------------------------------------------------------- leaderboards

        /// <summary>Submits a score. The best one is kept on the device and sent when the player is signed in.</summary>
        public static void SubmitScore(string key, long score)
        {
            if (!leaderboards.TryGetValue(key, out var lb)) { Debug.LogWarning("[OGPlatform] unknown leaderboard " + key); return; }
            string bestKey = "og.lb." + key;
            string had = PlayerPrefs.GetString(bestKey, "");
            if (had == "" || (lb.lowerIsBetter ? score < long.Parse(had) : score > long.Parse(had)))
            {
                PlayerPrefs.SetString(bestKey, score.ToString());
                PlayerPrefs.DeleteKey("og.lbSent." + key);
                PlayerPrefs.Save();
            }
            FlushScore(key);
        }

        static void FlushScore(string key)
        {
            if (!IsSignedIn || !leaderboards.TryGetValue(key, out var lb)) return;
            string best = PlayerPrefs.GetString("og.lb." + key, "");
            if (best == "" || PlayerPrefs.GetString("og.lbSent." + key, "") == best) return;
            long score = long.Parse(best);
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                if (string.IsNullOrEmpty(lb.ios)) return;
                Native.OGGC_SubmitScore(key, lb.ios, score);
#elif UNITY_ANDROID && !UNITY_EDITOR
                if (string.IsNullOrEmpty(lb.android) || lb.android.StartsWith("TODO")) return;
                Native.Android.CallStatic("submitScore", key, lb.android, score);
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] score " + key + " failed: " + e.Message); }
        }

        /// <summary>Loads the top `count` all-time scores. Calls back on the main thread with an empty list on failure.</summary>
        public static void LoadTopScores(string key, int count, Action<List<OGScore>> done)
        {
            if (!IsSignedIn || !leaderboards.TryGetValue(key, out var lb)) { done?.Invoke(new List<OGScore>()); return; }
            string req = "scores:" + (nextRequest++);
            scoreRequests[req] = done;
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_LoadTopScores(req, lb.ios, count);
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("loadTopScores", req, lb.android, count);
#else
                Deliver(req, false, "");
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] " + e.Message); Deliver(req, false, ""); }
        }

        /// <summary>Opens the platform's leaderboard screen (all leaderboards if key is null).</summary>
        public static void ShowLeaderboard(string key = null)
        {
            if (!IsSignedIn) { SignIn(); return; }
            string ios = "", android = "";
            if (key != null && leaderboards.TryGetValue(key, out var lb)) { ios = lb.ios; android = lb.android; }
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_ShowLeaderboard(ios);
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("showLeaderboard", android);
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] " + e.Message); }
        }

        // ---------------------------------------------------------------- cloud save (used by OGCloudSave)

        internal static void CloudLoad(string slot, Action<bool, string> done)
        {
            if (!IsSignedIn) { done(false, ""); return; }
            loadWaiters.Add(done);
            if (loadWaiters.Count > 1) return; // a load is already running; everyone gets its result
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_CloudLoad(slot);
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("cloudLoad", slot);
#else
                Deliver("load", false, "");
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] cloud load failed: " + e.Message); Deliver("load", false, ""); }
        }

        internal static void CloudSave(string slot, string data, string description, Action<bool> done)
        {
            if (!IsSignedIn) { done(false); return; }
            saveWaiter = done;
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                Native.OGGC_CloudSave(slot, data);
#elif UNITY_ANDROID && !UNITY_EDITOR
                Native.Android.CallStatic("cloudSave", slot, data, description);
#else
                Deliver("save", false, "");
#endif
            }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] cloud save failed: " + e.Message); Deliver("save", false, ""); }
        }

        // ---------------------------------------------------------------- results from the native side

        /// <summary>Every native result ends up here, on the main thread. op / ok / data.</summary>
        internal static void Deliver(string op, bool ok, string data)
        {
            data = data ?? "";
            if (op == "auth")
            {
                bool was = IsSignedIn;
                IsSignedIn = ok;
                if (ok)
                {
                    int nl = data.IndexOf('\n');
                    PlayerId = nl >= 0 ? data.Substring(0, nl) : data;
                    PlayerName = nl >= 0 ? data.Substring(nl + 1) : "";
                    Debug.Log("[OGPlatform] signed in to " + ServiceName + " as " + PlayerName);
                    foreach (var k in achievements.Keys) FlushAchievement(k);
                    foreach (var k in leaderboards.Keys) FlushScore(k);
                }
                else
                {
                    PlayerId = PlayerName = "";
                    if (data != "") Debug.Log("[OGPlatform] not signed in: " + data);
                }
                if (ok != was || ok) SignInChanged?.Invoke();
                if (ok) OGCloudSave.OnSignedIn();
                return;
            }
            if (op.StartsWith("ach:"))
            {
                string key = op.Substring(4);
                if (ok && int.TryParse(data, out int sent))
                {
                    if (sent > PlayerPrefs.GetInt("og.achSent." + key, 0)) { PlayerPrefs.SetInt("og.achSent." + key, sent); PlayerPrefs.Save(); }
                }
                else if (!ok) Debug.LogWarning("[OGPlatform] achievement " + key + " not sent: " + data);
                return;
            }
            if (op.StartsWith("lb:"))
            {
                string key = op.Substring(3);
                if (ok) { PlayerPrefs.SetString("og.lbSent." + key, data); PlayerPrefs.Save(); }
                else Debug.LogWarning("[OGPlatform] score for " + key + " not sent: " + data);
                return;
            }
            if (op.StartsWith("scores:"))
            {
                if (!scoreRequests.TryGetValue(op, out var cb)) return;
                scoreRequests.Remove(op);
                var list = new List<OGScore>();
                if (ok)
                    foreach (var line in data.Split('\n'))
                    {
                        var f = line.Split('\t');
                        if (f.Length < 4) continue;
                        long.TryParse(f[0], out long rank);
                        long.TryParse(f[2], out long score);
                        list.Add(new OGScore { rank = rank, name = f[1], score = score, isMe = f[3] == "1" });
                    }
                cb?.Invoke(list);
                return;
            }
            if (op == "load")
            {
                var waiters = loadWaiters.ToArray();
                loadWaiters.Clear();
                if (!ok) Debug.LogWarning("[OGPlatform] cloud load failed: " + data);
                foreach (var w in waiters) w(ok, ok ? data : "");
                return;
            }
            if (op == "save")
            {
                var w = saveWaiter;
                saveWaiter = null;
                if (!ok) Debug.LogWarning("[OGPlatform] cloud save failed: " + data);
                w?.Invoke(ok);
            }
        }

        // ---------------------------------------------------------------- native bindings

        static class Native
        {
#if UNITY_IOS && !UNITY_EDITOR
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_Init();
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_ReportAchievement(string key, string id, double percent, int steps);
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_ShowAchievements();
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_SubmitScore(string key, string id, long score);
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_LoadTopScores(string req, string id, int count);
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_ShowLeaderboard(string id);
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_CloudLoad(string slot);
            [System.Runtime.InteropServices.DllImport("__Internal")] public static extern void OGGC_CloudSave(string slot, string data);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            static AndroidJavaClass android;
            public static AndroidJavaClass Android => android ?? (android = new AndroidJavaClass("com.ogsoftware.platform.OGPlayGames"));

            public static AndroidJavaObject Activity
            {
                get { using (var p = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) return p.GetStatic<AndroidJavaObject>("currentActivity"); }
            }

            /// <summary>Java calls this from its own thread; the dispatcher hands it to the main thread.</summary>
            public class Callback : AndroidJavaProxy
            {
                public Callback() : base("com.ogsoftware.platform.OGPlayGames$Callback") { }
                public void onResult(string op, bool ok, string data) { OGPlatformDispatcher.Post(op, ok, data); }
            }
#endif
        }
    }

    /// <summary>
    /// Hidden object that receives native messages (iOS UnitySendMessage), moves Android callbacks to the main thread,
    /// and uploads a pending cloud save when the app goes to the background.
    /// </summary>
    public class OGPlatformDispatcher : MonoBehaviour
    {
        static OGPlatformDispatcher instance;
        static readonly object gate = new object();
        static readonly List<(string op, bool ok, string data)> queue = new List<(string, bool, string)>();

        public static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject(OGPlatform.ObjectName);
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            instance = go.AddComponent<OGPlatformDispatcher>();
        }

        public static void Post(string op, bool ok, string data)
        {
            lock (gate) queue.Add((op, ok, data));
        }

        /// <summary>iOS: "op\nok\ndata".</summary>
        public void OnNative(string message)
        {
            var parts = (message ?? "").Split(new[] { '\n' }, 3);
            if (parts.Length < 2) return;
            OGPlatform.Deliver(parts[0], parts[1] == "1", parts.Length > 2 ? parts[2] : "");
        }

        void Update()
        {
            (string op, bool ok, string data)[] items = null;
            lock (gate)
            {
                if (queue.Count > 0) { items = queue.ToArray(); queue.Clear(); }
            }
            if (items != null)
                foreach (var i in items)
                {
                    try { OGPlatform.Deliver(i.op, i.ok, i.data); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            OGCloudSave.Tick();
        }

        void OnApplicationPause(bool paused) { if (paused) OGCloudSave.PushNow(); }
        void OnApplicationQuit() { OGCloudSave.PushNow(); }
    }
}
