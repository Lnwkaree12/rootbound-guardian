using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Setup")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Controller Prompt Sprites")]
    [SerializeField] private Sprite southButtonSprite;
    [SerializeField] private Sprite eastButtonSprite;
    [SerializeField] private Sprite westButtonSprite;
    [SerializeField] private Sprite northButtonSprite;
    [SerializeField] private Sprite rightShoulderButtonSprite;
    [SerializeField] private Sprite rightTriggerButtonSprite;
    [SerializeField] private Sprite leftStickPressButtonSprite;
    [SerializeField] private Sprite selectButtonSprite;
    [SerializeField] private Sprite startButtonSprite;
    [SerializeField] private Sprite leftStickSprite;
    [SerializeField] private Sprite rightStickSprite;
    [SerializeField] private Sprite dpadSprite;

    public Vector2 MoveInput { get; private set; }
    public bool DashPressed { get; private set; }

    // ��ҹ��� WasPressedThisFrame �ç� �ҡ Action
    public bool InteractPressed => interactAction != null && interactAction.WasPressedThisFrame();

    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction interactAction;
    private static PlayerInputHandler activeInstance;

    private void Awake()
    {
        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move");
                dashAction = playerMap.FindAction("Sprint");
                interactAction = playerMap.FindAction("Interact");
            }
        }
    }

    private void OnEnable()
    {
        activeInstance = this;
        moveAction?.Enable();
        dashAction?.Enable();
        interactAction?.Enable();

        if (dashAction != null)
            dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }

        moveAction?.Disable();
        dashAction?.Disable();
        interactAction?.Disable();

        if (dashAction != null)
            dashAction.performed -= OnDashPerformed;
    }

    public static Sprite GetControllerPromptSprite(InputSystemInput.PromptAction action)
    {
        return activeInstance != null ? activeInstance.GetPromptSprite(action) : null;
    }

    private Sprite GetPromptSprite(InputSystemInput.PromptAction action)
    {
        switch (action)
        {
            case InputSystemInput.PromptAction.Confirm:
            case InputSystemInput.PromptAction.Jump: return southButtonSprite;
            case InputSystemInput.PromptAction.Back:
            case InputSystemInput.PromptAction.Crouch: return eastButtonSprite;
            case InputSystemInput.PromptAction.Attack: return westButtonSprite;
            case InputSystemInput.PromptAction.Interact: return northButtonSprite;
            case InputSystemInput.PromptAction.Sprint:
                return rightShoulderButtonSprite != null ? rightShoulderButtonSprite : leftStickPressButtonSprite;
            case InputSystemInput.PromptAction.Inventory: return selectButtonSprite;
            case InputSystemInput.PromptAction.Pause: return startButtonSprite;
            case InputSystemInput.PromptAction.Potion: return rightTriggerButtonSprite;
            case InputSystemInput.PromptAction.Move: return leftStickSprite;
            case InputSystemInput.PromptAction.Look: return rightStickSprite;
            case InputSystemInput.PromptAction.Dpad: return dpadSprite;
            default: return null;
        }
    }

    private void Update()
    {
        if (moveAction != null)
        {
            MoveInput = moveAction.ReadValue<Vector2>();
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        DashPressed = true;
    }

    public void ResetDashFlag()
    {
        DashPressed = false;
    }

    public void ResetInputState()
    {
        MoveInput = Vector2.zero;
        DashPressed = false;
    }
}

public static class InputSystemInput
{
    private const string ControllerPromptSpriteAssetResourcePath = "Sprite Assets/ControllerPromptIcons";

    public enum PromptAction
    {
        Confirm,
        Back,
        Jump,
        Interact,
        Attack,
        Crouch,
        Sprint,
        Inventory,
        Pause,
        Potion,
        Move,
        Look,
        Dpad
    }

    private static InputDevice lastControllerDevice;
    private static TMP_SpriteAsset controllerPromptSpriteAsset;
    private static bool controllerPromptSpriteAssetChecked;

