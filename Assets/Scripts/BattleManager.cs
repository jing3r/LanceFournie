using UnityEngine;
using System.Collections.Generic;
using System;

public enum GamePhase { Draft, Placement, Battle, End }

/// <summary>
/// Центральный менеджер, управляющий состоянием и ходом сражения.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    public static event Action OnBattleStarted;
    public static event Action OnPlacementPhaseStarted;
    public GamePhase currentPhase { get; private set; }
    private bool _player1LineupConfirmed = false;
    private bool _player2LineupConfirmed = false;
    private bool _player1Ready = false;
    private bool _player2Ready = false;
    private bool _battleEnded = false;

    private List<Character> _team1Fighters = new List<Character>();
    private List<Character> _team2Fighters = new List<Character>();
    public bool IsLineupConfirmed(int playerID)
    {
        if (playerID == 1) return _player1LineupConfirmed;
        if (playerID == 2) return _player2LineupConfirmed;
        return false;
    }
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
            currentPhase = GamePhase.Draft;
    }

    private void Update()
    {
        if (currentPhase == GamePhase.Battle && !_battleEnded)
        {
            CheckForVictory();
        }
    }
    /// <summary>
    /// Игрок подтверждает свой состав на "скамейке".
    /// </summary>
    public void ConfirmLineup(int playerID)
    {
        if (currentPhase != GamePhase.Draft) return;

        if (playerID == 1) _player1LineupConfirmed = true;
        if (playerID == 2) _player2LineupConfirmed = true;

        if (_player1LineupConfirmed && _player2LineupConfirmed)
        {
            currentPhase = GamePhase.Placement;
            OnPlacementPhaseStarted?.Invoke();
            Debug.Log("Placement phase has started. Rosters are now locked.");
        }
    }

    /// <summary>
    /// Обрабатывает сигнал готовности от одного из игроков.
    /// </summary>
    public void PlayerReady(int playerID)
    {
        if (currentPhase != GamePhase.Placement) return;

        if (playerID == 1) _player1Ready = true;
        if (playerID == 2) _player2Ready = true;
        
        if (_player1Ready && _player2Ready)
        {
            StartBattle();
        }
    }

    /// <summary>
    /// Регистрирует персонажа как участника боя.
    /// </summary>
    public void RegisterFighter(Character fighter)
    {
        var list = (fighter.teamID == 1) ? _team1Fighters : _team2Fighters;
        if (!list.Contains(fighter))
        {
            list.Add(fighter);
        }
    }

    /// <summary>
    /// Удаляет персонажа из списка участников боя.
    /// </summary>
    public void UnregisterFighter(Character fighter)
    {
        if (fighter.teamID == 1) _team1Fighters.Remove(fighter);
        else _team2Fighters.Remove(fighter);
    }

    /// <summary>
    /// Возвращает список бойцов команды-противника.
    /// </summary>
    public List<Character> GetOpposingFighters(int myTeamID)
    {
        return (myTeamID == 1) ? _team2Fighters : _team1Fighters;
    }
    
    private void StartBattle()
    {
        if (currentPhase == GamePhase.Battle) return;

        currentPhase = GamePhase.Battle;
        _battleEnded = false;
        Debug.Log($"Battle has started! Team 1: {_team1Fighters.Count} fighters. Team 2: {_team2Fighters.Count} fighters.");

        OnBattleStarted?.Invoke();
    }

    // Проверяет, не осталась ли на поле только одна команда.
    private void CheckForVictory()
    {
        // Эта проверка защищает от ошибок, если юнит был уничтожен,
        // но не успел отписаться от всех систем.
        _team1Fighters.RemoveAll(item => item == null);
        _team2Fighters.RemoveAll(item => item == null);

        bool team1Alive = _team1Fighters.Count > 0;
        bool team2Alive = _team2Fighters.Count > 0;

        if (!team1Alive && team2Alive) EndBattle(2);
        else if (team1Alive && !team2Alive) EndBattle(1);
        else if (!team1Alive && !team2Alive) EndBattle(0); // Ничья
    }

    private void EndBattle(int winningTeamID)
    {
        _battleEnded = true;
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