using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Invierte la gravedad del personaje (suelo <-> techo) con un número limitado de usos.
// Solo afecta a este cuerpo: las cajas y los proyectiles siguen con la gravedad normal.
// Al invertirla, PhysicsBody2D pasa a acelerar hacia arriba con g = 9,81 m/s² hasta apoyarse en el techo.
[RequireComponent(typeof(PhysicsBody2D))]
public class GravityInverter : MonoBehaviour
{
    [Header("Usos")]
    [SerializeField, Min(0)] private int maxUses = 3;
    [Tooltip("Si está desactivado, solo se puede invertir estando apoyado (en el suelo o en el techo)")]
    [SerializeField] private bool allowInAir = false;

    [Header("Controles")]
    [Tooltip("Tecla del teclado; en el mando es el botón Y (norte)")]
    [SerializeField] private Key keyboardKey = Key.E;

    private PhysicsBody2D body;
    private PlayerController2D player;
    private SpriteRenderer spriteRenderer;

    public int MaxUses => maxUses;
    public int UsesLeft { get; private set; }
    public bool IsInverted => body != null && body.GravitySign < 0;

    // Cambió el sentido de la gravedad
    public event Action GravityChanged;

    // Cambió la cantidad de usos disponibles
    public event Action UsesChanged;

    // Se intentó invertir sin usos o en el aire (para avisar en el HUD)
    public event Action Denied;

    private void Awake()
    {
        body = GetComponent<PhysicsBody2D>();
        player = GetComponent<PlayerController2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        UsesLeft = maxUses;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        Gamepad gamepad = Gamepad.current;
        bool pressed = (Keyboard.current != null && keyboardKey != Key.None && Keyboard.current[keyboardKey].wasPressedThisFrame)
                    || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        if (pressed) TryInvert();
    }

    // Invierte la gravedad si quedan usos (y si está apoyado, salvo que se permita en el aire)
    public bool TryInvert()
    {
        bool dead = player != null && player.IsDead;

        if (dead || UsesLeft <= 0 || (!allowInAir && !body.IsGrounded))
        {
            Denied?.Invoke();
            return false;
        }

        UsesLeft--;
        SetInverted(!IsInverted);
        UsesChanged?.Invoke();
        return true;
    }

    // Suma usos (por ejemplo, al recoger una recarga), sin pasar del máximo
    public void AddUses(int amount)
    {
        UsesLeft = Mathf.Clamp(UsesLeft + amount, 0, maxUses);
        UsesChanged?.Invoke();
    }

    // Gravedad normal y todos los usos, como al empezar el nivel
    public void ResetState()
    {
        if (IsInverted) SetInverted(false);
        UsesLeft = maxUses;
        UsesChanged?.Invoke();
    }

    private void SetInverted(bool inverted)
    {
        body.GravitySign = inverted ? -1 : 1;

        // El personaje se ve de cabeza mientras la gravedad está invertida
        if (spriteRenderer != null) spriteRenderer.flipY = inverted;

        GravityChanged?.Invoke();
    }
}
