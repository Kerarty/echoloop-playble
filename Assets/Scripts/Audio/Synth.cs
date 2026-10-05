using UnityEngine;

namespace Playble.EchoLoop
{
    public enum WaveShape
    {
        Sine,
        Triangle,
        Square,
        Saw,
        Noise,
    }

    /// <summary>
    /// Крошечный синтезатор: генерирует короткие звуки прямо в памяти.
    /// В проекте нет ни одного бинарного аудиофайла — весь звук выводится
    /// из кода, что делает репозиторий лёгким и не зависит от лицензий.
    /// </summary>
    public static class Synth
    {
        public const int SampleRate = 44100;

        public static AudioClip Make(
            string name,
            WaveShape shape,
            float frequencyStart,
            float frequencyEnd,
            float duration,
            float attack = 0.005f,
            float release = 0.12f,
            float volume = 0.35f,
            float vibrato = 0f)
        {
            int samples = Mathf.Max(16, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[samples];

            float phase = 0f;
            uint noiseState = 22222u;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float time = i / (float)SampleRate;

                // Глиссандо по экспоненте: так переход звучит естественнее линейного.
                float freq = Mathf.Lerp(frequencyStart, frequencyEnd, t * t);
                if (vibrato > 0f) freq *= 1f + Mathf.Sin(time * 38f) * vibrato;

                phase += freq / SampleRate;
                if (phase >= 1f) phase -= 1f;

                float sample;
                switch (shape)
                {
                    case WaveShape.Square:
                        sample = phase < 0.5f ? 1f : -1f;
                        break;
                    case WaveShape.Saw:
                        sample = phase * 2f - 1f;
                        break;
                    case WaveShape.Triangle:
                        sample = 1f - 4f * Mathf.Abs(Mathf.Round(phase) - phase);
                        break;
                    case WaveShape.Noise:
                        noiseState ^= noiseState << 13;
                        noiseState ^= noiseState >> 17;
                        noiseState ^= noiseState << 5;
                        sample = (noiseState / (float)uint.MaxValue) * 2f - 1f;
                        break;
                    default:
                        sample = Mathf.Sin(phase * Mathf.PI * 2f);
                        break;
                }

                float env = Envelope(t, attack, release);
                data[i] = sample * env * volume;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Envelope(float t, float attack, float release)
        {
            float a = Mathf.Max(attack, 0.0005f);
            float r = Mathf.Clamp01(release);
            if (t < a) return t / a;
            float tail = Mathf.Clamp01((1f - t) / Mathf.Max(r, 0.0005f));
            return tail;
        }

        /// <summary>Простой аккорд из нескольких нот — используется для финального экрана.</summary>
        public static AudioClip Chord(float[] frequencies, float duration, float volume = 0.22f)
        {
            int samples = Mathf.RoundToInt(duration * SampleRate);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float env = Mathf.Min(1f, t / 0.06f) * Mathf.Clamp01((1f - t) / 0.55f);
                float sum = 0f;

                for (int n = 0; n < frequencies.Length; n++)
                {
                    // Небольшая расстройка между нотами даёт «хор» без хоруса.
                    sum += Mathf.Sin(2f * Mathf.PI * frequencies[n] * i / SampleRate + n * 0.7f) * 0.9f;
                    sum += Mathf.Sin(2f * Mathf.PI * frequencies[n] * 1.004f * i / SampleRate) * 0.5f;
                }

                data[i] = sum / frequencies.Length * env * volume;
            }

            var clip = AudioClip.Create("Chord", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Фоновый дрон: две расстроенные пилы под тихим фильтрованным шумом.</summary>
        public static AudioClip Drone(float duration, float root = 55f)
        {
            int samples = Mathf.RoundToInt(duration * SampleRate);
            var data = new float[samples];

            float p1 = 0f, p2 = 0f, p3 = 0f;
            uint noiseState = 7777u;
            float lowPass = 0f;

            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)SampleRate;

                p1 += root / SampleRate;
                p2 += root * 1.5f / SampleRate;
                p3 += root * 0.5f / SampleRate;
                if (p1 >= 1f) p1 -= 1f;
                if (p2 >= 1f) p2 -= 1f;
                if (p3 >= 1f) p3 -= 1f;

                noiseState ^= noiseState << 13;
                noiseState ^= noiseState >> 17;
                noiseState ^= noiseState << 5;
                float noise = (noiseState / (float)uint.MaxValue) * 2f - 1f;
                lowPass = Mathf.Lerp(lowPass, noise, 0.02f);

                float swell = 0.6f + 0.4f * Mathf.Sin(time * 0.45f);
                float tone = Mathf.Sin(p1 * Mathf.PI * 2f) * 0.5f
                           + Mathf.Sin(p2 * Mathf.PI * 2f) * 0.22f
                           + Mathf.Sin(p3 * Mathf.PI * 2f) * 0.35f;

                // Кольцевая синусоида у краёв — петля стыкуется без щелчка.
                float fade = Mathf.Min(1f, i / (SampleRate * 0.25f)) * Mathf.Min(1f, (samples - i) / (SampleRate * 0.25f));
                data[i] = (tone + lowPass * 0.5f) * swell * 0.1f * fade;
            }

            var clip = AudioClip.Create("Drone", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}