using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputs input;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float ZoomInput { get; private set; }
    
    public bool AttackHeld { get; private set; }
    public bool AimMode { get; private set; }
    public bool AlterHeld { get; private set; }
    public event Action OnInteract;
    public event Action OnBlessingMenu;
    public event Action OnDrop;
    public event Action OnEscape;
    public event Action<float> OnScroll;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        input = new PlayerInputs();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Gameplay.Drop.performed += DropPerformed;
        input.Gameplay.Aim.performed += AimPerformed;
        input.Gameplay.BlessingMenu.performed += BlessingMenuPerformed;
        input.Gameplay.Interact.performed += InteractPerformed;
        input.Gameplay.Escape.performed += EscapePerformed;
        input.Gameplay.Scroll.performed += OnScrollThrow;
    }

    private void OnDisable()
    {
        input.Gameplay.Drop.performed -= DropPerformed;
        input.Gameplay.Aim.performed -= AimPerformed;
        input.Gameplay.BlessingMenu.performed -= BlessingMenuPerformed;
        input.Gameplay.Interact.performed -= InteractPerformed;
        input.Gameplay.Escape.performed -= EscapePerformed;
        input.Gameplay.Scroll.performed -= OnScrollThrow;
        input.Disable();
    }

    private void Update()
    {
        MoveInput = input.Gameplay.Move.ReadValue<Vector2>();
        LookInput = input.Gameplay.Look.ReadValue<Vector2>();
        ZoomInput = input.Gameplay.Zoom.ReadValue<float>();
        AttackHeld = input.Gameplay.Attack.IsPressed();
        AlterHeld = input.Gameplay.Alter.IsPressed();
    }

    private void DropPerformed(InputAction.CallbackContext context)
    {
        OnDrop?.Invoke();
    }
    private void AimPerformed(InputAction.CallbackContext context)
    {
        AimMode = !AimMode;
    }
    private void BlessingMenuPerformed(InputAction.CallbackContext context)
    {
        OnBlessingMenu?.Invoke();
    }
    private void InteractPerformed(InputAction.CallbackContext context)
    {
        OnInteract?.Invoke();
    }
    private void EscapePerformed(InputAction.CallbackContext context)
    {
        OnEscape?.Invoke();
    }
    private void OnScrollThrow(InputAction.CallbackContext context)
{
    float value = context.ReadValue<float>();

    if (Mathf.Abs(value) > 0.01f)
        OnScroll?.Invoke(value);
}
}