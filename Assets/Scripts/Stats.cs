using System;
using UnityEngine;
using UnityEngine.Events;

public class Stats : MonoBehaviour
{

    [Header("Health")]
    [SerializeField]
    private int _maxHP = 100;
    private int _hp;
    public int MaxHP => _maxHP;
    private int _baseDamage = 0;

    [Header("Shield")]
    [SerializeField] private int _maxShield = 100;
    private int _shield;
    public int MaxShield => _maxShield;

    public void SetMaxHP(int maxHP)
    {
        _maxHP = maxHP;
        _hp = _maxHP;
    }

    public void SetBaseDamage(int baseDamage)
    {
        _baseDamage = baseDamage;
    }

    public int GetDamage()
    {
        return _baseDamage;
    }

    public int HP
    {
        get => _hp;
        private set
        {
            var isDamage = value < _hp;
            _hp = Mathf.Clamp(value, 0, _maxHP);
            if (isDamage)
            {
                Damaged?.Invoke(_hp);
            }
            else
            {
                Healed?.Invoke(_hp);
            }

            if (_hp <= 0)
            {
                Died?.Invoke();
            }
        }
    }

    public int Shield
    {
        get => _shield;
        private set => _shield = Mathf.Clamp(value, 0, _maxShield);
    }

    public UnityEvent<int> Healed;
    public UnityEvent<int> Damaged;
    public UnityEvent<int> ShieldChanged;
    public UnityEvent Died;

    private void Awake()
    {
        _hp = _maxHP;
        _shield = 0;
    }

    public void Damage(int amount)
    {
        int remaining = amount;

        // Apply damage to shield first
        if (_shield > 0)
        {
            int shieldDamage = Mathf.Min(_shield, remaining);
            _shield -= shieldDamage;
            remaining -= shieldDamage;
            ShieldChanged?.Invoke(_shield);
        }

        // Any remaining damage hits HP
        if (remaining > 0)
        {
            HP -= remaining;
        }
    }


    public void Heal(int amount) => HP += amount;

    public void HealFull() => HP = _maxHP;


    public void Kill() => HP = 0;

    public void Adjust(int value)
    {
        HP = value;
    }
    
    public void RechargeShield(int amount)
    {
        Shield += amount;
        ShieldChanged?.Invoke(_shield);
    }

    public void AdjustShield(int value)
    {
        Shield = value;
        ShieldChanged?.Invoke(_shield);
    }
}
