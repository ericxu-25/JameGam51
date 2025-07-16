using UnityEngine;
using System.Collections.Generic;

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
            path.line.SetPosition(0, start.transform.position);
            path.line.SetPosition(1, end.transform.position);
        }
    }

    /// <summary>
    /// Class which manages each map node and the connections between it
    /// The base class is intended for nodes where nothing is triggered.
    /// </summary>
    public class MapNode: MonoBehaviour
    {
        // Instance specific values

        [HideInInspector, Tooltip("Paths one can travel from this node")]
        public List<MapPath> paths = null;

        /// <summary>
        /// distance of this node from the start from 0.0 to 1.0
        /// </summary>
        [HideInInspector]
        public float distance = 0f;

        /// <summary>
        /// adjacent neighbors of the node on the same path (including itself)
        /// Will be just itself if it is a single node
        /// </summary>
        [HideInInspector]
        public List<MapNode> neighbors = null;

        public bool Single { get { return neighbors != null && neighbors.Count == 1; } }

        /// <summary>
        /// Whether this node is a bonus node or not (created by splitting twice)
        /// </summary>
        [HideInInspector]
        public bool bonus = false;
        public bool Bonus { get { return bonus; } }


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
            paths.Add(path);
        }

        public void ResetPathConnections() {
            for(int i = 0; i < paths.Count; ++i) {
                paths[i].SyncPath();
            }
        }
    }
}
