using UnityEngine;
using Echobound.Core;

namespace Echobound.World
{
    /// <summary>Trigger volume that tells the simulation which World Bible location the player is in.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LocationZone : MonoBehaviour
    {
        public string Id = "TOWN_SQUARE";
        public Vector3 Center;
        public Vector2 Size = new Vector2(20, 20);

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Player.ThirdPersonController>() != null) GameManager.Instance?.PlayerEnteredLocation(Id);
        }

        private void OnTriggerStay(Collider other)
        {
            // Safety: if the player was teleported inside, make sure the location is registered.
            if (GameManager.Instance?.World != null && GameManager.Instance.World.Player.Location != Id && other.GetComponent<Player.ThirdPersonController>() != null)
                GameManager.Instance.PlayerEnteredLocation(Id);
        }

        public Vector3 RandomPoint(System.Random rng, float margin = 2f)
        {
            float hx = Mathf.Max(0.5f, Size.x / 2f - margin), hz = Mathf.Max(0.5f, Size.y / 2f - margin);
            return new Vector3(Center.x + (float)(rng.NextDouble() * 2 - 1) * hx, Center.y, Center.z + (float)(rng.NextDouble() * 2 - 1) * hz);
        }
    }
}
