using UnityEngine;
using System.Collections.Generic;
using Globals;

namespace Map
{
    /// <summary>
    /// Script that manages the creation and storage of path nodes
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Map : MonoBehaviour
    {
        [SerializeField, Tooltip("Definition for generating the map")]
        public MapDefinition MapParameters;

        private List<List<MapNode>> _paths = null;

        private MapNode _start = null;
        private List<MapNode> _ends = null;
        private RectTransform rectTransform = null;
        private int _seed = 0;

        private void OnValidate()
        {
            if (_paths == null) {
                _paths = new List<List<MapNode>>();
            }
            if (!MapParameters) {
                Debug.LogError("Must have a valid MapDefinition for map generation");
            }
            else if (MapParameters.NodesToGenerate.Length <= 0) {
                Debug.LogError("Must have a defined set of nodes to generate in MapDefinition");
            }
            rectTransform = GetComponent<RectTransform>();
            Random.InitState(_seed);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            OnValidate();
        }

        /// <summary>
        /// Destroys the map
        /// </summary>
        public void DestroyMap() {
            Debug.Log("resetting map!");
            _paths.Clear();
            foreach (Transform t in this.transform) {
                Destroy(t);
            }
            System.GC.Collect();
        }

        /// <summary>
        /// Takes all the generated map nodes and displays them at the correct location
        /// </summary>
        public void DisplayMap() {
            if (_paths.Count <= 0) {
                Debug.LogError("Cannot display a map that does not exist yet!");
                return;
            }
            // calculate relevant positioning and noise variables
            float verticalGap = 1f / (_paths.Count + 1);
            // noise stuff
            Random.InitState(_seed);
            Vector2 noiseOffset = new Vector2(Random.Range(0, 1000), Random.Range(0, 1000));
            Vector2 maxNoiseDrift = new Vector2(1f / (MapParameters.maxPathLength + 2) / 2.2f, verticalGap / 2.2f);
            // positioning
            Rect bounds = rectTransform.rect;
            bounds.position = rectTransform.TransformPoint(bounds.position);

            // position each node on each path on the correct position
            for (int i = 0; i < _paths.Count; ++i) {
                List<MapNode> path = _paths[i];
                bool middlePath = i == _paths.Count / 2;
                for (int j = middlePath? 0: 1; j < _paths.Count - (middlePath? 0 : _ends.Count); ++j) {
                    MapNode node = path[j];
                    if (node.Single)
                    {
                        node.transform.position = bounds.min + new Vector2(node.distance * bounds.width, verticalGap * i * bounds.height);
                        Vector2 noisePosition = noiseOffset + Helpers.Vec3ToVec2(node.transform.position);
                        Vector2 noiseShift = new Vector2(Mathf.PerlinNoise1D(noisePosition.x) - 0.5f, Mathf.PerlinNoise1D(noisePosition.y) - 0.5f);
                        noisePosition = Vector2.Scale(maxNoiseDrift, noiseShift);
                        node.transform.position = new Vector3(noisePosition.x, noisePosition.y, node.transform.position.z);
                        continue;
                    }
                    // node with siblings - do all siblings at once
                    float splitVerticalGap = verticalGap / (node.Siblings + 1) / 2f;
                    float splitStartVerticalPosition = verticalGap * (i - 0.5f);
                    for (int k = 0; k < node.Siblings; ++k) {
                        node = path[j + k];
                        float splitVerticalPosition = (splitStartVerticalPosition + splitVerticalGap * k);
                        node.transform.position = bounds.min + new Vector2(node.distance * bounds.width,  splitVerticalPosition * bounds.height);
                        Vector2 noisePosition = noiseOffset + Helpers.Vec3ToVec2(node.transform.position);
                        Vector2 noiseShift = new Vector2(Mathf.PerlinNoise1D(noisePosition.x) - 0.5f, Mathf.PerlinNoise1D(noisePosition.y) - 0.5f);
                        noisePosition = Vector2.Scale(maxNoiseDrift / (node.Siblings + 1), noiseShift);
                        node.transform.position = new Vector3(noisePosition.x, noisePosition.y, node.transform.position.z);
                        ++j;
                    }
                }
            }

            // update each node's connecting lines
            foreach(List<MapNode> path in _paths) {
                foreach (MapNode node in path) {
                    node.ResetPathConnections();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seed"></param>
        public void GenerateMap(int seed = 0) {
            if (seed == 0) {
                seed = (int) System.DateTime.Now.Ticks;
            }
            _seed = seed;
            Random.InitState(seed);
            Debug.Log("Creating map with seed of " + seed.ToString());
            // clear the map
            DestroyMap();
            // create start and ending nodes
            _start = MakeChildCopy(MapParameters.startingNode);
            _start.distance = 0f;
            _ends = new List<MapNode>();
            foreach(MapNode endingNode in MapParameters.endingNode)
            {
                MapNode end = MakeChildCopy(endingNode);
                end.distance = 1f;
                _ends.Add(end);
            }
            // generate the nodes and the connections between them
            for (int i = 0; i < MapParameters.totalPaths; ++i) {
                _paths.Add(GeneratePath(_start, _ends));
            }
            // perform post generation updates for each node
            foreach (List<MapNode> path in _paths)
            {
                foreach (MapNode node in path)
                {
                    node.OnGenerate(path, _paths);
                }
            }
        }

        /// <summary>
        /// Generates a list of MapNode game objects based on the given map configuration for a single path
        /// </summary>
        /// <returns></returns>
        private List<MapNode> GeneratePath(MapNode start, List<MapNode> ends)
        {
            List<MapNode> path = new List<MapNode>();
            // ensure starting node is connected
            List<MapNode> previousNodes = new List<MapNode>();
            previousNodes.Add(start);
            path.Add(start);
            // generated the nodes for the path
            int nodesToGenerate = Random.Range(MapParameters.minPathLength, MapParameters.maxPathLength + 1);
            for (int i = 0; i < nodesToGenerate; ++i)
            {
                // need to add offset for index and total nodes to account for start and ending nodes
                GenerateNextNodesInPath(ref previousNodes, ref path, i + 1, nodesToGenerate + 2);
            }
            // connect all final nodes to the ending nodes
            ConnectNodes(previousNodes, ends, path);
            path.AddRange(ends);
            return path;
        }



        /// <summary>
        /// Appends and connects new nodes onto the path given the previous nodes that were added onto the path
        /// At the end, updates the previous nodes to what nodes were just added
        /// </summary>
        /// <param name="previousNodes"></param>
        /// <param name="path"></param>
        /// <param name="index">Current index of the nodes being generated out of the total nodes to generate</param>
        /// <param name="totalNodes">Total nodes to generate</param>
        private void GenerateNextNodesInPath(ref List<MapNode> previousNodes, ref List<MapNode> path, int index, int totalNodes)
        {
            float distance = index * 1f/totalNodes;
            List<MapNode> nextNodes = new List<MapNode>();
            // determine if we are splitting the next node (e.g. creating a branch)
            float splitChance = MapParameters.splitChance.Evaluate(distance);
            bool split = Random.value < splitChance;
            int splitNodeAmount = split ? Random.Range(MapParameters.minSplitAmount, MapParameters.maxSplitAmount + 1) : 1;
            for (int j = 0; j < splitNodeAmount; ++j)
            {
                MapNode nextNode;
                if (j < previousNodes.Count)
                {
                    nextNode = GenerateNextNode(previousNodes[j], path, MapParameters.NodesToGenerate);
                }
                else
                {
                    nextNode = GenerateNextNode(ListHelpers.RandomFromList(previousNodes), path, MapParameters.NodesToGenerate);
                }
                // determine if we connect this node to its neighbor
                if (nextNodes.Count > 0 && Random.value < MapParameters.splitConnectionChance.Evaluate(distance)) {
                    MapNode neighborNode = nextNodes[nextNodes.Count - 1];
                    MapConnection nodeConnection = CreateConnection(neighborNode, nextNode, path);
                    nextNode.ConnectTo(neighborNode, nodeConnection);
                }
                nextNodes.Add(nextNode);
            }
            foreach (MapNode node in nextNodes)
            {
                node.distance = distance;
                node.neighbors = nextNodes;
            }
            path.AddRange(nextNodes);
            ConnectNodes(previousNodes, nextNodes, path);
            previousNodes = nextNodes;

            // handle splitting again from the split nodes which adds additional intermediate nodes
            if (split) { SplitNextNodesInPath(ref previousNodes, ref path, index, totalNodes); }
        }

        /// <summary>
        /// Conducts any bonus splitting of nodes on the path
        /// </summary>
        /// <param name="previousNodes"></param>
        /// <param name="index"></param>
        /// <param name="totalNodes"></param>
        private void SplitNextNodesInPath(ref List<MapNode> previousNodes, ref List<MapNode> path, int index, int totalNodes)
        {
            int totalSplits = 1;
            List<MapNode> generatedNodes = new List<MapNode>(previousNodes);
            List<MapNode> newlyGeneratedNodes = new List<MapNode>();
            List<MapNode> nodeEndings = new List<MapNode>();

            float stepSize = 1f/totalNodes;
            float splitChance = MapParameters.splitChance.Evaluate(index * stepSize);
            while (totalSplits <= MapParameters.maxSplitDepth)
            {
                splitChance *= MapParameters.splitDecay;
                // attempt to split each of the following nodes
                foreach (MapNode node in generatedNodes)
                {
                    bool splitAgain = Random.value < splitChance;
                    if (splitAgain)
                    {
                        float distance = (index + (float)totalSplits / MapParameters.maxSplitDepth) * stepSize;
                        List<MapNode> splitAgainNodes = new List<MapNode>();
                        int splitNodeAmount = splitAgain ? Random.Range(MapParameters.minSplitAmount, MapParameters.maxSplitAmount + 1) : 1;
                        for (int k = 0; k < splitNodeAmount; ++k)
                        {
                            splitAgainNodes.Add(GenerateNextNode(node, path, MapParameters.NodesToGenerate));
                        }
                        foreach (MapNode bonusNode in splitAgainNodes)
                        {
                            bonusNode.bonus = true;
                            bonusNode.distance = distance;
                            bonusNode.neighbors = splitAgainNodes;
                            MapConnection nodeConnection = CreateConnection(node, bonusNode, path);
                            node.ConnectTo(bonusNode, nodeConnection);
                        }
                        newlyGeneratedNodes.AddRange(splitAgainNodes);
                        path.AddRange(splitAgainNodes);
                    }
                    else
                    {
                        nodeEndings.Add(node);
                    }
                }
                ++totalSplits;
                generatedNodes = newlyGeneratedNodes;
                if (generatedNodes.Count == 0) break;
            }
            nodeEndings.AddRange(newlyGeneratedNodes);
            previousNodes = nodeEndings;
        }

        /// <summary>
        /// Returns the next path node to generate based on a single previous node and the previous path
        /// </summary>
        /// <param name="previousNode"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private MapNode GenerateNextNode(MapNode previousNode, List<MapNode> path, MapNodePolicy[] weightedNodeChoices)
        {
            ListHelpers.GetWeight<MapNodePolicy> validWeightLambda = (MapNodePolicy p) => { return p.node.IsValid(previousNode, path) ? 0 : p.weight; };
            MapNodePolicy result = ListHelpers.WeightedRandomFromList(weightedNodeChoices, validWeightLambda);
            MapNode newNode;
            if (default(MapNodePolicy).Equals(result))
            {
                // if there was no valid node in the weighted random, then we just use a random node
                newNode = ListHelpers.WeightedRandomFromList(weightedNodeChoices, (MapNodePolicy p) => { return p.weight; }).node;
            }
            else
            {
                newNode = result.node;
            }
            newNode = Instantiate(newNode);
            newNode.transform.SetParent(this.transform);
            newNode.name = "Generated " + newNode.name;
            return newNode;
        }

        /// <summary>
        /// Creates a copy of the object as a child of this transform
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <returns></returns>
        private T MakeChildCopy<T>(T original) where T : MonoBehaviour
        {
            T copy = Instantiate(original);
            copy.transform.SetParent(this.transform);
            copy.name = "Generated " + copy.name;
            return copy;
        }

        /// <summary>
        /// Connects two sets of nodes in pairs
        /// If there is more than one current node, may create a connection between them based on 
        /// the MapDefinition policy.
        /// </summary>
        /// <param name="previousNodes"></param>
        /// <param name="currentNodes"></param>
        /// <param name="path"></param>
        private void ConnectNodes(List<MapNode> previousNodes, List<MapNode> currentNodes, List<MapNode> path)
        {
            if (previousNodes == null || previousNodes.Count == 0) return;
            if (currentNodes == null || currentNodes.Count == 0)
            {
                Debug.LogError("Attempted to connect to an empty set of nodes!");
                return;
            }
            List<MapNode> remainingNodes = new List<MapNode>(currentNodes);
            foreach (MapNode previousNode in previousNodes)
            {
                MapNode nextNode = null;
                if (remainingNodes.Count > 0)
                {
                    // Connect to valid corresponding unconnected node
                    List<MapNode> validNodes = GetValidNodes(previousNode, remainingNodes, path);
                    nextNode = validNodes.Count > 0 ? validNodes[0] : null;
                    if (nextNode == null)
                    {
                        nextNode = remainingNodes[0];
                    }
                    remainingNodes.Remove(nextNode);
                }
                else { 
                    // We have connected to all current nodes but there are still previous nodes that are unconnected

                    // if the previous node already connects to a neighbor we can ignore it with a certain probability (since that neighbor will connect back eventually)
                    if (previousNode.paths.Count > 0) {
                        if (Random.value < MapParameters.splitConnectionChance.Evaluate(previousNode.distance)) {
                            continue;
                        }
                    }

                    // if we don't ignore it, connect it to a random valid current node.
                    List<MapNode> validNodes = GetValidNodes(previousNode, currentNodes, path);
                    if (validNodes.Count > 0) {
                        nextNode = validNodes[Random.Range(0, validNodes.Count - 1)];
                    }
                    // if there are no valid nodes to connect this node to, then we will have to connect it randomly
                    if (nextNode == null) {
                        nextNode = ListHelpers.RandomFromList(currentNodes);
                    }
                }
                MapConnection nodeConnection = CreateConnection(previousNode, nextNode, path);
                previousNode.ConnectTo(nextNode, nodeConnection);
            }
            // if there are remaining nodes to connect to, then we simply connect a random valid previous to each remaining current node
            if (remainingNodes.Count > 0) {
                foreach (MapNode node in remainingNodes) {
                    List<MapNode> validNodes = GetValidNodesFrom(previousNodes, node, path);
                    MapNode prevNode = null;
                    if (validNodes.Count > 0) {
                        prevNode = validNodes[Random.Range(0, validNodes.Count - 1)];
                    }
                    // if there are no valid nodes to connect this node to, then we will have to connect it randomly
                    if (prevNode == null) {
                        prevNode = ListHelpers.RandomFromList(validNodes);
                    }
                    MapConnection nodeConnection = CreateConnection(prevNode, node, path);
                    prevNode.ConnectTo(node, nodeConnection);
                }
            }
        }

        /// <summary>
        /// Creates the connection between two nodes.
        /// </summary>
        /// <param name="previousNode"></param>
        /// <param name="currentNode"></param>
        /// <returns></returns>
        private MapConnection CreateConnection(MapNode previousNode, MapNode currentNode, List<MapNode> path) {
            MapConnection nodeConnection = Instantiate(MapParameters.BaseNodeConnector);
            nodeConnection.transform.SetParent(this.transform);
            // an invalid connection should rarely happen, but if it does we make the player say something to acknowledge it
            if (!currentNode.IsValid(previousNode, path)) { 
                nodeConnection.TravelMessages.Add("I don't remember this path being here...");
            }
            return nodeConnection;
        }

        /// <summary>
        /// Returns the set of possible next nodes from a set given the current node and previous path
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="possibleNextNodes"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<MapNode> GetValidNodes(MapNode currentNode, List<MapNode> possibleNextNodes, List<MapNode> path) {
            List<MapNode> validNodes = new List<MapNode>();
            foreach (MapNode node in possibleNextNodes) {
                if (node.IsValid(currentNode, path)) {
                    validNodes.Add(node);
                }
            }
            return validNodes;
        }

        /// <summary>
        /// Returns the set of possible originating nodes from a set given the current node and previous path
        /// </summary>
        /// <param name="currentNodes"></param>
        /// <param name="nextNode"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<MapNode> GetValidNodesFrom(List<MapNode> currentNodes, MapNode nextNode, List<MapNode> path)
        {
            List<MapNode> validNodes = new List<MapNode>();
            foreach (MapNode node in currentNodes) {
                if (nextNode.IsValid(node, path)) {
                    validNodes.Add(node);
                }
            }
            return validNodes;
        }
    }
}
