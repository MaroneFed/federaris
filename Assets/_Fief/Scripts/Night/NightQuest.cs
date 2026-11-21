using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// FOUILLER LE CHATEAU (21/11). Le frere de Martin : "pas de pieces vides, plus de rebondissements, du style
    /// HeroQuest". Chaque salle est meublee (NightMap.Furnish) et ses coffres, tombeaux et bibliotheques se FOUILLENT :
    /// tenir E une seconde et demie, sans bouger. Ce qu'on trouve est tire au hasard a chaque partie :
    ///
    ///   la cle        celle de la chapelle (fermee a cle) : sans elle, pas de cierges, pas d'aube. Le Penitent se reveille ;
    ///   une relique   la foi remonte ;
    ///   un piege      une aiguille dans la serrure : la foi chute, et le bruit attire les Rodeurs ;
    ///   le tombeau    la dalle glisse... et quelque chose en sort (un Rodeur de plus) ;
    ///   un bruit      vide, mais quelque chose a bouge tout pres ;
    ///   rien.
    ///
    /// Et chaque salle a SON evenement, la premiere fois qu'on y entre (voir Enter).
    ///
    /// Concept Unity : une "classe" simple (Spot) qui n'est pas un MonoBehaviour : juste des donnees, rangees dans
    /// une liste. Pas besoin d'un GameObject pour se souvenir qu'un coffre est ouvert.
    /// </summary>
    public static class NightQuest
    {
        public enum Find { Nothing, Key, Relic, Trap, Tomb, Whisper, Secret, Lantern, Oil, HolyWater, Candles, Eternal }

        public class Spot
        {
            public Transform body;
            public Transform lid;
            public int kind;              // 0 coffre, 1 tombeau, 2 bibliotheque, 3 la bibliotheque du passage secret
            public char room;
            public Find find;
            public bool done;
            public float open = -1f;      // l'ouverture du couvercle, de 0 a 1 (-1 : ferme)
            public Vector3 lidPos;
            public Quaternion lidRot;
            public Light glint;
        }

        const float SearchTime = 1.5f;
        static readonly List<Spot> spots = new List<Spot>();
        static readonly HashSet<char> visited = new HashSet<char>();
        static Spot current;
        static float hold, lightning;
        static bool armorsPending;

        public static bool HasKey;
        /// <summary>L'eclair (1 : tout blanc). NightBootstrap le dessine.</summary>
        public static float Flash;

        public static void Clear()
        {
            spots.Clear(); visited.Clear();
            current = null; hold = 0f; HasKey = false; Flash = 0f; armorsPending = false; lightning = 6f;
        }

        public static void Add(Transform body, int kind, Transform lid, char room)
        {
            if (body == null) return;
            Spot s = new Spot();
            s.body = body; s.kind = kind; s.lid = lid; s.room = room;
            if (lid != null) { s.lidPos = lid.localPosition; s.lidRot = lid.localRotation; }
            spots.Add(s);
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>
        /// (21/11) Ce que contient chaque cachette. Martin : "les coffres, il y en a trop, on n'a rien dedans". Il n'y en a
        /// plus qu'un par salle, et CHACUN a quelque chose ; les tombeaux et les rayons vides ne se fouillent plus.
        ///   huile        ta torche reprend vie (objet 1)          eau benite   les monstres autour fuient (objet 2)
        ///   bougies      trois bougies a poser par terre (3)       cierge eternel  la bougie qui ne meurt jamais (4)
        ///   relique      ta torche s'use moins vite                lanterne     ta lumiere porte plus loin
        /// </summary>
        public static void Deal(Vector3 start)
        {
            Dictionary<char, Find> chest = new Dictionary<char, Find>
            {
                { 'H', Find.Oil }, { 'D', Find.Candles }, { 'A', Find.HolyWater }, { 'S', Find.Candles }, { 'P', Find.Oil },
                { 'B', Find.Oil }, { 'U', Find.Relic }, { 'W', Find.HolyWater }
            };
            bool lantern = false, eternal = false, yard = false, grave = false, book = false;
            foreach (Spot s in spots)
            {
                if (s.kind == 3) s.find = Find.Secret;
                else if (s.kind == 2 && !book) { s.find = Find.Relic; book = true; }
                else if (s.room == 'T') { s.find = lantern ? Find.Relic : Find.Lantern; lantern = true; }
                else if (s.kind == 0) { Find f; s.find = chest.TryGetValue(s.room, out f) ? f : Find.Oil; }
                else if (s.kind == 1 && s.room == 'O' && !eternal) { s.find = Find.Eternal; eternal = true; }
                else if (s.kind == 1 && s.room == 'J' && !grave) { s.find = Find.Tomb; grave = true; }
                else if (s.kind == 1 && s.room == 'Y' && !yard) { s.find = Find.Candles; yard = true; }
            }
            spots.RemoveAll(x => x.find == Find.Nothing);
            // Une lueur d'or filtre sous le couvercle des coffres : on sait qu'ils valent le detour.
            foreach (Spot s in spots)
            {
                if (s.kind != 0 || s.body == null) continue;
                Light l = new GameObject("Lueur du coffre").AddComponent<Light>();
                l.transform.SetParent(s.body, false);
                l.transform.localPosition = new Vector3(0f, 0.55f, 0.2f);
                l.color = new Color(1f, 0.72f, 0.3f);
                l.range = 1.6f;
                l.intensity = 0.7f;
                l.renderMode = LightRenderMode.ForceVertex;
                s.glint = l;
            }
            Debug.Log("[FIEF] " + spots.Count + " cachettes, toutes pleines.");
        }

        static Spot Nearest(Vector3 p)
        {
            Spot best = null;
            float bestD = float.MaxValue;
            foreach (Spot s in spots)
            {
                if (s.done || s.body == null) continue;
                float d = Flat(s.body.position - p);
                if (d < (s.kind == 1 ? 1.9f : 1.6f) && d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        public static string Hint(Vector3 p)
        {
            Spot s = Nearest(p);
            if (s == null) return null;
            return s.kind == 0 ? "E (tenir)  —  ouvrir le coffre" : s.kind == 1 ? "E (tenir)  —  faire glisser la dalle"
                : s.kind == 3 ? "E (tenir)  —  un livre dépasse du rayon" : "E (tenir)  —  un livre luit entre les autres";
        }

        /// <summary>A chaque image. Renvoie true quand une fouille se termine (ce qu'on a trouve : found, ou : where).</summary>
        public static bool Tick(float dt, Vector3 p, bool held, out Find found, out Vector3 where)
        {
            found = Find.Nothing;
            where = Vector3.zero;

            // Les couvercles qui s'ouvrent.
            foreach (Spot s in spots)
            {
                if (s.glint != null && s.done) s.glint.intensity = Mathf.MoveTowards(s.glint.intensity, 0f, dt * 0.8f);
                if (s.open < 0f || s.open >= 1f || s.lid == null) continue;
                s.open = Mathf.MoveTowards(s.open, 1f, dt / (s.kind == 1 ? 1.8f : 0.6f));
                float k = s.open * s.open * (3f - 2f * s.open);
                if (s.kind == 0) s.lid.localRotation = s.lidRot * Quaternion.Euler(-105f * k, 0f, 0f);
                else
                {
                    s.lid.localPosition = s.lidPos + s.lidRot * new Vector3(0.8f * k, 0f, 0.15f * k);
                    s.lid.localRotation = s.lidRot * Quaternion.Euler(0f, 16f * k, -4f * k);
                }
            }

            // L'orage, dans la cour : des eclairs, de temps en temps.
            Flash = Mathf.MoveTowards(Flash, 0f, dt * 2.5f);
            if (NightMap.RoomAt(p) == 'Y')
            {
                lightning -= dt;
                if (lightning <= 0f) { lightning = Random.Range(7f, 16f); Strike(p); }
            }
            if (NightMap.MoonLight != null) NightMap.MoonLight.intensity = 0.55f + 7f * Flash;

            // Les armures de la grande salle : elles se tournent toutes vers toi, au prochain clignement.
            if (armorsPending)
            {
                armorsPending = false;
                foreach (Transform a in NightMap.Armors)
                {
                    if (a == null) continue;
                    Vector3 to = p - a.position;
                    to.y = 0f;
                    if (to.magnitude > 0.5f) a.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
                }
                NightAudio.Sting(0.6f);
            }

            Spot near = Nearest(p);
            if (!held || near == null) { hold = 0f; current = null; return false; }
            if (near != current) { current = near; hold = 0f; }
            hold += dt;
            if (hold < SearchTime) return false;

            near.done = true;
            near.open = 0f;
            if (near.glint != null) near.glint.intensity = 2.2f;
            hold = 0f;
            current = null;
            found = near.find;
            where = near.body.position;
            if (found == Find.Key) HasKey = true;
            try
            {
                if (near.kind == 0) NightAudio.Chest(where);
                else if (near.kind == 1) NightAudio.Bang(where, 0.9f);
                else NightAudio.Book(where);
            }
            catch (System.Exception) { }
            return true;
        }

        public static void Strike(Vector3 me)
        {
            Flash = 1f;
            NightAudio.Thunder(me + new Vector3(12f, 30f, 8f));
        }

        /// <summary>Le joueur entre dans une salle. La premiere fois, il s'y passe quelque chose. Renvoie une phrase, ou null.</summary>
        public static string Enter(char room, Vector3 me)
        {
            if (room == '.' || !visited.Add(room)) return null;
            switch (room)
            {
                case 'H':
                    armorsPending = true;
                    NightAudio.Bang(NightMap.Center('H') + new Vector3(6f, 2f, 4f), 0.9f);
                    return null;
                case 'L':
                    NightAudio.Bang(NightMap.Center('L') + new Vector3(5f, 1.5f, 4f), 1f);
                    NightDirector.Summon(NightMap.Center('L') + new Vector3(5.5f, 0f, 4.5f), me);
                    return "Un livre est tombé. Tout seul.";
                case 'D':
                    NightPlayer.TorchOut = 2.8f;
                    NightAudio.Bang(me + Vector3.up, 1f);
                    return "Ta torche s'étouffe...";
                case 'Y':
                    Strike(me);
                    lightning = 5f;
                    return null;
                case 'P':
                    try { NightAudio.Chain(NightMap.Center('P') + Vector3.up); } catch (System.Exception) { }
                    return "Quelqu'un a gratté ces murs. De l'intérieur.";
                case 'Q':
                    try { NightAudio.Bang(NightMap.Center('Q'), 0.6f); } catch (System.Exception) { }
                    return "Une marmite roule sur les dalles. Le feu brûle encore.";
                case 'T':
                    try { NightAudio.Treasure(NightMap.Center('T')); } catch (System.Exception) { }
                    return "De la cire, de l'or, et la poussière de cent ans.";
                case 'A':
                    try { NightAudio.Chain(NightMap.Center('A') + Vector3.up); } catch (System.Exception) { }
                    return "Une lame est tombée du râtelier.";
                case 'J':
                    Strike(me);
                    lightning = 6f;
                    return "La grande cour. Des yeux, entre les tombes.";
                case 'B':
                    NightAudio.Snort();
                    return "Ça sent la paille et la peur. Un cheval tremble dans sa stalle.";
                case 'G':
                    try { NightAudio.Chain(NightMap.Center('G') + Vector3.up); } catch (System.Exception) { }
                    return "Le donjon. Un escalier monte dans le noir.";
                case 'U':
                    return "Quelqu'un respire, tout près. Doucement.";
                case 'W':
                    NightPlayer.TorchOut = 1.5f;
                    return "Les caves. L'air est froid, ta flamme se couche.";
                case 'O':
                    NightAudio.Sting(0.7f);
                    return "Des os, partout. Et quelque chose qui dort au milieu.";
            }
            return null;
        }

        /// <summary>La barre de la fouille, sous le centre de l'ecran.</summary>
        public static void Draw(float w, float h)
        {
            if (current == null || hold <= 0f) return;
            float bw = Mathf.Round(w * 0.16f), bh = Mathf.Max(3f, Mathf.Round(h * 0.006f));
            Rect r = new Rect(Mathf.Round(w * 0.5f - bw * 0.5f), Mathf.Round(h * 0.63f), bw, bh);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.78f, 0.32f, 0.95f);
            GUI.DrawTexture(new Rect(r.x, r.y, Mathf.Round(bw * Mathf.Clamp01(hold / SearchTime)), bh), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
