using UnityEngine;

namespace Fief
{
    /// <summary>
    /// DANSER SUR B (v44 -- Martin : "rajoute aussi des danses, s'il te plait, avec la musique,
    /// quand on appuie sur B").
    ///
    /// B : ton haricot danse. La camera sort de tes yeux et se place devant toi (tu peux tourner
    /// autour avec la souris), et la MUSIQUE DE DANSE part, chez toi (MusicDirector) : chaque pas
    /// tombe sur le temps. Chaque nouvel appui sur B change de danse -- les six du vainqueur, puis
    /// le dab, le moonwalk et le robot (CharacterRig.Party). Bouger, sauter, pousser, lancer, se
    /// faire frapper ou quitter le sol : la danse s'arrete et tu retrouves tes yeux.
    ///
    /// Les autres te voient danser, en ligne aussi : la danse part avec ta position (les bits
    /// 8 a 11 des drapeaux, NetGame.CommonFlags).
    /// </summary>
    public static class Emote
    {
        /// <summary>Ta danse en cours (-1 : tu ne danses pas).</summary>
        public static int Current { get { return current; } }
        static int current = -1;
        public static bool Dancing { get { return Current >= 0; } }
        static int next;

        /// <summary>A chaque image, avant que le joueur ne bouge (PlayerController.Update).</summary>
        public static void Tick(PlayerController pc, Seeker me)
        {
            bool playing = Game.Season != null && Game.Season.Running && Game.Menus != null && Game.Menus.Current == Menus.State.Playing;
            if (me == null || pc == null || !playing) { Stop(pc, me); return; }
            if (Dancing)
            {
                bool moved = FiefInput.Move.sqrMagnitude > 0.04f || FiefInput.JumpPressed || FiefInput.PushPressed || FiefInput.CastPressed(0);
                if (moved || pc.Airborne || me.Tumbling || me.Stunned || me.Rooted) { Stop(pc, me); return; }
            }
            if (!FiefInput.DancePressed || pc.InputLocked || pc.Airborne || me.Tumbling || me.Stunned) return;
            // Un appui : on commence ; un autre : la danse suivante.
            int move = Dancing ? (Current + 1) % CharacterRig.DanceCount : next;
            Start(pc, me, move);
        }

        static void Start(PlayerController pc, Seeker me, int move)
        {
            bool fresh = !Dancing;
            current = move;
            next = (move + 1) % CharacterRig.DanceCount;
            me.Dance = move;
            CharacterRig rig = Game.Rig;
            if (rig != null) rig.SetEmote(move);
            OrbitCamera cam = pc.orbitCamera;
            if (cam != null && fresh)
            {
                // La camera passe devant toi, un peu au-dessus : on voit le haricot et ses yeux.
                cam.target = pc.transform;
                cam.yaw = pc.transform.eulerAngles.y + 180f;
                cam.pitch = 10f;
                cam.SetCinematic(4.8f, 10f);
            }
            Sfx.Pop();
        }

        /// <summary>Arreter de danser (et revenir dans tes yeux, tourne comme ton corps).</summary>
        public static void Stop(PlayerController pc, Seeker me)
        {
            if (!Dancing) return;
            current = -1;
            if (me != null) me.Dance = -1;
            CharacterRig rig = Game.Rig;
            if (rig != null) rig.SetEmote(-1);
            OrbitCamera cam = pc != null ? pc.orbitCamera : null;
            if (cam != null)
            {
                cam.ReleaseCinematic();
                cam.yaw = pc.transform.eulerAngles.y;
                cam.pitch = 4f;
            }
        }
    }
}
