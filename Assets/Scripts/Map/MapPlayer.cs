using UnityEngine;

namespace Map
{
    /// <summary>
    /// Handles moving and displaying the player on the map. 
    /// </summary>
    public class MapPlayer : Singleton.RegulatorSingleton<MapPlayer>
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        /// <summary>
        /// Make the player sprite say something for a few seconds
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public void Say(string message, float duration = 2.0f) {
            // TODO
            Debug.Log("Map Player saying: " + message + " (for " + duration.ToString() + " seconds)");
        }
    }
}
