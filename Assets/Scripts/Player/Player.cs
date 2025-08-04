using UnityEngine;
using System.Linq;

// Player class following Single Responsibility Principle
public class Player : MonoBehaviour, IPlayer
{
    [SerializeField] private PlayerColor color;
    [SerializeField] private bool isHumanPlayer = true;
    [SerializeField] private PlayerPiece[] pieces;
    int playerIndex;
    private IPlayerStrategy strategy;

    public PlayerColor Color => color;
    public IPlayerPiece[] Pieces => pieces.Cast<IPlayerPiece>().ToArray();
    public bool IsHumanPlayer => isHumanPlayer;
    public IPlayerStrategy Strategy
    {
        get => strategy ?? (strategy = new HumanPlayerStrategy());
        set => strategy = value;
    }
    public int PlayerIndex
    {
        get => playerIndex;
        set => playerIndex = value;
    } 
    private void Awake()
    {
        Debug.Log($"[Player] Initializing {color} player. IsHuman: {isHumanPlayer}");

        // Initialize pieces with correct color
        foreach (var piece in pieces)
        {
            piece.GetComponent<PlayerPiece>().SetColor(color);
           // Debug.Log($"[Player] Set piece color to {color}");
        }

        // Set AI strategy for non-human players
        if (!isHumanPlayer)
        {
            strategy = new AIPlayerStrategy();
            Debug.Log($"[Player] AI strategy assigned to {color} player");
        }
        else
        {
            Debug.Log($"[Player] Human strategy assigned to {color} player");
        }
    }

    public bool HasMovablePieces(int diceValue)
    {
        bool hasMovable = pieces.Any(piece => piece.CanMove(diceValue));
        Debug.Log($"[Player] {color} player has movable pieces with dice {diceValue}: {hasMovable}");
        return hasMovable;
    }

    public IPlayerPiece GetBestPieceToMove(int diceValue)
    {
        Debug.Log($"[Player] Getting best piece to move for {color} player with dice value {diceValue}");
        // Strategy pattern - delegate decision to strategy
        var bestPiece = Strategy.GetBestMove(this, diceValue);
        Debug.Log($"[Player] Best piece selected: {(bestPiece != null ? "Found" : "None")}");
        return bestPiece;
    }
}

// Strategy Pattern Implementation
public class HumanPlayerStrategy : IPlayerStrategy
{
    public void TakeTurn(IPlayer player, int diceValue)
    {
        Debug.Log($"[HumanStrategy] Human player {player.Color} taking turn with dice value {diceValue}");
        // For human players, just enable piece selection
        // The actual move happens when player clicks a piece
        GameManager.Instance.EnablePieceSelection(player, diceValue);
        Debug.Log($"[HumanStrategy] Piece selection enabled for {player.Color} player");
    }

    public IPlayerPiece GetBestMove(IPlayer player, int diceValue)
    {
        Debug.Log($"[HumanStrategy] GetBestMove called for human player {player.Color} - returning null (manual selection)");
        // For human players, return null - they choose manually
        return null;
    }
}

public class AIPlayerStrategy : IPlayerStrategy
{
    public void TakeTurn(IPlayer player, int diceValue)
    {
        Debug.Log($"[AIStrategy] AI player {player.Color} taking turn with dice value {diceValue}");
        var bestPiece = GetBestMove(player, diceValue);
        if (bestPiece != null)
        {
            Debug.Log($"[AIStrategy] Moving piece at position {bestPiece.CurrentPosition}");
            GameManager.Instance.MovePiece(bestPiece, diceValue);
        }
        else
        {
            Debug.Log($"[AIStrategy] No valid moves available - ending turn");
            GameManager.Instance.EndTurn();
        }
    }

    public IPlayerPiece GetBestMove(IPlayer player, int diceValue)
    {
        Debug.Log($"[AIStrategy] Calculating best move for {player.Color} with dice {diceValue}");
        var movablePieces = player.Pieces.Where(p => p.CanMove(diceValue)).ToArray();
        Debug.Log($"[AIStrategy] Found {movablePieces.Length} movable pieces");

        if (movablePieces.Length == 0)
        {
            Debug.Log($"[AIStrategy] No movable pieces found");
            return null;
        }

        // AI Strategy: Priority order
        // 1. Move piece out of start area (if dice is 6)
        // 2. Knock out opponent piece
        // 3. Move piece closest to home
        // 4. Move piece furthest from start

        // Try to move piece from start area
        if (diceValue == 6)
        {
            var pieceInStart = movablePieces.FirstOrDefault(p => p.IsInStartArea);
            if (pieceInStart != null)
            {
                Debug.Log($"[AIStrategy] Priority 1: Moving piece from start area");
                return pieceInStart;
            }
        }

        // Try to knock out opponent
        foreach (var piece in movablePieces)
        {
            if (WouldKnockOutOpponent(piece, diceValue))
            {
                Debug.Log($"[AIStrategy] Priority 2: Found piece that can knock out opponent at position {piece.CurrentPosition}");
                return piece;
            }
        }

        // Move piece closest to home
        var closestToHome = movablePieces.OrderByDescending(p => p.CurrentPosition).First();
        Debug.Log($"[AIStrategy] Priority 3: Moving piece closest to home at position {closestToHome.CurrentPosition}");
        return closestToHome;
    }

    private bool WouldKnockOutOpponent(IPlayerPiece piece, int steps)
    {
        Debug.Log($"[AIStrategy] Checking if piece at {piece.CurrentPosition} would knock out opponent with {steps} steps");
        // This would need board access to check if there's an opponent piece
        // at the target position
        return false; // Simplified for now
    }
}

// Factory Pattern for creating players
public static class PlayerFactory
{
    public static Player CreatePlayer(PlayerColor color, bool isHuman)
    {
        Debug.Log($"[PlayerFactory] Creating {(isHuman ? "Human" : "AI")} player with color {color}");
        var playerObj = new GameObject($"{color}Player");
        var player = playerObj.AddComponent<Player>();

        // Set player properties through reflection or public setters
        // This would need to be implemented based on your specific setup
        Debug.Log($"[PlayerFactory] Player {color} created successfully");

        return player;
    }
}