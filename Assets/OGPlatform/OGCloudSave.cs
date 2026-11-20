using System;
using UnityEngine;

namespace OGSoftware.Platform
{
    /// <summary>What a game gives OGCloudSave: its save as text, a progress number, and a way to load a save back.</summary>
    public interface IOGCloudData
    {
        /// <summary>The whole profile as one string (usually the save file's JSON).</summary>
        string Export();

        /// <summary>
        /// A number that only grows as the player progresses (levels cleared, wins, gold earned, rounds played...).
        /// Used to pick between the device's save and the cloud's: the one with more progress wins.
        /// </summary>
        long Progress(string exported);

        /// <summary>True on a fresh install / after the data was lost: the cloud copy is always taken then.</summary>
        bool LocalIsEmpty { get; }

        /// <summary>Replace the local profile with this one (from Export on another device) and refresh the screens.</summary>
        void Import(string exported);
    }

    /// <summary>
    /// Keeps the player's profile in Game Center saved games (iOS, needs iCloud Drive) or Play Games saved games
    /// (Android), so it comes back on a new device or after a reinstall.
    ///
    /// - The game calls Register once at startup and MarkDirty after every local save. The upload waits a little
    ///   (so a burst of saves becomes one upload) and also happens when the app goes to the background.
    /// - On sign-in the cloud copy is read first. Nothing is uploaded until that read has succeeded, so a fresh
    ///   install can never overwrite a good cloud save.
    /// - Choosing: an empty device always takes the cloud copy; otherwise the copy with more progress wins, and on a
    ///   tie the newer one. Before the device's save is replaced it is kept in PlayerPrefs ("og.cloud.backup").
    /// </summary>
    public static class OGCloudSave
    {
        const string Slot = "profile";
        const float UploadDelay = 20f;

        [Serializable]
        class Envelope
        {
            public int v = 1;
            public long savedAt;    // unix seconds
            public long progress;
            public string app;      // Application.identifier, so a save can't load into another game
            public string version;  // app version that wrote it
            public string data;
        }

        static IOGCloudData game;
        static bool readOnce, loading, dirty, uploading;
        static float uploadAt;

        public static bool Enabled => game != null;

        /// <summary>Raised after a cloud save replaced the local profile.</summary>
        public static event Action Restored;

        public static void Register(IOGCloudData data)
        {
            game = data;
            if (OGPlatform.IsSignedIn) OnSignedIn();
        }

        /// <summary>Call after every local save.</summary>
        public static void MarkDirty()
        {
            PlayerPrefs.SetString("og.cloud.localSavedAt", Now().ToString());
            if (!dirty) uploadAt = Time.realtimeSinceStartup + UploadDelay;
            dirty = true;
        }

        internal static void OnSignedIn()
        {
            if (game == null || loading) return;
            loading = true;
            OGPlatform.CloudLoad(Slot, (ok, text) =>
            {
                loading = false;
                if (!ok) return; // try again on the next sign-in; never upload over a copy we couldn't read
                readOnce = true;
                Reconcile(text);
            });
        }

        static void Reconcile(string text)
        {
            Envelope cloud = null;
            if (!string.IsNullOrEmpty(text))
            {
                try { cloud = JsonUtility.FromJson<Envelope>(text); }
                catch (Exception e) { Debug.LogWarning("[OGPlatform] cloud save unreadable: " + e.Message); }
            }
            if (cloud == null || string.IsNullOrEmpty(cloud.data) || (cloud.app != null && cloud.app != Application.identifier))
            {
                if (!game.LocalIsEmpty) { dirty = true; PushNow(); }
                return;
            }

            string local = game.Export();
            long localProgress = game.LocalIsEmpty ? -1 : game.Progress(local);
            long.TryParse(PlayerPrefs.GetString("og.cloud.localSavedAt", "0"), out long localAt);
            bool takeCloud = game.LocalIsEmpty || cloud.progress > localProgress || (cloud.progress == localProgress && cloud.savedAt > localAt);

            if (takeCloud)
            {
                if (local == cloud.data) return;
                if (!game.LocalIsEmpty) PlayerPrefs.SetString("og.cloud.backup", local);
                try
                {
                    game.Import(cloud.data);
                    PlayerPrefs.SetString("og.cloud.localSavedAt", cloud.savedAt.ToString());
                    PlayerPrefs.Save();
                    dirty = false;
                    Debug.Log("[OGPlatform] profile restored from the cloud (progress " + cloud.progress + ")");
                    Restored?.Invoke();
                }
                catch (Exception e) { Debug.LogWarning("[OGPlatform] couldn't load the cloud save: " + e.Message); }
            }
            else if (local != cloud.data) { dirty = true; PushNow(); }
        }

        internal static void Tick()
        {
            if (dirty && !uploading && Time.realtimeSinceStartup >= uploadAt) PushNow();
        }

        /// <summary>Uploads now if there is anything new (called automatically when the app is backgrounded).</summary>
        public static void PushNow()
        {
            if (game == null || !dirty || uploading || !readOnce || !OGPlatform.IsSignedIn) return;
            string data;
            try { data = game.Export(); }
            catch (Exception e) { Debug.LogWarning("[OGPlatform] export failed: " + e.Message); return; }
            if (game.LocalIsEmpty) { dirty = false; return; }
            var env = new Envelope
            {
                savedAt = Now(),
                progress = game.Progress(data),
                app = Application.identifier,
                version = Application.version,
                data = data,
            };
            dirty = false;
            uploading = true;
            OGPlatform.CloudSave(Slot, JsonUtility.ToJson(env), Application.productName + " profile", ok =>
            {
                uploading = false;
                if (!ok) { dirty = true; uploadAt = Time.realtimeSinceStartup + 120f; } // retry in a couple of minutes
            });
        }

        static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
