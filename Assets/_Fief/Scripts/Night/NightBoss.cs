using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ABBE, le boss de la crypte (refait le 22/11 — Martin : "le duel du gros monstre est nul").
    ///
    /// Quand on descend derriere l'autel, le mur remonte : on est enferme avec lui. Il fait 4,6 m, il est noir, la nuque
    /// cassee, deux braises pour yeux. Pour sortir, il faut que les QUATRE brasiers des coins brulent EN MEME TEMPS.
    /// Le combat a trois temps, qui changent a chaque brasier allume :
    ///
    ///   1. LA TRAQUE      il marche vers toi, sans jamais s'arreter. Il ne peut pas entrer dans la lumiere d'un brasier.
    ///   2. LES AMES       (un brasier brule) il hurle et fait sortir deux Ames des gisants ; il marche plus vite.
    ///   3. LE SOUFFLE     (deux brasiers) toutes les vingt secondes, il ETEINT un brasier allume d'un revers de bras :
    ///                     il faut y retourner.
    ///   4. LA CHARGE      (trois brasiers) il recule, gronde... et charge en ligne droite. Ecarte-toi.
    ///
    /// Ta torche (clic gauche) le fait reculer deux secondes : ca coute de la flamme, mais ca sauve. Au quatrieme brasier,
    /// il brule et s'enfonce dans le sol.
    ///
    /// Concept Unity : une "machine a etats" : selon un nombre (ici combien de brasiers brulent), le meme Update fait
    /// des choses differentes.
    /// </summary>
    public class Abbot : MonoBehaviour
    {
        const float Reach = 1.9f, SafeRadius = 3.4f;
        NightPlayer prey;
        Material skin;
        Light glow;
        CreepyRig rig;
        Animation anim;
        float speed = 2.1f, stagger, step, burn = -1f, growl, snuffTimer = 20f, chargeTimer = 6f, chargeTime, windup;
        int lit;
        Vector3 chargeDir;
        public bool Active;
        public bool Burnt { get { return burn >= 3f; } }

        public bool Touching
        {
            get
            {
                if (!Active || burn >= 0f || prey == null || stagger > 0f) return false;
                Vector3 d = prey.transform.position - transform.position;
                d.y = 0f;
                return d.magnitude < Reach;
            }
        }

        public static Abbot Spawn(Transform parent, Vector3 at, NightPlayer prey)
        {
            GameObject go = new GameObject("L'ABBÉ");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Abbot a = go.AddComponent<Abbot>();
            a.prey = prey;
            Transform body = new GameObject("Corps").transform;
            body.SetParent(go.transform, false);
            body.localScale = new Vector3(0.72f, 1.35f, 0.72f);
            GameObject model = Puppet.Spawn("Modeles/Monstres/Monstre_Demon", body, 4.6f);
            if (model != null)
            {
                a.anim = model.GetComponentInChildren<Animation>();
                a.skin = Puppet.Silhouette(model, 0.01f);
                a.rig = CreepyRig.Add(model, go.transform, prey.transform, 62f, 30f);
                Puppet.Play(a.anim, 0.7f, false, "|Walk");
            }
            else
            {
                GameObject statue = NightKit.Model("penitent_3", go.transform);
                if (statue != null) statue.transform.localScale = Vector3.one * 1.65f;
            }
            NightKit.Smoke(go.transform, 4.6f);
            NightKit.Smoke(go.transform, 2.4f);
            a.glow = new GameObject("Braise de l'Abbé").AddComponent<Light>();
            a.glow.transform.SetParent(go.transform, false);
            a.glow.transform.localPosition = new Vector3(0f, 3.8f, 0.6f);
            a.glow.type = LightType.Point;
            a.glow.color = new Color(1f, 0.12f, 0.05f);
            a.glow.range = 8f;
            a.glow.intensity = 1.6f;
            a.Active = true;
            NightAudio.Roar(at + Vector3.up * 3f, 1f);
            return a;
        }

        /// <summary>Un brasier vient de s'allumer : il recule, il hurle, et le combat change.</summary>
        public void Stagger(int count)
        {
            lit = count;
            stagger = 2.4f;
            speed = 2.1f + 0.5f * count;
            NightAudio.Sting(1f);
            NightAudio.Roar(transform.position + Vector3.up * 3f, 1f);
            NightPlayer.Shake = Mathf.Max(NightPlayer.Shake, 0.8f);
            NightPlayer.Punch = 0.8f;
            // Le deuxieme temps : deux Ames sortent des gisants.
            if (count == 1 || count == 3)
                for (int i = 0; i < 2; i++)
                {
                    Vector3 p = NightMap.Center('R') + new Vector3(i == 0 ? -1.5f : 1.5f, 0.4f, i == 0 ? -2.6f : 2.6f);
                    try { Beast.Spawn(transform.parent, Beast.Kind.Soul, p, prey); } catch (System.Exception) { }
                }
        }

        /// <summary>Un coup de torche : il recule deux secondes en hurlant.</summary>
        public void Hit(Vector3 from)
        {
            if (burn >= 0f) return;
            stagger = Mathf.Max(stagger, 2f);
            chargeTime = 0f; windup = 0f;
            Vector3 away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(-away.normalized, Vector3.up);
            NightAudio.Scream(transform.position + Vector3.up * 3f, 1f);
        }

        /// <summary>Le quatrieme brasier : il brule et s'effondre dans le sol.</summary>
        public void Burn()
        {
            if (burn >= 0f) return;
            burn = 0f;
            NightAudio.Roar(transform.position + Vector3.up * 3f, 1f);
            NightAudio.Bang(transform.position + Vector3.up * 2f, 1f);
        }

        public void SendHome(Vector3 home) { transform.position = home; stagger = 1.5f; chargeTime = 0f; windup = 0f; }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        bool InLight(Vector3 p)
        {
            foreach (GameObject f in NightMap.CryptFires)
                if (f != null && f.activeInHierarchy && Flat(p - f.transform.position).magnitude < SafeRadius) return true;
            return false;
        }

        void Update()
        {
            if (!Active || prey == null) return;
            float dt = Time.deltaTime;
            float dist = Flat(prey.transform.position - transform.position).magnitude;
            if (rig != null) rig.Shiver = Mathf.InverseLerp(10f, 2f, dist);
            if (skin != null && skin.HasProperty("_EmissionColor"))
                skin.SetColor("_EmissionColor", burn >= 0f ? new Color(1f, 0.15f, 0.03f) * 5f * (1f - burn / 3f) : Color.black);
            if (burn >= 0f)
            {
                // Il brule : il tremble, il s'enfonce, sa braise grossit puis s'eteint.
                burn += dt;
                transform.position += Vector3.down * dt * 1.6f + Random.insideUnitSphere * 0.04f;
                if (glow != null) { glow.intensity = 7f * Mathf.Clamp01(1f - burn / 3f); glow.range = 14f; }
                if (burn >= 3f) { Active = false; gameObject.SetActive(false); }
                return;
            }
            if (stagger > 0f)
            {
                stagger -= dt;
                transform.position -= transform.forward * dt * 1.4f;
                Puppet.Play(anim, 1.4f, false, "|HitReact;|Idle");
                return;
            }

            // Le souffle (deux brasiers ou plus) : il eteint un brasier allume, au loin, d'un geste.
            if (lit >= 2)
            {
                snuffTimer -= dt;
                if (snuffTimer <= 0f)
                {
                    snuffTimer = 20f;
                    foreach (GameObject f in NightMap.CryptFires)
                        if (f != null && f.activeSelf)
                        {
                            f.SetActive(false);
                            lit = Mathf.Max(0, lit - 1);
                            NightAudio.Roar(transform.position + Vector3.up * 3f, 1f);
                            NightAudio.Bang(f.transform.position + Vector3.up, 0.8f);
                            Abbot.Snuffed = true;
                            break;
                        }
                }
            }

            Vector3 to = Flat(prey.transform.position - transform.position);
            // La charge (trois brasiers) : il recule, gronde une seconde... et fonce tout droit.
            if (lit >= 3)
            {
                if (chargeTime > 0f)
                {
                    chargeTime -= dt;
                    Vector3 n = transform.position + chargeDir * 11f * dt;
                    if (InLight(n) || Physics.Linecast(transform.position + Vector3.up, n + Vector3.up + chargeDir, ~(1 << 2), QueryTriggerInteraction.Ignore))
                    {
                        chargeTime = 0f; stagger = 2.5f;
                        NightAudio.Bang(transform.position + Vector3.up * 2f, 1f);
                        NightPlayer.Shake = 1f;
                        return;
                    }
                    transform.position = n;
                    Puppet.Play(anim, 1.8f, false, "|Run");
                    return;
                }
                if (windup > 0f)
                {
                    windup -= dt;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 200f * dt);
                    if (windup <= 0f) { chargeDir = to.normalized; chargeTime = 1.6f; NightAudio.Scream(transform.position + Vector3.up * 3f, 1f); }
                    Puppet.Play(anim, 0.3f, false, "|Idle");
                    return;
                }
                chargeTimer -= dt;
                if (chargeTimer <= 0f && dist > 4f && dist < 16f) { chargeTimer = Random.Range(6f, 9f); windup = 1.1f; NightAudio.Roar(transform.position + Vector3.up * 3f, 1f); return; }
            }

            if (to.magnitude < 0.05f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 120f * dt);
            Vector3 next = transform.position + to.normalized * speed * dt;
            // La lumiere d'un brasier allume : il ne peut pas y entrer.
            if (InLight(next))
            {
                growl -= dt;
                if (growl <= 0f) { growl = 3f; NightAudio.Growl(transform.position + Vector3.up * 3f, 0.9f); }
                Puppet.Play(anim, 0.4f, false, "|Idle");
                return;
            }
            transform.position = next;
            Puppet.Play(anim, 0.6f + 0.15f * speed, false, "|Walk");
            step -= dt;
            if (step <= 0f) { step = 1.1f; NightAudio.Bang(transform.position, 0.45f); NightPlayer.Shake = Mathf.Max(NightPlayer.Shake, 0.25f); }
        }

        /// <summary>Il vient d'eteindre un brasier (NightBootstrap le dit au joueur).</summary>
        public static bool Snuffed;
    }
}
