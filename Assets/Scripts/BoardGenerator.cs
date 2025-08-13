using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Отвечает за полную процедурную генерацию игровой доски: поля боя,
/// зон подготовки игроков, ростеров, юнитов и препятствий.
/// </summary>
public class BoardGenerator : MonoBehaviour
{
    [Header("Scenario Settings")]
    [Tooltip("Тип боя, определяющий компоновку и правила.")]
    public BattleScenario currentScenario = BattleScenario.TwoPlayersVersus;
    [Tooltip("Список активных игроков и их расположение на доске.")]
    public List<PlayerLayoutSetup> activePlayers;

    [Header("Board Layout")]
    [Tooltip("Размер центрального поля боя.")]
    public Vector2Int battlefieldSize = new Vector2Int(6, 6);
    [Tooltip("Список активных ролей, доступных в этом сценарии.")]
    public List<RoleType> availableActiveRoles;
    [Tooltip("Количество слотов для базовых бойцов (Men-at-Arms).")]
    public int menAtArmsCount = 4;
    [Tooltip("Максимальный размер ростера (будет выстроен в ряды по 8).")]
    public int rosterSize = 24;
    [Tooltip("Ширина 'воздушных' отступов между зонами.")]
    [Min(1)] public int separatorWidth = 1;
    [Tooltip("Количество случайных препятствий на поле боя.")]
    [Range(0, 20)] public int obstacleCount = 4;

    [Header("Assets & Dependencies")]
    [Tooltip("Физический размер одной клетки в мировых координатах.")]
    public float tileSize = 2.0f;
    [Tooltip("Префаб тайла для генерации.")]
    public GameObject tilePrefab;
    [Tooltip("Префаб персонажа для спауна.")]
    public GameObject characterPrefab;
    [Tooltip("Материал для тайлов-препятствий.")]
    public Material obstacleMaterial;
    [Tooltip("Ссылка на GridManager для регистрации созданных тайлов.")]
    public GridManager gridManager;

    public static float TileSize { get; private set; }

    private struct BoardZone
    {
        public RectInt Bounds;
        public BoardSide Side;
        public bool IsRosterZone;
        public int OwnerPlayerID;
    }

    private List<BoardZone> _allZones;
    
    private void Awake()
    {
        TileSize = tileSize;
    }

    private void Start()
    {
        GenerateBoard();
    }

    /// <summary>
    /// Главный метод, запускающий всю процедуру генерации доски.
    /// </summary>
    public void GenerateBoard()
    {
        Debug.Log("Starting board generation...");

        if (gridManager == null || tilePrefab == null || characterPrefab == null)
        {
            Debug.LogError("BoardGenerator is missing dependencies. Aborting generation.", this);
            return;
        }

        gridManager.ClearGrid();
        
        CalculateZoneBounds();
        CreateTiles();
        PopulateRosters();
        GenerateObstacles();

        Debug.Log("Board generation finished.");
    }
    
    #region Zone Calculation and Tile Creation

    // Рассчитывает размеры и положение всех зон на доске.
    private void CalculateZoneBounds()
    {
        _allZones = new List<BoardZone>();

        int frontRowWidth = availableActiveRoles.Count;
        int backRowWidth = Mathf.Min(menAtArmsCount, 8);
        int overflowRowWidth = Mathf.Max(0, menAtArmsCount - 8);

        int handWidth = Mathf.Max(frontRowWidth, backRowWidth, overflowRowWidth);
        int handHeight = (frontRowWidth > 0 ? 1 : 0) + (backRowWidth > 0 ? 1 : 0) + (overflowRowWidth > 0 ? 1 : 0);
        
        int rosterWidth = 8;
        int rosterHeight = Mathf.CeilToInt((float)rosterSize / rosterWidth);
        
        _allZones.Add(new BoardZone { Bounds = new RectInt(0, 0, battlefieldSize.x, battlefieldSize.y), Side = BoardSide.Center });

        switch (currentScenario)
        {
            case BattleScenario.TwoPlayersVersus:
                CreatePlayerZones(BoardSide.South, handWidth, handHeight, rosterWidth, rosterHeight);
                CreatePlayerZones(BoardSide.North, handWidth, handHeight, rosterWidth, rosterHeight);
                break;
            case BattleScenario.FourPlayersFreeForAll:
            case BattleScenario.FourPlayersTeams:
                CreatePlayerZones(BoardSide.South, handWidth, handHeight, rosterWidth, rosterHeight);
                CreatePlayerZones(BoardSide.North, handWidth, handHeight, rosterWidth, rosterHeight);
                CreatePlayerZones(BoardSide.West, handWidth, handHeight, rosterWidth, rosterHeight);
                CreatePlayerZones(BoardSide.East, handWidth, handHeight, rosterWidth, rosterHeight);
                break;
        }

        AssignZoneOwners();
    }

