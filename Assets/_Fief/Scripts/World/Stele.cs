using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE STELE A SOI : ton coffre-fort, et ton score.
    ///
    /// Chaque chercheur a la sienne, fixe, tiree au hasard ; on nait a cote. Tout le
    /// butin (★) qu'on y DEPOSE compte a la cloche. Et ca se voit : un tas d'or
    /// grandit a son pied. Un tas d'or, ca attire.
    ///
    /// Ce que fait E devant une stele :
    ///   - la tienne : deposer tout ton butin (et tes pierres-lune, ★2 chacune) ;
    ///   - celle d'un autre, quand il n'est pas a cote : la PILLER (E maintenu 3 s),
    ///     et emporter la moitie de son or.
    /// </summary>
    public class Stele : MonoBehaviour, IInteractable
    {
        public static readonly List<Stele> All = new List<Stele>();

        [System.NonSerialized] public Seeker owner;

        Light glow;
        Renderer rune;
        Material runeOff;
        Material runeOn;
        bool announcedToPlayer;
        AudioSource hum;

        static readonly Color StoneBlack = new Color(0.14f, 0.14f, 0.15f);
        static readonly Color DaisStone = new Color(0.27f, 0.27f, 0.26f);
        public static readonly Color RuneBlue = new Color(0.55f, 0.72f, 1f);

        /// <summary>
        /// Une pierre levee d'un metre quarante, moussue, sans banniere. On ne la voit
        /// qu'a quelques pas, dans la brume. Mais elle CHANTE : un bourdonnement tres
        /// doux, qu'on entend a quinze metres. Et son or luit.
        /// </summary>
        public static Stele Build(Transform parent, Vector3 at, float yaw, Seeker owner)
        {
            GameObject root = new GameObject("STÈLE de " + owner.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = root.transform;

            Proto.Cube(t, new Vector3(0f, 0.62f, 0f), new Vector3(0.62f, 1.44f, 0.32f), StoneBlack, "Pierre");
            Stele stele = root.AddComponent<Stele>();
            stele.owner = owner;

            Proto.BeginVisualOnly();
            GameObject top = Proto.Cube(t, new Vector3(0.04f, 1.38f, 0f), new Vector3(0.5f, 0.16f, 0.3f), StoneBlack, "Tête");
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            Proto.Cube(t, new Vector3(-0.2f, 0.25f, -0.17f), new Vector3(0.3f, 0.5f, 0.04f), new Color(0.2f, 0.26f, 0.16f), "Mousse");
            // Une rune de la couleur de son proprietaire : elle s'allume des qu'il y a de l'or.
            GameObject runeGo = Proto.Cube(t, new Vector3(0f, 0.9f, -0.17f), new Vector3(0.2f, 0.34f, 0.02f), owner.Colour, "Rune");
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + 0.4f;
                GameObject s = Proto.Cube(t, new Vector3(Mathf.Cos(a) * 0.6f, 0.04f, Mathf.Sin(a) * 0.6f), new Vector3(0.36f, 0.08f, 0.28f),
                                          DaisStone, "Pierre plate");
                s.transform.localRotation = Quaternion.Euler(0f, i * 50f, 0f);
            }
            Proto.EndVisualOnly();

            stele.rune = runeGo.GetComponent<Renderer>();
            stele.runeOff = MaterialFactory.Get(Palette.Shade(owner.Colour, 0.4f));
            stele.runeOn = MaterialFactory.GetGlow(owner.Colour, 1.6f);
            if (stele.rune != null) stele.rune.sharedMaterial = stele.runeOff;

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.8f, -0.9f);
            stele.glow = lightGo.AddComponent<Light>();
            stele.glow.type = LightType.Point;
            stele.glow.color = new Color(1f, 0.8f, 0.45f);
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

        /// <summary>Le proprietaire se tient a moins de 9 m : on ne pille pas sous son nez.</summary>
        public bool Guarded
        {
            get
            {
                if (owner == null || owner.Body == null || !owner.Alive) return false;
                Vector3 d = owner.Body.position - transform.position;
                d.y = 0f;
                return d.magnitude < 9f;
            }
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        // LE FIL D'OR : au-dessus de TA stele, un mince fil de lumiere doree que toi
        // seul vois, au-dessus des arbres. Il s'efface quand on est tout pres, et
        // brille plus fort quand on porte du butin.
        LightBeam thread;

        void Thread()
        {
            if (!Mine || Game.PlayerTransform == null) return;
            if (thread == null)
            {
                thread = LightBeam.Build(transform, transform.position, new Color(1f, 0.82f, 0.45f), 0.7f, 46f);
                if (thread == null) return;
                thread.fadeSpeed = 0.8f;
            }
            float far = Flat(Game.PlayerTransform.position - transform.position).magnitude;
            bool carrying = owner.Hoard.Carried > 0;
            thread.targetAlpha = far < 20f ? 0f : carrying ? 0.5f : 0.22f;
            thread.source = transform.position;
        }

        void Update()
        {
            Thread();
            pileTimer -= Time.deltaTime;
            if (pileTimer <= 0f) { pileTimer = 0.5f; RefreshPile(); }
            Hoard h = owner != null ? owner.Hoard : null;
            bool lit = h != null && h.Banked > 0;

            // Decouverte : passer a moins de 8 m d'une stele rivale, c'est la connaitre.
            Seeker me = Game.Me;
            if (me != null && owner != me && me.Body != null && !me.Knows(owner))
            {
                if (Flat(me.Body.position - transform.position).magnitude < 8f)
                {
                    me.Discover(owner);
                    if (!announcedToPlayer && Game.Hud != null)
                    {
                        announcedToPlayer = true;
                        Sfx.Discovery();
                        Game.Hud.ShowDiscovery("", "Stèle de " + owner.Name, "★" + h.Banked, "", owner.Colour);
                    }
                }
            }

            if (rune != null)
            {
                Material want = lit ? runeOn : runeOff;
                if (rune.sharedMaterial != want) rune.sharedMaterial = want;
            }
            if (glow != null)
            {
                float tier = h != null ? Mathf.Clamp01(h.Banked / 150f) : 0f;
                glow.intensity = Mathf.MoveTowards(glow.intensity, lit ? 0.5f + tier : 0f, Time.deltaTime * 2f);
                glow.range = 3f + tier * 3f;
            }
            if (hum != null && h != null) hum.maxDistance = 15f + Mathf.Min(15f, h.Banked / 10f);
        }

        // ------------------------------------------------------------------ le tas d'or

        // Ce qui dort dans la stele SE VOIT : un tas de pieces qui grandit a son pied,
        // avec des coupes et des coffrets. On sait d'un coup d'oeil ou on en est --
        // et les pillards aussi.
        Transform pile;
        int shownPile = -1;
        float pileTimer;

        void RefreshPile()
        {
            Hoard h = owner != null ? owner.Hoard : null;
            if (h == null) return;
            int stacks = Mathf.Min(24, (h.Banked + 7) / 8);
            if (stacks == shownPile) return;
            shownPile = stacks;

            if (pile != null) Destroy(pile.gameObject);
            GameObject go = new GameObject("Or");
            go.transform.SetParent(transform, false);
            pile = go.transform;
            Proto.BeginVisualOnly();
            Material gold = MaterialFactory.GetGlow(new Color(0.95f, 0.76f, 0.3f), 1.1f);
            System.Random rng = new System.Random(owner.Name.Length * 7);
            for (int i = 0; i < stacks; i++)
            {
                // Des piles de pieces en demi-cercle devant la pierre, de plus en plus hautes.
                float a = Mathf.PI * (0.15f + 0.7f * (i % 8) / 7f);
                float r = 0.75f + (i / 8) * 0.28f;
                int coins = 2 + rng.Next(4);
                for (int k = 0; k < coins; k++)
                {
                    GameObject coin = Proto.Cylinder(pile, new Vector3(Mathf.Cos(a) * r, 0.02f + k * 0.035f, -Mathf.Sin(a) * r),
                                                     new Vector3(0.16f, 0.016f, 0.16f), Color.white, "Pièces");
                    coin.transform.localRotation = Quaternion.Euler(rng.Next(6), rng.Next(360), rng.Next(6));
                    coin.GetComponent<Renderer>().sharedMaterial = gold;
                }
            }
            Proto.EndVisualOnly();
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
                if (Mine) return me.Hoard.Carried > 0 || me.Bag.Get(ResourceType.Moonstone) > 0;
                return owner.Hoard.Banked > 0 && !Guarded;
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
                    int stars = me.Hoard.Carried + me.Bag.Get(ResourceType.Moonstone) * Hoard.MoonstoneStars;
                    return "Déposer  ★" + stars;
                }
                return "Piller " + owner.Name + "  ★" + Mathf.CeilToInt(owner.Hoard.Banked * Hoard.PillageShare);
            }
        }

        public float HoldDuration { get { return Mine ? 0f : 3f; } }

        public void Interact()
        {
            Seeker me = Game.Me;
            if (me == null || owner == null) return;

            if (Mine)
            {
                int stars = me.Hoard.RequestBank(me.Bag);
                me.SyncWeight();
                if (stars <= 0) return;
                Sfx.Stash();
                Sfx.Coin();
                FloatingTexts.Spawn(transform.position + Vector3.up * 1.9f, "+★" + stars, Palette.Gold);
                // Ca doit se SENTIR : une gerbe d'or, un coup sourd, le classement qui brille.
                Ambiance.Burst(null, transform.position + Vector3.up * 1.2f, new Color(1f, 0.8f, 0.35f));
                Ambiance.Burst(null, transform.position + Vector3.up * 0.6f, new Color(1f, 0.9f, 0.55f));
                if (Game.Hud != null) { Game.Hud.FlashScore(); if (Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(stars >= 20 ? 0.3f : 0.12f); }
                if (stars >= 20) Sfx.Bell();
                RefreshPile();
                return;
            }
            Pillage(me);
        }

        /// <summary>Piller la stele d'un autre : la moitie de son or passe dans ton butin porte.</summary>
        void Pillage(Seeker me)
        {
            int taken = me.Hoard.RequestPillage(owner.Hoard);
            me.SyncWeight();
            if (taken <= 0) { Sfx.Deny(); return; }
            Stats.Looted += taken;
            Sfx.Coin();
            Sfx.Discovery();
            Pickup.FlyLoot(transform.position + Vector3.up, taken);
            if (Game.Hud != null)
                Game.Hud.ShowDiscovery("", "+★" + taken, owner.Name, "", owner.Colour);
            Rival.NotifyTheft(owner, me);
        }
    }
}
