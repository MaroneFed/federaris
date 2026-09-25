using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON FAIT, LES MAINS VIDES (27/09 -- Martin : "pas d'epee, juste des
    /// capacites, on tiendra jamais rien en main").
    ///
    ///   clic gauche   POUSSER : le premier devant soi part en arriere et en l'air ;
    ///                 s'il porte la Couronne, il la lache ;
    ///   clic droit    ta premiere capacite active ;
    ///   R             la deuxieme ;
    ///   C             la troisieme ;
    ///   V             le DON d'un sanctuaire (pour la manche) ;
    ///   F             face a un arbre : GRIMPER (F encore pour redescendre).
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

        PlayerController player;
        Transform perch;             // la plate-forme dans l'arbre, si l'on y est

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        /// <summary>Les capacites actives du joueur, dans l'ordre des touches (0 : clic droit, 1 : R, 2 : C).</summary>
        public static List<Ability> Actives(Seeker s)
        {
            return s != null ? s.Slot.Actives : new List<Ability>();
        }

        void Update()
        {
            Hint = null;
            FoeInReach = false;
            Seeker me = Game.Me;
            if (me == null || player == null || player.InputLocked) return;
            Transform eye = player.cameraTransform;
            if (eye == null) return;

            // Une escalade interrompue (pousse, projete) : on oublie le perchoir.
            if (climbing && !player.Scripted) climbing = false;
            if (!climbing && perch != null && (transform.position - perch.position).magnitude > 3f)
            {
                Destroy(perch.gameObject);
                perch = null;
            }
            if (climbing) { Climb(); return; }
            if (perch != null)
            {
                Hint = "F|descendre";
                if (FiefInput.ClimbPressed) ClimbDown();
            }

            // --- grimper
            RaycastHit hit = new RaycastHit();
            bool tree = perch == null && Physics.Raycast(eye.position, eye.forward, out hit, 3.2f, ~0, QueryTriggerInteraction.Ignore)
                        && Forest.IsTree(hit.collider);
            if (tree && !me.CarriesCrown)
            {
                if (FiefInput.ClimbPressed) { ClimbUp(hit.collider); return; }
                Hint = "F|grimper";
            }

            // --- pousser
            FoeInReach = me.CanShove && Combat.FoeAhead(me, eye.forward);
            if (FiefInput.PushPressed)
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
            for (int i = 0; i < 3; i++)
            {
                if (!FiefInput.CastPressed(i)) continue;
                if (i >= actives.Count) { Refuse("Aucune capacité"); continue; }
                Cast(me, actives[i], eye);
            }
            if (FiefInput.CastPressed(3))
            {
                if (me.HasGift) Cast(me, me.Gift, eye);
                else Refuse("Aucun don");
            }
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

        // ================================================================== grimper

        // Le mouvement en cours : on monte (ou on descend) le long du tronc, traction
        // apres traction. Pendant ce temps, rien d'autre ne se fait.
        bool climbing;
        bool climbingUp;
        float climbT;
        float climbDuration;
        Vector3 climbStart, climbBase, climbTop, climbEnd;
        int pullsDone;
        int Pulls = 5;

        /// <summary>
        /// S'installer dans l'arbre : une petite plate-forme de branches a quatre
        /// metres, contre le tronc. On y MONTE (1,6 s) : on s'approche du tronc, puis
        /// cinq tractions, chacune avec son froissement de branches, un peu de
        /// balancement ; enfin on se hisse sur la plate-forme. Personne ne regarde
        /// en l'air dans une foret : c'est le meilleur poste de guet du jeu.
        /// </summary>
        void ClimbUp(Collider trunk)
        {
            Vector3 centre = trunk.bounds.center;
            Vector3 toMe = transform.position - centre;
            toMe.y = 0f;
            toMe = toMe.sqrMagnitude > 0.01f ? toMe.normalized : Vector3.forward;
            float ground = Ground.Sample(centre.x, centre.z);
            // Un GEANT : on monte tout en haut, au-dessus de la canopee (voir Forest).
            float giant = Forest.GiantPerch(trunk);
            CapsuleCollider cap = trunk as CapsuleCollider;
            float trunkRadius = cap != null ? cap.radius * trunk.transform.lossyScale.x : 0.3f;
            float perchHeight = giant > 0f ? giant : 4.2f;
            Vector3 spot = new Vector3(centre.x, ground + perchHeight, centre.z) + toMe * (trunkRadius + 0.6f);
            Pulls = giant > 0f ? Mathf.RoundToInt(perchHeight / 1.6f) : 5;

            GameObject platform = new GameObject("Perchoir");
            platform.transform.position = spot;
            platform.transform.rotation = Quaternion.LookRotation(toMe, Vector3.up);
            BoxCollider floor = platform.AddComponent<BoxCollider>();
            floor.size = new Vector3(1.5f, 0.2f, 1.5f);
            floor.center = new Vector3(0f, -0.1f, 0f);
            // Un parapet invisible : on ne tombe pas en se retournant.
            AddRail(platform.transform, new Vector3(0f, 0.5f, 0.8f), new Vector3(1.6f, 1f, 0.1f));
            AddRail(platform.transform, new Vector3(0.8f, 0.5f, 0f), new Vector3(0.1f, 1f, 1.6f));
            AddRail(platform.transform, new Vector3(-0.8f, 0.5f, 0f), new Vector3(0.1f, 1f, 1.6f));
            Proto.BeginVisualOnly();
            Color wood = new Color(0.26f, 0.2f, 0.14f);
            for (int i = -1; i <= 1; i++)
            {
                GameObject plank = Proto.Cube(platform.transform, new Vector3(i * 0.45f, -0.08f, 0f), new Vector3(0.4f, 0.08f, 1.5f), wood, "Branche");
                plank.transform.localRotation = Quaternion.Euler(0f, i * 6f, 0f);
            }
            Proto.EndVisualOnly();
            perch = platform.transform;

            // Le chemin : le pied du tronc, puis tout droit le long de l'ecorce,
            // puis le rebord de la plate-forme.
            Vector3 hug = centre + toMe * (trunkRadius + 0.45f);
            StartClimb(true, transform.position,
                       new Vector3(hug.x, ground + 0.05f, hug.z),
                       new Vector3(hug.x, spot.y - 0.2f, hug.z),
                       spot + Vector3.up * 0.05f, giant > 0f ? 1.6f + perchHeight * 0.12f : 1.6f);
        }

        static void AddRail(Transform parent, Vector3 at, Vector3 size)
        {
            GameObject rail = new GameObject("Rambarde");
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = at;
            rail.AddComponent<BoxCollider>().size = size;
        }

        void ClimbDown()
        {
            float height = perch.position.y - Ground.Sample(perch.position.x, perch.position.z);
            Pulls = height > 8f ? Mathf.RoundToInt(height / 2f) : 5;
            Vector3 trunkSide = perch.position - perch.forward * 0.15f;
            Vector3 landing = Ground.Place(perch.position.x + perch.forward.x * 1.2f, perch.position.z + perch.forward.z * 1.2f, 0.1f);
            float ground = Ground.Sample(trunkSide.x, trunkSide.z);
            StartClimb(false, transform.position,
                       new Vector3(trunkSide.x, transform.position.y - 0.1f, trunkSide.z),
                       new Vector3(trunkSide.x, ground + 0.1f, trunkSide.z),
                       landing, height > 8f ? 1.1f + height * 0.06f : 1.1f);
        }

        void StartClimb(bool up, Vector3 start, Vector3 baseAt, Vector3 top, Vector3 end, float duration)
        {
            climbing = true;
            climbingUp = up;
            climbT = 0f;
            climbDuration = duration;
            climbStart = start;
            climbBase = baseAt;
            climbTop = top;
            climbEnd = end;
            pullsDone = 0;
            player.BeginScripted();
            Sfx.Rustle();
        }

        /// <summary>Un pas de l'animation. Trois temps : s'approcher, grimper, se poser.</summary>
        void Climb()
        {
            climbT = Mathf.Min(1f, climbT + Time.deltaTime / climbDuration);
            float t = climbT;
            Vector3 p;
            if (t < 0.15f)
            {
                p = Vector3.Lerp(climbStart, climbBase, Mathf.SmoothStep(0f, 1f, t / 0.15f));
            }
            else if (t < 0.88f)
            {
                // Les tractions : la hauteur avance par a-coups (vite pendant la
                // traction, presque rien entre deux), avec un leger balancement.
                float u = (t - 0.15f) / 0.73f;
                float steps = u * Pulls;
                float within = steps - Mathf.Floor(steps);
                float eased = (Mathf.Floor(steps) + Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(within * 1.6f))) / Pulls;
                p = Vector3.Lerp(climbBase, climbTop, Mathf.Clamp01(eased));
                Vector3 side = Vector3.Cross(Vector3.up, climbTop - climbEnd).normalized;
                p += side * Mathf.Sin(steps * Mathf.PI) * 0.06f;
                int pull = Mathf.FloorToInt(steps);
                if (pull > pullsDone && pull <= Pulls)
                {
                    pullsDone = pull;
                    Sfx.Rustle();
                    if (pull == 2) Sfx.Creak3D(p + Vector3.up);
                    if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.04f);
                }
            }
            else
            {
                p = Vector3.Lerp(climbTop, climbEnd, Mathf.SmoothStep(0f, 1f, (t - 0.88f) / 0.12f));
            }
            player.ScriptedMove(p);

            if (climbT < 1f) return;
            climbing = false;
            player.EndScripted(climbEnd);
            if (climbingUp)
            {
                Sfx.Step();
            }
            else
            {
                if (perch != null) Destroy(perch.gameObject);
                perch = null;
                Sfx.Step();
            }
        }
    }
}
