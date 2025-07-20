using Globals;
using System.Collections;
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
        public List<MapNode> HiddenNodes = null;

        [Tooltip("Optional messages the player might say while traveling on this path")]
        public List<string> _travelMessages = null;

        public List<string> TravelMessages
        {
            get
            {
                if (_travelMessages == null) _travelMessages = new List<string>();
                return _travelMessages;
            }
        }

        public string GetRandomTravelMessage()
        {
            return ListHelpers.RandomFromList(TravelMessages);
        }

        public void ClearHiddenNode() {
            HiddenNodes = null;
        }

        void OnValidate()
        {
            Awake();
        }

        void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        /// <summary>
        /// Called when player travels on this path
        /// </summary>
        /// <param name="duration"></param>
        public void OnTravel(float duration) {
            // TODO handle hidden nodes and travel messages
            if (HiddenNodes != null && HiddenNodes.Count > 0) {
                MapManager.Instance.CurrentlyMoving = true;
                StartCoroutine(CoroutineHelper.CallbackCoroutine(HandleHiddenNodes(duration), () => MapManager.Instance.CurrentlyMoving = false));
            }
            if (_travelMessages != null && TravelMessages.Count > 0) { 
                StartCoroutine(HandleTravelMessages(duration));
            }
        }

        private IEnumerator HandleHiddenNodes(float travelTime) {
            Debug.Log("Activated Hidden Node on connection: " + HiddenNodes[0].name);
            yield break;
        }

        private IEnumerator HandleTravelMessages(float travelTime) { 
            yield break;
        }
    }
}
