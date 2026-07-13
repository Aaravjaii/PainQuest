using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string demoSceneName = "DemoScene";

    [Header("Web Settings")]
    [SerializeField] private string webAppURL = "https://painquest.onrender.com/";

    [Header("Button References - DRAG FROM HIERARCHY")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button webButton;

    private SoundManager soundManager;

    void Awake()
    {
        // Show cursor for menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Get SoundManager
        soundManager = SoundManager.Instance;

        if (soundManager == null)
        {
            Debug.LogError("SoundManager not found! Make sure it exists in the scene.");
        }
    }

    void Start()
    {
        // Play menu music when entering Menu scene
        if (soundManager != null)
        {
            soundManager.PlayMenuMusic();
            Debug.Log("MenuManager: Menu music started");
        }

        // Setup buttons
        SetupButtons();
    }

    void SetupButtons()
    {
        // Clear and setup Play button
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
            Debug.Log("PlayButton connected");
        }
        else
        {
            Debug.LogError("PlayButton not assigned in Inspector!");
        }

        // Clear and setup Quit button
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitButtonClicked);
            Debug.Log("QuitButton connected");
        }
        else
        {
            Debug.LogError("QuitButton not assigned in Inspector!");
        }

        // Clear and setup Web button
        if (webButton != null)
        {
            webButton.onClick.RemoveAllListeners();
            webButton.onClick.AddListener(OnWebButtonClicked);
            Debug.Log("WebButton connected");
        }
        else
        {
            Debug.LogError("WebButton not assigned in Inspector!");
        }
    }

    public void OnPlayButtonClicked()
    {
        Debug.Log("Play button clicked - Loading DemoScene...");

        // Stop menu music before switching
        if (soundManager != null)
        {
            soundManager.StopMenuMusic();
        }

        // Load DemoScene (BackgroundMusicManager will handle demo music)
        SceneManager.LoadScene(demoSceneName);
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quit button clicked - Quitting application...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnWebButtonClicked()
    {
        Debug.Log($"Web button clicked - Opening: {webAppURL}");
        Application.OpenURL(webAppURL);
    }
}