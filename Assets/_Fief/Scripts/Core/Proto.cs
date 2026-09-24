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

        public static Mesh SharedMesh(PrimitiveType type)
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

        static readonly Dictionary<int, Mesh> Cones = new Dictionary<int, Mesh>();

        /// <summary>
        /// Un cone a facettes de rayon 1 et de hauteur 1, base en y = 0. Unity n'a pas
        /// de cone parmi ses primitives : on le fabrique. Chaque face a ses propres
        /// sommets, pour qu'elle capte la lumiere a plat (le look low-poly).
        ///
        /// Sens des triangles : une face est vue de DEVANT quand ses trois sommets
        /// tournent dans le sens des aiguilles d'une montre vu de l'exterieur.
        /// Dans l'autre sens, Unity ne la dessine pas -- on verrait au travers.
        /// </summary>
        public static Mesh ConeMesh(int sides)
        {
            sides = Mathf.Clamp(sides, 3, 32);
            Mesh mesh;
            if (Cones.TryGetValue(sides, out mesh) && mesh != null) return mesh;

            List<Vector3> v = new List<Vector3>();
            List<int> tris = new List<int>();
            Vector3 tip = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 b0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 b1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));

                // flanc : b0, pointe, b1
                int k = v.Count;
                v.Add(b0); v.Add(tip); v.Add(b1);
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);

                // dessous : centre, b0, b1
                k = v.Count;
                v.Add(Vector3.zero); v.Add(b0); v.Add(b1);
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
            }

            mesh = new Mesh();
            mesh.name = "Cone" + sides;
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Cones[sides] = mesh;
            return mesh;
        }

        /// <summary>Un cone pose (toit de tour, fleche, chapeau) : toujours sans collider.</summary>
        public static GameObject Cone(Transform parent, Vector3 basePos, float radius, float height,
                                      Color color, string name = "Cone", int sides = 8)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = basePos;
            go.transform.localScale = new Vector3(radius, height, radius);
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh(sides);
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Get(color);
            return go;
        }

        /// <summary>
        /// Un bloc invisible qui arrete le joueur. Pour les objets dont la forme
        /// visible est compliquee (un puits, une charrette) : un seul collider simple
        /// vaut mieux que dix colliders exacts.
        /// </summary>
        public static GameObject Blocker(Transform parent, Vector3 centre, Vector3 size, string name = "Obstacle")
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = size;
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
        public static GameObject Banner(Transform parent, Vector3 pos, Color color, float height, string name = "Bannière")
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
