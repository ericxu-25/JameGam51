using System.Collections;
using UnityEngine;

namespace Globals
{
    public delegate void CoroutineCallback();
    public class CoroutineHelper : MonoBehaviour
    {
        /// <summary>
        /// Executes a coroutine and callsback a provided function once it is finished.
        /// </summary>
        /// <param name="coroutine"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static IEnumerator CallbackCoroutine(IEnumerator coroutine, CoroutineCallback func)
        {
            // for info, see: https://www.alanzucconi.com/2017/02/15/nested-coroutines-in-unity/
            // and also: https://discussions.unity.com/t/coroutines-which-call-other-coroutines/820084/5
            yield return coroutine;
            func();
        }
    }
}
