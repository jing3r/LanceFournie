using UnityEngine;
using System;

/// <summary>
/// Управляет всеми боевыми характеристиками и параметрами персонажа.
/// Рассчитывает производные атрибуты на основе данных из CharacterBlueprint.
/// </summary>
public class CharacterStats : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Ссылка на 'анкету' персонажа с базовыми характеристиками.")]
    public CharacterBlueprint blueprint;

    [Header("Live Combat Stats")]
    public float currentHealth;
    public int currentFatigue;

    // --- Производные атрибуты ---
    public float maxHealth { get; private set; }
    public int maxFatigue { get; private set; }
    public float damage { get; private set; }
    public float hitChance { get; private set; }
    public float dodgeChance { get; private set; }
    public int attackRange { get; private set; }
    public int fatigueRegenPerTurn { get; private set; }
    public int attackFatigueCost { get; private set; }
    public int restFatigueRecovery { get; private set; }

    /// <summary>
    /// Событие, которое вызывается при изменении здоровья.
    /// Передает (текущее здоровье, максимальное здоровье).
    /// </summary>
    public event Action<float, float> OnHealthChanged;
    
    /// <summary>
    /// Инициализирует статы персонажа на основе его blueprint и оповещает UI.
    /// </summary>
    public void Initialize(CharacterBlueprint bp)
    {
        this.blueprint = bp;
        this.attackFatigueCost = bp.attackFatigueCost;

        CalculateDerivedStats();
        currentFatigue = 0;
        currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Применяет урон к текущему здоровью и вызывает событие OnHealthChanged.
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Рассчитывает все производные атрибуты на основе базовых.
    /// </summary>
    private void CalculateDerivedStats()
    {
        if (blueprint == null)
        {
            Debug.LogError("CharacterBlueprint is not assigned to CharacterStats on " + gameObject.name);
            return;
        }

        // --- Формулы расчета. Вынесены сюда для централизованного управления балансом. ---
        maxHealth = blueprint.Strength * 10 + blueprint.Endurance * 10;
        maxFatigue = blueprint.Endurance * 10;
        damage = blueprint.Strength * 5;
        hitChance = 50 + blueprint.Accuracy * 5;
        dodgeChance = 5 + blueprint.Reflexes * 5;

        // --- Усталость ---
        // Пассивная регенерация за ход зависит от Воли и Выносливости.
        fatigueRegenPerTurn = blueprint.Willpower + blueprint.Endurance;
        // Активное восстановление при отдыхе зависит от Выносливости и опыта (Интеллекта).
        restFatigueRecovery = blueprint.Endurance * 2 + blueprint.Intellect;

        ApplyClassBonuses();
    }

    /// <summary>
    /// Применяет модификаторы к статам в зависимости от класса персонажа.
    /// </summary>
    private void ApplyClassBonuses()
    {
        // По умолчанию дальность атаки - 1 клетка.
        attackRange = 1;

        if (blueprint == null) return;
        switch (blueprint.characterClass)
        {
            case "Spearman": 
                attackRange = 2; 
                break;
        }
    }
}