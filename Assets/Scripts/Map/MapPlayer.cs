using UnityEngine;
using TMPro;
using System.Collections;
using System.Text;

namespace Map
{
    /// <summary>
    /// Handles displaying the player properties on the ui of the map. 
    /// </summary>
    public class MapPlayer : Singleton.RegulatorSingleton<MapPlayer>
    {

        [SerializeField, Tooltip("Text used to display current player health")]
        TextMeshProUGUI HealthValueText;

        [SerializeField, Tooltip("Text used to display what the current player is saying")]
        TextMeshProUGUI PlayerDialogueText;

        [SerializeField, Tooltip("Speed the player dialogue appears (per letter)")]
        float talkSpeed = 0.1f;
        private bool _talking = false;

        private void Start()
        {
            Player.Instance.stats.Damaged.AddListener(UpdateHealthText);
            Player.Instance.stats.Healed.AddListener(UpdateHealthText);
            UpdateHealthText(0);
        }

        private void UpdateHealthText(int healthChange) {
            HealthValueText.text = Player.Instance.stats.HP.ToString() + "/" + Player.Instance.stats.MaxHP.ToString();
        }

        /// <summary>
        /// Make the player sprite say something for a few seconds
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public void Say(string message, float duration = 2.0f) {
            if (!_talking)
            {
                Debug.Log("Map Player saying: " + message + " (for " + duration.ToString() + " seconds)");
                StartCoroutine(SayText(message, duration, this.talkSpeed));
            }
            else { 
                Debug.Log("Map Player kept the message to themselves: " + message);
            }
        }

        /// <summary>
        /// Make the player sprite say something for a given duration and a talk speed. If duration is <= 0, will not erase the message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="talkspeed"></param>
        /// <returns></returns>
        public IEnumerator SayText(string message, float duration = 0f, float talkSpeed = 0f) {
            _talking = true;
            StringBuilder currentMessage = new StringBuilder("", message.Length);
            float lastTime = Time.time;
            foreach (char letter in message)
            {
                currentMessage.Append(letter);
                PlayerDialogueText.text = currentMessage.ToString();
                if (talkSpeed <= 0f || Time.time - lastTime < talkSpeed) {
                    continue;
                }
                yield return new WaitForSeconds(Time.time - lastTime);
                lastTime = Time.time;
            }
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
                PlayerDialogueText.text = "...";
            }
            _talking = false;
        }
    }
}
