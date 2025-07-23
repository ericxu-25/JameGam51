#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Collections.Generic;

// reference for this part: https://www.wayline.io/blog/using-loadscene-and-loadsceneasync-in-unity
// another reference: https://www.youtube.com/watch?v=OmobsXZSRKo
// and another: https://www.youtube.com/watch?v=CE9VOZivb3I

namespace Globals
{
    // planning into the future with events. See here https://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
    public class PauseEventArgs : EventArgs
    {
        public PauseEventArgs(bool paused)
        {
            Paused = paused;
        }
        public bool Paused { get; private set; }
    }
    public delegate void PauseGameEventHandler(PauseEventArgs e);

    public class SceneEventArgs : EventArgs {
        public SceneEventArgs(int nextScene) {
            this.nextScene = nextScene;
        }

        public int nextScene { get; private set; }
    }
    public delegate void SceneEventHandler(SceneEventArgs e);

    /// <summary>
    /// Manages transitioning between, quitting, and altering global game scene settings (like pausing/unpausing)
    /// Should be the primary hookup in external scripts.
    /// </summary>
    public class GameSceneManager : Singleton.PersistentSingleton<GameSceneManager>
    {
        [Tooltip("Minimum amount of time to load"), SerializeField] private float minimumLoadTime = 0;
        [Header("Global Settings")]
        [SerializeField] private float GameTimeScale = 1;
        [Tooltip("Name/index of the scene to return to for the menu")]
        [SerializeField] private string menuScene = "0";

        private List<int> OpenScenes;

        private bool _busy = false; // flag used to prevent loading multiple things at once
        public bool Busy { get { return _busy; } }

        /// <summary>
        /// Internal method to start a busy coroutine which cannot be run if another busy coroutine started in the same script is not finished.
        /// </summary>
        /// <param name="coroutine"></param>
        /// <returns>false if the coroutine could not be run, otherwise true.</returns>
        private bool StartBusyCoroutine(IEnumerator coroutine)
        {
            if (_busy) { return false; }
            _busy = true;
            StartCoroutine(CoroutineHelper.CallbackCoroutine(coroutine, () => { _busy = false; Debug.Log("\tBusy operation completed"); }));
            return true;
        }

        private bool _gamePaused = false;
        public bool GamePaused
        {
            get { return _gamePaused; }
            private set
            {
                _gamePaused = value;
            }
        }
        public event PauseGameEventHandler PauseGameEvent;
        public event SceneEventHandler NextSceneEvent;
        public event SceneEventHandler CloseSceneEvent;

        protected override void Awake()
        {
            base.Awake();
            OpenScenes = new List<int>();
            OpenScenes.Add(SceneManager.GetActiveScene().buildIndex);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            OpenScenes.Remove(scene.buildIndex);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            OpenScenes.Add(scene.buildIndex);
        }

        

        public static int AbsoluteSceneIndexFromName(string sceneName)
        {
            int nextSceneIndex = -1;
            if (!string.IsNullOrEmpty(sceneName))
            {
                bool isNextSceneANumber = int.TryParse(sceneName, out nextSceneIndex);
                if (!isNextSceneANumber)
                {
                    nextSceneIndex = SceneManager.GetSceneByName(sceneName).buildIndex;
                }
            }
            return nextSceneIndex;
        }
        public static int SafeSceneIndexFromName(string sceneName)
        {
            int nextSceneIndex = AbsoluteSceneIndexFromName(sceneName);
            if (nextSceneIndex < 0)
            {
                Debug.LogWarning("Could not find " + sceneName + " in available scenes, defaulting to next scene...");
                nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            }
            return nextSceneIndex;
        }

        // loads the next scene; if no scene name/index given, will load the next in the build index
        public bool NextScene(string nextScene, LoadSceneMode loadMode=LoadSceneMode.Single)
        {
            int nextSceneIndex = SafeSceneIndexFromName(nextScene);
            nextScene = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
            if (!StartBusyCoroutine(LoadScene(nextSceneIndex, loadMode)))
            {
                Debug.Log("Could not load next scene; in middle of another busy operation.");
                return false;
            }
            Debug.Log("Transitioning to next scene: " + nextScene);
            return true;
        }

        /// <summary>
        /// Loads a given scene in a coroutine
        /// </summary>
        /// <param name="nextScene"></param>
        /// <param name="loadMode"></param>
        /// <returns></returns>
        public IEnumerator NextSceneAsync(string nextScene, LoadSceneMode loadMode = LoadSceneMode.Single) { 
            int nextSceneIndex = SafeSceneIndexFromName(nextScene);
            nextScene = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
            if (_busy) { 
                Debug.Log("Transitioning to next scene: " + nextScene + "... but waiting until other busy operation completes.");
            }
            yield return new WaitWhile(() => _busy);
            _busy = true;
            Debug.Log("Transitioning to next scene: " + nextScene);
            yield return LoadScene(nextSceneIndex, loadMode);
            _busy = false;
        }

