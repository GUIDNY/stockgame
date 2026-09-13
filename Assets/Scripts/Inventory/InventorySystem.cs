using System;
using System.Collections.Generic;
using System.Linq;
using Echobound.Core;
using Echobound.World;

namespace Echobound.Inventory
{
    /// <summary>Pure inventory + currency logic operating on PlayerState.</summary>
    public class InventorySystem
    {
        private readonly PlayerState _player;
        public event Action Changed;

        public InventorySystem(PlayerState player) { _player = player; }

        public IReadOnlyList<Item> Items => _player.Items;
        public int Coins => _player.Coins;

        public void Add(Item item)
        {
            if (item == null) return;
            _player.Items.Add(item);
            GameEvents.RaiseNotification($"Picked up: {item.Label}");
            Changed?.Invoke();
        }

        public bool Remove(Item item)
        {
            bool ok = _player.Items.Remove(item);
            if (ok) Changed?.Invoke();
            return ok;
        }

        public Item Find(string type, string label = null) =>
            _player.Items.FirstOrDefault(i => i.Type == type && (label == null || i.Label == label));

        public bool Has(string type, string label = null) => Find(type, label) != null;
        public IEnumerable<Item> OfType(string type) => _player.Items.Where(i => i.Type == type);

        public bool TrySpend(int coins)
        {
            if (coins < 0 || _player.Coins < coins) return false;
            _player.Coins -= coins;
            Changed?.Invoke();
            return true;
        }

        public void AddCoins(int coins)
        {
            _player.Coins = Math.Max(0, _player.Coins + coins);
            if (coins > 0) GameEvents.RaiseNotification($"+{coins} coins");
            Changed?.Invoke();
        }
    }
}
