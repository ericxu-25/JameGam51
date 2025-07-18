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
        [SerializeField, Tooltip("Definitions for generating the map. If multiple definitions are used, then each generated definition will be chained together")]
        public MapDefinition MapParameters;

        private List<List<MapNode>> _paths = null;

        private MapNode _start = null;
        private List<MapNode> _ends = null;
        private RectTransform rectTransform = null;

        [SerializeField, Tooltip("Random seed used in map generation. Set to 0 to generate a new one")]
        private int seed = 0;

        [SerializeField, Tooltip("Whether to use the preset seed or not")]
        private bool useSeed = false;
        public int Seed { get { return seed; } set { seed = value; } }

        private void OnValidate()
        {
            if (_paths == null)
            {
                _paths = new List<List<MapNode>>();
            }
            if (!MapParameters)
            {
                Debug.LogError("Must have a valid MapDefinition for map generation");
            }
            else if (MapParameters.NodesToGenerate.Length <= 0)
            {
                Debug.LogError("Must have a defined set of nodes to generate in MapDefinition");
            }
            rectTransform = GetComponent<RectTransform>();
            if (seed == 0)
            {
                seed = Random.Range(0, Mathf.Abs((int)System.DateTime.Now.Ticks));
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            OnValidate();
        }

        /// <summary>
        /// Destroys the map
        /// </summary>
        /// <param name="immediate"></param>
        public void DestroyMap(bool immediate = false)
        {
            Debug.Log("resetting map!");
            _paths.Clear();
            foreach (Transform t in this.transform)
            {
                if (immediate)
                {
                    DestroyImmediate(t.gameObject, false);
                    continue;
                }
                Destroy(t.gameObject);
            }
            System.GC.Collect();
        }

        /// <summary>
        /// Destroys the map
        /// </summary>
        [ContextMenu("DestroyMap")]
        public void DestroyMap()
        {
            DestroyMap(immediate: false);
        }

        /// <summary>
        /// Takes all the generated map nodes and displays them left to right
        /// </summary>
        [ContextMenu("DisplayMap")]
        public void DisplayMap()
        {
            if (_paths.Count <= 0)
            {
                Debug.LogError("Cannot display a map that does not exist yet!");
                return;
            }
            // calculate relevant positioning and noise variables
            float verticalGap = 1f / (_paths.Count + 1);
            // noise stuff
            if(useSeed) Random.InitState(seed);
            Vector2 noiseOffset = new Vector2(Random.Range(0, 1000), Random.Range(0, 1000));
            Vector2 maxNoiseDrift = Vector2.zero;
            // positioning
            Rect bounds = rectTransform.rect;
            Debug.Log("Displaying map with size: " + bounds.size.ToString() + " and " + _paths.Count.ToString() + " paths");
            Debug.Log("Map bottom left corner: " + bounds.min.ToString());
            // position each node on each path on the correct position
            for (int i = 0; i < _paths.Count; ++i)
            {
                List<MapNode> path = _paths[i];
                if (path.Count == 0) continue;
                // only draw start and end nodes on the middle path
                bool middlePath = (i == Mathf.FloorToInt(_paths.Count / 2));
                int pathEnd = path.Count - (middlePath ? 0 : _ends.Count);
                if (middlePath) Debug.Log("Generating middle path");
                int sliceIndex = 0;
                int sliceSubIndex = 0;
                List<MapNode> slice = new List<MapNode>();
                MapNode node = path[0];
                for (int j = middlePath ? 0 : 1; j <= pathEnd; ++j)
                {
                    if (j != pathEnd)
                    {
                        node = path[j];
                        Debug.Log(j.ToString() + " At index: " + node.index.ToString() + (node.IsBonus? ("B" + node.bonus.ToString()) : "") + " which is " + path[j].name);
                        // group nodes together into slices based on their index and subindex (bonus level)
                        if (node.index == sliceIndex && node.bonus == sliceSubIndex)
                        {
                            slice.Add(node);
                            if (j != pathEnd - 1) continue; // don't skip if last node
                        }
                        else
                        {
                            sliceIndex = node.index;
                            sliceSubIndex = node.bonus;
                        }
                    }
                    // reached the end or a node in a different slice
                    if (slice.Count == 0) {
                        // if nothing in the slice, just continue
                        slice.Add(node);
                        continue;
                    }
                    // before starting the next slice, draw all the nodes in the current slice in order
                    float splitVerticalGap = verticalGap / slice.Count;
                    maxNoiseDrift.y = splitVerticalGap / 2f;
                    maxNoiseDrift.x = slice[0].distance - node.distance;
                    for (int k = 0; k < slice.Count; ++k) {
                        MapNode sliceNode = slice[k];
                        float verticalPosition = verticalGap * (i + 0.5f) + splitVerticalGap * k;
                        sliceNode.transform.localPosition = bounds.min + new Vector2(sliceNode.distance * bounds.width, verticalPosition * bounds.height);
                        Vector2 noisePosition = noiseOffset + Helpers.Vec3ToVec2(sliceNode.transform.localPosition);
                        float noiseShift = Mathf.PerlinNoise(noisePosition.x, noisePosition.y);
                        noisePosition = MapParameters.nodeNoisiness.Evaluate(sliceNode.distance) * noiseShift * noiseShift * maxNoiseDrift;
                        noisePosition = Vector2.Scale(noisePosition, bounds.size);
                        noisePosition = Helpers.Vec3ToVec2(sliceNode.transform.localPosition) + noisePosition;
                        sliceNode.transform.localPosition = new Vector3(noisePosition.x, noisePosition.y, sliceNode.transform.localPosition.z);
                        Debug.Log(sliceNode.name + " position: " + sliceNode.transform.localPosition);
                    }
                    // update the next slice
                    slice.Clear();
                    if(j != pathEnd) slice.Add(node);
                }
            }

            // update each node's connecting lines
            foreach (List<MapNode> path in _paths)
            {
                foreach (MapNode node in path)
                {
                    node.ResetPathConnections();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seed"></param>
        [ContextMenu("GenerateMap")]
        public void GenerateMap()
        {
            if(useSeed) Random.InitState(seed);
            Debug.Log("Creating map with seed of " + seed.ToString());
            // clear the map
            DestroyMap();
            // create start and ending nodes
            _start = MakeChildCopy(MapParameters.startingNode, prefix: "Starting");
            _start.distance = 0f;
            _start.index = 0;
            _ends = new List<MapNode>();
            foreach (MapNode endingNode in MapParameters.endingNodes)
            {
                MapNode end = MakeChildCopy(endingNode, prefix: "End");
                end.distance = 1f;
                end.index = MapParameters.maxPathLength + 2;
                _ends.Add(end);
            }
            foreach (MapNode endingNode in _ends)
            {
                endingNode.neighbors = _ends;
            }
            if (_ends.Count == 0)
            {
                Debug.LogError("MapParameters lack defined ending nodes!");
            }
            // generate the nodes and the connections between them
            for (int i = 0; i < MapParameters.totalPaths; ++i)
            {
                _paths.Add(GeneratePath(_start, _ends));
            }
            // perform post generation updates for each node
            // while also tallying path info
            int totalNodes = 0;
            int totalBonusNodes = 0;
            int totalConnections = 0;
            foreach (List<MapNode> path in _paths)
            {
                for (int j = 0; j < path.Count; ++j)
                {
                    MapNode node = path[j];
                    if (node.IsBonus) ++totalBonusNodes;
                    if (j == 0 && totalNodes != 0) { continue; } // only count start node once
                    ++totalNodes;
                    if (node.paths != null) totalConnections += node.paths.Count;
                }
            }
            // remove duplicate ending nodes
            totalNodes -= (_paths.Count - 1) * MapParameters.endingNodes.Length;
            Debug.Log("Created " + _paths.Count.ToString()
                + " paths with " + totalNodes.ToString() + " nodes, "
                + totalBonusNodes.ToString() + " bonus nodes and "
                + totalConnections.ToString() + " connections.");
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
            float distance = index * 1f / totalNodes;
            List<MapNode> nextNodes = new List<MapNode>();
            // determine if we are splitting the next node (e.g. creating a branch)
            float splitChance = MapParameters.splitChance.Evaluate(distance);
            bool split = Random.value < splitChance;
            int splitNodeAmount = split ? Random.Range(MapParameters.minSplitAmount, MapParameters.maxSplitAmount + 1) + 1 : 1;
            for (int j = 0; j < splitNodeAmount; ++j)
            {
                MapNode nextNode;
                if (j < previousNodes.Count)
                {
                    nextNode = GenerateNextNode(previousNodes[j], path, MapParameters.NodesToGenerate, index.ToString());
                }
                else
                {
                    nextNode = GenerateNextNode(ListHelpers.RandomFromList(previousNodes), path, MapParameters.NodesToGenerate, index.ToString());
                }
                // determine if we connect this node to its neighbor
                if (nextNodes.Count > 0 && Random.value < MapParameters.splitConnectionChance.Evaluate(distance))
                {
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
                node.index = index;
            }
            path.AddRange(nextNodes);
            ConnectNodes(previousNodes, nextNodes, path);
            previousNodes = nextNodes;

            // handle splitting again from the split nodes which adds additional intermediate nodes
            if (split) { SplitBonusNodesInPath(ref previousNodes, ref path, index, totalNodes); }
        }

        /// <summary>
        /// Conducts any bonus splitting of nodes on the path
        /// </summary>
        /// <param name="previousNodes"></param>
        /// <param name="index"></param>
        /// <param name="totalNodes"></param>
        private void SplitBonusNodesInPath(ref List<MapNode> previousNodes, ref List<MapNode> path, int index, int totalNodes)
        {
            int splitDepth = 1;
            List<MapNode> generatedNodes = new List<MapNode>(previousNodes);
            List<MapNode> newlyGeneratedNodes = new List<MapNode>();
            List<MapNode> nodeEndings = new List<MapNode>();

            float stepSize = 1f / totalNodes;
            float splitChance = MapParameters.splitChance.Evaluate(index * stepSize);
            while (splitDepth <= MapParameters.maxSplitDepth)
            {
                int totalSplits = 0;
                splitChance *= MapParameters.splitDecay;
                // attempt to split each of the following nodes
                foreach (MapNode node in generatedNodes)
                {
                    bool splitAgain;
                    if (totalSplits >= MapParameters.maxBonusSplits) { splitAgain = false; }
                    else { splitAgain = Random.value < splitChance; }
                    if (splitAgain)
                    {
                        ++totalSplits;
                        float distance = (index + ((float)splitDepth) / (MapParameters.maxSplitDepth + 1)) * stepSize;
                        List<MapNode> splitAgainNodes = new List<MapNode>();
                        int splitNodeAmount = splitAgain ? Random.Range(MapParameters.minSplitAmount, MapParameters.maxSplitAmount + 1) + 1 : 1;
                        for (int k = 0; k < splitNodeAmount; ++k)
                        {
                            MapNodePolicy[] generationPolicy;
                            if (MapParameters.BonusNodesToGenerate == null || MapParameters.BonusNodesToGenerate.Length == 0)
                            {
                                generationPolicy = MapParameters.NodesToGenerate;
                            }
                            else
                            {
                                generationPolicy = MapParameters.BonusNodesToGenerate;
                            }
                            splitAgainNodes.Add(GenerateNextNode(node, path, generationPolicy, index.ToString() + "B" + splitDepth.ToString()));
                        }
                        foreach (MapNode bonusNode in splitAgainNodes)
                        {
                            bonusNode.bonus = totalSplits;
                            bonusNode.distance = distance;
                            bonusNode.neighbors = splitAgainNodes;
                            bonusNode.index = index;
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
                ++splitDepth;
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
        /// <param name="prefix"></param>
        /// <returns></returns>
        private MapNode GenerateNextNode(MapNode previousNode, List<MapNode> path, MapNodePolicy[] weightedNodeChoices, string prefix = null)
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
            newNode = MakeChildCopy(newNode, prefix);
            return newNode;
        }

        /// <summary>
        /// Creates a copy of the object as a child of this transform
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private T MakeChildCopy<T>(T original, string prefix = null) where T : MonoBehaviour
        {
            T copy = Instantiate(original, this.transform);
            if (prefix != null)
            {
                copy.name = prefix + " " + original.name;
            }
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
            List<MapNode> detouredPreviousNodes = new List<MapNode>();
            List<MapNode> undetouredPreviousNodes = new List<MapNode>();
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
                else
                {
                    // We have connected to all current nodes but there are still previous nodes that are unconnected

                    // if the previous node already connects to a neighbor we can ignore it with a certain probability (since that neighbor will connect back eventually)
                    if (previousNode.paths != null && previousNode.paths.Count > 0)
                    {
                        if (Random.value < MapParameters.detourChance.Evaluate(previousNode.distance))
                        {
                            detouredPreviousNodes.Add(previousNode);
                            continue;
                        }
                    }

                    // if we don't ignore it, connect it to a random valid current node.
                    List<MapNode> validNodes = GetValidNodes(previousNode, currentNodes, path);
                    if (validNodes.Count > 0)
                    {
                        nextNode = ListHelpers.RandomFromList(validNodes);
                    }
                    // if there are no valid nodes to connect this node to, then we will have to connect it randomly
                    if (nextNode == null)
                    {
                        nextNode = ListHelpers.RandomFromList(currentNodes);
                    }
                }
                undetouredPreviousNodes.Add(previousNode);
                MapConnection nodeConnection = CreateConnection(previousNode, nextNode, path);
                previousNode.ConnectTo(nextNode, nodeConnection);
            }
            // if there are remaining nodes to connect to, then we simply connect a random valid previous to each remaining current node
            if (remainingNodes.Count > 0)
            {
                foreach (MapNode node in remainingNodes)
                {
                    List<MapNode> validNodes = GetValidNodesFrom(undetouredPreviousNodes, node, path);
                    MapNode prevNode;
                    if (validNodes.Count > 0)
                    {
                        prevNode = ListHelpers.RandomFromList(validNodes);
                    }
                    else
                    {
                        // if there are no valid nodes in the undetoured set, then we will have to check from the detoured set
                        validNodes = GetValidNodesFrom(detouredPreviousNodes, node, path);
                        prevNode = ListHelpers.RandomFromList(validNodes);
                        // if there are no valid nodes in that set too, then we choose a random, preferring the undetoured set
                        if (validNodes.Count == 0)
                        {
                            if (undetouredPreviousNodes.Count == 0)
                                prevNode = ListHelpers.RandomFromList(detouredPreviousNodes);
                            else
                                prevNode = ListHelpers.RandomFromList(undetouredPreviousNodes);
                        }
                        else
                        {
                            prevNode = ListHelpers.RandomFromList(validNodes);
                        }
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
        private MapConnection CreateConnection(MapNode previousNode, MapNode currentNode, List<MapNode> path)
        {
            MapConnection nodeConnection = MakeChildCopy(MapParameters.BaseNodeConnector);
            // an invalid connection should rarely happen, but if it does we make the player say something to acknowledge it
            if (!currentNode.IsValid(previousNode, path))
            {
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
        private List<MapNode> GetValidNodes(MapNode currentNode, List<MapNode> possibleNextNodes, List<MapNode> path)
        {
            List<MapNode> validNodes = new List<MapNode>();
            foreach (MapNode node in possibleNextNodes)
            {
                if (node.IsValid(currentNode, path))
                {
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
            foreach (MapNode node in currentNodes)
            {
                if (nextNode.IsValid(node, path))
                {
                    validNodes.Add(node);
                }
            }
            return validNodes;
        }
    }
}
