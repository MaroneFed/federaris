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
    }
}
