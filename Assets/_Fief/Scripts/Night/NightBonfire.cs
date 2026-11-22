using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// (22/11) LES FEUX DE VEILLE. Martin : "le jeu est impossible, il faudrait des checkpoints, des gros feux comme dans
    /// Elden Ring ; quand il y a des gros feux, c'est que t'es good ; la premiere piece doit avoir un gros feu ; les pieces
    /// avec des gros feux, ca peut pas passer".
    ///
    /// Un grand brasero cercle de pierres, un par grande salle (le vestibule, la grande salle, les cuisines, la grande
    /// cour, le donjon et son etage, les caves, la chapelle). Celui du vestibule brule des le debut. Les autres, on les
    /// RANIME avec sa torche (E). Un feu de veille allume :
    ///   - AUCUN monstre n'entre dans sa salle (NightMap.Passable, NightWard.Hold) : la salle est un refuge ;
    ///   - ta torche s'y remplit toute seule ;
    ///   - c'est la que tu te releves quand un monstre t'a eu (le dernier feu ranime).
    ///
    /// Concept Unity : une liste "static" de tous les feux ; n'importe quel script peut demander "cette salle est-elle sure ?".
    /// </summary>
    public class NightBonfire : MonoBehaviour
    {
        public static readonly List<NightBonfire> All = new List<NightBonfire>();
        static readonly HashSet<char> safe = new HashSet<char>();

        public char Room;
        public bool Lit;
        GameObject fire;
        Light glow, ember;
        float pulse;

        public static void Clear() { All.Clear(); safe.Clear(); }

        /// <summary>Une salle ou brule un feu de veille : les monstres n'y entrent pas.</summary>
        public static bool Safe(char room) { return safe.Contains(room); }

        public static void Build(Transform world)
        {
            Clear();
            Place(world, 'V', new Vector3(22f, 0f, 6.2f), true);
            Place(world, 'H', NightMap.Center('H'), false);
            Place(world, 'Q', NightMap.Center('Q') + new Vector3(3f, 0f, -3f), false);
            Place(world, 'J', NightMap.Center('J') + new Vector3(-6f, 0f, 2f), false);
            Place(world, 'G', NightMap.Center('G') + new Vector3(0f, 0f, 2f), false);
            Place(world, 'U', NightMap.Center('U') + new Vector3(-2f, 0f, -4f), false);
            Place(world, 'W', NightMap.Center('W') + new Vector3(0f, 0f, 4.5f), false);
            Place(world, 'K', NightMap.Center('K') + new Vector3(0f, 0f, 0.5f), false);
        }

        static void Place(Transform world, char room, Vector3 at, bool lit)
        {
            at.y = NightMap.FloorY(at);
            GameObject go = new GameObject("Feu de veille : " + NightMap.NameOf(room));
            go.transform.SetParent(world, false);
            go.transform.position = at;
            NightBonfire b = go.AddComponent<NightBonfire>();
            b.Room = room;
            // Le brasero du kit, deux fois plus grand, et son feu.
            NightKit.FireAtY("Brasero_Feu", at.x, at.z, 0f, false, at.y);
            b.fire = NightKit.LastFire;
            if (b.fire != null)
            {
                Transform body = b.fire.transform.parent;
                if (body != null) { body.localScale = Vector3.one * 1.8f; body.position = at; }
                // Les flammes (des particules) ne grandissent avec le brasero que si elles suivent l'echelle de leurs parents.
                foreach (ParticleSystem ps in b.fire.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule m = ps.main;
                    m.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                b.fire.transform.position += Vector3.down * 0.45f;      // les flammes DANS la vasque, pas au-dessus
                b.fire.SetActive(false);
            }
            // Un cercle de pierres plates autour, comme un lieu ancien.
            Material stone = NightKit.Mat("Pierre");
            Proto.BeginVisualOnly();
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                GameObject s = Proto.Cube(go.transform, new Vector3(Mathf.Cos(a) * 1.7f, 0.07f, Mathf.Sin(a) * 1.7f), new Vector3(0.6f, 0.14f, 0.38f), Color.gray, "Pierre du cercle");
                s.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f + Random.Range(-12f, 12f), Random.Range(-4f, 4f));
                if (stone != null) s.GetComponent<Renderer>().sharedMaterial = stone;
            }
            Proto.EndVisualOnly();
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.9f, 0f);
            box.size = new Vector3(1.4f, 1.8f, 1.4f);
            // Une grande lumiere chaude (allume), une braise rouge qui couve (eteint : on le voit de loin).
            b.glow = new GameObject("Lumière du feu").AddComponent<Light>();
            b.glow.transform.SetParent(go.transform, false);
            b.glow.transform.localPosition = new Vector3(0f, 1.5f, 0.6f);
            b.glow.type = LightType.Point;
            b.glow.color = new Color(1f, 0.66f, 0.32f);
            b.glow.range = 14f;
            b.glow.intensity = 0f;
            b.glow.shadows = LightShadows.Soft;
            b.ember = new GameObject("Braise").AddComponent<Light>();
            b.ember.transform.SetParent(go.transform, false);
            b.ember.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            b.ember.type = LightType.Point;
            b.ember.color = new Color(1f, 0.25f, 0.06f);
            b.ember.range = 3f;
            b.ember.intensity = 0.8f;
            All.Add(b);
            if (lit) b.Ignite(true);
        }

        void OnDestroy() { All.Remove(this); }

        public void Ignite(bool quiet)
        {
            if (Lit) return;
            Lit = true;
            safe.Add(Room);
            if (fire != null) fire.SetActive(true);
            if (ember != null) ember.enabled = false;
            if (!quiet)
            {
                NightAudio.Fire(transform.position + Vector3.up);
                NightAudio.Treasure(transform.position + Vector3.up);
                Shown = 4f;
            }
        }

        /// <summary>Le grand titre "FEU DE VEILLE" (secondes restantes), comme les lieux de grace.</summary>
        public static float Shown;

        public static NightBonfire Near(Vector3 p, float r)
        {
            foreach (NightBonfire b in All)
            {
                if (b == null) continue;
                Vector3 d = b.transform.position - p;
                if (Mathf.Abs(d.y) < 2f && new Vector2(d.x, d.z).magnitude < r) return b;
            }
            return null;
        }

        void Update()
        {
            if (!Lit || glow == null) return;
            pulse += Time.deltaTime;
            glow.intensity = 1.15f + 0.2f * Mathf.PerlinNoise(pulse * 3f, transform.position.x) + 0.1f * Mathf.Sin(pulse * 11f);
        }

        /// <summary>Le titre, au centre de l'ecran, en or, qui apparait et s'efface.</summary>
        public static void Draw(float w, float h, GUIStyle text)
        {
            if (Shown <= 0f) return;
            if (Event.current.type == EventType.Repaint) Shown = Mathf.MoveTowards(Shown, 0f, Time.unscaledDeltaTime);
            float a = Mathf.Clamp01((4f - Shown) / 0.6f) * Mathf.Clamp01(Shown / 1.2f);
            GUI.color = new Color(0f, 0f, 0f, 0.45f * a);
            GUI.DrawTexture(new Rect(0f, h * 0.36f, w, h * 0.16f), Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.7f, 0.3f, 0.8f * a);
            GUI.DrawTexture(new Rect(w * 0.3f, h * 0.36f, w * 0.4f, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(w * 0.3f, h * 0.52f, w * 0.4f, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUIStyle t = new GUIStyle(text);
            t.font = NightFont.Title();
            t.fontSize = Mathf.RoundToInt(h * 0.07f);
            t.normal.textColor = new Color(1f, 0.82f, 0.45f, a);
            NightFont.Label(new Rect(0f, h * 0.37f, w, h * 0.1f), "Feu de veille", t);
            GUIStyle s = new GUIStyle(text);
            s.fontSize = Mathf.RoundToInt(h * 0.022f);
            s.fontStyle = FontStyle.Italic;
            s.normal.textColor = new Color(0.9f, 0.82f, 0.68f, a);
            NightFont.Label(new Rect(0f, h * 0.46f, w, h * 0.05f), "Aucun monstre n'entrera ici. C'est ici que tu te relèveras.", s);
        }
    }
}
