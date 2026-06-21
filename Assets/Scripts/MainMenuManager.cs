using UnityEngine;
using UnityEngine.SceneManagement; 

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

        #if UNITY_EDITOR
            // This stops the game in the Unity Editor
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // This quits the actual .exe build
            Application.Quit();
        #endif
    }
}