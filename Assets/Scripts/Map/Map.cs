using UnityEngine;
using System.Collections.Generic;
using Globals;

namespace Map
{
    /// <summary>
    /// Represents a single set of paths of a map
    /// </summary>
    public struct MapSegment {
        public List<List<MapNode>> paths;
        public MapNode start;
        public List<MapNode> ends;
        public MapDefinition definition;
        public int seed;
    }

    /// <summary>
    /// Script that manages the creation, storage, and display of path nodes.
    /// </summary>
    public class Map : MonoBehaviour
    {
        [SerializeField, Tooltip("Definitions for generating the map. If multiple definitions are used, then each generated definition will be chained together")]
        private MapDefinition[] MapDefinitions;

        [Header("Map Display")]
        [SerializeField, Tooltip("Horizontal and vertical padding to use around the borders of the map when generating nodes.")]
        private Vector2 padding = new Vector2(40f, 20f);

        [SerializeField, Tooltip("Canvas to display the map on and parent nodes to when displaying")]
        private RectTransform mapRoot = null;

        private List<MapSegment> maps = null;
        private HashSet<MapNode> nodes = null;
        private GameObject _root = null;
        private Rect baseRect;
        private bool _started = false;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            maps = new List<MapSegment>();
            nodes = new HashSet<MapNode>();
            if (MapDefinitions == null || MapDefinitions.Length == 0)
            {
                Debug.LogError("Must have a valid MapDefinition for map generation");
                return;
            }
            if (mapRoot == null) {
                Debug.LogError("Map must have a valid RectTransform to draw on!");
                return;
            }
            // create a container for the nodes on this map
            _root = new GameObject(this.name + " Node Container");
            _root.transform.SetParent(this.transform, false);
            _root.SetActive(false);
            baseRect = new Rect(mapRoot.rect);
            foreach (MapDefinition definition in MapDefinitions) {
                definition.PrintDebug();
            }
            _started = true;
        }

        /// <summary>
        /// Destroys the map
        /// </summary>
        /// <param name="immediate"></param>
        public void DestroyMap(bool immediate = false)
        {
            Debug.Log("resetting map!");
            foreach (MapNode node in nodes)
            {
                if (immediate)
                {
                    DestroyImmediate(node.gameObject, false);
                    continue;
                }
                Destroy(node.gameObject);
            }
            maps.Clear();
            nodes.Clear();
            System.GC.Collect();
        }

        /// <summary>
        /// Destroys the map
        /// </summary>
        [ContextMenu("DestroyMap")]
        public void DestroyMap()
        {
            if (!_started)
            {
                Debug.LogError("Map modification is not allowed in editor");
                return;
            }
            DestroyMap(immediate: false);
        }

