using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Board class handling spatial logic and piece positioning
public class Board : MonoBehaviour, IBoard
{
    [Header("Path Points")]
    [SerializeField] private Transform[] commonPath;
    [SerializeField] private Transform[] blueHomePath;
    [SerializeField] private Transform[] redHomePath;
    [SerializeField] private Transform[] greenHomePath;
    [SerializeField] private Transform[] yellowHomePath;

    [Header("Start Positions")]
    [SerializeField] private Transform[] blueStartPositions;
    [SerializeField] private Transform[] redStartPositions;
    [SerializeField] private Transform[] greenStartPositions;
    [SerializeField] private Transform[] yellowStartPositions;

    [Header("Safe Zones")]
    private int[] safeZoneIndices = { 0, 8, 13, 21, 26, 34, 39, 47 };

    private Dictionary<Vector3, IPlayerPiece> occupiedPositions = new Dictionary<Vector3, IPlayerPiece>();

    // Color-specific starting positions on common path
    private readonly Dictionary<PlayerColor, int> startingIndices = new Dictionary<PlayerColor, int>
    {
        { PlayerColor.Blue, 0 },
        { PlayerColor.Red, 13 },
        { PlayerColor.Green, 26 },
        { PlayerColor.Yellow, 39 }
    };

    // Color-specific home path entry points
    private readonly Dictionary<PlayerColor, int> homePathEntries = new Dictionary<PlayerColor, int>
    {
        { PlayerColor.Blue, 51 },
        { PlayerColor.Red, 12 },
        { PlayerColor.Green, 25 },
        { PlayerColor.Yellow, 38 }
    };

    public Vector3 GetPositionAt(PlayerColor color, int pathIndex)
    {
        Debug.Log($"Getting position for {color} at pathIndex {pathIndex}");

        if (pathIndex == -1)
        {
            Vector3 startPos = GetStartAreaPosition(color);
            Debug.Log($"Returning start area position for {color}: {startPos}");
            return startPos;
        }

        if (pathIndex >= 51 && pathIndex <= 56)
        {
            int homeIndex = pathIndex - 51;
            Vector3 homePos = GetHomePathPosition(color, homeIndex);
            Debug.Log($"Returning home path position for {color} at homeIndex {homeIndex}: {homePos}");
            return homePos;
        }

        if (pathIndex == 57)
        {
            Vector3 homePos = GetHomePosition(color);
            Debug.Log($"Returning home position for {color}: {homePos}");
            return homePos;
        }

        int adjustedIndex = (pathIndex + startingIndices[color]) % 52;
        if (adjustedIndex < commonPath.Length)
        {
            Debug.Log($"Returning common path position for {color} at adjustedIndex {adjustedIndex}");
            Vector3 commonPos = commonPath[adjustedIndex].position;
            Debug.Log($"Returning common path position for {color} at adjustedIndex {adjustedIndex}: {commonPos}");
            return commonPos;
        }

        Debug.LogWarning($"Invalid position requested for {color} at pathIndex {pathIndex}, returning Vector3.zero");
        return Vector3.zero;
    }

    public bool IsPositionSafe(PlayerColor color, int pathIndex)
    {
        if (pathIndex == -1 || pathIndex >= 51)
        {
            Debug.Log($"Position for {color} at pathIndex {pathIndex} is safe (start area or home path)");
            return true;
        }

        int adjustedIndex = (pathIndex + startingIndices[color]) % 52;
        bool isSafe = safeZoneIndices.Contains(adjustedIndex);
        Debug.Log($"Checking if position for {color} at pathIndex {pathIndex} (adjusted: {adjustedIndex}) is safe: {isSafe}");
        return isSafe;
    }

    public IPlayerPiece GetPieceAt(PlayerColor color, int pathIndex)
    {
        Vector3 position = GetPositionAt(color, pathIndex);
        bool hasPiece = occupiedPositions.ContainsKey(position);
        Debug.Log($"Checking piece at {color} pathIndex {pathIndex} (position: {position}): {(hasPiece ? occupiedPositions[position] : "None")}");
        return hasPiece ? occupiedPositions[position] : null;
    }

    public void PlacePiece(IPlayerPiece piece, int pathIndex)
    {
        Vector3 position = GetPositionAt(piece.Color, pathIndex);
        Debug.Log($"Placing piece {piece} of color {piece.Color} at pathIndex {pathIndex} (position: {position})");

        RemovePiece(piece);
        occupiedPositions[position] = piece;
        piece.SetPosition(position);
        Debug.Log($"Piece {piece} placed at position {position}. Occupied positions count: {occupiedPositions.Count}");
    }

    public void RemovePiece(IPlayerPiece piece)
    {
        var positionToRemove = occupiedPositions.FirstOrDefault(kvp => kvp.Value == piece).Key;
        if (positionToRemove != Vector3.zero)
        {
            occupiedPositions.Remove(positionToRemove);
            Debug.Log($"Removed piece {piece} from position {positionToRemove}. Occupied positions count: {occupiedPositions.Count}");
        }
        else
        {
            Debug.Log($"No position found to remove for piece {piece}");
        }
    }

    private Vector3 GetStartAreaPosition(PlayerColor color)
    {
        // Return the first available start position for the color
        Transform[] startPositions = color switch
        {
            PlayerColor.Blue => blueStartPositions,
            PlayerColor.Red => redStartPositions,
            PlayerColor.Green => greenStartPositions,
            PlayerColor.Yellow => yellowStartPositions,
            _ => blueStartPositions
        };

        return startPositions[0].position; // Simplified - should handle multiple pieces
    }

    private Vector3 GetHomePathPosition(PlayerColor color, int homeIndex)
    {
        Transform[] homePath = color switch
        {
            PlayerColor.Blue => blueHomePath,
            PlayerColor.Red => redHomePath,
            PlayerColor.Green => greenHomePath,
            PlayerColor.Yellow => yellowHomePath,
            _ => blueHomePath
        };

        return homeIndex < homePath.Length ? homePath[homeIndex].position : Vector3.zero;
    }

    private Vector3 GetHomePosition(PlayerColor color)
    {
        // Return the final home position for each color
        return color switch
        {
            PlayerColor.Blue => blueHomePath[^1].position,
            PlayerColor.Red => redHomePath[^1].position,
            PlayerColor.Green => greenHomePath[^1].position,
            PlayerColor.Yellow => yellowHomePath[^1].position,
            _ => Vector3.zero
        };
    }
}