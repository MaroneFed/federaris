using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Fusionne des milliers de petites formes en quelques gros maillages.
    ///
    /// LE PROBLEME : un monde "rempli partout", c'est 5 000 arbres et 4 000 touffes.
    /// Un GameObject par forme, ca fait 30 000 objets : Unity met dix secondes a les
    /// creer et la carte graphique s'etrangle a les dessiner un par un.
    ///
    /// LA SOLUTION : on ne cree aucun objet pour le decor. On prend la geometrie de
    /// chaque forme, on la deplace/tourne/redimensionne a la main, et on l'empile dans
    /// un grand maillage commun. A la fin, un seul objet par (zone de carte, couleur).
    /// On passe de 30 000 objets a quelques centaines, et le decor devient gratuit.
    ///
    /// Le decoupage en zones de carte sert au "frustum culling" : Unity saute
    /// entierement les zones qui ne sont pas devant la camera.
    /// </summary>
    public class Batcher
    {
        class Bucket
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<int> triangles = new List<int>();
        }

        readonly Dictionary<long, Dictionary<Color, Bucket>> cells =
            new Dictionary<long, Dictionary<Color, Bucket>>();

        readonly float cellSize;
        int shapeCount;

        public int ShapeCount { get { return shapeCount; } }

        public Batcher(float cellSize)
        {
            this.cellSize = Mathf.Max(20f, cellSize);
        }

        public void Add(PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
        {
            Mesh source = Proto.SharedMesh(type);
            if (source == null) return;

            // Meme filet, plus grossier : le decor n'a pas besoin de 16 millions
            // de nuances, et chaque nuance supplementaire coute un maillage entier.
            color = Palette.Quantize(color, 12);

            Bucket bucket = BucketFor(position, color);
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);

            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            int[] sourceTriangles = source.triangles;

            int offset = bucket.vertices.Count;

            for (int i = 0; i < sourceVertices.Length; i++)
            {
                bucket.vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[i]));
                Vector3 n = i < sourceNormals.Length
                    ? matrix.MultiplyVector(sourceNormals[i]).normalized
                    : Vector3.up;
                bucket.normals.Add(n);
            }

            for (int i = 0; i < sourceTriangles.Length; i++)
            {
                bucket.triangles.Add(offset + sourceTriangles[i]);
            }

            shapeCount++;
        }

        public void Add(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            Add(type, position, scale, Quaternion.identity, color);
        }

        Bucket BucketFor(Vector3 position, Color color)
        {
            int cx = Mathf.FloorToInt(position.x / cellSize);
            int cz = Mathf.FloorToInt(position.z / cellSize);
            long key = ((long)(cx + 100000) << 20) + (cz + 100000);

            Dictionary<Color, Bucket> byColor;
            if (!cells.TryGetValue(key, out byColor))
            {
                byColor = new Dictionary<Color, Bucket>();
                cells[key] = byColor;
            }

            Bucket bucket;
            if (!byColor.TryGetValue(color, out bucket))
            {
                bucket = new Bucket();
                byColor[color] = bucket;
            }
            return bucket;
        }

        /// <summary>Cree les objets finaux et vide le tampon. Retourne la racine.</summary>
        public GameObject Flush(Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            int meshes = 0;
            foreach (KeyValuePair<long, Dictionary<Color, Bucket>> cell in cells)
            {
                foreach (KeyValuePair<Color, Bucket> entry in cell.Value)
                {
                    Bucket bucket = entry.Value;
                    if (bucket.triangles.Count == 0) continue;

                    Mesh mesh = new Mesh();
                    mesh.name = name + "_" + meshes;
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    mesh.SetVertices(bucket.vertices);
                    mesh.SetNormals(bucket.normals);
                    mesh.SetTriangles(bucket.triangles, 0);
                    mesh.RecalculateBounds();

                    GameObject go = new GameObject(name + "_" + meshes);
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Get(entry.Key);
                    meshes++;
                }
            }

            cells.Clear();
            Debug.Log("[FIEF] " + name + " : " + shapeCount + " formes fusionnees en " + meshes + " maillages.");
            return root;
        }
    }
}
