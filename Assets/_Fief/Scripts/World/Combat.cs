using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE COMBAT : simple et lisible (decision verrouillee), mais il compte.
    ///
    ///   - On ne frappe qu'avec une EPEE (Tab, Artisanat : 2 bois, 3 fer).
    ///     Quatre coups tuent. Un coup, c'est un arc devant soi, a 2,4 m.
    ///   - QUI PORTE UNE RELIQUE NE PEUT PAS FRAPPER (la sienne en main, ou une
    ///     volee). Les autres, si. Porter, c'est etre une cible.
    ///   - Tomber, c'est tout lacher : le sac, les outils, la relique. Il reste une
    ///     DEPOUILLE que n'importe qui peut fouiller. On se releve a sa stele (ou a
    ///     son camp) quelques secondes plus tard, les mains vides.
    ///   - La vie remonte doucement apres huit secondes sans coup.
    /// </summary>
    public static class Combat
    {
        public const float SwordDamage = 25f;
        public const float Reach = 2.4f;
        public const float RespawnSeconds = 5f;

        /// <summary>Le joueur frappe avec son epee.</summary>
        public static void PlayerStrike(Transform eye)
        {
            Seeker me = Game.Me;
            if (me == null) return;
            if (!me.CanStrike)
            {
                Sfx.Deny();
                Toasts.Show("Tu portes une relique : tu ne peux pas frapper.", new Color(0.9f, 0.6f, 0.4f));
                return;
            }
            Sfx.Whoosh();

            Vector3 flatForward = eye.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
            float damage = SwordDamage * (me.Hoard.Level(UpgradeKind.Lame) > 0 ? UpgradeInfo.LameFactor : 1f);
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r == null || !r.seeker.Alive) continue;
                Vector3 to = r.transform.position - me.Body.position;
                to.y = 0f;
                if (to.magnitude > Reach || Vector3.Angle(flatForward, to) > 55f) continue;
                if (me.Kit.Wear(1)) Toasts.Show("Ton épée s'est brisée.", new Color(0.8f, 0.6f, 0.4f));
                Hit(r.seeker, me, damage);
                Punch.Apply(r.Figure, me.Body.position);
                return;                             // un coup, une cible
            }
            // Les betes : loups, revenants.
            for (int i = 0; i < Beast.All.Count; i++)
            {
                Beast b = Beast.All[i];
                if (b == null || !b.Alive) continue;
                Vector3 to = b.transform.position - me.Body.position;
                to.y = 0f;
                if (to.magnitude > Reach + 0.4f || Vector3.Angle(flatForward, to) > 60f) continue;
                if (me.Kit.Wear(1)) Toasts.Show("Ton épée s'est brisée.", new Color(0.8f, 0.6f, 0.4f));
                b.Hurt(damage, me);
                return;
            }
        }

        /// <summary>Y a-t-il un ennemi (rival, bete) a portee d'epee, devant soi ?</summary>
        public static bool FoeAhead(Transform eye)
        {
            Seeker me = Game.Me;
            if (me == null || me.Body == null) return false;
            Vector3 f = eye.forward;
            f.y = 0f;
            f.Normalize();
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r == null || !r.seeker.Alive) continue;
                Vector3 to = r.transform.position - me.Body.position;
                to.y = 0f;
                if (to.magnitude <= Reach && Vector3.Angle(f, to) <= 55f) return true;
            }
            for (int i = 0; i < Beast.All.Count; i++)
            {
                Beast b = Beast.All[i];
                if (b == null || !b.Alive) continue;
                Vector3 to = b.transform.position - me.Body.position;
                to.y = 0f;
                if (to.magnitude <= Reach + 0.4f && Vector3.Angle(f, to) <= 60f) return true;
            }
            return false;
        }

        /// <summary>Un coup porte. Tout passe par ici : degats, cris, mort.</summary>
        public static void Hit(Seeker victim, Seeker attacker, float damage)
        {
            Hit(victim, attacker, damage, "sous les coups de " + (attacker != null ? attacker.Name : "la forêt"));
        }

        /// <summary>Un coup porte, en disant de quoi on meurt s'il est mortel.</summary>
        public static void Hit(Seeker victim, Seeker attacker, float damage, string how)
        {
            if (victim == null || !victim.Alive) return;
            bool dead = victim.TakeDamage(damage, Time.time);
            Sfx.Thud();
            if (victim.Body != null && !victim.IsPlayer)
                Ambiance.Burst(null, victim.Body.position + Vector3.up * 1.2f, new Color(0.55f, 0.1f, 0.08f));
            if (victim.Body != null)
                FloatingTexts.Spawn(victim.Body.position + Vector3.up * 2.1f, "-" + Mathf.RoundToInt(damage), new Color(1f, 0.35f, 0.3f));

            if (victim.IsPlayer)
            {
                if (Game.Hud != null) Game.Hud.Hurt();
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.2f);
            }
            else
            {
                Rival r = Rival.Of(victim);
                if (r != null) r.OnHit(attacker);
            }

            if (dead) Fall(victim, attacker, how);
        }

        /// <summary>
        /// Une mort d'un coup : un piege, une bete, la chute. "how" dit comment, pour
        /// l'ecran de chute ("dans un piege de Mahaut").
        /// </summary>
        public static void Kill(Seeker victim, Seeker killer, string how)
        {
            if (victim == null || !victim.Alive) return;
            victim.TakeDamage(9999f, Time.time);
            if (victim.IsPlayer && Game.Hud != null) Game.Hud.Hurt();
            Fall(victim, killer, how);
        }

        /// <summary>Tomber : tout ce qu'on porte reste sur place, dans une depouille.</summary>
        static void Fall(Seeker victim, Seeker killer, string how)
        {
            Vector3 at = victim.Body != null ? victim.Body.position : Vector3.zero;
            // Celui qui abat son voleur reprend sa relique, tout de suite.
            if (killer != null && victim.Hoard.Trophy != null && victim.Hoard.TrophyFrom == killer)
            {
                killer.Hoard.TryRecover(victim.Hoard.TrySurrenderTrophy());
                killer.SyncWeight();
            }
            Remains.Drop(victim, at);

            if (victim.IsPlayer)
            {
                Stats.Deaths++;
                Stats.LastDeath = how;
                if (Game.Hud != null) Game.Hud.ShowDeath(how);
            }
            else
            {
                Rival r = Rival.Of(victim);
                if (r != null) r.Die();
                if (killer != null && killer.IsPlayer) Stats.RivalsDowned++;
                if (killer != null && killer.IsPlayer)
                    Toasts.Show(victim.Name + " est tombé. Fouille-le (E).", victim.Colour);
            }
        }

        /// <summary>Se relever : a sa stele, sinon a son camp, sinon là où l'on est tombe.</summary>
        public static Vector3 RespawnPoint(Seeker s, Vector3 fallback)
        {
            Hoard h = s.Hoard;
            Vector3 p = h.StelePlanted ? h.StelePosition : h.CampPlanted ? h.CampPosition : fallback;
            return Ground.Place(p.x + 1.5f, p.z + 1.5f, 0.2f);
        }
    }

    /// <summary>
    /// UNE DEPOUILLE : ce qu'un chercheur portait quand il est tombe. Un sac eventre,
    /// une lanterne renversee. On la fouille (E maintenu) : on prend ce qui rentre
    /// dans son sac, la relique s'il y en avait une (elle devient un trophee, sauf si
    /// c'est la tienne), et les outils s'il reste une place.
    /// </summary>
    public class Remains : MonoBehaviour, IInteractable
    {
        /// <summary>Toutes les depouilles au sol (la tienne va sur la boussole et la carte).</summary>
        public static readonly System.Collections.Generic.List<Remains> All = new System.Collections.Generic.List<Remains>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Vrai si c'est ta depouille, et qu'il y reste quelque chose.</summary>
        public bool IsMine { get { return owner != null && owner == Game.Me && !Empty; } }

        readonly Inventory contents = new Inventory();
        Relic relic;
        Seeker owner;
        Tool[] tools = new Tool[2];

        public static void Drop(Seeker victim, Vector3 at)
        {
            GameObject go = new GameObject("DÉPOUILLE de " + victim.Name);
            go.transform.position = Ground.Place(at.x, at.z, 0f);
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.4f, 0f);
            trigger.size = new Vector3(1.4f, 0.8f, 1.4f);
            Remains r = go.AddComponent<Remains>();
            r.owner = victim;
            r.contents.MaxWeight = 999f;

            // Tout ce qu'il portait change de main -- par TryRemove / TryAdd, comme toujours.
            for (int i = 0; i < ResourceInfo.Count; i++)
            {
                ResourceType t = (ResourceType)i;
                int n = victim.Bag.TryRemove(t, victim.Bag.Get(t));
                r.contents.TryAdd(t, n);
            }
            Hoard h = victim.Hoard;
            if (h.Trophy != null) r.relic = h.TrySurrenderTrophy();
            else if (h.RelicInHand) r.relic = h.TrySurrenderRelic();
            for (int i = 0; i < victim.Kit.Slots.Length; i++) r.tools[i] = victim.Kit.Slots[i];
            victim.Kit.Clear();
            victim.SyncWeight();

            // LA DEPOUILLE : une cape etalee a sa couleur, le sac eventre, la lanterne
            // renversee qui brule encore un peu (c'est ce qu'on voit de loin), et ce
            // qu'il portait, repandu : des buches, des eclats bleus, des lingots.
            Color cloth = Palette.Shade(victim.Colour, 0.5f);
            Transform body = go.transform;
            Proto.BeginVisualOnly();
            GameObject cape = Proto.Cube(body, new Vector3(0.2f, 0.02f, 0.1f), new Vector3(1.3f, 0.03f, 0.9f), cloth, "Cape");
            cape.transform.localRotation = Quaternion.Euler(0f, 23f, 2f);
            Proto.Cube(body, new Vector3(0.75f, 0.03f, 0.4f), new Vector3(0.3f, 0.03f, 0.2f), Palette.Shade(victim.Colour, 0.85f), "Bord");
            GameObject sack = Proto.Sphere(body, new Vector3(-0.1f, 0.16f, -0.05f), new Vector3(0.55f, 0.32f, 0.42f), new Color(0.36f, 0.3f, 0.22f), "Sac éventré");
            sack.transform.localRotation = Quaternion.Euler(0f, 30f, 20f);
            Proto.Cube(body, new Vector3(0.12f, 0.1f, -0.1f), new Vector3(0.22f, 0.04f, 0.3f), new Color(0.26f, 0.21f, 0.15f), "Rabat");
            GameObject lamp = Proto.Cube(body, new Vector3(-0.55f, 0.1f, -0.25f), new Vector3(0.16f, 0.2f, 0.16f), new Color(0.15f, 0.15f, 0.16f), "Lanterne");
            lamp.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
            GameObject ember = Proto.Cube(body, new Vector3(-0.5f, 0.1f, -0.25f), new Vector3(0.06f, 0.06f, 0.06f), Color.white, "Braise");
            ember.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.55f, 0.2f), 2.2f);
            System.Random rng = new System.Random(victim.Name.Length * 31 + Mathf.RoundToInt(at.x));
            int wood = Mathf.Min(6, r.contents.Get(ResourceType.Deadwood) / 2);
            for (int i = 0; i < wood; i++)
            {
                GameObject stick = Proto.Cylinder(body, new Vector3(0.4f + (float)rng.NextDouble() * 0.5f, 0.04f, -0.5f + (float)rng.NextDouble() * 0.6f),
                                                  new Vector3(0.07f, 0.22f, 0.07f), new Color(0.42f, 0.34f, 0.24f), "Bûche");
                stick.transform.localRotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 180f, 0f);
            }
            int moon = Mathf.Min(5, r.contents.Get(ResourceType.Moonstone));
            Material shine = MaterialFactory.GetGlow(new Color(0.62f, 0.8f, 1f), 1.4f);
            for (int i = 0; i < moon; i++)
            {
                GameObject chip = Proto.Cone(body, new Vector3(-0.3f + (float)rng.NextDouble() * 0.5f, 0.02f, 0.35f + (float)rng.NextDouble() * 0.3f),
                                             0.05f, 0.14f, new Color(0.62f, 0.8f, 1f), "Éclat", 6);
                chip.transform.localRotation = Quaternion.Euler(70f, (float)rng.NextDouble() * 360f, 0f);
                chip.GetComponent<Renderer>().sharedMaterial = shine;
            }
            int iron = Mathf.Min(4, r.contents.Get(ResourceType.Iron));
            for (int i = 0; i < iron; i++)
            {
                GameObject bar = Proto.Cube(body, new Vector3(-0.6f + i * 0.14f, 0.03f, 0.3f), new Vector3(0.1f, 0.05f, 0.26f),
                                            ResourceInfo.Tint(ResourceType.Iron), "Lingot");
                bar.transform.localRotation = Quaternion.Euler(0f, i * 25f, 0f);
            }
            Proto.EndVisualOnly();
            GameObject lightGo = new GameObject("Braise");
            lightGo.transform.SetParent(body, false);
            lightGo.transform.localPosition = new Vector3(-0.45f, 0.3f, -0.25f);
            Light glow = lightGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.55f, 0.25f);
            glow.range = 3.5f;
            glow.intensity = 0.7f;
            glow.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
            if (r.relic != null)
                Ambiance.Sparkles(go.transform, new Vector3(0f, 0.4f, 0f), Stele.RuneBlue);
        }

        bool Empty
        {
            get { return contents.IsEmpty && relic == null && tools[0] == null && tools[1] == null; }
        }

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return !Empty; } }
        public string Prompt
        {
            get
            {
                string who = owner == Game.Me ? "ta dépouille" : "la dépouille de " + owner.Name;
                return "Fouiller " + who + (relic != null ? "  (une relique !)" : "");
            }
        }
        public float HoldDuration { get { return 1.5f; } }

        public void Interact()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            int taken = 0;
            for (int i = 0; i < ResourceInfo.Count; i++)
            {
                ResourceType t = (ResourceType)i;
                int n = contents.TryRemove(t, Mathf.Min(contents.Get(t), me.Bag.SpaceFor(t)));
                me.Bag.TryAdd(t, n);
                taken += n;
            }
            if (relic != null)
            {
                bool mine = owner == me;
                if (mine && me.Hoard.TryRecover(relic)) relic = null;
                else if (!mine && me.Hoard.TryTakeTrophy(relic, owner)) relic = null;
            }
            for (int i = 0; i < tools.Length; i++)
            {
                int slot = me.Kit.FreeSlot;
                if (tools[i] == null || slot < 0) continue;
                me.Kit.Slots[slot] = tools[i];
                tools[i] = null;
            }
            me.SyncWeight();
            Sfx.HarvestTap(ResourceType.Deadwood);
            Toasts.Show(Empty ? "Tu as tout repris." : "Sac plein : il en reste.", UiStyle.InkDim);
            if (Empty) Destroy(gameObject, 0.1f);
        }
    }
}
