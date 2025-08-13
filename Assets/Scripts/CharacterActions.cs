using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет боевым поведением персонажа: поиском цели, выбором и выполнением действий.
/// </summary>
public class CharacterActions : MonoBehaviour
{
    private BattleManager _battleManager;
    private Character _self;
    private CharacterStats _stats;
    private CharacterMover _mover;

    [Header("AI State")]
    private Character _currentTarget;
    
    // TODO: Кулдаун атаки должен зависеть от оружия/скорости персонажа.
    private const float ATTACK_COOLDOWN = 2.0f;
    private float _lastAttackTime;

    private void Awake()
    {
        _self = GetComponent<Character>();
        _stats = GetComponent<CharacterStats>();
        _mover = GetComponent<CharacterMover>();
        _battleManager = BattleManager.Instance;
    }

    /// <summary>
    /// Выполняет один такт логики персонажа. Вызывается из Character.Update().
    /// </summary>
    public void Tick()
    {
        if (_mover.isMoving) return;

        if (_currentTarget == null || _currentTarget.Stats.currentHealth <= 0)
        {
            FindClosestEnemy();
            if (_currentTarget == null) return; // Бой окончен, врагов не осталось.
        }

        int distance = GetDistanceToTarget(_currentTarget);

        if (distance > _stats.attackRange)
        {
            _mover.FindPathAndMove(_currentTarget);
        }
        else
        {
            Act();
        }  
    }

    /// <summary>
    /// Обрабатывает получение урона этим персонажем.
    /// </summary>
    public void TakeDamage(float amount, Character attacker)
    {
        _stats.TakeDamage(amount);
        if (_stats.currentHealth <= 0)
        {
            Die();
        }
    }

    // Определяет, какое действие предпринять, когда персонаж находится в радиусе атаки.
    private void Act()
    {
        if (Time.time > _lastAttackTime + ATTACK_COOLDOWN)
        {
            // Пассивное восстановление усталости в начале хода.
            _stats.currentFatigue -= _stats.fatigueRegenPerTurn;
            _stats.currentFatigue = Mathf.Max(0, _stats.currentFatigue);

            if (_stats.currentFatigue + _stats.attackFatigueCost <= _stats.maxFatigue)
            {
                Attack(_currentTarget);
            }
            else
            {
                Rest();
            }
        }
    }

    // Выполняет действие "Отдых", активно восстанавливая усталость.
    private void Rest()
    {
        _stats.currentFatigue -= _stats.restFatigueRecovery;
        _stats.currentFatigue = Mathf.Max(0, _stats.currentFatigue);
        _lastAttackTime = Time.time;
    }

    // Выполняет атаку по указанной цели.
    private void Attack(Character target)
    {
        _stats.currentFatigue += _stats.attackFatigueCost;
        _stats.currentFatigue = Mathf.Min(_stats.currentFatigue, _stats.maxFatigue);

        float chanceToHit = _stats.hitChance - target.Stats.dodgeChance;
        if (Random.Range(0, 100) <= chanceToHit)
        {
            float damageDealt = _stats.damage;
            target.Actions.TakeDamage(damageDealt, _self);
            FeedbackManager.Instance.ShowFeedbackText(target.transform, damageDealt.ToString("F0"), Color.red);
        }
        else
        {
            FeedbackManager.Instance.ShowFeedbackText(target.transform, "Miss", Color.white);
        }

        _lastAttackTime = Time.time;
    }

    // Обрабатывает смерть персонажа.
    private void Die()
    {
        _battleManager.UnregisterFighter(_self);

        if (_mover != null)
        {
            var tile = GridManager.Instance.GetTile(_mover.currentX, _mover.currentY);
            if (tile != null) tile.ClearOccupant();
        }
        Destroy(gameObject);
    }

    // Находит ближайшего живого противника среди зарегистрированных участников боя.
    private void FindClosestEnemy()
    {
        List<Character> enemies = _battleManager.GetOpposingFighters(_self.teamID);
        if (enemies == null || enemies.Count == 0)
        {
            _currentTarget = null;
            return;
        }

        Character closestEnemy = null;
        float minDistanceSqr = float.MaxValue;

        foreach (var other in enemies)
        {
            // Пропускаем уже мертвых врагов (на случай, если они еще не удалены из списка).
            if (other == null || other.Stats.currentHealth <= 0) continue;

            float distanceSqr = (transform.position - other.transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestEnemy = other;
            }
        }
        _currentTarget = closestEnemy;
    }

    // Рассчитывает расстояние до цели в клетках (Расстояние Чебышёва).
    private int GetDistanceToTarget(Character target) => 
        Mathf.Max(Mathf.Abs(_mover.currentX - target.Mover.currentX), Mathf.Abs(_mover.currentY - target.Mover.currentY));
}