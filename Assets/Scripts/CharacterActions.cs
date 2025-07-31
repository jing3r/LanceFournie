using UnityEngine;

/// <summary>
/// Управляет боевой логикой и принятием решений персонажа.
/// Отвечает за поиск целей, выбор действия (атака, движение, отдых) и их выполнение.
/// </summary>
public class CharacterActions : MonoBehaviour
{
    // Ссылки на другие компоненты этого же персонажа
    private Character self;
    private CharacterStats stats;
    private CharacterMover mover;

    [Header("AI State")]
    private Character currentTarget;
    
    // HACK: Временно кулдаун является фиксированным. 
    // В будущем он должен зависеть от оружия или скорости персонажа.
    private const float ATTACK_COOLDOWN = 2.0f;
    private float lastAttackTime;

    private void Awake()
    {
        self = GetComponent<Character>();
        stats = GetComponent<CharacterStats>();
        mover = GetComponent<CharacterMover>();
    }

    /// <summary>
    /// Выполняет один "тик" логики персонажа. Вызывается каждый кадр из Character.Update().
    /// </summary>
    public void Tick()
    {
        if (mover.isMoving) return;

        // 1. Поиск цели, если ее нет.
        if (currentTarget == null || currentTarget.Stats.currentHealth <= 0)
        {
            FindClosestEnemy();
            if (currentTarget == null) return; // Врагов не осталось.
        }

        // 2. Определение дистанции до цели.
        int distance = GetDistanceToTarget(currentTarget);

        // 3. Выбор действия: двигаться или атаковать/отдыхать.
        if (distance > stats.attackRange)
        {
            mover.FindPathAndMove(currentTarget);
        }
        else
        {
            // Персонаж готов действовать, если его кулдаун прошел.
            if (Time.time > lastAttackTime + ATTACK_COOLDOWN)
            {
                // Это "ход" персонажа. Сначала он пассивно восстанавливает немного сил.
                stats.currentFatigue -= stats.fatigueRegenPerTurn;
                stats.currentFatigue = Mathf.Max(0, stats.currentFatigue);

                // Проверяем, хватает ли сил на атаку.
                if (stats.currentFatigue + stats.attackFatigueCost <= stats.maxFatigue)
                {
                    Attack(currentTarget);
                }
                else
                {
                    // Если сил не хватает, персонаж вынужден пропустить атаку и отдохнуть.
                    Rest();
                }
            }
        }  
    }

    /// <summary>
    /// Выполняет действие "Отдых", активно восстанавливая усталость.
    /// </summary>
    private void Rest()
    {
        stats.currentFatigue -= stats.restFatigueRecovery;
        stats.currentFatigue = Mathf.Max(0, stats.currentFatigue);
        
        // Отдых считается действием и запускает кулдаун.
        lastAttackTime = Time.time;
        // Debug.Log($"{self.name} is resting. Fatigue is now {stats.currentFatigue}/{stats.maxFatigue}");
    }


    private void Attack(Character target)
    {
        stats.currentFatigue += stats.attackFatigueCost;
        stats.currentFatigue = Mathf.Min(stats.currentFatigue, stats.maxFatigue);

        float chanceToHit = stats.hitChance - target.Stats.dodgeChance;
        if (Random.Range(0, 100) <= chanceToHit)
        {
            float damageDealt = stats.damage;
            target.Actions.TakeDamage(damageDealt, self);
            FeedbackManager.Instance.ShowFeedbackText(target.transform, damageDealt.ToString("F0"), Color.red);
        }
        else
        {
            FeedbackManager.Instance.ShowFeedbackText(target.transform, "Miss", Color.white);
        }

        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Обрабатывает получение урона этим персонажем.
    /// </summary>
    /// <param name="amount">Количество полученного урона.</param>
    /// <param name="attacker">Персонаж, нанесший урон.</param>
    public void TakeDamage(float amount, Character attacker)
    {
        stats.TakeDamage(amount);
        if (stats.currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Логика поражения персонажа в бою.
    /// </summary>
    private void Die()
    {
        if (mover != null)
        {
            var tile = GridManager.Instance.GetTile(mover.currentX, mover.currentY);
            if(tile != null) tile.ClearOccupant();
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Находит ближайшего живого противника на поле.
    /// </summary>
    private void FindClosestEnemy()
    {
        // TODO: Оптимизировать поиск. FindObjectsOfType - ресурсоемкая операция.
        // В будущем можно кэшировать списки команд в BattleManager.
        Character[] allCharacters = FindObjectsOfType<Character>();
        Character closestEnemy = null;
        float minDistanceSqr = float.MaxValue;

        foreach (var other in allCharacters)
        {
            if (other.teamID == self.teamID || other.Stats.currentHealth <= 0) continue;

            // Используем sqrMagnitude вместо Distance для оптимизации, т.к. нам не нужно точное расстояние,
            // а только сравнение. Это избавляет от операции извлечения корня.
            float distanceSqr = (transform.position - other.transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestEnemy = other;
            }
        }
        currentTarget = closestEnemy;
    }

    /// <summary>
    /// Рассчитывает расстояние до цели в клетках (Расстояние Чебышёва).
    /// </summary>
    private int GetDistanceToTarget(Character target) => 
        Mathf.Max(Mathf.Abs(mover.currentX - target.Mover.currentX), Mathf.Abs(mover.currentY - target.Mover.currentY));
}