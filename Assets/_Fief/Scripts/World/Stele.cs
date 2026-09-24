using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE STELE A SOI. Chaque chercheur (toi, chaque rival) plante la sienne, une
    /// fois, ou il veut dans la sylve. A la cloche, seule compte la relique POSEE
    /// sur sa propre stele.
    ///
    /// Et c'est la toute la tension : une stele se voit. Sa banniere, sa rune, la
    /// relique qui flotte au-dessus et luit. Qui la trouve peut VOLER ce qu'il y a
    /// dessus. Il faut donc choisir : la planter pres de tout (pratique, exposee) ou
    /// au fond d'un fourre (sure, loin de tout).
    ///
    /// Ce que fait E devant une stele depend de QUI la regarde :
    ///   - la tienne : poser ta relique, la reprendre, ou y fondre une relique volee ;
    ///   - celle d'un rival : voler la relique posee dessus (E maintenu 3 s).
    /// </summary>
    public class Stele : MonoBehaviour, IInteractable
    {
        public static readonly List<Stele> All = new List<Stele>();

        [System.NonSerialized] public Seeker owner;

        Light glow;
        Transform relicShown;
        int shownTier = -1;
        Transform[] rings = new Transform[0];
        Renderer rune;
        Material runeOff;
        Material runeOn;
        bool announcedToPlayer;
        AudioSource hum;

        static readonly Color StoneBlack = new Color(0.14f, 0.14f, 0.15f);
        static readonly Color DaisStone = new Color(0.27f, 0.27f, 0.26f);
        static readonly Color Pole = new Color(0.22f, 0.17f, 0.12f);
        public static readonly Color RuneBlue = new Color(0.55f, 0.72f, 1f);

        const float RelicHeight = 1.85f;

        /// <summary>
        /// Une stele DISCRETE (Martin, 24/09 : "quand meme un minimum cachee" -- et
        /// en multijoueur, une stele enorme n'aurait aucun sens) : une pierre
        /// levee d'un metre quarante, moussue, sans banniere. On ne la voit qu'a
        /// quelques pas, dans la brume. Mais elle CHANTE : un bourdonnement tres
        /// doux, qu'on entend a quinze metres. On peut la trouver a l'oreille --
        /// toi, et les autres.
        /// </summary>
        public static Stele Build(Transform parent, Vector3 at, float yaw, Seeker owner)
        {
            GameObject root = new GameObject("STELE de " + owner.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = root.transform;

            Proto.Cube(t, new Vector3(0f, 0.62f, 0f), new Vector3(0.62f, 1.44f, 0.32f), StoneBlack, "Pierre");
            Stele stele = root.AddComponent<Stele>();
            stele.owner = owner;

            Proto.BeginVisualOnly();
            GameObject top = Proto.Cube(t, new Vector3(0.04f, 1.38f, 0f), new Vector3(0.5f, 0.16f, 0.3f), StoneBlack, "Tete");
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            Proto.Cube(t, new Vector3(-0.2f, 0.25f, -0.17f), new Vector3(0.3f, 0.5f, 0.04f), new Color(0.2f, 0.26f, 0.16f), "Mousse");
            // Une rune de la couleur de son proprietaire : eteinte, elle se confond avec la pierre.
            GameObject runeGo = Proto.Cube(t, new Vector3(0f, 0.9f, -0.17f), new Vector3(0.2f, 0.34f, 0.02f), owner.Colour, "Rune");
            // Trois pierres plates a ses pieds.
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + 0.4f;
                GameObject s = Proto.Cube(t, new Vector3(Mathf.Cos(a) * 0.6f, 0.04f, Mathf.Sin(a) * 0.6f), new Vector3(0.36f, 0.08f, 0.28f),
                                          DaisStone, "Pierre plate");
                s.transform.localRotation = Quaternion.Euler(0f, i * 50f, 0f);
            }
            Proto.EndVisualOnly();

            GameObject relic = new GameObject("Relique");
            relic.transform.SetParent(t, false);
            relic.transform.localPosition = new Vector3(0f, RelicHeight, 0f);
            relic.transform.localScale = Vector3.one * 0.55f;
            stele.relicShown = relic.transform;
            relic.SetActive(false);

            stele.rune = runeGo.GetComponent<Renderer>();
            stele.runeOff = MaterialFactory.Get(Palette.Shade(owner.Colour, 0.4f));
            stele.runeOn = MaterialFactory.GetGlow(owner.Colour, 1.6f);
            if (stele.rune != null) stele.rune.sharedMaterial = stele.runeOff;

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.8f, -0.6f);
            stele.glow = lightGo.AddComponent<Light>();
            stele.glow.type = LightType.Point;
            stele.glow.color = RuneBlue;
            stele.glow.range = 3f;
            stele.glow.intensity = 0f;
            stele.glow.shadows = LightShadows.None;

            // Le chant : un son en boucle, spatialise, qui s'eteint a 15 m.
            stele.hum = root.AddComponent<AudioSource>();
            stele.hum.clip = Sfx.SteleHum();
            stele.hum.loop = true;
            stele.hum.spatialBlend = 1f;
            stele.hum.rolloffMode = AudioRolloffMode.Linear;
            stele.hum.minDistance = 1.5f;
            stele.hum.maxDistance = 15f;
            stele.hum.dopplerLevel = 0f;
            stele.hum.volume = 0.35f;
            stele.hum.Play();

            All.Add(stele);
            return stele;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>Ce chercheur est-il a moins de "metres" de sa propre stele ?</summary>
        public static bool NearOwn(Seeker s, float metres)
        {
            if (s == null || s.Body == null || !s.Hoard.StelePlanted) return false;
            Vector3 d = s.Body.position - s.Hoard.StelePosition;
            d.y = 0f;
            return d.magnitude < metres;
        }

        public static Stele Of(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == s) return All[i];
            return null;
        }

        float sentinelCooldown;

        /// <summary>
        /// LA SENTINELLE (amelioration) : si un rival rode a moins de 12 m de TA
        /// stele pendant que tu es loin, elle sonne -- et te dit ou elle est par
        /// rapport a toi. Une fois toutes les 40 secondes, pas plus.
        /// </summary>
        void Watch()
        {
            if (sentinelCooldown > 0f) { sentinelCooldown -= Time.deltaTime; return; }
            Seeker me = Game.Me;
            if (me == null || owner != me || me.Hoard.Level(UpgradeKind.Sentinelle) <= 0 || me.Body == null) return;
            if (Flat(me.Body.position - transform.position).magnitude < 25f) return;
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r == null || !r.seeker.Alive) continue;
                if (Flat(r.transform.position - transform.position).magnitude > 12f) continue;
                sentinelCooldown = 40f;
                Sfx.Alarm();
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("SENTINELLE", r.seeker.Name + " rode a ta stele",
                                           "Elle est " + Hud.Direction(me.Body.position, transform.position) + ".",
                                           "Il vient pour ta reserve. Ou pour ta relique.", r.seeker.Colour);
                return;
            }
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        void Update()
        {
            Watch();
            pileTimer -= Time.deltaTime;
            if (pileTimer <= 0f) { pileTimer = 1f; RefreshPiles(); }
            Hoard h = owner != null ? owner.Hoard : null;
            bool lit = h != null && h.RelicOnStele && h.Relic != null;

            // Decouverte : passer a moins de 18 m d'une stele rivale, c'est la connaitre.
            Seeker me = Game.Me;
            if (me != null && owner != me && me.Body != null && !me.Knows(owner))
            {
                Vector3 d = me.Body.position - transform.position;
                d.y = 0f;
                if (d.magnitude < 8f)
                {
                    me.Discover(owner);
                    if (!announcedToPlayer && Game.Hud != null)
                    {
                        announcedToPlayer = true;
                        Sfx.Discovery();
                        Game.Hud.ShowDiscovery("TU AS TROUVE", "La stele de " + owner.Name,
                                               lit ? "Une relique y flotte. Puissance " + h.FinalScore + "." : "Rien dessus. Pour l'instant.",
                                               "Retiens le chemin : rien ne te la montrera.", owner.Colour);
                    }
                }
            }

            if (rune != null)
            {
                Material want = lit ? runeOn : runeOff;
                if (rune.sharedMaterial != want) rune.sharedMaterial = want;
            }

            int tier = lit ? Relic.Tier(h.FinalScore) : 0;
            if (tier != shownTier) ShowTier(tier);

            if (relicShown != null)
            {
                if (relicShown.gameObject.activeSelf != lit) relicShown.gameObject.SetActive(lit);
                if (lit)
                {
                    relicShown.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
                    Vector3 p = relicShown.localPosition;
                    p.y = RelicHeight + Mathf.Sin(Time.time * 1.3f) * 0.06f;
                    relicShown.localPosition = p;
                    for (int i = 0; i < rings.Length; i++)
                        rings[i].Rotate(new Vector3(i == 1 ? 60f : 0f, i == 2 ? 50f : 0f, i == 0 ? 70f : 25f) * Time.deltaTime, Space.Self);
                }
            }
            if (glow != null)
            {
                glow.intensity = Mathf.MoveTowards(glow.intensity, lit ? 0.6f + tier * 0.15f : 0f, Time.deltaTime * 2f);
                glow.range = 3f + tier * 0.4f;
            }
        }

        // ------------------------------------------------------------------ la relique posee

        /// <summary>
        /// La relique posee change d'allure avec sa puissance. Babiole : un cube.
        /// Fetiche : un joyau et un anneau. Relique : trois eclats en orbite. Tresor :
        /// deux anneaux d'or. Legende : trois anneaux, huit eclats. Petite, mais elle
        /// luit : de pres, on sait tout de suite ce qu'elle vaut.
        /// </summary>
        void ShowTier(int tier)
        {
            shownTier = tier;
            if (relicShown == null) return;
            for (int i = relicShown.childCount - 1; i >= 0; i--) Destroy(relicShown.GetChild(i).gameObject);
            RelicModels.Build(relicShown, tier, out rings);

            // Le chant monte avec la relique posee : une legende s'entend de plus loin.
            if (hum != null)
            {
                hum.volume = tier > 0 ? 0.45f + tier * 0.06f : 0.3f;
                hum.maxDistance = 15f + tier * 2f;
            }
        }

        // ------------------------------------------------------------------ IInteractable

        public Transform Anchor { get { return transform; } }

        bool Mine { get { return owner != null && owner == Game.Me; } }

        /// <summary>Ce qu'il y a a prendre sur la stele d'un autre : sa relique posee, sa reserve.</summary>
        bool HasLoot
        {
            get
            {
                Hoard o = owner.Hoard;
                bool relic = o.RelicOnStele && o.Relic != null && Game.Me.Hoard.Trophy == null;
                bool store = o.Store != null && !o.Store.Contents.IsEmpty;
                return relic || store;
            }
        }

        public bool CanInteract
        {
            get
            {
                Seeker me = Game.Me;
                if (me == null || owner == null) return false;
                if (Game.Season != null && Game.Season.Over) return false;
                if (Mine) return true;
                // On ne pille pas une stele sous le nez de son proprietaire.
                if (Rival.IsGuarding(owner, transform.position)) return false;
                return HasLoot;
            }
        }

        public string Prompt
        {
            get
            {
                Seeker me = Game.Me;
                if (me == null || owner == null) return "";
                if (Mine)
                {
                    Hoard h = me.Hoard;
                    if (h.Trophy != null) return "Ta stele  --  fondre la relique de " + h.TrophyFrom.Name;
                    string sack = me.Bag.IsEmpty ? "" : "deposer ton sac, ";
                    if (h.RelicInHand) return "Ta stele  --  " + sack + "poser ta relique";
                    return "Ta stele  --  " + sack + "reserve : " + StoreSummary(h);
                }
                Hoard o = owner.Hoard;
                string what = o.RelicOnStele && o.Relic != null ? "sa relique (" + o.FinalScore + ")" : "";
                if (o.Store != null && !o.Store.Contents.IsEmpty)
                    what += (what.Length > 0 ? " et " : "") + "sa reserve (" + o.Store.Contents.TotalUnits + ")";
                return "PILLER la stele de " + owner.Name + " : " + what;
            }
        }

        public float HoldDuration { get { return Mine ? 0f : 3f; } }

        public void Interact()
        {
            Seeker me = Game.Me;
            if (me == null || owner == null) return;

            if (Mine)
            {
                // D'abord, ce qu'on est venu faire neuf fois sur dix : vider son sac.
                int stored = me.Hoard.RequestStoreAll(me.Bag);
                me.SyncWeight();
                if (stored > 0)
                {
                    Sfx.Stash();
                    Toasts.Show("Depose a ta stele : " + stored + " ressources. Reserve : " + StoreSummary(me.Hoard) + ".", Palette.Gold);
                    RefreshPiles();
                }
                // Une relique volee ou la sienne en main : l'onglet Relique d'abord.
                int first = me.Hoard.Trophy != null || me.Hoard.RelicInHand ? 1 : 0;
                if (Game.Hud != null) Game.Hud.OpenPanel(new StelePanel(this, first));
                Sfx.Pop();
                return;
            }
            Loot(me);
        }

        /// <summary>"34 bois, 6 lune, 2 fer" -- ou "vide".</summary>
        public static string StoreSummary(Hoard h)
        {
            if (h == null || h.Store == null || h.Store.Contents.IsEmpty) return "vide";
            Inventory c = h.Store.Contents;
            string s = "";
            string[] shortNames = { "bois", "lune", "fer" };
            for (int i = 0; i < ResourceInfo.Count; i++)
            {
                int n = c.Get((ResourceType)i);
                if (n <= 0) continue;
                if (s.Length > 0) s += ", ";
                s += n + " " + (i < shortNames.Length ? shortNames[i] : ResourceInfo.Name((ResourceType)i));
            }
            return s;
        }

        // ------------------------------------------------------------------ la reserve, visible

        // Ce qui dort dans la reserve SE VOIT autour de la pierre : un tas de bois,
        // des cristaux, des lingots. On sait d'un coup d'oeil ou on en est -- et
        // les pillards aussi. Une stele pleine, ca attire.
        Transform piles;
        int[] shownPiles = new int[3];
        float pileTimer;

        void RefreshPiles()
        {
            Hoard h = owner != null ? owner.Hoard : null;
            if (h == null || h.Store == null) return;
            Inventory c = h.Store.Contents;
            int wood = Mathf.Min(12, (c.Get(ResourceType.Deadwood) + 3) / 4);
            int moon = Mathf.Min(9, (c.Get(ResourceType.Moonstone) + 1) / 2);
            int iron = Mathf.Min(10, (c.Get(ResourceType.Iron) + 1) / 2);
            if (wood == shownPiles[0] && moon == shownPiles[1] && iron == shownPiles[2]) return;
            shownPiles[0] = wood; shownPiles[1] = moon; shownPiles[2] = iron;

            if (piles != null) Destroy(piles.gameObject);
            GameObject go = new GameObject("Reserve");
            go.transform.SetParent(transform, false);
            piles = go.transform;
            Proto.BeginVisualOnly();
            // Le bois : des buches empilees en pyramide, a gauche de la pierre.
            Color[] barks = { new Color(0.42f, 0.34f, 0.24f), new Color(0.34f, 0.27f, 0.19f), new Color(0.5f, 0.44f, 0.36f) };
            for (int i = 0; i < wood; i++)
            {
                int row = i < 5 ? 0 : i < 9 ? 1 : 2;
                int inRow = row == 0 ? i : row == 1 ? i - 5 : i - 9;
                float x = -1.1f + (inRow - (row == 0 ? 2f : row == 1 ? 1.5f : 1f)) * 0.15f;
                GameObject log = Proto.Cylinder(piles, new Vector3(x, 0.07f + row * 0.13f, 0.5f), new Vector3(0.14f, 0.36f, 0.14f), barks[i % 3], "Buche");
                log.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // La pierre-lune : des cristaux plantes a droite, qui luisent.
            Material glow = MaterialFactory.GetGlow(new Color(0.62f, 0.8f, 1f), 1.4f);
            for (int i = 0; i < moon; i++)
            {
                float a = i * 1.1f;
                GameObject shard = Proto.Cone(piles, new Vector3(0.95f + Mathf.Cos(a) * 0.22f, 0f, 0.4f + Mathf.Sin(a) * 0.22f),
                                              0.05f, 0.22f + (i % 3) * 0.06f, new Color(0.62f, 0.8f, 1f), "Cristal", 6);
                shard.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 18f, a * 40f, Mathf.Cos(a) * 18f);
                shard.GetComponent<Renderer>().sharedMaterial = glow;
            }
            // Le fer : des lingots croises, derriere.
            Color ingot = ResourceInfo.Tint(ResourceType.Iron);
            for (int i = 0; i < iron; i++)
            {
                int layer = i / 3;
                GameObject bar = Proto.Cube(piles, new Vector3((i % 3 - 1) * 0.13f, 0.04f + layer * 0.07f, -0.6f),
                                            new Vector3(0.1f, 0.06f, 0.3f), ingot, "Lingot");
                bar.transform.localRotation = Quaternion.Euler(0f, layer % 2 == 0 ? 0f : 90f, 0f);
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Piller la stele d'un autre : sa relique posee (en trophee), et sa reserve.</summary>
        void Loot(Seeker me)
        {
            Hoard h = me.Hoard;
            Hoard o = owner.Hoard;
            string relicLine = "";
            if (o.RelicOnStele && o.Relic != null && h.Trophy == null)
            {
                Relic taken = o.TrySurrenderRelic();
                if (taken != null && h.TryTakeTrophy(taken, owner))
                    relicLine = "Sa relique (puissance " + taken.Power + ") : porte-la a TA stele pour la fondre.";
            }
            int units = o.RequestLoot(me.Bag);
            me.SyncWeight();
            owner.SyncWeight();
            if (relicLine.Length == 0 && units == 0)
            {
                Sfx.Deny();
                Toasts.Show("Ton sac est plein : tu ne peux rien emporter.", UiStyle.InkDim);
                return;
            }
            Sfx.Discovery();
            if (Game.Hud != null)
                Game.Hud.ShowDiscovery("STELE PILLEE", "celle de " + owner.Name,
                                       relicLine.Length > 0 ? relicLine : units + " ressources emportees de sa reserve.",
                                       relicLine.Length > 0 && units > 0 ? "Et " + units + " ressources de sa reserve." : "Il saura que c'est toi.",
                                       owner.Colour);
            Rival.NotifyTheft(owner, me);
        }

        // ------------------------------------------------------------------ les gestes, partages
        //
        // Le panneau de la stele (StelePanel) appelle ces trois-la. Ils ne font que
        // demander a Hoard, puis le dire.

        public static void PlaceRelic(Seeker me)
        {
            if (me == null || !me.Hoard.TryPlaceOnStele()) return;
            me.SyncWeight();
            Sfx.Build();
            Toasts.Show("Ta relique repose sur ta stele. Elle comptera a la cloche -- si personne ne la vole.", RuneBlue);
        }

        public static void TakeRelic(Seeker me)
        {
            if (me == null || !me.Hoard.TryTakeFromStele()) return;
            me.SyncWeight();
            Sfx.Pop();
            Toasts.Show("Tu reprends ta relique. Elle ne compte plus tant qu'elle n'est pas reposee.", Palette.Gold);
        }

        public static void AbsorbTrophy(Seeker me)
        {
            Hoard h = me != null ? me.Hoard : null;
            if (h == null || h.Trophy == null) return;
            string from = h.TrophyFrom != null ? h.TrophyFrom.Name : "quelqu'un";
            int gained = h.RequestAbsorbTrophy();
            if (h.RelicInHand) h.TryPlaceOnStele();
            me.SyncWeight();
            Sfx.Build();
            if (Game.Hud != null)
                Game.Hud.ShowDiscovery("LA RELIQUE DE " + from.ToUpperInvariant(), "fondue dans la tienne",
                                       "+" + gained + "  --  puissance " + h.Relic.Power, "Le vol ne rend que 60 % : le reste s'est perdu.",
                                       RuneBlue);
        }
    }

    /// <summary>
    /// Les allures de la relique selon son palier. Partage par les steles et le
    /// Registre du chateau.
    /// </summary>
    public static class RelicModels
    {
        public static void Build(Transform parent, int tier, out Transform[] rings)
        {
            Color blue = Stele.RuneBlue;
            Color accent = tier >= 4 ? new Color(0.95f, 0.78f, 0.35f) : blue;
            Material core = MaterialFactory.GetGlow(blue, 2.4f + tier * 0.3f);
            Material gold = MaterialFactory.GetGlow(accent, 2.2f);

            Proto.BeginVisualOnly();
            if (tier <= 1)
            {
                GameObject cube = Proto.Cube(parent, Vector3.zero, Vector3.one * 0.38f, blue, "Babiole");
                cube.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                cube.GetComponent<Renderer>().sharedMaterial = core;
                rings = new Transform[0];
                Proto.EndVisualOnly();
                return;
            }

            float size = 0.28f + tier * 0.09f;
            GameObject top = Proto.Cone(parent, Vector3.zero, size, size * 1.2f, blue, "Joyau", 6);
            GameObject bottom = Proto.Cone(parent, Vector3.zero, size, size * 1.5f, blue, "Joyau", 6);
            bottom.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            top.GetComponent<Renderer>().sharedMaterial = core;
            bottom.GetComponent<Renderer>().sharedMaterial = core;

            int ringCount = tier <= 3 ? 1 : tier == 4 ? 2 : 3;
            rings = new Transform[ringCount];
            for (int r = 0; r < ringCount; r++)
            {
                GameObject ring = new GameObject("Anneau");
                ring.transform.SetParent(parent, false);
                ring.transform.localRotation = Quaternion.Euler(r * 60f, r * 40f, 0f);
                float radius = size * 2.1f + r * 0.16f;
                for (int k = 0; k < 14; k++)
                {
                    float a = k / 14f * Mathf.PI * 2f;
                    GameObject seg = Proto.Cube(ring.transform, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius),
                                                new Vector3(0.05f, 0.05f, radius * 0.47f), accent, "Maillon");
                    seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                    seg.GetComponent<Renderer>().sharedMaterial = gold;
                }
                rings[r] = ring.transform;
            }

            int shards = tier == 3 ? 3 : tier == 4 ? 6 : tier >= 5 ? 8 : 0;
            for (int k = 0; k < shards; k++)
            {
                float a = k / (float)shards * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2f) * 0.3f, Mathf.Sin(a)) * (size * 3.2f);
                GameObject shard = Proto.Cone(parent, p, 0.07f, 0.26f, accent, "Eclat", 4);
                shard.transform.localRotation = Quaternion.Euler(k * 37f, k * 53f, 0f);
                shard.GetComponent<Renderer>().sharedMaterial = gold;
            }
            Proto.EndVisualOnly();
        }
    }
}
