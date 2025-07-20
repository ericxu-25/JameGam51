using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Map
{
    public struct MapPath {
        public MapPath(MapNode start, MapNode end, MapConnection path) {
            this.start = start;
            this.end = end;
            this.path = path;
        }
        public MapNode start;
        public MapNode end;
        public MapConnection path;
        /// <summary>
        /// Syncs the current MapConnection line start and end points
        /// As a side effect, regresses each line into a straight one.
        /// </summary>
        public void SyncPath() { 
            path.line.positionCount = 2;
            path.line.useWorldSpace = false;
            path.line.SetPosition(0, path.transform.InverseTransformPoint(start.transform.position));
            path.line.SetPosition(1, path.transform.InverseTransformPoint(end.transform.position));
        }
    }

    /// <summary>
    /// Class which manages each map node and the connections between it
    /// </summary>
    public abstract class MapNode: MonoBehaviour
    {
        // Instance specific values

        [HideInInspector, Tooltip("Paths one can travel from this node")]
        public List<MapPath> paths = null;

        [HideInInspector, Tooltip("Paths which travel to this node")]
        public List<MapPath> backPaths = null;

        /// <summary>
        /// distance of this node from the start from 0.0 to 1.0
        /// </summary>
        [HideInInspector]
        public float distance = 0f;

        /// <summary>
        /// Position of the node on the path
        /// </summary>
        [HideInInspector]
        public int index = 0;

        /// <summary>
        /// adjacent neighbors of the node on the same path at the same index (including itself)
        /// Will be just itself if it is a single node, more if a split node
        /// </summary>
        [HideInInspector]
        public List<MapNode> neighbors = null;

        /// <summary>
        /// set of nodes which connect to this node
        /// </summary>
        [HideInInspector]
        public List<MapNode> fromNodes = null;

        public bool Single { get { return neighbors == null || neighbors.Count <= 1; } }
        public bool ConnectedToNeighbor
        {
            get {
                if (Single) return false;
                foreach (MapPath connection in paths) {
                    if (neighbors.Contains(connection.end)) return true;
                }
                return false;
            }
        }

        public int Siblings { get { return (Single ? 0 : neighbors.Count - 1); } }

        /// <summary>
        /// What bonus split depth this node is on
        /// </summary>
        [HideInInspector]
        public int bonus = 0;

        public bool IsBonus { get { return bonus > 0; } }

        /// <summary>
        /// If this is a hidden node
        /// </summary>
        [HideInInspector]
        public bool hidden = false;

        /// <summary>
        /// Returns if this node is a valid addition as the next node to the current path
        /// </summary>
        /// <param name="previousNode"></param>
        /// <param name="currentPath"></param>
        /// <returns></returns>
        public virtual bool IsValid(MapNode previousNode, List<MapNode> currentPath) {
            if (previousNode == null) return true;
            return true;
        }

        /// <summary>
        /// Connects this node to the next with a given map connection
        /// </summary>
        /// <param name="nextNode"></param>
        /// <param name="connection"></param>
        public void ConnectTo(MapNode nextNode, MapConnection connection) {
            MapPath path = new MapPath(this, nextNode, connection);
            path.SyncPath();
            if (paths == null) {
                paths = new List<MapPath>();
            }
            if (nextNode.backPaths == null) {
                nextNode.backPaths = new List<MapPath>();
            }
            paths.Add(path);
            nextNode.backPaths.Add(path);
            path.path.transform.SetParent(this.transform, true);
            if (nextNode.fromNodes == null) nextNode.fromNodes = new List<MapNode>();
            nextNode.fromNodes.Add(this);
        }

        public void ResetPathConnections() {
            if (paths == null) return;
            for(int i = 0; i < paths.Count; ++i) {
                paths[i].SyncPath();
            }
        }

        /// <summary>
        /// Called on each node right after it is generated.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="allPaths"></param>
        public abstract void OnGenerate(List<MapNode> currentPath, List<List<MapNode>> allPaths);

        /// <summary>
        /// Called when the player starts to move while adjacent to this node
        /// </summary>
        public abstract void OnMoveNearby();

        /// <summary>
        /// Called when the player finishes movement and can move to this node from the current node 
        /// </summary>
        public abstract void OnApproach();

        /// <summary>
        /// Called when the player arrives at this node while moving 
        /// </summary>
        public abstract IEnumerator OnArrive();

        /// <summary>
        /// Called when the player leaves this node while moving
        /// </summary>
        public abstract IEnumerator OnLeave();

    }
}
