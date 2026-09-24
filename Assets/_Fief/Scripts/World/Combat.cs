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
            Sfx.HarvestTap(ResourceType.Iron);

            Vector3 flatForward = eye.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r == null || !r.seeker.Alive) continue;
                Vector3 to = r.transform.position - me.Body.position;
                to.y = 0f;
                if (to.magnitude > Reach || Vector3.Angle(flatForward, to) > 55f) continue;
                if (me.Kit.Wear(1)) Toasts.Show("Ton epee s'est brisee.", new Color(0.8f, 0.6f, 0.4f));
                Hit(r.seeker, me, SwordDamage);
                break;                              // un coup, une cible
            }
        }

        /// <summary>Un coup porte. Tout passe par ici : degats, cris, mort.</summary>
        public static void Hit(Seeker victim, Seeker attacker, float damage)
        {
            if (victim == null || !victim.Alive) return;
            bool dead = victim.TakeDamage(damage, Time.time);
            Sfx.Harvest(ResourceType.Iron);
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

            if (dead) Fall(victim, attacker);
        }

        /// <summary>Tomber : tout ce qu'on porte reste sur place, dans une depouille.</summary>
        static void Fall(Seeker victim, Seeker killer)
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
                if (Game.Hud != null) Game.Hud.ShowDeath(killer != null ? killer.Name : "la foret");
            }
            else
            {
                Rival r = Rival.Of(victim);
                if (r != null) r.Die();
                if (killer != null && killer.IsPlayer)
                    Toasts.Show(victim.Name + " est tombe. Sa depouille est a toi -- fouille-la (E).", victim.Colour);
            }
        }

        /// <summary>Se relever : a sa stele, sinon a son camp, sinon la ou l'on est tombe.</summary>
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
        readonly Inventory contents = new Inventory();
        Relic relic;
        Seeker owner;
        Tool[] tools = new Tool[2];

        public static void Drop(Seeker victim, Vector3 at)
        {
            GameObject go = new GameObject("DEPOUILLE de " + victim.Name);
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

            Color cloth = Palette.Shade(victim.Colour, 0.5f);
            Proto.BeginVisualOnly();
            GameObject sack = Proto.Cube(go.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.7f, 0.36f, 0.5f), cloth, "Sac eventre");
            sack.transform.localRotation = Quaternion.Euler(0f, 30f, 12f);
            Proto.Cube(go.transform, new Vector3(0.5f, 0.05f, 0.3f), new Vector3(0.9f, 0.06f, 0.6f), Palette.Shade(cloth, 0.8f), "Cape");
            GameObject lamp = Proto.Cube(go.transform, new Vector3(-0.5f, 0.1f, -0.2f), new Vector3(0.18f, 0.2f, 0.18f), new Color(0.15f, 0.15f, 0.16f), "Lanterne");
            lamp.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
            Proto.EndVisualOnly();
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
                string who = owner == Game.Me ? "ta depouille" : "la depouille de " + owner.Name;
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
            Toasts.Show(Empty ? "Tu as tout repris." : "Ton sac est plein : il reste des choses dans la depouille.", UiStyle.InkDim);
            if (Empty) Destroy(gameObject, 0.1f);
        }
    }
}
