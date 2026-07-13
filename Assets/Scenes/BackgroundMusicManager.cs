using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool playOnStart = true;
    public bool loopMusic = true;

    [Header("Fade Settings")]
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 1f;

    [Header("UI References (Optional)")]
    public Button[] menuButtons;

    private static BackgroundMusicManager _instance;
    private AudioSource _audioSource;
    private bool _isFading = false;
    private float _targetVolume = 0f;
    private float _fadeStartTime = 0f;
    private float _fadeStartVolume = 0f;
    private string _currentSceneName;
    private bool _isMusicPlaying = false;

    void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.loop = loopMusic;
            _audioSource.volume = 0f;
            _audioSource.playOnAwake = false;

            // Subscribe to scene change events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            Debug.Log("[BackgroundMusicManager] Initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (!playOnStart || backgroundMusic == null)
            return;

        if (SceneManager.GetActiveScene().name == "DemoScene")
        {
            PlayMusic();
        }
    }

    void Update()
    {
        // Handle fade
        if (_isFading)
        {
            float elapsed = Time.time - _fadeStartTime;
            float duration = _targetVolume > 0f ? fadeInDuration : fadeOutDuration;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Smooth step
            float smoothProgress = progress * progress * (3f - 2f * progress);
            _audioSource.volume = Mathf.Lerp(_fadeStartVolume, _targetVolume, smoothProgress);

            if (progress >= 1f)
            {
                _isFading = false;
                _audioSource.volume = _targetVolume;

                if (_targetVolume <= 0f)
                {
                    _audioSource.Stop();
                    _isMusicPlaying = false;
                }
                else
                {
                    _isMusicPlaying = true;
                }
            }
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // ─────────────────────────────────────────────────────────────────────
    // SCENE MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentSceneName = scene.name;

        Debug.Log($"[BackgroundMusicManager] Scene Loaded: {_currentSceneName}");

        // DemoScene → play demo music
        if (_currentSceneName == "DemoScene")
        {
            SoundManager soundManager = SoundManager.Instance;

            if (soundManager != null)
            {
                soundManager.StopMenuMusicImmediate();
            }

            PlayMusic();
        }
        // Any other scene → stop demo music
        else
        {
            StopMusicImmediate();

            Invoke(nameof(ReenableButtons), 0.1f);
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[BackgroundMusicManager] Scene unloaded: {scene.name}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUBLIC METHODS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Play background music with fade in
    /// </summary>
    public void PlayMusic()
    {
        if (backgroundMusic == null)
        {
            Debug.LogWarning("[BackgroundMusicManager] No music assigned!");
            return;
        }

        // Stop any previous playback
        _audioSource.Stop();

        _audioSource.clip = backgroundMusic;
        _audioSource.volume = 0f;

        _audioSource.Play();

        _fadeStartVolume = 0f;
        _targetVolume = volume;
        _fadeStartTime = Time.time;
        _isFading = true;
        _isMusicPlaying = true;

        Debug.Log("[BackgroundMusicManager] Playing DemoScene music");
    }
    /// <summary>
    /// Stop background music with fade out
    /// </summary>
    public void StopMusic()
    {
        if (!_audioSource.isPlaying && !_isMusicPlaying) return;

        // Fade out
        _targetVolume = 0f;
        _fadeStartVolume = _audioSource.volume;
        _fadeStartTime = Time.time;
        _isFading = true;

        Debug.Log("[BackgroundMusicManager] Stopping demo music");
    }

    /// <summary>
    /// Immediately stop music (no fade)
    /// </summary>
    public void StopMusicImmediate()
    {
        if (_audioSource == null)
            return;

        _audioSource.Stop();
        _audioSource.clip = null;

        _audioSource.volume = 0f;

        _isFading = false;
        _isMusicPlaying = false;

        Debug.Log("[BackgroundMusicManager] Demo music stopped");
    }
    /// <summary>
    /// Set volume
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (!_isFading && _isMusicPlaying)
        {
            _audioSource.volume = volume;
        }
    }

    /// <summary>
    /// Check if music is playing
    /// </summary>
    public bool IsPlaying()
    {
        return _isMusicPlaying || (_audioSource.isPlaying && _audioSource.volume > 0f);
    }

    /// <summary>
    /// Toggle music on/off
    /// </summary>
    public void ToggleMusic()
    {
        if (IsPlaying())
        {
            StopMusic();
        }
        else
        {
            PlayMusic();
        }
    }

    /// <summary>
    /// Re-enable all menu buttons (call this when returning to menu)
    /// </summary>
    public void ReenableButtons()
    {
        if (menuButtons != null && menuButtons.Length > 0)
        {
            foreach (Button btn in menuButtons)
            {
                if (btn != null)
                {
                    btn.interactable = true;
                    Debug.Log($"[BackgroundMusicManager] Re-enabled button: {btn.name}");
                }
            }
        }
        else
        {
            // Auto-find all buttons in the scene
            Button[] allButtons = FindObjectsOfType<Button>(true);
            foreach (Button btn in allButtons)
            {
                btn.interactable = true;
            }
            Debug.Log($"[BackgroundMusicManager] Re-enabled {allButtons.Length} buttons");
        }
    }

    /// <summary>
    /// Get the singleton instance
    /// </summary>
    public static BackgroundMusicManager Instance
    {
        get { return _instance; }
    }
}