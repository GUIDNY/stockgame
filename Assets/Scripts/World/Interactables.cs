using System.Collections.Generic;
using UnityEngine;
using Echobound.Core;
using Echobound.Inventory;
using Echobound.Player;

namespace Echobound.World
{
    /// <summary>A crate/shelf/desk the player can search. Contents mirror WorldState.PlacedItems for its location.</summary>
    public class SearchSpot : InteractableBase
    {
        public string LocationId = "";
        public string Label = "crate";
        public bool IsPrivate;
        public readonly List<Item> Contents = new List<Item>();
        private Renderer _renderer;
        private Material _empty, _full;

        public void Init(string locationId, string label, bool isPrivate, Material empty, Material full)
        {
            LocationId = locationId; Label = label; IsPrivate = isPrivate; _empty = empty; _full = full;
            _renderer = GetComponentInChildren<Renderer>();
            Refresh();
        }

        public void Refresh()
        {
            if (_renderer != null) _renderer.sharedMaterial = Contents.Count > 0 ? _full : _empty;
        }

        public override string Prompt => (IsPrivate ? "Rummage through " : "Search ") + Label + " [E]";
        public override void Interact() => GameManager.Instance?.Search(this);
    }

    public class BedSpot : InteractableBase
    {
        public override string Prompt => "Sleep until morning [E]";
        public override void Interact() => GameManager.Instance?.Sleep();
    }

    public class NoticeBoard : InteractableBase
    {
        public override string Prompt => "Read the notice board [E]";
        public override void Interact() => GameManager.Instance?.ReadNoticeBoard();
    }

    /// <summary>Generic townsperson: wanders, has no memory, says a line when spoken to. Adds life without AI cost.</summary>
    public class BackgroundNpc : InteractableBase
    {
        private static readonly string[] Lines =
        {
            "Keep your head down, stranger.", "Haven't seen you before. Passing through?", "The tavern's warm, at least.",
            "Guards are jumpy tonight.", "Don't go near the ruins after dark.", "Prices keep going up and nobody says why."
        };
        private GreyboxTownBuilder _town;
        private Vector3 _target;
        private float _timer;
        private System.Random _rng;
        private CharacterController _cc;

        public void Init(GreyboxTownBuilder town, int seed)
        {
            _town = town; _rng = new System.Random(seed);
            _cc = GetComponent<CharacterController>();
            PickTarget();
        }

        private void PickTarget()
        {
            var loc = WorldBible.Locations[_rng.Next(0, 6)]; // townsfolk stay in the civilised half
            _target = _town.RandomPointIn(loc);
            _timer = 6f + (float)_rng.NextDouble() * 10f;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            Vector3 to = _target - transform.position; to.y = 0;
            if (to.magnitude < 1f || _timer <= 0f) { PickTarget(); return; }
            var dir = GreyboxTownBuilder.Steer(transform, to.normalized);
            _cc.Move((dir * 1.6f + Vector3.down * 5f) * Time.deltaTime);
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
        }

        public override string Prompt => "Townsperson [E]";
        public override void Interact() => GameEvents.RaiseNotification("Townsperson: " + Lines[_rng.Next(Lines.Length)]);
    }
}
