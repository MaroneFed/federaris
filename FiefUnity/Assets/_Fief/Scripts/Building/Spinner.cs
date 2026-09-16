using UnityEngine;

namespace Fief
{
    /// <summary>Fait tourner une piece (la lame de la scierie). Purement cosmetique,
    /// mais un batiment qui bouge a l'air vivant, et ca compte pour la Porte 1.</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float degreesPerSecond = 120f;

        void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
