using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Stats stats;
    // [SerializeField] private int damage;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<Stats>();

        stats.Died.AddListener(OnDeath);
        stats.Damaged.AddListener(OnDamaged);
        stats.Healed.AddListener(OnHealed);
        stats.ShieldChanged.AddListener(OnShieldChanged);
    }

    public Stats GetStats()
    {
        return stats;
    }


    private void OnDeath()
    {
        Debug.Log("Enemy died.");
        Destroy(gameObject);

    } 
    private void OnDamaged(int hp) => Debug.Log($"Enemy damaged, HP: {hp}");
    private void OnHealed(int hp) => Debug.Log($"Enemy healed, HP: {hp}");
    private void OnShieldChanged(int shield) => Debug.Log($"Enemy Shield changed: {shield}");

    // private void SetDamage(int dmg)
    // {
    //     damage = dmg;
    // }
}
