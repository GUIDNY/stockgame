using System.Collections.Generic;
using UnityEngine;
using Echobound.Core;
using Echobound.World;

namespace Echobound.Combat
{
    /// <summary>Spawns thugs where the director asks for them. Caps the total so events cannot flood the town.</summary>
    public class EnemySpawner : MonoBehaviour
    {
        public int MaxAlive = 8;
        private readonly List<EnemyAI> _alive = new List<EnemyAI>();

        public void Spawn(string locationId, int count, string faction)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Town == null) return;
            _alive.RemoveAll(e => e == null);
            var mat = gm.Town.MaterialFor(faction == "TOWN_GUARD" ? new Color(0.3f, 0.35f, 0.6f) : new Color(0.55f, 0.15f, 0.15f));
            for (int i = 0; i < count && _alive.Count < MaxAlive; i++)
            {
                var pos = gm.Town.RandomPointIn(locationId);
                var enemy = EnemyAI.Create(pos, string.IsNullOrEmpty(faction) || faction == WorldBible.NoFaction ? "IRON_HAND" : faction, locationId, mat, UI.UIFactory.DefaultFont);
                enemy.transform.SetParent(transform, true);
                _alive.Add(enemy);
            }
            gm.World?.Log($"{count} {WorldBible.FactionName(faction)} thug(s) appeared at the {WorldBible.LocationName(locationId)}.", 4);
        }

        public void ClearAll()
        {
            foreach (var e in _alive) if (e != null) Destroy(e.gameObject);
            _alive.Clear();
        }

        public int AliveCount { get { _alive.RemoveAll(e => e == null); return _alive.Count; } }
    }
}
