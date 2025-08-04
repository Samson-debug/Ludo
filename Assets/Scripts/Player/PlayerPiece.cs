using UnityEngine;
using System.Collections;

// Single Responsibility: Handles only piece movement and state
public class PlayerPiece : MonoBehaviour, IPlayerPiece
{
    [SerializeField] private PlayerColor color;
    [SerializeField] private int currentPosition = -1; // -1 means in start area
    [SerializeField] private bool isInStartArea = true;

    private IBoard board;
    private const int HOME_POSITION = 56;
    private const int SAFE_ZONE_START = 50;

    public PlayerColor Color => color;
    public int CurrentPosition => currentPosition;
    public bool IsInStartArea => isInStartArea;
    public bool IsInSafeZone => currentPosition ==0&& currentPosition >= SAFE_ZONE_START && currentPosition < HOME_POSITION;
    public bool HasReachedHome => currentPosition >= HOME_POSITION;
    public Transform Transform => transform;

    private void Awake()
    {
       // Debug.Log($"[PlayerPiece] Initializing {color} piece");
        board = FindObjectOfType<Board>();
        if (board != null)
        {
           // Debug.Log($"[PlayerPiece] Board reference found for {color} piece");
        }
        else
        {
            Debug.LogError($"[PlayerPiece] Board reference NOT found for {color} piece!");
        }
    }

    public bool CanMove(int steps)
    {
        Debug.Log($"[PlayerPiece] Checking if {color} piece at position {currentPosition} can move {steps} steps");

        if (steps <= 0)
        {
            Debug.Log($"[PlayerPiece] Cannot move - invalid steps: {steps}");
            return false;
        }

        // If in start area, can only move with a 6
        if (isInStartArea)
        {
            bool canMove = steps == 6;
            Debug.Log($"[PlayerPiece] Piece in start area - can move with 6: {canMove}");
            return canMove;
        }

        // Check if move would exceed home position
        int newPosition = currentPosition + steps;
        bool validMove = newPosition <= HOME_POSITION;
        Debug.Log($"[PlayerPiece] New position would be {newPosition}, valid: {validMove}");
        return validMove;
    }

    public MoveResult Move(int steps)
    {
        Debug.Log($"[PlayerPiece] Starting move for {color} piece with {steps} steps");

        if (!CanMove(steps))
        {
            Debug.LogWarning($"[PlayerPiece] Invalid move attempted - {steps} steps from position {currentPosition}");
            return MoveResult.InvalidMove;
        }

        // Handle moving from start area
        if (isInStartArea)
        {
            Debug.Log($"[PlayerPiece] Moving {color} piece from start area to position 0");
            currentPosition = 0;
            isInStartArea = false;
        }
        else
        {
            int oldPosition = currentPosition;
            currentPosition += steps;
            Debug.Log($"[PlayerPiece] Moving {color} piece from position {oldPosition} to {currentPosition}");
        }

        // Update visual position
        Vector3 newWorldPosition = board.GetPositionAt(color, currentPosition);
        Debug.Log($"[PlayerPiece] Moving to world position: {newWorldPosition}");
        StartCoroutine(SmoothMove(newWorldPosition));

        // Check for knockouts
        if (!IsInSafeZone && !HasReachedHome && !board.IsPositionSafe(color, currentPosition))
        {
            Debug.Log($"[PlayerPiece] Checking for knockouts at position {currentPosition}");
            IPlayerPiece pieceAtPosition = board.GetPieceAt(color, currentPosition);
            if (pieceAtPosition != null && pieceAtPosition.Color != color)
            {
                Debug.Log($"[PlayerPiece] KNOCKOUT! {color} piece knocked out {pieceAtPosition.Color} piece");
                pieceAtPosition.ReturnToStart();
                return MoveResult.PieceKnockedOut;
            }
        }
        else
        {
            Debug.Log($"[PlayerPiece] Piece in safe zone or at home - no knockout check needed");
        }

        // Update board state
        board.PlacePiece(this, currentPosition);
        Debug.Log($"[PlayerPiece] Piece placed on board at position {currentPosition}");

        // Check for win condition
        if (HasReachedHome)
        {
            Debug.Log($"[PlayerPiece] {color} piece reached home!");
            // Check if all pieces of this color have reached home
            var player = GameManager.Instance.GetPlayer(color);
            bool allPiecesHome = true;
            int piecesAtHome = 0;
            foreach (var piece in player.Pieces)
            {
                if (piece.HasReachedHome)
                {
                    piecesAtHome++;
                }
                else
                {
                    allPiecesHome = false;
                }
            }

            Debug.Log($"[PlayerPiece] {piecesAtHome}/{player.Pieces.Length} pieces at home for {color} player");

            if (allPiecesHome)
            {
                Debug.Log($"[PlayerPiece] GAME WON! All {color} pieces have reached home!");
                return MoveResult.GameWon;
            }
        }

        Debug.Log($"[PlayerPiece] Move completed successfully");
        return MoveResult.Success;
    }

    public void ReturnToStart()
    {
        Debug.Log($"[PlayerPiece] Returning {color} piece to start from position {currentPosition}");

        board.RemovePiece(this);
        currentPosition = -1;
        isInStartArea = true;

        // Return to start position visually
        Vector3 startPosition = board.GetPositionAt(color, -1); // Start area position
        Debug.Log($"[PlayerPiece] Moving to start position: {startPosition}");
        StartCoroutine(SmoothMove(startPosition));
    }

    public void SetPosition(Vector3 position)
    {
        Debug.Log($"[PlayerPiece] Setting {color} piece position to {position}");
        transform.position = position;
    }

    private IEnumerator SmoothMove(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float moveTime = 0.3f;
        float elapsedTime = 0f;

        Debug.Log($"[PlayerPiece] Starting smooth move from {startPosition} to {targetPosition}");

        while (elapsedTime < moveTime)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        Debug.Log($"[PlayerPiece] Smooth move completed to {targetPosition}");
    }

    private void OnMouseDown()
    {
        Debug.Log($"[PlayerPiece] {color} piece at position {currentPosition} clicked");
        // Delegate to game manager for turn validation
        GameManager.Instance.OnPieceClicked(this);
    }

    public void SetColor(PlayerColor newColor)
    {
        Debug.Log($"[PlayerPiece] Setting piece color from {color} to {newColor}");
        color = newColor;
    }
}