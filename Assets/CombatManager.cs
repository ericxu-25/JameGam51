using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Stats playerStats;
    [SerializeField] private Stats enemyStats;
    [SerializeField] private ItemGrid playerGrid;

    //get list of weapons from playerGrid
    //iterate the time until cast of weapons by one second
    //for all weapons that cast, deal damage to enemyStats

    private List<InventoryItem> activeWeapons = new();
    private bool BattleMode = false;

    private void BattleStart()
    {
        // Get all unique weapons from the grid
        var items = playerGrid.GetAllInventoryItems();
        foreach (var item in items)
        {
            if (item.TryGetComponent(out InventoryItem weapon))
            {
                activeWeapons.Add(weapon);
            }
        }
    }

    private void Update()
    {
        if (BattleMode == true)
        {
            //Player does damage
            float deltaTime = Time.deltaTime;

            foreach (var weapon in activeWeapons)
            {
                weapon.UpdateTimer(deltaTime);

                if (weapon.IsReadyToFire())
                {
                    enemyStats.Damage(weapon.AttackDmg());
                    playerStats.Heal(weapon.regenHP());
                    playerStats.RechargeShield(weapon.shieldAmt());
                    weapon.ResetTimer();
                }
            }

            //Enemy does damage
        }


        if (Input.GetKeyDown(KeyCode.B))
        {
            BattleStart();
            BattleMode = true;
            enemyStats.Died.AddListener(OnEnemyDeath);
        }
    }

    private void OnEnemyDeath() => Debug.Log("Enemy died.");


}
