using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Управляет всеми действиями игрока в фазе подготовки: выбором юнитов
/// и их перемещением между ростером, "скамейкой" и полем боя.
/// </summary>
public class DraftController : MonoBehaviour
{
    private Character _selectedCharacter;
    private List<Tile> _availableSlots = new List<Tile>();
    
    // Отслеживание визуальных эффектов пульсации
    private Dictionary<Transform, Vector3> _originalScales = new Dictionary<Transform, Vector3>();
    private List<Coroutine> _activeCoroutines = new List<Coroutine>();

    // Кэшированные списки тайлов для оптимизации
    private List<Tile> _allRosterTiles;
    private List<Tile> _allStagingTiles;
    private List<Tile> _allBattlefieldTiles;
    
    private BoardGenerator _boardGenerator;
    private bool _isActive = false;

    [Header("Visuals")]
    [Tooltip("Множитель размера объекта при пульсации.")]
    [SerializeField] private float pulseScaleMultiplier = 1.05f;
    [Tooltip("Скорость анимации пульсации.")]
    [SerializeField] private float pulseSpeed = 1.5f;

    private void Update()
    {
        // TODO: Заменить на систему событий для большей эффективности.
        bool shouldBeActive = BattleManager.Instance.currentPhase == GamePhase.Draft || BattleManager.Instance.currentPhase == GamePhase.Placement;

        if (shouldBeActive && !_isActive) Activate();
        else if (!shouldBeActive && _isActive) Deactivate();

        if (!_isActive) return;
        
        if (Input.GetMouseButtonDown(0)) HandleMouseClick();
    }

    private void Activate()
    {
        _isActive = true;
        _boardGenerator = FindObjectOfType<BoardGenerator>();
        CacheRelevantTiles();
        Debug.Log("DraftController Activated");
    }

    private void Deactivate()
    {
        _isActive = false;
        if (_selectedCharacter != null) DeselectAll();
        
        _allRosterTiles?.Clear();
        _allStagingTiles?.Clear();
        _allBattlefieldTiles?.Clear();
        Debug.Log("DraftController Deactivated");
    }