    // Создаёт прифронтовую ("рука") и тыловую ("ростер") зоны для указанной стороны доски.
    private void CreatePlayerZones(BoardSide side, int handWidth, int handHeight, int rosterWidth, int rosterHeight)
    {
        RectInt handBounds, rosterBounds;

        int handOffsetX = (battlefieldSize.x - handWidth) / 2;
        int handOffsetY = (battlefieldSize.y - handWidth) / 2; 

        int rosterOffsetX = (handWidth - rosterWidth) / 2;
        int rosterOffsetY = (handWidth - rosterWidth) / 2;

        switch (side)
        {
            case BoardSide.South:
                handBounds = new RectInt(handOffsetX, -(handHeight + separatorWidth), handWidth, handHeight);
                rosterBounds = new RectInt(handBounds.x + rosterOffsetX, handBounds.yMin - rosterHeight - separatorWidth, rosterWidth, rosterHeight);
                break;
            case BoardSide.North:
                handBounds = new RectInt(handOffsetX, battlefieldSize.y + separatorWidth, handWidth, handHeight);
                rosterBounds = new RectInt(handBounds.x + rosterOffsetX, handBounds.yMax + separatorWidth, rosterWidth, rosterHeight);
                break;
            case BoardSide.West:
                handBounds = new RectInt(-(handHeight + separatorWidth), handOffsetY, handHeight, handWidth);
                rosterBounds = new RectInt(handBounds.xMin - rosterHeight - separatorWidth, handBounds.y + rosterOffsetY, rosterHeight, rosterWidth);
                break;
            default: // East
                handBounds = new RectInt(battlefieldSize.x + separatorWidth, handOffsetY, handHeight, handWidth);
                rosterBounds = new RectInt(handBounds.xMax + separatorWidth, handBounds.y + rosterOffsetY, rosterHeight, rosterWidth);
                break;
        }

        _allZones.Add(new BoardZone { Bounds = handBounds, Side = side, IsRosterZone = false });
        _allZones.Add(new BoardZone { Bounds = rosterBounds, Side = side, IsRosterZone = true });
    }

    // Присваивает ID игроков-владельцев соответствующим зонам.
    private void AssignZoneOwners()
    {
        var playerSideMapping = activePlayers.ToDictionary(p => p.side, p => p.playerID);
        for (int i = 0; i < _allZones.Count; i++)
        {
            var zone = _allZones[i];
            if (playerSideMapping.TryGetValue(zone.Side, out int ownerID))
            {
                zone.OwnerPlayerID = ownerID;
                _allZones[i] = zone;
            }
        }
    }

    // Создаёт GameObjects тайлов для всех рассчитанных зон.
    private void CreateTiles()
    {
        foreach (var zone in _allZones)
        {
            for (int x = zone.Bounds.xMin; x < zone.Bounds.xMax; x++)
            {
                for (int y = zone.Bounds.yMin; y < zone.Bounds.yMax; y++)
                {
                    var coords = new Vector2Int(x, y);
                    if (gridManager.GetTile(x, y) != null) continue;

                    var newTileGO = Instantiate(tilePrefab, new Vector3(x * tileSize, 0, y * tileSize), Quaternion.identity, gridManager.transform);
                    newTileGO.name = $"Tile_{x}_{y}";
                    
                    var tileComponent = newTileGO.GetComponent<Tile>();
                    if (tileComponent != null)
                    {
                        tileComponent.x = x;
                        tileComponent.y = y;
                        SetTileProperties(tileComponent, zone, coords);
                        gridManager.RegisterTile(coords, tileComponent);
                    }
                }
            }
        }
    }

