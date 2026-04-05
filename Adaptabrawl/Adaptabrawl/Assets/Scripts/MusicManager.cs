using UnityEngine;
using Adaptabrawl.Settings;

namespace Adaptabrawl
{
    /// <summary>
    /// Singleton that plays the global background music across all scenes.
    /// Place this on a GameObject in your StartScene — it will persist for the lifetime of the game.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.5f;

        private AudioSource audioSource;
        private SettingsContext settingsContext;

        private void Awake()
        {
            // Singleton: destroy any duplicate that loads into a second scene
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.playOnAwake = false;
            ApplySavedMusicVolume();

            if (backgroundMusic == null)
            {
                Debug.LogWarning("[MusicManager] No background music clip assigned! Drag the mp3 into the Background Music slot.", this);
                return;
            }

            audioSource.clip = backgroundMusic;
            audioSource.Play();
        }

        private void Start()
        {
            // Fallback: if Awake ran but Play didn't take (e.g. audio system not ready yet)
            if (audioSource != null && backgroundMusic != null && !audioSource.isPlaying)
            {
                audioSource.clip = backgroundMusic;
                audioSource.Play();
            }
        }

        private void OnEnable()
        {
            settingsContext = SettingsContext.EnsureExists();
            if (settingsContext != null)
                settingsContext.SettingsChanged += HandleSettingsChanged;
        }

        private void OnDisable()
        {
            if (settingsContext != null)
                settingsContext.SettingsChanged -= HandleSettingsChanged;
            settingsContext = null;
        }

        private void HandleSettingsChanged()
        {
            if (settingsContext == null)
                return;

            SetVolume(settingsContext.musicVolume);
        }

        private void ApplySavedMusicVolume()
        {
            float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", volume);
            SetVolume(savedMusicVolume);
        }

        /// <summary>Pause the music (e.g., on pause screen).</summary>
        public void Pause() => audioSource.Pause();

        /// <summary>Resume after pausing.</summary>
        public void Resume() => audioSource.UnPause();

        /// <summary>Smoothly set volume at runtime.</summary>
        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            if (audioSource != null)
                audioSource.volume = volume;
        }
    }
}
