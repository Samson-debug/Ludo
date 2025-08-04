using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// UI Manager implementing Observer Pattern and Single Responsibility
public class UIManager : MonoBehaviour, IGameObserver
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Game UI Elements")]
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI diceValueText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private Button[] playerSelectionButtons;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        ShowMainMenu();

        // Register as observer when game manager is available
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterObserver(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister observer
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterObserver(this);
        }
    }

    #region Menu Navigation

    public void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
    }

    public void ShowGamePanel()
    {
        SetActivePanel(gamePanel);
    }

    public void ShowSettingsPanel()
    {
        SetActivePanel(settingsPanel);
    }

    public void ShowGameOverPanel()
    {
        SetActivePanel(gameOverPanel);
    }

    private void SetActivePanel(GameObject activePanel)
    {
        mainMenuPanel.SetActive(activePanel == mainMenuPanel);
        gamePanel.SetActive(activePanel == gamePanel);
        settingsPanel.SetActive(activePanel == settingsPanel);
        gameOverPanel.SetActive(activePanel == gameOverPanel);
    }

    #endregion

    #region Game Setup

    public void StartGame(int numberOfPlayers)
    {
        PlayerPrefs.SetInt("NumberOfPlayers", numberOfPlayers);
        ShowGamePanel();
        GameManager.Instance.SetNumberOfPlayers(numberOfPlayers);
    }

    public void StartTwoPlayerGame() => StartGame(2);
    public void StartThreePlayerGame() => StartGame(3);
    public void StartFourPlayerGame() => StartGame(4);

    #endregion

    #region IGameObserver Implementation

    public void OnPlayerTurnChanged(PlayerColor currentPlayer)
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"Current Player: {currentPlayer}";
            currentPlayerText.color = GetPlayerColor(currentPlayer);
        }

        UpdateGameStatus($"{currentPlayer}'s turn - Roll the dice!");
    }

    public void OnDiceRolled(int value)
    {
        if (diceValueText != null)
        {
            diceValueText.text = $"Dice: {value}";
        }

        string message = value == 6 ? "Great roll! You get another turn!" : $"Rolled {value} - Choose your piece";
        UpdateGameStatus(message);
    }

    public void OnPieceMoved(IPlayerPiece piece, int steps)
    {
        UpdateGameStatus($"{piece.Color} piece moved {steps} steps");

        // Optional: Show move animation feedback in UI
        ShowMoveEffect(piece.Color, steps);
    }

    public void OnPieceKnockedOut(IPlayerPiece knockedPiece, IPlayerPiece attackingPiece)
    {
      //  UpdateGameStatus($"{attackingPiece.Color} knocked out {knockedPiece.Color}!");

        // Show knockout effect
       // ShowKnockoutEffect(knockedPiece.Color, attackingPiece.Color);
    }

    public void OnGameWon(PlayerColor winner)
    {
        ShowGameOverPanel();

        if (winnerText != null)
        {
            winnerText.text = $"{winner} Wins!";
            winnerText.color = GetPlayerColor(winner);
        }

        // Optional: Play celebration effect
        PlayWinEffect(winner);
    }

    #endregion

    #region UI Utilities

    private void UpdateGameStatus(string message)
    {
        if (gameStatusText != null)
        {
            gameStatusText.text = message;
        }
    }

    private Color GetPlayerColor(PlayerColor playerColor)
    {
        return playerColor switch
        {
            PlayerColor.Blue => Color.blue,
            PlayerColor.Red => Color.red,
            PlayerColor.Green => Color.green,
            PlayerColor.Yellow => Color.yellow,
            _ => Color.white
        };
    }

    private void ShowMoveEffect(PlayerColor color, int steps)
    {
        // Implement visual feedback for moves
        // Could be particle effects, UI animations, etc.
        Debug.Log($"{color} moved {steps} steps");
    }

    private void ShowKnockoutEffect(PlayerColor knocked, PlayerColor attacker)
    {
        // Implement visual feedback for knockouts
        Debug.Log($"{attacker} knocked out {knocked}!");
    }

    private void PlayWinEffect(PlayerColor winner)
    {
        // Implement celebration effects
        Debug.Log($"{winner} wins the game!");
    }

    #endregion

    #region Button Handlers

    public void OnPlayAgainClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnQuitGameClicked()
    {
        Application.Quit();
    }

    public void OnSettingsClicked()
    {
        ShowSettingsPanel();
    }

    public void OnBackFromSettingsClicked()
    {
        ShowMainMenu();
    }

    #endregion
}

// Separate class for settings management (Single Responsibility)
public class GameSettings : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Gameplay Settings")]
    [SerializeField] private Slider animationSpeedSlider;
    [SerializeField] private Toggle showHintsToggle;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUTE_KEY = "Mute";
    private const string ANIMATION_SPEED_KEY = "AnimationSpeed";
    private const string SHOW_HINTS_KEY = "ShowHints";

    private void Start()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.7f);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.7f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.isOn = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }

        if (animationSpeedSlider != null)
        {
            animationSpeedSlider.value = PlayerPrefs.GetFloat(ANIMATION_SPEED_KEY, 1.0f);
            animationSpeedSlider.onValueChanged.AddListener(OnAnimationSpeedChanged);
        }

        if (showHintsToggle != null)
        {
            showHintsToggle.isOn = PlayerPrefs.GetInt(SHOW_HINTS_KEY, 1) == 1;
            showHintsToggle.onValueChanged.AddListener(OnShowHintsToggled);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        // Apply to audio system
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        // Apply to audio system
        AudioManager.Instance?.SetSFXVolume(value);
    }

    private void OnMuteToggled(bool isMuted)
    {
        PlayerPrefs.SetInt(MUTE_KEY, isMuted ? 1 : 0);
        // Apply to audio system
        AudioManager.Instance?.SetMute(isMuted);
    }

    private void OnAnimationSpeedChanged(float speed)
    {
        PlayerPrefs.SetFloat(ANIMATION_SPEED_KEY, speed);
        // Apply to game
        Time.timeScale = speed;
    }

    private void OnShowHintsToggled(bool showHints)
    {
        PlayerPrefs.SetInt(SHOW_HINTS_KEY, showHints ? 1 : 0);
        // Apply to game
        GameManager.Instance?.SetHintsEnabled(showHints);
    }
}