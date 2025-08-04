using UnityEngine;
using System;
using System.Collections;
using System.ComponentModel;

// Dice implementation following Single Responsibility Principle
public class Dice : MonoBehaviour, IDice
{
    [SerializeField] private Sprite[] numberSprites;
    [SerializeField] private SpriteRenderer numberSpriteRenderer;
    [SerializeField] private SpriteRenderer diceAnimationRenderer;
    [SerializeField] private float rollAnimationDuration = 0.5f;

    private int lastRolledValue;
    private bool canRoll = true;
    public PlayerColor diceColor;
    public event Action<int, PlayerColor> OnDiceRolled;
    public int LastRolledValue => lastRolledValue;
    public bool CanRoll => canRoll;

    private void Start()
    {
        Debug.Log("[Dice] Dice initialized");
        numberSpriteRenderer.gameObject.SetActive(true);
        diceAnimationRenderer.gameObject.SetActive(false);
        Debug.Log("[Dice] Sprite renderers configured - Number: active, Animation: inactive");
    }

    public void Roll()
    {
        Debug.Log($"[Dice] Roll() called - CanRoll: {canRoll}");
        if (!canRoll)
        {
            Debug.LogWarning("[Dice] Roll attempted but dice cannot roll right now");
            return;
        }

        Debug.Log("[Dice] Starting roll animation");
        StartCoroutine(RollAnimation());
    }

    private IEnumerator RollAnimation()
    {
        Debug.Log("[Dice] RollAnimation started");
        canRoll = false;

        // Hide number, show animation
        numberSpriteRenderer.gameObject.SetActive(false);
        diceAnimationRenderer.gameObject.SetActive(true);
        Debug.Log("[Dice] Animation sprites swapped - showing roll animation");

        yield return new WaitForSeconds(rollAnimationDuration);

        // Generate random number (1-6)
        lastRolledValue = UnityEngine.Random.Range(1, 7);
        Debug.Log($"[Dice] Random value generated: {lastRolledValue}");

        // Show result
        numberSpriteRenderer.sprite = numberSprites[lastRolledValue - 1];
        numberSpriteRenderer.gameObject.SetActive(true);
        diceAnimationRenderer.gameObject.SetActive(false);
        Debug.Log($"[Dice] Result displayed - showing number {lastRolledValue}");

        // Notify observers
        OnDiceRolled?.Invoke(lastRolledValue,diceColor);
        Debug.Log($"[Dice] OnDiceRolled event fired with value: {lastRolledValue}");

        canRoll = true;
        Debug.Log("[Dice] Roll animation completed - dice ready for next roll");
    }

    private void OnMouseDown()
    {
        Debug.Log($"[Dice] Mouse clicked on dice - Current game state: {GameManager.Instance.CurrentState}");
        if (GameManager.Instance.CurrentState == GameState.WaitingForDiceRoll)
        {
            Debug.Log("[Dice] Game state allows dice roll - calling Roll()");
            Roll();
        }
        else
        {
            Debug.Log("[Dice] Game state does not allow dice roll - ignoring click");
        }
    }
}

// Command Pattern for dice rolling
public class RollDiceCommand
{
    private readonly IDice dice;

    public RollDiceCommand(IDice dice)
    {
        this.dice = dice;
        Debug.Log("[RollDiceCommand] Command created");
    }

    public void Execute()
    {
        Debug.Log($"[RollDiceCommand] Execute() called - Dice CanRoll: {dice.CanRoll}");
        if (dice.CanRoll)
        {
            Debug.Log("[RollDiceCommand] Executing dice roll");
            dice.Roll();
        }
        else
        {
            Debug.Log("[RollDiceCommand] Cannot execute - dice not ready");
        }
    }
}

public enum DiceColor
{
    Blue,
    Red,
    Green,
    Yellow
}