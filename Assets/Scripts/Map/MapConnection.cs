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

        [Tooltip("Optional messages the player might say while traveling backwards on this path")]
        public List<string> _returnMessages = null;

        // variables used when moving on the path
        private float lastDistance;

        public List<string> TravelMessages
        {
            get
            {
                if (_travelMessages == null) _travelMessages = new List<string>();
                return _travelMessages;
            }
        }

        public List<string> ReturnMessages
        {
            get {
                if (_returnMessages == null) _returnMessages = new List<string>();
                return _returnMessages;
            }
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
            lastDistance = 0f;
        }
        /// <summary>
        /// Called when player starts to travel on this path.
        /// </summary>
        /// <param name="distance"></param>
        public void OnTravelStart(bool backtracking = false)
        {
            lastDistance = 0f;
            if (!backtracking && _travelMessages != null && TravelMessages.Count > 0)
            {
                StartCoroutine(HandleTravelMessages(TravelMessages));
            }
            else if (_returnMessages != null && ReturnMessages.Count > 0) { 
                StartCoroutine(HandleTravelMessages(ReturnMessages));
            }
        }

        /// <summary>
        /// Called when player travels on this path.
        /// </summary>
        /// <param name="distance"></param>
        public IEnumerator OnTravel(float distance) {
            if (lastDistance == 1.0f) lastDistance = 0f;
            // TODO handle hidden nodes and travel messages
            if (HiddenNodes != null && HiddenNodes.Count > 0) {
                MapManager.Instance.CanMove = false;
                yield return CoroutineHelper.CallbackCoroutine(HandleHiddenNodes(distance), () => MapManager.Instance.CanMove = true);
            }
            lastDistance = distance;
        }

        private static bool Between(float value, float start, float end) { 
            if (start < end) return start <= value && end >= value;
            else return end <= value && start >= value;
        }

        private IEnumerator HandleHiddenNodes(float currentDistance) {
            for (int i = 0; i < HiddenNodes.Count; ++i) {
                if (Between(1.0f/(HiddenNodes.Count + 1), lastDistance, currentDistance)) 
                {
                    Debug.Log("Activated Hidden Node on connection: " + HiddenNodes[i].name);
                    yield return HiddenNodes[i].OnArrive();
                    yield return HiddenNodes[i].OnLeave();
                }
            }
            yield break;
        }

        private IEnumerator HandleTravelMessages(List<string> messages) {
            MapPlayer.Instance.Say(ListHelpers.RandomFromList(messages));
            yield break;
        }
    }
}
