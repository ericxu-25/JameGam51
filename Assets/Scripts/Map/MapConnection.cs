using Globals;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// Representation of a line connecting two MapNodes
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class MapConnection : MonoBehaviour
    {

        [HideInInspector]
        public LineRenderer line;

        [Tooltip("Optional encounters the player will have while traveling on the path")]
        public List<MapNode> HiddenNode = null;

        [Tooltip("Optional messages the player might say while traveling on this path")]
        public List<string> TravelMessages;

        public string GetRandomTravelMessage() {
            return ListHelpers.RandomFromList(TravelMessages);
        }

        public void ClearHiddenNode() {
            HiddenNode = null;
        }

        void OnValidate()
        {
            Awake();
        }

        void Awake()
        {
            TravelMessages = null;
            line = GetComponent<LineRenderer>();
        }
    }
}
