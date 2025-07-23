using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
// using System.Numerics;

public class CombatManager : Singleton.PersistentSingleton<CombatManager>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Enemy enemyPrefab;
    public Transform enemyPoint;
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
    private bool _battleMode = false;
    public bool BattleMode { get { return _battleMode; } }
    private bool _waitingToStart = false;
    public bool WaitingToStart { get { return _waitingToStart; } }
    public Button battleButton;
    public Button continueButton;
    public Button rewardButton;

    public void Start()
    {
        battleButton.onClick.AddListener(BattleStart);
        continueButton.onClick.AddListener(ReturnToMap);
        rewardButton.onClick.AddListener(RewardClaimed);
        battleButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
        rewardButton.gameObject.SetActive(false);
    }

    public void BattleStart()
    {
        //turn off the button
        _waitingToStart = false;
        Player.Instance.gameObject.SetActive(true);
        battleButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);

        //set Player stats
        Player.Instance.stats.HealFull();
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


        _battleMode = true;
        enemyStats.Died.AddListener(OnEnemyDeath);
        Player.Instance.stats.Died.AddListener(OnPlayerDeath);
        enemyTimer = 0f;
        playerTimer = 0f;
        interval = 1f;

    }

    private void Update()
    {
        if (_battleMode == true)
        {
            //Player does damage w weapons + base damage
            WeaponsFire(Player.Instance.stats, enemyStats);

            //enemy does damage every once per second
            enemyTimer += Time.deltaTime;
            if (enemyTimer >= interval)
            {
                enemyTimer = 0f;
                Player.Instance.stats.Damage(enemyStats.GetDamage());
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
        _battleMode = false;
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
        _battleMode = false;
        Debug.Log("Player died.");
        battleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Try Again!";
        battleButton.gameObject.SetActive(true);
    }

    public void RewardClaimed()
    {
        rewardButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
    }

    public void ReturnToMap() {
        battleButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
        _waitingToStart = true;
        battleButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Battle";
    }



}


