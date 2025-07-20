using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class MovementNode: MapNode
    {
        public override IEnumerator OnArrive(MapManager manager)
        {
            throw new System.NotImplementedException();
        }

        public override void OnGenerate(List<MapNode> currentPath, List<List<MapNode>> allPaths)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerator OnLeave(MapManager manager)
        {
            throw new System.NotImplementedException();
        }
    }
}
