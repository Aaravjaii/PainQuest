using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.8f;
    [SerializeField] private float fadeDuration = 1.0f;

    private static SoundManager instance;
    private float targetVolume = 0f;
    private float currentVolume = 0f;
    private bool isPlaying = false;

    public static SoundManager Instance => instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("[SoundManager] Created");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        audioSource.clip = menuMusic;
    }

    void Start()
    {
        // If game starts directly in Menu
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            PlayMenuMusic();
        }
    }

    void Update()
    {
        currentVolume = Mathf.Lerp(
            currentVolume,
            targetVolume,
            Time.deltaTime / fadeDuration);

        audioSource.volume = currentVolume;

        if (Mathf.Abs(currentVolume - targetVolume) < 0.001f)
        {
            currentVolume = targetVolume;
            audioSource.volume = currentVolume;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SoundManager] Scene Loaded: {scene.name}");

        if (scene.name == "Menu")
        {
            PlayMenuMusic();
        }
        else
        {
            StopMenuMusicImmediate();
        }
    }

    public void PlayMenuMusic()
    {
        if (menuMusic == null)
        {
            Debug.LogError("[SoundManager] Menu Music not assigned!");
            return;
        }

        BackgroundMusicManager bgManager =
            BackgroundMusicManager.Instance;

        if (bgManager != null)
        {
            bgManager.StopMusicImmediate();
        }

        audioSource.Stop();
        audioSource.clip = menuMusic;
        audioSource.Play();

        currentVolume = 0f;
        audioSource.volume = 0f;

        targetVolume = musicVolume;
        isPlaying = true;

        Debug.Log("[SoundManager] Playing Menu Music");
    }

    public void StopMenuMusic()
    {
        targetVolume = 0f;
        isPlaying = false;
    }

    public void StopMenuMusicImmediate()
    {
        audioSource.Stop();
        audioSource.volume = 0f;
        currentVolume = 0f;
        targetVolume = 0f;
        isPlaying = false;

        Debug.Log("[SoundManager] Menu Music Stopped");
    }

    public bool IsMusicPlaying()
    {
        return isPlaying && audioSource.isPlaying;
    }

    public void SetVolume(float newVolume)
    {
        musicVolume = Mathf.Clamp01(newVolume);

        if (isPlaying)
            targetVolume = musicVolume;
    }

    public float GetVolume()
    {
        return musicVolume;
    }
}