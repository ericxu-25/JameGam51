using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
// using System.Numerics;

public class CombatManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Enemy enemyPrefab;
    public Transform enemyPoint;
    [SerializeField] private Stats playerStats;
    [SerializeField] private Enemy enemy;
    private Stats enemyStats;
    [SerializeField] private ItemGrid playerGrid;

    //get list of weapons from playerGrid
    //iterate the time until cast of weapons by one second
    //for all weapons that cast, deal damage to enemyStats

    //Enemy Attack thingie (constant for now)
    private float enemyTimer = 0f;
    private float playerTimer = 0f;
    private float interval = 1f;
    private int enemyHP = 100;
    private int enemyDMG = 10;

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

        if (enemy == null)
        {
            float enemyHeight = enemyPrefab.GetComponent<SpriteRenderer>().size.y * enemyPrefab.transform.localScale.y;
            float scaledHeight = enemyHeight;
            Vector3 spawnPoint = enemyPoint.position + new Vector3(0f, enemyHeight / 2, 0f);
            Debug.Log(enemyHeight);
            enemy = Instantiate(enemyPrefab, spawnPoint, Quaternion.identity);
        }
        //Set Enemy Stats
        enemyStats = enemy.GetStats();
        enemyStats.SetMaxHP(enemyHP);
        enemyStats.SetBaseDamage(enemyDMG);


        BattleMode = true;
        enemyStats.Died.AddListener(OnEnemyDeath);
        playerStats.Died.AddListener(OnPlayerDeath);
        enemyTimer = 0f;
        playerTimer = 0f;
        interval = 1f;

    }

    private void Update()
    {
        if (BattleMode == true)
        {
            //Player does damage w weapons + base damage
            WeaponsFire(playerStats, enemyStats);

            //enemy does damage every once per second
            enemyTimer += Time.deltaTime;
            if (enemyTimer >= interval)
            {
                enemyTimer = 0f;
                playerStats.Damage(enemyStats.GetDamage());
            }
        }
    }

    private void WeaponsFire(Stats attackerStats, Stats victimStats)
    {
        float deltaTime = Time.deltaTime;

        foreach (InventoryItem weapon in activeWeapons)
        {
            weapon.UpdateTimer(deltaTime);

            if (weapon.IsReadyToFire())
            {
                victimStats .Damage(weapon.AttackDmg());
                attackerStats .Heal(weapon.regenHP());
                attackerStats .RechargeShield(weapon.shieldAmt());
                weapon.ResetTimer();
            }
        }
        playerTimer += Time.deltaTime;
        if (playerTimer >= interval)
        {
            playerTimer = 0f;
            victimStats.Damage(attackerStats.GetDamage());
        }
    }

    private void OnEnemyDeath()
    {
        BattleMode = false;
        Debug.Log("Enemy died.");
        rewardButton.gameObject.SetActive(true);

        IncreaseEnemyStats();

    }

    private void IncreaseEnemyStats()
    {
        //update enemy stats for next battle
        float enemyDMGf = enemyDMG * Random.Range(1.1f, 1.3f);
        enemyDMG = Mathf.RoundToInt(enemyDMGf);
        float enemyHPf = enemyHP * Random.Range(1.1f, 1.3f);
        enemyHP = Mathf.RoundToInt(enemyHPf);
    }

    private void OnPlayerDeath()
    {
        BattleMode = false;
        Debug.Log("Player died.");
        battleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Try Again!";
        battleButton.gameObject.SetActive(true);
    }

    public void RewardClaimed()
    {
        rewardButton.gameObject.SetActive(false);
        battleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Battle!";
        battleButton.gameObject.SetActive(true);
    }



}


