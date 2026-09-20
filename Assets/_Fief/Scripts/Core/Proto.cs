using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Petite boite a outils pour fabriquer le decor a partir des primitives Unity
    /// (Cube, Sphere, Cylinder...). Tout le monde low-poly de la Phase 1 sort d'ici :
    /// zero asset 3D a telecharger, zero import. On remplacera ces primitives par les
    /// modeles Kenney/Synty quand ton frere les aura prepares, sans toucher a la logique.
    /// </summary>
    public static class Proto
    {
        /// <summary>
        /// Quand c'est faux, les primitives sont creees SANS collider.
        ///
        /// Pourquoi : GameObject.CreatePrimitive ajoute toujours un collider, qu'il
        /// faut ensuite detruire. Pour le decor (des milliers d'objets : arbres,
        /// buissons, nuages, membres du personnage), c'est des milliers de colliders
        /// crees puis jetes au lancement. On passe donc par un maillage mis en cache.
        /// </summary>
        public static bool CollidersEnabled = true;

        static readonly Dictionary<PrimitiveType, Mesh> PrimitiveMeshes =
            new Dictionary<PrimitiveType, Mesh>();

        static Mesh SharedMesh(PrimitiveType type)
        {
            Mesh mesh;
            if (PrimitiveMeshes.TryGetValue(type, out mesh) && mesh != null) return mesh;

            GameObject temp = GameObject.CreatePrimitive(type);
            MeshFilter filter = temp.GetComponent<MeshFilter>();
            mesh = filter != null ? filter.sharedMesh : null;
            Object.Destroy(temp);

            PrimitiveMeshes[type] = mesh;
            return mesh;
        }

        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos,
                                      Vector3 localScale, Color color, string name)
        {
            GameObject go;

            if (CollidersEnabled)
            {
                go = GameObject.CreatePrimitive(type);
            }
            else
            {
                go = new GameObject();
                go.AddComponent<MeshFilter>().sharedMesh = SharedMesh(type);
                go.AddComponent<MeshRenderer>();
            }

            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Get(color);
            return go;
        }

        /// <summary>Execute une construction de decor sans creer le moindre collider.</summary>
        public static void BeginVisualOnly() { CollidersEnabled = false; }
        public static void EndVisualOnly() { CollidersEnabled = true; }

        public static GameObject Cube(Transform parent, Vector3 pos, Vector3 scale, Color color, string name = "Cube")
        {
            return Make(PrimitiveType.Cube, parent, pos, scale, color, name);
        }

        public static GameObject Sphere(Transform parent, Vector3 pos, Vector3 scale, Color color, string name = "Sphere")
        {
            return Make(PrimitiveType.Sphere, parent, pos, scale, color, name);
        }

        public static GameObject Cylinder(Transform parent, Vector3 pos, Vector3 scale, Color color, string name = "Cylinder")
        {
            return Make(PrimitiveType.Cylinder, parent, pos, scale, color, name);
        }

        /// <summary>
        /// Un disque plat pose au sol, purement visuel (sans collider).
        /// yOffset sert a empiler les dalles sans qu'elles "clignotent" :
        /// deux surfaces exactement a la meme hauteur produisent du z-fighting.
        /// </summary>
        public static GameObject Pad(Transform parent, Vector3 center, float radius, Color color,
                                     string name = "Pad", float yOffset = 0.02f)
        {
            GameObject go = Cylinder(parent, center + new Vector3(0f, yOffset, 0f),
                                     new Vector3(radius * 2f, 0.01f, radius * 2f), color, name);
            StripCollider(go);
            return go;
        }

        /// <summary>Supprime le collider : utile pour tout ce qui est purement decoratif.</summary>
        public static void StripCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
        }

        public static void StripCollidersRecursive(GameObject go)
        {
            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) Object.Destroy(colliders[i]);
        }

        /// <summary>Un mat avec une banniere de couleur : sert a reperer les fiefs de loin.</summary>
        public static GameObject Banner(Transform parent, Vector3 pos, Color color, float height, string name = "Banniere")
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            GameObject pole = Cylinder(root.transform, new Vector3(0f, height * 0.5f, 0f),
                                       new Vector3(0.18f, height * 0.5f, 0.18f), Palette.Trunk, "Mat");
            StripCollider(pole);

            GameObject cloth = Cube(root.transform, new Vector3(0.75f, height - 1.1f, 0f),
                                    new Vector3(1.5f, 1.8f, 0.08f), color, "Toile");
            StripCollider(cloth);
            return root;
        }
    }
}
