// Doublures MINIMALES de l'Input System, pour que le compilateur de controle
// passe aussi par la branche #if ENABLE_INPUT_SYSTEM de FiefInput. Les noms sont
// ceux du vrai paquet (com.unity.inputsystem) : n'ajoute ici que des membres qui
// existent vraiment dans l'Input System.
namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed { get { return false; } }
        public bool wasPressedThisFrame { get { return false; } }
        public bool wasReleasedThisFrame { get { return false; } }
    }
    public class KeyControl : ButtonControl { }
    public class Vector2Control
    {
        public UnityEngine.Vector2 ReadValue() { return UnityEngine.Vector2.zero; }
    }
}

namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;

    public class Keyboard
    {
        public static Keyboard current { get { return null; } }
        public KeyControl wKey, aKey, sKey, dKey, eKey, cKey, gKey, pKey, fKey, qKey, zKey, hKey, mKey, tKey, rKey;
        public KeyControl upArrowKey, downArrowKey, leftArrowKey, rightArrowKey;
        public KeyControl spaceKey, escapeKey, tabKey, leftShiftKey, enterKey;
        public KeyControl f1Key, f2Key, f3Key, digit1Key, digit2Key, digit3Key, digit4Key, digit5Key;
    }

    public class Mouse
    {
        public static Mouse current { get { return null; } }
        public Vector2Control delta, scroll, position;
        public ButtonControl leftButton, rightButton, middleButton;
    }
}
