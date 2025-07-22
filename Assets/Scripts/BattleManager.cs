using UnityEngine;

/// <summary>
/// Перечисление, определяющее текущую фазу боя.
/// </summary>
public enum GamePhase { Placement, Battle, End }

/// <summary>
/// Центральный менеджер, управляющий состоянием и ходом всего сражения.
/// Реализован как синглтон для глобального доступа к текущей фазе боя.
/// </summary>
public class BattleManager : MonoBehaviour
{
    // Синглтон для легкого доступа из любой точки кода.
    public static BattleManager Instance;

    [Tooltip("Текущая фаза боя.")]
    public GamePhase currentPhase;

    private bool player1Ready = false;
    private bool player2Ready = false;
    private bool battleEnded = false;

    private void Awake()
    {
        // Стандартная реализация синглтона.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Бой всегда начинается с фазы расстановки.
        currentPhase = GamePhase.Placement;
    }

    private void Update()
    {
        // Проверка условия победы выполняется каждый кадр только в фазе боя.
        if (currentPhase == GamePhase.Battle && !battleEnded)
        {
            CheckForVictory();
        }
    }
    
    /// <summary>
    /// Регистрирует готовность игрока к началу боя.
    /// </summary>
    /// <param name="playerID">ID игрока (1 или 2), который нажал кнопку готовности.</param>
    public void PlayerReady(int playerID)
    {
        if (currentPhase != GamePhase.Placement) return;

        if (playerID == 1) player1Ready = true;
        if (playerID == 2) player2Ready = true;

        // Бой начинается, когда оба игрока подтвердили свою готовность.
        if (player1Ready && player2Ready)
        {
            StartBattle();
        }
    }

    /// <summary>
    /// Начинает фазу боя.
    /// </summary>
    private void StartBattle()
    {
        currentPhase = GamePhase.Battle;
        battleEnded = false;
        Debug.Log("Battle has started!");
    }

    /// <summary>
    /// Проверяет, остались ли на поле бойцы у обеих команд.
    /// </summary>
    private void CheckForVictory()
    {
        // TODO: Оптимизировать. Постоянный вызов FindObjectsOfType может быть медленным в больших боях.
        // Лучше иметь кэшированные списки команд, которые обновляются при смерти юнита.
        Character[] allCharacters = FindObjectsOfType<Character>();
        int team1Alive = 0;
        int team2Alive = 0;

        foreach (var character in allCharacters)
        {
            if (character.teamID == 1) team1Alive++;
            else team2Alive++;
        }

        if (team1Alive == 0 && team2Alive > 0)
        {
            EndBattle(2);
        }
        else if (team2Alive == 0 && team1Alive > 0)
        {
            EndBattle(1);
        }
        // Если у обеих команд 0 бойцов (например, умерли одновременно), можно считать ничьей.
        else if (team1Alive == 0 && team2Alive == 0)
        {
            EndBattle(0); // 0 - ID для ничьей
        }
    }

    /// <summary>
    /// Завершает бой и объявляет победителя.
    /// </summary>
    /// <param name="winningTeamID">ID победившей команды (или 0 для ничьей).</param>
    private void EndBattle(int winningTeamID)
    {
        battleEnded = true;
        currentPhase = GamePhase.End;
        if (winningTeamID > 0)
        {
            Debug.Log($"Battle has ended! Team {winningTeamID} is victorious!");
        }
        else
        {
            Debug.Log("Battle has ended in a draw!");
        }
    }
}