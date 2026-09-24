using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Ce qu'un PNJ doit savoir dire pour qu'on puisse lui parler. Le panneau ne
    /// connait RIEN des personnages : il demande le texte et les choix, et rappelle
    /// Choose(i) quand on clique. Ajouter un PNJ = ecrire une classe qui repond a
    /// ces questions, sans toucher a l'interface.
    ///
    /// Concept C# : une INTERFACE est un contrat. "Je promets d'avoir ces methodes."
    /// Le panneau accepte n'importe quel objet qui tient la promesse -- le Veilleur,
    /// l'Ermite, ou un PNJ qui n'existe pas encore.
    /// </summary>
    public interface IDialogue
    {
        string Speaker { get; }
        Color Tint { get; }
        Transform Anchor { get; }

        /// <summary>Ce qu'il dit en ce moment. Relu a chaque image : il peut changer.</summary>
        string Body { get; }

        int ChoiceCount { get; }
        string ChoiceLabel(int index);
        bool ChoiceEnabled(int index);

        /// <summary>Renvoie vrai si ce choix met fin a la conversation.</summary>
        bool Choose(int index);
    }

    /// <summary>La fenetre de conversation : un nom, ce qu'il dit, des reponses.</summary>
    public class DialoguePanel : IPanel
    {
        const float Reach = 5.5f;
        readonly IDialogue who;

        public DialoguePanel(IDialogue who)
        {
            this.who = who;
        }

        public bool IsStillValid
        {
            get
            {
                if (who == null || who.Anchor == null || Game.PlayerTransform == null) return false;
                Vector3 d = Game.PlayerTransform.position - who.Anchor.position;
                d.y = 0f;
                return d.magnitude <= Reach;
            }
        }

        public void Draw()
        {
            float w = UiStyle.S(620);
            float h = UiStyle.S(250) + who.ChoiceCount * UiStyle.S(38);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - h - UiStyle.S(40), w, h);
            UiStyle.Frame(box);
            UiStyle.Fill(new Rect(box.x + UiStyle.S(10), box.y, box.width - UiStyle.S(20), 2f), who.Tint);

            float pad = UiStyle.S(22);
            float x = box.x + pad;
            float inner = w - pad * 2f;
            float y = box.y + UiStyle.S(16);

            UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(30)), who.Speaker, UiStyle.Head, who.Tint);
            y += UiStyle.S(34);

            GUIStyle body = UiStyle.Label;
            bool wrap = body.wordWrap;
            body.wordWrap = true;
            float textH = UiStyle.S(150);
            GUI.Label(new Rect(x, y, inner, textH), who.Body, body);
            body.wordWrap = wrap;
            y += textH + UiStyle.S(8);

            float bh = UiStyle.S(32);
            for (int i = 0; i < who.ChoiceCount; i++)
            {
                GUI.enabled = who.ChoiceEnabled(i);
                if (GUI.Button(new Rect(x, y, inner, bh), who.ChoiceLabel(i), UiStyle.ButtonGhost))
                {
                    Sfx.Pop();
                    if (who.Choose(i) && Game.Hud != null) Game.Hud.ClosePanel();
                }
                GUI.enabled = true;
                y += bh + UiStyle.S(6);
            }
        }
    }
}
