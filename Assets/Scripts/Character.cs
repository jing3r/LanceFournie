using UnityEngine;

/// <summary>
/// Главный компонент-концентратор для любого персонажа на поле боя.
/// Отвечает за координацию всех остальных компонентов (Stats, Actions, Mover, Visuals).
/// Является основной точкой доступа к персонажу из других систем.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(CharacterActions))]
[RequireComponent(typeof(CharacterMover))]
[RequireComponent(typeof(CharacterVisuals))]
public class Character : MonoBehaviour
{
    private int _teamID;

    /// <summary>
    /// ID команды, к которой принадлежит персонаж.
    /// Присвоение этого значения автоматически обновляет визуальное представление (цвет).
    /// </summary>
    public int teamID
    {
        get { return _teamID; }
        set
        {
            _teamID = value;
            if (Visuals != null)
            {
                Visuals.UpdateVisuals();
            }
        }
    }

    // --- Ссылки на компоненты для легкого доступа ---
    public CharacterStats Stats { get; private set; }
    public CharacterActions Actions { get; private set; }
    public CharacterMover Mover { get; private set; }
    public CharacterVisuals Visuals { get; private set; }

    private void Awake()
    {
        // Кэшируем ссылки на компоненты при создании объекта для повышения производительности.
        Stats = GetComponent<CharacterStats>();
        Actions = GetComponent<CharacterActions>();
        Mover = GetComponent<CharacterMover>();
        Visuals = GetComponent<CharacterVisuals>();
    }
    
    /// <summary>
    /// Инициализирует персонажа, используя данные из его "паспорта" (CharacterBlueprint).
    /// Этот метод должен быть вызван сразу после создания объекта персонажа.
    /// </summary>
    /// <param name="bp">"Анкета" персонажа с базовыми характеристиками.</param>
    public void Initialize(CharacterBlueprint bp)
    {
        this.name = bp.characterName;

        // Делегируем инициализацию специализированным компонентам.
        Stats.Initialize(bp);
        Mover.ApplyClassBonuses();
        
        // Визуальное обновление произойдет автоматически при присвоении teamID снаружи.
    }

    private void Update()
    {
        // Основной цикл жизни персонажа в бою.
        // Передаем управление компоненту Actions, если бой активен и персонаж жив.
        if (BattleManager.Instance.currentPhase == GamePhase.Battle && Stats.currentHealth > 0)
        {
            Actions.Tick();
        }
    }
}