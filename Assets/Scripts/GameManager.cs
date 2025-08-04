using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


// Singleton Game Manager with State Pattern and Observer Pattern
public class GameManager : MonoBehaviour, IGameManager
{
    public static GameManager Instance { get; private set; }

    [Header("Game Setup")]
    [SerializeField] private int numberOfPlayers = 4;
    [SerializeField] private Player[] players;
    [SerializeField] private Dice[] dices;

    private GameState currentState = GameState.WaitingForDiceRoll;
    private int currentPlayerIndex = 0;
    private int lastDiceValue = 0;
    private List<IGameObserver> observers = new List<IGameObserver>();
    private IBoard board;

    // State Pattern
    private Dictionary<GameState, IGameState> gameStates;
    private IGameState currentGameState;

    public GameState CurrentState => currentState;
    public PlayerColor CurrentPlayer => players[currentPlayerIndex].Color;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameStates();
            SetIndex();
        }
        else
        {
            Debug.Log("GameManager: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        board = FindObjectOfType<Board>();
        SetupGame();
    }
    void SetIndex()
    {
       for(int i =0; i< players.Length; i++)
       {
            players[i].PlayerIndex = i;
       }
    }
    private void InitializeGameStates()
    {
        gameStates = new Dictionary<GameState, IGameState>
        {
            { GameState.WaitingForDiceRoll, new WaitingForDiceRollState(this) },
            { GameState.WaitingForPlayerMove, new WaitingForPlayerMoveState(this) },
            { GameState.PlayerMoving, new PlayerMovingState(this) },
            { GameState.GameOver, new GameOverState(this) }
        };

        currentGameState = gameStates[currentState];
        Debug.Log($"GameManager: Game states initialized. Current state: {currentState}");
    }

    private void SetupGame()
    {
       // Debug.Log("GameManager: Setting up game");
        if (!ValidateGameSetup()) return;

        // Subscribe to dice events
        foreach (var dice in dices)
        {
            if (dice != null)
            {
                dice.OnDiceRolled += OnDiceRolled;
                Debug.Log($"GameManager: Subscribed to dice events for dice: {dice.name}");
            }
        }
      //  Debug.Log("GameManager: Game setup completed");
    }

    public void StartGame()
    {
       // Debug.Log("GameManager: Starting game");
        ChangeState(GameState.WaitingForDiceRoll);
        NotifyObservers(obs => obs.OnPlayerTurnChanged(CurrentPlayer));
        Debug.Log($"GameManager: Game started. Current player: {CurrentPlayer}");
    }

    public void OnDiceRolled(int value, PlayerColor color)
    {
        Debug.Log($"GameManager: Dice rolled with value: {value}");      
        currentPlayerIndex = GetPlayer(color).PlayerIndex;
        lastDiceValue = value;
        NotifyObservers(obs => obs.OnDiceRolled(value));
        currentGameState.OnDiceRolled(value);
    }

    public void OnPieceClicked(IPlayerPiece piece)
    {
        Debug.Log($"GameManager: Piece clicked - Color: {piece.Color}, Position: {piece.CurrentPosition}");
        currentGameState.OnPieceClicked(piece);
    }

    public void MovePiece(IPlayerPiece piece, int steps)
    {
        Debug.Log($"GameManager: MovePiece called - Piece: {piece.Color}, Steps: {steps}, Current Position: {piece.CurrentPosition}");

        if (piece.Color != CurrentPlayer)
        {
            Debug.LogWarning($"Attempted to move piece of wrong color. Expected: {CurrentPlayer}, Got: {piece.Color}");
            return;
        }

        if (!piece.CanMove(steps))
        {
            Debug.LogWarning($"Invalid move attempted. Piece at position {piece.CurrentPosition} cannot move {steps} steps.");
            return;
        }

        ChangeState(GameState.PlayerMoving);

        var result = piece.Move(steps);
        Debug.Log($"GameManager: Move result: {result}");

        NotifyObservers(obs => obs.OnPieceMoved(piece, steps));

        switch (result)
        {
            case MoveResult.PieceKnockedOut:
                Debug.Log("GameManager: Piece knocked out opponent");
                // The knocked out piece is handled in the PlayerPiece.Move method
                NotifyObservers(obs => obs.OnPieceKnockedOut(null, piece)); // Could be improved with better knockout tracking
                break;
            case MoveResult.GameWon:
                Debug.Log($"GameManager: Game won by {piece.Color}!");
                NotifyObservers(obs => obs.OnGameWon(piece.Color));
                ChangeState(GameState.GameOver);
                return;
            case MoveResult.InvalidMove:
                Debug.LogError("Move was validated but still returned InvalidMove result!");
                ChangeState(GameState.WaitingForDiceRoll);
                return;
        }

        // Determine next turn based on game rules
        bool shouldContinueTurn = ShouldContinueTurn(steps, result);
        Debug.Log($"GameManager: Should continue turn: {shouldContinueTurn} (Dice: {steps}, Result: {result})");

        if (shouldContinueTurn)
        {
            ChangeState(GameState.WaitingForDiceRoll);
        }
        else
        {
            EndTurn();
        }
    }

    private bool ShouldContinueTurn(int diceValue, MoveResult result)
    {
        // Continue turn if rolled a 6 or knocked out an opponent
        bool shouldContinue = diceValue == 6 || result == MoveResult.PieceKnockedOut;
        Debug.Log($"GameManager: ShouldContinueTurn - Dice: {diceValue}, Result: {result}, Continue: {shouldContinue}");
        return shouldContinue;
    }

    public void EndTurn()
    {
        Debug.Log($"GameManager: Ending turn for player {CurrentPlayer} (index: {currentPlayerIndex})");

        // Move to next active player
        int startIndex = currentPlayerIndex;
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % numberOfPlayers;
            Debug.Log($"GameManager: Checking next player index: {currentPlayerIndex}");

            // Safety check to prevent infinite loop
            if (currentPlayerIndex == startIndex)
            {
                Debug.LogError("No active players found!");
                break;
            }
        }
        while (currentPlayerIndex >= players.Length ||
               players[currentPlayerIndex] == null ||
               !players[currentPlayerIndex].gameObject.activeSelf);

        Debug.Log($"GameManager: Turn ended. New current player: {CurrentPlayer} (index: {currentPlayerIndex})");
        ChangeState(GameState.WaitingForDiceRoll);
        NotifyObservers(obs => obs.OnPlayerTurnChanged(CurrentPlayer));
    }

    public void EnablePieceSelection(IPlayer player, int diceValue)
    {
        Debug.Log($"GameManager: Enabling piece selection for player {player.Color} with dice value {diceValue}");

        // Enable visual feedback for movable pieces
        int movablePieces = 0;
        foreach (var piece in player.Pieces)
        {
            if (piece.CanMove(diceValue))
            {
                // Add visual indicator (glow, highlight, etc.)
                EnablePieceHighlight(piece);
                movablePieces++;
            }
        }
        Debug.Log($"GameManager: {movablePieces} pieces can be moved");
    }

    private void EnablePieceHighlight(IPlayerPiece piece)
    {
        Debug.Log($"GameManager: Highlighting piece at position {piece.CurrentPosition}");
        // Implementation for visual feedback
        var renderer = piece.Transform.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.yellow; // Highlight color
        }
    }

    public void DisableAllHighlights()
    {
        Debug.Log("GameManager: Disabling all highlights");
        foreach (var player in players)
        {
            if (player != null && player.gameObject.activeSelf)
            {
                foreach (var piece in player.Pieces)
                {
                    var renderer = piece.Transform.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = Color.white; // Normal color
                    }
                }
            }
        }
    }

    public void SetHintsEnabled(bool enabled)
    {
        Debug.Log($"GameManager: Setting hints enabled: {enabled}");
        // Implementation for enabling/disabling hints
        PlayerPrefs.SetInt("ShowHints", enabled ? 1 : 0);
    }

    public void ChangeState(GameState newState)
    {
        Debug.Log($"GameManager: State change from {currentState} to {newState}");
        currentState = newState;
        currentGameState?.OnExit();
        currentPlayerIndex = GetCurrentPlayer().PlayerIndex;
        currentGameState = gameStates[newState];
        currentGameState?.OnEnter();
        SetDiceActive();
    }

    public void RegisterObserver(IGameObserver observer)
    {
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
            Debug.Log($"GameManager: Observer registered. Total observers: {observers.Count}");
        }
    }

    public void UnregisterObserver(IGameObserver observer)
    {
        bool removed = observers.Remove(observer);
        Debug.Log($"GameManager: Observer unregistered: {removed}. Total observers: {observers.Count}");
    }

    private void NotifyObservers(Action<IGameObserver> action)
    {
        Debug.Log($"GameManager: Notifying {observers.Count} observers");
        foreach (var observer in observers.ToList())
        {
            action(observer);
        }
    }

    public Player GetPlayer(PlayerColor color)
    {
        var player = players.FirstOrDefault(p => p.Color == color);
        Debug.Log($"GameManager: GetPlayer({color}) - Found: {player != null}");
        return player;
    }
   
    // Public methods for state classes
    public Player GetCurrentPlayer()
    {
        var player = players[currentPlayerIndex];
        Debug.Log($"GameManager: GetCurrentPlayer() - Player: {player.Color} (index: {currentPlayerIndex})");
        return player;
    }

    public int GetLastDiceValue()
    {
        Debug.Log($"GameManager: GetLastDiceValue() - Value: {lastDiceValue}");
        return lastDiceValue;
    }

    public void SetDiceActive()
    {
        Debug.Log($"GameManager: SetDiceActive() for player index {currentPlayerIndex}");
        if (currentPlayerIndex < dices.Length && dices[currentPlayerIndex] != null)
        {
            foreach(var player in players)
            {
                if(player.PlayerIndex ==  currentPlayerIndex)
                    dices[currentPlayerIndex].gameObject.SetActive(true);
                else
                    dices[player.PlayerIndex].gameObject.SetActive(false);
            }
           
        }
    }

    // Additional helper methods for better game management
    public bool IsCurrentPlayerTurn(PlayerColor color)
    {
        bool isCurrentTurn = CurrentPlayer == color;
        Debug.Log($"GameManager: IsCurrentPlayerTurn({color}) - Result: {isCurrentTurn}");
        return isCurrentTurn;
    }

    public bool HasWinningCondition(PlayerColor color)
    {
        var player = GetPlayer(color);
        if (player == null)
        {
            Debug.Log($"GameManager: HasWinningCondition({color}) - Player not found");
            return false;
        }

        bool hasWon = player.Pieces.All(piece => piece.HasReachedHome);
        Debug.Log($"GameManager: HasWinningCondition({color}) - Result: {hasWon}");
        return hasWon;
    }

    public int GetNumberOfPlayersInGame()
    {
        Debug.Log($"GameManager: GetNumberOfPlayersInGame() - Count: {numberOfPlayers}");
        return numberOfPlayers;
    }

    public void SetNumberOfPlayers(int count)
    {
        Debug.Log($"GameManager: SetNumberOfPlayers called with count: {count}");
        numberOfPlayers = Mathf.Clamp(count, 1, 4);
        Debug.Log($"GameManager: Number of players set to: {numberOfPlayers}");

        // Activate/deactivate players based on count
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                bool shouldActivate = false;

                if (numberOfPlayers == 2)
                {
                    // Activate players at positions 0 and 2 (opposite corners)
                    shouldActivate = (i == 0 || i == 2);
                }
                else if (numberOfPlayers == 3)
                {
                    // Activate players at positions 0, 1, and 3
                    shouldActivate = (i == 0 || i == 1 || i == 3);
                }
                else if (numberOfPlayers == 4)
                {
                    // Activate all players
                    shouldActivate = true;
                }

                players[i].gameObject.SetActive(shouldActivate);
                //dices[i].gameObject.SetActive(shouldActivate);
                Debug.Log($"GameManager: Player {i} ({players[i].Color}) active: {shouldActivate}");
            }
        }

        // Ensure current player index is valid
        if (currentPlayerIndex >= numberOfPlayers)
        {
            currentPlayerIndex = 0;
            Debug.Log($"GameManager: Reset current player index to 0");
        }
        StartGame();
    }

    // Method to handle game restart
    public void RestartGame()
    {
        Debug.Log("GameManager: Restarting game");

        // Reset all pieces to start positions
        foreach (var player in players)
        {
            if (player != null && player.gameObject.activeSelf)
            {
                Debug.Log($"GameManager: Resetting pieces for player {player.Color}");
                foreach (var piece in player.Pieces)
                {
                    piece.ReturnToStart();
                }
            }
        }

        // Reset game state
        currentPlayerIndex = 0;
        lastDiceValue = 0;
        Debug.Log("GameManager: Game state reset");

        // Clear observers if needed and re-register
        observers.Clear();
        Debug.Log("GameManager: Observers cleared");

        // Start new game
        StartGame();
    }

    // Method to pause/resume game
    public void PauseGame()
    {
        Debug.Log("GameManager: Game paused");
        Time.timeScale = 0f;
        // Additional pause logic
    }

    public void ResumeGame()
    {
       // Debug.Log("GameManager: Game resumed");
        Time.timeScale = 1f;
        // Additional resume logic
    }

    // Method to save game state (for future implementation)
    public void SaveGameState()
    {
        Debug.Log("GameManager: SaveGameState called (not implemented)");
        // Implementation for saving current game state
        // This could serialize the current positions of all pieces,
        // current player, dice value, etc.
    }

    // Method to load game state (for future implementation)
    public void LoadGameState()
    {
        Debug.Log("GameManager: LoadGameState called (not implemented)");
        // Implementation for loading saved game state
    }

    // Validation methods
    private bool ValidateGameSetup()
    {
        Debug.Log("GameManager: Validating game setup");

        if (players == null || players.Length == 0)
        {
            Debug.LogError("No players configured!");
            return false;
        }
        Debug.Log($"GameManager: {players.Length} players configured");

        if (dices == null || dices.Length == 0)
        {
            Debug.LogError("No dice configured!");
            return false;
        }
        Debug.Log($"GameManager: {dices.Length} dice configured");

        if (board == null)
        {
            Debug.LogError("No board found!");
            return false;
        }
        Debug.Log("GameManager: Board validated");

        Debug.Log("GameManager: Game setup validation passed");
        return true;
    }

    // Debug methods for testing
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugGameState()
    {
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Current Player: {CurrentPlayer}");
        Debug.Log($"Last Dice Value: {lastDiceValue}");
        Debug.Log($"Number of Observers: {observers.Count}");
        Debug.Log($"Number of Players: {numberOfPlayers}");
    }

    // Method to force a specific dice value (for testing)
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void SetTestDiceValue(int value)
    {
        lastDiceValue = Mathf.Clamp(value, 1, 6);
        Debug.Log($"GameManager: Test dice value set to: {lastDiceValue}");
      //  OnDiceRolled(lastDiceValue);
    }

    private void OnDestroy()
    {
        Debug.Log("GameManager: OnDestroy called");

        // Cleanup event subscriptions
        if (dices != null)
        {
            foreach (var dice in dices)
            {
                if (dice != null)
                {
                    dice.OnDiceRolled -= OnDiceRolled;
                }
            }
            Debug.Log("GameManager: Dice event subscriptions cleaned up");
        }

        // Clear observers
        observers.Clear();
        Debug.Log("GameManager: Observers cleared on destroy");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
       // Debug.Log($"GameManager: Application pause status: {pauseStatus}");
        if (pauseStatus)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    // Unity Editor specific methods for testing
#if UNITY_EDITOR
    [UnityEngine.ContextMenu("Debug Game State")]
    private void DebugGameStateMenu()
    {
        DebugGameState();
    }

    [UnityEngine.ContextMenu("Force Next Turn")]
    private void ForceNextTurn()
    {
        EndTurn();
    }

    [UnityEngine.ContextMenu("Reset Game")]
    private void ResetGameMenu()
    {
        RestartGame();
    }
#endif
}

