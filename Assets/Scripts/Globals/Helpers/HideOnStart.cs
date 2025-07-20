using UnityEngine;

namespace Globals
{
    /// <summary>
    /// Hides the game object on start
    /// </summary>
    public class HideOnStart : MonoBehaviour
    {
        void Start()
        {
            this.gameObject.SetActive(false);    
        }
    }
}
