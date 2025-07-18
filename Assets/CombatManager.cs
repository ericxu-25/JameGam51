using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class CombatManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Stats playerStats;
    [SerializeField] private Stats enemyStats;
    [SerializeField] private ItemGrid playerGrid;

    //get list of weapons from playerGrid
    //iterate the time until cast of weapons by one second
    //for all weapons that cast, deal damage to enemyStats

    //Enemy Attack thingie (constant for now)
    private float enemyTimer = 0f;
    private float interval = 1f;

    private List<InventoryItem> activeWeapons = new();
    private bool BattleMode = false;
    public Button battleButton;
    public Button rewardButton;

    public void BattleStart()
    {
        //turn off the button
        battleButton.gameObject.SetActive(false);

        //set Player stats
        playerStats.HealFull();
        // Get all unique weapons from the grid
        var items = playerGrid.GetAllInventoryItems();
        foreach (var item in items)
        {
            if (item.TryGetComponent(out InventoryItem weapon))
            {
                activeWeapons.Add(weapon);
            }
        }


        //Set Enemy Stats
        enemyStats.SetMaxHP(200);
        enemyStats.SetBaseDamage(10);
        BattleMode = true;
        enemyStats.Died.AddListener(OnEnemyDeath);
        playerStats.Died.AddListener(OnPlayerDeath);

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

            //enemy does damage every once per second
            enemyTimer += Time.deltaTime;
            if (enemyTimer >= interval)
            {
                enemyTimer = 0f;
                playerStats.Damage(enemyStats.GetDamage());
            }
        }


        // if (Input.GetKeyDown(KeyCode.B))
        // {
        //     BattleStart();

        // }
    }

    private void OnEnemyDeath()
    {
        BattleMode = false;
        Debug.Log("Enemy died.");
        rewardButton.gameObject.SetActive(true);

    }

    private void OnPlayerDeath()
    {
        BattleMode = false;
        Debug.Log("Player died.");
        battleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Try Again!";
        battleButton.gameObject.SetActive(true);
    }

    public void rewardClaimed()
    {
        rewardButton.gameObject.SetActive(false);
    }

}
