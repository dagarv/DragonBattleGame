using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DragonBattle.Audio
{
    public class SoundPlayer : MonoBehaviour
    {
        private const float PanStrength = 0.6f;
        private const float DuckAttackTime = 0.06f;

        private static readonly int[] PoolSizes = { 20, 6, 4 };

        private static readonly float[] BusVolumes = { 1f, 1f, 1f };

        private static SoundPlayer instance;

        private Bus[] buses;
        private float duckDepth;
        private float duckUntil;
        private float duckReleaseRate = 1f;

        private class Bus
        {
            public SoundBus Id;
            public GameObject Root;
            public AudioReverbFilter Reverb;
            public readonly List<Voice> Voices = new List<Voice>();
            public int NextVoice;
            public float Gain = 1f;
        }

        private class Voice
        {
            public AudioSource Source;
            public Bus Bus;
            public float BusyUntil;
            public float Level;
            public float TargetLevel;
            public float FadeRate;
            public bool StopAfterFade;
        }

        private static SoundPlayer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("Sound Player").AddComponent<SoundPlayer>();
                }
                return instance;
            }
        }

        public static void SetBusVolume(SoundBus bus, float volume)
        {
            BusVolumes[(int)bus] = Mathf.Clamp01(volume);
        }

        public static void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip != null)
            {
                Instance.Shoot(SoundBus.Sfx, clip, StereoPan(position), volume, pitch);
            }
        }

        public static void PlayOneShot2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip != null)
            {
                Instance.Shoot(SoundBus.Ui, clip, 0f, volume, pitch);
            }
        }

        public static AudioSource PlayLoop(AudioClip clip, float volume, float fadeInTime, float pitch = 1f, SoundBus bus = SoundBus.Sfx)
        {
            if (clip == null)
            {
                return null;
            }

            Voice voice = ClaimVoice(Instance.GetBus(bus));
            AudioSource source = voice.Source;
            source.clip = clip;
            source.loop = true;
            source.pitch = pitch;
            source.panStereo = 0f;
            voice.Level = fadeInTime > 0f ? 0f : volume;
            source.volume = voice.Level * voice.Bus.Gain * BusVolumes[(int)voice.Bus.Id];
            source.Play();
            voice.BusyUntil = float.PositiveInfinity;
            voice.StopAfterFade = false;
            Instance.SetFade(voice, volume, fadeInTime);
            return source;
        }

        public static void FadeTo(AudioSource source, float volume, float duration)
        {
            Voice voice = instance != null ? instance.Find(source) : null;
            if (voice != null)
            {
                voice.StopAfterFade = false;
                instance.SetFade(voice, volume, duration);
            }
        }

        public static void Stop(AudioSource source, float fadeOutTime)
        {
            Voice voice = instance != null ? instance.Find(source) : null;
            if (voice == null)
            {
                return;
            }

            voice.StopAfterFade = true;
            instance.SetFade(voice, 0f, fadeOutTime);
        }

        public static void SetPan(AudioSource source, Vector3 position)
        {
            if (source != null)
            {
                source.panStereo = StereoPan(position);
            }
        }

        public static void Duck(float depth, float hold, float release)
        {
            if (depth <= 0f)
            {
                return;
            }

            SoundPlayer player = Instance;
            float now = Time.unscaledTime;
            player.duckDepth = now < player.duckUntil ? Mathf.Max(player.duckDepth, depth) : depth;
            player.duckDepth = Mathf.Clamp01(player.duckDepth);
            player.duckUntil = Mathf.Max(player.duckUntil, now + hold);
            player.duckReleaseRate = 1f / Mathf.Max(0.01f, release);
        }

        public static void SetReverb(AudioReverbPreset preset, float roomOffset)
        {
            AudioReverbFilter reverb = Instance.GetBus(SoundBus.Sfx).Reverb;
            reverb.enabled = preset != AudioReverbPreset.Off;
            reverb.reverbPreset = preset;
            if (reverb.enabled && roomOffset != 0f)
            {
                reverb.room = Mathf.Clamp(reverb.room + roomOffset, -10000f, 0f);
            }
        }

        private static float StereoPan(Vector3 position)
        {
            Camera view = Camera.main;
            if (view == null)
            {
                return 0f;
            }

            float x = view.WorldToViewportPoint(position).x;
            return Mathf.Clamp((x - 0.5f) * 2f, -1f, 1f) * PanStrength;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneUnloaded += HandleSceneUnloaded;

            buses = new Bus[PoolSizes.Length];
            for (int i = 0; i < buses.Length; i++)
            {
                Bus bus = new Bus { Id = (SoundBus)i, Root = new GameObject(((SoundBus)i).ToString()) };
                bus.Root.transform.SetParent(transform, false);
                for (int j = 0; j < PoolSizes[i]; j++)
                {
                    AudioSource source = bus.Root.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    source.ignoreListenerPause = bus.Id != SoundBus.Sfx;
                    bus.Voices.Add(new Voice { Source = source, Bus = bus });
                }
                buses[i] = bus;
            }

            Bus sfx = GetBus(SoundBus.Sfx);
            sfx.Reverb = sfx.Root.AddComponent<AudioReverbFilter>();
            sfx.Reverb.reverbPreset = AudioReverbPreset.Off;
            sfx.Reverb.enabled = false;
        }

        private Bus GetBus(SoundBus bus)
        {
            return buses[(int)bus];
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            duckDepth = 0f;
            duckUntil = 0f;
            foreach (Bus bus in buses)
            {
                bus.Gain = 1f;
                foreach (Voice voice in bus.Voices)
                {
                    if (voice.Source.loop)
                    {
                        Release(voice);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            UpdateDuck(deltaTime);

            foreach (Bus bus in buses)
            {
                foreach (Voice voice in bus.Voices)
                {
                    UpdateVoice(voice, deltaTime);
                }
            }
        }

        private void UpdateDuck(float deltaTime)
        {
            Bus music = GetBus(SoundBus.Music);
            bool ducking = Time.unscaledTime < duckUntil;
            float target = ducking ? 1f - duckDepth : 1f;
            float rate = music.Gain > target ? 1f / DuckAttackTime : duckReleaseRate;
            music.Gain = Mathf.MoveTowards(music.Gain, target, rate * deltaTime);
        }

        private static void UpdateVoice(Voice voice, float deltaTime)
        {
            AudioSource source = voice.Source;
            if (!source.loop)
            {
                return;
            }

            if (voice.FadeRate > 0f)
            {
                voice.Level = Mathf.MoveTowards(voice.Level, voice.TargetLevel, voice.FadeRate * deltaTime);
                if (Mathf.Approximately(voice.Level, voice.TargetLevel))
                {
                    voice.FadeRate = 0f;
                    if (voice.StopAfterFade)
                    {
                        Release(voice);
                        return;
                    }
                }
            }

            source.volume = voice.Level * voice.Bus.Gain * BusVolumes[(int)voice.Bus.Id];
        }

        private void Shoot(SoundBus busId, AudioClip clip, float pan, float volume, float pitch)
        {
            Bus bus = GetBus(busId);
            Voice voice = ClaimVoice(bus);
            AudioSource source = voice.Source;
            pitch = Mathf.Max(0.05f, pitch);
            source.clip = null;
            source.loop = false;
            source.volume = bus.Gain * BusVolumes[(int)busId];
            source.pitch = pitch;
            source.panStereo = pan;
            source.PlayOneShot(clip, volume);
            voice.FadeRate = 0f;
            voice.StopAfterFade = false;
            voice.BusyUntil = Time.unscaledTime + clip.length / pitch;
        }

        private static Voice ClaimVoice(Bus bus)
        {
            List<Voice> voices = bus.Voices;
            for (int i = 0; i < voices.Count; i++)
            {
                Voice candidate = voices[(bus.NextVoice + i) % voices.Count];
                if (Time.unscaledTime >= candidate.BusyUntil)
                {
                    bus.NextVoice = (bus.NextVoice + i + 1) % voices.Count;
                    return candidate;
                }
            }

            for (int i = 0; i < voices.Count; i++)
            {
                Voice candidate = voices[(bus.NextVoice + i) % voices.Count];
                if (!candidate.Source.loop)
                {
                    bus.NextVoice = (bus.NextVoice + i + 1) % voices.Count;
                    candidate.Source.Stop();
                    return candidate;
                }
            }

            Voice fallback = voices[bus.NextVoice];
            bus.NextVoice = (bus.NextVoice + 1) % voices.Count;
            Release(fallback);
            return fallback;
        }

        private Voice Find(AudioSource source)
        {
            if (source == null)
            {
                return null;
            }

            foreach (Bus bus in buses)
            {
                Voice voice = bus.Voices.Find(candidate => candidate.Source == source);
                if (voice != null)
                {
                    return voice;
                }
            }
            return null;
        }

        private void SetFade(Voice voice, float volume, float duration)
        {
            voice.TargetLevel = volume;
            if (duration <= 0f)
            {
                voice.Level = volume;
                voice.Source.volume = volume * voice.Bus.Gain;
                voice.FadeRate = 0f;
                if (voice.StopAfterFade)
                {
                    Release(voice);
                }
                return;
            }

            voice.FadeRate = Mathf.Max(0.0001f, Mathf.Abs(voice.Level - volume) / duration);
        }

        private static void Release(Voice voice)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.loop = false;
            voice.Level = 0f;
            voice.FadeRate = 0f;
            voice.StopAfterFade = false;
            voice.BusyUntil = 0f;
        }
    }
}
