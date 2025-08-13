using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет доступом ко всем клеткам (Tile) на игровом поле.
/// Реализован как синглтон для глобального доступа.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    // Эти поля устарели и оставлены для временной совместимости
    // со старыми классами. Будут удалены в будущем.
    public int width = 6;
    public int height = 6;
    
    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Регистрирует новый тайл в сетке. Вызывается из BoardGenerator.
    /// </summary>
    public void RegisterTile(Vector2Int coords, Tile tile)
    {
        if (!_tiles.ContainsKey(coords))
        {
            _tiles[coords] = tile;
        }
        else
        {
            Debug.LogWarning($"Grid coordinate {coords} is already occupied. Ignoring new tile.");
        }
    }

    /// <summary>
    /// Полностью очищает сетку и уничтожает все дочерние объекты тайлов.
    /// </summary>
    public void ClearGrid()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }
        _tiles.Clear();
    }

    /// <summary>
    /// Возвращает объект клетки по ее координатам.
    /// </summary>
    /// <returns>Объект Tile или null, если клетка с такими координатами не существует.</returns>
    public Tile GetTile(int x, int y)
    {
        _tiles.TryGetValue(new Vector2Int(x, y), out Tile tile);
        return tile;
    }
}