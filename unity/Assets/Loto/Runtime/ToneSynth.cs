using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The app's sounds, generated in code (no audio assets in the repo). Every clip is mono, at most 0.4 of
    /// full scale, and fades in over 5 ms and out over the last 30 ms so it never clicks. Hum is the exception:
    /// it loops, so it is exactly 1 s of whole cycles and has no fade.
    /// </summary>
    public static class ToneSynth
    {
        static int Rate { get { int r = AudioSettings.outputSampleRate; return r > 0 ? r : 48000; } }

        /// <summary>The machine running: 100 Hz with two harmonics, 1 s, loops without a click.</summary>
        public static AudioClip Hum()
        {
            int rate = Rate;
            var data = new float[rate];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                data[i] = 0.22f * Mathf.Sin(2f * Mathf.PI * 100f * t)
                        + 0.11f * Mathf.Sin(2f * Mathf.PI * 200f * t)
                        + 0.05f * Mathf.Sin(2f * Mathf.PI * 300f * t);
            }
            return Make("Hum", data);
        }

        /// <summary>PURGAR: air or oil escaping, a soft noise, 1 s that loops (no fade: noise hides the seam).</summary>
        public static AudioClip Hiss()
        {
            int rate = Rate;
            var data = new float[rate];
            var random = new System.Random(11);
            float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)random.NextDouble() * 2f - 1f;
                low += (white - low) * 0.35f;              // a little low-pass: a hiss, not a crackle
                data[i] = (white - low * 0.5f) * 0.22f;
            }
            return Make("Hiss", data);
        }

        /// <summary>A correct step: a short high ping.</summary>
        public static AudioClip Ping()
        {
            return Make("Ping", Fill(0.10f, (t, i) => Mathf.Sin(2f * Mathf.PI * 1320f * t) * Mathf.Exp(-t * 30f) * 0.4f));
        }

        /// <summary>An error that lets the student go on: two square pulses, 660 Hz then 520 Hz.</summary>
        public static AudioClip Warn()
        {
            const float pulse = 0.12f, gap = 0.06f;
            return Make("Warn", Fill(pulse * 2f + gap, (t, i) =>
            {
                if (t < pulse) return Square(660f, t) * 0.25f * Edge(t, pulse);
                if (t < pulse + gap) return 0f;
                float u = t - pulse - gap;
                return Square(520f, u) * 0.25f * Edge(u, pulse);
            }));
        }

        /// <summary>The start-up test of the verify step: a dry clack and nothing starts.</summary>
        public static AudioClip Clac()
        {
            var random = new System.Random(7);
            return Make("Clac", Fill(0.04f, (t, i) => ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.4f));
        }

        /// <summary>The simulation stopped on an error: a low buzzer.</summary>
        public static AudioClip Buzz()
        {
            return Make("Buzz", Fill(0.6f, (t, i) => Square(160f, t) * 0.3f));
        }

        /// <summary>The sequence completed: three rising notes.</summary>
        public static AudioClip Chime()
        {
            float[] notes = { 523f, 659f, 784f };
            const float each = 0.13f;
            return Make("Chime", Fill(each * notes.Length, (t, i) =>
            {
                int n = Mathf.Min(notes.Length - 1, (int)(t / each));
                float u = t - n * each;
                return Mathf.Sin(2f * Mathf.PI * notes[n] * u) * Mathf.Exp(-u * 8f) * 0.4f * Edge(u, each);
            }));
        }

        // ---------------------------------------------------------------- helpers

        delegate float Sample(float t, int i);

        static float[] Fill(float seconds, Sample sample)
        {
            int rate = Rate;
            var data = new float[Mathf.Max(1, Mathf.RoundToInt(seconds * rate))];
            for (int i = 0; i < data.Length; i++) data[i] = sample((float)i / rate, i);
            ApplyEnvelope(data, rate);
            return data;
        }

        /// <summary>5 ms fade in and 30 ms fade out to zero, so the clip starts and ends silent.</summary>
        static void ApplyEnvelope(float[] data, int rate)
        {
            int fadeIn = Mathf.Min(data.Length, rate / 200);
            int fadeOut = Mathf.Min(data.Length, rate * 30 / 1000);
            for (int i = 0; i < fadeIn; i++) data[i] *= (float)i / fadeIn;
            for (int i = 0; i < fadeOut; i++) data[data.Length - 1 - i] *= (float)i / fadeOut;
        }

        /// <summary>A short fade at both ends of a piece inside a clip (pulses, notes).</summary>
        static float Edge(float t, float length)
        {
            const float ramp = 0.005f;
            return Mathf.Clamp01(Mathf.Min(t / ramp, (length - t) / ramp));
        }

        static float Square(float frequency, float t)
        {
            return Mathf.Sin(2f * Mathf.PI * frequency * t) >= 0f ? 1f : -1f;
        }

        static AudioClip Make(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create("Loto" + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
