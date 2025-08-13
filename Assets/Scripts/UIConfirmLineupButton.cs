using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIConfirmLineupButton : MonoBehaviour
{
    [Tooltip("ID игрока, к которому привязана эта кнопка.")]
    public int playerID;
    
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnConfirm);
    }

    private void OnConfirm()
    {
        BattleManager.Instance.ConfirmLineup(playerID);
        _button.interactable = false;
    }
}