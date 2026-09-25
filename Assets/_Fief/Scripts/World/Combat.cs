using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE COMBAT : simple et lisible, mais il compte.
    ///
    ///   - L'EPEE (clic gauche, quand on ne tient pas d'objet) : un arc devant soi, a
    ///     2,4 m. Quatre coups tuent un joueur. On frappe les joueurs, les betes, les
    ///     gardes, le Roi Creux. Le porteur de la Couronne ne frappe pas.
    ///   - LA POUSSEE (clic droit) : l'autre part en arriere -- et s'il porte la
    ///     Couronne, IL LA LACHE. Toutes les 3 s (1,5 s avec la Poigne).
    ///   - Tomber, c'est tout lacher : les objets restent dans une DEPOUILLE, la
    ///     Couronne roule par terre. On se releve a son point de depart 5 s plus tard.
    ///     (La Seconde chance, une fois par manche : on se releve sur place.)
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
            if (me == null || !me.CanStrike) return;
            Sfx.Whoosh();
            Vector3 f = Flat(eye.forward).normalized;
            Vector3 from = me.Body.position;

            // Les autres joueurs.
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == me || !s.Alive || s.Body == null || !InArc(from, f, s.Body.position, Reach, 55f)) continue;
                Hit(s, me, SwordDamage);
                Rival r = Rival.Of(s);
                if (r != null) Punch.Apply(r.Figure, from);
                Hud.HitStop(0.06f);
                return;                             // un coup, une cible
            }
            for (int i = 0; i < Beast.All.Count; i++)
            {
                Beast b = Beast.All[i];
                if (b == null || !b.Alive || !InArc(from, f, b.transform.position, Reach + 0.4f, 60f)) continue;
                b.Hurt(SwordDamage, me);
                Hud.HitStop(0.05f);
                return;
            }
            for (int i = 0; i < Guard.All.Count; i++)
            {
                Guard g = Guard.All[i];
                if (g == null || !g.Alive || !InArc(from, f, g.transform.position, Reach + g.Girth, 55f)) continue;
                g.Hurt(SwordDamage, me);
                Hud.HitStop(0.06f);
                return;
            }
        }

        /// <summary>Y a-t-il quelqu'un a portee d'epee, devant soi ?</summary>
        public static bool FoeAhead(Transform eye)
        {
            Seeker me = Game.Me;
            if (me == null || me.Body == null) return false;
            Vector3 f = Flat(eye.forward).normalized;
            Vector3 from = me.Body.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s != me && s.Alive && s.Body != null && InArc(from, f, s.Body.position, Reach, 55f)) return true;
            }
            for (int i = 0; i < Beast.All.Count; i++)
                if (Beast.All[i] != null && Beast.All[i].Alive && InArc(from, f, Beast.All[i].transform.position, Reach + 0.4f, 60f)) return true;
            for (int i = 0; i < Guard.All.Count; i++)
                if (Guard.All[i] != null && Guard.All[i].Alive && InArc(from, f, Guard.All[i].transform.position, Reach + Guard.All[i].Girth, 55f)) return true;
            return false;
        }

        static bool InArc(Vector3 from, Vector3 forward, Vector3 target, float reach, float angle)
        {
            Vector3 to = target - from;
            if (Mathf.Abs(to.y) > 2.2f) return false;
            to.y = 0f;
            return to.magnitude <= reach && Vector3.Angle(forward, to) <= angle;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        // ================================================================== la poussee

        /// <summary>
        /// POUSSER : le premier joueur (ou bete) devant soi, a 2,6 m, part en arriere.
        /// Le porteur de la Couronne la lache. Renvoie vrai si on a touche quelqu'un.
        /// </summary>
        public static bool Shove(Seeker by, Vector3 forward)
        {
            if (by == null || by.Body == null || !by.Alive) return false;
            Vector3 f = Flat(forward).normalized;
            float force = by.Has(Power.Poigne) ? 16f : 9f;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || !s.Alive || s.Body == null || !InArc(by.Body.position, f, s.Body.position, 2.6f, 60f)) continue;
                Vector3 push = Flat(s.Body.position - by.Body.position).normalized;
                if (push.sqrMagnitude < 0.01f) push = f;
                Knockback(s, push * force + Vector3.up * 3.5f);
                if (s.CarriesCrown) Crown.KnockOff(s, push);
                Sfx.Thud();
                if (s.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.3f);
                Rival r = Rival.Of(s);
                if (r != null) r.OnShoved(by);
                return true;
            }
            return false;
        }

        /// <summary>Projeter un joueur (poussee, coup du Roi).</summary>
        public static void Knockback(Seeker s, Vector3 velocity)
        {
            if (s == null || s.Body == null) return;
            if (s.IsPlayer && Game.Player != null) Game.Player.Push(velocity);
            else
            {
                Rival r = Rival.Of(s);
                if (r != null) r.Push(velocity);
            }
        }

        // ================================================================== les coups

        /// <summary>Un coup porte. Tout passe par ici : degats, cris, mort.</summary>
        public static void Hit(Seeker victim, Seeker attacker, float damage)
        {
            Hit(victim, attacker, damage, attacker != null ? attacker.Name : "la forêt");
        }

        /// <summary>Un coup porte, en disant de quoi on meurt s'il est mortel.</summary>
        public static void Hit(Seeker victim, Seeker attacker, float damage, string how)
        {
            if (victim == null || !victim.Alive) return;
            bool dead = victim.TakeDamage(damage, Time.time);
            Sfx.Thud();
            if (victim.Body != null && !victim.IsPlayer)
            {
                Ambiance.Burst(null, victim.Body.position + Vector3.up * 1.2f, new Color(0.55f, 0.1f, 0.08f));
                FloatingTexts.Spawn(victim.Body.position + Vector3.up * 2.1f, "-" + Mathf.RoundToInt(damage), new Color(1f, 0.35f, 0.3f));
            }
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

        /// <summary>Une mort d'un coup (un piege, la chute).</summary>
        public static void Kill(Seeker victim, Seeker killer, string how)
        {
            if (victim == null || !victim.Alive) return;
            victim.TakeDamage(9999f, Time.time);
            if (victim.IsPlayer && Game.Hud != null) Game.Hud.Hurt();
            Fall(victim, killer, how);
        }

        /// <summary>Tomber : la Couronne roule, les objets restent dans une depouille.</summary>
        static void Fall(Seeker victim, Seeker killer, string how)
        {
            Vector3 at = victim.Body != null ? victim.Body.position : Vector3.zero;
            if (victim.CarriesCrown && Crown.Instance != null) Crown.Instance.Drop(at);

            // La Seconde chance : on se releve sur place, une fois par manche.
            if (victim.Has(Power.SecondeChance) && !victim.SecondChanceUsed)
            {
                victim.SecondChanceUsed = true;
                victim.Health = victim.MaxHealth * 0.5f;
                Ambiance.Burst(null, at + Vector3.up, PowerInfo.Tint(Power.SecondeChance));
                Sfx.Discovery();
                return;
            }

            Remains.Drop(victim, at);
            if (killer != null && killer.IsPlayer && killer != victim)
            {
                Stats.PlayersDowned++;
                Sfx.Coin();
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.25f);
            }
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
            }
        }

        /// <summary>Se relever : a son point de depart de la manche.</summary>
        public static Vector3 RespawnPoint(Seeker s, Vector3 fallback)
        {
            Vector3 p = Spawns.Of(s.Index, fallback);
            float a = Random.value * Mathf.PI * 2f;
            return Ground.Place(p.x + Mathf.Cos(a) * 2f, p.z + Mathf.Sin(a) * 2f, 0.2f);
        }
    }

    /// <summary>
    /// UNE DEPOUILLE : ce qu'un joueur portait quand il est tombe -- ses objets. On la
    /// fouille (E maintenu) : on prend ce qui rentre dans ses mains.
    /// </summary>
    public class Remains : MonoBehaviour, IInteractable
    {
        public static readonly List<Remains> All = new List<Remains>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        readonly List<Item> items = new List<Item>();
        Seeker owner;

        public bool HasLoot { get { return items.Count > 0; } }
        public bool IsMine { get { return owner != null && owner == Game.Me && HasLoot; } }

        public static void Drop(Seeker victim, Vector3 at)
        {
            Remains r = null;
            for (int k = 0; k < Loadout.Size; k++)
            {
                Item it = victim.Items.Slots[k];
                if (it == Item.None) continue;
                if (r == null) r = Create(victim, at);
                r.items.Add(it);
            }
            victim.Items.Clear();
        }

        static Remains Create(Seeker victim, Vector3 at)
        {
            GameObject go = new GameObject("DÉPOUILLE de " + victim.Name);
            go.transform.position = Ground.Place(at.x, at.z, 0f);
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.4f, 0f);
            trigger.size = new Vector3(1.4f, 0.8f, 1.4f);
            Remains r = go.AddComponent<Remains>();
            r.owner = victim;

            // Une cape etalee a sa couleur, un sac eventre, une lanterne renversee.
            Transform body = go.transform;
            Proto.BeginVisualOnly();
            GameObject cape = Proto.Cube(body, new Vector3(0.2f, 0.02f, 0.1f), new Vector3(1.3f, 0.03f, 0.9f), Palette.Shade(victim.Colour, 0.5f), "Cape");
            cape.transform.localRotation = Quaternion.Euler(0f, 23f, 2f);
            GameObject sack = Proto.Sphere(body, new Vector3(-0.1f, 0.16f, -0.05f), new Vector3(0.55f, 0.32f, 0.42f), new Color(0.36f, 0.3f, 0.22f), "Sac");
            sack.transform.localRotation = Quaternion.Euler(0f, 30f, 20f);
            GameObject ember = Proto.Cube(body, new Vector3(-0.5f, 0.1f, -0.25f), new Vector3(0.06f, 0.06f, 0.06f), Color.white, "Braise");
            ember.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.55f, 0.2f), 2.2f);
            Proto.EndVisualOnly();
            Ambiance.Sparkles(go.transform, new Vector3(0f, 0.4f, 0f), Palette.Gold);
            Destroy(go, 120f);
            return r;
        }

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return HasLoot; } }
        public string Prompt { get { return owner == Game.Me ? "Tes objets" : "Fouiller"; } }
        public float HoldDuration { get { return 1f; } }

        public void Interact()
        {
            if (TakeFor(Game.Me) > 0) Sfx.Discovery();
            else Sfx.Deny();
        }

        /// <summary>Fouiller : toi ou un bot. Renvoie le nombre d'objets pris.</summary>
        public int TakeFor(Seeker s)
        {
            int taken = 0;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (!s.Items.TryAdd(items[i])) continue;
                items.RemoveAt(i);
                taken++;
            }
            if (!HasLoot) Destroy(gameObject, 0.1f);
            return taken;
        }
    }

    /// <summary>
    /// LES POINTS DE DEPART : un par place, a la lisiere, a egale distance du chateau,
    /// tournes d'un angle au hasard a chaque manche. On s'y releve quand on tombe.
    /// </summary>
    public static class Spawns
    {
        static readonly Dictionary<int, Vector3> Points = new Dictionary<int, Vector3>();

        public static void Place(int players, int seed)
        {
            Points.Clear();
            System.Random rng = new System.Random(seed ^ 0x51a);
            float turn = (float)rng.NextDouble() * 360f;
            for (int i = 0; i < players; i++)
            {
                float a = (turn + i * 360f / Mathf.Max(1, players)) * Mathf.Deg2Rad;
                Vector3 best = Vector3.zero;
                float bestScore = float.MaxValue;
                for (int k = 0; k < 24; k++)
                {
                    float aa = a + (k - 12) * 0.02f;
                    float r = 150f + (k % 4) * 6f;
                    float x = Mathf.Cos(aa) * r, z = Mathf.Sin(aa) * r;
                    if (Landmarks.Near(x, z, 10f)) continue;
                    float score = Forest.Canopy(x, z) + Ground.Slope(x, z);
                    if (score < bestScore) { bestScore = score; best = new Vector3(x, 0f, z); }
                }
                Points[i] = Ground.Place(best.x, best.z, 1f);
            }
        }

        public static Vector3 Of(int slot, Vector3 fallback)
        {
            Vector3 p;
            return Points.TryGetValue(slot, out p) ? p : fallback;
        }
    }
}
