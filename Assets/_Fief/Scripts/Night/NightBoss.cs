using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ABBE (21/11) : le boss, dans la crypte. Martin : "des boss, rendre le truc beaucoup plus grand".
    ///
    /// Quand on descend derriere l'autel, le mur remonte : on est enferme avec lui. Il est immense (4,5 m), noir,
    /// et lui, il ne s'arrete JAMAIS, qu'on le regarde ou non. Il marche moins vite que toi : tu peux lui echapper,
    /// tourner autour des piliers. Mais pour sortir, il faut rallumer les quatre brasiers des coins (le geste du
    /// feu, trois etincelles, pendant qu'il approche). Un brasier allume : il recule en hurlant, et il ne peut plus
    /// entrer dans sa lumiere. A chaque brasier il devient plus rapide. Au quatrieme, il brule.
    ///
    /// Concept Unity : localScale grossit un modele (ici 1,65 fois) sans le refaire.
    /// </summary>
    public class Abbot : MonoBehaviour
    {
        const float Reach = 1.6f, SafeRadius = 3.4f;
        NightPlayer prey;
        Material skin;
        Light glow;
        float speed = 1.9f, stagger, step, burn = -1f, growl;
        public bool Active;
        public bool Burnt { get { return burn >= 3f; } }

        public bool Touching
        {
            get
            {
                if (!Active || burn >= 0f || prey == null) return false;
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
            GameObject model = NightKit.Model("penitent_3", go.transform);
            if (model != null)
            {
                model.transform.localScale = Vector3.one * 1.65f;
                a.skin = NightKit.Shadow();
                if (a.skin != null)
                    foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = a.skin;
            }
            NightKit.Smoke(go.transform, 4.4f);
            a.glow = new GameObject("Braise de l'Abbé").AddComponent<Light>();
            a.glow.transform.SetParent(go.transform, false);
            a.glow.transform.localPosition = new Vector3(0f, 3.4f, 0.6f);
            a.glow.type = LightType.Point;
            a.glow.color = new Color(1f, 0.12f, 0.05f);
            a.glow.range = 7f;
            a.glow.intensity = 1.4f;
            a.Active = true;
            return a;
        }

        /// <summary>Un brasier vient de s'allumer : il recule, il hurle, il repart plus vite.</summary>
        public void Stagger(int lit)
        {
            stagger = 2.2f;
            speed = 1.9f + 0.45f * lit;
            NightAudio.Sting(1f);
            NightAudio.Scream(transform.position + Vector3.up * 3f, 1f);
        }

        /// <summary>Le quatrieme brasier : il brule et s'effondre dans le sol.</summary>
        public void Burn()
        {
            if (burn >= 0f) return;
            burn = 0f;
            NightAudio.Sting(1f);
            NightAudio.Bang(transform.position + Vector3.up * 2f, 1f);
        }

        public void SendHome(Vector3 home) { transform.position = home; stagger = 1.5f; }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void Update()
        {
            if (!Active || prey == null) return;
            float dt = Time.deltaTime;
            if (skin != null)
                skin.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.03f) * (burn >= 0f ? 6f * (1f - burn / 3f) : 2.4f + Mathf.Sin(Time.time * 5f)));
            if (burn >= 0f)
            {
                // Il brule : il tremble, il s'enfonce, sa braise grossit puis s'eteint.
                burn += dt;
                transform.position += Vector3.down * dt * 1.6f;
                transform.localScale = Vector3.one * (1f - burn / 3.4f);
                if (glow != null) glow.intensity = 6f * Mathf.Clamp01(1f - burn / 3f);
                if (burn >= 3f) { Active = false; gameObject.SetActive(false); }
                return;
            }
            if (stagger > 0f)
            {
                stagger -= dt;
                transform.position -= transform.forward * dt * 1.2f;
                return;
            }
            Vector3 to = Flat(prey.transform.position - transform.position);
            if (to.magnitude < 0.05f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 120f * dt);
            Vector3 next = transform.position + to.normalized * speed * dt;
            // La lumiere d'un brasier allume : il ne peut pas y entrer.
            foreach (GameObject f in NightMap.CryptFires)
            {
                if (f == null || !f.activeInHierarchy) continue;
                if (Flat(next - f.transform.position).magnitude < SafeRadius)
                {
                    growl -= dt;
                    if (growl <= 0f) { growl = 2.5f; NightAudio.Bang(transform.position + Vector3.up * 2f, 0.5f); }
                    return;
                }
            }
            transform.position = next;
            step -= dt;
            if (step <= 0f) { step = 1.1f; NightAudio.Bang(transform.position, 0.45f); NightPlayer.Shake = Mathf.Max(NightPlayer.Shake, 0.25f); }
        }
    }
}
