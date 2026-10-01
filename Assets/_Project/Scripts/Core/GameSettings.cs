using DragonBattle.Audio;
using UnityEngine;

namespace DragonBattle.Core
{
    public static class GameSettings
    {
        private const string MasterKey = "settings.masterVolume";
        private const string MusicKey = "settings.musicVolume";
        private const string EffectsKey = "settings.effectsVolume";
        private const string ShakeKey = "settings.screenShake";

        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static float EffectsVolume { get; private set; } = 1f;
        public static bool ScreenShake { get; private set; } = true;
        public static bool Fullscreen { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            EffectsVolume = PlayerPrefs.GetFloat(EffectsKey, 1f);
            ScreenShake = PlayerPrefs.GetInt(ShakeKey, 1) == 1;
            Fullscreen = Screen.fullScreen;
            ApplyAudio();
        }

        public static void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            ApplyAudio();
        }

        public static void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            ApplyAudio();
        }

        public static void SetEffectsVolume(float volume)
        {
            EffectsVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(EffectsKey, EffectsVolume);
            ApplyAudio();
        }

        public static void SetScreenShake(bool isEnabled)
        {
            ScreenShake = isEnabled;
            PlayerPrefs.SetInt(ShakeKey, isEnabled ? 1 : 0);
        }

        public static void SetFullscreen(bool isFullscreen)
        {
            Fullscreen = isFullscreen;
            Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;
            SoundPlayer.SetBusVolume(SoundBus.Music, MusicVolume);
            SoundPlayer.SetBusVolume(SoundBus.Sfx, EffectsVolume);
            SoundPlayer.SetBusVolume(SoundBus.Ui, EffectsVolume);
        }
    }
}
