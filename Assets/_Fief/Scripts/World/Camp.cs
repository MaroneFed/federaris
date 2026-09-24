using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CAMP : une tente, une couverture, un feu eteint. Plante une seule fois par
    /// Saison, avec la touche C, là où l'on se tient.
    ///
    /// La tente est un depot : on y laisse ce qu'on ne peut plus porter. Plus grande
    /// qu'une cache (60 kg contre 40), mais elle se VOIT -- en Phase 2, c'est la
    /// premiere chose qu'un rival pillera s'il tombe dessus.
    ///
    /// Concept Unity : ce MonoBehaviour ne garde RIEN. Le contenu de la tente vit dans
    /// Hoard.CampStash, une classe C# pure. L'objet du monde n'est qu'une facade qu'on
    /// peut detruire, deplacer, ou dupliquer pour un autre joueur sans toucher a la
    /// regle.
    /// </summary>
    public class Camp : MonoBehaviour, IInteractable
    {
        static readonly Color Canvas = new Color(0.40f, 0.37f, 0.30f);
        static readonly Color CanvasShade = new Color(0.31f, 0.29f, 0.24f);
        static readonly Color Pole = new Color(0.27f, 0.21f, 0.15f);
        static readonly Color Wool = new Color(0.34f, 0.22f, 0.18f);
        static readonly Color Stone = new Color(0.24f, 0.24f, 0.25f);
        static readonly Color Ash = new Color(0.10f, 0.09f, 0.09f);

        public static Camp Build(Vector3 at, float yaw)
        {
            GameObject root = new GameObject("CAMP");
            root.transform.position = at;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = root.transform;

            // La tente elle-meme bloque : un collider unique, plein.
            BoxCollider body = root.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.75f, 0f);
            body.size = new Vector3(2.2f, 1.5f, 2.6f);

            Proto.BeginVisualOnly();
            // Deux pans de toile en A, legerement differents : l'un est a l'ombre.
            GameObject left = Proto.Cube(t, new Vector3(-0.55f, 0.72f, 0f), new Vector3(0.05f, 1.75f, 2.5f), Canvas, "Toile");
            left.transform.localRotation = Quaternion.Euler(0f, 0f, -34f);
            GameObject right = Proto.Cube(t, new Vector3(0.55f, 0.72f, 0f), new Vector3(0.05f, 1.75f, 2.5f), CanvasShade, "Toile");
            right.transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
            Proto.Cube(t, new Vector3(0f, 1.46f, 0f), new Vector3(0.07f, 0.07f, 2.9f), Pole, "Faîtière");
            Proto.Cube(t, new Vector3(0f, 0.72f, 1.3f), new Vector3(0.06f, 1.5f, 0.06f), Pole, "Mat");
            Proto.Cube(t, new Vector3(0f, 0.72f, -1.3f), new Vector3(0.06f, 1.5f, 0.06f), Pole, "Mat");
            Proto.Cube(t, new Vector3(0f, 0.02f, 0f), new Vector3(2.3f, 0.03f, 2.7f), CanvasShade, "Tapis");
            Proto.Cube(t, new Vector3(0.1f, 0.1f, -0.2f), new Vector3(0.7f, 0.14f, 1.6f), Wool, "Couverture");

            // Le feu, devant, eteint : un cercle de pierres et des braises froides.
            // Un feu allume se verrait a cent metres. On est la pour se cacher.
            Vector3 fire = new Vector3(0f, 0f, 2.6f);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                GameObject s = Proto.Cube(t, fire + new Vector3(Mathf.Cos(a) * 0.55f, 0.08f, Mathf.Sin(a) * 0.55f),
                                          new Vector3(0.26f, 0.18f, 0.22f), Stone, "Pierre");
                s.transform.localRotation = Quaternion.Euler(0f, i * 51f, 0f);
            }
            Proto.Cube(t, fire + new Vector3(0f, 0.03f, 0f), new Vector3(0.7f, 0.04f, 0.7f), Ash, "Cendres");
            GameObject log = Proto.Cube(t, fire + new Vector3(0f, 0.1f, 0f), new Vector3(0.1f, 0.1f, 0.7f), Ash, "Tison");
            log.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            Proto.EndVisualOnly();

            return root.AddComponent<Camp>();
        }

        // ------------------------------------------------------------------ IInteractable

        public Transform Anchor { get { return transform; } }

        public bool CanInteract
        {
            get { return Game.Hoard != null && Game.Hoard.CampStash != null; }
        }

        public string Prompt
        {
            get
            {
                Cache stash = Game.Hoard != null ? Game.Hoard.CampStash : null;
                if (stash == null) return "";
                return "Ta tente   (" + Mathf.RoundToInt(stash.Contents.Weight) + " / "
                       + Mathf.RoundToInt(stash.Contents.MaxWeight) + " kg)";
            }
        }

        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null || Game.Hoard == null || Game.Hoard.CampStash == null) return;
            Game.Hud.OpenPanel(new StashPanel(Game.Hoard.CampStash, transform, "TA TENTE"));
            Sfx.Pop();
        }
    }
}
