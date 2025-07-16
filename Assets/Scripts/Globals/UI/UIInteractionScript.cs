using Globals;
using System.Collections;
using UnityEngine;

/// <summary>
/// The UI Interaction Script contains the set of public interface methods to activate a variety of Game UI actions
/// </summary>
public class UIInteractionScript : MonoBehaviour
{
    [SerializeField] private string _nextScene = null;

    IEnumerable allCanvases()
    {
        foreach (Canvas canvas in this.GetComponents<Canvas>()) yield return canvas;
        foreach (Canvas canvas in this.GetComponentsInChildren<Canvas>()) yield return canvas;
    }

    public void Start() {
        // dynamically update world space and camera screen space canvases
        foreach (Canvas canvas in allCanvases()) { 
            if(canvas.renderMode == RenderMode.WorldSpace || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                canvas.worldCamera = Camera.main;
        }
    }

    public void NextScene()
    {
        GameSceneManager.Instance.NextScene(_nextScene);
    }

    public void ReturnToMenu()
    {
        GameSceneManager.Instance.ReturnToMainMenu();
    }

    public void QuitGame()
    {
        GameSceneManager.Instance.QuitGame();
    }
    public void PauseGame()
    {
        GameSceneManager.Instance.PauseGame();
    }

    public void UnPauseGame()
    {
        GameSceneManager.Instance.ResumeGame();
    }

}
