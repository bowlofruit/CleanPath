using System.Collections.Generic;
using UnityEngine;

namespace CleanPath.Core
{
    public class InfectionSolver
    {
        readonly float _energyPerMass, _blastMultiplier, _linkCost, _costPerMeter, _maxLinkGap;

        public InfectionSolver(float energyPerMass, float blastMultiplier, float linkCost, float costPerMeter,
            float maxLinkGap)
        {
            _energyPerMass = energyPerMass;
            _blastMultiplier = blastMultiplier;
            _linkCost = linkCost;
            _costPerMeter = costPerMeter;
            _maxLinkGap = maxLinkGap;
        }

        /// <summary>
        /// Direct hit always infects at least one obstacle. Chain and blast need enough shot energy.
        /// </summary>
        public List<InfectionResult> Solve(IObstacleReadOnly field, Vec3 impact, float shotMass, float radiusPerMassCbrt,
            int directHitIndex)
        {
            var results = new List<InfectionResult>();
            if (directHitIndex < 0 || directHitIndex >= field.Count || !field.IsAlive(directHitIndex))
                return results;

            float e0 = Mathf.Max(_energyPerMass * shotMass, 0f);
            float seedRadius = _blastMultiplier * MassModel.Radius(shotMass, radiusPerMassCbrt);
            bool canBlast = e0 >= _linkCost;
            var energy = new float[field.Count];
            var hop = new int[field.Count];
            var infected = new bool[field.Count];
            var heap = new List<int>();

            void Seed(int i, float e, int h)
            {
                if (!field.IsAlive(i)) return;
                if (infected[i] && e <= energy[i]) return;
                energy[i] = e;
                hop[i] = h;
                infected[i] = true;
                HeapPush(heap, i, energy);
            }

            Seed(directHitIndex, e0, 0);

            if (canBlast)
            {
                for (int i = 0; i < field.Count; i++)
                {
                    if (i == directHitIndex || !field.IsAlive(i)) continue;
                    Vec3 p = field.GetPosition(i);
                    if (Vec3.Dist(p, impact) > seedRadius) continue;
                    Seed(i, e0 * 0.75f, 0);
                }
            }

            while (heap.Count > 0)
            {
                int a = HeapPop(heap, energy);
                if (!infected[a]) continue;
                float eA = energy[a];
                Vec3 posA = field.GetPosition(a);
                float radA = field.GetRadius(a);

                foreach (int b in field.GetNeighbours(a))
                {
                    if (!field.IsAlive(b) || infected[b]) continue;
                    Vec3 posB = field.GetPosition(b);
                    float radB = field.GetRadius(b);
                    float gap = Mathf.Max(0f, Vec3.Dist(posA, posB) - radA - radB);
                    if (gap > _maxLinkGap) continue;
                    float cost = _linkCost + _costPerMeter * gap;
                    if (eA < cost) continue;
                    float eB = eA - cost;
                    if (infected[b] && eB <= energy[b]) continue;
                    energy[b] = eB;
                    hop[b] = hop[a] + 1;
                    infected[b] = true;
                    HeapPush(heap, b, energy);
                }
            }

            for (int i = 0; i < field.Count; i++)
            {
                if (!infected[i]) continue;
                results.Add(new InfectionResult { index = i, hopIndex = hop[i], energyLeft = energy[i] });
            }

            results.Sort((a, b) => a.hopIndex != b.hopIndex ? a.hopIndex.CompareTo(b.hopIndex) : a.index.CompareTo(b.index));
            return results;
        }

        static void HeapPush(List<int> heap, int idx, float[] energy)
        {
            heap.Add(idx);
            int c = heap.Count - 1;
            while (c > 0)
            {
                int p = (c - 1) / 2;
                if (energy[heap[c]] <= energy[heap[p]]) break;
                (heap[c], heap[p]) = (heap[p], heap[c]);
                c = p;
            }
        }

        static int HeapPop(List<int> heap, float[] energy)
        {
            int top = heap[0];
            int last = heap[^1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return top;
            heap[0] = last;
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, best = i;
                if (l < heap.Count && energy[heap[l]] > energy[heap[best]]) best = l;
                if (r < heap.Count && energy[heap[r]] > energy[heap[best]]) best = r;
                if (best == i) break;
                (heap[i], heap[best]) = (heap[best], heap[i]);
                i = best;
            }
            return top;
        }
    }

    public interface IObstacleReadOnly
    {
        int Count { get; }
        bool IsAlive(int i);
        Vec3 GetPosition(int i);
        float GetRadius(int i);
        IEnumerable<int> GetNeighbours(int i);
        float GoalZ { get; }
        float FieldMinX { get; }
        float FieldMaxX { get; }
    }

    public interface IObstacleCloneable : IObstacleReadOnly
    {
        IObstacleCloneable Clone();
        void MarkDead(int index);
    }
}
