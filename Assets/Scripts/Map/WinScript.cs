using Globals;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// On trigger, moves us to the win screen!
    /// </summary>
    public class WinScript : MonoBehaviour
    {
        [SerializeField, Tooltip("Scene to go to when we win")]
        private string winScene;
        public void Win() {
            GameSceneManager.Instance.NextScene(winScene);
        } 
    }
}
