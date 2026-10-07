using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES ALERTES DES CAPACITES (12/10, v36 -- Martin : "les capas, on ne comprend rien a ce qui
    /// se passe ; quand tu dis qu'il y a une comete qui vient, il faut que ce soit vraiment
    /// intuitif, soit UI, soit vraiment tres intuitif"). Deux choses, et seulement deux :
    ///
    ///   L'ANNONCE  quand un autre lance une grosse capacite (une divine, ou une des plus folles) :
    ///              un bandeau en haut -- l'icone de la capacite, SON pseudo a sa couleur, et le
    ///              nom de la capacite en or. "GOTAGA  COMETE !" On sait qui, et quoi.
    ///   LE DANGER  quand TU es dans une cible au sol (DivineFx.Mark) : les bords de l'ecran
    ///              battent a la couleur du coup, et au milieu, en haut, l'icone de ce qui arrive
    ///              et les secondes qui restent, en gros. Un "bip" doux en y entrant. On court.
    ///
    /// Comme les cris (Shouts), c'est une exception assumee au "zero texte" : demandee par Martin.
    /// </summary>
    public static class Warnings
    {
        static Seeker castBy;
        static Ability castWhat;
        static float castAt = -99f;
        const float CastLife = 2.4f;

        /// <summary>Les capacites qui meritent l'annonce : les divines et les plus folles.</summary>
        public static bool Worth(Ability a)
        {
            return AbilityInfo.IsActive(a) && (AbilityInfo.IsGod(a) || AbilityInfo.InGodPool(a) || AbilityInfo.IsBig(a));
        }

        /// <summary>"by" vient de lancer "a" (AbilityCaster.Cast).</summary>
        public static void Announce(Seeker by, Ability a)
        {
            if (by == null || by.IsPlayer || by.Body == null || !Worth(a)) return;
            Camera cam = Camera.main;
            if (cam != null && (cam.transform.position - by.Body.position).magnitude > 170f) return;
            castBy = by;
            castWhat = a;
            castAt = Time.unscaledTime;
        }

        public static void Clear() { castBy = null; castAt = -99f; wasInDanger = false; }

        static bool wasInDanger;

        public static void Draw()
        {
            DrawDanger();
            DrawCast();
            DrawSurge();
            CastFeel.Draw();
        }

        // (v38) LA MONTEE : a chaque palier (x1,5, x2, x2,5), un bandeau en or et un son.
        static int surgeTier;
        static float surgeAt = -99f;
        const float SurgeLife = 2.6f;

        static void DrawSurge()
        {
            float p = Combat.Power;
            int tier = Mathf.FloorToInt((p - 1f) / 0.5f + 0.001f);
            if (tier < surgeTier) surgeTier = tier;          // nouvelle manche : on repart d'en bas
            if (tier > surgeTier)
            {
                surgeTier = tier;
                surgeAt = Time.unscaledTime;
                Sfx.Discovery();
                if (Game.Hud != null) Game.Hud.Flash(new Color(1f, 0.6f, 0.15f, 0.25f));
            }
            float age = Time.unscaledTime - surgeAt;
            if (age > SurgeLife) return;
            float a = Mathf.Clamp01(age / 0.12f) * Mathf.Clamp01((SurgeLife - age) / 0.4f);
            int fs = Mathf.RoundToInt(UiStyle.S(34));
            float h = Mathf.Round(fs * 1.7f);
            float mult = 1f + surgeTier * 0.5f;
            string what = "PUISSANCE x" + (mult % 1f == 0f ? mult.ToString("0") : mult.ToString("0.0").Replace('.', ',')) + " !";
            float icon = Mathf.Round(h * 0.86f);
            float gap = Mathf.Round(fs * 0.4f);
            float ww = Icons.Width(what, fs);
            float total = icon + gap + ww;
            float pad = Mathf.Round(fs * 0.6f);
            float x = Mathf.Round((Screen.width - total) * 0.5f);
            float y = Mathf.Round(Screen.height * 0.16f);
            Color hot = new Color(1f, 0.55f, 0.15f);
            Icons.Pill(new Rect(x - pad, y, total + pad * 2f, h), new Color(0.12f, 0.05f, 0.04f, 0.65f * a));
            Rect ir = new Rect(x, y + Mathf.Round((h - icon) * 0.5f), icon, icon);
            Icons.Pill(ir, new Color(hot.r, hot.g, hot.b, a));
            Icons.Draw(new Rect(ir.x + icon * 0.16f, ir.y + icon * 0.16f, icon * 0.68f, icon * 0.68f), Icons.Of(Ability.Foudre), new Color(1f, 1f, 1f, a));
            Icons.Text(new Rect(x + icon + gap, y, ww, h), what, fs, new Color(1f, 0.84f, 0.36f, a), TextAnchor.MiddleLeft, true);
        }

        static void DrawCast()
        {
            float age = Time.unscaledTime - castAt;
            if (castBy == null || age > CastLife) return;
            float a = Mathf.Clamp01(age / 0.12f) * Mathf.Clamp01((CastLife - age) / 0.4f);
            int fs = Mathf.RoundToInt(UiStyle.S(30));
            float h = Mathf.Round(fs * 1.75f);
            string who = castBy.Name.ToUpperInvariant();
            string what = AbilityInfo.Name(castWhat).ToUpperInvariant() + " !";
            float gap = Mathf.Round(fs * 0.4f);
            float icon = Mathf.Round(h * 0.86f);
            float pad = Mathf.Round(fs * 0.5f);
            float wn = Icons.Width(who, fs) + pad * 2f;
            float ww = Icons.Width(what, fs);
            float total = icon + gap + wn + gap + ww;
            float x = Mathf.Round((Screen.width - total) * 0.5f);
            float y = Mathf.Round(Screen.height * 0.075f);
            // Un petit "pop" a l'arrivee : la pastille grossit d'un coup puis se pose (redessinee, pas zoomee).
            Color tint = AbilityInfo.Tint(castWhat);
            Icons.Pill(new Rect(x - pad, y, total + pad * 2f, h), new Color(0.06f, 0.05f, 0.12f, 0.6f * a));
            Rect ir = new Rect(x, y + Mathf.Round((h - icon) * 0.5f), icon, icon);
            Icons.Pill(ir, new Color(tint.r * 0.8f, tint.g * 0.8f, tint.b * 0.8f, a));
            Icons.Draw(new Rect(ir.x + icon * 0.16f, ir.y + icon * 0.16f, icon * 0.68f, icon * 0.68f), Icons.Of(castWhat), new Color(1f, 1f, 1f, a));
            x += icon + gap;
            Color nc = castBy.Colour;
            Rect pill = new Rect(x, y + Mathf.Round(h * 0.12f), wn, Mathf.Round(h * 0.76f));
            Icons.Pill(pill, new Color(nc.r * 0.75f, nc.g * 0.75f, nc.b * 0.75f, a));
            Icons.Text(pill, who, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, true);
            x += wn + gap;
            Icons.Text(new Rect(x, y, ww, h), what, fs, new Color(1f, 0.84f, 0.36f, a), TextAnchor.MiddleLeft, true);
        }

        static void DrawDanger()
        {
            Seeker me = Game.Me;
            if (me == null || me.Body == null) { wasInDanger = false; return; }
            Vector3 p = me.Body.position;
            TargetMark worst = null;
            float soonest = float.MaxValue;
            for (int i = 0; i < TargetMark.All.Count; i++)
            {
                TargetMark m = TargetMark.All[i];
                if (m == null || m.Harmless(me)) continue;
                Vector3 c = m.transform.position;
                if (Mathf.Abs(c.y - p.y) > 14f) continue;
                if (new Vector2(c.x - p.x, c.z - p.z).magnitude > m.Radius + 0.6f) continue;
                if (m.Remaining < soonest) { soonest = m.Remaining; worst = m; }
            }
            bool inDanger = worst != null;
            if (inDanger && !wasInDanger) Sfx.Warn();
            wasInDanger = inDanger;
            if (!inDanger) return;

            float w = Screen.width, h = Screen.height;
            Color col = Color.Lerp(worst.Colour, new Color(1f, 0.2f, 0.15f), 0.35f);
            // Les bords battent de plus en plus vite a l'approche.
            float speed = Mathf.Lerp(5f, 16f, Mathf.Clamp01(1f - soonest / 3f));
            float beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
            float edge = Mathf.Round(UiStyle.S(26f));
            for (int k = 0; k < 4; k++)
            {
                float f = 1f - k / 4f;
                float al = (0.18f + 0.22f * beat) * f;
                float d = edge * k / 4f;
                float t = edge / 4f;
                Color cc = new Color(col.r, col.g, col.b, al);
                UiStyle.Fill(new Rect(0f, d, w, t), cc);
                UiStyle.Fill(new Rect(0f, h - d - t, w, t), cc);
                UiStyle.Fill(new Rect(d, 0f, t, h), cc);
                UiStyle.Fill(new Rect(w - d - t, 0f, t, h), cc);
            }
            // Au milieu, en haut : l'icone de ce qui arrive, et les secondes qui restent.
            float big = Mathf.Round(UiStyle.S(84f));
            float y = Mathf.Round(h * 0.2f);
            float x = Mathf.Round(w * 0.5f - big * 0.5f);
            float pop = 1f + 0.08f * beat;
            float b2 = Mathf.Round(big * pop);
            Rect r = new Rect(Mathf.Round(x - (b2 - big) * 0.5f), Mathf.Round(y - (b2 - big) * 0.5f), b2, b2);
            Icons.Pill(r, new Color(col.r * 0.85f, col.g * 0.85f, col.b * 0.85f, 0.95f));
            Icons.Draw(new Rect(r.x + b2 * 0.17f, r.y + b2 * 0.17f, b2 * 0.66f, b2 * 0.66f), worst.Icon, Color.white);
            int secs = Mathf.Max(1, Mathf.CeilToInt(soonest));
            int fs = Mathf.RoundToInt(UiStyle.S(40));
            Icons.Number(new Rect(r.xMax + UiStyle.S(10), r.y, big, b2), secs.ToString(), fs, Color.white, TextAnchor.MiddleLeft);
            Icons.Number(new Rect(r.x - big - UiStyle.S(10), r.y, big, b2), "!", fs, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleRight);
        }
    }
}
