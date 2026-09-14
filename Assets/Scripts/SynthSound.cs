using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Janken
{
    /// <summary>数式でPCM波形を作る、小さな効果音シンセサイザー。</summary>
    public sealed class SynthSound : MonoBehaviour
    {
        private const int Rate = 44100;
        private readonly Dictionary<string, AudioClip> clips = new();
        private AudioSource source;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JankenWebAudio_Init();

        [DllImport("__Internal")]
        private static extern void JankenWebAudio_Play(int soundId, float volume);
#endif

        public void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 最初のpointerdownをブラウザー側で捕捉し、AudioContextを確実に解除する。
            JankenWebAudio_Init();
#else
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.82f;
            clips["start"] = Make("start", 0.7f, StartWave);
            clips["choose"] = Make("choose", 0.18f, ChooseWave);
            clips["count"] = Make("count", 0.23f, CountWave);
            clips["pon"] = Make("pon", 0.65f, ImpactWave);
            clips["win"] = Make("win", 1.25f, WinWave);
            clips["lose"] = Make("lose", 0.9f, LoseWave);
            clips["draw"] = Make("draw", 0.75f, DrawWave);
            clips["tap"] = Make("tap", 0.16f, TapWave);
            clips["suspense"] = Make("suspense", 1.8f, SuspenseWave);
            clips["reveal"] = Make("reveal", 0.72f, RevealWave);
            clips["timeup"] = Make("timeup", 0.48f, TimeUpWave);
#endif
        }

        public void Play(string id, float volume = 1f)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JankenWebAudio_Play(SoundId(id), Mathf.Clamp01(volume));
#else
            if (source != null && clips.TryGetValue(id, out AudioClip clip)) source.PlayOneShot(clip, volume);
#endif
        }

        private static int SoundId(string id) => id switch
        {
            "start" => 0,
            "choose" => 1,
            "count" => 2,
            "pon" => 3,
            "win" => 4,
            "lose" => 5,
            "draw" => 6,
            "tap" => 7,
            "suspense" => 8,
            "reveal" => 9,
            "timeup" => 10,
            _ => 1
        };

        private static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            float[] data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            AudioClip clip = AudioClip.Create("Synth_" + name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Env(float t, float attack, float decay) => Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay);
        private static float Sine(float hz, float t) => Mathf.Sin(Mathf.PI * 2f * hz * t);
        private static float Noise(float t) => Mathf.PerlinNoise(t * 9701f, 0.37f) * 2f - 1f;

        private static float StartWave(float t)
        {
            float rise = 280f + t * 900f;
            return (Sine(rise, t) * 0.55f + Sine(rise * 2f, t) * 0.18f + Noise(t) * 0.08f) * Env(t, 0.015f, 3.2f);
        }

        private static float ChooseWave(float t) => (Sine(660f, t) + Sine(990f, t) * 0.35f) * Env(t, 0.004f, 18f) * 0.55f;

        private static float CountWave(float t)
        {
            float kick = Sine(105f - t * 180f, t) * Env(t, 0.002f, 16f);
            float click = Noise(t) * Env(t, 0.001f, 28f);
            return kick * 0.72f + click * 0.16f;
        }

        private static float ImpactWave(float t)
        {
            float kick = Sine(125f * Mathf.Exp(-t * 7f) + 42f, t) * Env(t, 0.001f, 9f);
            float crash = Noise(t) * Env(t, 0.002f, 5.5f);
            float laser = Sine(980f - t * 1100f, t) * Env(t, 0.003f, 7f);
            return kick * 0.75f + crash * 0.25f + laser * 0.2f;
        }

        private static float WinWave(float t)
        {
            int step = Mathf.Min(4, Mathf.FloorToInt(t / 0.18f));
            float local = t - step * 0.18f;
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            float bell = (Sine(notes[step], t) + Sine(notes[step] * 2f, t) * 0.24f) * Env(local, 0.008f, 4f);
            float sparkle = Noise(t) * Mathf.Max(0f, t - 0.65f) * Env(Mathf.Max(0f, t - 0.65f), 0.01f, 4f) * 0.12f;
            return bell * 0.5f + sparkle;
        }

        private static float LoseWave(float t)
        {
            float hz = Mathf.Lerp(330f, 92f, t / 0.9f);
            return (Sine(hz, t) * 0.6f + Sine(hz * 0.5f, t) * 0.25f) * Env(t, 0.01f, 2.6f);
        }

        private static float DrawWave(float t)
        {
            float wobble = 220f + Sine(7f, t) * 55f;
            return (Sine(wobble, t) * 0.55f + Sine(wobble * 1.5f, t) * 0.18f) * Env(t, 0.006f, 3.5f);
        }

        private static float TapWave(float t)
        {
            float snap = Noise(t) * Env(t, .001f, 36f) * .22f;
            return (Sine(720f, t) + Sine(1080f, t) * .34f) * Env(t, .003f, 23f) * .42f + snap;
        }

        private static float SuspenseWave(float t)
        {
            float pulseTime = t % .28f;
            float pulse = Sine(72f, pulseTime) * Env(pulseTime, .004f, 15f);
            float rising = Sine(170f + t * 210f, t) * Env(t, .04f, .45f) * .17f;
            return pulse * (.32f + t * .17f) + rising;
        }

        private static float RevealWave(float t)
        {
            float boom = Sine(145f * Mathf.Exp(-t * 6f) + 38f, t) * Env(t, .001f, 6f);
            float crash = Noise(t) * Env(t, .001f, 4.8f);
            float shine = Sine(1250f, t) * Env(t, .004f, 7f);
            return boom * .78f + crash * .28f + shine * .16f;
        }

        private static float TimeUpWave(float t)
        {
            return (Sine(520f - t * 500f, t) * .55f + Sine(260f - t * 180f, t) * .35f) * Env(t, .004f, 5f);
        }
    }
}
