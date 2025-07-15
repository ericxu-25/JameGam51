using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Health health;
    public int damage = 10;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        health.Died.AddListener(OnDeath);
        health.Damaged.AddListener(OnDamaged);
        health.Healed.AddListener(OnHealed);
        health.ShieldChanged.AddListener(OnShieldChanged);
    }

    private void OnDeath() => Debug.Log("Player died.");
    private void OnDamaged(int hp) => Debug.Log($"Player damaged, HP: {hp}");
    private void OnHealed(int hp) => Debug.Log($"Player healed, HP: {hp}");
    private void OnShieldChanged(int shield) => Debug.Log($"Shield changed: {shield}");
}
