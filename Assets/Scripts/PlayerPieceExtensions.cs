using UnityEngine;
using System.Linq;

// Extension methods for better code readability and reusability
public static class PlayerPieceExtensions
{
    public static void SetColor(this PlayerPiece piece, PlayerColor color)
    {
        // Use reflection or a public setter to set the color
        var field = typeof(PlayerPiece).GetField("color",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(piece, color);
    }
}

public static class GameManagerExtensions
{
    public static void ChangeState(this GameManager gameManager, GameState newState)
    {
        // This would need to be implemented as a public method in GameManager
        // or use reflection to access private methods
    }

    public static void DisableAllHighlights(this GameManager gameManager)
    {
        // Implementation for disabling highlights
    }

    public static void SetHintsEnabled(this GameManager gameManager, bool enabled)
    {
        // Implementation for enabling/disabling hints
    }
}

// Utility class for game calculations
public static class LudoGameUtils
{
    private const int BOARD_SIZE = 52;
    private const int HOME_PATH_LENGTH = 6;
    private const int HOME_POSITION = 57;

    public static int CalculateGlobalPosition(PlayerColor color, int localPosition)
    {
        int startOffset = GetStartOffset(color);

        if (localPosition < 0) return -1; // Start area
        if (localPosition >= 51) return localPosition; // Home path

        return (localPosition + startOffset) % BOARD_SIZE;
    }

    public static int GetStartOffset(PlayerColor color)
    {
        return color switch
        {
            PlayerColor.Blue => 1,
            PlayerColor.Red => 14,
            PlayerColor.Green => 27,
            PlayerColor.Yellow => 40,
            _ => 0
        };
    }

    public static bool IsValidMove(int currentPosition, int steps, int maxPosition = HOME_POSITION)
    {
        if (steps <= 0) return false;

        int newPosition = currentPosition + steps;
        return newPosition <= maxPosition;
    }

    public static bool IsSafePosition(int globalPosition)
    {
        int[] safePositions = { 1, 9, 14, 22, 27, 35, 40, 48 };
        return safePositions.Contains(globalPosition);
    }

    public static Vector3 CalculateMultiPieceOffset(int pieceIndex, int totalPieces, float spacing = 0.2f)
    {
        if (totalPieces <= 1) return Vector3.zero;

        bool isEven = totalPieces % 2 == 0;
        int halfCount = totalPieces / 2;

        float offset;
        if (isEven)
        {
            offset = (pieceIndex - halfCount + 0.5f) * spacing;
        }
        else
        {
            offset = (pieceIndex - halfCount) * spacing;
        }

        return new Vector3(offset, 0, 0);
    }
}

// Validation utility for game rules
public static class GameRuleValidator
{
    public static bool CanPieceEnterBoard(IPlayerPiece piece, int diceValue)
    {
        return piece.IsInStartArea && diceValue == 6;
    }

    public static bool CanPieceMove(IPlayerPiece piece, int diceValue)
    {
        if (piece.HasReachedHome) return false;
        if (piece.IsInStartArea) return diceValue == 6;

        return LudoGameUtils.IsValidMove(piece.CurrentPosition, diceValue);
    }

    public static bool WillKnockoutOpponent(IPlayerPiece piece, int diceValue, IBoard board)
    {
        if (piece.IsInStartArea && diceValue != 6) return false;

        int targetPosition = piece.IsInStartArea ? 0 : piece.CurrentPosition + diceValue;

        // Can't knockout in safe zones or home path
        if (LudoGameUtils.IsSafePosition(targetPosition) || targetPosition >= 51)
            return false;

        var pieceAtTarget = board.GetPieceAt(piece.Color, targetPosition);
        return pieceAtTarget != null && pieceAtTarget.Color != piece.Color;
    }

