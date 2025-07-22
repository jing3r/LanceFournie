using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отвечает за создание и начальное размещение юнитов на поле боя.
/// </summary>
public class UnitSpawner : MonoBehaviour
{
    [Tooltip("Префаб персонажа, который будет создаваться на сцене.")]
    public GameObject characterPrefab;
    [Tooltip("Количество юнитов для каждой команды.")]
    public int unitsPerTeam = 4;

    private void Start()
    {
        SpawnTeam(1);
        SpawnTeam(2);
    }

    /// <summary>
    /// Создает и размещает на поле полную команду.
    /// </summary>
    private void SpawnTeam(int teamID)
    {
        int startY = (teamID == 1) ? 0 : GridManager.Instance.height - 1;
        int yIncrement = (teamID == 1) ? 1 : -1;

        for (int i = 0; i < unitsPerTeam; i++)
        {
            // 1. Генерируем "анкету" для нового персонажа.
            CharacterBlueprint blueprint = GenerateRandomBlueprint(teamID, i);
            
            // 2. Ищем для него свободную клетку в зоне расстановки.
            Tile spawnTile = FindFirstAvailableTile(startY, yIncrement, teamID);
            if (spawnTile != null)
            {
                // 3. Создаем объект персонажа из префаба.
                var unitGO = Instantiate(characterPrefab, spawnTile.transform.position, Quaternion.identity);
                var character = unitGO.GetComponent<Character>();

                // 4. Инициализируем персонажа с сгенерированными данными.
                character.Initialize(blueprint);
                character.teamID = teamID; // Присвоение ID автоматически вызовет перекраску.
                character.Mover.PlaceOnGrid(spawnTile.x, spawnTile.y);
            }
        }
    }

    /// <summary>
    /// Процедурно генерирует данные для нового персонажа.
    /// </summary>
    private CharacterBlueprint GenerateRandomBlueprint(int teamID, int unitIndex)
    {
        CharacterBlueprint bp = ScriptableObject.CreateInstance<CharacterBlueprint>();

        bp.characterName = $"Team {teamID} Unit {unitIndex + 1}";
        bp.age = Random.Range(18, 40);
        bp.height = Random.Range(1.65f, 1.95f);
        bp.weight = Random.Range(65.0f, 100.0f);

        // Генерация основных атрибутов по "колоколообразной" кривой.
        bp.Strength = RollStat();
        bp.Endurance = RollStat();
        bp.Accuracy = RollStat();
        bp.Reflexes = RollStat();
        bp.Intellect = RollStat();
        bp.Wits = RollStat();
        bp.Charisma = RollStat();
        bp.Willpower = RollStat();

        // TODO: Заменить на более осмысленную систему распределения классов.
        int classRoll = Random.Range(0, 3);
        if (classRoll == 0) bp.characterClass = "Infantry";
        else if (classRoll == 1) bp.characterClass = "Spearman";
        else bp.characterClass = "Cavalry";
        
        return bp;
    }

    /// <summary>
    /// Генерирует значение стата по биномиальному распределению (от 1 до 6).
    /// Это дает "колоколообразную" кривую, где средние значения (3, 4) выпадают чаще.
    /// </summary>
    private int RollStat()
    {
        int result = 0;
        const int ROLLS_COUNT = 5;
        for (int i = 0; i < ROLLS_COUNT; i++)
        {
            result += Random.Range(0, 2); // 0 или 1
        }
        return result + 1; // Результат от 1 до 6
    }
    
    /// <summary>
    /// Находит первую доступную клетку для спауна в зоне команды.
    /// </summary>
    private Tile FindFirstAvailableTile(int startY, int yIncrement, int teamID)
    {
        // Зона команды 1: y < 3. Зона команды 2: y >= 3.
        int yLimit = (teamID == 1) ? 3 : 2; 

        for (int y = startY; (teamID == 1) ? y < yLimit : y > yLimit; y += yIncrement)
        {
            // Команда 1 итерирует слева направо.
            if (teamID == 1)
            {
                for (int x = 0; x < GridManager.Instance.width; x++)
                {
                    Tile tile = GridManager.Instance.GetTile(x, y);
                    if (tile != null && tile.IsAvailable()) return tile;
                }
            }
            // Команда 2 итерирует справа налево для зеркальности.
            else 
            {
                for (int x = GridManager.Instance.width - 1; x >= 0; x--)
                {
                    Tile tile = GridManager.Instance.GetTile(x, y);
                    if (tile != null && tile.IsAvailable()) return tile;
                }
            }
        }
        return null; 
    }
}