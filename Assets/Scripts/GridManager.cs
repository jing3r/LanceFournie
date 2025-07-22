using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отвечает за создание и управление сеткой (полем боя).
/// Предоставляет доступ к любой клетке (Tile) по ее координатам.
/// Реализован как синглтон.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    [Tooltip("Префаб клетки, из которой будет строиться поле.")]
    public GameObject tilePrefab;
    [Tooltip("Ширина поля в клетках.")]
    public int width = 6;
    [Tooltip("Высота поля в клетках.")]
    public int height = 6;
    [Tooltip("Физический размер одной клетки в мировых координатах (метрах).")]
    public float tileSize = 2.0f;

    // Словарь для быстрого доступа к клеткам по их координатам.
    private Dictionary<Vector2Int, Tile> tiles;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        GenerateGrid();
    }

    /// <summary>
    /// Генерирует поле боя из префабов клеток.
    /// </summary>
    private void GenerateGrid()
    {
        tiles = new Dictionary<Vector2Int, Tile>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 tilePosition = new Vector3(x * tileSize, 0, y * tileSize);
                var spawnedTile = Instantiate(tilePrefab, tilePosition, Quaternion.identity, this.transform);
                spawnedTile.name = $"Tile_{x}_{y}";

                var tileScript = spawnedTile.GetComponent<Tile>();
                tileScript.x = x;
                tileScript.y = y;

                tiles[new Vector2Int(x, y)] = tileScript;
            }
        }
    }

    /// <summary>
    /// Возвращает объект клетки по ее координатам.
    /// </summary>
    /// <returns>Объект Tile или null, если клетка с такими координатами не существует.</returns>
    public Tile GetTile(int x, int y)
    {
        tiles.TryGetValue(new Vector2Int(x, y), out Tile tile);
        return tile;
    }
}