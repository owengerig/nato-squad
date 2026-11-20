using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NatoSquad
{
    /// <summary>A full-screen UI page. Subclasses build their content in Build().</summary>
    public abstract class GameScreen : MonoBehaviour
    {
        protected RectTransform Root => (RectTransform)transform;
        protected GameManager G => GameManager.I;
        public abstract void Build();
        /// <summary>Back button / Escape on this screen.</summary>
        public virtual void OnBack() { }
        /// <summary>When true, the screen draws its own background (the battle shows the game world behind the UI).</summary>
        public virtual bool Transparent => false;
    }

    /// <summary>
    /// Boots automatically when Play is pressed: camera, event system, canvas, audio, save data, store, then the main menu.
    /// The whole game is built from code; the scene only holds a camera.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public SaveData Save;
        public GameSettings Settings;
        public GameAudio Audio;
        public IAPManager Store;
        public Camera Cam;

        // battle hand-off
        [NonSerialized] public int PendingMission;
        [NonSerialized] public BattleResult LastResult;

        Canvas canvas;
        RectTransform screenRoot, overlayRoot;
        Image background, fader;
        GameScreen current;
        GameObject dialog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            new GameObject("NATO Squad").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Save = SaveSystem.Load();
            Settings = SaveSystem.LoadSettings();

            SetupCamera();
            SetupEventSystem();
            SetupCanvas();

            Audio = gameObject.AddComponent<GameAudio>();
            Audio.Init();
            Audio.Apply(Settings);

            Store = gameObject.AddComponent<IAPManager>();
            Store.Init(this);

            // Game Center / Google Play Games: sign-in, achievements, leaderboards, cloud copy of the save
            try { GameServices.Init(); }
            catch (Exception e) { Debug.LogError("[NATO Squad] game services: " + e); }

            Show<MainMenuScreen>();
        }

        void SetupCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                Cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.transform.position = new Vector3(0, 0, -10);
            }
            Cam.orthographic = true;
            Cam.orthographicSize = 5f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Pal.Bg;
            DontDestroyOnLoad(Cam.gameObject);
        }

        void SetupEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
                DontDestroyOnLoad(go);
            }
            if (es.GetComponent<BaseInputModule>() == null)
            {
                var module = es.gameObject.AddComponent<InputSystemUIInputModule>();
                if (module.actionsAsset == null) module.AssignDefaultActions();
            }
        }

        void SetupCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();

            background = UI.Img(go.transform, "Background", Color.white, Art.Sky(new Color(0.06f, 0.10f, 0.20f), new Color(0.14f, 0.24f, 0.38f)));
            UI.Stretch(background.rectTransform);

            screenRoot = UI.Rect("Screens", go.transform);
            UI.Stretch(screenRoot);
            go.AddComponent<SafeAreaFitter>().target = screenRoot;
            overlayRoot = UI.Rect("Overlays", go.transform);
            UI.Stretch(overlayRoot);
            go.GetComponent<SafeAreaFitter>().extra = overlayRoot;

            fader = UI.Img(go.transform, "Fader", Color.black);
            UI.Stretch(fader.rectTransform, -50, -50, -50, -50);
            SetFade(0f);
        }

        // ------------------------------------------------------------------ settings & saving
        public void SaveSettings()
        {
            SaveSystem.SaveSettings(Settings);
            Audio.Apply(Settings);
        }

        public bool SaveGame() => SaveSystem.Save(Save);

        public void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Settings.vibration) Handheld.Vibrate();
