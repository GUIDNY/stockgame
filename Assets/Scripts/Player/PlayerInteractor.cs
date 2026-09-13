using UnityEngine;
using Echobound.Core;

namespace Echobound.Player
{
    /// <summary>Finds the nearest interactable in front of the player and triggers it on E.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public float Radius = 2.4f;
        public KeyCode Key = KeyCode.E;
        public IInteractable Current { get; private set; }
        private readonly Collider[] _hits = new Collider[24];

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) { Current = null; return; }
            Current = FindBest();
            gm.UI?.HUD?.SetPrompt(Current != null && Current.CanInteract ? Current.Prompt : "");
            if (Current != null && Current.CanInteract && Input.GetKeyDown(Key)) Current.Interact();
        }

        private IInteractable FindBest()
        {
            var origin = transform.position + Vector3.up * 0.9f + transform.forward * 0.8f;
            int n = Physics.OverlapSphereNonAlloc(origin, Radius, _hits, ~0, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i];
                if (c == null || c.transform == transform || c.transform.IsChildOf(transform)) continue;
                var interactable = c.GetComponentInParent<InteractableBase>();
                if (interactable == null || !interactable.CanInteract) continue;
                Vector3 to = c.transform.position - transform.position; to.y = 0;
                float facing = Vector3.Dot(transform.forward, to.normalized);
                float score = to.magnitude - facing * 0.8f;
                if (score < bestScore) { bestScore = score; best = interactable; }
            }
            return best;
        }
    }
}