    public static bool IsGameWon(IPlayer player)
    {
        return player.Pieces.All(piece => piece.HasReachedHome);
    }
}

// Animation utility class
public static class AnimationUtils
{
    public static void AnimatePieceMove(Transform piece, Vector3 targetPosition, float duration = 0.3f)
    {
        piece.GetComponent<MonoBehaviour>().StartCoroutine(SmoothMoveCoroutine(piece, targetPosition, duration));
    }

    private static System.Collections.IEnumerator SmoothMoveCoroutine(Transform piece, Vector3 target, float duration)
    {
        Vector3 start = piece.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            piece.position = Vector3.Lerp(start, target, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        piece.position = target;
    }

    public static void AnimatePieceHighlight(Transform piece, Color highlightColor, float duration = 0.5f)
    {
        var renderer = piece.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            piece.GetComponent<MonoBehaviour>().StartCoroutine(HighlightCoroutine(renderer, highlightColor, duration));
        }
    }

    private static System.Collections.IEnumerator HighlightCoroutine(SpriteRenderer renderer, Color highlightColor, float duration)
    {
        Color originalColor = renderer.color;
        float elapsedTime = 0f;

        // Fade to highlight color
        while (elapsedTime < duration / 2)
        {
            renderer.color = Color.Lerp(originalColor, highlightColor, elapsedTime / (duration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;

        // Fade back to original color
        while (elapsedTime < duration / 2)
        {
            renderer.color = Color.Lerp(highlightColor, originalColor, elapsedTime / (duration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        renderer.color = originalColor;
    }
}

// Event system for loose coupling
public class GameEventSystem
{
    private static GameEventSystem instance;
    public static GameEventSystem Instance => instance ??= new GameEventSystem();

    public System.Action<PlayerColor> OnPlayerTurnChanged;
    public System.Action<int> OnDiceRolled;
    public System.Action<IPlayerPiece, int> OnPieceMoved;
    public System.Action<IPlayerPiece, IPlayerPiece> OnPieceKnockedOut;
    public System.Action<PlayerColor> OnGameWon;

    public void TriggerPlayerTurnChanged(PlayerColor player) => OnPlayerTurnChanged?.Invoke(player);
    public void TriggerDiceRolled(int value) => OnDiceRolled?.Invoke(value);
    public void TriggerPieceMoved(IPlayerPiece piece, int steps) => OnPieceMoved?.Invoke(piece, steps);
    public void TriggerPieceKnockedOut(IPlayerPiece knocked, IPlayerPiece attacker) => OnPieceKnockedOut?.Invoke(knocked, attacker);
    public void TriggerGameWon(PlayerColor winner) => OnGameWon?.Invoke(winner);
}

// Configuration class for game settings
[System.Serializable]
public class GameConfiguration
{
    [Header("Game Rules")]
    public int requiredSixToStart = 6;
    public int maxPositionsOnBoard = 57;
    public bool allowMultiplePiecesOnSameSpot = true;
    public bool autoMoveOnSingleOption = true;

    [Header("Animation Settings")]
    public float pieceMoveDuration = 0.3f;
    public float diceRollDuration = 0.5f;
    public float highlightDuration = 0.5f;

    [Header("Visual Settings")]
    public Color[] playerColors = { Color.blue, Color.red, Color.green, Color.yellow };
    public Color highlightColor = Color.yellow;
    public float pieceScale = 1.0f;

    [Header("Audio Settings")]
    public bool enableSFX = true;
    public bool enableMusic = true;
    public float defaultVolume = 0.7f;
}

// Performance optimization utilities
public static class PerformanceUtils
{
    private static readonly System.Collections.Generic.Dictionary<string, object> objectPool =
        new System.Collections.Generic.Dictionary<string, object>();

    public static T GetPooledObject<T>(string key) where T : class
    {
        if (objectPool.ContainsKey(key))
        {
            return objectPool[key] as T;
        }
        return null;
    }

    public static void ReturnToPool<T>(string key, T obj) where T : class
    {
        objectPool[key] = obj;
    }

    public static void ClearPool()
    {
        objectPool.Clear();
    }
}