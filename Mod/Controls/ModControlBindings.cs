using System;
using System.Reflection;
using SFS.Input;
using SFSEnhanced.Mod.Integrated;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace SFSEnhanced.Mod.Controls
{
    internal enum ShortcutAction
    {
        ToggleMenu,
        ToggleTorque
    }

    internal struct ShortcutBinding
    {
        public KeyCode Key;
        public bool Control;
        public bool Shift;
        public bool Alt;
    }

    internal static class ModControlBindings
    {
        private const string Prefix = "sfs_enhanced.controls.";
        private static ShortcutAction? _captureAction;

        public static event Action Changed;

        public static string Status { get; private set; } = "Shortcuts are active globally when their context allows.";

        public static bool IsCapturing => _captureAction.HasValue;

        public static ShortcutAction? CapturingAction => _captureAction;

        public static ShortcutBinding GetBinding(ShortcutAction action)
        {
            KeyCode defaultKey = action == ShortcutAction.ToggleMenu ? KeyCode.F8 : KeyCode.T;
            bool defaultControl = action == ShortcutAction.ToggleTorque;
            string key = action.ToString();
            int rawKey = PlayerPrefs.GetInt(Prefix + key + ".key", (int)defaultKey);
            KeyCode resolvedKey = Enum.IsDefined(typeof(KeyCode), rawKey) ? (KeyCode)rawKey : defaultKey;
            return new ShortcutBinding
            {
                Key = resolvedKey,
                Control = PlayerPrefs.GetInt(Prefix + key + ".ctrl", defaultControl ? 1 : 0) == 1,
                Shift = PlayerPrefs.GetInt(Prefix + key + ".shift", 0) == 1,
                Alt = PlayerPrefs.GetInt(Prefix + key + ".alt", 0) == 1
            };
        }

        public static string Display(ShortcutAction action)
        {
            ShortcutBinding binding = GetBinding(action);
            if (binding.Key == KeyCode.None) return "UNBOUND";
            string value = string.Empty;
            if (binding.Control) value += "Ctrl + ";
            if (binding.Shift) value += "Shift + ";
            if (binding.Alt) value += "Alt + ";
            return value + binding.Key;
        }

        public static void BeginCapture(ShortcutAction action)
        {
            _captureAction = action;
            Status = "Press a key or joystick button. Escape cancels.";
            NotifyChanged();
        }

        public static void CancelCapture()
        {
            if (!_captureAction.HasValue) return;
            _captureAction = null;
            Status = "Key capture cancelled.";
            NotifyChanged();
        }

        public static void ResetDefaults()
        {
            foreach (ShortcutAction action in Enum.GetValues(typeof(ShortcutAction)))
            {
                string key = Prefix + action;
                PlayerPrefs.DeleteKey(key + ".key");
                PlayerPrefs.DeleteKey(key + ".ctrl");
                PlayerPrefs.DeleteKey(key + ".shift");
                PlayerPrefs.DeleteKey(key + ".alt");
            }
            PlayerPrefs.Save();
            _captureAction = null;
            Status = "Default shortcuts restored.";
            NotifyChanged();
        }

        public static void Tick(ModMain mod)
        {
            if (mod == null || mod.IsDisabled) return;
            if (_captureAction.HasValue)
            {
                CaptureNextInput();
                return;
            }
            if (HasTextInputFocus()) return;

            if (Pressed(ShortcutAction.ToggleMenu))
            {
                if (mod.Menu != null && mod.Menu.CanOpenHere()) mod.Menu.Toggle();
                return;
            }

            if (Pressed(ShortcutAction.ToggleTorque) && SceneManager.GetActiveScene().name == "World_PC")
                TorqueTogglePatch.Toggle();
        }

        private static void CaptureNextInput()
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || !Input.GetKeyDown(key)) continue;
                if (IsModifier(key) || IsMouseButton(key)) continue;
                if (key == KeyCode.Escape)
                {
                    CancelCapture();
                    return;
                }

                var binding = new ShortcutBinding
                {
                    Key = key,
                    Control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                    Shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                    Alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
                };

                ShortcutAction action = _captureAction.Value;
                ShortcutAction other = action == ShortcutAction.ToggleMenu ? ShortcutAction.ToggleTorque : ShortcutAction.ToggleMenu;
                if (Same(binding, GetBinding(other)))
                {
                    Status = "That shortcut is already assigned to " + other + ". Choose another key.";
                    NotifyChanged();
                    return;
                }

                string nativeAction;
                if (ConflictsWithNative(binding, out nativeAction))
                {
                    Status = "That shortcut is already assigned to the native SFS control " + nativeAction + ". Choose another key.";
                    NotifyChanged();
                    return;
                }

                string prefix = Prefix + action;
                PlayerPrefs.SetInt(prefix + ".key", (int)binding.Key);
                PlayerPrefs.SetInt(prefix + ".ctrl", binding.Control ? 1 : 0);
                PlayerPrefs.SetInt(prefix + ".shift", binding.Shift ? 1 : 0);
                PlayerPrefs.SetInt(prefix + ".alt", binding.Alt ? 1 : 0);
                PlayerPrefs.Save();
                _captureAction = null;
                Status = action + " is now bound to " + Display(action) + ".";
                NotifyChanged();
                return;
            }
        }

        private static bool Pressed(ShortcutAction action)
        {
            ShortcutBinding binding = GetBinding(action);
            if (binding.Key == KeyCode.None || !Input.GetKeyDown(binding.Key)) return false;
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            return control == binding.Control && shift == binding.Shift && alt == binding.Alt;
        }

        private static bool HasTextInputFocus()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }

        private static bool IsModifier(KeyCode key)
        {
            return key == KeyCode.LeftControl || key == KeyCode.RightControl
                || key == KeyCode.LeftShift || key == KeyCode.RightShift
                || key == KeyCode.LeftAlt || key == KeyCode.RightAlt;
        }

        private static bool IsMouseButton(KeyCode key)
        {
            return (int)key >= (int)KeyCode.Mouse0 && (int)key <= (int)KeyCode.Mouse6;
        }

        private static bool ConflictsWithNative(ShortcutBinding binding, out string actionName)
        {
            actionName = null;
            if (binding.Key == KeyCode.None || binding.Shift || binding.Alt) return false;
            KeybindingsPC.Data data = KeybindingsPC.keys;
            if (data == null) return false;
            foreach (FieldInfo field in typeof(KeybindingsPC.Data).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = field.GetValue(data);
                if (value is KeybindingsPC.Key key && key.key == binding.Key && key.ctrl == binding.Control)
                {
                    actionName = field.Name;
                    return true;
                }
                if (value is KeybindingsPC.Key[] keys)
                {
                    foreach (KeybindingsPC.Key item in keys)
                    {
                        if (item == null || item.key != binding.Key || item.ctrl != binding.Control) continue;
                        actionName = field.Name;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool Same(ShortcutBinding left, ShortcutBinding right)
        {
            return left.Key == right.Key && left.Control == right.Control
                && left.Shift == right.Shift && left.Alt == right.Alt;
        }

        private static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
