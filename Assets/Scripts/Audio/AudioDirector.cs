using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Единая точка для звука: один AudioSource на короткие эффекты и отдельный
    /// на фоновый дрон. Все клипы синтезируются при старте.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioDirector : MonoBehaviour
    {
        AudioSource _sfx;
        AudioSource _ambient;

        AudioClip _jump, _shard, _loopReset, _plate, _gate, _death, _complete, _ui, _spawn;
        AudioClip _drone;

        public float MasterVolume = 1f;

        public void Initialize()
        {
            _sfx = BuildSource("Sfx", false);
            _ambient = BuildSource("Ambient", true);

            _jump = Synth.Make("jump", WaveShape.Triangle, 340f, 620f, 0.14f, 0.004f, 0.5f, 0.22f);
            _shard = Synth.Make("shard", WaveShape.Sine, 880f, 1760f, 0.28f, 0.003f, 0.7f, 0.24f);
            _spawn = Synth.Make("spawn", WaveShape.Saw, 180f, 420f, 0.22f, 0.01f, 0.6f, 0.14f);
            _loopReset = Synth.Make("loop", WaveShape.Triangle, 620f, 150f, 0.42f, 0.004f, 0.6f, 0.2f);
            _plate = Synth.Make("plate", WaveShape.Square, 180f, 150f, 0.08f, 0.002f, 0.7f, 0.16f);
            _gate = Synth.Make("gate", WaveShape.Saw, 90f, 260f, 0.3f, 0.01f, 0.6f, 0.16f, 0.05f);
            _death = Synth.Make("death", WaveShape.Noise, 420f, 60f, 0.5f, 0.002f, 0.8f, 0.26f);
            _ui = Synth.Make("ui", WaveShape.Square, 700f, 700f, 0.05f, 0.002f, 0.8f, 0.14f);
            _complete = Synth.Chord(new[] { 392f, 494f, 587f, 784f }, 1.4f, 0.18f);

            _drone = Synth.Drone(6f);
            _ambient.clip = _drone;
            _ambient.volume = 0.35f;
            _ambient.loop = true;
            if (_ambient.clip != null) _ambient.Play();
        }

        AudioSource BuildSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        public void SetAmbientActive(bool active)
        {
            if (_ambient == null) return;

            if (active && !_ambient.isPlaying) _ambient.Play();
            else if (!active && _ambient.isPlaying) _ambient.Stop();
        }

        void Play(AudioClip clip, float volume, float pitchJitter = 0.06f)
        {
            if (_sfx == null || clip == null) return;

            _sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            _sfx.volume = volume * MasterVolume;
            _sfx.PlayOneShot(clip);
        }

        public void Jump() => Play(_jump, 0.5f);
        public void Shard() => Play(_shard, 0.75f, 0.1f);
        public void Spawn() => Play(_spawn, 0.45f);
        public void LoopReset() => Play(_loopReset, 0.6f);
        public void Plate() => Play(_plate, 0.55f, 0.12f);
        public void Gate() => Play(_gate, 0.5f, 0.08f);
        public void Death() => Play(_death, 0.7f);
        public void Ui() => Play(_ui, 0.4f, 0.05f);
        public void Complete() => Play(_complete, 0.9f, 0.02f);
    }
}