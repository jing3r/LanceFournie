using UnityEngine;

/// <summary>
/// Представляет одну клетку на игровом поле.
/// Хранит свои координаты и информацию о том, кто ее занимает.
/// </summary>
public class Tile : MonoBehaviour
{
    [Tooltip("Координата X на сетке.")]
    public int x;
    [Tooltip("Координата Y на сетке.")]
    public int y;

    [Tooltip("Ссылка на персонажа, который в данный момент занимает эту клетку.")]
    public Character occupiedBy;
    
    // Флаг, используемый для предотвращения "состояния гонки",
    // когда два персонажа пытаются пойти на одну и ту же клетку в одном кадре.
    private bool isReserved = false;

    /// <summary>
    /// Проверяет, доступна ли клетка для перемещения.
    /// </summary>
    /// <returns>True, если клетка не занята и не зарезервирована.</returns>
    public bool IsAvailable()
    {
        return occupiedBy == null && !isReserved;
    }

    /// <summary>
    /// Устанавливает персонажа как "владельца" этой клетки.
    /// </summary>
    public void SetOccupant(Character character)
    {
        occupiedBy = character;
        isReserved = false; // Когда юнит физически пришел, резервация снимается.
    }

    /// <summary>
    /// Освобождает клетку.
    /// </summary>
    public void ClearOccupant()
    {
        occupiedBy = null;
        isReserved = false;
    }

    /// <summary>
    /// Мгновенно резервирует клетку, делая ее недоступной для других.
    /// </summary>
    public void Reserve()
    {
        isReserved = true;
    }
}