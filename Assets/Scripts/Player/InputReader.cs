using UnityEngine;
using UnityEngine.InputSystem;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Тонкая обёртка над новым Input System. Игра читает ввод только здесь,
    /// поэтому переключение на раскладку или геймпад не трогает игровую логику.
    /// </summary>
    [DisallowMultipleComponent]
    public class InputReader : MonoBehaviour
    {
        const float StickDeadzone = 0.18f;

        public Vector2 Move { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }

        bool _pauseLatch;
        bool _restartLatch;

        public bool PausePressed
        {
            get
            {
                bool pressed = ReadKeyPause() || ReadButtonPause();
                bool result = pressed && !_pauseLatch;
                _pauseLatch = pressed;
                return result;
            }
        }

        public bool RestartPressed
        {
            get
            {
                bool pressed = ReadKeyRestart() || ReadButtonRestart();
                bool result = pressed && !_restartLatch;
                _restartLatch = pressed;
                return result;
            }
        }

        void Update()
        {
            ReadGameplay();
        }

        void ReadGameplay()
        {
            Vector2 v = Vector2.zero;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > StickDeadzone * StickDeadzone) v += stick;
            }

            if (v.sqrMagnitude > 1f) v.Normalize();
            Move = v;

            JumpPressed = (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                          (pad != null && pad.buttonSouth.wasPressedThisFrame);
            JumpHeld = (kb != null && kb.spaceKey.isPressed) ||
                       (pad != null && pad.buttonSouth.isPressed);
        }

        public static bool ConfirmPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            return pad != null && pad.buttonSouth.wasPressedThisFrame;
        }

        public static bool UpPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            return pad != null && pad.dpad.up.wasPressedThisFrame;
        }

        public static bool DownPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            return pad != null && pad.dpad.down.wasPressedThisFrame;
        }

        public static bool CancelPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            return pad != null && pad.buttonEast.wasPressedThisFrame;
        }

        static bool ReadKeyPause() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        static bool ReadButtonPause() => Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        static bool ReadKeyRestart() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        static bool ReadButtonRestart() => Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
    }
}