// State Pattern Implementation
public interface IGameState
{
    void OnEnter();
    void OnExit();
    void OnDiceRolled(int value);
    void OnPieceClicked(IPlayerPiece piece);
}

public class WaitingForDiceRollState : IGameState
{
    private readonly GameManager gameManager;

    public WaitingForDiceRollState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void OnEnter()
    {
        Debug.Log("State: Entered WaitingForDiceRollState");
        
    }

    public void OnExit()
    {
        Debug.Log("State: Exited WaitingForDiceRollState");
       
    }

    public void OnDiceRolled(int value)
    {
        Debug.Log($"WaitingForDiceRollState: Dice rolled with value {value}");
        var currentPlayer = gameManager.GetCurrentPlayer();

        if (currentPlayer.HasMovablePieces(value))
        {
            Debug.Log($"WaitingForDiceRollState: Player {currentPlayer.Color} has movable pieces");
            if (currentPlayer.IsHumanPlayer)
            {
                Debug.Log("WaitingForDiceRollState: Human player, waiting for move selection");
                gameManager.ChangeState(GameState.WaitingForPlayerMove);
                gameManager.EnablePieceSelection(currentPlayer, value);
            }
            else
            {
                Debug.Log("WaitingForDiceRollState: AI player, taking turn automatically");
                // AI player takes turn automatically
                currentPlayer.Strategy.TakeTurn(currentPlayer, value);
            }
        }
        else
        {
            Debug.Log($"WaitingForDiceRollState: Player {currentPlayer.Color} has no movable pieces, ending turn");
            // No movable pieces, end turn
            gameManager.EndTurn();
        }
    }

