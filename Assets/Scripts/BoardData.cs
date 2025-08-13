using UnityEngine;
using System;

/// <summary>
/// Функциональное назначение клетки на доске.
/// </summary>
public enum TilePurpose
{
    Battlefield,
    StagingArea,
    Roster,
    Impassable
}

/// <summary>
/// Определяет физическую высоту клетки.
/// </summary>
public enum ElevationType
{
    Flat,
    High,
    Low
}

/// <summary>
/// Определяет сложность передвижения по клетке.
/// </summary>
public enum MovementType
{
    Normal,
    Difficult
}

/// <summary>
/// Определяет, можно ли встать на клетку.
/// </summary>
public enum PassabilityType
{
    Passable,
    Impassable
}

/// <summary>
/// Определяет боевую роль, назначенную на слот или персонажа.
/// </summary>
public enum RoleType
{
    Knight,
    Squire,
    Sergeant,
    Champion,
    ManAtArms 
}

/// <summary>
/// Определяет сторону доски, которую занимает игрок.
/// </summary>
public enum BoardSide
{
    Center,
    South,
    North,
    West,
    East
}

/// <summary>
/// Структура для конфигурации расположения одного игрока на доске.
/// </summary>
[System.Serializable]
public class PlayerLayoutSetup
{
    [Tooltip("ID игрока (1, 2, 3 или 4).")]
    public int playerID;

    [Tooltip("Сторона доски, которую занимает игрок.")]
    public BoardSide side;

    [Tooltip("Количество юнитов, которое нужно создать для этого игрока.")]
    public int unitsToSpawn = 8;
}

/// <summary>
/// Определяет тип сценария боя, влияющий на компоновку доски и правила.
/// </summary>
public enum BattleScenario
{
    TwoPlayersVersus,
    FourPlayersFreeForAll,
    FourPlayersTeams
}