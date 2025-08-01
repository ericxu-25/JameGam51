using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    /// <summary>
    /// Class which manages the player on each map
    /// </summary>
    public class MapManager : Singleton.PersistentSingleton<MapManager>
    {
        [SerializeField, Tooltip("Set of maps in order of traversal")]
        Map[] maps;

        [SerializeField, Tooltip("Method(s) to call once we attempt to move out of the final map")]
        UnityEvent OnLeaveLastMap;

        [Header("Map Display Settings")]
        [SerializeField, Tooltip("Horizontal and vertical padding to use around the borders of the map when generating nodes.")]
        private Vector2 Padding = new Vector2(40f, 20f);

        [SerializeField, Tooltip("Canvas to display the map on and parent nodes to when displaying")]
        private RectTransform MapRoot;

        [Header("Travel Settings")]

        [SerializeField, Tooltip("Transform to move from one map node to the other")]
        private Transform player;

        [SerializeField, Tooltip("Time it takes to move from one node to the other")]
        private float movementTime = 0.2f;

        [SerializeField, Tooltip("Allow backtracking?")]
        private bool allowBacktracking = false;

        // path traveling information
        private Map currentMap;
        private int _currentMapIndex;
        private bool allowMovement = true;
        private MapNode currentNode = null;
        private MapNode previousNode = null;

        // flags
        private bool _moving = false;
        private bool _paused = false;
        public bool PauseMovement { get { return _paused; } set { _paused = value; } }

        // Interface used by MapNodes
        public bool CanMove { get { return allowMovement; } set { allowMovement = value; } }
        public bool CurrentlyMoving { get { return _moving; } set { _moving = value; } }
        public Transform Player { get { return player; } }

        /// <summary>
        /// Called when the player wants to move to a node.
        /// </summary>
        /// <returns> Whether the movement was accepted </returns>
        public bool RequestMove(MapNode nextNode){
            if (CanMoveTo(nextNode, out MapPath path))
            {
                if (path.Equals(default(MapPath))) {
                    Debug.LogWarning("Moving to " + nextNode.name + " from a null node!");
                    StartCoroutine(MoveTo(nextNode, null, NoAnimation));
                    return true;
                }
                Debug.Log("Movement to node " + nextNode.name + " accepted.");
                StartCoroutine(MoveTo(nextNode, path.path, DefaultMoveAnimation));
                return true;
            }
            Debug.Log("Movement to node " + nextNode.name + " rejected.");
            return false;
        }

        /// <summary>
        /// Returns if we can move from the current node to the next node
        /// Also outputs the path from the current node to the next node
        /// </summary>
        /// <param name="nextNode"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool CanMoveTo(MapNode nextNode, out MapPath path) {
            path = default(MapPath);
            if (nextNode == null) return false;
            if (nextNode == currentNode) {
                Debug.Log("Checked movement to " + nextNode.name + " but player is already at that node.");
                return false;
            }
            if (!CanMove)
            {
                Debug.Log("Checked movement to " + nextNode.name + " but movement is disabled.");
                return false;
            }
            if (_moving) { 
                Debug.Log("Checked movement to " + nextNode.name + " but movement is temporarily disabled.");
                return false;
            }
            if (currentNode == null)
            {
                Debug.Log("Moving to " + nextNode.name + " from a null node!");
                return true;
            }
            bool nextNodeConnected = false;
            if (currentNode.paths == null)
            {
                Debug.Log("Checked movement from " + currentNode.name + " but it is a dead end.");
                if (!allowBacktracking) return false;
            }
            else
            {
                foreach (MapPath fromPath in currentNode.paths)
                {
                    if (fromPath.end == nextNode)
                    {
                        nextNodeConnected = true;
                        path = fromPath;
                        break;
                    }
                }
            }
            if (allowBacktracking && !nextNodeConnected) {
                if (nextNode.paths == null) { 
                    Debug.Log("Checked movement to " + nextNode.name + " but it is a dead end.");
                    return false;
                }
                foreach (MapPath toPath in nextNode.paths)
                {
                    if (toPath.end == currentNode)
                    {
                        nextNodeConnected = true;
                        path = toPath;
                        break;
                    }
                }
            }
            if (!nextNodeConnected)
            {
                Debug.Log("Checked movement to " + nextNode.name + " but it is not connected to the current node.");
                return false;
            }
            Debug.Log("Checked movement to " + nextNode.name + " accepted.");
            return true;
        }

        /// <summary>
        /// Called when the player wants to move to the next map.
        /// </summary>
        public void RequestNextMap(){
            if (_currentMapIndex + 1 < maps.Length) {
                Debug.Log("Moving to the next map.");
                MoveToMap(_currentMapIndex + 1);
            }
            else
            {
                // we've reached the last map! 
                if (OnLeaveLastMap == null) return;
                Debug.Log("Reached the end of the last map!");
                OnLeaveLastMap?.Invoke();
            }
        }

        private bool _mapHidden = false;
        private bool _previousCanMove;
        public bool IsMapHidden { get { return _mapHidden; } }

        /// <summary>
        /// Called to hide the current map and temporarily disable movement
        /// </summary>
        public void HideCurrentMap() {
            if (_mapHidden) return;
            Debug.Log("Hiding current map");
            currentMap.HideMap();
            CanMove = false;
            _mapHidden = true;
            _previousCanMove = true;
        }

        /// <summary>
        /// Called to show the current map and restore movement state
        /// </summary>
        public void ShowCurrentMap() {
            if (!_mapHidden) return;
            Debug.Log("Showing current map");
            currentMap.DisplayMap();
            _mapHidden = false;
            CanMove = _previousCanMove;
        }

        // movement animations - handle moving the player on a connection between two nodes
        private delegate IEnumerator MovementAnimation(MapNode startingNode, MapNode endingNode, MapConnection connection);
        IEnumerator NoAnimation(MapNode startingNode, MapNode endingNode, MapConnection connection) {
            if (connection != null) yield return connection.OnTravel(1.0f);
            player.localPosition = endingNode.transform.localPosition;
            yield return null;
        }
        IEnumerator DefaultMoveAnimation(MapNode startingNode, MapNode endingNode, MapConnection connection) {
            if (startingNode == null) {
                Debug.LogError("Attempted default move animation on a null node!");
                yield return new WaitForSeconds(movementTime);
                player.localPosition = endingNode.transform.localPosition;
                yield break;
            }
            // moves in a straight line from one node to the other
            Vector3 distance = endingNode.transform.localPosition - startingNode.transform.localPosition;
            player.localPosition = startingNode.transform.localPosition;
            float traveledDistance = 0f;
            while (traveledDistance < distance.magnitude) {
                if (movementTime <= Time.fixedDeltaTime) {
                    player.localPosition += distance;
                    yield break;
                }
                if (connection != null) yield return connection.OnTravel(traveledDistance/distance.magnitude);
                Vector3 frameDistance = distance * Time.fixedDeltaTime / movementTime;
                player.localPosition += frameDistance;
                traveledDistance += frameDistance.magnitude;
                yield return new WaitUntil(() => !PauseMovement);
                yield return new WaitForFixedUpdate();
            }
            if (traveledDistance > distance.magnitude) {
                player.localPosition -= (traveledDistance - distance.magnitude) * distance.normalized;
                yield return connection.OnTravel(1.0f);
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (maps == null || maps.Length == 0) {
                Debug.LogWarning("There are no maps in MapManager!");
                return;
            }
            // initialize and generate maps at start
            foreach (Map map in maps) {
                map.Padding = Padding;
                map.MapRoot = MapRoot;
                map.Initialize();
                map.GenerateMap();
                map.HideMap();
            }
            // setup first map and starting node
            MoveToMap(0);
        }

        /// <summary>
        /// Moves the player onto a new map
        /// </summary>
        /// <param name="mapIndex"></param>
        void MoveToMap(int mapIndex) {
            Debug.Log("Moving to map #" + mapIndex.ToString());
            if (mapIndex >= maps.Length) {
                Debug.LogError("Out of bounds map movement");
                return;
            }
            Map map = maps[mapIndex];
            if (currentMap != null) {
                currentMap.HideMap();
            }
            _currentMapIndex = mapIndex;
            currentMap = maps[mapIndex];
            currentMap.DisplayMap();
            currentNode = currentMap.StartingNode;
            player.SetParent(map.Root.transform);
            StartCoroutine(MoveTo(currentNode, null, NoAnimation));
        }

        /// <summary>
        /// Moves from the current node to some next node on some connection 
        /// </summary>
        IEnumerator MoveTo(MapNode nextNode, MapConnection connection, MovementAnimation movement) {
            if (_moving)
            {
                yield return new WaitUntil(() => !_moving);
            }
            if (nextNode == null)
            {
                Debug.LogError("Attempted to move onto a nonexistant node!");
                yield break;
            }
            _moving = true;
            // call OnMoveNearby for any neighboring nodes to the current one
            if (currentNode && currentNode.paths != null) {
                foreach (MapPath path in currentNode.paths) {
                    path.end.OnMoveNearby();
                }
            }
            if (currentNode && currentNode.backPaths != null)
            {
                foreach (MapPath path in currentNode.backPaths)
                {
                    path.start.OnMoveNearby();
                }
            }
            // do the movement
            previousNode = currentNode;
            currentNode = nextNode;
            if(previousNode != null) yield return previousNode.OnLeave();
            // handle starting to travel on the connection 
            if (connection != null) {
                bool backtracking = allowBacktracking;
                if (backtracking) {
                    foreach (MapPath path in previousNode.paths) {
                        if (path.path == connection) {
                            backtracking = false;
                            break;
                        }
                    }
                }
                connection.OnTravelStart(backtracking);
            }
            // play animation for moving
            yield return nextNode.OnMoveTowards();
            yield return movement(previousNode, currentNode, connection);
            yield return currentNode.OnArrive();
            // call OnApproach for any neighboring nodes
            if (currentNode.paths != null) {
                foreach (MapPath path in currentNode.paths) {
                    path.end.OnApproach();
                }
            }
            if (allowBacktracking && currentNode.backPaths != null)
            {
                foreach (MapPath path in currentNode.backPaths)
                {
                    path.start.OnApproach();
                }
            }
            _moving = false;
        }
    }
}