    public void OnPieceClicked(IPlayerPiece piece)
    {
        Debug.Log("WaitingForDiceRollState: Piece click ignored (waiting for dice roll)");
        // Ignore piece clicks when waiting for dice roll
    }
}

public class WaitingForPlayerMoveState : IGameState
{
    private readonly GameManager gameManager;

    public WaitingForPlayerMoveState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void OnEnter()
    {
        Debug.Log("State: Entered WaitingForPlayerMoveState");
        // Visual feedback enabled in GameManager
    }

    public void OnExit()
    {
        Debug.Log("State: Exited WaitingForPlayerMoveState");
        gameManager.DisableAllHighlights();
    }

    public void OnDiceRolled(int value)
    {
        Debug.Log($"WaitingForPlayerMoveState: Dice roll ignored (value: {value})");
        // Ignore additional dice rolls
    }

    public void OnPieceClicked(IPlayerPiece piece)
    {
        Debug.Log($"WaitingForPlayerMoveState: Piece clicked - Color: {piece.Color},gameManager CurrentPlayer: {gameManager.CurrentPlayer} lastdiceValue: {gameManager.GetLastDiceValue()}");

        if (piece.Color == gameManager.CurrentPlayer && piece.CanMove(gameManager.GetLastDiceValue()))
        {
            Debug.Log("WaitingForPlayerMoveState: Valid piece selected, moving piece");
            gameManager.MovePiece(piece, gameManager.GetLastDiceValue());
        }
        else
        {
            Debug.Log("WaitingForPlayerMoveState: Invalid piece selection");
        }
    }
}

