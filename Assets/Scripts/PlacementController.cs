using UnityEngine;
using System.Collections;

/// <summary>
/// Управляет логикой расстановки персонажей на поле в фазе Placement.
/// </summary>
public class PlacementController : MonoBehaviour
{
    private Character selectedCharacter;
    private Coroutine selectionPulseCoroutine;

    private void Update()
    {
        if (BattleManager.Instance.currentPhase != GamePhase.Placement)
        {
            // Если фаза сменилась, а у нас все еще есть выделение, отменяем его.
            if (selectedCharacter != null)
            {
                StopSelectionPulse();
                selectedCharacter = null;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// Обрабатывает клики мыши в фазе расстановки для выбора и перемещения персонажей.
    /// </summary>
    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Попытка выбрать персонажа.
            if (hit.collider.TryGetComponent(out Character character))
            {
                if (selectedCharacter != null)
                {
                    StopSelectionPulse();
                }

                selectedCharacter = character;
                StartSelectionPulse();
                return;
            }

            // Попытка выбрать клетку для перемещения уже выбранного персонажа.
            if (hit.collider.TryGetComponent(out Tile tile) && selectedCharacter != null)
            {
                // Определяем, находится ли клетка в зоне расстановки команды выбранного персонажа.
                bool isPlayer1Zone = selectedCharacter.teamID == 1 && tile.y < 3;
                bool isPlayer2Zone = selectedCharacter.teamID == 2 && tile.y >= 3;

                if ((isPlayer1Zone || isPlayer2Zone) && tile.IsAvailable())
                {
                    StopSelectionPulse();
                    selectedCharacter.Mover.PlaceOnGrid(tile.x, tile.y);
                    selectedCharacter = null; 
                }
            }
        }
    }
    
    /// <summary>
    /// Запускает корутину визуальной пульсации для выделенного объекта.
    /// </summary>
    private void StartSelectionPulse()
    {
        if (selectedCharacter != null)
        {
            selectionPulseCoroutine = StartCoroutine(PulseCoroutine(selectedCharacter.transform));
        }
    }

    /// <summary>
    /// Останавливает корутину пульсации и возвращает объект к исходному размеру.
    /// </summary>
    private void StopSelectionPulse()
    {
        if (selectionPulseCoroutine != null)
        {
            StopCoroutine(selectionPulseCoroutine);
            selectionPulseCoroutine = null;
        }
        
        if (selectedCharacter != null)
        {
            selectedCharacter.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Корутина, реализующая плавную пульсацию размера объекта.
    /// </summary>
    private IEnumerator PulseCoroutine(Transform targetTransform)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.05f;
        float speed = 1f;

        while (true)
        {
            float timer = Mathf.PingPong(Time.time * speed, 1.0f);
            targetTransform.localScale = Vector3.Lerp(originalScale, targetScale, timer);
            yield return null;
        }
    }
}