        // closes the given scene
        public bool CloseScene(string nextScene)
        {
            int nextSceneIndex = SafeSceneIndexFromName(nextScene);
            nextScene = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
            if (!OpenScenes.Contains(nextSceneIndex)) {
                Debug.LogError("Attempted to close " + nextScene + " when it isn't open!");
                return false;
            }
            if (OpenScenes.Count == 1) {
                Debug.LogError("Attempted to close " + nextScene + " when it is the only scene open!");
                return false;
            }
            if (!StartBusyCoroutine(UnloadScene(nextSceneIndex)))
            {
                Debug.Log("Could not unload next scene; in middle of another busy operation.");
                return false;
            }
            Debug.Log("Closed scene: " + nextScene);
            return true;
        }
        IEnumerator LoadScene(int sceneBuildIndex, LoadSceneMode loadMode)
        {
            // play animation
            PlayerUIManager.Instance.SetLoadingProgress(0, true);
            yield return PlayerUIManager.Instance.ShowLoadingScreen();
            // begin async loading
            var scene = SceneManager.LoadSceneAsync(sceneBuildIndex, loadMode);
            NextSceneEvent?.Invoke(new SceneEventArgs(sceneBuildIndex));
            scene.allowSceneActivation = false;
            // Unity caps scene loading to 90%
            do
            {
                PlayerUIManager.Instance.SetLoadingProgress(scene.progress * 10 / 9);
                yield return new WaitForSecondsRealtime(0.002f);
            } while (scene.progress < 0.9f);
            // change to next scene
            scene.allowSceneActivation = true; 
            // complete loading screen
            if (minimumLoadTime > 0)
            {
                yield return new WaitForSecondsRealtime(minimumLoadTime / 2);
                PlayerUIManager.Instance.SetLoadingProgress(1.0f, true);
                yield return new WaitForSecondsRealtime(minimumLoadTime / 2);
            }
            yield return PlayerUIManager.Instance.HideLoadingScreen();
        }
        IEnumerator UnloadScene(int sceneBuildIndex)
        {
            var scene = SceneManager.UnloadSceneAsync(sceneBuildIndex);
            CloseSceneEvent?.Invoke(new SceneEventArgs(sceneBuildIndex));
            bool _completed = false;
            scene.completed += (AsyncOperation a) => { _completed = true; } ;
            yield return new WaitUntil(() => _completed);
        }

        public bool IsSceneOpen(string scene) { 
            int nextSceneIndex = SafeSceneIndexFromName(scene);
            if (nextSceneIndex < 0) return false; 
            if (OpenScenes.Contains(nextSceneIndex)) {
                return true;
            }
            return false;
        }

        public void QuitGame()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                Debug.Log("Quitting Game (editor)");
                EditorApplication.isPlaying = false;
#endif
            }
            else
            {
                Debug.Log("Quitting Game");
                Application.Quit();
            }
        }

        private void SendPauseEvent(bool paused)
        {
            Instance.PauseGameEvent?.Invoke(new PauseEventArgs(paused));
        }

        private void PauseGameTime()
        {
            if (GamePaused == true) { return; }
            GamePaused = true;
            SendPauseEvent(GamePaused);
            Time.timeScale = 0;
        }

        private void UnPauseGameTime()
        {
            if (GamePaused != true) { return; }
            GamePaused = false;
            SendPauseEvent(GamePaused);
            Time.timeScale = GameTimeScale;
        }

        public void PauseGame()
        {
            if (GamePaused)
            {
                Debug.Log("Game already paused");
                return;
            }
            if (!StartBusyCoroutine(PlayerUIManager.Instance.ShowPauseScreen())) { return; }
            Debug.Log("Pausing Game");
            PauseGameTime();
        }

        public void ResumeGame()
        {
            if (!GamePaused)
            {
                Debug.Log("Game is not paused");
                return;
            }
            if (!StartBusyCoroutine(PlayerUIManager.Instance.HidePauseScreen())) { return; }
            Debug.Log("Resuming Game");
            UnPauseGameTime();
        }
        public void ReturnToMainMenu()
        {
            if (Instance.NextScene(menuScene))
            {
                Debug.Log("Returning to main menu!");
                UnPauseGameTime();
            }
        }
    }
}
