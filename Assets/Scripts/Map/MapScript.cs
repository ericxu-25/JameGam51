using UnityEngine;
using System.Collections.Generic;
using Globals;

namespace Map
{
    /// <summary>
    /// Script that manages the creation and storage of path nodes
    /// </summary>
    public class MapScript : MonoBehaviour
    {
        [SerializeField, Tooltip("Definition for generating the map")]
        public MapDefinition MapParameters;

        private List<List<MapNode>> _paths = null;

        private void OnValidate()
        {
            if (_paths == null) {
                _paths = new List<List<MapNode>>();
            }
            if (MapParameters.NodesToGenerate.Length <= 0) {
                Debug.LogError("Must have a defined set of nodes to generate in MapDefinition");
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            OnValidate();
        }

        /// <summary>
        /// Generates the entire map with nodes
        /// </summary>
        public void GenerateMap() {
            // generate the nodes and the connections between them
            for (int i = 0; i < MapParameters.totalPaths; ++i) {
                _paths.Add(GeneratePath());
            }
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
        /// Generates a list of MapNode game objects based on the given map configuration for a single path
        /// </summary>
        /// <returns></returns>
        private List<MapNode> GeneratePath()
        {
            List<MapNode> previousNodes = new List<MapNode>();
            List<MapNode> path = new List<MapNode>();
            // generated the nodes for the path
            int nodesToGenerate = Random.Range(MapParameters.minPathLength, MapParameters.maxPathLength + 1);
            float stepSize = 1f / nodesToGenerate;
            for (int i = 0; i < nodesToGenerate; ++i)
            {
                GenerateNextNodesInPath(ref previousNodes, ref path, i, nodesToGenerate);
            }
            return path;
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
            return newNode;
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
                if (remainingNodes.Count > 0)
                {
                    // Connect to valid corresponding unconnected node
                    List<MapNode> validNodes = GetValidNodes(previousNode, remainingNodes, path);
                    MapNode nextNode = validNodes.Count > 0 ? validNodes[0] : null;
                    if (nextNode == null)
                    {
                        nextNode = remainingNodes[0];
                    }
                    remainingNodes.Remove(nextNode);
                    MapConnection nodeConnection = CreateConnection(previousNode, nextNode, path);
                    previousNode.ConnectTo(nextNode, nodeConnection);
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
                    MapNode nextNode = null;
                    if (validNodes.Count > 0) {
                        nextNode = validNodes[Random.Range(0, validNodes.Count - 1)];
                    }
                    // if there are no valid nodes to connect this node to, then we will have to connect it randomly
                    if (nextNode == null) {
                        nextNode = ListHelpers.RandomFromList(currentNodes);
                    }
                    MapConnection nodeConnection = CreateConnection(previousNode, nextNode, path);
                    previousNode.ConnectTo(nextNode, nodeConnection);
                }
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
            // an invalid connection should rarely happen, but if it does we make the player say something to acknowledge it
            if (!currentNode.IsValid(previousNode, path)) { 
                nodeConnection.TravelMessages.Add("I don't remember this path being here...");
            }
            // TODO - ambush stuff
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
