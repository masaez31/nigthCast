using UnityEngine;

namespace NightCast.Juice
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        // Valor seguro por defecto para evitar division por cero en el hilo de audio antes de Awake
        private float _sampleRate = 48000f;
        private float _phase = 0f;
        private double _dspTime = 0.0;

        public float TargetTension { get; set; }
        public bool IsFishRunning { get; set; }

        private void Awake()
        {
            int sr = AudioSettings.outputSampleRate;
            if (sr > 0) _sampleRate = sr;

            AudioSource source = GetComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.loop = true;
            source.playOnAwake = true;

            // Unity 6 necesita un AudioClip asignado para invocar OnAudioFilterRead
            source.clip = AudioClip.Create("Dummy", 44100, 1, 44100, false);

            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_sampleRate <= 0f) return;

            float clampedTension = TargetTension < 0f ? 0f : (TargetTension > 1f ? 1f : TargetTension);
            float volume = IsFishRunning ? (clampedTension * 0.18f) : 0f;
            if (volume <= 0.0001f) return;

            float frequency = 420f + (clampedTension * 1050f);
            float phaseIncrement = frequency / _sampleRate;
            double timeIncrement = 1.0 / _sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                _phase += phaseIncrement;
                if (_phase > 1f) _phase -= 1f;

                _dspTime += timeIncrement;

                // Onda diente de sierra modulada por pulsos para simular carraca de freno del carrete
                float lfo = System.Math.Sin(_dspTime * 42.0) > 0.0 ? 1f : 0f;
                float val = (_phase * 2f - 1f) * volume * lfo;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += val;
                }
            }
        }
    }
}
