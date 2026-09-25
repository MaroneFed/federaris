using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les deux gestes qui font de la foret TON territoire :
    ///   C  -- planter le camp (une seule fois par Saison, là où l'on se tient) ;
    ///   G  -- creuser une cache (maintenir, trois au maximum).
    ///
    /// Pourquoi C ne se maintient pas et G si : planter le camp est une DECISION,
    /// on la prend d'un coup. Creuser est un TRAVAIL : on reste plante la, a genoux,
    /// pendant plusieurs secondes, et plus le sac est lourd plus c'est long (la meme
    /// regle que la recolte). En Phase 2, c'est le moment ou l'on est vulnerable.
    ///
    /// Ce composant ne garde rien : il demande a Hoard (TryPlantCamp, TryDig), puis
    /// pose l'objet du monde qui represente la reponse.
    /// </summary>
    public class CampActions : MonoBehaviour
    {
        const float TentAhead = 4f;         // la tente se pose devant soi, pas sur soi
        const float CacheAhead = 1.2f;
        const float MaxSlope = 0.42f;
        const float MinCacheSpacing = 4f;
        const float WanderLimit = 0.7f;     // bouger de plus que ca annule le creusage

        PlayerController player;
        float digTimer;
        float swingTimer;
        Vector3 digStart;

        /// <summary>Le HUD lit ces deux valeurs pour dessiner la jauge de creusage.</summary>
        public static bool Digging { get; private set; }
        public static float Progress01 { get; private set; }

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        void OnDestroy()
        {
            Digging = false;
            Progress01 = 0f;
        }

        void Update()
        {
            bool locked = player != null && player.InputLocked;
            if (locked)
            {
                StopDigging();
                return;
            }

            if (FiefInput.CampPressed) TryCamp();
            if (FiefInput.ListenPressed) Listen();

            if (FiefInput.DigPressed) BeginDigging();
            if (Digging) ContinueDigging();
        }

        // ------------------------------------------------------------------ le camp

        void TryCamp()
        {
            Hoard hoard = Game.Hoard;
            if (hoard == null) return;

            if (hoard.CampPlanted)
            {
                Refuse("Ton camp est déjà plante, " + Hud.Direction(transform.position, hoard.CampPosition) + ".");
                return;
            }

            Vector3 forward = Facing();
            Vector3 at = Ground.Place(transform.position + forward * TentAhead, 0f);

            string why = WhyNotHere(at, 12f);
            if (why == null && Blocked(at, forward)) why = "Pas la place ici pour une tente.";
            if (why != null) { Refuse(why); return; }

            if (!hoard.TryPlantCamp(at)) return;

            // La tente tourne le dos a celui qui la plante : le feu est entre elle et lui.
            float yaw = Mathf.Atan2(-forward.x, -forward.z) * Mathf.Rad2Deg;
            Camp.Build(at, yaw);

            Sfx.Build();
            Toasts.Show("Camp planté.", Palette.Gold);
        }

        /// <summary>
        /// Une tente de 2,2 x 2,6 m ne doit rien traverser : ni tronc, ni rocher, ni
        /// mur. On interroge la physique sur son volume, en ignorant le sol (le seul
        /// MeshCollider du monde), les declencheurs, et le joueur lui-meme.
        /// </summary>
        bool Blocked(Vector3 at, Vector3 forward)
        {
            return BlockedAt(at, forward, new Vector3(1.3f, 0.7f, 1.5f));
        }

        bool BlockedAt(Vector3 at, Vector3 forward, Vector3 halfExtents)
        {
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Collider[] hits = Physics.OverlapBox(at + Vector3.up * 1f, halfExtents, rotation,
                                                 ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c is MeshCollider) continue;
                if (c.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ ecouter

        static float listenReady;

        /// <summary>
        /// H : TENDRE L'OREILLE. Ta stele repond : trois notes claires, jouees depuis
        /// elle, qu'on entend a quatre cents metres. On sait de quel cote elle est --
        /// pas a quelle distance, pas par ou passer. Une fois par minute.
        /// C'est le filet de securite de la boussole vide : on peut se perdre, pas
        /// pour toujours.
        /// </summary>
        void Listen()
        {
            Stele stele = Stele.Of(Game.Me);
            if (stele == null) return;
            if (Time.time < listenReady)
            {
                Refuse("Ta stèle se tait encore " + Mathf.CeilToInt(listenReady - Time.time) + " s.");
                return;
            }
            listenReady = Time.time + 60f;
            GameObject go = new GameObject("Appel de la stèle");
            go.transform.position = stele.transform.position + Vector3.up * 1.5f;
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = Sfx.SteleCall();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 30f;
            src.maxDistance = 450f;
            src.dopplerLevel = 0f;
            src.volume = Sfx.Muted ? 0f : 1f;
            src.Play();
            Destroy(go, src.clip.length + 0.2f);
            OrbitCamera.Crouch = Mathf.Max(OrbitCamera.Crouch, 0.3f);
            Toasts.Show("Tu tends l'oreille... ta stèle répond, quelque part.", Stele.RuneBlue);
        }

        // ------------------------------------------------------------------ les caches

        void BeginDigging()
        {
            if (Digging || Game.Hoard == null) return;

            Vector3 at = CacheSpotAhead();
            string why = Game.Hoard.CanDig ? WhyNotHere(at, 4f) : "Tu as déjà creuse tes " + Game.Hoard.MaxCaches + " caches.";
            if (why == null) why = TooClose(at);
            if (why != null) { Refuse(why); return; }

            Digging = true;
            digTimer = 0f;
            swingTimer = 0f;
            digStart = transform.position;
        }

        void ContinueDigging()
        {
            if (!FiefInput.DigHeld || Flat(transform.position - digStart).magnitude > WanderLimit)
            {
                StopDigging();
                return;
            }

            // Un coup de pelle toutes les 0,6 s : le bras bouge, la terre sonne.
            swingTimer -= Time.deltaTime;
            if (swingTimer <= 0f)
            {
                swingTimer = 0.6f;
                if (Game.Rig != null) Game.Rig.PlaySwing();
                Sfx.HarvestTap(ResourceType.Moonstone);
            }

            OrbitCamera.Crouch = Mathf.Max(OrbitCamera.Crouch, 1f);     // on creuse a genoux
            float duration = DigDuration();
            digTimer += Time.deltaTime;
            Progress01 = Mathf.Clamp01(digTimer / duration);
            if (digTimer < duration) return;

            StopDigging();
            Vector3 at = CacheSpotAhead();
            Cache cache = Game.Hoard.TryDig(at);
            if (cache == null) return;

            CacheSpot.Build(cache, Random.Range(0f, 360f));
            Sfx.Harvest(ResourceType.Moonstone);
            int left = Game.Hoard.MaxCaches - Game.Hoard.Caches.Count;
            Toasts.Show("Cache " + cache.Number + " creusée. Toi seul sais qu'elle est là."
                        + (left > 0 ? "  (encore " + left + ")" : "  (c'était la dernière)"),
                        new Color(0.80f, 0.66f, 0.46f));
        }

        /// <summary>Meme regle que la recolte : un sac lourd rend le geste lent.</summary>
        float DigDuration()
        {
            GameConfig cfg = Game.Config;
            float baseDuration = cfg != null ? cfg.digDuration : 3.5f;
            float penalty = cfg != null ? cfg.actionPenaltyFull : 2.4f;
            if (Game.Brewed) penalty = 1f;          // l'infusion de l'Ermite
            float load = Game.Inventory != null ? Game.Inventory.Load01 : 0f;
            float duration = baseDuration * Mathf.Lerp(1f, penalty, load);
            // La Pelle d'os creuse trois fois plus vite.
            if (Game.Hoard != null && Game.Hoard.Has(Talisman.Pelle)) duration /= TalismanInfo.PelleSpeed;
            return duration;
        }

        void StopDigging()
        {
            Digging = false;
            Progress01 = 0f;
            digTimer = 0f;
        }

        Vector3 CacheSpotAhead()
        {
            return Ground.Place(transform.position + Facing() * CacheAhead, 0f);
        }

        string TooClose(Vector3 at)
        {
            Hoard hoard = Game.Hoard;
            for (int i = 0; i < hoard.Caches.Count; i++)
                if (Flat(hoard.Caches[i].Position - at).magnitude < MinCacheSpacing)
                    return "Trop près de ta cache " + hoard.Caches[i].Number + ".";
            if (hoard.CampPlanted && Flat(hoard.CampPosition - at).magnitude < MinCacheSpacing)
                return "Trop près de ta tente.";
            return null;
        }

        // ------------------------------------------------------------------ commun

        /// <summary>Null si l'endroit convient, sinon la raison, dite au joueur.</summary>
        static string WhyNotHere(Vector3 at, float castleMargin)
        {
            if (Castle.Covers(at.x, at.z, castleMargin)) return "Pas au pied du château : on te verrait.";
            if (Ground.Slope(at.x, at.z) > MaxSlope) return "Le sol est trop en pente ici.";
            return null;
        }

        static void Refuse(string why)
        {
            Sfx.Deny();
            Toasts.Show(why, UiStyle.InkDim);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        /// <summary>Le regard du corps, a plat et de longueur 1.</summary>
        Vector3 Facing()
        {
            Vector3 f = Flat(transform.forward);
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }
    }
}
