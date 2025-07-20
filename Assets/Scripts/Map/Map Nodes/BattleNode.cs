using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class BattleNode : MovementNode
    {
        public override IEnumerator OnArrive()
        {
            // TODO trigger a battle, temporarily disable the map until the 
            return base.OnArrive();
        }
    }
}
