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

        static readonly Dictionary<string, Mesh> Lathes = new Dictionary<string, Mesh>();

        /// <summary>
        /// UN VOLUME TOURNE (02/10) : une silhouette -- des points (rayon, hauteur) du bas vers
        /// le haut -- qu'on fait tourner autour de l'axe, comme un vase sur un tour de potier.
        /// Les ombres sont LISSES (la lumiere glisse d'une face a l'autre) : c'est ce qui
        /// fait un toit en cloche d'une seule piece, sans la marche entre deux cones
        /// empiles ; un point double dans la silhouette fait une arete vive. Toujours sans
        /// collider. Le haut est ferme si le dernier rayon est nul ; le bas est ferme par un
        /// disque (on ne voit pas dedans par-dessous).
        ///
        /// Concept Unity : un Mesh, ce sont des sommets et des triangles. Ici, pour chaque
        /// point de la silhouette, un anneau de "sides" sommets ; entre deux anneaux, une
        /// bande de triangles. Les maillages identiques sont gardes en cache (meme forme =
        /// meme maillage), comme les cones.
        /// </summary>
        public static GameObject Lathe(Transform parent, Vector3 basePos, Vector2[] profile, int sides, Color color, string name)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = basePos;
            go.AddComponent<MeshFilter>().sharedMesh = LatheMesh(profile, sides);
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Get(color);
            return go;
        }

        static Mesh LatheMesh(Vector2[] profile, int sides)
        {
            System.Text.StringBuilder key = new System.Text.StringBuilder();
            key.Append(sides);
            for (int i = 0; i < profile.Length; i++) key.Append('|').Append(profile[i].x.ToString("0.###")).Append(',').Append(profile[i].y.ToString("0.###"));
            Mesh mesh;
            if (Lathes.TryGetValue(key.ToString(), out mesh) && mesh != null) return mesh;

            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<int> tris = new List<int>();
            int ring = sides + 1;                       // un sommet de plus : la couture
            for (int i = 0; i < profile.Length; i++)
            {
                // La normale vient de la silhouette elle-meme (la pente entre le point d'avant
                // et celui d'apres) : pas de couture visible la ou l'anneau se referme. Un point
                // DOUBLE dans la silhouette fait une arete vive (un bandeau, une corniche).
                Vector2 along = profile[Mathf.Min(i + 1, profile.Length - 1)] - profile[Mathf.Max(i - 1, 0)];
                Vector2 side = new Vector2(along.y, -along.x);
                side = side.sqrMagnitude > 0.000001f ? side.normalized : Vector2.up;
                for (int k = 0; k <= sides; k++)
                {
                    float a = k / (float)sides * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * profile[i].x, profile[i].y, Mathf.Sin(a) * profile[i].x));
                    n.Add(new Vector3(Mathf.Cos(a) * side.x, side.y, Mathf.Sin(a) * side.x));
                }
            }
            for (int i = 0; i < profile.Length - 1; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a0 = i * ring + k, a1 = a0 + 1, b0 = a0 + ring, b1 = b0 + 1;
                    // Vus de dehors : dans le sens des aiguilles d'une montre (Unity).
                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
            // Le fond : un disque tourne vers le bas.
            int centre = v.Count;
            v.Add(new Vector3(0f, profile[0].y, 0f));
            n.Add(Vector3.down);
            int start = v.Count;
            for (int k = 0; k <= sides; k++)
            {
                float a = k / (float)sides * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * profile[0].x, profile[0].y, Mathf.Sin(a) * profile[0].x));
                n.Add(Vector3.down);
            }
            for (int k = 0; k < sides; k++) { tris.Add(centre); tris.Add(start + k); tris.Add(start + k + 1); }

            mesh = new Mesh();
            mesh.name = "Tour de potier";
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(n);
            mesh.RecalculateBounds();
            Lathes[key.ToString()] = mesh;
            return mesh;
        }

        /// <summary>
        /// SOUDER (02/10 -- "le jeu n'est pas tellement fluide") : les morceaux immobiles d'un
        /// objet (les enfants directs de "group" qui n'ont qu'un maillage et une matiere, sans
        /// collider ni script ni enfant) deviennent UN seul maillage, avec un morceau par
        /// matiere. Une gargouille, c'etait soixante-cinq petits objets dessines un par un ;
        /// soudee, c'est une poignee. Ceux de "keep" (un oeil qui change de couleur) restent
        /// a part. Rend le nouveau Renderer (null s'il n'y avait rien a souder).
        ///
        /// Concept Unity : chaque objet visible coute un "draw call" (un ordre envoye a la
        /// carte graphique) par matiere. Mille petits objets, c'est mille ordres par image,
        /// meme s'ils sont minuscules. Mesh.CombineMeshes fusionne des maillages en un seul :
        /// on les dessine d'un coup. Le static batching (GameBootstrap) fait la meme chose
        /// pour le decor, mais pas pour ce qui porte un script (une gargouille bouge la tete).
        /// </summary>
        public static MeshRenderer Weld(Transform group, string name, List<Renderer> keep)
        {
            List<Material> mats = new List<Material>();
            List<List<CombineInstance>> byMat = new List<List<CombineInstance>>();
            List<GameObject> used = new List<GameObject>();
            Matrix4x4 toGroup = group.worldToLocalMatrix;
            int verts = 0;
            UnityEngine.Rendering.ShadowCastingMode shadows = UnityEngine.Rendering.ShadowCastingMode.On;
            for (int i = 0; i < group.childCount; i++)
            {
                Transform c = group.GetChild(i);
                if (c.childCount > 0 || !c.gameObject.activeSelf) continue;
                MeshFilter f = c.GetComponent<MeshFilter>();
                MeshRenderer r = c.GetComponent<MeshRenderer>();
                if (f == null || r == null || f.sharedMesh == null || r.sharedMaterial == null) continue;
                if (keep != null && keep.Contains(r)) continue;
                if (c.GetComponents<Component>().Length != 3) continue;     // Transform, MeshFilter, MeshRenderer : rien d'autre
                int m = mats.IndexOf(r.sharedMaterial);
                if (m < 0)
                {
                    mats.Add(r.sharedMaterial);
                    byMat.Add(new List<CombineInstance>());
                    m = mats.Count - 1;
                }
                CombineInstance part = new CombineInstance();
                part.mesh = f.sharedMesh;
                part.transform = toGroup * c.localToWorldMatrix;
                byMat[m].Add(part);
                verts += f.sharedMesh.vertexCount;
                if (used.Count == 0) shadows = r.shadowCastingMode;
                used.Add(c.gameObject);
            }
            if (used.Count < 2) return null;

            bool big = verts > 65000;
            CombineInstance[] layers = new CombineInstance[mats.Count];
            for (int m = 0; m < mats.Count; m++)
            {
                Mesh layer = new Mesh();
                if (big) layer.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                layer.CombineMeshes(byMat[m].ToArray(), true, true);
                layers[m].mesh = layer;
                layers[m].transform = Matrix4x4.identity;
            }
            Mesh mesh = new Mesh();
            mesh.name = name;
            if (big) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(layers, false, false);
            for (int m = 0; m < layers.Length; m++) Object.Destroy(layers[m].mesh);

            GameObject go = new GameObject(name);
            go.transform.SetParent(group, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer welded = go.AddComponent<MeshRenderer>();
            welded.sharedMaterials = mats.ToArray();
            welded.shadowCastingMode = shadows;
            // Eteints tout de suite (Destroy n'agit qu'a la fin de l'image : d'ici la, on ne
            // doit ni les dessiner deux fois, ni les compter ailleurs).
            for (int i = 0; i < used.Count; i++)
            {
                used[i].SetActive(false);
                Object.Destroy(used[i]);
            }
            return welded;
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

        /// <summary>
        /// Un cylindre. ATTENTION (30/09, le bug des bots qui ne prenaient pas la
        /// Couronne) : le cylindre d'Unity recoit un CapsuleCollider, et une capsule plus
        /// large que haute devient une BOULE. Une marche de 5 m de large et 30 cm de haut
        /// avait donc un collider spherique de 2,6 m de rayon : une bulle invisible sur le
        /// socle, qui tenait tout le monde a distance. On le remplace par un collider qui
        /// a la vraie forme du cylindre (MeshCollider convexe).
        /// </summary>
        public static GameObject Cylinder(Transform parent, Vector3 pos, Vector3 scale, Color color, string name = "Cylinder")
        {
            GameObject go = Make(PrimitiveType.Cylinder, parent, pos, scale, color, name);
            CapsuleCollider capsule = go.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Object.DestroyImmediate(capsule);
                MeshCollider exact = go.AddComponent<MeshCollider>();
                exact.sharedMesh = SharedMesh(PrimitiveType.Cylinder);
                exact.convex = true;
            }
            return go;
        }

        /// <summary>
        /// Une gelule (cylindre aux bouts arrondis). A l'echelle 1 : 1 m de large, 2 m de
        /// haut. C'est la forme des membres de la Garde Pale -- lisse, sans arete.
        /// </summary>
        public static GameObject Capsule(Transform parent, Vector3 pos, Vector3 scale, Color color, string name = "Capsule")
        {
            return Make(PrimitiveType.Capsule, parent, pos, scale, color, name);
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
