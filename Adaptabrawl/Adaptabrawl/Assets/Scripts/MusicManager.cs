using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] private AudioClip battleMusic;
        [SerializeField] private float battleStartPauseDuration = 0.1f;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.5f;

        private AudioSource audioSource;
        private SettingsContext settingsContext;
        private Coroutine pendingTrackSwitch;

        private enum MusicTrack
        {
            None,
            Background,
            Battle
        }

        private MusicTrack currentTrack = MusicTrack.None;

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
            audioSource.playOnAwake = false;
            ApplySavedMusicVolume();

            if (backgroundMusic == null)
            {
                Debug.LogWarning("[MusicManager] No background music clip assigned! Drag the mp3 into the Background Music slot.", this);
                return;
            }

            PlayClip(backgroundMusic, loop: true);
            currentTrack = MusicTrack.Background;
        }

        private void Start()
        {
            // Fallback: if Awake ran but Play didn't take (e.g. audio system not ready yet)
            RefreshTrackForScene(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            settingsContext = SettingsContext.EnsureExists();
            if (settingsContext != null)
                settingsContext.SettingsChanged += HandleSettingsChanged;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (settingsContext != null)
                settingsContext.SettingsChanged -= HandleSettingsChanged;
            settingsContext = null;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
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

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            RefreshTrackForScene(scene);
        }

        private void RefreshTrackForScene(Scene scene)
        {
            if (IsGameplayScene(scene.name))
            {
                QueueBattleMusicStart();
                return;
            }

            SwitchToBackgroundMusicImmediate();
        }

        private static bool IsGameplayScene(string sceneName)
        {
            return sceneName == "GameScene"
                || sceneName == "OnlineGameScene"
                || sceneName == "TestCharacter";
        }

        private void QueueBattleMusicStart()
        {
            if (battleMusic == null)
            {
                Debug.LogWarning("[MusicManager] Battle music is not assigned. Keeping background music active.", this);
                SwitchToBackgroundMusicImmediate();
                return;
            }

            if (currentTrack == MusicTrack.Battle && audioSource != null && audioSource.isPlaying && audioSource.clip == battleMusic)
                return;

            CancelPendingTrackSwitch();
            pendingTrackSwitch = StartCoroutine(StartBattleMusicAfterPause());
        }

        private IEnumerator StartBattleMusicAfterPause()
        {
            if (audioSource == null)
                yield break;

            if (audioSource.isPlaying)
                audioSource.Pause();

            if (battleStartPauseDuration > 0f)
                yield return new WaitForSecondsRealtime(battleStartPauseDuration);

            PlayClip(battleMusic, loop: true);
            currentTrack = MusicTrack.Battle;
            pendingTrackSwitch = null;
        }

        private void SwitchToBackgroundMusicImmediate()
        {
            CancelPendingTrackSwitch();

            if (backgroundMusic == null || audioSource == null)
                return;

            if (currentTrack == MusicTrack.Background && audioSource.isPlaying && audioSource.clip == backgroundMusic)
                return;

            PlayClip(backgroundMusic, loop: true);
            currentTrack = MusicTrack.Background;
        }

        private void CancelPendingTrackSwitch()
        {
            if (pendingTrackSwitch == null)
                return;

            StopCoroutine(pendingTrackSwitch);
            pendingTrackSwitch = null;
        }

        private void PlayClip(AudioClip clip, bool loop)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.volume = volume;
            audioSource.Play();
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
