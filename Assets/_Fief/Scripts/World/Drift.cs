using UnityEngine;

namespace Fief
{
    /// <summary>Translation lente et sans fin : quand l'objet sort d'un cote, il revient de l'autre.</summary>
    public class Drift : MonoBehaviour
    {
        public Vector3 velocity = Vector3.right;
        public float wrapDistance = 400f;

        void Update()
        {
            transform.position += velocity * Time.deltaTime;

            Vector3 p = transform.position;
            if (p.x > wrapDistance) p.x = -wrapDistance;
            else if (p.x < -wrapDistance) p.x = wrapDistance;
            if (p.z > wrapDistance) p.z = -wrapDistance;
            else if (p.z < -wrapDistance) p.z = wrapDistance;
            transform.position = p;
        }
    }
}
