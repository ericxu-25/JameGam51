using System.Collections;
using UnityEngine;

namespace Map
{
    public class WinNode : MovementNode 
    {
        public string message;
        public override IEnumerator OnArrive()
        {
            yield return base.OnArrive();
            MapManager.Instance.RequestNextMap();
            yield break;
        }

        public override void OnConnectTo(MapConnection connection)
        {
            
        }

        public override IEnumerator OnMoveTowards()
        {
            if(message != null)
                yield return MapPlayer.Instance.SayText(message, 0f);
            yield break;
        }
    }
}
