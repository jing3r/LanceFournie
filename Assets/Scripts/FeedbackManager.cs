using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Централизованный менеджер для отображения визуальной обратной связи в бою
/// (всплывающий урон, статусы и т.д.). Использует адаптируемый пул объектов.
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    [Header("Prefabs")]
    [Tooltip("Префаб для отображения всплывающего текста. Должен иметь компонент FloatingText.")]
    public FloatingText floatingTextPrefab;
    [Tooltip("Префаб для отображения полоски здоровья. Должен иметь компонент HealthBar.")]
    public HealthBar healthBarPrefab;

    [Header("Object Pooling")]
    [Tooltip("Начальный размер пула. Должен покрывать большинство пиковых нагрузок.")]
    [SerializeField] private int initialPoolSize = 50;
    
    [Header("Visual Settings")]
    [Tooltip("Высота в метрах, на которой будет появляться всплывающий текст относительно пивота цели.")]
    [SerializeField] private float textSpawnHeightOffset = 3.0f;
    
    private Queue<FloatingText> _textPoolQueue = new Queue<FloatingText>();
    private List<FloatingText> _allPooledObjects = new List<FloatingText>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            FloatingText newText = Instantiate(floatingTextPrefab, transform);
            newText.gameObject.SetActive(false);
            _allPooledObjects.Add(newText);
            _textPoolQueue.Enqueue(newText);
        }
    }
    
    private FloatingText GetTextFromPool()
    {
        if (_textPoolQueue.Count > 0)
        {
            return _textPoolQueue.Dequeue();
        }

        // Защита от пиковой нагрузки, если пул исчерпан.
        FloatingText newText = Instantiate(floatingTextPrefab, transform);
        _allPooledObjects.Add(newText);
        Debug.LogWarning("FeedbackManager pool was depleted. A new object was created at runtime. Consider increasing the initial pool size.");
        return newText;
    }

    /// <summary>
    /// Возвращает экземпляр FloatingText обратно в пул объектов.
    /// </summary>
    public void ReturnTextToPool(FloatingText text)
    {
        text.gameObject.SetActive(false);
        _textPoolQueue.Enqueue(text);
    }
    
    /// <summary>
    /// Сбрасывает пул в исходное состояние, уничтожая объекты, созданные во время боя сверх
    /// начального лимита. Предотвращает утечки памяти между сражениями.
    /// </summary>
    public void ResetPool()
    {
        for (int i = _allPooledObjects.Count - 1; i >= initialPoolSize; i--)
        {
            FloatingText extraObject = _allPooledObjects[i];
            Destroy(extraObject.gameObject);
            _allPooledObjects.RemoveAt(i);
        }

        _textPoolQueue.Clear();
        
        foreach (var textObject in _allPooledObjects)
        {
            textObject.gameObject.SetActive(false);
            _textPoolQueue.Enqueue(textObject);
        }

        Debug.Log($"Feedback pool reset. Current size: {_allPooledObjects.Count}");
    }

    /// <summary>
    /// Отображает всплывающий текст над указанной целью.
    /// </summary>
    /// <param name="targetTransform">Transform цели, над которой появится текст.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="color">Цвет текста.</param>
    public void ShowFeedbackText(Transform targetTransform, string message, Color color)
    {
        if (floatingTextPrefab == null) return;
        
        Vector3 spawnPosition = targetTransform.position + Vector3.up * textSpawnHeightOffset;
        FloatingText textInstance = GetTextFromPool();
        
        textInstance.transform.position = spawnPosition;
        textInstance.gameObject.SetActive(true);
        textInstance.Show(message, color);
    }

    /// <summary>
    /// Создает и инициализирует полоску здоровья для указанного персонажа.
    /// </summary>
    public void CreateHealthBarFor(Character character)
    {
        if (healthBarPrefab == null) return;
        HealthBar newBar = Instantiate(healthBarPrefab);
        newBar.Initialize(character);
    }
}