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
        private const int Polyphony = 10;
        private readonly Dictionary<string, AudioClip> clips = new();
        private AudioSource[] sources;
        private int nextSource;

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
            sources = new AudioSource[Polyphony];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].volume = 0.82f;
            }
            clips["start"] = Make("start", 0.7f, StartWave);
            clips["choose"] = Make("choose", 0.18f, ChooseWave);
            clips["count"] = Make("count", 0.09f, CountWave);
            clips["pon"] = Make("pon", 0.12f, ImpactWave);
            clips["win"] = Make("win", 1.25f, WinWave);
            clips["lose"] = Make("lose", 0.9f, LoseWave);
            clips["draw"] = Make("draw", 0.75f, DrawWave);
            clips["tap"] = Make("tap", 0.16f, TapWave);
            clips["suspense"] = Make("suspense", 1.8f, SuspenseWave);
            clips["reveal"] = Make("reveal", 0.72f, RevealWave);
            clips["timeup"] = Make("timeup", 0.48f, TimeUpWave);
            clips["voice_rock"] = Make("voice_rock", 0.62f, RockVoiceWave);
            clips["voice_scissors"] = Make("voice_scissors", 0.74f, ScissorsVoiceWave);
            clips["voice_paper"] = Make("voice_paper", 0.64f, PaperVoiceWave);
#endif
        }

        public void PlayHand(int hand, float volume = 1f)
        {
            Play(hand == 0 ? "voice_rock" : hand == 1 ? "voice_scissors" : "voice_paper", volume);
        }

        public void Play(string id, float volume = 1f)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JankenWebAudio_Play(SoundId(id), Mathf.Clamp01(volume));
#else
            if (sources != null && clips.TryGetValue(id, out AudioClip clip))
            {
                AudioSource voice = sources[nextSource];
                nextSource = (nextSource + 1) % sources.Length;
                voice.PlayOneShot(clip, volume);
            }
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
            "voice_rock" => 11,
            "voice_scissors" => 12,
            "voice_paper" => 13,
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
        private static float Square(float hz, float t) => Sine(hz, t) >= 0f ? 1f : -1f;
        private static float Saw(float hz, float t) => 2f * (t * hz - Mathf.Floor(.5f + t * hz));
        private static float Noise(float t) => Mathf.PerlinNoise(t * 9701f, 0.37f) * 2f - 1f;

        private static float Adsr(float t, float duration, float attack, float decay, float sustain, float release)
        {
            if (t < 0f || t >= duration) return 0f;
            if (t < attack) return t / attack;
            if (t < attack + decay) return Mathf.Lerp(1f, sustain, (t - attack) / decay);
            if (t < duration - release) return sustain;
            return sustain * Mathf.Clamp01((duration - t) / release);
        }

        private static float StartWave(float t)
        {
            float rise = 280f + t * 900f;
            return (Sine(rise, t) * 0.55f + Sine(rise * 2f, t) * 0.18f + Noise(t) * 0.08f) * Env(t, 0.015f, 3.2f);
        }

        private static float ChooseWave(float t) => (Sine(660f, t) + Sine(990f, t) * 0.35f) * Env(t, 0.004f, 18f) * 0.55f;

        private static float CountWave(float t)
        {
            float body = Square(Mathf.Lerp(1760f, 1160f, t / .09f), t) * Env(t, .001f, 48f);
            float click = Noise(t) * Env(t, .0005f, 70f);
            return body * .28f + click * .18f;
        }

        private static float ImpactWave(float t)
        {
            float body = Square(Mathf.Lerp(1320f, 720f, t / .12f), t) * Env(t, .001f, 38f);
            float click = Noise(t + .19f) * Env(t, .0005f, 58f);
            return body * .38f + click * .22f;
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

        private static float Vowel(float t, float fundamental, float formant1, float formant2)
        {
            float vibrato = 1f + Mathf.Sin(t * Mathf.PI * 11f) * .012f;
            return Sine(fundamental * vibrato, t) * .42f
                + Sine(formant1, t) * .18f
                + Sine(formant2, t) * .09f;
        }

        // 「グー」: 濁音の有声立ち上がり + /u/ を二拍ぶん長く保つ。
        private static float RockVoiceWave(float t)
        {
            float onset = t < .075f ? (Sine(118f, t) * .28f + Noise(t) * .10f) * Env(t, .004f, 22f) : 0f;
            float local = Mathf.Max(0f, t - .045f);
            float longU = Vowel(local, 154f, 360f, 880f) * Env(local, .025f, 2.0f);
            float lowC = (Square(130.81f, local) * .72f + Sine(261.62f, local) * .18f)
                * Adsr(local, .56f, .008f, .07f, .62f, .14f);
            return onset * .65f + longU * .38f + lowC * .46f;
        }

        // 「チョキ」: /ch/ の摩擦、拗音 /yo/ への滑り、閉鎖後の短い /ki/。
        private static float ScissorsVoiceWave(float t)
        {
            float affricate = t < .105f ? Noise(t) * Env(t, .003f, 17f) * .34f : 0f;
            float choLocal = Mathf.Max(0f, t - .075f);
            float glide = Mathf.Clamp01(choLocal / .12f);
            float cho = t < .43f ? Vowel(choLocal, 178f, Mathf.Lerp(1250f, 520f, glide), Mathf.Lerp(2200f, 980f, glide)) * Env(choLocal, .018f, 3.1f) : 0f;
            float kBurst = t > .43f && t < .49f ? Noise(t + .31f) * Env(t - .43f, .002f, 34f) * .28f : 0f;
            float kiLocal = Mathf.Max(0f, t - .47f);
            float ki = t > .47f ? Vowel(kiLocal, 192f, 310f, 2250f) * Env(kiLocal, .012f, 7f) * .72f : 0f;
            float noteLocal = Mathf.Max(0f, t - .055f);
            float noteProgress = Mathf.Clamp01(noteLocal / .62f);
            float gPitch = noteProgress < .5f
                ? Mathf.Lerp(196f, 164.81f, noteProgress * 2f)
                : Mathf.Lerp(164.81f, 196f, (noteProgress - .5f) * 2f);
            float movingG = Saw(gPitch, noteLocal) * Adsr(noteLocal, .62f, .01f, .09f, .58f, .13f);
            return affricate * .6f + cho * .30f + kBurst * .65f + ki * .28f + movingG * .48f;
        }

        // 「パー」: 半濁音 /p/ の無声破裂 + /a/ を二拍ぶん伸ばす。
        private static float PaperVoiceWave(float t)
        {
            float burst = t < .055f ? Noise(t + .67f) * Env(t, .001f, 38f) * .48f : 0f;
            float local = Mathf.Max(0f, t - .045f);
            float longA = Vowel(local, 168f, 760f, 1220f) * Env(local, .018f, 2.05f);
            float highC = Sine(261.63f, local) * Adsr(local, .56f, .014f, .08f, .7f, .16f);
            return burst * .65f + longA * .34f + highC * .62f;
        }
    }
}
