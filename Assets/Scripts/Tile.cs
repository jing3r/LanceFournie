using UnityEngine;

/// <summary>
/// Представляет одну клетку на игровом поле, храня ее координаты и свойства.
/// </summary>
public class Tile : MonoBehaviour
{
    [Tooltip("Координата X на сетке.")]
    public int x;
    [Tooltip("Координата Y на сетке.")]
    public int y;

    [Tooltip("Ссылка на персонажа, который в данный момент занимает эту клетку.")]
    public Character occupiedBy;
    
    // Предотвращает попытку нескольких юнитов занять одну клетку в одном кадре.
    private bool isReserved = false;

    // Свойства тайла
    public TilePurpose Purpose { get; set; }
    public RoleType AssignedRole { get; set; }
    public ElevationType Elevation { get; set; }
    public MovementType MovementCost { get; set; }
    public PassabilityType Passability { get; set; }
    public int OwnerPlayerID { get; set; }
    
    /// <summary>
    /// Проверяет, доступна ли клетка для перемещения персонажа.
    /// </summary>
    public bool IsAvailableForMovement()
    {
        return occupiedBy == null && !isReserved && Passability == PassabilityType.Passable;
    }
    
    /// <summary>
    /// Устанавливает персонажа как occupant'а этой клетки.
    /// </summary>
    public void SetOccupant(Character character)
    {
        occupiedBy = character;
        isReserved = false; // Резервация снимается после того, как юнит занял тайл.
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
    /// Резервирует клетку, делая ее временно недоступной для других.
    /// </summary>
    public void Reserve()
    {
        isReserved = true;
    }
}