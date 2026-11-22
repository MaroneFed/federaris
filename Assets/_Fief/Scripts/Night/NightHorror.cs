using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// (22/11) LES ANIMATIONS QUI FONT PEUR. Martin : "fais des animations flippantes pour les monstres". Les animations du
    /// pack (marcher, courir) sont faites pour des personnages gentils. Ce composant passe APRES elles a chaque image et
    /// tord le squelette par-dessus :
    ///
    ///   la nuque cassee   la tete penchee sur l'epaule (40 a 70 degres), comme si le cou ne tenait plus ;
    ///   le regard         la tete se tourne TOUJOURS vers toi, plus loin que ne le permettrait un cou humain ;
    ///   le craquement     de temps en temps, la tete se redresse ou bascule de l'autre cote, d'un coup, en un eclair ;
    ///   le dos            voute vers l'avant, les epaules basses ;
    ///   les bras          qui pendent, trop longs, trop lourds ;
    ///   le tremblement    tout le corps vibre tres legerement quand il est pres de toi.
    ///
    /// Concept Unity : LateUpdate est appele APRES que le composant Animation a pose les os pour cette image : ce qu'on
    /// change ici s'ajoute a l'animation au lieu d'etre efface par elle. On retrouve les os par leur nom ("Head", "Neck"...).
    /// </summary>
    public class CreepyRig : MonoBehaviour
    {
        public Transform Owner;        // le corps qui marche (sa direction "devant")
        public Transform Target;       // ce qu'il fixe (toi)
        public float Tilt = 55f;       // la nuque cassee
        public float Hunch = 22f;      // le dos voute
        public float Hang = 28f;       // les bras qui pendent
        public float Shiver;           // de 0 a 1 : le tremblement (pres de toi)

        Transform head, neck;
        readonly List<Transform> spine = new List<Transform>();
        readonly List<Transform> arms = new List<Transform>();
        float roll, rollTarget, nextSnap, look;

        public static CreepyRig Add(GameObject model, Transform owner, Transform target, float tilt, float hunch)
        {
            if (model == null) return null;
            CreepyRig c = model.AddComponent<CreepyRig>();
            c.Owner = owner; c.Target = target; c.Tilt = tilt; c.Hunch = hunch;
            c.Find();
            c.rollTarget = c.roll = (Random.value < 0.5f ? -1f : 1f) * tilt;
            c.nextSnap = Random.Range(2f, 6f);
            return c;
        }

        void Find()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                int bar = n.LastIndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                if (head == null && (n == "head" || n.EndsWith("_head") || n.StartsWith("head"))) head = t;
                else if (neck == null && n.StartsWith("neck")) neck = t;
                else if (n.StartsWith("spine") || n == "chest" || n == "body" || n.StartsWith("torso")) spine.Add(t);
                else if (n.Contains("upperarm") || n.StartsWith("shoulder") || n.Contains("arm.l") || n.Contains("arm.r") || n == "upperarm.l" || n == "upperarm.r") { if (arms.Count < 2) arms.Add(t); }
            }
        }

        void LateUpdate()
        {
            if (Owner == null) return;
            float dt = Time.deltaTime;
            Vector3 fwd = Owner.forward, right = Owner.right;

            // Le dos voute, les epaules basses.
            if (spine.Count > 0)
            {
                float each = Hunch / spine.Count;
                for (int i = 0; i < spine.Count; i++) spine[i].rotation = Quaternion.AngleAxis(each, right) * spine[i].rotation;
            }
            // Les bras qui pendent, un peu ecartes vers l'avant.
            for (int i = 0; i < arms.Count; i++)
            {
                float side = Vector3.Dot(arms[i].position - Owner.position, right) >= 0f ? 1f : -1f;
                arms[i].rotation = Quaternion.AngleAxis(-side * Hang, fwd) * arms[i].rotation;
            }
            if (head == null) return;

            // Le craquement : la nuque bascule d'un coup d'un cote a l'autre, ou se redresse un instant.
            nextSnap -= dt;
            if (nextSnap <= 0f)
            {
                nextSnap = Random.Range(1.5f, 5f) * (1f - 0.6f * Shiver);
                float pick = Random.value;
                rollTarget = pick < 0.4f ? -rollTarget : pick < 0.6f ? rollTarget * 0.15f : (Random.value < 0.5f ? -1f : 1f) * Tilt * Random.Range(0.8f, 1.3f);
            }
            roll = Mathf.MoveTowards(roll, rollTarget, dt * 600f);      // un eclair : 600 degres par seconde

            // Le regard : la tete se tourne vers toi, bien plus loin qu'un cou humain (jusqu'a 120 degres).
            Quaternion delta = Quaternion.identity;
            if (Target != null)
            {
                Vector3 to = (Target.position + Vector3.up * 1.5f) - head.position;
                if (to.sqrMagnitude > 0.01f)
                {
                    Vector3 flatTo = Vector3.ProjectOnPlane(to, Vector3.up).normalized;
                    float yaw = Vector3.SignedAngle(fwd, flatTo, Vector3.up);
                    look = Mathf.MoveTowards(look, Mathf.Clamp(yaw, -120f, 120f), dt * 260f);
                    delta = Quaternion.AngleAxis(look, Vector3.up);
                }
            }
            Vector3 lookDir = delta * fwd;
            Quaternion bend = Quaternion.AngleAxis(roll, lookDir) * delta;
            if (neck != null) neck.rotation = Quaternion.Slerp(Quaternion.identity, bend, 0.35f) * neck.rotation;
            head.rotation = Quaternion.Slerp(Quaternion.identity, bend, neck != null ? 0.65f : 1f) * head.rotation;
            // Le tremblement, pres de toi.
            if (Shiver > 0.01f) head.rotation = Quaternion.Euler(Random.insideUnitSphere * 6f * Shiver) * head.rotation;
        }
    }
}