    public static bool IsControllerInputActive { get; private set; }
    public static event Action<bool> OnLastInputDeviceChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLastInputDevice()
    {
        IsControllerInputActive = false;
        lastControllerDevice = null;
        OnLastInputDeviceChanged = null;
        controllerPromptSpriteAsset = null;
        controllerPromptSpriteAssetChecked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeControllerPromptSpriteAsset()
    {
        EnsureControllerPromptSpriteAsset();
    }

    public static bool SubmitPressedThisFrame => IsGamepadButtonPressedThisFrame(0);
    public static bool SubmitPressed => IsGamepadButtonPressed(0);
    public static bool CancelPressedThisFrame => IsGamepadButtonPressedThisFrame(1);
    public static bool PausePressedThisFrame => IsGamepadButtonPressedThisFrame(7);
    public static bool InventoryPressedThisFrame => IsGamepadButtonPressedThisFrame(6);
    public static bool UsePotionPressedThisFrame => IsGamepadTriggerPressedThisFrame();
    public static bool DpadUpPressedThisFrame => IsGamepadDpadPressedThisFrame(0);
    public static bool DpadDownPressedThisFrame => IsGamepadDpadPressedThisFrame(1);
    public static bool DpadLeftPressedThisFrame => IsGamepadDpadPressedThisFrame(2);
    public static bool DpadRightPressedThisFrame => IsGamepadDpadPressedThisFrame(3);
    public static bool DpadDownPressed => IsGamepadDpadPressed(1);
    public static bool DpadLeftPressed => IsGamepadDpadPressed(2);
    public static bool DpadRightPressed => IsGamepadDpadPressed(3);
    public static string InteractButtonDisplayName => GetControllerButtonDisplayName(3, "Y");
    public static string SubmitButtonDisplayName => GetControllerButtonDisplayName(0, "A");
    public static string CancelButtonDisplayName => GetControllerButtonDisplayName(1, "B");
    public static string InventoryButtonDisplayName => GetControllerButtonDisplayName(6, "View");
    public static string PauseButtonDisplayName => GetControllerButtonDisplayName(7, "Start");
    public static string PotionButtonDisplayName => GetControllerTriggerDisplayName("RT");
    public static string SprintButtonDisplayName => GetControllerButtonDisplayName(5, "RB");

    public static Sprite GetControllerPromptSprite(PromptAction action)
    {
        return PlayerInputHandler.GetControllerPromptSprite(action);
    }

    public static string GetControllerPromptTag(PromptAction action)
    {
        string spriteName = GetControllerPromptSpriteName(action);
        return GetControllerPromptTag(spriteName, $"[{GetControllerPromptFallback(action)}]");
    }

    public static string FormatControllerGuideText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        const string rightStickLabelPlaceholder = "__RIGHT_STICK_LABEL__";
        string moveTag = GetControllerPromptTag("analogL", "[Left Stick]");
        string dpadTag = GetControllerPromptTag("bottonArrow5", "[D-pad]");
        return text
            .Replace("RB</color>", $"{GetControllerPromptTag(PromptAction.Sprint)}</color>")
            .Replace("[RB]", GetControllerPromptTag(PromptAction.Sprint))
            .Replace("Analog", moveTag)
            .Replace("右摇杆", rightStickLabelPlaceholder)
            .Replace("左摇杆", moveTag)
            .Replace("摇杆", moveTag)
            .Replace("LEFT JOYSTICK", moveTag)
            .Replace("DIRECTIONAL PAD", dpadTag)
            .Replace("方向键", dpadTag)
            .Replace("[Up]", GetControllerPromptTag("bottonArrowUp", "[Up]"))
            .Replace("[Down]", GetControllerPromptTag("bottonArrowDown", "[Down]"))
            .Replace("[Left]", GetControllerPromptTag("bottonArrowLeft", "[Left]"))
            .Replace("[Right]", GetControllerPromptTag("bottonArrowRight", "[Right]"))
            .Replace("[上]", GetControllerPromptTag("bottonArrowUp", "[上]"))
            .Replace("[下]", GetControllerPromptTag("bottonArrowDown", "[下]"))
            .Replace("[左]", GetControllerPromptTag("bottonArrowLeft", "[左]"))
            .Replace("[右]", GetControllerPromptTag("bottonArrowRight", "[右]"))
            .Replace(rightStickLabelPlaceholder, "右摇杆");
    }

