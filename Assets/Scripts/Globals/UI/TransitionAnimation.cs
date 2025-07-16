using System;
using System.Collections;
using UnityEngine;

namespace Globals
{
   /// <summary>
    /// Structured serializable for easy transition animation integration.
    /// </summary>
    [Serializable]
    public struct TransitionAnimation
    {
        [Tooltip("Animator component")] public Animator _animator;
        [Tooltip("Time it takes to play the transition animation")] public float _animationTime;
        [Tooltip("Boolean parameter to set while animating")] public string _animatorParameter;
        [Tooltip("Canvas/GameObject to enable/disable")] public GameObject canvas;
        [Tooltip("Animation using unscaled time")] public bool usingRealTime;
        public IEnumerator Play()
        {
            if (canvas != null) canvas.SetActive(true);
            if(this._animator != null) this._animator.SetBool(this._animatorParameter, true);
            if (usingRealTime)
            {
                yield return new WaitForSecondsRealtime(this._animationTime);
            }
            else
            {
                yield return new WaitForSeconds(this._animationTime);
            }
        }

        public IEnumerator End()
        {
            if(this._animator != null) this._animator.SetBool(this._animatorParameter, false);
            if (usingRealTime)
                yield return new WaitForSecondsRealtime(this._animationTime);
            else
            {
                yield return new WaitForSeconds(this._animationTime);
            }
            if (canvas != null) canvas.SetActive(false);
        }
    }
}
