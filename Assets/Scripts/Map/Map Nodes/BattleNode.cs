using Globals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map
{
    public class BattleNode : MovementNode
    {
        [SerializeField, Tooltip("Scene to transition to for the battle")]
        private string BattleScene;

        private void StartBattle() {
            MapManager.Instance.HideCurrentMap();
            GameCamera.Instance.gameObject.SetActive(false);
            // starts the battle scene, hides the map
            GameSceneManager.Instance.NextScene(BattleScene, LoadSceneMode.Additive);
        }
        public override IEnumerator OnArrive()
        {
            // TODO trigger a battle and temporarily disable the map until the battle is over
            yield return base.OnArrive();
            StartBattle();
            yield break; 
        }
    }
}
