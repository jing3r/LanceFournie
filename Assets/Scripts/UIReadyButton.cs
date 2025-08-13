using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент-посредник для UI кнопок "Готов".
/// Сообщает BattleManager о нажатии и передает ID игрока.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIReadyButton : MonoBehaviour
{
    [Tooltip("ID игрока, к которому привязана эта кнопка (1, 2, 3 или 4).")]
    public int playerID;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.PlayerReady(playerID);
            _button.interactable = false; 
        }
        else
        {
            Debug.LogError("BattleManager.Instance is not found in the scene.");
        }
    }
}