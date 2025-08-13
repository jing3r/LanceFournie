using UnityEngine;
using TMPro;

/// <summary>
/// Управляет отображением и позиционированием тултипа.
/// Реализован как синглтон.
/// </summary>
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI statsText;

    private RectTransform _panelRect;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (tooltipPanel != null)
        {
            _panelRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            // Смещаем позицию, чтобы курсор не перекрывал текст.
            Vector2 position = Input.mousePosition + new Vector3(10, -10);
            _panelRect.position = position;
        }
    }

    /// <summary>
    /// Показывает тултип с указанным содержимым.
    /// </summary>
    /// <param name="header">Текст для заголовка.</param>
    /// <param name="stats">Текст для основного контента.</param>
    public void ShowTooltip(string header, string stats)
    {
        headerText.text = header;
        statsText.text = stats;
        tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// Скрывает тултип.
    /// </summary>
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}