        /// <summary>
        /// Displays all the generated map segments by parenting them to the map root and positioning them
        /// </summary>
        [ContextMenu("DisplayMap")]
        public void DisplayMap()
        {
            if (!_started)
            {
                Debug.LogError("Map modification is not allowed in editor");
                return;
            }
            if (maps.Count == 0) {
                Debug.LogError("Cannot display map when no segment has been generated yet");
                return;
            }
            // adjust the full bounds (extending up/down and right) to be able to fit all our maps
            Rect fullBounds = new Rect(baseRect);
            Debug.Log("full bounds.size: " + fullBounds.size);
            float totalMapWidth = 0;
            float totalMapHeight = 0;
            foreach (MapDefinition definition in MapDefinitions)
            {
                totalMapWidth += definition.displayWidth;
                totalMapHeight = Mathf.Max(definition.displayHeight, totalMapHeight);
            }
            fullBounds.width *= totalMapWidth;
            fullBounds.y += fullBounds.height * (totalMapHeight - 1f) / 2;
            fullBounds.height *= totalMapHeight;
            // adjust our actual transform
            mapRoot.anchorMin = new Vector2(totalMapWidth / 2, totalMapHeight / 2);
            mapRoot.anchorMax = mapRoot.anchorMin;
            mapRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullBounds.width);
            mapRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fullBounds.height);
            // recalculate bounding box with padding
            fullBounds = mapRoot.rect;
            fullBounds.width -= padding.x * 2;
            fullBounds.x += padding.x;
            fullBounds.height -= padding.y * 2;
            fullBounds.y += padding.y;
            // Display each map segment from right to left
            float currentXMin = fullBounds.xMax;
            float stepSize = fullBounds.width / totalMapWidth;
            for (int i = maps.Count - 1; i >= 0; --i) {
                Rect bounds = new Rect(fullBounds);
                bounds.xMax = currentXMin;
                currentXMin -= stepSize * maps[i].definition.displayWidth;
                bounds.xMin = currentXMin;
                bounds.height *= maps[i].definition.displayHeight;
                bounds.y = fullBounds.y;
                // add an offset to prevent overlap between start and end nodes
                if (i != 0) bounds.xMin += bounds.width / (2 + maps[i].definition.maxPathLength);
                Debug.Log("bounds for of map segment " + i.ToString() + " " + maps[i].definition.name + ": " + bounds.size);
                DisplayMapSegment(maps[i], bounds);
                // Connect the next map segment to this one 
                if (i == maps.Count - 1) continue;
                List<MapNode> startNode = new List<MapNode>();
                startNode.Add(maps[i + 1].start);
                ConnectNodes(maps[i].ends, startNode, null, maps[i].definition);
            }
        }

        /// <summary>
        /// Hides the map's nodes without destroying them
        /// </summary>
        public void HideMap() {
            if (maps.Count == 0) {
                Debug.LogWarning("Attempted to hide a map that is empty");
                return;
            }
            foreach (MapNode node in nodes) {
                node.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Takes all the generated map nodes in a segment and displays them in a defined bounds
        /// </summary>
        public void DisplayMapSegment(MapSegment map, Rect bounds) { 
            if (map.paths.Count <= 0)
            {
                Debug.LogError("Cannot display a map segment with no paths!");
                return;
            }
            // parent the root container for the nodes to the mapRoot
            _root.transform.SetParent(mapRoot, false);
            _root.SetActive(true);
            // calculate relevant positioning and noise variables
            float verticalGap = 1f / (map.paths.Count + 1);
            // noise stuff
            Random.InitState(map.seed);
            Vector2 noiseOffset = new Vector2(Random.Range(0, 1000), Random.Range(0, 1000));
            Vector2 maxNoiseDrift = Vector2.zero;
            // positioning
            Debug.Log("Displaying map with size: " + bounds.size.ToString() + " and " + map.paths.Count.ToString() + " paths");
            Debug.Log("Map bottom left corner: " + bounds.min.ToString());
            // position each node on each path on the correct position
            for (int i = 0; i < map.paths.Count; ++i)
            {
                List<MapNode> path = map.paths[i];
                if (path.Count == 0) continue;
                // only draw start and end nodes on the middle path
                bool middlePath = (i == Mathf.FloorToInt(map.paths.Count / 2));
                int pathEnd = path.Count - (middlePath ? 0 : map.ends.Count);
                // if (middlePath) Debug.Log("Generating middle path");
                int sliceIndex = 0;
                int sliceSubIndex = 0;
                List<MapNode> slice = new List<MapNode>();
                MapNode node = path[0];
                for (int j = middlePath ? 0 : 1; j <= pathEnd; ++j)
                {
                    bool processSlice;
                    // use extra index to ensure final node/slice gets tracked
                    if (j == pathEnd)
                    {
                        processSlice = true;
                    }
                    else
                    {
                        node = path[j];
                        // Debug.Log(j.ToString() + " At index: " + node.index.ToString() + (node.IsBonus? ("B" + node.bonus.ToString()) : "") + " which is " + path[j].name);
                        // group nodes together into slices based on their index and subindex (bonus level)
                        if (node.index == sliceIndex && node.bonus == sliceSubIndex)
                        {
                            slice.Add(node);
                            processSlice = false;
                        }
                        else
                        {
                            sliceIndex = node.index;
                            sliceSubIndex = node.bonus;
                            processSlice = true;
                        }
                    }
                    if (!processSlice) continue;
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
                        // don't apply noise to the first node of each path
                        if (middlePath && path[0] == sliceNode) continue;
                        // appply perlin noise to our nodes
                        Vector2 noisePosition = noiseOffset + map.definition.noiseScale * new Vector2(sliceNode.distance, verticalPosition);
                        float noiseShift = Mathf.PerlinNoise(noisePosition.x, noisePosition.y);
                        noisePosition = map.definition.noiseMagnitude.Evaluate(sliceNode.distance) * noiseShift * maxNoiseDrift;
                        noisePosition = Vector2.Scale(noisePosition, bounds.size);
                        noisePosition = Helpers.Vec3ToVec2(sliceNode.transform.localPosition) + noisePosition;
                        sliceNode.transform.localPosition = new Vector3(noisePosition.x, noisePosition.y, sliceNode.transform.localPosition.z);
                        // Debug.Log(sliceNode.name + " position: " + sliceNode.transform.localPosition);
                    }
                    // update the next slice
                    slice.Clear();
                    if(j != pathEnd) slice.Add(node);
                }
            }
            // update each node's connecting lines
            foreach (List<MapNode> path in map.paths)
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
            if (!_started)
            {
                Debug.LogError("Map modification is not allowed in editor");
                return;
            }
            // clear any previous maps
            DestroyMap();
            // generate each map segment
            foreach (MapDefinition definition in MapDefinitions) {
                MapSegment newMapSegment = GenerateMapSegment(definition);
                maps.Add(newMapSegment);
                foreach (List<MapNode> path in newMapSegment.paths) {
                    foreach (MapNode node in path) {
                        nodes.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// Generate a MapSegment from a Map Definitioon
        /// </summary>
        public MapSegment GenerateMapSegment(MapDefinition definition) {
            int seed;
            if (definition.useSeed) seed = definition.seed;
            else seed = Random.Range(0, (int)System.DateTime.Now.Ticks);
            Random.InitState(seed);
            Debug.Log("Creating map segment from " + definition.name + " with seed of " + seed.ToString());
            // create start and ending nodes
            MapNode start = MakeChildCopy(definition.startingNode, prefix: "Start of " + definition.name);
            start.distance = 0f;
            start.index = 0;
            List<MapNode> ends = new List<MapNode>();
            List<List<MapNode>> paths = new List<List<MapNode>>();
            foreach (MapNode endingNode in definition.endingNodes)
            {
                MapNode end = MakeChildCopy(endingNode, prefix: "End of " + definition.name);
                end.distance = 1f;
                end.index = definition.maxPathLength + 2;
                ends.Add(end);
            }
            foreach (MapNode endingNode in ends)
            {
                endingNode.neighbors = ends;
            }
            if (ends.Count == 0)
            {
                Debug.LogError("MapParameters lack defined ending nodes!");
            }
            // generate the nodes and the connections between them
            for (int i = 0; i < definition.totalPaths; ++i)
            {
                paths.Add(GeneratePath(start, ends, definition));
            }
            // perform post generation updates for each node
            // while also tallying path info
            int totalNodes = 0;
            int totalBonusNodes = 0;
            int totalConnections = 0;
            foreach (List<MapNode> path in paths)
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
            totalNodes -= (paths.Count - 1) * definition.endingNodes.Length;
            Debug.Log("Created " + paths.Count.ToString()
                + " paths with " + totalNodes.ToString() + " nodes, "
                + totalBonusNodes.ToString() + " bonus nodes and "
                + totalConnections.ToString() + " connections.");
            MapSegment newMap = new MapSegment();
            newMap.definition = definition;
            newMap.seed = seed;
            newMap.paths = paths;
            newMap.start = start;
            newMap.ends = ends;
            return newMap;
        }

        /// <summary>
        /// Generates a list of MapNode game objects based on the given map configuration for a single path
        /// </summary>
        /// <returns></returns>
        private List<MapNode> GeneratePath(MapNode start, List<MapNode> ends, MapDefinition definition)
        {
            List<MapNode> path = new List<MapNode>();
            // ensure starting node is connected
            List<MapNode> previousNodes = new List<MapNode>();
            previousNodes.Add(start);
            path.Add(start);
            // generated the nodes for the path
            int nodesToGenerate = Random.Range(definition.minPathLength, definition.maxPathLength + 1);
            for (int i = 0; i < nodesToGenerate; ++i)
            {
                // need to add offset for index and total nodes to account for start and ending nodes
                GenerateNextNodesInPath(ref previousNodes, ref path, i + 1, nodesToGenerate + 2, definition);
            }
            // connect all final nodes to the ending nodes
            ConnectNodes(previousNodes, ends, path, definition);
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
        private void GenerateNextNodesInPath(ref List<MapNode> previousNodes, ref List<MapNode> path, int index, int totalNodes, MapDefinition definition)
        {
            float distance = index * 1f / totalNodes;
            List<MapNode> nextNodes = new List<MapNode>();
            // determine if we are splitting the next node (e.g. creating a branch)
            float splitChance = definition.splitChance.Evaluate(distance);
            bool split = Random.value < splitChance;
            int splitNodeAmount = split ? Random.Range(definition.minSplitAmount, definition.maxSplitAmount + 1) + 1 : 1;
            for (int j = 0; j < splitNodeAmount; ++j)
            {
                MapNode nextNode;
                if (j < previousNodes.Count)
                {
                    nextNode = GenerateNextNode(previousNodes[j], path, definition.NodesToGenerate, index.ToString());
                }
                else
                {
                    nextNode = GenerateNextNode(ListHelpers.RandomFromList(previousNodes), path, definition.NodesToGenerate, index.ToString());
                }
                // determine if we connect this node to its neighbor
                if (nextNodes.Count > 0 && Random.value < definition.splitConnectionChance.Evaluate(distance))
                {
                    MapNode neighborNode = nextNodes[nextNodes.Count - 1];
                    MapConnection nodeConnection = CreateConnection(neighborNode, nextNode, path, definition);
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
            ConnectNodes(previousNodes, nextNodes, path, definition);
            previousNodes = nextNodes;

            // handle splitting again from the split nodes which adds additional intermediate nodes
            if (split) { SplitBonusNodesInPath(ref previousNodes, ref path, index, totalNodes, definition); }
        }

        /// <summary>
        /// Conducts any bonus splitting of nodes on the path
        /// </summary>
        /// <param name="previousNodes"></param>
        /// <param name="index"></param>
        /// <param name="totalNodes"></param>
        private void SplitBonusNodesInPath(ref List<MapNode> previousNodes, ref List<MapNode> path, int index, int totalNodes, MapDefinition definition)
        {
            int splitDepth = 1;
            List<MapNode> generatedNodes = new List<MapNode>(previousNodes);
            List<MapNode> newlyGeneratedNodes = new List<MapNode>();
            List<MapNode> nodeEndings = new List<MapNode>();

            float stepSize = 1f / totalNodes;
            float splitChance = definition.splitChance.Evaluate(index * stepSize);
            while (splitDepth <= definition.maxSplitDepth)
            {
                int totalSplits = 0;
                splitChance *= definition.splitDecay;
                // attempt to split each of the following nodes
                newlyGeneratedNodes.Clear();
                foreach (MapNode node in generatedNodes)
                {
                    bool splitAgain;
                    if (totalSplits >= definition.maxBonusSplits) { splitAgain = false; }
                    else { splitAgain = Random.value < splitChance; }
                    if (splitAgain)
                    {
                        ++totalSplits;
                        float distance = (index + ((float)splitDepth) / (definition.maxSplitDepth + 1)) * stepSize;
                        List<MapNode> splitAgainNodes = new List<MapNode>();
                        int splitNodeAmount = splitAgain ? Random.Range(definition.minSplitAmount, definition.maxSplitAmount + 1) + 1 : 1;
                        for (int k = 0; k < splitNodeAmount; ++k)
                        {
                            MapNodePolicy[] generationPolicy;
                            if (definition.BonusNodesToGenerate == null || definition.BonusNodesToGenerate.Length == 0)
                            {
                                generationPolicy = definition.NodesToGenerate;
                            }
                            else
                            {
                                generationPolicy = definition.BonusNodesToGenerate;
                            }
                            splitAgainNodes.Add(GenerateNextNode(node, path, generationPolicy, index.ToString() + "B" + splitDepth.ToString()));
                        }
                        foreach (MapNode bonusNode in splitAgainNodes)
                        {
                            bonusNode.bonus = totalSplits;
                            bonusNode.distance = distance;
                            bonusNode.neighbors = splitAgainNodes;
                            bonusNode.index = index;
                            MapConnection nodeConnection = CreateConnection(node, bonusNode, path, definition);
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
                generatedNodes = new List<MapNode>(newlyGeneratedNodes);
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
            T copy = Instantiate(original, _root.transform);
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
        private void ConnectNodes(List<MapNode> previousNodes, List<MapNode> currentNodes, List<MapNode> path, MapDefinition definition)
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
                        if (Random.value < definition.detourChance.Evaluate(previousNode.distance))
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
                MapConnection nodeConnection = CreateConnection(previousNode, nextNode, path, definition);
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
                    MapConnection nodeConnection = CreateConnection(prevNode, node, path, definition);
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
        private MapConnection CreateConnection(MapNode previousNode, MapNode currentNode, List<MapNode> path, MapDefinition definition)
        {
            MapConnection nodeConnection = MakeChildCopy(definition.BaseNodeConnector);
            // an invalid connection should rarely happen, but if it does we make the player say something to acknowledge it
            if (!currentNode.IsValid(previousNode, path))
            {
                nodeConnection.TravelMessages.Add("I don't remember this path being here...");
            }
            // here we add any hidden nodes to the connection
            if (definition.HiddenNodesToGenerate.Length > 0 && Random.value < definition.HiddenNodeChance.Evaluate((previousNode.distance + currentNode.distance) / 2)) {
                if(nodeConnection.HiddenNode == null) { 
                    nodeConnection.HiddenNode = new List<MapNode>();
                }
                MapNode hiddenNode = ListHelpers.WeightedRandomFromList(definition.HiddenNodesToGenerate, (MapNodePolicy p) => { return p.weight; }).node;
                if (hiddenNode != default(MapNode))
                {
                    string prefix = "Hidden " + previousNode.name;
                    hiddenNode = MakeChildCopy(hiddenNode, prefix);
                    hiddenNode.gameObject.SetActive(false);
                    hiddenNode.bonus = currentNode.bonus;
                    hiddenNode.index = currentNode.index;
                    hiddenNode.hidden = true;
                    nodeConnection.HiddenNode.Add(hiddenNode);
                }
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
        /// Returns the set of valid possible originating nodes from a set given the current node and previous path
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
