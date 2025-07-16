using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Globals
{
    /// <summary>
    /// PlayerUIManager manages the transitions between scenes as well as the overall Player UI
    /// </summary>
    public class PlayerUIManager : Singleton.RegulatorSingleton<PlayerUIManager>
    {
        [Header("Transition Animations")]
        [Tooltip("Animation while pausing"), SerializeField]
        private TransitionAnimation _pauseCanvasAnimation
            = new TransitionAnimation { _animator = null, _animationTime = 0, _animatorParameter = "Paused" };

        [Tooltip("Things to disable while paused and enable on resume"), SerializeField]
        private GameObject[] DisabledWhilePaused;


        [Tooltip("Animation for loading"), SerializeField]
        private TransitionAnimation _loadingCanvasAnimation
            = new TransitionAnimation { _animator = null, _animationTime = 0, _animatorParameter = "Loading" };
        [Tooltip("Fillable image to lerp while loading"), SerializeField] private Image _progressBar;
        private float _loadingProgress;
        [Tooltip("Things to disable while loading and enable after"), SerializeField]
        private GameObject[] DisabledWhileLoading;

        private bool _isLoading = false;
        private void SetActiveDisableWhileLoadingObjects(bool active) { 
           if (DisabledWhileLoading != null)
            {
                foreach (GameObject o in DisabledWhileLoading)
                {
                    o.SetActive(active);
                }
            }
        }
            private void SetActiveDisableWhilePausedObjects(bool active) { 
           if (DisabledWhilePaused != null)
            {
                foreach (GameObject o in DisabledWhilePaused)
                {
                    o.SetActive(active);
                }
            }
        }
        public IEnumerator ShowPauseScreen()
        {
            SetActiveDisableWhilePausedObjects(false);
            if (_pauseCanvasAnimation.Equals(default(TransitionAnimation)))
            {
                Debug.LogWarning("Utilizing default uninitialized transition animation for pause screen");
                yield break;
            }
            yield return _pauseCanvasAnimation.Play();
        }
        public IEnumerator HidePauseScreen()
        {
            SetActiveDisableWhilePausedObjects(true);
            if (_pauseCanvasAnimation.Equals(default(TransitionAnimation)))
            {
                Debug.LogWarning("Utilizing default uninitialized transition animation for pause screen");
                yield break;
            }
            yield return _pauseCanvasAnimation.End();
        }
        public IEnumerator ShowLoadingScreen()
        {
            SetActiveDisableWhileLoadingObjects(false);
            SetActiveDisableWhilePausedObjects(false);
            if (_loadingCanvasAnimation.Equals(default(TransitionAnimation)))
            {
                Debug.LogWarning("Utilizing default uninitialized transition animation for loading screen");
                _isLoading = true;
                yield break;
            }
            yield return _loadingCanvasAnimation.Play();
            _isLoading = true;
        }
        public IEnumerator HideLoadingScreen()
        {
            SetActiveDisableWhileLoadingObjects(true);
            SetActiveDisableWhilePausedObjects(true);
            if (_loadingCanvasAnimation.Equals(default(TransitionAnimation)))
            {
                Debug.LogWarning("Utilizing default uninitialized transition animation for loading screen");
                _isLoading = false;
                yield break;
            }
            yield return _loadingCanvasAnimation.End();
            _isLoading = false;
        }

        public void SetLoadingProgress(float value, bool skipLerp = false)
        {
            _loadingProgress = value;
            if (skipLerp && _progressBar)
            {
                _progressBar.fillAmount = value;
                _progressBar.SetAllDirty();
            }
        }

        public void Update()
        {
            if (_isLoading && _progressBar)
            {
                _progressBar.fillAmount = Mathf.MoveTowards(_progressBar.fillAmount, _loadingProgress, 10 * Time.deltaTime);
            }
        }
    }
}
