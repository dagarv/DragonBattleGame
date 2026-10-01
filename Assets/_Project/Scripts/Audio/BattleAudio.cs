using System.Collections;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Audio
{
    public class BattleAudio : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Music")]
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;
        [SerializeField, Min(0f)] private float musicFadeIn = 2f;
        [SerializeField, Range(0f, 1f)] private float gameOverMusicVolume = 0.1f;
        [SerializeField, Min(0f)] private float gameOverFade = 1.5f;

        [Header("Ambience")]
        [SerializeField] private AudioClip ambience;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.35f;
        [SerializeField, Min(0f)] private float ambienceFadeIn = 3f;

        [Header("Room")]
        [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.Stoneroom;
        [SerializeField, Range(-3000f, 0f)] private float reverbRoomOffset = -800f;

        [Header("Stingers")]
        [SerializeField] private SoundCue battleStart = new SoundCue();
        [SerializeField, Min(0f)] private float battleStartDelay = 0.3f;
        [SerializeField] private SoundCue victory = new SoundCue();
        [SerializeField] private SoundCue defeat = new SoundCue();

        private AudioSource musicSource;
        private AudioSource ambienceSource;

        private void Reset()
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameOver += HandleGameOver;
                gameManager.OnBattleStarted += HandleBattleStarted;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameOver -= HandleGameOver;
                gameManager.OnBattleStarted -= HandleBattleStarted;
            }
        }

        private void Awake()
        {
            SoundPlayer.SetReverb(reverbPreset, reverbRoomOffset);
        }

        private void Start()
        {
            musicSource = SoundPlayer.PlayLoop(music, musicVolume, musicFadeIn, 1f, SoundBus.Music);
            ambienceSource = SoundPlayer.PlayLoop(ambience, ambienceVolume, ambienceFadeIn, 1f, SoundBus.Music);
        }

        private void HandleBattleStarted()
        {
            StartCoroutine(PlayBattleStart());
        }

        private IEnumerator PlayBattleStart()
        {
            yield return new WaitForSeconds(battleStartDelay);
            battleStart.Play2D();
        }

        private void HandleGameOver(bool playerWon)
        {
            SoundPlayer.FadeTo(musicSource, gameOverMusicVolume, gameOverFade);
            StartCoroutine(PlayResult(playerWon));
        }

        private IEnumerator PlayResult(bool playerWon)
        {
            yield return new WaitForSeconds(gameManager.WinnerScreenDelay);
            (playerWon ? victory : defeat).Play2D();
        }
    }
}
