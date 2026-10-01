using System;
using System.Collections;
using DragonBattle.Combat;
using DragonBattle.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DragonBattle.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Dragons")]
        [SerializeField] private Health player;
        [SerializeField] private Health enemy;
        [SerializeField] private Behaviour[] disableOnGameOver;

        [Header("Intro")]
        [SerializeField] private FightBanner fightBanner;
        [SerializeField] private MainMenu mainMenu;

        [Header("Winner Screen")]
        [SerializeField] private WinnerScreen winnerScreen;
        [SerializeField, Min(0f)] private float winnerScreenDelay = 2f;

        public event Action<bool> OnGameOver;
        public event Action OnBattleStarted;

        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }
        public bool CanPause => controlsActive && !IsGameOver && !IsPaused;
        public float WinnerScreenDelay => winnerScreenDelay;

        private bool controlsActive;

        private void OnEnable()
        {
            player.OnDied += HandleDied;
            enemy.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            player.OnDied -= HandleDied;
            enemy.OnDied -= HandleDied;
        }

        private void Start()
        {
            SetControlsEnabled(false);
            if (mainMenu != null && mainMenu.Open(BeginBattle))
            {
                return;
            }
            BeginBattle();
        }

        private void BeginBattle()
        {
            OnBattleStarted?.Invoke();
            if (fightBanner == null)
            {
                SetControlsEnabled(!IsGameOver);
                return;
            }
            fightBanner.Play(() => SetControlsEnabled(!IsGameOver));
        }

        private void HandleDied(Health fallen)
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            SetControlsEnabled(false);

            OnGameOver?.Invoke(fallen != player);
            StartCoroutine(ShowWinner(fallen == player ? enemy : player));
        }

        private IEnumerator ShowWinner(Health winner)
        {
            yield return new WaitForSeconds(winnerScreenDelay);

            DragonIdentity identity = winner.GetComponent<DragonIdentity>();
            string winnerName = identity != null ? identity.DisplayName : winner.name;
            Color winnerColor = identity != null ? identity.AccentColor : Color.white;
            winnerScreen.Show(winnerName, winnerColor, winner == player, Restart);
        }

        private void SetControlsEnabled(bool isEnabled)
        {
            controlsActive = isEnabled;
            ApplyControls();
        }

        private void ApplyControls()
        {
            bool isEnabled = controlsActive && !IsPaused;
            foreach (Behaviour behaviour in disableOnGameOver)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = isEnabled;
                }
            }
        }

        public void SetPaused(bool paused)
        {
            if (paused == IsPaused || (paused && !CanPause))
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            ApplyControls();
        }

        private void OnDestroy()
        {
            ClearPause();
        }

        private void ClearPause()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void Restart()
        {
            ClearPause();
            MainMenu.SkipNextOpen();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMenu()
        {
            ClearPause();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
