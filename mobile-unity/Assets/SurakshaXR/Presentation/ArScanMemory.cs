using System.Collections.Generic;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    // Retains observations through brief missed samples; never authorizes placement
    // without a fresh plane check. This is readiness bookkeeping, not pose correction.
    public sealed class ArScanMemory
    {
        private sealed class Sample { public Vector3 point; public float seen; public int count; }
        private readonly Dictionary<int, Sample> samples = new Dictionary<int, Sample>();
        public void Observe(int id, Vector3 point, float now, float tolerance, float lifetime = float.PositiveInfinity)
        {
            if (!samples.TryGetValue(id, out var sample)) samples[id] = sample = new Sample();
            sample.count = sample.count > 0 && now - sample.seen <= lifetime && Vector3.Distance(sample.point, point) <= tolerance ? sample.count + 1 : 1;
            sample.point = point; sample.seen = now;
        }
        public List<Vector3> Usable(float now, float lifetime, int minimumObservations)
        {
            var result = new List<Vector3>(); var expired = new List<int>();
            foreach (var entry in samples) {
                if (now - entry.Value.seen > lifetime) expired.Add(entry.Key);
                else if (entry.Value.count >= minimumObservations) result.Add(entry.Value.point);
            }
            foreach (var id in expired) samples.Remove(id);
            return result;
        }
        public void Clear() => samples.Clear();
    }
}