    // Находит и кэширует все тайлы, необходимые для работы контроллера.
    private void CacheRelevantTiles()
    {
        // TODO: Оптимизировать. Вместо FindObjectsOfType, GridManager должен предоставлять списки тайлов по их Purpose.
        var allTiles = FindObjectsOfType<Tile>();
        _allRosterTiles = allTiles.Where(t => t.Purpose == TilePurpose.Roster).ToList();
        _allStagingTiles = allTiles.Where(t => t.Purpose == TilePurpose.StagingArea).ToList();
        _allBattlefieldTiles = allTiles.Where(t => t.Purpose == TilePurpose.Battlefield).ToList();
    }

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out Tile tile))
            {
                if (_selectedCharacter != null && _availableSlots.Contains(tile)) PlaceCharacterOnTile(_selectedCharacter, tile);
                else if (tile.occupiedBy != null) SelectCharacter(tile.occupiedBy);
            }
            else if (hit.collider.TryGetComponent(out Character character))
            {
                SelectCharacter(character);
            }
        }
        else 
        {
            DeselectAll();
        }
    }

    private void SelectCharacter(Character character)
    {
        if (BattleManager.Instance.currentPhase == GamePhase.Draft && BattleManager.Instance.IsLineupConfirmed(character.teamID))
        {
            Debug.Log($"Team {character.teamID}'s lineup is confirmed and cannot be changed during the Draft phase.");
            return;
        }

        DeselectAll();
        _selectedCharacter = character;
        StartPulsing(_selectedCharacter.transform);
        FindAndHighlightAvailableSlots(character);
    }


    // Определяет и подсвечивает все доступные для перемещения слоты для выбранного персонажа.
    private void FindAndHighlightAvailableSlots(Character character)
    {
        var originalTile = GridManager.Instance.GetTile(character.Mover.currentX, character.Mover.currentY);
        if (originalTile == null) return;
        int teamID = character.teamID;

        // В фазе ДРАФТА:
        if (BattleManager.Instance.currentPhase == GamePhase.Draft)
        {
            // Проверяем, подтвердил ли игрок свой состав. Если да - ничего не подсвечиваем.
            // ЭТА ПРОВЕРКА УЖЕ ЕСТЬ В SelectCharacter, но здесь она для дополнительной надежности.
            if (BattleManager.Instance.IsLineupConfirmed(teamID)) return;
            // В фазе ДРАФТА можно двигаться только между ростером и скамейкой.
            switch (originalTile.Purpose)
            {
                case TilePurpose.Roster:
                    HighlightEmptySlotsInList(_allStagingTiles, teamID, originalTile);
                    HighlightEmptySlotsInList(_allRosterTiles, teamID, originalTile);
                    break;
                case TilePurpose.StagingArea:
                    HighlightEmptySlotsInList(_allRosterTiles, teamID, originalTile);
                    HighlightEmptySlotsInList(_allStagingTiles, teamID, originalTile);
                    break;
            }
        }
        else if (BattleManager.Instance.currentPhase == GamePhase.Placement)
        {
            // В фазе РАССТАНОВКИ можно двигаться только между скамейкой и полем боя.
            // Юниты в ростере больше не доступны.
            switch (originalTile.Purpose)
            {
                case TilePurpose.StagingArea:
                    HighlightEmptySlotsInList(_allStagingTiles, teamID, originalTile);
                    HighlightEmptySlotsOnBattlefield(teamID);
                    break;
                case TilePurpose.Battlefield:
                    HighlightEmptySlotsInList(_allStagingTiles, teamID, originalTile);
                    HighlightEmptySlotsOnBattlefield(teamID, originalTile);
                    break;
            }
        }
    }

    private void HighlightEmptySlotsInList(List<Tile> tileList, int teamID, Tile ignoredTile = null)
    {
        if (tileList == null) return;
        foreach (var tile in tileList)
        {
            if (tile == ignoredTile) continue;
            if (tile.OwnerPlayerID != teamID) continue; 
            if (tile.occupiedBy == null)
            {
                // TODO: Добавить проверку на соответствие роли персонажа и слота.
                _availableSlots.Add(tile);
                StartPulsing(tile.transform);
            }
        }
    }

    private void HighlightEmptySlotsOnBattlefield(int teamID, Tile ignoredTile = null)
    {
        if (_allBattlefieldTiles == null) return;
        foreach (var tile in _allBattlefieldTiles)
        {
            if (tile == ignoredTile) continue;
            if (tile.occupiedBy != null) continue;
            
            if (IsInPlayerPlacementZone(tile, teamID))
            {
                 _availableSlots.Add(tile);
                 StartPulsing(tile.transform);
            }
        }
    }

    // Определяет, находится ли тайл в зоне расстановки указанной команды.
    private bool IsInPlayerPlacementZone(Tile tile, int teamID)
    {
        if (_boardGenerator == null) return false;

        Vector2Int battlefieldSize = _boardGenerator.battlefieldSize;

        switch (_boardGenerator.currentScenario)
        {
            case BattleScenario.TwoPlayersVersus:
                if (teamID == 1) return tile.y < battlefieldSize.y / 2;
                if (teamID == 2) return tile.y >= battlefieldSize.y / 2;
                break;
            case BattleScenario.FourPlayersFreeForAll:
            case BattleScenario.FourPlayersTeams:
                // Заглушка для будущих режимов.
                Debug.LogWarning("Placement logic for 4 players is not implemented yet.");
                break;
        }

        return false;
    }

    private void PlaceCharacterOnTile(Character character, Tile targetTile)
    {
        var originalTile = GridManager.Instance.GetTile(character.Mover.currentX, character.Mover.currentY);

        if (originalTile != null && originalTile.Purpose == TilePurpose.Battlefield)
        {
            BattleManager.Instance.UnregisterFighter(character);
        }
        if (targetTile.Purpose == TilePurpose.Battlefield)
        {
            BattleManager.Instance.RegisterFighter(character);
        }

        if (originalTile != null)
        {
            originalTile.occupiedBy = null;
        }
        character.Mover.PlaceOnGrid(targetTile.x, targetTile.y);

        DeselectAll();
    }
    
    private void DeselectAll()
    {
        foreach (var coroutine in _activeCoroutines)
            if (coroutine != null) StopCoroutine(coroutine);
        _activeCoroutines.Clear();

        foreach (var entry in _originalScales)
            if (entry.Key != null) entry.Key.localScale = entry.Value;
        _originalScales.Clear();

        _availableSlots.Clear();
        _selectedCharacter = null;
    }
    
    private void StartPulsing(Transform targetTransform)
    {
        if (!_originalScales.ContainsKey(targetTransform))
        {
            _originalScales[targetTransform] = targetTransform.localScale;
        }
        var pulseCoroutine = StartCoroutine(PulseCoroutine(targetTransform));
        _activeCoroutines.Add(pulseCoroutine);
    }
    
    private IEnumerator PulseCoroutine(Transform targetTransform)
    {
        if (!_originalScales.TryGetValue(targetTransform, out Vector3 originalScale))
        {
            yield break; // Безопасный выход, если scale не был сохранен
        }
        
        Vector3 targetScale = originalScale * pulseScaleMultiplier;

        while (true)
        {
            float timer = Mathf.PingPong(Time.time * pulseSpeed, 1.0f);
            targetTransform.localScale = Vector3.Lerp(originalScale, targetScale, timer);
            yield return null;
        }
    }
    
    private void OnDisable()
    {
        if (_selectedCharacter != null || _originalScales.Count > 0)
        {
            DeselectAll();
        }
    }
}