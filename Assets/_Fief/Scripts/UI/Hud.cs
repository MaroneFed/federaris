using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// Le HUD : l'horloge de la Saison, le sac, la relique, l'invite d'interaction,
    /// et des reperes vers ce qui t'appartient.
    ///
    /// PRESQUE RIEN A L'ECRAN, et c'est voulu : ce qu'on aime dans ce jeu, c'est la
    /// vue. Chaque element ici doit meriter sa place.
    ///
    /// LES REPERES. On voit ou sont TON camp et TES caches -- tu sais ou tu as
    /// enterre tes affaires, meme dans la brume. On voit le chateau, qui est le seul
    /// lieu que tout le monde connait. On ne voit PAS le mage : on le trouve a
    /// l'oreille. Un repere sur le mage tuerait la chasse.
    ///
    /// Il ne decide RIEN. Il lit l'etat du jeu et l'affiche. La souris, la pause et
    /// le verrouillage des entrees sont geres au meme endroit, dans Menus.cs.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public OrbitCamera orbitCamera;
        public PlayerInteractor interactor;
        public Camera viewCamera;
        public Menus menus;

        IPanel panel;
        bool showHelp = true;
        bool showDiagnostic;
        float helpTimer = 22f;

        public bool PanelOpen { get { return panel != null; } }

        public void OpenPanel(IPanel newPanel) { panel = newPanel; }
        public void ClosePanel() { panel = null; }

        void Update()
        {
            Toasts.Tick(Time.unscaledDeltaTime);
            FloatingTexts.Tick(Time.unscaledDeltaTime);

            if (panel != null && !panel.IsStillValid) panel = null;

            if (FiefInput.DiagnosticPressed) showDiagnostic = !showDiagnostic;

            if (FiefInput.HelpPressed)
            {
                showHelp = !showHelp;
                helpTimer = showHelp ? 99999f : 0f;
            }
            else if (helpTimer > 0f && helpTimer < 99999f)
            {
                helpTimer -= Time.deltaTime;
                if (helpTimer <= 0f) showHelp = false;
            }
        }

        bool Hidden { get { return menus != null && menus.Blocking; } }

        void OnGUI()
        {
            UiStyle.Ensure();
            if (Hidden) return;

            DrawMarkers();
            FloatingTexts.Draw(viewCamera != null ? viewCamera : Camera.main);
            DrawSeason();
            DrawPack();
            DrawPrompt();
            Toasts.Draw();
            DrawHelp();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();

            if (panel != null) panel.Draw();
        }

        // ---------------------------------------------------------------- horloge

        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        /// <summary>Huit directions, pour dire "au nord-est" plutot qu'un angle.</summary>
        public static string Direction(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float angle = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            string[] names = { "au nord", "au nord-est", "a l'est", "au sud-est",
                               "au sud", "au sud-ouest", "a l'ouest", "au nord-ouest" };
            return names[Mathf.RoundToInt(angle / 45f) % 8];
        }

        void DrawSeason()
        {
            Season season = Game.Season;
            if (season == null) return;

            float w = UiStyle.S(360);
            float x = (Screen.width - w) * 0.5f;
            float y = UiStyle.S(14);

            DrawCompass(new Rect((Screen.width - UiStyle.S(300)) * 0.5f, y, UiStyle.S(300), UiStyle.S(18)));
            y += UiStyle.S(22);

            // La cloche approche : l'horloge rougit dans les trois dernieres minutes.
            float left = season.Remaining;
            Color clock = left < 180f ? new Color(0.92f, 0.45f, 0.32f) : UiStyle.Ink;
            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(26)), Clock(left), UiStyle.Centered, clock);
            y += UiStyle.S(24);

            string mage;
            Color tint;
            if (season.MagePresent)
            {
                mage = "Le mage chante quelque part.  Il repart dans " + Clock(season.MageTimeLeft);
                tint = new Color(0.62f, 0.72f, 1f);
            }
            else if (season.NextMageIn >= 0f)
            {
                mage = "Le mage reviendra dans " + Clock(season.NextMageIn);
                tint = UiStyle.InkDim;
            }
            else
            {
                mage = "Le mage ne reviendra plus.  Pose ta relique.";
                tint = new Color(0.92f, 0.62f, 0.32f);
            }
            UiStyle.Tinted(new Rect(x - UiStyle.S(100), y, w + UiStyle.S(200), UiStyle.S(20)),
                           mage, UiStyle.CenteredSmall, tint);
        }

        /// <summary>
        /// Une bande de boussole : sans elle, "le mage chante au nord-est" ne sert a
        /// rien dans une foret sans horizon.
        /// </summary>
        void DrawCompass(Rect band)
        {
            Transform eye = viewCamera != null ? viewCamera.transform : null;
            if (eye == null) return;

            float heading = eye.eulerAngles.y;
            string[] names = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            for (int i = 0; i < 8; i++)
            {
                float delta = Mathf.DeltaAngle(heading, i * 45f);
                if (Mathf.Abs(delta) > 75f) continue;
                float px = band.center.x + delta / 75f * band.width * 0.5f;
                float alpha = 1f - Mathf.Abs(delta) / 75f;
                Color c = i == 0 ? Palette.Gold : UiStyle.InkDim;
                UiStyle.Tinted(new Rect(px - UiStyle.S(20), band.y, UiStyle.S(40), band.height),
                               names[i], UiStyle.CenteredSmall, new Color(c.r, c.g, c.b, alpha));
            }
            UiStyle.Fill(new Rect(band.center.x - 1f, band.yMax, 2f, UiStyle.S(4)),
                         new Color(1f, 1f, 1f, 0.35f));
        }

        // ---------------------------------------------------------------- le sac

        void DrawPack()
        {
            Inventory inv = Game.Inventory;
            if (inv == null) return;

            float pad = UiStyle.S(16);
            float w = UiStyle.S(270);
            float h = UiStyle.S(172);
            Rect box = new Rect(pad, Screen.height - h - pad, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float inner = w - UiStyle.S(32);
            float y = box.y + UiStyle.S(12);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "SAC", UiStyle.Head);

            GUIStyle right = UiStyle.Small;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)),
                      Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg", right);
            right.alignment = previous;

            y += UiStyle.S(28);
            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int amount = inv.Get(type);

                UiStyle.Chip(new Rect(x, y + UiStyle.S(5), UiStyle.S(12), UiStyle.S(12)), ResourceInfo.Tint(type));
                UiStyle.Tinted(new Rect(x + UiStyle.S(20), y, UiStyle.S(140), UiStyle.S(22)),
                               ResourceInfo.Name(type), UiStyle.Label,
                               amount > 0 ? UiStyle.Ink : UiStyle.InkFaint);

                right.alignment = TextAnchor.MiddleRight;
                UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(22)), amount.ToString(), right,
                               amount > 0 ? UiStyle.Ink : UiStyle.InkFaint);
                right.alignment = previous;
                y += UiStyle.S(22);
            }

            // --- la jauge de charge : c'est elle qui dit "va cacher"
            y += UiStyle.S(6);
            float load = inv.Load01;
            Color fill = Color.Lerp(new Color(0.44f, 0.78f, 0.40f),
                                    new Color(0.88f, 0.31f, 0.25f), Mathf.Pow(load, 0.85f));
            UiStyle.Bar(new Rect(x, y, inner, UiStyle.S(10)), load, fill, UiStyle.BarBg);
            y += UiStyle.S(16);

            // --- la relique
            Hoard hoard = Game.Hoard;
            string relic;
            Color tint;
            if (hoard == null || hoard.Relic == null)
            {
                relic = "Pas encore de relique";
                tint = UiStyle.InkFaint;
            }
            else
            {
                relic = "Relique  " + hoard.Relic.Power + (hoard.RelicOnStele ? "   sur la stele" : "   en main");
                tint = hoard.RelicOnStele ? new Color(0.62f, 0.78f, 0.95f) : Palette.Gold;
            }
            UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(20)), relic, UiStyle.Small, tint);
        }

        // ---------------------------------------------------------------- invite

        void DrawPrompt()
        {
            if (panel != null || interactor == null) return;

            IInteractable target = interactor.Current;
            if (target == null) return;

            float w = UiStyle.S(430);
            float h = UiStyle.S(50);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(216), w, h);

            UiStyle.DropShadow(box, UiStyle.S(14));
            GUI.Box(box, GUIContent.none, UiStyle.CardBox);

            float capW = UiStyle.S(34);
            Rect cap = new Rect(box.x + UiStyle.S(13), box.y + (h - capW) * 0.5f, capW, capW);
            UiStyle.Pill(cap);
            UiStyle.Tinted(cap, "E", UiStyle.Centered, Palette.Gold);

            GUIStyle label = UiStyle.Label;
            GUI.Label(new Rect(cap.xMax + UiStyle.S(13), box.y, box.width - capW - UiStyle.S(40), h * 0.62f),
                      target.Prompt, label);

            string hint = target.HoldDuration > 0f ? "maintenir" : "appuyer";
            GUI.Label(new Rect(cap.xMax + UiStyle.S(13), box.y + h * 0.54f,
                               box.width - capW - UiStyle.S(40), h * 0.42f), hint, UiStyle.Tiny);

            if (target.HoldDuration > 0f && interactor.HoldProgress01 > 0.001f)
            {
                Rect bar = new Rect(box.x + UiStyle.S(6), box.yMax - UiStyle.S(5),
                                    box.width - UiStyle.S(12), UiStyle.S(4));
                UiStyle.Bar(bar, interactor.HoldProgress01, Palette.Gold, new Color(0f, 0f, 0f, 0.5f));
            }
        }

        // ---------------------------------------------------------------- reperes

        void DrawMarkers()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || Game.PlayerTransform == null) return;
            Vector3 me = Game.PlayerTransform.position;

            // Le chateau : le seul lieu que tout le monde connait. On le cache quand
            // on y est -- a quoi bon un repere sur le lieu ou l'on se tient.
            Vector3 castle = Game.CastleCentre + Vector3.up * 14f;
            if (FlatDistance(me, Game.CastleCentre) > Castle.HalfSize + 20f)
                DrawMarker(cam, castle, "CHATEAU", new Color(0.92f, 0.72f, 0.42f));

            Hoard hoard = Game.Hoard;
            if (hoard == null) return;

            if (hoard.CampPlanted && FlatDistance(me, hoard.CampPosition) > 6f)
                DrawMarker(cam, hoard.CampPosition + Vector3.up * 2.2f, "CAMP", new Color(0.78f, 0.86f, 0.62f));

            for (int i = 0; i < hoard.Caches.Count; i++)
            {
                Cache cache = hoard.Caches[i];
                if (FlatDistance(me, cache.Position) < 5f) continue;
                DrawMarker(cam, cache.Position + Vector3.up * 1.2f, "CACHE " + cache.Number,
                           new Color(0.80f, 0.66f, 0.46f));
            }
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void DrawMarker(Camera cam, Vector3 world, string text, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;

            float margin = UiStyle.S(66);
            float x = Mathf.Clamp(sp.x, margin, Screen.width - margin);
            float y = Mathf.Clamp(Screen.height - sp.y, margin, Screen.height - margin);

            string label = text;
            if (Game.PlayerTransform != null)
                label += "  " + Mathf.RoundToInt(FlatDistance(Game.PlayerTransform.position, world)) + " m";

            float dotSize = UiStyle.S(8);
            UiStyle.Fill(new Rect(x - dotSize * 0.5f, y - dotSize * 0.5f, dotSize, dotSize),
                         new Color(0f, 0f, 0f, 0.5f));
            UiStyle.Fill(new Rect(x - dotSize * 0.5f + 1f, y - dotSize * 0.5f + 1f, dotSize - 2f, dotSize - 2f), color);

            GUIStyle style = UiStyle.CenteredSmall;
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(x - UiStyle.S(75) + 1f, y - UiStyle.S(27) + 1f, UiStyle.S(150), UiStyle.S(20)), label, style);
            style.normal.textColor = new Color(color.r, color.g, color.b, 0.9f);
            GUI.Label(new Rect(x - UiStyle.S(75), y - UiStyle.S(27), UiStyle.S(150), UiStyle.S(20)), label, style);
            style.normal.textColor = original;
        }

        // ---------------------------------------------------------------- diagnostic

        /// <summary>
        /// Une panne pendant la construction du monde s'affiche en grand, en rouge.
        /// Plus besoin d'aller chercher dans la Console : le jeu dit ce qui a casse.
        /// </summary>
        void DrawBuildError()
        {
            if (string.IsNullOrEmpty(Game.BuildError)) return;

            float w = Mathf.Min(Screen.width - UiStyle.S(40), UiStyle.S(760));
            float h = UiStyle.S(150);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(120), w, h);

            UiStyle.DropShadow(box, UiStyle.S(18));
            UiStyle.Fill(box, new Color(0.22f, 0.05f, 0.05f, 0.96f));
            UiStyle.Fill(new Rect(box.x, box.y, box.width, 3f), new Color(0.90f, 0.25f, 0.20f));

            float x = box.x + UiStyle.S(16);
            UiStyle.Tinted(new Rect(x, box.y + UiStyle.S(10), w, UiStyle.S(26)),
                           "LA CONSTRUCTION DU MONDE A ECHOUE", UiStyle.Head, new Color(1f, 0.55f, 0.45f));

            GUIStyle wrapped = UiStyle.Small;
            bool previousWrap = wrapped.wordWrap;
            wrapped.wordWrap = true;
            GUI.Label(new Rect(x, box.y + UiStyle.S(38), w - UiStyle.S(32), h - UiStyle.S(48)),
                      Game.BuildError, wrapped);
            wrapped.wordWrap = previousWrap;
        }

        /// <summary>
        /// Panneau F3. Il repond a la question "je suis dans quoi ?" : il liste ce
        /// dont la boite englobante contient la camera, et ce que touche un rayon
        /// tire vers l'avant. C'est ce qui remplace les allers-retours a l'aveugle.
        /// </summary>
        void DrawDiagnostic()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;

            float w = UiStyle.S(500);
            float h = UiStyle.S(356);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(90), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float y = box.y + UiStyle.S(12);
            float inner = w - UiStyle.S(32);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "DIAGNOSTIC  (F3)", UiStyle.Head);
            y += UiStyle.S(26);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            y = Line(x, y, inner, "Monde construit en", Game.BuildMilliseconds + " ms");

            // Tout le reglage de la lumiere depend de cet espace, et le depot ne le
            // versionne pas : c'est Unity qui le choisit sur chaque machine.
            y = Line(x, y, inner, "Espace colorimetrique",
                     QualitySettings.activeColorSpace == ColorSpace.Linear ? "lineaire" : "gamma");
            y = Line(x, y, inner, "Vue",
                     orbitCamera != null && orbitCamera.ThroughEyes ? "premiere personne" : "ecran-titre");

            if (Game.PlayerTransform != null)
            {
                Vector3 p = Game.PlayerTransform.position;
                y = Line(x, y, inner, "Joueur",
                         p.x.ToString("0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0"));
                y = Line(x, y, inner, "Sol sous les pieds", Ground.Sample(p.x, p.z).ToString("0.0") + " m");
            }

            if (cam != null)
            {
                Vector3 c = cam.transform.position;
                y = Line(x, y, inner, "Oeil",
                         c.x.ToString("0.0") + " / " + c.y.ToString("0.0") + " / " + c.z.ToString("0.0"));

                // --- DANS QUOI SOMMES-NOUS ?
                //
                // "Contains" ne repondait qu'a moitie : un objet peut remplir l'ecran
                // sans contenir l'oeil -- c'est le cas d'un vetement, dont l'oeil sort
                // par le col. On mesure donc aussi la DISTANCE a chaque morceau du
                // corps, et on nomme les deux plus proches. C'est cette ligne-la qui
                // dit en un mot ce qui bouche la vue.
                string inside = "rien";
                string nearest = "rien";
                if (Game.Rig != null)
                {
                    Renderer[] parts = Game.Rig.GetComponentsInChildren<Renderer>(false);

                    string firstName = null, secondName = null;
                    float firstDist = 99f, secondDist = 99f;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        // ShadowsOnly = la piece porte encore son ombre mais n'est
                        // plus dessinee : elle ne peut donc plus boucher la vue.
                        if (parts[i] == null || !parts[i].enabled) continue;
                        if (parts[i].shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;

                        if (inside == "rien" && parts[i].bounds.Contains(c))
                            inside = parts[i].gameObject.name + " (ton personnage)";

                        float d = Vector3.Distance(parts[i].bounds.ClosestPoint(c), c);
                        if (d < firstDist)
                        {
                            secondDist = firstDist; secondName = firstName;
                            firstDist = d; firstName = parts[i].gameObject.name;
                        }
                        else if (d < secondDist)
                        {
                            secondDist = d; secondName = parts[i].gameObject.name;
                        }
                    }

                    if (firstName != null)
                    {
                        nearest = firstName + " a " + Mathf.RoundToInt(firstDist * 100f) + " cm";
                        if (secondName != null)
                            nearest += ",  " + secondName + " a " + Mathf.RoundToInt(secondDist * 100f) + " cm";
                    }
                }
                y = Line(x, y, inner, "Camera a l'interieur de", inside);
                y = Line(x, y, inner, "Colle a l'oeil", nearest);

                Collider[] touching = Physics.OverlapSphere(c, 0.25f, ~0, QueryTriggerInteraction.Ignore);
                y = Line(x, y, inner, "Solides autour de l'oeil",
                         touching.Length == 0 ? "aucun" : touching[0].gameObject.name
                             + (touching.Length > 1 ? " +" + (touching.Length - 1) : ""));

                RaycastHit hit;
                string ahead = "rien a moins de 40 m";
                if (Physics.Raycast(c, cam.transform.forward, out hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                    ahead = hit.collider.gameObject.name + " a " + hit.distance.ToString("0.0") + " m";
                y = Line(x, y, inner, "Devant toi", ahead);
            }

            y += UiStyle.S(6);
            GUI.Label(new Rect(x, y, inner, UiStyle.S(34)),
                      "Lis-moi \"Colle a l'oeil\" : c'est ce qui est devant la camera.", UiStyle.Tiny);
        }

        float Line(float x, float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, width * 0.52f, UiStyle.S(20)), label, UiStyle.Small);
            UiStyle.Tinted(new Rect(x + width * 0.52f, y, width * 0.48f, UiStyle.S(20)), value,
                           UiStyle.Small, Palette.Gold);
            return y + UiStyle.S(21);
        }

        // ---------------------------------------------------------------- aide

        void DrawHelp()
        {
            if (!showHelp) return;

            float w = UiStyle.S(300);
            float h = UiStyle.S(235);
            Rect box = new Rect(Screen.width - w - UiStyle.S(16), UiStyle.S(16), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float inner = w - UiStyle.S(32);
            float y = box.y + UiStyle.S(12);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(22)), "COMMANDES", UiStyle.Head);
            GUIStyle right = UiStyle.Tiny;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(22)), "F1", right);
            right.alignment = previous;

            y += UiStyle.S(26);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            string[,] rows =
            {
                { "ZQSD", "se deplacer" },
                { "Maj", "courir" },
                { "Souris", "camera" },
                { "E", "recolter, interagir" },
                { "F3", "diagnostic" },
                { "Echap", "pause" }
            };

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                UiStyle.Tinted(new Rect(x, y, UiStyle.S(78), UiStyle.S(20)), rows[i, 0], UiStyle.Small, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(84), y, inner - UiStyle.S(84), UiStyle.S(20)), rows[i, 1], UiStyle.Small);
                y += UiStyle.S(21);
            }
        }
    }
}
