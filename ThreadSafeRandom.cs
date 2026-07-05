using System;

namespace LogiOptions.Native
{
    /// <summary>
    /// Lock-based thread-safe wrapper around <see cref="Random"/>.
    /// Preserves seeded determinism for tests while being safe for
    /// concurrent use across Task.WhenAll branches in production.
    /// </summary>
    internal sealed class ThreadSafeRandom
    {
        private readonly Random _random;

        public ThreadSafeRandom() => _random = new Random();
        public ThreadSafeRandom(int seed) => _random = new Random(seed);

        public int Next(int maxValue) { lock (_random) return _random.Next(maxValue); }
        public int Next(int minValue, int maxValue) { lock (_random) return _random.Next(minValue, maxValue); }
        public double NextDouble() { lock (_random) return _random.NextDouble(); }
    }
}