    internal static void SetLastInputDevice(bool isController, InputDevice device = null)
    {
        if (IsControllerInputActive == isController &&
            (!isController || lastControllerDevice == device))
        {
            return;
        }

        IsControllerInputActive = isController;
        lastControllerDevice = isController ? device : null;
        OnLastInputDeviceChanged?.Invoke(isController);
    }

    public static string FormatControllerPromptText(string text)
    {
        if (!IsControllerInputActive || string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("[SPACEBAR]", GetControllerPromptTag(PromptAction.Sprint))
            .Replace("[Space]", GetControllerPromptTag(PromptAction.Confirm))
            .Replace("[SPACE]", GetControllerPromptTag(PromptAction.Confirm))
            .Replace("[空格键]", GetControllerPromptTag(PromptAction.Confirm))
            .Replace("[ENTER]", GetControllerPromptTag(PromptAction.Confirm))
            .Replace("[E]", GetControllerPromptTag(PromptAction.Interact))
            .Replace("[ESC]", GetControllerPromptTag(PromptAction.Back))
            .Replace("[Tab]", $"[{InventoryButtonDisplayName}]")
            .Replace("[I]", $"[{InventoryButtonDisplayName}]")
            .Replace("[1]", GetControllerPromptTag(PromptAction.Potion))
            .Replace("[H]", GetControllerPromptTag(PromptAction.Potion))
            .Replace("Space / Click", $"{GetControllerPromptTag(PromptAction.Confirm)} / Click")
            .Replace("Space / คลิก", $"{GetControllerPromptTag(PromptAction.Confirm)} / คลิก")
            .Replace("空格 / 点击", $"{GetControllerPromptTag(PromptAction.Confirm)} / 点击")
            .Replace("ANY KEY", "ANY BUTTON")
            .Replace("Any key", "Any button")
            .Replace("any key", "any button")
            .Replace("任意键", "任意按钮");
    }

    public static string FormatControllerPromptTextForLegacyUI(string text)
    {
        if (!IsControllerInputActive || string.IsNullOrEmpty(text))
        {
            return text;
        }

        return FormatControllerPromptText(text)
            .Replace("<sprite name=\"buttonJoyA\">", $"[{SubmitButtonDisplayName}]")
            .Replace("<sprite name=\"buttonJoyB\">", $"[{CancelButtonDisplayName}]")
            .Replace("<sprite name=\"buttonJoyY\">", $"[{InteractButtonDisplayName}]")
            .Replace("<sprite name=\"buttonJoyRB\">", $"[{SprintButtonDisplayName}]")
            .Replace("<sprite name=\"analogL\">", "[Left Stick]")
            .Replace("<sprite name=\"bottonArrow5\">", "[D-pad]")
            .Replace("<sprite name=\"bottonArrowUp\">", "[Up]")
            .Replace("<sprite name=\"bottonArrowDown\">", "[Down]")
            .Replace("<sprite name=\"bottonArrowLeft\">", "[Left]")
            .Replace("<sprite name=\"bottonArrowRight\">", "[Right]");
    }

    private static bool EnsureControllerPromptSpriteAsset()
    {
        if (controllerPromptSpriteAsset != null)
        {
            return true;
        }

        controllerPromptSpriteAsset = Resources.Load<TMP_SpriteAsset>(ControllerPromptSpriteAssetResourcePath);
        if (controllerPromptSpriteAsset == null)
        {
            if (!controllerPromptSpriteAssetChecked)
            {
                Debug.LogWarning("[InputSystemInput] Controller prompt sprite asset is missing. Generate it with Tools > SproutScout > Generate Controller Prompt Sprite Asset.");
                controllerPromptSpriteAssetChecked = true;
            }

            return false;
        }

        TMP_SpriteAsset defaultSpriteAsset = TMP_Settings.defaultSpriteAsset;
        if (defaultSpriteAsset == null)
        {
            TMP_Settings.defaultSpriteAsset = controllerPromptSpriteAsset;
        }
        else if (defaultSpriteAsset != controllerPromptSpriteAsset)
        {
            if (defaultSpriteAsset.fallbackSpriteAssets == null)
            {
                defaultSpriteAsset.fallbackSpriteAssets = new System.Collections.Generic.List<TMP_SpriteAsset>();
            }

            if (!defaultSpriteAsset.fallbackSpriteAssets.Contains(controllerPromptSpriteAsset))
            {
                defaultSpriteAsset.fallbackSpriteAssets.Add(controllerPromptSpriteAsset);
            }
        }

        controllerPromptSpriteAssetChecked = true;
        return true;
    }

    private static string GetControllerPromptSpriteName(PromptAction action)
    {
        switch (action)
        {
            case PromptAction.Confirm:
            case PromptAction.Jump: return "buttonJoyA";
            case PromptAction.Back:
            case PromptAction.Crouch: return "buttonJoyB";
            case PromptAction.Interact: return "buttonJoyY";
            case PromptAction.Sprint: return "buttonJoyRB";
            case PromptAction.Move: return "analogL";
            case PromptAction.Dpad: return "bottonArrow5";
            default: return null;
        }
    }

    private static string GetControllerPromptTag(string spriteName, string fallback)
    {
        return !string.IsNullOrEmpty(spriteName) && EnsureControllerPromptSpriteAsset()
            ? $"<sprite name=\"{spriteName}\">"
            : fallback;
    }

    private static string GetControllerPromptFallback(PromptAction action)
    {
        switch (action)
        {
            case PromptAction.Confirm:
            case PromptAction.Jump: return SubmitButtonDisplayName;
            case PromptAction.Back:
            case PromptAction.Crouch: return CancelButtonDisplayName;
            case PromptAction.Interact: return InteractButtonDisplayName;
            case PromptAction.Attack: return "X";
            case PromptAction.Sprint: return SprintButtonDisplayName;
            case PromptAction.Inventory: return InventoryButtonDisplayName;
            case PromptAction.Pause: return PauseButtonDisplayName;
            case PromptAction.Potion: return PotionButtonDisplayName;
            case PromptAction.Move: return "Left Stick";
            case PromptAction.Look: return "Right Stick";
            case PromptAction.Dpad: return "D-pad";
            default: return string.Empty;
        }
    }

    public static bool AnyKeyDown
    {
        get
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null &&
                (mouse.leftButton.wasPressedThisFrame ||
                 mouse.rightButton.wasPressedThisFrame ||
                 mouse.middleButton.wasPressedThisFrame))
            {
                return true;
            }

            foreach (Gamepad gamepad in Gamepad.all)
            {
                if (AnyGamepadButtonPressed(gamepad))
                {
                    return true;
                }
            }

            foreach (Joystick joystick in Joystick.all)
            {
                if (AnyJoystickButtonPressed(joystick))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static bool GetKeyDown(KeyCode keyCode)
    {
        if (TryGetJoystickButtonIndex(keyCode, out int joystickButtonIndex))
        {
            return IsGamepadButtonPressedThisFrame(joystickButtonIndex);
        }

        return TryGetKeyControl(keyCode, out KeyControl keyControl) &&
               keyControl.wasPressedThisFrame;
    }

    public static bool GetKey(KeyCode keyCode)
    {
        if (TryGetJoystickButtonIndex(keyCode, out int joystickButtonIndex))
        {
            return IsGamepadButtonPressed(joystickButtonIndex);
        }

        return TryGetKeyControl(keyCode, out KeyControl keyControl) &&
               keyControl.isPressed;
    }

    public static bool GetMouseButtonDown(int button)
    {
        ButtonControl mouseButton = GetMouseButtonControl(button);
        return mouseButton != null && mouseButton.wasPressedThisFrame;
    }

    public static bool GetMouseButton(int button)
    {
        ButtonControl mouseButton = GetMouseButtonControl(button);
        return mouseButton != null && mouseButton.isPressed;
    }

    public static bool IsControllerConnected => Gamepad.all.Count > 0 || Joystick.all.Count > 0;

    public static void UpdateLastInputDeviceActivity()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            SetLastInputDevice(false);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame ||
             mouse.middleButton.wasPressedThisFrame ||
             mouse.delta.ReadValue().sqrMagnitude > 1f ||
             mouse.scroll.ReadValue().sqrMagnitude > 1f))
        {
            SetLastInputDevice(false);
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (AnyGamepadButtonPressed(gamepad) ||
                gamepad.leftTrigger.ReadValue() > 0.2f ||
                gamepad.rightTrigger.ReadValue() > 0.2f ||
                gamepad.leftStick.ReadValue().sqrMagnitude > 0.04f ||
                gamepad.rightStick.ReadValue().sqrMagnitude > 0.04f)
            {
                SetLastInputDevice(true, gamepad);
            }
        }

