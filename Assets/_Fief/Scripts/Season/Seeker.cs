using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN JOUEUR dans la manche : toi, ou un bot (qui sera un joueur en ligne en Phase 3).
    ///
    /// PLUS DE VIE (27/09 : "pas d'epee, juste des capacites"). On ne meurt pas : on se
    /// fait POUSSER, PROJETER, ETOURDIR -- et lacher la Couronne. C'est Smash, pas Dark
    /// Souls : on perd sa place, jamais la partie.
    ///
    /// Tout ce qu'il subit est ici, dans une classe C# pure : ses etats (etourdi,
    /// ralenti, invisible), ses recharges. Ses CAPACITES viennent de sa place dans le
    /// match (PlayerSlot), qui, elle, traverse les manches. Le DON d'un sanctuaire ne
    /// dure que la manche.
    ///
    /// Body est la presence dans le monde : c'est le seul lien avec Unity.
    /// </summary>
    public class Seeker
    {
        public readonly PlayerSlot Slot;
        public readonly string Name;
        public readonly Color Colour;
        /// <summary>Le joueur de cette machine (les autres sont des bots, ou en ligne).</summary>
        public readonly bool IsPlayer;
        public Transform Body;

        public Seeker(PlayerSlot slot)
        {
            Slot = slot;
            Name = slot.Name;
            Colour = slot.Colour;
            IsPlayer = slot.IsLocal;
        }

        public int Index { get { return Slot.Index; } }

        /// <summary>
        /// (04/10, en ligne) Joue par UNE AUTRE MACHINE : ici, ce n'est qu'une marionnette qui suit
        /// ce que le reseau dit (Rival en mode Remote). Les coups qu'on lui donne partent chez
        /// lui (NetGame.RemoteHit) ; les obstacles d'ici l'ignorent (il a les siens).
        /// </summary>
        public bool Remote { get { return Slot.IsRemote; } }
        public bool Has(Ability a) { return Slot.Has(a) || HasGift && Gift == a; }

        // ------------------------------------------------------------------ les capacites

        /// <summary>Le don d'un sanctuaire : une capacite active, pour cette manche (il remplace le clic gauche).</summary>
        public Ability Gift;
        public bool HasGift;

        /// <summary>
        /// TA capacite du clic gauche : le don d'un sanctuaire s'il y en a un (pour la
        /// manche), sinon ton active. (29/09 : une seule active.)
        /// </summary>
        public bool HasActive { get { return HasGift || Slot.Actives.Count > 0; } }
        public Ability CurrentActive { get { return HasGift ? Gift : Slot.Actives.Count > 0 ? Slot.Actives[0] : Ability.Ruee; } }

        readonly Dictionary<Ability, float> readyAt = new Dictionary<Ability, float>();

        /// <summary>La capacite est-elle prete a l'instant "now" ?</summary>
        public bool Ready(Ability a, float now)
        {
            float t;
            return !readyAt.TryGetValue(a, out t) || now >= t;
        }

        /// <summary>0 : on vient de s'en servir ; 1 : prete.</summary>
        public float Ready01(Ability a, float now)
        {
            float t;
            if (!readyAt.TryGetValue(a, out t)) return 1f;
            float cd = CooldownOf(a);
            return cd <= 0f ? 1f : Mathf.Clamp01(1f - (t - now) / cd);
        }

        public float Remaining(Ability a, float now)
        {
            float t;
            return readyAt.TryGetValue(a, out t) ? Mathf.Max(0f, t - now) : 0f;
        }

        public float CooldownOf(Ability a)
        {
            return AbilityInfo.Cooldown(a) * (Has(Ability.Recharge) ? 0.67f : 1f);
        }

        /// <summary>Se servir d'une capacite : vrai si elle etait prete (elle repart en recharge).</summary>
        public bool TrySpend(Ability a, float now)
        {
            if (!Ready(a, now) || Stunned) return false;
            readyAt[a] = now + CooldownOf(a);
            return true;
        }

        /// <summary>Rendre une capacite tout de suite (quand elle n'a rien fait : un grappin dans le vide).</summary>
        public void Refund(Ability a) { readyAt.Remove(a); }

        // ------------------------------------------------------------------ la poussee

        public const float ShoveCooldown = 0.9f;
        public float ShoveReadyAt;
        /// <summary>Le prochain piqué d'aigle possible (voir Combat.DiveTarget).</summary>
        public float DiveReadyAt;

        // ------------------------------------------------------------------ les etats

        public float StunnedUntil = -1f;    // etourdi : ni bouger ni agir
        public float SlowUntil = -1f;       // givre
        public float HiddenUntil = -1f;     // voile : invisible
        /// <summary>Le dernier coup encaisse (la musique, l'ecran s'en servent).</summary>
        public float LastHurt = -99f;
        /// <summary>Qui l'a frappe en dernier (pour l'aura de qui l'a ejecte dans les nuages).</summary>
        public Seeker LastHitBy;
        /// <summary>Quand un JOUEUR l'a frappe pour la derniere fois (un obstacle ne l'efface pas : pousse sous un pendule, le KO est a toi).</summary>
        public float LastHitByAt = -99f;
        /// <summary>Le pique d'aigle en cours : sur qui, et jusqu'a quand (le porteur vise en est prevenu).</summary>
        public Seeker DiveTarget;
        public float DiveUntil = -1f;
        /// <summary>
        /// LA GRACE : protege (rien ne le projette, les Yeux l'ignorent). Au depart, apres
        /// un respawn, et une seconde et demie apres avoir vole la Couronne.
        /// </summary>
        public float GraceUntil = -1f;
        /// <summary>
        /// LE REPIT DES GARGOUILLES (02/10 -- "il a trois tirs sur lui qui font bam") :
        /// jusqu'a cet instant, aucune gargouille ne le vise. Pose apres chaque tir sur lui
        /// (9 s s'il est touche, 3 s s'il a esquive).
        /// </summary>
        public float EyeCalmUntil = -1f;
        /// <summary>
        /// LES AILES D'OR (28/09) : prises au sommet de la tour (les planeurs) ou au depart
        /// d'une arbaleste, gardees jusqu'au prochain atterrissage. Tout le monde plane ;
        /// avec elles, on plane plus vite et plus loin (voir Wings). Le Planeur (passif)
        /// les donne toujours.
        /// </summary>
        public bool HasWings;
        /// <summary>Quand il les a prises (on ne les perd pas en touchant le socle de l'arbaleste au depart).</summary>
        public float WingsAt = -99f;
        /// <summary>
        /// LE VOL LIBRE (04/10 -- Martin : "j'aimerais qu'on puisse voler a fond, comme on veut,
        /// mais seulement si on part d'une arbalete") : tire par une arbaleste, on VOLE pour de
        /// vrai -- on va ou l'on regarde, on monte comme on descend, sans tomber -- jusqu'a ce
        /// qu'on se pose. (Le sceau de la citadelle tient toujours : pas de raccourci vers la tour.)
        /// </summary>
        public bool FreeFlight;
        /// <summary>
        /// LA CHUTE (29/09 -- "les obstacles doivent nous faire retomber en bas de la
        /// tour") : frappe par un obstacle ou une gargouille sur la tour, on est jete
        /// hors de la rampe et les ailes restent FERMEES jusqu'a ce qu'on touche le sol.
        /// On retombe dans la cour, et on remonte.
        /// </summary>
        public float TumbleUntil = -1f;
        public float TumbleAt = -99f;
        public bool Tumbling { get { return Time.time < TumbleUntil; } }
        public void Tumble(float seconds) { TumbleAt = Time.time; TumbleUntil = Mathf.Max(TumbleUntil, Time.time + seconds); }
        /// <summary>Pose au sol : la chute est finie (pas dans la demi-seconde du coup, on est encore sur la rampe).</summary>
        public void Landed()
        {
            if (Tumbling && Time.time - TumbleAt > 0.5f) TumbleUntil = -1f;
            if (Launched && Time.time - LaunchAt > 0.3f) LaunchUntil = -1f;
        }
        /// <summary>
        /// PROJETE (01/10 -- Martin : "quand ca pousse, que ca pousse bien, pas un tout petit
        /// peu") : apres une poussee, l'elan ne se freine presque plus tant qu'on est en l'air
        /// -- on part en cloche d'une quinzaine de metres. Les ailes, elles, restent libres.
        /// </summary>
        public float LaunchUntil = -1f;
        public float LaunchAt = -99f;
        public bool Launched { get { return Time.time < LaunchUntil; } }
        public void Launch(float seconds) { LaunchAt = Time.time; LaunchUntil = Mathf.Max(LaunchUntil, Time.time + seconds); }
        /// <summary>
        /// L'ANNEAU DE VENT (03/10) : traverse en planant, il te propulse -- vers l'avant et vers
        /// le haut, pendant un peu plus d'une seconde (voir WindRing).
        /// </summary>
        public float BoostUntil = -1f;
        public WindRing LastRing;
        public float LastRingAt = -99f;
        /// <summary>Combien d'anneaux de la meme chaine il vient d'enfiler d'affilee (4 : l'enfilade).</summary>
        public int RingChain;
        /// <summary>Le dernier anneau qu'il a frole sans y entrer (le "pfff").</summary>
        public WindRing MissedRing;
        /// <summary>Tout le monde plane (28/09), sauf etourdi ou en pleine chute.</summary>
        public bool CanGlide { get { return !Stunned && !Tumbling && !Rooted && !Ballooned; } }
        public bool Graced { get { return Time.time < GraceUntil; } }
        /// <summary>
        /// Qui vient de perdre la Couronne ne la reprend pas tout de suite en retombant
        /// dessus (sinon on la "rattrape" d'office en volant dans la meme direction).
        /// </summary>
        public float CrownLockUntil = -1f;
        /// <summary>La Prise ferme a deja servi pour ce port de Couronne.</summary>
        public bool GripUsed;

        /// <summary>(05/10) GEANT jusqu'a cet instant (GiantAura) : on ne le bouge presque plus.</summary>
        public float GiantUntil = -1f;
        public bool Giant { get { return Time.time < GiantUntil; } }
        /// <summary>(05/10) VAMPIRE : il court plus vite jusqu'a cet instant (chaque coup donne).</summary>
        public float RushUntil = -1f;

        // (06/10 -- "une prison qui t'enchaine au sol dix secondes, des trucs de fou") : ce que
        // les capacites de fou font a leur cible. Poses par Combat.Afflict, jamais ailleurs.
        /// <summary>PRISON : enchaine au sol (ne bouge plus, ne saute plus, ne vole plus). Un coup le libere.</summary>
        public float RootedUntil = -1f;
        /// <summary>GLU : englue (tres lent, ne saute plus).</summary>
        public float GluedUntil = -1f;
        /// <summary>TETE A L'ENVERS : ses commandes sont inversees.</summary>
        public float InvertedUntil = -1f;
        /// <summary>MINI : minuscule, lent, et pousse deux fois plus loin.</summary>
        public float TinyUntil = -1f;
        /// <summary>ENCRE : l'ecran couvert d'encre (un bot, lui, avance au hasard).</summary>
        public float InkUntil = -1f;
        /// <summary>BALLON : il gonfle et s'envole, puis eclate.</summary>
        public float BalloonUntil = -1f;
        /// <summary>RAGE : combien de coups recus depuis sa derniere poussee (0 a 6).</summary>
        public int Rage;
        /// <summary>ANGE GARDIEN : deja servi cette manche.</summary>
        public bool AngelUsed;

        public bool Rooted { get { return Time.time < RootedUntil; } }
        public bool Glued { get { return Time.time < GluedUntil; } }
        public bool Inverted { get { return Time.time < InvertedUntil; } }
        public bool Tiny { get { return Time.time < TinyUntil; } }
        public bool Inked { get { return Time.time < InkUntil; } }
        public bool Ballooned { get { return Time.time < BalloonUntil; } }
        /// <summary>Ni saut ni second saut (enchaine, englue, en ballon).</summary>
        public bool NoJump { get { return Rooted || Glued || Ballooned; } }

        public bool Stunned { get { return Time.time < StunnedUntil; } }
        public bool Hidden { get { return Time.time < HiddenUntil; } }
        public bool Slowed { get { return Time.time < SlowUntil; } }

        /// <summary>Qui porte la Couronne la tient a deux mains : il ne pousse pas (sauf Porteur).</summary>
        public bool CarriesCrown { get { return Crown.Holder == this; } }
        public bool CanShove { get { return !Stunned && (!CarriesCrown || Has(Ability.Porteur)); } }

        /// <summary>Multiplicateur de vitesse : capacites, Couronne, givre, etourdissement.</summary>
        public float SpeedFactor
        {
            get
            {
                float f = Has(Ability.Coureur) ? 1.15f : 1f;
                if (CarriesCrown && !Has(Ability.Porteur)) f *= 0.85f;
                if (Time.time < RushUntil) f *= 1.3f;
                if (Slowed) f *= 0.5f;
                if (Tiny) f *= 0.75f;
                if (Glued) f *= 0.3f;
                if (Stunned || Rooted) f = 0f;
                return f;
            }
        }
    }
}
