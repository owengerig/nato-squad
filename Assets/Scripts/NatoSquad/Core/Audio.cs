using System.Collections.Generic;
using UnityEngine;

namespace NatoSquad
{
    public enum Sfx { Shot, Hit, Explosion, BigExplosion, Pickup, Coin, PowerUp, Special, Missile, Warning, Click, Buy, Hurt, Victory, Fail, Crate }

    /// <summary>
    /// All sound is synthesized at startup (no audio files): short effects plus two looping music tracks
    /// (menu and battle) built from simple oscillators.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const int Rate = 22050;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource sfxSource, musicSource;
        AudioClip menuMusic, battleMusic, bossMusic;
        float sfxVol = 0.8f, musicVol = 0.5f;

        public void Init()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            clips[Sfx.Shot] = Make("shot", 0.06f, (t, i) => Noise(i) * 0.25f * Env(t, 0.06f) + Square(t, 900 - t * 6000) * 0.12f * Env(t, 0.05f));
            clips[Sfx.Hit] = Make("hit", 0.08f, (t, i) => Noise(i) * 0.4f * Env(t, 0.08f));
            clips[Sfx.Explosion] = Make("expl", 0.45f, (t, i) => LowNoise(i, 0.25f) * Env(t, 0.45f) * 0.9f);
            clips[Sfx.BigExplosion] = Make("bigexpl", 1.2f, (t, i) => (LowNoise(i, 0.12f) * 1.1f + Sine(t, 45 - t * 20) * 0.4f) * Env(t, 1.2f));
            clips[Sfx.Pickup] = Make("pickup", 0.18f, (t, i) => Square(t, 600 + t * 3000) * 0.18f * Env(t, 0.18f));
            clips[Sfx.Coin] = Make("coin", 0.14f, (t, i) => Square(t, t < 0.05f ? 1300 : 1750) * 0.14f * Env(t, 0.14f));
            clips[Sfx.PowerUp] = Make("pow", 0.4f, (t, i) => Square(t, 300 + 200 * Mathf.Floor(t * 20)) * 0.16f * Env(t, 0.4f));
            clips[Sfx.Special] = Make("special", 0.35f, (t, i) => (Noise(i) * 0.3f + Saw(t, 200 - t * 300) * 0.2f) * Env(t, 0.35f));
            clips[Sfx.Missile] = Make("missile", 0.3f, (t, i) => LowNoise(i, 0.5f) * 0.4f * Env(t, 0.3f));
            clips[Sfx.Warning] = Make("warn", 1.6f, (t, i) => Square(t, (Mathf.Floor(t * 2.5f) % 2 == 0) ? 520 : 390) * 0.18f * (t < 1.5f ? 1f : (1.6f - t) * 10f));
            clips[Sfx.Click] = Make("click", 0.05f, (t, i) => Square(t, 1100) * 0.12f * Env(t, 0.05f));
            clips[Sfx.Buy] = Make("buy", 0.3f, (t, i) => Square(t, t < 0.1f ? 880 : t < 0.2f ? 1100 : 1320) * 0.14f * Env(t, 0.3f));
            clips[Sfx.Hurt] = Make("hurt", 0.25f, (t, i) => (Saw(t, 160 - t * 200) * 0.3f + Noise(i) * 0.2f) * Env(t, 0.25f));
            clips[Sfx.Victory] = Make("victory", 1.4f, (t, i) => Square(t, Note(Arp(t, new[] { 0, 4, 7, 12, 7, 12, 16, 19 }, 0.15f) + 60)) * 0.13f * (t < 1.2f ? 1f : (1.4f - t) * 5f));
            clips[Sfx.Fail] = Make("fail", 1.2f, (t, i) => Saw(t, Note(Arp(t, new[] { 7, 6, 5, 4, 3, 0 }, 0.18f) + 48)) * 0.14f * (t < 1f ? 1f : (1.2f - t) * 5f));
            clips[Sfx.Crate] = Make("crate", 0.7f, (t, i) => (Square(t, 400 + t * 900) * 0.12f + Noise(i) * 0.08f) * Env(t, 0.7f));