#endif
        }

        // ------------------------------------------------------------------ screens
        const float FadeOut = 0.18f, FadeIn = 0.25f;
        Action pending;
        Coroutine fadeRoutine;

        public GameScreen Current => current;

        public void Show<T>() where T : GameScreen
        {
            pending = () => ShowNow<T>();
            if (current == null) SetFade(1f);
            if (fadeRoutine == null) fadeRoutine = StartCoroutine(FadeRoutine());
        }

        void ShowNow<T>() where T : GameScreen
        {
            CloseDialog();
            if (current != null) Destroy(current.gameObject);
            Time.timeScale = 1f;
            var rt = UI.Rect(typeof(T).Name, screenRoot);
            UI.Stretch(rt);
            var s = rt.gameObject.AddComponent<T>();
            current = s;
            background.enabled = !s.Transparent;
            s.Build();
        }

        void SetFade(float a)
        {
            fader.color = new Color(0, 0, 0, a);
            fader.raycastTarget = a > 0.01f;
            fader.enabled = a > 0.001f;
        }

        IEnumerator FadeRoutine()
        {
            while (pending != null)
            {
                for (float a = fader.color.a; a < 1f; a += Time.unscaledDeltaTime / FadeOut) { SetFade(a); yield return null; }
                SetFade(1f);
                var act = pending; pending = null;
                act();
                yield return null;
                for (float a = 1f; a > 0f && pending == null; a -= Time.unscaledDeltaTime / FadeIn) { SetFade(a); yield return null; }
                if (pending == null) SetFade(0f);
            }
            fadeRoutine = null;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (dialog != null) CloseDialog();
                else if (current != null) current.OnBack();
            }
        }

        // ------------------------------------------------------------------ dialogs & toasts
        public RectTransform Overlay => overlayRoot;
        public bool DialogOpen => dialog != null;

        /// <summary>Modal message with up to two buttons.</summary>
        public void Dialog(string title, string body, string ok = "OK", Action onOk = null, string cancel = null, Action onCancel = null)
        {
            CloseDialog();
            var dim = UI.Img(overlayRoot, "Dialog", new Color(0, 0, 0, 0.7f));
            dim.raycastTarget = true;
            UI.Stretch(dim.rectTransform);
            dialog = dim.gameObject;
            var panel = UI.Box(dim.transform, "Panel", Pal.Panel);
            UI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1100, 560), Vector2.zero);
            var t = UI.Label(panel.transform, title, 56, Pal.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 40, 0, 40, 40);
            var b = UI.Label(panel.transform, body, 36, Pal.TextDim, TextAnchor.MiddleCenter);
            UI.Stretch(b.rectTransform, 60, 140, 60, 120);
            if (cancel != null)
            {
                var c = UI.Button(panel.transform, cancel, 40, Pal.PanelLight, () => { CloseDialog(); onCancel?.Invoke(); });
                UI.Place((RectTransform)c.transform, new Vector2(0.5f, 0f), new Vector2(380, 100), new Vector2(-210, 40), new Vector2(0.5f, 0f));
                var o = UI.Button(panel.transform, ok, 40, Pal.Accent, () => { CloseDialog(); onOk?.Invoke(); });
                UI.Place((RectTransform)o.transform, new Vector2(0.5f, 0f), new Vector2(380, 100), new Vector2(210, 40), new Vector2(0.5f, 0f));
            }
            else
            {
                var o = UI.Button(panel.transform, ok, 40, Pal.Accent, () => { CloseDialog(); onOk?.Invoke(); });
                UI.Place((RectTransform)o.transform, new Vector2(0.5f, 0f), new Vector2(420, 100), new Vector2(0, 40), new Vector2(0.5f, 0f));
            }
        }

        public void CloseDialog()
        {
            if (dialog != null) Destroy(dialog);
            dialog = null;
        }

        /// <summary>Short message that fades out at the top of the screen.</summary>
        public void Toast(string msg, Color? color = null)
        {
            var box = UI.Box(overlayRoot, "Toast", new Color(0, 0, 0, 0.8f));
            UI.Place(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(1200, 100), new Vector2(0, -140), new Vector2(0.5f, 1f));
            var t = UI.Label(box.transform, msg, 38, color ?? Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Stretch(t.rectTransform, 20, 0, 20, 0);
            StartCoroutine(FadeToast(box.gameObject));
        }

        IEnumerator FadeToast(GameObject go)
        {
            var g = go.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false;
            for (float t = 0; t < 2.2f; t += Time.unscaledDeltaTime)
            {
                if (go == null) yield break;
                g.alpha = t < 1.6f ? 1f : 1f - (t - 1.6f) / 0.6f;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        // ------------------------------------------------------------------ economy helpers
        public bool Spend(long amount)
        {
            if (Save.gold < amount) { Audio.Play(Sfx.Fail, 0.4f); Toast("Not enough gold", Pal.Red); return false; }
            Save.gold -= amount;
            Audio.Play(Sfx.Buy);
            SaveGame();
            return true;
        }

        public float GoldMultiplier => (Save.doubleGold ? 2f : 1f);
    }

    /// <summary>Keeps screens inside the device safe area (notches, rounded corners, home indicator).</summary>
    public class SafeAreaFitter : MonoBehaviour
    {
        public RectTransform target, extra;
        Rect last;
        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == last || Screen.width == 0 || Screen.height == 0) return;
            last = sa;
            Apply(target, sa); Apply(extra, sa);
        }
        static void Apply(RectTransform t, Rect sa)
        {
            if (t == null) return;
            t.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            t.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            t.offsetMin = t.offsetMax = Vector2.zero;
        }
    }
}