public class PlayerMovingState : IGameState
{
    private readonly GameManager gameManager;

    public PlayerMovingState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void OnEnter()
    {
        Debug.Log("State: Entered PlayerMovingState");
        // Animation state - disable interactions
    }

    public void OnExit()
    {
        Debug.Log("State: Exited PlayerMovingState");
        // Re-enable interactions
    }

    public void OnDiceRolled(int value)
    {
        Debug.Log($"PlayerMovingState: Dice roll ignored during movement (value: {value})");
        // Ignore dice rolls during movement
    }

    public void OnPieceClicked(IPlayerPiece piece)
    {
        Debug.Log("PlayerMovingState: Piece click ignored during movement");
        // Ignore piece clicks during movement
    }
}

public class GameOverState : IGameState
{
    private readonly GameManager gameManager;

    public GameOverState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void OnEnter()
    {
        Debug.Log("State: Entered GameOverState");
        // Show game over UI
        Debug.Log($"Game Over! Winner: {gameManager.CurrentPlayer}");
    }

    public void OnExit()
    {
        Debug.Log("State: Exited GameOverState");
        // Clean up game over state
    }

    public void OnDiceRolled(int value)
    {
        Debug.Log($"GameOverState: All interactions ignored (dice: {value})");
        // Ignore all interactions
    }

    public void OnPieceClicked(IPlayerPiece piece)
    {
        Debug.Log("GameOverState: All interactions ignored (piece click)");
        // Ignore all interactions
    }
}