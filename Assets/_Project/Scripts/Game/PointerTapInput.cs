using CleanPath.Config;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    public class PointerTapInput : ITapInput
    {
        UnityEngine.InputSystem.InputAction _tap;
        bool _useFallback;
        public string InputPath { get; private set; }

        public PointerTapInput()
        {
            _tap = new UnityEngine.InputSystem.InputAction(
                type: UnityEngine.InputSystem.InputActionType.Button,
                binding: "<Pointer>/press");
            _tap.Enable();

            if (_tap.controls.Count > 0)
            {
                InputPath = "<Pointer>/press";
                _useFallback = false;
            }
            else
            {
                Debug.LogWarning("PointerTapInput: <Pointer>/press resolved to no controls; using fallback polling.");
                InputPath = "Mouse/Touch fallback";
                _useFallback = true;
            }
        }

        public bool IsHeld
        {
            get
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse?.leftButton.isPressed == true) return true;
                var touch = UnityEngine.InputSystem.Touchscreen.current;
                if (touch?.primaryTouch.press.isPressed == true) return true;
                if (!_useFallback) return _tap.IsPressed();
                return false;
            }
        }

        public void Dispose() => _tap?.Disable();
    }
}
