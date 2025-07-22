using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Управляет всем, что связано с перемещением персонажа по сетке.
/// Включает в себя логику поиска пути A* и плавное визуальное движение.
/// </summary>
public class CharacterMover : MonoBehaviour
{
    private CharacterStats stats;
    
    [Header("Grid & State")]
    public int currentX;
    public int currentY;
    public bool isMoving { get; private set; }
    private Tile currentTile;

    [Header("Movement Stats")]
    [Tooltip("Скорость движения персонажа в клетках/сек.")]
    public float moveSpeed = 2.0f;
    
    // Множитель скорости для кавалерии. Вынесен в константу для читаемости и легкой правки.
    private const float CAVALRY_SPEED_MULTIPLIER = 1.5f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    /// <summary>
    /// Применяет классовые модификаторы к параметрам движения.
    /// </summary>
    public void ApplyClassBonuses()
    {
        if (stats.blueprint.characterClass == "Cavalry")
        {
            moveSpeed *= CAVALRY_SPEED_MULTIPLIER;
        }
    }

    /// <summary>
    /// Логически и физически размещает персонажа на указанной клетке.
    /// </summary>
    public void PlaceOnGrid(int x, int y)
    {
        Tile targetTile = GridManager.Instance.GetTile(x, y);
        if (targetTile != null)
        {
            if (currentTile != null) currentTile.ClearOccupant();
            
            var characterOnTile = GetComponent<Character>();
            targetTile.SetOccupant(characterOnTile);
            
            currentX = x;
            currentY = y;
            currentTile = targetTile;
            
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
            
            // Система резервирования: перед началом движения клетка помечается как "занятая",
            // чтобы другой персонаж не попытался пойти на нее в том же кадре.
            if (targetTile != null && targetTile.IsAvailable())
            {
                targetTile.Reserve();
                PlaceOnGrid(nextStep.x, nextStep.y);
            }
        }
    }
    
    /// <summary>
    /// Корутина для плавного визуального перемещения объекта к цели.
    /// </summary>
    private IEnumerator MoveToPositionCoroutine(Vector3 target)
    {
        isMoving = true;
        Vector3 startingPos = transform.position;
        // Цель смещается по высоте, чтобы персонаж стоял "на" клетке, а не "в" ней.
        Vector3 finalTarget = new Vector3(target.x, 1.0f, target.z);

        float distance = Vector3.Distance(startingPos, finalTarget);
        if (distance > 0.1f)
        {
            float duration = distance / (moveSpeed * GridManager.Instance.tileSize);
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
    
    // Внутренний класс Node используется исключительно для хранения данных узла в алгоритме A*.
    // Он приватный, так как не нужен никаким другим системам.
    private class Node
    {
        public int x, y;
        public int gCost; // Стоимость пути от старта до этого узла
        public int hCost; // Эвристическая стоимость от этого узла до цели (Расстояние Чебышёва)
        public int fCost => gCost + hCost; // Общая стоимость
        public Node parent; // Узел, из которого мы пришли в текущий

        public Node(int x, int y) { this.x = x; this.y = y; }
    }
    
    /// <summary>
    /// Реализация алгоритма A* для поиска пути.
    /// </summary>
    /// <returns>Список узлов, представляющих путь, или null, если путь не найден.</returns>
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

            if (GetDistance(currentNode, endNode) <= stats.attackRange)
                return RetracePath(startNode, currentNode);

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (IsNodeInList(closedList, neighbour)) continue;
                
                Tile tile = GridManager.Instance.GetTile(neighbour.x, neighbour.y);
                if (tile == null) continue;

                // Клетки, занятые другими (не целью), считаются непроходимыми.
                if (!tile.IsAvailable() && tile.occupiedBy != target) continue;

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
    
    // Вспомогательные функции для A*
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
                if (checkX >= 0 && checkX < GridManager.Instance.width && checkY >= 0 && checkY < GridManager.Instance.height)
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