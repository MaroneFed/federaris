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

        public Seeker owner;

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

        public static Stele Of(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == s) return All[i];
            return null;
        }

        void Update()
        {
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
                                               "Elle apparait maintenant sur ta boussole.", owner.Colour);
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

        public bool CanInteract
        {
            get
            {
                Seeker me = Game.Me;
                if (me == null || owner == null) return false;
                if (Game.Season != null && Game.Season.Over) return false;
                Hoard h = me.Hoard;
                if (Mine) return h.Trophy != null || h.RelicInHand || h.RelicOnStele;
                // On ne pille pas une stele sous le nez de son proprietaire.
                if (Rival.IsGuarding(owner, transform.position)) return false;
                return owner.Hoard.RelicOnStele && owner.Hoard.Relic != null && h.Trophy == null;
            }
        }

        public string Prompt
        {
            get
            {
                Seeker me = Game.Me;
                if (me == null || owner == null) return "";
                Hoard h = me.Hoard;
                if (Mine)
                {
                    if (h.Trophy != null)
                        return "Fondre la relique de " + h.TrophyFrom.Name + " dans la tienne";
                    if (h.RelicInHand) return "Poser ta relique (puissance " + h.Relic.Power + ")";
                    if (h.RelicOnStele) return "Reprendre ta relique (puissance " + h.Relic.Power + ")";
                    return "";
                }
                return "VOLER la relique de " + owner.Name + " (puissance " + owner.Hoard.FinalScore + ")";
            }
        }

        public float HoldDuration { get { return Mine ? 1.2f : 3f; } }

        public void Interact()
        {
            Seeker me = Game.Me;
            if (me == null || owner == null) return;
            Hoard h = me.Hoard;

            if (!Mine)
            {
                if (!owner.Hoard.RelicOnStele) return;
                Relic taken = owner.Hoard.TrySurrenderRelic();
                if (taken == null || !h.TryTakeTrophy(taken, owner)) return;
                me.SyncWeight();
                owner.SyncWeight();
                Sfx.Discovery();
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("RELIQUE VOLEE", "La relique de " + owner.Name,
                                           "Puissance " + taken.Power + ". Porte-la a TA stele pour la fondre.",
                                           "Elle pese lourd. S'il te rattrape, il la reprend.", owner.Colour);
                Rival.NotifyTheft(owner, me);
                return;
            }

            if (h.Trophy != null)
            {
                string from = h.TrophyFrom != null ? h.TrophyFrom.Name : "quelqu'un";
                int gained = h.RequestAbsorbTrophy();
                if (h.RelicInHand) h.TryPlaceOnStele();
                me.SyncWeight();
                Sfx.Build();
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("LA RELIQUE DE " + from.ToUpperInvariant(), "fondue dans la tienne",
                                           "+" + gained + "  --  puissance " + h.Relic.Power, "Le vol ne rend que 60 % : le reste s'est perdu.",
                                           RuneBlue);
                return;
            }

            if (h.RelicInHand && h.TryPlaceOnStele())
            {
                me.SyncWeight();
                Sfx.Build();
                Toasts.Show("Ta relique repose sur ta stele. Elle comptera a la cloche -- si personne ne la vole.", RuneBlue);
            }
            else if (h.RelicOnStele && h.TryTakeFromStele())
            {
                me.SyncWeight();
                Sfx.Pop();
                Toasts.Show("Tu reprends ta relique. Elle ne compte plus tant qu'elle n'est pas reposee.", Palette.Gold);
            }
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
