using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Generateur passif (la Scierie) : ajoute une ressource au coffre du fief
    /// a intervalle regulier. C'est le premier revenu qui tombe sans toi,
    /// et donc la premiere vraie raison d'investir son or.
    /// </summary>
    public class PassiveProducer : MonoBehaviour
    {
        public ResourceType type = ResourceType.Wood;
        public int amountPerCycle = 1;
        public float interval = 8f;

        float timer;

        public float Progress01 { get { return interval <= 0f ? 0f : Mathf.Clamp01(timer / interval); } }

        void Update()
        {
            if (Game.Fief == null) return;

            timer += Time.deltaTime;
            if (timer < interval) return;

            timer = 0f;
            Game.Fief.AddStock(type, amountPerCycle);
        }
    }
}
