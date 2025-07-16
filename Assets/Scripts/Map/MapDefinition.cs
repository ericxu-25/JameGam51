using UnityEngine;

namespace Map
{
    /// <summary>
    /// Describes how a MapNode prefab is generated along a path
    /// </summary>
    [System.Serializable]
    public struct MapNodePolicy{
        public int weight;
        public MapNode node;
    }

    /// <summary>
    /// Definition that describes the properties of a generated map
    /// </summary>
    [CreateAssetMenu(fileName = "MapCreationPolicy", menuName = "Scriptable Objects/MapCreationPolicy")]
    public class MapDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Minimum amount of nodes on the path")]
        public int minPathLength = 3;
        [SerializeField, Tooltip("Maximum amount of nodes on the path")]
        public int maxPathLength = 5;
        [SerializeField, Tooltip("Total amount of paths to generate")]
        public int totalPaths = 3;

        [SerializeField, Tooltip("Base connection to use to connect each node")]
        public MapConnection BaseNodeConnector;

        [Header("Path Splitting")]
        [SerializeField, Tooltip("Chance for a node to split based on distance to end of path")]
        public AnimationCurve splitChance;

        [SerializeField, Tooltip("How much the node will split at minimum if split")]
        public int minSplitAmount = 1;

        [SerializeField, Tooltip("How much the node will split at most if split")]
        public int maxSplitAmount = 4;

        [SerializeField, Tooltip("How much the chance to split is multiplied by for each successive split")]
        public float splitDecay = 0.8f;

        [SerializeField, Tooltip("Maximum depth of splitting a node before rejoining")]
        public int maxSplitDepth = 1;

        [SerializeField, Tooltip("Chance a node will connect to its nearby split neighbors based on distance to end of path")]
        public AnimationCurve splitConnectionChance;

        [Header("Node Generation")]
        [SerializeField, Tooltip("Weighted nodes to generate along path")]
        public MapNodePolicy[] NodesToGenerate;

        [SerializeField, Tooltip("Weighted nodes to generate as hidden nodes on paths")]
        public MapNodePolicy[] HiddenNodesToGenerate;

        [SerializeField, Tooltip("Chance a valid hidden node will generate on a path based on distance")]
        public AnimationCurve HiddenNodeChance;

        [SerializeField, Tooltip("Node(s) to start from")]
        public MapNode startingNode;

        [SerializeField, Tooltip("Node(s) to end at")]
        public MapNode[] endingNode;

        private void OnValidate()
        {
            minPathLength = Mathf.Abs(minPathLength);
            if (minPathLength < 1) minPathLength = 1;
            maxPathLength = Mathf.Clamp(maxPathLength, minPathLength, int.MaxValue);
            totalPaths = Mathf.Abs(totalPaths);
            if (totalPaths < 1) totalPaths = 1;
            minSplitAmount = Mathf.Clamp(minSplitAmount, 1, int.MaxValue);
            maxSplitAmount = Mathf.Clamp(minSplitAmount, minSplitAmount, int.MaxValue);
            splitDecay = Mathf.Clamp(splitDecay, 0f, Mathf.Infinity);
            maxSplitDepth = Mathf.Clamp(maxSplitDepth, 0, 10);
        }

    }
}
