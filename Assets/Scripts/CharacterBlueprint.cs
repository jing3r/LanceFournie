using UnityEngine;

/// <summary>
/// ScriptableObject, хранящий постоянные, базовые данные о персонаже.
/// Является "анкетой", которая используется для создания персонажа
/// на поле боя. Не меняется в ходе одного сражения.
/// </summary>
[CreateAssetMenu(fileName = "New Character Blueprint", menuName = "Characters/Character Blueprint")]
public class CharacterBlueprint : ScriptableObject
{
    [Header("Identity")]
    public string characterName;
    public int age;
    public float height; // в метрах
    public float weight; // в кг
    // TODO: Реализовать влияние этих параметров на геймплей.

    [Header("Class & Role")]
    public string characterClass; // "Infantry", "Spearman", "Cavalry"
    // TODO: Добавить систему ролей ("Knight", "Sergeant", etc.).

    [Header("Primary Attributes (Base Values)")]
    public int Strength;
    public int Endurance;
    public int Accuracy;
    public int Reflexes;
    public int Intellect;
    public int Wits;
    public int Charisma;
    public int Willpower;

    [Header("Equipment-driven Stats")]
    // HACK: Временно стоимость атаки - это свойство персонажа.
    // В будущем она должна определяться типом используемого оружия.
    public int attackFatigueCost = 15;

    // TODO: Добавить поля для хранения прогресса между боями (опыт, травмы).
}