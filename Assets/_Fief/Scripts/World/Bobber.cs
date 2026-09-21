using UnityEngine;

namespace Fief
{
    /// <summary>Flotte et tourne sur place. Sert aux marqueurs des gisements.</summary>
    public class Bobber : MonoBehaviour
    {
        public float amplitude = 0.35f;
        public float speed = 1.8f;
        public float spin = 55f;

        Vector3 origin;
        float phase;

        void Start()
        {
            origin = transform.localPosition;
            phase = Random.value * 6.283f;
        }

        void Update()
        {
            phase += speed * Time.deltaTime;
            transform.localPosition = origin + new Vector3(0f, Mathf.Sin(phase) * amplitude, 0f);
            transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.Self);
        }
    }
}