    // Назначает свойства для одного тайла на основе его зоны и координат.
    private void SetTileProperties(Tile tile, BoardZone zone, Vector2Int coords)
    {
        tile.OwnerPlayerID = zone.OwnerPlayerID;
        tile.Elevation = ElevationType.Flat;
        tile.MovementCost = MovementType.Normal;
        tile.Passability = PassabilityType.Passable;

        if (zone.Side == BoardSide.Center)
        {
            tile.Purpose = TilePurpose.Battlefield;
            return;
        }

        if (zone.OwnerPlayerID == 0)
        {
            MakeTileImpassable(tile);
            return;
        }

        if (zone.IsRosterZone)
        {
            tile.Purpose = TilePurpose.Roster;
        }
        else
        {
            AssignStagingAreaProperties(tile, zone, coords);
        }
    }

    // Определяет, является ли тайл слотом в зоне подготовки, и назначает ему роль.
    private void AssignStagingAreaProperties(Tile tile, BoardZone zone, Vector2Int coords)
    {
        int localX, localY;
        if (zone.Side == BoardSide.South || zone.Side == BoardSide.North)
        {
            localX = coords.x - zone.Bounds.xMin;
            localY = (zone.Side == BoardSide.South) ? zone.Bounds.yMax - 1 - coords.y : coords.y - zone.Bounds.yMin;
        }
        else // West or East
        {
            localX = coords.y - zone.Bounds.yMin;
            localY = (zone.Side == BoardSide.West) ? zone.Bounds.xMax - 1 - coords.x : coords.x - zone.Bounds.xMin;
        }

        int frontRowWidth = availableActiveRoles.Count;
        int backRowWidth = Mathf.Min(menAtArmsCount, 8);
        int overflowRowWidth = Mathf.Max(0, menAtArmsCount - 8);
        
        int maxRowWidth = zone.Side is BoardSide.South or BoardSide.North ? zone.Bounds.width : zone.Bounds.height;
        
        int frontRowOffset = (maxRowWidth - frontRowWidth) / 2;
        int backRowOffset = (maxRowWidth - backRowWidth) / 2;
        int overflowRowOffset = (maxRowWidth - overflowRowWidth) / 2;

        if (localY == 0 && frontRowWidth > 0) // Передний ряд (активные роли)
        {
            if (localX >= frontRowOffset && localX < frontRowOffset + frontRowWidth)
            {
                tile.Purpose = TilePurpose.StagingArea;
                tile.AssignedRole = availableActiveRoles[localX - frontRowOffset];
            }
            else MakeTileImpassable(tile);
        }
        else if (localY == 1 && backRowWidth > 0) // Второй ряд (Men-at-Arms)
        {
            if (localX >= backRowOffset && localX < backRowOffset + backRowWidth)
            {
                tile.Purpose = TilePurpose.StagingArea;
                tile.AssignedRole = RoleType.ManAtArms;
            }
            else MakeTileImpassable(tile);
        }
        else if (localY == 2 && overflowRowWidth > 0) // Третий (аварийный) ряд
        {
             if (localX >= overflowRowOffset && localX < overflowRowOffset + overflowRowWidth)
             {
                 tile.Purpose = TilePurpose.StagingArea;
                 tile.AssignedRole = RoleType.ManAtArms;
             }
             else MakeTileImpassable(tile);
        }
        else
        {
            MakeTileImpassable(tile);
        }
    }
    
    // Делает тайл непроходимым и невидимым.
    private void MakeTileImpassable(Tile tile)
    {
        tile.Purpose = TilePurpose.Impassable;
        tile.Passability = PassabilityType.Impassable;
        tile.gameObject.SetActive(false); 
    }
    
    #endregion

    #region Unit and Obstacle Generation

