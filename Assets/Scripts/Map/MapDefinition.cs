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
        [SerializeField, Tooltip("Random seed used in map generation. Set to 0 to generate a random seed")]
        public int seed = 0;
        [SerializeField, Tooltip("Whether to use the preset seed or not")]
        public bool useSeed = false;

        [Header("Path Splitting")]
        [SerializeField, Tooltip("Chance for a node to split based on distance")]
        public AnimationCurve splitChance;

        [SerializeField, Tooltip("How much the node will split at minimum if split")]
        public int minSplitAmount = 1;

        [SerializeField, Tooltip("How much the node will split at most if split")]
        public int maxSplitAmount = 4;

        [SerializeField, Tooltip("How much the chance to split is multiplied by for each successive split")]
        public float splitDecay = 0.8f;

        [SerializeField, Tooltip("Maximum depth of splitting a node before rejoining")]
        public int maxSplitDepth = 1;

        [SerializeField, Tooltip("Maximum times at each depth an additional split can be made")]
        public int maxBonusSplits = 1;

        [SerializeField, Tooltip("Chance a node will connect to its neighbor based on distance")]
        public AnimationCurve splitConnectionChance;

        [SerializeField, Tooltip("Chance a split node will not continue to the next node if connected to its neighbor based on distance")]
        public AnimationCurve detourChance;

        [Header("Node Generation")]
        [SerializeField, Tooltip("Weighted nodes to generate along path")]
        public MapNodePolicy[] NodesToGenerate;

        [SerializeField, Tooltip("Weighted nodes to generate for bonus nodes along path")]
        public MapNodePolicy[] BonusNodesToGenerate;

        [SerializeField, Tooltip("Weighted nodes to generate as hidden nodes on paths")]
        public MapNodePolicy[] HiddenNodesToGenerate;

        [SerializeField, Tooltip("Chance a valid hidden node will generate on a path based on distance")]
        public AnimationCurve HiddenNodeChance;

        [SerializeField, Tooltip("Base connection to use to connect each node")]
        public MapConnection BaseNodeConnector;

        [SerializeField, Tooltip("Node to start from")]
        public MapNode startingNode;

        [SerializeField, Tooltip("Node(s) to end at")]
        public MapNode[] endingNodes;

        [Header("Aesthetic Details")]
        [SerializeField, Tooltip("How much noise to apply to node position based on distance")]
        public AnimationCurve noiseMagnitude;

        [SerializeField, Tooltip("Zoom on the base noise map to use (higher values result in less local connectivity)")]
        public float noiseScale = 1f;


        [SerializeField, Tooltip("How much of the base map width the map is expected to take up.")]
        public float displayWidth = 1f;

        [SerializeField, Tooltip("How much of the base map height the map is expected to take up.")]
        public float displayHeight = 1f;

        private void OnValidate()
        {
            minPathLength = Mathf.Abs(minPathLength);
            maxPathLength = Mathf.Clamp(maxPathLength, minPathLength, int.MaxValue);
            totalPaths = Mathf.Abs(totalPaths);
            if (totalPaths < 1) totalPaths = 1;
            minSplitAmount = Mathf.Clamp(minSplitAmount, 1, int.MaxValue);
            maxSplitAmount = Mathf.Clamp(maxSplitAmount, minSplitAmount, int.MaxValue);
            splitDecay = Mathf.Clamp(splitDecay, 0f, Mathf.Infinity);
            maxSplitDepth = Mathf.Clamp(maxSplitDepth, 0, 10);
            maxBonusSplits = Mathf.Clamp(maxBonusSplits, 1, int.MaxValue);
            if (seed == 0)
            {
                seed = Random.Range(0, Mathf.Abs((int)System.DateTime.Now.Ticks));
            }
            displayWidth = Mathf.Clamp(displayWidth, 0.1f, int.MaxValue);
            displayHeight = Mathf.Clamp(displayHeight, 0.1f, int.MaxValue);
        }

        public void PrintDebug() {
            if (NodesToGenerate == null || NodesToGenerate.Length == 0) {
                Debug.LogError(this.name + " Map Definition has no nodes to generate!");
            }
            if (BonusNodesToGenerate == null || BonusNodesToGenerate.Length == 0) {
                Debug.Log(this.name + " Map Definition has no bonus node definition, will use default node generation");
            }
            if (HiddenNodesToGenerate == null || HiddenNodesToGenerate.Length == 0) {
                Debug.Log(this.name + " Map Definition has no HiddenNodesToGenerate");
            }
        }

    }
}
