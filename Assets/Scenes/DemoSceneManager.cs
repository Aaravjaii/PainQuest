using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoSceneManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string menuSceneName = "Menu";

    private SoundManager soundManager;
    private BackgroundMusicManager bgMusicManager;

    void Awake()
    {
        // Get SoundManager
        soundManager = SoundManager.Instance;

        // Get BackgroundMusicManager
        bgMusicManager = BackgroundMusicManager.Instance;

        if (soundManager == null)
        {
            Debug.LogError("SoundManager not found! Make sure it exists.");
        }

        if (bgMusicManager == null)
        {
            Debug.LogError("BackgroundMusicManager not found! Make sure it exists.");
        }
    }

    void Start()
    {
        // Play demo music when entering DemoScene
        if (bgMusicManager != null)
        {
            bgMusicManager.PlayMusic();
            Debug.Log("DemoSceneManager: Demo music started");
        }

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("DemoScene loaded - Press ESC to return to Menu");
    }

    void Update()
    {
        // Check for ESC key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    public void ReturnToMenu()
    {
        Debug.Log("Returning to Menu...");

        // Stop demo music before switching
        if (bgMusicManager != null)
        {
            bgMusicManager.StopMusic();
        }

        // Reset cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Load Menu scene (SoundManager will handle menu music)
        SceneManager.LoadScene(menuSceneName);
    }
}