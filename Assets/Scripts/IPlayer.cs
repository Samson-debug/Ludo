// Core enums and interfaces
using UnityEngine;
using System;

public enum PlayerColor
{
    Blue,
    Red,
    Green,
    Yellow
}

public enum GameState
{
    WaitingForDiceRoll,
    WaitingForPlayerMove,
    PlayerMoving,
    GameOver
}

public enum MoveResult
{
    Success,
    InvalidMove,
    PieceKnockedOut,
    GameWon
}

// Observer pattern for game events
public interface IGameObserver
{
    void OnPlayerTurnChanged(PlayerColor currentPlayer);
    void OnPieceMoved(IPlayerPiece piece, int steps);
    void OnPieceKnockedOut(IPlayerPiece knockedPiece, IPlayerPiece attackingPiece);
    void OnGameWon(PlayerColor winner);
    void OnDiceRolled(int value);
}

// Strategy pattern for different AI behaviors
public interface IPlayerStrategy
{
    void TakeTurn(IPlayer player, int diceValue);
    IPlayerPiece GetBestMove(IPlayer player, int diceValue);
}

// Interface segregation principle - separate concerns
public interface IMovable
{
    bool CanMove(int steps);
    MoveResult Move(int steps);
}

public interface IPlayerPiece : IMovable
{
    PlayerColor Color { get; }
    int CurrentPosition { get; }
    bool IsInStartArea { get; }
    bool IsInSafeZone { get; }
    bool HasReachedHome { get; }
    Transform Transform { get; }

    void SetPosition(Vector3 position);
    void ReturnToStart();
}

public interface IPlayer
{
    PlayerColor Color { get; }
    IPlayerPiece[] Pieces { get; }
    bool IsHumanPlayer { get; }
    IPlayerStrategy Strategy { get; set; }

    bool HasMovablePieces(int diceValue);
    IPlayerPiece GetBestPieceToMove(int diceValue);
}

public interface IDice
{
    event Action<int, PlayerColor> OnDiceRolled;
    void Roll();
    int LastRolledValue { get; }
    bool CanRoll { get; }
}

public interface IBoard
{
    Vector3 GetPositionAt(PlayerColor color, int pathIndex);
    bool IsPositionSafe(PlayerColor color, int pathIndex);
    IPlayerPiece GetPieceAt(PlayerColor color, int pathIndex);
    void PlacePiece(IPlayerPiece piece, int pathIndex);
    void RemovePiece(IPlayerPiece piece);
}

public interface IGameManager
{
    GameState CurrentState { get; }
    PlayerColor CurrentPlayer { get; }
    void StartGame();
    void EndTurn();
    void RegisterObserver(IGameObserver observer);
    void UnregisterObserver(IGameObserver observer);
}