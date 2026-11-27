#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
internal static class VirtualGamepadSimulator
{
    private const string MenuPath = "Tools/SproutScout/Virtual Gamepad Simulator";
    private const string EnabledPreference = "SproutScout.VirtualGamepadSimulator.Enabled";

    private static Gamepad virtualGamepad;
    private static bool IsEnabled => EditorPrefs.GetBool(EnabledPreference, false);

    static VirtualGamepadSimulator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += QueueKeyboardInput;

        if (EditorApplication.isPlaying && IsEnabled)
        {
            AddVirtualGamepad();
        }
    }

    [MenuItem(MenuPath)]
    private static void ToggleSimulator()
    {
        bool enable = !IsEnabled;
        EditorPrefs.SetBool(EnabledPreference, enable);

        if (enable && EditorApplication.isPlaying)
        {
            AddVirtualGamepad();
        }
        else if (!enable)
        {
            RemoveVirtualGamepad();
        }

        Debug.Log(enable
            ? "[VirtualGamepadSimulator] Enabled. In Play Mode, use F6=A/South, F7=B/East, F8=X/West, F9=Y/North, and arrow keys=left stick. Use a normal keyboard key or mouse to switch back."
            : "[VirtualGamepadSimulator] Disabled.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggleSimulator()
    {
        Menu.SetChecked(MenuPath, IsEnabled);
        return true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && IsEnabled)
        {
            AddVirtualGamepad();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            RemoveVirtualGamepad();
        }
    }

    private static void AddVirtualGamepad()
    {
        if (virtualGamepad == null || !virtualGamepad.added)
        {
            virtualGamepad = InputSystem.AddDevice<Gamepad>();
            Debug.Log("[VirtualGamepadSimulator] Virtual gamepad added.");
        }
    }

    private static void RemoveVirtualGamepad()
    {
        if (virtualGamepad != null && virtualGamepad.added)
        {
            InputSystem.RemoveDevice(virtualGamepad);
            Debug.Log("[VirtualGamepadSimulator] Virtual gamepad removed.");
        }

        virtualGamepad = null;
    }

    private static void QueueKeyboardInput()
    {
        if (!IsEnabled || !EditorApplication.isPlaying ||
            virtualGamepad == null || !virtualGamepad.added)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        GamepadState state = new GamepadState()
            .WithButton(GamepadButton.South, keyboard.f6Key.isPressed)
            .WithButton(GamepadButton.East, keyboard.f7Key.isPressed)
            .WithButton(GamepadButton.West, keyboard.f8Key.isPressed)
            .WithButton(GamepadButton.North, keyboard.f9Key.isPressed);

        float horizontal =
            (keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical =
            (keyboard.upArrowKey.isPressed ? 1f : 0f) -
            (keyboard.downArrowKey.isPressed ? 1f : 0f);
        state.leftStick = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

        InputSystem.QueueStateEvent(virtualGamepad, state);
    }
}
#endif
