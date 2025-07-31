using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет отображением и позиционированием полоски здоровья в режиме Screen Space.
/// Этот компонент должен находиться на префабе Slider'а.
/// </summary>
[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    private Slider _slider;
    private Character _owner;
    private RectTransform _rectTransform;
    private Camera _mainCamera;

    [Header("Positioning")]
    [Tooltip("Смещение полоски здоровья по вертикали относительно пивота персонажа в мировых координатах.")]
    [SerializeField] private float _heightOffset = 2.5f;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _rectTransform = GetComponent<RectTransform>();
        _mainCamera = Camera.main;
    }

    /// <summary>
    /// Инициализирует полоску здоровья, привязывает ее к владельцу и главному Canvas'у на сцене,
    /// а также подписывается на события изменения здоровья.
    /// </summary>
    public void Initialize(Character character)
    {
        _owner = character;

        if (_owner == null || _owner.Stats == null)
        {
            Destroy(gameObject);
            return;
        }

        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            transform.SetParent(canvas.transform, false);
        }
        else
        {
            Debug.LogError("No Screen Space Canvas found in the scene for HealthBar.", this);
        }

        UpdateHealthBar(_owner.Stats.currentHealth, _owner.Stats.maxHealth);
        _owner.Stats.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDestroy()
    {
        // Отписка от событий при уничтожении объекта обязательна во избежание утечек памяти.
        if (_owner != null && _owner.Stats != null)
        {
            _owner.Stats.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        _slider.maxValue = maxHealth;
        _slider.value = currentHealth;
    }

    // Позиция обновляется в LateUpdate, чтобы гарантировать, что персонаж и камера
    // уже завершили свое движение в текущем кадре.
    private void LateUpdate()
    {
        if (_owner == null || _mainCamera == null)
        {
            // Самоуничтожение, если владелец был удален со сцены.
            if (_owner == null) Destroy(gameObject);
            return;
        }

        // Преобразуем 3D позицию над головой персонажа в 2D точку на экране.
        Vector3 worldPosition = _owner.transform.position + Vector3.up * _heightOffset;
        Vector3 screenPoint = _mainCamera.WorldToScreenPoint(worldPosition);

        // Скрываем полоску, если ее владелец находится за плоскостью камеры.
        bool isVisible = screenPoint.z > 0;
        if (gameObject.activeSelf != isVisible)
        {
            gameObject.SetActive(isVisible);
        }
        
        if (isVisible)
        {
            _rectTransform.position = screenPoint;
        }
    }
}