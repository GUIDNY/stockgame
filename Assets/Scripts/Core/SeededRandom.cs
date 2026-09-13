using System;
using System.Collections.Generic;

namespace Echobound.Core
{
    /// <summary>Deterministic random helper used by the mock narrative generator and fallback systems.</summary>
    public class SeededRandom
    {
        private readonly Random _rng;
        public int Seed { get; }

        public SeededRandom(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }

        public int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public double NextDouble() => _rng.NextDouble();
        public bool Chance(double probability) => _rng.NextDouble() < probability;

        public T Pick<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) throw new ArgumentException("Cannot pick from an empty list");
            return list[_rng.Next(list.Count)];
        }

        public T PickExcept<T>(IList<T> list, T except)
        {
            var candidates = new List<T>();
            foreach (var item in list) if (!Equals(item, except)) candidates.Add(item);
            return candidates.Count == 0 ? except : Pick(candidates);
        }

        public List<T> Shuffle<T>(IEnumerable<T> source)
        {
            var list = new List<T>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
