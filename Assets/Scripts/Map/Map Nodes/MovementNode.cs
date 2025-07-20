using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    /// <summary>
    /// Represents a MapNode on which the player can move to with the click of a button 
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MovementNode: MapNode
    {
        private Button _button;
        public virtual void Initialize() {
            _button = GetComponent<Button>();
            _button.interactable = false;
            _button.onClick.AddListener(Select);
        }
        /// <summary>
        /// Attempt to move to this node when it is selected
        /// </summary>
        public virtual void Select() {
            MapManager.Instance.RequestMove(this);
        }

        public override IEnumerator OnArrive()
        {
            Debug.Log("Arrived at " + this.name);
            yield break;
        }

        public override void OnGenerate(List<MapNode> currentPath, List<List<MapNode>> allPaths)
        {
            Initialize();
        }

        public override IEnumerator OnLeave()
        {
            Debug.Log("Left " + this.name);
            yield break;
        }

        public override void OnMoveNearby()
        {
            _button.interactable = false;
        }

        public override void OnApproach()
        {
            _button.interactable = true;
        }
    }
}
