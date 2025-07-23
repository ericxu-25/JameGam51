using UnityEngine;

public class Player : Singleton.RegulatorSingleton<Player> 
{
    [SerializeField] public Stats stats;
    // public int damage = 10;

    private new void Awake()
    {
        base.Awake();
        if (stats == null)
            stats = GetComponent<Stats>();

        stats.Died.AddListener(OnDeath);
        stats.Damaged.AddListener(OnDamaged);
        stats.Healed.AddListener(OnHealed);
        stats.ShieldChanged.AddListener(OnShieldChanged);
    }

    private void OnDeath() => Debug.Log("Player died.");
    private void OnDamaged(int hp) => Debug.Log($"Player damaged, HP: {hp}");
    private void OnHealed(int hp) => Debug.Log($"Player healed, HP: {hp}");
    private void OnShieldChanged(int shield) => Debug.Log($"Shield changed: {shield}");
}
