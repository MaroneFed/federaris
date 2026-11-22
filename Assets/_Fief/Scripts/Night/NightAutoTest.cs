using System.Collections;
using System.IO;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// (21/11) LE TEST AUTOMATIQUE. Claude ne peut pas tenir la souris : quand le jeu est lance avec "-autotest", il se
    /// joue tout seul, ecran par ecran, et prend de VRAIES captures (l'interface comprise, que les photos de l'editeur ne
    /// voient pas) dans Logs/AutoTest, a cote du dossier Builds. Puis il se ferme.
    ///   FIEF-La-Nuit.exe -autotest -screen-fullscreen 0 -screen-width 1600 -screen-height 900
    ///
    /// Concept Unity : une coroutine (IEnumerator + yield) est une fonction qui s'arrete et reprend plus tard :
    /// "yield return new WaitForSecondsRealtime(2)" attend deux secondes, meme quand le jeu est en pause.
    /// </summary>
    public class NightAutoTest : MonoBehaviour
    {
        string dir;
        int n;

        public static bool Wanted { get { return System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autotest") >= 0; } }

        void Start()
        {
            dir = Path.Combine(Directory.GetParent(Application.dataPath).Parent.Parent.FullName, "Logs", "AutoTest");
            Directory.CreateDirectory(dir);
            foreach (string old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
            StartCoroutine(Run());
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            n++;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, n.ToString("00") + "_" + name + ".png"));
            Debug.Log("[FIEF] autotest : " + name);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("menu");
            NightUI.HelpOpen = true;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("commandes");
            NightUI.HelpOpen = false;

            // L'arrivee a cheval : quatre moments, dont la porte qui s'ouvre.
            NightUI.MenuOpen = false;
            NightUI.StartRequested = true;
            float[] at = { 4f, 11f, 18f, 21.5f, 24f };
            while (!NightIntro.Playing) yield return null;
            float t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < at.Length; i++)
            {
                while (Time.realtimeSinceStartup - t0 < at[i] && NightIntro.Playing) yield return null;
                if (!NightIntro.Playing) break;
                yield return Shot("cheval_" + at[i].ToString("0"));
            }
            while (NightIntro.Playing) yield return null;

            // L'ouverture : le Veilleur.
            while (!NightCinema.Playing) yield return null;
            yield return new WaitForSecondsRealtime(4f);
            yield return Shot("veilleur");
            yield return new WaitForSecondsRealtime(9f);
            yield return Shot("veilleur_torche");
            NightCinema.Skip();
            yield return new WaitForSecondsRealtime(3f);

            // L'ecran de jeu : quete, carte, bougie, objets.
            Journal.Oil = 2; Journal.HolyWater = 1; Journal.Candles = 3;
            yield return Shot("jeu");
            NightPlayer p = Object.FindAnyObjectByType<NightPlayer>();
            if (p != null) { p.Face(180f, 0f); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("jeu_porte_entree"); p.Face(0f, 0f); }
            NightUI.BookOpen = true;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("carnet");
            NightUI.BookOpen = false;
            NightUI.PauseOpen = true;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("pause");
            NightUI.PauseOpen = false;
            yield return new WaitForSecondsRealtime(0.5f);
            NightGames.StartLock(p != null ? p.transform.position : Vector3.zero, null);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("serrure");
            NightGames.Abort();
            NightTalk.Start("Mathilde, la cuisinière", new[] { "Ne me fais pas de mal... Oh. Un vivant. Un vrai." }, null);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("dialogue");
            NightTalk.Open = false;
            // (22/11) Les nouveautes, sur place : les anneaux, le passage derobe, un monstre de pres, l'Abbe.
            if (p != null)
            {
                float x0, z0, x1, z1;
                NightMap.Bounds('Q', out x0, out z0, out x1, out z1);
                p.Teleport(new Vector3(x0 + 4f, 0.1f, z0 + 3.5f), 180f);
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("cuisines_anneaux");
                NightMap.Reveal("LX");
                NightMap.Bounds('L', out x0, out z0, out x1, out z1);
                p.Teleport(new Vector3(x1 - 3f, 0.1f, (z0 + z1) * 0.5f), 90f);
                yield return new WaitForSecondsRealtime(3.5f);
                yield return Shot("passage_derobe");
                Beast near = null;
                foreach (Beast b in Beast.All) if (b != null && b.kind == Beast.Kind.Demon) near = b;
                if (near != null)
                {
                    near.Held = true;
                    Vector3 spot = p.transform.position + p.transform.forward * 3.2f;
                    spot.y = NightMap.FloorY(spot);
                    near.transform.position = spot;
                    near.transform.rotation = Quaternion.LookRotation(-p.transform.forward, Vector3.up);
                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return Shot("demon_de_pres");
                    near.transform.position = NightMap.Center('H');
                }
                Vector3 r = NightMap.Center('R');
                p.Teleport(NightMap.CryptEntry + Vector3.up * 0.1f, 45f);
                Abbot ab = Abbot.Spawn(null, r + new Vector3(2f, 0f, 3f), p);
                yield return new WaitForSecondsRealtime(1.5f);
                p.Face(Quaternion.LookRotation(ab.transform.position - p.transform.position).eulerAngles.y, -12f);
                ab.Active = false;
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("abbe");
                Object.Destroy(ab.gameObject);
                p.Teleport(NightMap.Center('V') + Vector3.up * 0.1f, 0f);
                yield return new WaitForSecondsRealtime(1f);
            }
            Faith.Value = 0.15f;
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("torche_faible");
            Faith.Value = 1f;
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }
    }
}
