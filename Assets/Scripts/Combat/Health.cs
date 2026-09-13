using System;
using UnityEngine;

namespace Echobound.Combat
{
    /// <summary>Hit points for NPCs and enemies. The player's health lives in PlayerState instead.</summary>
    public class Health : MonoBehaviour
    {
        public int Max = 100;
        public int Current = 100;
        public bool IsDead => Current <= 0;
        public event Action<int, string> Damaged;
        public event Action<string> Died;

        public void TakeDamage(int amount, string source)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount, source);
            if (Current <= 0) Died?.Invoke(source);
        }

        public void Set(int max, int current) { Max = max; Current = Mathf.Clamp(current, 0, max); }
    }
}