        foreach (Joystick joystick in Joystick.all)
        {
            if (HasJoystickActivity(joystick))
            {
                SetLastInputDevice(true, joystick);
            }
        }
    }

    public static string ControllerDisplayName
    {
        get
        {
            if (Gamepad.all.Count > 0)
            {
                return Gamepad.all[0].displayName;
            }

            if (Joystick.all.Count > 0)
            {
                return Joystick.all[0].displayName;
            }

            return null;
        }
    }

    private static bool TryGetKeyControl(KeyCode keyCode, out KeyControl keyControl)
    {
        keyControl = null;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !TryConvertKeyCode(keyCode, out Key key))
        {
            return false;
        }

        keyControl = keyboard[key];
        return keyControl != null;
    }

    private static bool TryConvertKeyCode(KeyCode keyCode, out Key key)
    {
        string keyName = keyCode.ToString();
        if (keyName == "Return")
        {
            keyName = "Enter";
        }
        else if (keyName == "KeypadEnter")
        {
            keyName = "NumpadEnter";
        }
        else if (keyName.StartsWith("Alpha", StringComparison.Ordinal))
        {
            keyName = "Digit" + keyName.Substring("Alpha".Length);
        }
        else if (keyName.StartsWith("Keypad", StringComparison.Ordinal))
        {
            keyName = "Numpad" + keyName.Substring("Keypad".Length);
        }

        return Enum.TryParse(keyName, out key) && key != Key.None;
    }

    private static bool TryGetJoystickButtonIndex(KeyCode keyCode, out int index)
    {
        index = (int)keyCode - (int)KeyCode.JoystickButton0;
        return index >= 0 && index <= 19;
    }

    private static bool IsGamepadButtonPressedThisFrame(int index)
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            ButtonControl button = GetGamepadButton(gamepad, index);
            if (button != null && button.wasPressedThisFrame)
            {
                return true;
            }
        }

        foreach (Joystick joystick in Joystick.all)
        {
            ButtonControl button = GetJoystickButton(joystick, index);
            if (button != null && button.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGamepadButtonPressed(int index)
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            ButtonControl button = GetGamepadButton(gamepad, index);
            if (button != null && button.isPressed)
            {
                return true;
            }
        }

        foreach (Joystick joystick in Joystick.all)
        {
            ButtonControl button = GetJoystickButton(joystick, index);
            if (button != null && button.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    private static ButtonControl GetGamepadButton(Gamepad gamepad, int index)
    {
        switch (index)
        {
            case 0: return gamepad.buttonSouth;
            case 1: return gamepad.buttonEast;
            case 2: return gamepad.buttonWest;
            case 3: return gamepad.buttonNorth;
            case 4: return gamepad.leftShoulder;
            case 5: return gamepad.rightShoulder;
            case 6: return gamepad.selectButton;
            case 7: return gamepad.startButton;
            case 8: return gamepad.leftStickButton;
            case 9: return gamepad.rightStickButton;
            default: return null;
        }
    }

    private static string GetControllerButtonDisplayName(int index, string fallback)
    {
        if (lastControllerDevice is Gamepad gamepad)
        {
            ButtonControl gamepadButton = GetGamepadButton(gamepad, index);
            if (gamepadButton != null && !string.IsNullOrEmpty(gamepadButton.displayName))
            {
                return gamepadButton.displayName;
            }
        }
        else if (lastControllerDevice is Joystick joystick)
        {
            ButtonControl joystickButton = GetJoystickButton(joystick, index);
            if (joystickButton != null && !string.IsNullOrEmpty(joystickButton.displayName))
            {
                return joystickButton.displayName;
            }
        }

        return fallback;
    }

    private static string GetControllerTriggerDisplayName(string fallback)
    {
        if (lastControllerDevice is Gamepad gamepad &&
            !string.IsNullOrEmpty(gamepad.rightTrigger.displayName))
        {
            return gamepad.rightTrigger.displayName;
        }

        if (lastControllerDevice is Joystick joystick &&
            !string.IsNullOrEmpty(joystick.trigger.displayName))
        {
            return joystick.trigger.displayName;
        }

        return fallback;
    }

    private static bool IsGamepadTriggerPressedThisFrame()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad.rightTrigger.wasPressedThisFrame)
            {
                return true;
            }
        }

        foreach (Joystick joystick in Joystick.all)
        {
            if (joystick.trigger.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGamepadDpadPressedThisFrame(int direction)
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            ButtonControl button = GetDpadButton(gamepad, direction);
            if (button.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGamepadDpadPressed(int direction)
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            ButtonControl button = GetDpadButton(gamepad, direction);
            if (button.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    private static ButtonControl GetDpadButton(Gamepad gamepad, int direction)
    {
        switch (direction)
        {
            case 0: return gamepad.dpad.up;
            case 1: return gamepad.dpad.down;
            case 2: return gamepad.dpad.left;
            case 3: return gamepad.dpad.right;
            default: return null;
        }
    }

    private static ButtonControl GetJoystickButton(Joystick joystick, int index)
    {
        ButtonControl button = joystick.TryGetChildControl<ButtonControl>($"button{index + 1}");
        if (button == null && index == 0)
        {
            button = joystick.trigger;
        }

        return button;
    }

    private static bool AnyGamepadButtonPressed(Gamepad gamepad)
    {
        return gamepad.buttonSouth.wasPressedThisFrame ||
               gamepad.buttonEast.wasPressedThisFrame ||
               gamepad.buttonWest.wasPressedThisFrame ||
               gamepad.buttonNorth.wasPressedThisFrame ||
               gamepad.leftShoulder.wasPressedThisFrame ||
               gamepad.rightShoulder.wasPressedThisFrame ||
               gamepad.leftTrigger.wasPressedThisFrame ||
               gamepad.rightTrigger.wasPressedThisFrame ||
               gamepad.leftStickButton.wasPressedThisFrame ||
               gamepad.rightStickButton.wasPressedThisFrame ||
               gamepad.startButton.wasPressedThisFrame ||
               gamepad.selectButton.wasPressedThisFrame ||
               gamepad.dpad.up.wasPressedThisFrame ||
               gamepad.dpad.down.wasPressedThisFrame ||
               gamepad.dpad.left.wasPressedThisFrame ||
               gamepad.dpad.right.wasPressedThisFrame;
    }

    private static bool AnyJoystickButtonPressed(Joystick joystick)
    {
        foreach (var control in joystick.allControls)
        {
            ButtonControl button = control as ButtonControl;
            if (button != null && button.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasJoystickActivity(Joystick joystick)
    {
        foreach (InputControl control in joystick.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }

            if (control is AxisControl axis && Mathf.Abs(axis.ReadValue()) > 0.25f)
            {
                return true;
            }

            if (control is Vector2Control vector && vector.ReadValue().sqrMagnitude > 0.04f)
            {
                return true;
            }
        }

        return false;
    }

    private static ButtonControl GetMouseButtonControl(int button)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return null;
        }

        switch (button)
        {
            case 0: return mouse.leftButton;
            case 1: return mouse.rightButton;
            case 2: return mouse.middleButton;
            default: return null;
        }
    }
}