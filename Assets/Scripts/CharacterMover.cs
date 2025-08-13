using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Управляет перемещением персонажа по сетке, включая поиск пути A*.
/// </summary>
public class CharacterMover : MonoBehaviour
{
    private CharacterStats _stats;
    
    [Header("Grid State")]
    public int currentX;
    public int currentY;
    public bool isMoving { get; private set; }
    private Tile _currentTile;

    [Header("Movement Stats")]
    [Tooltip("Скорость движения персонажа в клетках/сек.")]
    public float moveSpeed = 2.0f;
    
    private const float CAVALRY_SPEED_MULTIPLIER = 1.5f;

    private void Awake()
    {
        _stats = GetComponent<CharacterStats>();
    }

    /// <summary>
    /// Применяет классовые модификаторы к параметрам движения.
    /// </summary>
    public void ApplyClassBonuses()
    {
        if (_stats.blueprint.characterClass == "Cavalry")
        {
            moveSpeed *= CAVALRY_SPEED_MULTIPLIER;
        }
    }

    /// <summary>
    /// Логически и физически размещает персонажа на указанной клетке.
    /// </summary>
    /// <param name="teleport">Если true, персонаж переместится мгновенно.</param>
    public void PlaceOnGrid(int x, int y, bool teleport = false)
    {
        Tile targetTile = GridManager.Instance.GetTile(x, y);
        if (targetTile == null) return;

        if (_currentTile != null) _currentTile.ClearOccupant();
        
        var characterOnTile = GetComponent<Character>();
        targetTile.SetOccupant(characterOnTile);
        
        currentX = x;
        currentY = y;
        _currentTile = targetTile;
        
        Vector3 finalTarget = new Vector3(targetTile.transform.position.x, 1.0f, targetTile.transform.position.z);

        if (teleport)
        {
            transform.position = finalTarget;
            isMoving = false;
        }
        else
        {
            StartCoroutine(MoveToPositionCoroutine(targetTile.transform.position));
        }
    }
    
    /// <summary>
    /// Находит путь до цели и инициирует движение на одну клетку по этому пути.
    /// </summary>
    public void FindPathAndMove(Character target)
    {
        List<Node> path = FindPath(currentX, currentY, target.Mover.currentX, target.Mover.currentY, target);
        if (path != null && path.Count > 1)
        {
            Node nextStep = path[1];
            Tile targetTile = GridManager.Instance.GetTile(nextStep.x, nextStep.y);
            
            if (targetTile != null && targetTile.IsAvailableForMovement())
            {
                targetTile.Reserve();
                PlaceOnGrid(nextStep.x, nextStep.y);
            }
        }
    }
    
    private IEnumerator MoveToPositionCoroutine(Vector3 target)
    {
        isMoving = true;
        Vector3 startingPos = transform.position;
        Vector3 finalTarget = new Vector3(target.x, 1.0f, target.z);

        float distance = Vector3.Distance(startingPos, finalTarget);
        if (distance > 0.1f)
        {
            // TODO: BoardGenerator.TileSize недоступен после загрузки сцены, если генератор отключается.
            // Возможно, стоит хранить tileSize в более глобальном месте.
            float duration = distance / (moveSpeed * 2.0f); // Временный хардкод
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                transform.position = Vector3.Lerp(startingPos, finalTarget, (elapsedTime / duration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        transform.position = finalTarget;
        isMoving = false;
    }


    #region A* Pathfinding
    
    // Внутренняя реализация A* с использованием метрики Чебышёва.
    // Node является приватным классом, используемым только внутри алгоритма.

    private class Node
    {
        public int x, y, gCost, hCost;
        public int fCost => gCost + hCost;
        public Node parent;
        public Node(int x, int y) { this.x = x; this.y = y; }
    }
    
    private List<Node> FindPath(int startX, int startY, int endX, int endY, Character target)
    {
        Node startNode = new Node(startX, startY);
        Node endNode = new Node(endX, endY);

        List<Node> openList = new List<Node> { startNode };
        HashSet<Node> closedList = new HashSet<Node>();

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
                if (openList[i].fCost < currentNode.fCost || (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                    currentNode = openList[i];

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            if (GetDistance(currentNode, endNode) <= _stats.attackRange)
                return RetracePath(startNode, currentNode);

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (IsNodeInList(closedList, neighbour)) continue;
                
                Tile tile = GridManager.Instance.GetTile(neighbour.x, neighbour.y);
                if (tile == null) continue;
                if (!tile.IsAvailableForMovement() && tile.occupiedBy != target) continue;

                int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newCostToNeighbour < neighbour.gCost || !IsNodeInList(openList, neighbour))
                {
                    neighbour.gCost = newCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, endNode);
                    neighbour.parent = currentNode;

                    if (!IsNodeInList(openList, neighbour))
                        openList.Add(neighbour);
                }
            }
        }
        return null;
    }    
    
    private List<Node> RetracePath(Node start, Node end)
    {
        List<Node> path = new List<Node>();
        Node current = end;
        while (current.x != start.x || current.y != start.y)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int checkX = node.x + x;
                int checkY = node.y + y;
                if (GridManager.Instance.GetTile(checkX, checkY) != null) 
                    neighbours.Add(new Node(checkX, checkY));
            }
        return neighbours;
    }

    private int GetDistance(Node a, Node b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    private bool IsNodeInList(IEnumerable<Node> list, Node node)
    {
        foreach (var n in list) if (n.x == node.x && n.y == node.y) return true;
        return false;
    }

    #endregion
}