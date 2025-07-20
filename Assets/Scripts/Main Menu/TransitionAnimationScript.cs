using System.Collections;
using UnityEngine;
using static Globals.CoroutineHelper;

namespace Menu
{
    /// <summary>
    /// Class which handles a single transition animation between two states
    /// </summary>
    public class TransitionAnimationScript: MonoBehaviour
    {
        [SerializeField, Tooltip("Animation to Play When the Credits Screen is Shown")]
        Globals.TransitionAnimation TransitionAnimation;

        [SerializeField, Tooltip("Additional objects to disable while showing the credits scene")]
        GameObject[] DisabledWhileActive;

        private bool _active = false;

        private void SetActiveDisabledWhileActiveObjects(bool active) {
            if (DisabledWhileActive == null) return;
            foreach (GameObject o in DisabledWhileActive)
                o.SetActive(active);
        }

        protected IEnumerator TransitionToActive()
        {
            SetActiveDisabledWhileActiveObjects(false);
            if (TransitionAnimation.Equals(default(Globals.TransitionAnimation)))
            {
                Debug.LogWarning("Utilizing default uninitialized transition animation");
                yield break;
            }
            yield return TransitionAnimation.Play();
        }
        protected IEnumerator TransitionToInactive()
        {
            SetActiveDisabledWhileActiveObjects(true);
            if (TransitionAnimation.Equals(default(Globals.TransitionAnimation)))
            {
                Debug.LogWarning("Utilizing default uninitialized transition animation");
                yield break;
            }
            yield return TransitionAnimation.End();
        }

        private void UpdateTransitionState() {
            if (_active) _active = false;
            else _active = true;
        }

        public void Show() {
            if (!_active)
            {
                StartCoroutine(CallbackCoroutine(TransitionToActive(), UpdateTransitionState));
            }
            else {
                Debug.LogWarning("Attempted to play transition animation from active to active state.");
            }
        }

        public void Hide() {

            if (_active)
            {
                StartCoroutine(CallbackCoroutine(TransitionToInactive(), UpdateTransitionState));
            }
            else
            {
                Debug.LogWarning("Attempted to play transition animation from active to active state.");
            }
        }
    }
}
