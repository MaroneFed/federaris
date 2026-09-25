using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Fief
{
    /// <summary>
    /// Couche d'abstraction des entrees.
    ///
    /// Pourquoi : Unity a DEUX systemes d'input (l'ancien "Input Manager" et le nouvel
    /// "Input System"). Selon la config du projet, l'un ou l'autre est actif. Ce fichier
    /// compile dans les deux cas grace aux directives #if : le reste du code appelle
    /// simplement FiefInput.Move et ne se pose aucune question.
    ///
    /// AZERTY : le nouvel Input System raisonne en POSITION physique de touche.
    /// "Key.W" designe la touche en haut a gauche du bloc de deplacement, donc
    /// le Z d'un clavier AZERTY. ZQSD et WASD marchent donc tous les deux, sans reglage.
    /// </summary>
    public static class FiefInput
    {
        /// <summary>x = lateral, y = avant/arriere. Longueur max 1.</summary>
        public static Vector2 Move
        {
            get
            {
                float x = 0f;
                float y = 0f;
#if ENABLE_INPUT_SYSTEM
                Keyboard k = Keyboard.current;
                if (k != null)
                {
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                }
#else
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
#endif
                Vector2 v = new Vector2(x, y);
                if (v.sqrMagnitude > 1f) v.Normalize();
                return v;
            }
        }

        /// <summary>Deplacement de la souris de la frame, en pixels.</summary>
        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Mouse m = Mouse.current;
                if (m == null) return Vector2.zero;
                return m.delta.ReadValue();
#else
                // L'axe legacy est deja lisse et divise par ~10 : on remet a l'echelle "pixels".
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
            }
        }

        /// <summary>Molette, normalisee a +/-1 par cran.</summary>
        public static float ZoomNotches
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Mouse m = Mouse.current;
                if (m == null) return 0f;
                float raw = m.scroll.ReadValue().y;
                if (Mathf.Abs(raw) < 0.01f) return 0f;
                return Mathf.Clamp(raw / 120f, -3f, 3f);
#else
                return Input.GetAxis("Mouse ScrollWheel") * 10f;
#endif
            }
        }

        public static bool InteractHeld { get { return KeyHeld(KeyCode.E); } }
        public static bool InteractPressed { get { return KeyPressed(KeyCode.E); } }
        public static bool JumpPressed { get { return KeyPressed(KeyCode.Space); } }
        public static bool CancelPressed { get { return KeyPressed(KeyCode.Escape); } }
        public static bool HelpPressed { get { return KeyPressed(KeyCode.F1); } }
        public static bool DiagnosticPressed { get { return KeyPressed(KeyCode.F3); } }
        public static bool SprintHeld { get { return KeyHeld(KeyCode.LeftShift); } }

        public static bool JumpHeld { get { return KeyHeld(KeyCode.Space); } }
        /// <summary>F : grimper dans un arbre, ou en redescendre.</summary>
        public static bool ClimbPressed { get { return KeyPressed(KeyCode.F); } }
        /// <summary>Tab maintenu : le score du match.</summary>
        public static bool ScoresHeld { get { return KeyHeld(KeyCode.Tab); } }

        // --- les menus au clavier
        public static bool UpPressed { get { return KeyPressed(KeyCode.UpArrow) || KeyPressed(KeyCode.W); } }
        public static bool DownPressed { get { return KeyPressed(KeyCode.DownArrow) || KeyPressed(KeyCode.S); } }
        public static bool LeftPressed { get { return KeyPressed(KeyCode.LeftArrow) || KeyPressed(KeyCode.A); } }
        public static bool RightPressed { get { return KeyPressed(KeyCode.RightArrow) || KeyPressed(KeyCode.D); } }
        public static bool ConfirmPressed { get { return KeyPressed(KeyCode.Return) || KeyPressed(KeyCode.KeypadEnter); } }

        /// <summary>Clic gauche, a l'instant : POUSSER.</summary>
        public static bool PushPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Mouse m = Mouse.current;
                return m != null && m.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        /// <summary>
        /// Les capacites actives : 0 = clic droit, 1 = R, 2 = C, 3 = V (le don d'un
        /// sanctuaire). Voir AbilityInfo.Keys.
        /// </summary>
        public static bool CastPressed(int slot)
        {
            switch (slot)
            {
                case 0:
#if ENABLE_INPUT_SYSTEM
                    Mouse m = Mouse.current;
                    return m != null && m.rightButton.wasPressedThisFrame;
#else
                    return Input.GetMouseButtonDown(1);
#endif
                case 1: return KeyPressed(KeyCode.R);
                case 2: return KeyPressed(KeyCode.C);
                default: return KeyPressed(KeyCode.V);
            }
        }

        static bool KeyHeld(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM
            ButtonControl b = Resolve(code);
            return b != null && b.isPressed;
#else
            return Input.GetKey(code);
#endif
        }

        static bool KeyPressed(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM
            ButtonControl b = Resolve(code);
            return b != null && b.wasPressedThisFrame;
#else
            return Input.GetKeyDown(code);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static ButtonControl Resolve(KeyCode code)
        {
            Keyboard k = Keyboard.current;
            if (k == null) return null;
            switch (code)
            {
                case KeyCode.E: return k.eKey;
                case KeyCode.Space: return k.spaceKey;
                case KeyCode.Escape: return k.escapeKey;
                case KeyCode.F1: return k.f1Key;
                case KeyCode.F3: return k.f3Key;
                case KeyCode.LeftShift: return k.leftShiftKey;
                case KeyCode.F: return k.fKey;
                case KeyCode.R: return k.rKey;
                case KeyCode.C: return k.cKey;
                case KeyCode.V: return k.vKey;
                case KeyCode.W: return k.wKey;
                case KeyCode.A: return k.aKey;
                case KeyCode.S: return k.sKey;
                case KeyCode.D: return k.dKey;
                case KeyCode.UpArrow: return k.upArrowKey;
                case KeyCode.DownArrow: return k.downArrowKey;
                case KeyCode.LeftArrow: return k.leftArrowKey;
                case KeyCode.RightArrow: return k.rightArrowKey;
                case KeyCode.Return: return k.enterKey;
                case KeyCode.KeypadEnter: return k.numpadEnterKey;
                case KeyCode.Tab: return k.tabKey;
            }
            return null;
        }
#endif
    }
}
