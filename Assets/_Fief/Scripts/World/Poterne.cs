using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA POTERNE : la porte derobee, au milieu du mur nord.
    ///
    /// C'est la regle du "second pilier" (CLAUDE.md, la destruction a regles) :
    /// une porte derobee ne se force PAS. On a beau taper dessus, elle ne bouge pas.
    /// Elle s'ouvre de l'interieur, quand un garde soudoye tire le verrou -- et
    /// alors elle reste ouverte pour tout le monde : un raccourci vers la foret du
    /// nord, loin des yeux de la grande porte.
    /// </summary>
    public class Poterne : MonoBehaviour, IInteractable
    {
        static readonly List<Poterne> All = new List<Poterne>();

        Transform leaf;
        float open;         // 0 fermee -> 1 ouverte
        bool opening;

        public static Poterne Build(Transform parent, Vector3 at)
        {
            GameObject root = new GameObject("POTERNE");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = at;
            Poterne p = root.AddComponent<Poterne>();

            // Le vantail tourne autour de son gond, a gauche (vu de la cour).
            GameObject hinge = new GameObject("Gond");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(-Castle.PosterneWidth * 0.5f, 0f, -0.2f);
            p.leaf = hinge.transform;

            Color wood = new Color(0.2f, 0.15f, 0.1f);
            Color iron = new Color(0.12f, 0.12f, 0.13f);
            float w = Castle.PosterneWidth;
            Proto.Cube(hinge.transform, new Vector3(w * 0.5f, Castle.PosterneHeight * 0.5f, 0f),
                       new Vector3(w, Castle.PosterneHeight, 0.16f), wood, "Vantail");
            Proto.BeginVisualOnly();
            for (int i = 0; i < 3; i++)
                Proto.Cube(hinge.transform, new Vector3(w * 0.5f, 0.4f + i * 0.9f, -0.09f), new Vector3(w + 0.02f, 0.12f, 0.04f), iron, "Penture");
            for (int i = 0; i < 6; i++)
                Proto.Cube(hinge.transform, new Vector3(0.25f + (i % 3) * 0.55f, 0.85f + (i / 3) * 0.9f, -0.1f),
                           new Vector3(0.06f, 0.06f, 0.04f), iron, "Clou");
            // Le verrou, cote cour : une grosse barre de fer. Dehors, il n'y a RIEN a saisir.
            Proto.Cube(hinge.transform, new Vector3(w * 0.8f, 1.3f, -0.14f), new Vector3(0.5f, 0.1f, 0.08f), iron, "Verrou");
            Proto.EndVisualOnly();

            All.Add(p);
            return p;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>Un garde a ete paye : toutes les poternes s'ouvrent (il n'y en a qu'une).</summary>
        public static void OpenAll()
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == null || All[i].opening) continue;
                All[i].opening = true;
                Sfx.Creak3D(All[i].transform.position);
            }
        }

        void Update()
        {
            if (!opening || open >= 1f) return;
            open = Mathf.MoveTowards(open, 1f, Time.deltaTime / 2.2f);
            // S'ouvre vers la cour (-z), lentement, en grincant.
            leaf.localRotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 1f, open) * 100f, 0f);
        }

        // ------------------------------------------------------------------ IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return !opening; } }
        public string Prompt { get { return "La poterne"; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            Sfx.Deny();
            Toasts.Show("Fermée de l'intérieur. Un garde, contre de l'or ?",
                        new Color(0.95f, 0.8f, 0.4f));
        }
    }
}
