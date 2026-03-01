using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Adaptabrawl.Settings;

namespace Adaptabrawl.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu UI")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private bool isPaused = false;
        
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        
        [Header("Settings")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private bool canPause = true;
        
        private float previousTimeScale = 1f;
        
        private void Start()
        {
            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
            
            // Hide pause panel initially
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }
        
        private void Update()
        {
            if (!canPause) return;
            
            // Check for pause input
            if (UnityEngine.Input.GetKeyDown(pauseKey))
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
        
        public void PauseGame()
        {
            if (isPaused || !canPause) return;
            
            isPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            
            if (pausePanel != null)
                pausePanel.SetActive(true);
            
            // Disable player input
            // This would typically disable input handlers
        }
        
        public void ResumeGame()
        {
            if (!isPaused) return;
            
            isPaused = false;
            Time.timeScale = previousTimeScale;
            
            if (pausePanel != null)
                pausePanel.SetActive(false);
            
            // Re-enable player input
        }
        
        public void OpenSettings()
        {
            // Store current scene to return to
            PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
            PlayerPrefs.SetInt("WasPaused", isPaused ? 1 : 0);
            
            // Load settings scene
            SceneManager.LoadScene("SettingsScene");
        }
        
        public void ReturnToMainMenu()
        {
            // Resume time scale before leaving
            Time.timeScale = 1f;
            
            // Return to main menu
            SceneManager.LoadScene("StartScene");
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        public void SetCanPause(bool canPause)
        {
            this.canPause = canPause;
        }
        
        public bool IsPaused => isPaused;
        
        private void OnDestroy()
        {
            // Ensure time scale is reset
            Time.timeScale = 1f;
        }
    }
}

