using UnityEngine;
using System.Text;

/// <summary>
/// Триггер, отвечающий за отображение тултипа при наведении на объект.
/// Должен находиться на одном GameObject с компонентом Character.
/// </summary>
[RequireComponent(typeof(Character))]
public class TooltipTrigger : MonoBehaviour
{
    private Character _character;
    private CharacterStats _stats;

    private void Awake()
    {
        _character = GetComponent<Character>();
        _stats = GetComponent<CharacterStats>();
    }

    private void OnMouseEnter()
    {
        if (TooltipManager.Instance == null) return;
        
        string header = $"{_stats.blueprint.characterName} ({_stats.blueprint.characterClass})";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Health: {_stats.currentHealth:F0} / {_stats.maxHealth:F0}");
        sb.AppendLine($"Damage: {_stats.damage:F0}");
        sb.AppendLine($"Hit Chance: {_stats.hitChance:F0}%");
        sb.AppendLine($"Dodge: {_stats.dodgeChance:F0}%");
        sb.AppendLine($"Attack Range: {_stats.attackRange}");

        TooltipManager.Instance.ShowTooltip(header, sb.ToString());
    }

    private void OnMouseExit()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}