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

        private bool _triggered = false;

        private IEnumerator StartBattle() {
            // starts the battle scene, hides the map
            MapManager.Instance.HideCurrentMap();
            GameCamera.Instance.gameObject.SetActive(false);
            if (!GameSceneManager.Instance.IsSceneOpen(BattleScene))
            {
                yield return GameSceneManager.Instance.NextSceneAsync(BattleScene, LoadSceneMode.Additive);
            }
            CombatManager.Instance.gameObject.SetActive(true);
            Player.Instance.gameObject.SetActive(true);
            MapManager.Instance.PauseMovement = true;
            yield return new WaitUntil(() => CombatManager.Instance.BattleMode);
        }

        private IEnumerator EndBattle() {
            // shows the map, hides the battle scene
            MapManager.Instance.ShowCurrentMap();
            CombatManager.Instance.gameObject.SetActive(false);
            Player.Instance.gameObject.SetActive(false);
            GameCamera.Instance.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            MapManager.Instance.PauseMovement = false;
        }
        public override IEnumerator OnArrive()
        {
            yield return base.OnArrive();
            if (_triggered) yield break; // skip if we've already been here
            // trigger a battle and temporarily disable the map until the battle is over
            yield return StartBattle();
            yield return new WaitUntil(() => CombatManager.Instance.WaitingToStart);
            yield return EndBattle();
            _triggered = true;
            yield break; 
        }

        public override void OnConnectTo(MapConnection connection)
        {
            base.OnConnectTo(connection);
            if(Random.value < 0.5f)
                connection.TravelMessages.Add("I see an enemy up ahead...");
        }

        public override void OnHiddenConnectTo(MapConnection connection)
        {
            base.OnHiddenConnectTo(connection);
            if(Random.value < 0.25f)
                connection.TravelMessages.Add("I don't have a good feeling about this path...");
        }
    }
}
