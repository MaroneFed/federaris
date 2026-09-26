using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON FAIT, LES MAINS VIDES (27/09 -- Martin : "pas d'epee, juste des
    /// capacites, on tiendra jamais rien en main").
    ///
    ///   clic droit    POUSSER : le premier devant soi part en arriere et en l'air ;
    ///                 s'il porte la Couronne, il la lache ;
    ///   clic gauche   ta premiere capacite active (28/09 : les touches inversables
    ///                 dans les reglages, "Pousser sur") ;
    ///                 (28/09 : celles qu'on VISE -- ruee, grappin, crochet, clignement,
    ///                 mur, givre, echange, souffle -- se lancent au RELACHEMENT : tant
    ///                 qu'on tient la touche, un apercu montre ou elles iront.)
    ///   E             la deuxieme ;
    ///   R             la troisieme ;
    ///   V             le DON d'un sanctuaire (pour la manche) ;
    ///
    /// Les capacites elles-memes sont dans World/AbilityCaster.cs : les bots passent
    /// par le meme code. Rien ne s'affiche en main.
    /// </summary>
    public class AbilityUser : MonoBehaviour
    {
        /// <summary>Ce que le HUD affiche sous le reticule ("F|grimper").</summary>
        public static string Hint;
        /// <summary>Quelqu'un a portee de poussee : le reticule s'ouvre.</summary>
        public static bool FoeInReach;
        /// <summary>Le dernier refus ("Recharge", "Mains prises") et son heure, pour le HUD.</summary>
        public static string Refusal;
        public static float RefusalAt = -9f;
        /// <summary>La touche qu'on tient pour viser (0 a 3), -1 sinon : le HUD souleve sa carte.</summary>
        public static int AimingSlot = -1;
        /// <summary>Le porteur sur qui tu peux piquer maintenant (le HUD l'annonce), null sinon.</summary>
        public static Seeker DiveAt;

        PlayerController player;
        AbilityPreview preview;
        Ability aiming;

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        /// <summary>Les capacites actives du joueur, dans l'ordre des touches (0 : clic gauche, 1 : E, 2 : R).</summary>
        public static List<Ability> Actives(Seeker s)
        {
            return s != null ? s.Slot.Actives : new List<Ability>();
        }

        void Update()
        {
            Hint = null;
            FoeInReach = false;
            DiveAt = null;
            Seeker me = Game.Me;
            if (me == null || player == null || player.InputLocked || Ballista.PlayerOn != null || player.cameraTransform == null)
            {
                // (Sur une arbaleste : le clic gauche tire, voir Ballista.)
                StopAiming();
                return;
            }
            Transform eye = player.cameraTransform;

            // --- pousser
            FoeInReach = me.CanShove && Combat.FoeAhead(me, eye.forward);
            // En l'air, le porteur dans le viseur : la poussee devient le PIQUE D'AIGLE.
            DiveAt = player.Airborne && !player.Diving ? Combat.DiveTarget(me, eye.position, eye.forward) : null;
            if (FiefInput.PushPressed && DiveAt != null) Combat.Dive(me, DiveAt);
            else if (FiefInput.PushPressed)
            {
                if (!me.CanShove) Refuse(me.Stunned ? "Étourdi" : "Mains prises");
                else if (Time.time < me.ShoveReadyAt) Sfx.Deny();
                else
                {
                    me.ShoveReadyAt = Time.time + Seeker.ShoveCooldown * (me.Has(Ability.Poigne) ? 0.6f : 1f);
                    if (Game.Rig != null) Game.Rig.PlaySwing();
                    if (!Combat.Shove(me, eye.forward)) Sfx.Whoosh();
                }
            }

            // --- les capacites
            List<Ability> actives = me.Slot.Actives;
            for (int i = 0; i < 4; i++)
            {
                if (!FiefInput.CastPressed(i)) continue;
                Ability a;
                if (i < 3)
                {
                    if (i >= actives.Count) { Refuse("Aucune capacité"); continue; }
                    a = actives[i];
                }
                else
                {
                    if (!me.HasGift) { Refuse("Aucun don"); continue; }
                    a = me.Gift;
                }
                // Une capacite qui vise : on la tient, l'apercu s'affiche ; sinon elle part.
                if (AbilityCaster.NeedsAim(a))
                {
                    string why = AbilityCaster.WhyNot(me, a);
                    if (why != null) { Refuse(why); continue; }
                    AimingSlot = i;
                    aiming = a;
                }
                else Cast(me, a, eye);
            }
            if (AimingSlot >= 0)
            {
                if (preview == null) preview = AbilityPreview.Attach(transform);
                if (FiefInput.CastHeld(AimingSlot)) preview.Show(me, aiming, eye.position, eye.forward);
                else
                {
                    Ability a = aiming;
                    StopAiming();
                    Cast(me, a, eye);
                }
            }
        }

        void StopAiming()
        {
            AimingSlot = -1;
            if (preview != null) preview.Hide();
        }

        void Cast(Seeker me, Ability a, Transform eye)
        {
            string why = AbilityCaster.WhyNot(me, a);
            if (why != null) { Refuse(why); return; }
            if (!AbilityCaster.Cast(me, a, eye.position, eye.forward)) Refuse("Rien à viser");
            else if (Game.Rig != null) Game.Rig.PlaySwing();
        }

        static void Refuse(string why)
        {
            Refusal = why;
            RefusalAt = Time.time;
            Sfx.Deny();
        }
    }

    /// <summary>
    /// L'APERCU DE VISEE (28/09 -- Martin : "comment on les utilise") : pendant qu'on
    /// tient la touche d'une capacite qui vise, des lignes de lumiere montrent ce
    /// qu'elle va faire -- ou le grappin mord, qui le crochet attrape, ou l'on
    /// reapparait, le chemin de la ruee, la courbe du givre, le mur, le couloir du
    /// souffle. Couleur de la capacite si c'est bon ; gris si rien a viser.
    ///
    /// Concept Unity : un LineRenderer dessine une ligne entre des points qu'on lui
    /// donne, chaque image. Trois suffisent pour tous les apercus.
    /// </summary>
    public class AbilityPreview : MonoBehaviour
    {
        LineRenderer line, line2, ring;
        readonly Vector3[] arc = new Vector3[80];
        static readonly Color Nothing = new Color(0.6f, 0.6f, 0.6f, 0.5f);

        public static AbilityPreview Attach(Transform owner)
        {
            GameObject go = new GameObject("Aperçu de visée");
            go.transform.SetParent(owner, false);
            AbilityPreview p = go.AddComponent<AbilityPreview>();
            p.line = p.NewLine("Ligne", false);
            p.line2 = p.NewLine("Ligne 2", false);
            p.ring = p.NewLine("Anneau", true);
            return p;
        }

        LineRenderer NewLine(string name, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = Ambiance.Additive;
            l.useWorldSpace = true;
            l.loop = loop;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.enabled = false;
            return l;
        }

        public void Hide()
        {
            line.enabled = false;
            line2.enabled = false;
            ring.enabled = false;
        }

        void Paint(LineRenderer l, Color c, float width, float endAlpha)
        {
            l.enabled = true;
            l.widthMultiplier = width;
            l.startColor = new Color(c.r, c.g, c.b, 0.9f);
            l.endColor = new Color(c.r, c.g, c.b, endAlpha);
        }

        void Straight(LineRenderer l, Vector3 a, Vector3 b, Color c, float width, float endAlpha)
        {
            Paint(l, c, width, endAlpha);
            l.positionCount = 2;
            l.SetPosition(0, a);
            l.SetPosition(1, b);
        }

        void Circle(Vector3 centre, Vector3 normal, float radius, Color c)
        {
            Paint(ring, c, 0.14f, 0.9f);
            ring.positionCount = 36;
            Quaternion q = Quaternion.FromToRotation(Vector3.up, normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up);
            float spin = Time.time * 2f;
            for (int i = 0; i < 36; i++)
            {
                float a = i / 36f * Mathf.PI * 2f + spin;
                ring.SetPosition(i, centre + q * new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius));
            }
        }

        /// <summary>Montrer ce que ferait "a" lancee maintenant depuis "eye", vers "aim".</summary>
        public void Show(Seeker s, Ability a, Vector3 eye, Vector3 aim)
        {
            Hide();
            if (s == null || s.Body == null) return;
            Color tint = AbilityInfo.Tint(a);
            Vector3 pos = s.Body.position;
            Vector3 chest = pos + Vector3.up * 1.1f;
            Vector3 flat = new Vector3(aim.x, 0f, aim.z);
            flat = flat.sqrMagnitude > 0.001f ? flat.normalized : s.Body.forward;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 10f);
            switch (a)
            {
                case Ability.Grappin:
                {
                    RaycastHit hit;
                    if (AbilityCaster.RayFrom(s, eye, aim, AbilityCaster.GrappinRange, out hit))
                    {
                        Straight(line, chest, hit.point, tint, 0.08f, 0.9f);
                        Circle(hit.point + hit.normal * 0.05f, hit.normal, 0.8f * pulse, tint);
                    }
                    else Straight(line, chest, eye + aim * AbilityCaster.GrappinRange, Nothing, 0.05f, 0f);
                    break;
                }
                case Ability.Crochet:
                case Ability.Echange:
                {
                    float range = a == Ability.Crochet ? AbilityCaster.CrochetRange : AbilityCaster.EchangeRange;
                    Seeker t = Combat.Aimed(s, eye, aim, range, AbilityCaster.AimAngle);
                    if (t != null && t.Body != null)
                    {
                        Straight(line, chest, t.Body.position + Vector3.up * 1.1f, tint, 0.08f, 0.9f);
                        Circle(t.Body.position + Vector3.up * 0.1f, Vector3.up, 1.3f * pulse, tint);
                        if (a == Ability.Echange) Straight(line2, pos + Vector3.up * 0.1f, t.Body.position + Vector3.up * 0.1f, tint, 0.2f, 0.2f);
                    }
                    else Straight(line, chest, eye + aim * range, Nothing, 0.05f, 0f);
                    break;
                }
                case Ability.Clignement:
                {
                    Vector3 dest = AbilityCaster.BlinkDestination(s, flat);
                    Straight(line, pos + Vector3.up * 0.1f, dest + Vector3.up * 0.1f, tint, 0.12f, 0.9f);
                    // Ton fantome : un anneau au sol et un trait debout, la ou tu apparaitras.
                    Circle(dest, Vector3.up, 0.9f * pulse, tint);
                    Straight(line2, dest, dest + Vector3.up * 2f, tint, 0.5f, 0.1f);
                    break;
                }
                case Ability.Ruee:
                {
                    Vector3 end = AbilityCaster.RueeEnd(s, flat);
                    Straight(line, pos + Vector3.up * 0.1f, end + Vector3.up * 0.1f, tint, 1.6f, 0.4f);
                    Circle(end, Vector3.up, 0.8f * pulse, tint);
                    break;
                }
                case Ability.Mur:
                {
                    // Le trace du mur au sol.
                    Vector3 at = pos + flat * AbilityCaster.WallAhead;
                    RaycastHit hit;
                    if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
                    Vector3 side = new Vector3(flat.z, 0f, -flat.x) * 5f;
                    Vector3 depth = flat * 0.55f;
                    Paint(ring, tint, 0.14f, 0.9f);
                    ring.positionCount = 4;
                    ring.SetPosition(0, at - side - depth + Vector3.up * 0.1f);
                    ring.SetPosition(1, at + side - depth + Vector3.up * 0.1f);
                    ring.SetPosition(2, at + side + depth + Vector3.up * 0.1f);
                    ring.SetPosition(3, at - side + depth + Vector3.up * 0.1f);
                    Straight(line, at + Vector3.up * 0.1f, at + Vector3.up * 4.5f, tint, 0.3f, 0.05f);
                    break;
                }
                case Ability.Gel:
                {
                    // La courbe de la boule, jusqu'a ce qu'elle touche.
                    Vector3 p = eye + aim * 0.8f;
                    Vector3 v = Thrown.Lob(aim, AbilityCaster.FrostReach);
                    int n = 0;
                    Vector3 end = p;
                    bool hitSomething = false;
                    for (int i = 0; i < arc.Length - 1; i++)
                    {
                        arc[n++] = p;
                        Vector3 next = p + v * 0.05f;
                        v += Vector3.up * -14f * 0.05f;
                        RaycastHit hit;
                        if (Physics.Linecast(p, next, out hit, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(s.Body))
                        {
                            arc[n++] = hit.point;
                            end = hit.point;
                            hitSomething = true;
                            break;
                        }
                        p = next;
                        end = p;
                    }
                    Paint(line, tint, 0.1f, 0.9f);
                    line.positionCount = n;
                    for (int i = 0; i < n; i++) line.SetPosition(i, arc[i]);
                    if (hitSomething) Circle(end, Vector3.up, Thrown.SlowRadius * pulse, tint);
                    break;
                }
                case Ability.Souffle:
                {
                    // Le couloir de la vague : deux bords qui s'ecartent et s'eteignent au loin.
                    Vector3 dir = aim;
                    dir.y = Mathf.Clamp(dir.y, -0.3f, 0.45f);
                    dir.Normalize();
                    Vector3 side = new Vector3(flat.z, 0f, -flat.x);
                    Vector3 start = chest + flat * 1.2f - Vector3.up * 0.8f;
                    Vector3 far = start + dir * Gale.Range;
                    Straight(line, start - side * 1.5f, far - side * 9f, tint, 0.25f, 0f);
                    Straight(line2, start + side * 1.5f, far + side * 9f, tint, 0.25f, 0f);
                    break;
                }
            }
        }
    }
}
