using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// (22/11) LES PASSAGES SECRETS, et leurs mecanismes. Martin : "plus de passages secrets, beaucoup plus, et des
    /// compliques". Huit en tout, et presque aucun ne s'ouvre d'une simple touche :
    ///
    ///   la crypte        (chapelle)      les quatre cierges allumes : le mur derriere l'autel s'enfonce ;
    ///   le tresor        (bibliotheque)  un livre depasse d'un rayon : la bibliotheque pivote ;
    ///   le garde-manger  (cuisines)      trois anneaux de fer au mur sud, a tirer dans le BON ORDRE. Mathilde le connait :
    ///                                    "d'abord la flamme, puis la lune, puis l'os". Une erreur : tout se reverrouille,
    ///                                    dans un grand bruit de chaines... et le Boucher entend ;
    ///   la sortie        (garde-manger)  un levier, de l'interieur, vers les cachots ;
    ///   le passage       (bibliotheque)  trois bougeoirs, aux trois coins : il faut qu'ils brulent TOUS EN MEME TEMPS
    ///                                    (les bougies ne durent qu'une minute : il faut courir) ;
    ///   la sortie        (passage)       un levier, de l'interieur, vers le dortoir ;
    ///   la cave cachee   (caves)         une bouteille, dans un ratelier, ne bouge pas comme les autres ;
    ///   la tombe creuse  (grande cour)   quand les DEUX braseros de la cour brulent, une dalle glisse : un escalier
    ///                                    descend jusqu'a l'ossuaire.
    ///
    /// Concept Unity : une "enigme" n'est que quelques nombres qu'on retient (l'ordre attendu, ou on en est) et qu'on
    /// compare a ce que fait le joueur. Le decor (les anneaux, les dalles) ne fait que le montrer.
    /// </summary>
    public static class NightSecrets
    {
        class Lever
        {
            public string name;
            public Transform handle;
            public Vector3 at;
            public float pull;        // l'animation : de 0 (au repos) a 1 (tire)
            public bool down;
        }

        static readonly List<Lever> rings = new List<Lever>();
        static Lever zLever;
        static Lever xLever;
        static readonly string[] Order = { "la flamme", "la lune", "l'os" };
        static int progress;
        static readonly List<NightWard> libraryCandles = new List<NightWard>();
        static bool trapOpen;
        static float trapAnim;
        static Vector3 trapLid0;
        /// <summary>Une phrase a dire au joueur ("" : rien).</summary>
        public static string Message = "";
        /// <summary>La tombe creuse : ou elle te descend (sinon null).</summary>
        public static bool Descend;
        public static Vector3 DescendTo;

        public static int Opened { get { return NightMap.SecretsOpen + (trapOpen ? 1 : 0); } }
        public static int Total { get { return NightMap.SecretsTotal + 1; } }

        /// <summary>Ce que Mathilde dit des anneaux (dans son dialogue).</summary>
        public static string KitchenClue { get { return "Mon homme cachait les réserves de l'hiver derrière le mur du sud. Il tirait les anneaux : d'abord " + Order[0] + ", puis " + Order[1] + ", puis " + Order[2] + "."; } }

        public static void Build(Transform world)
        {
            rings.Clear(); libraryCandles.Clear();
            progress = 0; trapOpen = false; trapAnim = 0f; Message = ""; Descend = false;
            Material iron = NightKit.Mat("Fer");
            // Les trois anneaux, au mur sud des cuisines (de part et d'autre du passage), dans le desordre.
            float x0, z0, x1, z1;
            NightMap.Bounds('Q', out x0, out z0, out x1, out z1);
            float[] xs = { x0 + 1.6f, x0 + 6f, x1 - 1.8f };
            string[] names = { Order[2], Order[0], Order[1] };
            for (int i = 0; i < 3; i++) rings.Add(MakeLever(world, new Vector3(xs[i], 0f, z0 + 0.28f), 0f, names[i], iron, true));
            // Les leviers de l'interieur.
            NightMap.Bounds('Z', out x0, out z0, out x1, out z1);
            zLever = MakeLever(world, new Vector3(x0 + 2.4f, 0f, z0 + 0.28f), 0f, "le levier", iron, false);
            NightMap.Bounds('X', out x0, out z0, out x1, out z1);
            xLever = MakeLever(world, new Vector3((x0 + x1) * 0.5f, 0f, z1 - 0.28f), 180f, "le levier", iron, false);
            // Les trois bougeoirs de la bibliotheque, aux coins.
            NightMap.Bounds('L', out x0, out z0, out x1, out z1);
            Vector3[] spots = { new Vector3(x0 + 1.4f, 0f, z1 - 1.4f), new Vector3(x1 - 1.4f, 0f, z1 - 1.4f), new Vector3(x0 + 1.4f, 0f, z0 + 1.4f) };
            foreach (Vector3 p in spots) libraryCandles.Add(NightLight.Candles(null, p, 60f, false));
            if (NightMap.TrapTomb != null)
            {
                Transform lid = NightMap.TrapTomb.Find("tombeau_couvercle");
                if (lid != null) trapLid0 = lid.localPosition;
            }
        }

        static Lever MakeLever(Transform world, Vector3 at, float yaw, string name, Material iron, bool ring)
        {
            Lever l = new Lever();
            l.name = name;
            at.y = NightMap.FloorY(at);
            l.at = at;
            GameObject root = new GameObject(ring ? "Anneau : " + name : "Levier");
            root.transform.SetParent(world, false);
            root.transform.position = at + Vector3.up * 1.4f;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Proto.BeginVisualOnly();
            GameObject plate = Proto.Cube(root.transform, Vector3.zero, new Vector3(0.36f, 0.36f, 0.05f), Color.gray, "Platine");
            if (iron != null) plate.GetComponent<Renderer>().sharedMaterial = iron;
            Transform pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 0f, 0.05f);
            if (ring)
            {
                // Un anneau de fer (huit maillons) qui pend, et le symbole grave au-dessus.
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f;
                    GameObject link = Proto.Cube(pivot, new Vector3(Mathf.Cos(a) * 0.12f, -0.2f + Mathf.Sin(a) * 0.12f, 0.03f), new Vector3(0.05f, 0.1f, 0.04f), Color.gray, "Maillon");
                    link.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
                    if (iron != null) link.GetComponent<Renderer>().sharedMaterial = iron;
                }
                Color glyph = name == Order[0] ? new Color(1f, 0.45f, 0.1f) : name == Order[1] ? new Color(0.6f, 0.75f, 1f) : new Color(0.85f, 0.82f, 0.72f);
                GameObject sign = Proto.Sphere(root.transform, new Vector3(0f, 0.32f, 0.03f), new Vector3(0.1f, 0.1f, 0.03f), glyph, "Symbole");
                sign.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(glyph, 0.6f);
            }
            else
            {
                GameObject bar = Proto.Cube(pivot, new Vector3(0f, 0.18f, 0.05f), new Vector3(0.06f, 0.42f, 0.06f), Color.gray, "Manche");
                if (iron != null) bar.GetComponent<Renderer>().sharedMaterial = iron;
                GameObject knob = Proto.Sphere(pivot, new Vector3(0f, 0.4f, 0.05f), Vector3.one * 0.09f, new Color(0.2f, 0.1f, 0.05f), "Pommeau");
                knob.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(new Color(0.2f, 0.1f, 0.05f), 0.3f, 0f);
            }
            Proto.EndVisualOnly();
            l.handle = pivot;
            return l;
        }

        static bool Near(Vector3 me, Vector3 p, float r) { return Mathf.Abs(me.y - p.y) < 2f && new Vector2(me.x - p.x, me.z - p.z).magnitude < r; }

        static Lever NearLever(Vector3 me)
        {
            foreach (Lever l in rings) if (Near(me, l.at, 1.7f)) return l;
            if (zLever != null && !NightMap.Revealed("PZ") && Near(me, zLever.at, 1.7f)) return zLever;
            if (xLever != null && !NightMap.Revealed("DX") && Near(me, xLever.at, 1.7f)) return xLever;
            return null;
        }

        public static string Hint(Vector3 me)
        {
            Lever l = NearLever(me);
            if (l != null)
            {
                if (rings.Contains(l)) return NightMap.Revealed("QZ") ? null : "E  —  tirer l'anneau de " + l.name;
                return "E  —  abaisser le levier";
            }
            if (trapOpen && NightMap.TrapTomb != null && Near(me, NightMap.TrapTomb.position, 2f)) return "E  —  descendre l'escalier sous la tombe";
            return null;
        }

        /// <summary>E vient d'etre enfonce. Renvoie true si un mecanisme a repondu.</summary>
        public static bool Interact(Vector3 me)
        {
            Lever l = NearLever(me);
            if (l != null)
            {
                l.pull = 0.001f; l.down = true;
                NightAudio.Chain(l.at + Vector3.up * 1.4f);
                if (l == zLever) { NightMap.Reveal("PZ"); Message = "Un mur s'enfonce : les cachots, de l'autre côté."; return true; }
                if (l == xLever) { NightMap.Reveal("DX"); Message = "Le mur pivote sur le dortoir."; return true; }
                if (NightMap.Revealed("QZ")) return true;
                if (l.name == Order[progress])
                {
                    progress++;
                    NightAudio.Lock(l.at + Vector3.up * 1.4f);
                    if (progress >= Order.Length)
                    {
                        NightMap.Reveal("QZ");
                        NightAudio.Bang(l.at + Vector3.up, 0.9f);
                        Message = "Un déclic sourd, derrière le mur du sud. Les pierres s'enfoncent...";
                    }
                    else Message = "Un cliquetis, dans le mur.";
                }
                else
                {
                    // Une erreur : tout se reverrouille, dans un fracas de chaines. Et ca s'entend loin.
                    progress = 0;
                    foreach (Lever r in rings) r.down = false;
                    NightAudio.Chain(l.at + Vector3.up);
                    NightAudio.Bang(l.at + Vector3.up, 1f);
                    Beast.Hear(l.at);
                    Prowler.Hear(l.at);
                    Message = "Les anneaux retombent avec fracas. Ça s'est entendu.";
                }
                return true;
            }
            if (trapOpen && NightMap.TrapTomb != null && Near(me, NightMap.TrapTomb.position, 2f))
            {
                Descend = true;
                DescendTo = NightMap.BeastLair + new Vector3(3.2f, 0.1f, -3.5f);
                Message = "Des marches, sous la dalle, qui s'enfoncent dans le noir... jusqu'à l'ossuaire.";
                return true;
            }
            return false;
        }

        public static void Tick(float dt)
        {
            // Les anneaux et les leviers : ils descendent quand on les tire, remontent quand on les relache.
            foreach (Lever l in rings) Animate(l, dt);
            if (zLever != null) Animate(zLever, dt);
            if (xLever != null) Animate(xLever, dt);

            // Les trois bougeoirs de la bibliotheque : tous allumes en meme temps.
            if (!NightMap.Revealed("LX") && libraryCandles.Count == 3)
            {
                bool all = true;
                foreach (NightWard w in libraryCandles) if (w == null || !w.Burning) all = false;
                if (all)
                {
                    NightMap.Reveal("LX");
                    NightAudio.Bang(NightMap.Center('L') + Vector3.up * 2f, 1f);
                    Message = "Les trois flammes se couchent vers l'est... un pan de mur glisse, entre les rayons.";
                }
            }

            // La tombe creuse : les deux braseros de la cour brulent.
            if (!trapOpen && NightMap.TrapTomb != null)
            {
                int burning = 0;
                foreach (NightWard w in NightWard.All)
                    if (w != null && w.kind == NightWard.Kind.Brazier && w.Burning && NightMap.RoomAt(w.Center) == 'J') burning++;
                if (burning >= 2)
                {
                    trapOpen = true;
                    NightAudio.Bang(NightMap.TrapTomb.position + Vector3.up, 1f);
                    Message = "Dans la cour, une dalle de tombe a glissé toute seule.";
                }
            }
            if (trapOpen && trapAnim < 1f && NightMap.TrapTomb != null)
            {
                trapAnim = Mathf.MoveTowards(trapAnim, 1f, dt / 2.5f);
                Transform lid = NightMap.TrapTomb.Find("tombeau_couvercle");
                if (lid != null) lid.localPosition = trapLid0 + new Vector3(1.6f * trapAnim, 0.1f * Mathf.Sin(trapAnim * Mathf.PI), 0f);
            }
        }

        static void Animate(Lever l, float dt)
        {
            if (l.handle == null) return;
            l.pull = Mathf.MoveTowards(l.pull, l.down ? 1f : 0f, dt * 4f);
            l.handle.localRotation = Quaternion.Euler(-70f * l.pull, 0f, 0f);
        }
    }
}
