using UnityEngine;

namespace TetrisGame
{
    public sealed class TetrisAudio : MonoBehaviour
    {
        AudioSource source;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        public void PlayMove()
        {
            PlayTone(420f, 0.035f, 0.08f);
        }

        public void PlayRotate()
        {
            PlayTone(560f, 0.045f, 0.1f);
        }

        public void PlayDrop()
        {
            PlayTone(180f, 0.09f, 0.16f);
        }

        public void PlayClear(int lines)
        {
            PlayTone(620f + lines * 70f, 0.16f, 0.2f);
        }

        public void PlayGameOver()
        {
            PlayTone(140f, 0.45f, 0.22f);
        }

        void PlayTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)samples;
                data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t)) * volume * envelope;
            }

            clip.SetData(data, 0);
            source.PlayOneShot(clip);
        }
    }
}