    // Создаёт и размещает юнитов в зонах ростера для всех активных игроков.
    private void PopulateRosters()
    {
        var playerSetupsByID = activePlayers.ToDictionary(p => p.playerID, p => p);
        var rosterZones = _allZones.FindAll(z => z.IsRosterZone && z.OwnerPlayerID != 0);

        foreach (var zone in rosterZones)
        {
            if (!playerSetupsByID.TryGetValue(zone.OwnerPlayerID, out PlayerLayoutSetup setup)) continue;

            int unitIndex = 0;
            
            bool isReversedY = (zone.Side == BoardSide.North);
            bool isReversedX = (zone.Side == BoardSide.North);

            int yStart = isReversedY ? zone.Bounds.yMin : zone.Bounds.yMax - 1;
            int yEnd = isReversedY ? zone.Bounds.yMax : zone.Bounds.yMin - 1;
            int yStep = isReversedY ? 1 : -1;

            int xStart = isReversedX ? zone.Bounds.xMax - 1 : zone.Bounds.xMin;
            int xEnd = isReversedX ? zone.Bounds.xMin - 1 : zone.Bounds.xMax;
            int xStep = isReversedX ? -1 : 1;

            for (int y = yStart; y != yEnd; y += yStep)
            {
                for (int x = xStart; x != xEnd; x += xStep)
                {
                    if (unitIndex >= setup.unitsToSpawn) break;

                    var tile = gridManager.GetTile(x, y);
                    if (tile != null)
                    {
                        var blueprint = GenerateRandomBlueprint(zone.OwnerPlayerID, unitIndex);
                        var unitGO = Instantiate(characterPrefab);
                        var character = unitGO.GetComponent<Character>();
                        
                        character.Initialize(blueprint);
                        character.teamID = zone.OwnerPlayerID;
                        character.Mover.PlaceOnGrid(tile.x, tile.y, true);

                        unitIndex++;
                    }
                }
                if (unitIndex >= setup.unitsToSpawn) break;
            }
        }
    }
    
    // Создаёт случайные непроходимые препятствия на поле боя.
    private void GenerateObstacles()
    {
        // TODO: Оптимизировать. Вместо FindObjectsOfType, GridManager должен предоставлять список тайлов по Purpose.
        var battlefieldTiles = FindObjectsOfType<Tile>().Where(t => t.Purpose == TilePurpose.Battlefield).ToList();
        
        // Перемешиваем список, чтобы получить случайный порядок (алгоритм Фишера-Йетса).
        for (int i = 0; i < battlefieldTiles.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, battlefieldTiles.Count);
            (battlefieldTiles[i], battlefieldTiles[randomIndex]) = (battlefieldTiles[randomIndex], battlefieldTiles[i]);
        }
        
        int obstaclesToCreate = Mathf.Min(obstacleCount, battlefieldTiles.Count);
        for (int i = 0; i < obstaclesToCreate; i++)
        {
            var tileToBlock = battlefieldTiles[i];
            
            tileToBlock.Passability = PassabilityType.Impassable;
            tileToBlock.Purpose = TilePurpose.Impassable;
            
            var renderer = tileToBlock.GetComponent<Renderer>();
            if (renderer != null && obstacleMaterial != null)
            {
                renderer.material = obstacleMaterial;
            }
            
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(tileToBlock.transform, false);
            marker.transform.localPosition = new Vector3(0, 0.75f, 0);
            marker.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            
            Destroy(marker.GetComponent<Collider>());
            
            var markerRenderer = marker.GetComponent<Renderer>();
            if(markerRenderer != null && obstacleMaterial != null)
            {
                markerRenderer.material = obstacleMaterial;
            }
        }
    }
    
    #endregion

    #region Blueprint Generation
    
    // Создаёт ScriptableObject с "анкетой" для нового персонажа.
    private CharacterBlueprint GenerateRandomBlueprint(int teamID, int unitIndex)
    {
        CharacterBlueprint bp = ScriptableObject.CreateInstance<CharacterBlueprint>();

        bp.characterName = $"Team {teamID} Unit {unitIndex + 1}";
        bp.age = Random.Range(18, 40);
        bp.height = Random.Range(1.65f, 1.95f);
        bp.weight = Random.Range(65.0f, 100.0f);

        bp.Strength = RollStat();
        bp.Endurance = RollStat();
        bp.Accuracy = RollStat();
        bp.Reflexes = RollStat();
        bp.Intellect = RollStat();
        bp.Wits = RollStat();
        bp.Charisma = RollStat();
        bp.Willpower = RollStat();
        
        int classRoll = Random.Range(0, 3);
        if (classRoll == 0) bp.characterClass = "Infantry";
        else if (classRoll == 1) bp.characterClass = "Spearman";
        else bp.characterClass = "Cavalry";

        return bp;
    }

    // Генерирует значение стата по колоколообразной кривой.
    private int RollStat()
    {
        int result = 0;
        const int ROLLS_COUNT = 5;
        for (int i = 0; i < ROLLS_COUNT; i++)
        {
            result += Random.Range(0, 2);
        }
        return result + 1;
    }
    
    #endregion
}