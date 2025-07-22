using UnityEngine;

/// <summary>
/// Управляет визуальным представлением персонажа, в частности - цветом.
/// </summary>
public class CharacterVisuals : MonoBehaviour
{
    // Ссылки на соседние компоненты
    private CharacterStats stats;
    private Character character;
    private Renderer objRenderer;

    [Header("Team Colors")]
    public Color team1SpearmanColor = new Color(0.0f, 0.0f, 0.6f);
    public Color team1InfantryColor = new Color(0.2f, 0.2f, 1.0f);
    public Color team1CavalryColor =  new Color(0.6f, 0.6f, 1.0f);
    public Color team2SpearmanColor = new Color(0.6f, 0.0f, 0.0f);
    public Color team2InfantryColor = new Color(1.0f, 0.2f, 0.2f);
    public Color team2CavalryColor =  new Color(1.0f, 0.6f, 0.6f);

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        character = GetComponent<Character>();
        objRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Обновляет цвет объекта в зависимости от команды и класса персонажа.
    /// Вызывается автоматически при изменении teamID в главном классе Character.
    /// </summary>
    public void UpdateVisuals()
    {
        if (stats == null || stats.blueprint == null || character == null || objRenderer == null)
        {
            return;
        }
        
        Color finalColor = Color.grey;
        if (character.teamID == 1)
        {
            switch (stats.blueprint.characterClass)
            {
                case "Spearman": finalColor = team1SpearmanColor; break;
                case "Infantry": finalColor = team1InfantryColor; break;
                case "Cavalry":  finalColor = team1CavalryColor;  break;
            }
        }
        else // teamID == 2
        {
            switch (stats.blueprint.characterClass)
            {
                case "Spearman": finalColor = team2SpearmanColor; break;
                case "Infantry": finalColor = team2InfantryColor; break;
                case "Cavalry":  finalColor = team2CavalryColor;  break;
            }
        }
        
        // Используется .material для создания уникального экземпляра материала для этого объекта,
        // чтобы изменение цвета не затронуло другие объекты с тем же исходным материалом.
        if(objRenderer.material != null)
        {
            objRenderer.material.color = finalColor;
        }
    }
}