            menuMusic = MakeMusic("menu", 96f, new[] { 57, 53, 48, 55 }, false);
            battleMusic = MakeMusic("battle", 138f, new[] { 52, 52, 48, 50 }, true);
            bossMusic = MakeMusic("boss", 150f, new[] { 50, 51, 50, 46 }, true);
        }

        public void Apply(GameSettings s)
        {
            sfxVol = s.soundVolume;
            musicVol = s.musicVolume;
            if (musicSource != null) musicSource.volume = musicVol * 0.55f;
        }

        public void Play(Sfx s, float vol = 1f, float pitchJitter = 0.06f)
        {
            if (sfxSource == null || sfxVol <= 0f) return;
            // the same sound at most every 40 ms (e.g. dozens of shots per second)
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(s, out var t) && now - t < 0.04f) return;
            lastPlayed[s] = now;
            sfxSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            sfxSource.PlayOneShot(clips[s], vol * sfxVol);
        }

        public enum Track { None, Menu, Battle, Boss }
        Track current = Track.None;

        public void PlayMusic(Track t)
        {
            if (musicSource == null || t == current) return;
            current = t;
            musicSource.clip = t == Track.Menu ? menuMusic : t == Track.Battle ? battleMusic : t == Track.Boss ? bossMusic : null;
            musicSource.volume = musicVol * 0.55f;
            if (musicSource.clip != null) musicSource.Play(); else musicSource.Stop();
        }

        // ------------------------------------------------------------------ synthesis
        delegate float Gen(float t, int i);

        static AudioClip Make(string name, float seconds, Gen g)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(g(i / (float)Rate, i), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static uint seed = 1234567;
        static float Noise(int i) { seed = seed * 1664525u + 1013904223u; return ((seed >> 9) / 4194304f) * 2f - 1f; }
        static float lp;
        static float LowNoise(int i, float k) { if (i == 0) lp = 0; lp += (Noise(i) - lp) * k; return lp * 2f; }
        static float Env(float t, float len) { float x = Mathf.Clamp01(t / len); return (1f - x) * (1f - x); }
        static float Sine(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Square(float t, float f) => Mathf.Repeat(f * t, 1f) < 0.5f ? 1f : -1f;
        static float Saw(float t, float f) => Mathf.Repeat(f * t, 1f) * 2f - 1f;
        static float Note(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        static int Arp(float t, int[] steps, float step) => steps[Mathf.Min(steps.Length - 1, (int)(t / step))];

        /// <summary>A looping 8-bar track: bass on the root, an arpeggio on a minor triad, and a noise hi-hat/kick.</summary>
        static AudioClip MakeMusic(string name, float bpm, int[] roots, bool driving)
        {
            float beat = 60f / bpm;
            int bars = 8;
            float seconds = beat * 4 * bars;
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            int[] chord = { 0, 3, 7, 12, 7, 3, 10, 7 };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int beatIndex = (int)(t / beat);
                int bar = beatIndex / 4;
                int root = roots[bar % roots.Length];
                float inBeat = (t % beat) / beat;
                // bass: eighth notes
                float eighth = (t % (beat * 0.5f)) / (beat * 0.5f);
                float bass = Square(t, Note(root - 12)) * 0.16f * (1f - eighth * 0.7f);
                // arpeggio: sixteenths
                int six = (int)(t / (beat * 0.25f));
                float sixT = (t % (beat * 0.25f)) / (beat * 0.25f);
                float arp = Square(t, Note(root + 12 + chord[six % chord.Length])) * (driving ? 0.06f : 0.045f) * (1f - sixT);
                // lead (driving tracks): a slow melody on the chord tones, every two beats
                float lead = 0f;
                if (driving)
                {
                    int[] mel = { 7, 12, 10, 7, 3, 5, 7, 0 };
                    int mi = (beatIndex / 2) % mel.Length;
                    float lt = (t % (beat * 2)) / (beat * 2);
                    lead = Saw(t, Note(root + 24 + mel[mi])) * 0.035f * (1f - lt * 0.6f);
                }
                else
                {
                    lead = Sine(t, Note(root + 24 + chord[(beatIndex) % 4])) * 0.05f * (1f - inBeat);
                }
                // drums
                float kick = inBeat < 0.18f ? Sine(t, 70f - inBeat * 200f) * 0.35f * (1f - inBeat / 0.18f) : 0f;
                if (!driving && beatIndex % 2 == 1) kick = 0f;
                float hat = (eighth < 0.1f) ? Noise(i) * 0.05f * (1f - eighth / 0.1f) : 0f;
                float snare = (driving && beatIndex % 2 == 1 && inBeat < 0.2f) ? Noise(i) * 0.12f * (1f - inBeat / 0.2f) : 0f;
                data[i] = Mathf.Clamp(bass + arp + lead + kick + hat + snare, -1f, 1f) * 0.8f;
            }
            // tiny fades at the loop point to avoid a click
            for (int i = 0; i < 200; i++) { float k = i / 200f; data[i] *= k; data[n - 1 - i] *= k; }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
