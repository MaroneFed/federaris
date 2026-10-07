using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA SENSATION DES CAPACITES, POUR CELUI QUI LES LANCE (14/10, v41 -- Martin : "refais tout le
    /// design de toutes les capacites, je veux des dingueries, que ce soit exceptionnel pour celui qui
    /// la porte, une ADN incroyable").
    ///
    /// En premiere personne, tu ne te vois pas lancer : jusqu'ici, tu entendais un son et l'effet
    /// partait devant toi. Maintenant, CHAQUE capacite a trois temps, dosés par son RANG
    /// (AbilityInfo.Tier : commune, rare, epique, legendaire, divine) :
    ///
    ///   LE DEPART   ta camera encaisse (le champ de vision s'ouvre d'un coup, une secousse), une
    ///               gerbe a la couleur de la capacite jaillit du viseur ; legendaire et divine :
    ///               les bords de l'ecran s'embrasent de sa couleur un instant.
    ///   LA TOUCHE   ta capacite touche quelqu'un : une croix de touche s'allume autour du viseur,
    ///               un "ding" qui monte d'une note a chaque joueur touche, et un temps d'arret
    ///               (hit-stop) quand le coup est gros.
    ///   LE COMBO    plusieurs joueurs touches en moins d'une seconde et demie : "x2", "x3"... a cote
    ///               du viseur, de plus en plus gros.
    ///
    /// Rien de tout ca ne se voit chez les autres : c'est TA sensation.
    /// </summary>
    public static class CastFeel
    {
        static float castAt = -99f;
        static Color castTint = Color.white;
        static int castTier;

        static float hitAt = -99f;
        static Color hitTint = Color.white;
        static int combo;
        static float comboAt = -99f;
        static bool bigHit;

        const float CastLife = 0.45f;
        const float HitLife = 0.32f;
        const float ComboWindow = 1.5f;

        /// <summary>Tu viens de lancer "a" (AbilityCaster.Cast).</summary>
        public static void Cast(Seeker s, Ability a)
        {
            if (s == null || !s.IsPlayer) return;
            castTier = AbilityInfo.Tier(a);
            castTint = AbilityInfo.Tint(a);
            castAt = Time.unscaledTime;
            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null)
            {
                cam.Kick(2f + castTier * 1.6f);
                cam.Shake(0.04f + castTier * 0.035f);
            }
            if (castTier >= 3 && Game.PlayerTransform != null) Sfx.ThudAt(Game.PlayerTransform.position);
        }

        /// <summary>Un coup (ou un sort) de "by" vient de toucher "victim" (Combat.Hit, Combat.Afflict).</summary>
        public static void Hit(Seeker by, Seeker victim, float force)
        {
            if (by == null || !by.IsPlayer || victim == null || victim == by) return;
            float now = Time.unscaledTime;
            if (now - comboAt > ComboWindow) combo = 0;
            combo++;
            comboAt = now;
            hitAt = now;
            hitTint = by.HasActive ? AbilityInfo.Tint(by.CurrentActive) : Color.white;
            bigHit = force > 20f || combo >= 3;
            Sfx.Beep(1f + Mathf.Min(combo - 1, 6) * 0.12f);
            if (combo == 1 && force > 22f) Hud.HitStop(0.05f);
            else if (combo == 3) Hud.HitStop(0.08f);
        }

        public static void Draw()
        {
            float now = Time.unscaledTime;
            Vector2 c = new Vector2(Mathf.Round(Screen.width * 0.5f), Mathf.Round(Screen.height * 0.5f));
            DrawCast(now, c);
            DrawHit(now, c);
            DrawCombo(now, c);
        }

        static void DrawCast(float now, Vector2 c)
        {
            float age = now - castAt;
            if (age > CastLife || age < 0f) return;
            float t = age / CastLife;
            float a = 1f - t;
            // La gerbe : des eclats qui partent du viseur en etoile.
            int n = 8 + castTier * 3;
            float reach = UiStyle.S(40 + castTier * 26) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.6f));
            float size = UiStyle.S(9 + castTier * 2) * (1f - t * 0.6f);
            for (int k = 0; k < n; k++)
            {
                float ang = k * Mathf.PI * 2f / n + castTier * 0.3f;
                Vector2 p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * reach;
                Rect r = new Rect(Mathf.Round(p.x - size * 0.5f), Mathf.Round(p.y - size * 0.5f), Mathf.Round(size), Mathf.Round(size));
                Icons.Pill(r, new Color(castTint.r, castTint.g, castTint.b, a));
            }
            // Legendaire, divine : les bords de l'ecran s'embrasent un instant.
            if (castTier >= 3)
            {
                Color edge = castTier == 4 ? Color.Lerp(castTint, AbilityInfo.TierColour(4), 0.5f) : Color.Lerp(castTint, AbilityInfo.TierColour(3), 0.35f);
                float w = Screen.width, h = Screen.height;
                float thick = Mathf.Round(UiStyle.S(castTier == 4 ? 34f : 24f));
                for (int k = 0; k < 4; k++)
                {
                    float f = 1f - k / 4f;
                    float al = 0.42f * a * f;
                    float d = thick * k / 4f, tk = thick / 4f;
                    Color cc = new Color(edge.r, edge.g, edge.b, al);
                    UiStyle.Fill(new Rect(0f, d, w, tk), cc);
                    UiStyle.Fill(new Rect(0f, h - d - tk, w, tk), cc);
                    UiStyle.Fill(new Rect(d, 0f, tk, h), cc);
                    UiStyle.Fill(new Rect(w - d - tk, 0f, tk, h), cc);
                }
            }
        }

        static void DrawHit(float now, Vector2 c)
        {
            float age = now - hitAt;
            if (age > HitLife || age < 0f) return;
            float a = 1f - age / HitLife;
            // La croix de touche : quatre traits en diagonale autour du viseur, qui s'ecartent un peu.
            float gap = UiStyle.S(bigHit ? 12 : 9) + UiStyle.S(10) * (age / HitLife);
            float len = UiStyle.S(bigHit ? 16 : 11);
            float thick = Mathf.Max(2f, Mathf.Round(UiStyle.S(bigHit ? 4 : 3)));
            Color white = new Color(1f, 1f, 1f, a);
            Color tint = new Color(hitTint.r, hitTint.g, hitTint.b, a);
            for (int k = 0; k < 4; k++)
            {
                float ang = 45f + k * 90f;
                Matrix4x4 keep = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, c);
                UiStyle.Fill(new Rect(c.x + gap - 1f, c.y - thick * 0.5f - 1f, len + 2f, thick + 2f), new Color(0.05f, 0.04f, 0.1f, 0.7f * a));
                UiStyle.Fill(new Rect(c.x + gap, c.y - thick * 0.5f, len, thick), k % 2 == 0 ? white : tint);
                GUI.matrix = keep;
            }
        }

        static void DrawCombo(float now, Vector2 c)
        {
            float age = now - comboAt;
            if (combo < 2 || age > ComboWindow) return;
            float a = Mathf.Clamp01((ComboWindow - age) / 0.35f);
            float pop = 1f + 0.5f * Mathf.Clamp01(1f - age / 0.15f);
            int fs = Mathf.RoundToInt(UiStyle.S(26 + Mathf.Min(combo, 6) * 4) * pop);
            string txt = "x" + combo;
            float w = Icons.Width(txt, fs) + fs * 0.6f;
            Rect r = new Rect(Mathf.Round(c.x + UiStyle.S(34)), Mathf.Round(c.y - fs * 0.9f), Mathf.Round(w), Mathf.Round(fs * 1.4f));
            Icons.Pill(r, new Color(hitTint.r * 0.75f, hitTint.g * 0.75f, hitTint.b * 0.75f, 0.9f * a));
            Icons.Number(r, txt, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
        }
    }
}
