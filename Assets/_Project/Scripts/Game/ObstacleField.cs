using System.Collections.Generic;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    public class ObstacleField : MonoBehaviour, IObstacleCloneable
    {
        struct Obstacle
        {
            public Vec3 pos;
            public float radius;
            public bool alive;
        }

        Obstacle[] _data;
        GameObject[] _gos;
        Renderer[] _renderers;
        List<int>[] _neighbours;
        float _goalZ, _maxLinkGap;
        float _fieldMinX, _fieldMaxX;

        public int Count => _data?.Length ?? 0;
        public float GoalZ => _goalZ;
        public float FieldMinX => _fieldMinX;
        public float FieldMaxX => _fieldMaxX;
        public float SliceStep { get; private set; } = 0.3f;

        public void Init(float maxLinkGap = 1.2f)
        {
            _maxLinkGap = maxLinkGap;
        }

        public void PrepareScene(float goalZ)
        {
            _goalZ = goalZ;
            _data = System.Array.Empty<Obstacle>();
            _gos = System.Array.Empty<GameObject>();
            _renderers = System.Array.Empty<Renderer>();
            _neighbours = System.Array.Empty<List<int>>();
            ImportSceneObstacles();
            ComputeFieldBounds();
            SliceStep = ComputeSliceStep();
            if (_goalZ <= 0f)
                _goalZ = ComputeFallbackGoalZ();
        }

        void ImportSceneObstacles()
        {
            var list = new List<Obstacle>();
            var gos = new List<GameObject>();

            foreach (Transform child in transform)
            {
                if (child.GetComponent<BushBillboardVisual>() == null) continue;
                if (!TryGetHitDisc(child, out Vector3 center, out float radius)) continue;
                list.Add(new Obstacle
                {
                    pos = new Vec3(center.x, center.y, center.z),
                    radius = radius,
                    alive = true
                });
                gos.Add(child.gameObject);
            }

            if (list.Count == 0) return;

            _data = list.ToArray();
            _gos = gos.ToArray();
            _renderers = new Renderer[_data.Length];
            for (int i = 0; i < _data.Length; i++)
            {
                var rend = gos[i].GetComponentInChildren<Renderer>();
                _renderers[i] = rend != null ? rend : gos[i].GetComponent<Renderer>();
            }
            BuildNeighbours();
        }

        void ComputeFieldBounds()
        {
            if (_data == null || _data.Length == 0)
            {
                _fieldMinX = -2f;
                _fieldMaxX = 2f;
                return;
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            const float pad = 0.35f;
            for (int i = 0; i < _data.Length; i++)
            {
                minX = Mathf.Min(minX, _data[i].pos.x - _data[i].radius - pad);
                maxX = Mathf.Max(maxX, _data[i].pos.x + _data[i].radius + pad);
            }
            _fieldMinX = minX;
            _fieldMaxX = maxX;
        }

        float ComputeSliceStep()
        {
            if (_data == null || _data.Length == 0) return 0.3f;
            float maxR = 0.15f;
            for (int i = 0; i < _data.Length; i++)
                maxR = Mathf.Max(maxR, _data[i].radius);
            return Mathf.Clamp(maxR * 0.65f, 0.15f, 0.5f);
        }

        float ComputeFallbackGoalZ()
        {
            if (_data == null || _data.Length == 0) return 16f;
            float maxZ = float.MinValue;
            for (int i = 0; i < _data.Length; i++)
                maxZ = Mathf.Max(maxZ, _data[i].pos.z + _data[i].radius);
            return maxZ + SliceStep * 4f;
        }

        static bool TryGetHitDisc(Transform obstacleRoot, out Vector3 center, out float radius)
        {
            var billboard = obstacleRoot.Find("Billboard");
            if (billboard != null)
            {
                center = billboard.position;
                Vector3 scale = billboard.lossyScale;
                radius = Mathf.Max(scale.x, scale.y) * 0.5f;
                return radius > 0.02f;
            }

            var rend = obstacleRoot.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                center = rend.transform.position;
                Vector3 scale = rend.transform.lossyScale;
                radius = Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;
                return radius > 0.02f;
            }

            center = obstacleRoot.position;
            radius = 0.5f * Mathf.Max(obstacleRoot.lossyScale.x, obstacleRoot.lossyScale.z, 0.16f);
            return true;
        }

        bool TryGetHitDisc(int i, out Vector2 center, out float radius)
        {
            if (!_data[i].alive)
            {
                center = default;
                radius = 0f;
                return false;
            }

            if (TryGetHitDisc(_gos[i].transform, out Vector3 worldCenter, out float worldRadius))
            {
                center = new Vector2(worldCenter.x, worldCenter.z);
                radius = worldRadius;
                return true;
            }

            center = new Vector2(_data[i].pos.x, _data[i].pos.z);
            radius = _data[i].radius;
            return true;
        }

        static float DistPointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-8f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + ab * t);
        }

        void BuildNeighbours()
        {
            _neighbours = new List<int>[_data.Length];
            for (int i = 0; i < _data.Length; i++) _neighbours[i] = new List<int>();

            for (int i = 0; i < _data.Length; i++)
            {
                for (int j = i + 1; j < _data.Length; j++)
                {
                    float gap = Mathf.Max(0f, Vec3.Dist(_data[i].pos, _data[j].pos) - _data[i].radius - _data[j].radius);
                    if (gap <= _maxLinkGap)
                    {
                        _neighbours[i].Add(j);
                        _neighbours[j].Add(i);
                    }
                }
            }
        }

        public float SampleCenterX(float z) => (_fieldMinX + _fieldMaxX) * 0.5f;

        public bool IsAlive(int i) => _data[i].alive;

        public Vec3 GetPosition(int i)
        {
            if (_data[i].alive && TryGetHitDisc(_gos[i].transform, out Vector3 center, out _))
                return new Vec3(center.x, center.y, center.z);
            return _data[i].pos;
        }

        public float GetRadius(int i)
        {
            if (TryGetHitDisc(i, out _, out float radius))
                return radius;
            return _data[i].radius;
        }

        public IEnumerable<int> GetNeighbours(int i) => _neighbours[i];

        public void MarkDead(int index)
        {
            if (!_data[index].alive) return;
            _data[index].alive = false;
            _gos[index].SetActive(false);
        }

        public void SetInfectedVisual(int index)
        {
            var bush = _gos[index].GetComponent<BushBillboardVisual>();
            if (bush != null)
                bush.SetInfected();
        }

        public int HitTest(Vector3 pos, float probeRadius = 0f)
        {
            if (_data == null) return -1;

            Vector2 p = new Vector2(pos.x, pos.z);
            float best = float.MaxValue;
            int hit = -1;

            for (int i = 0; i < _data.Length; i++)
            {
                if (!TryGetHitDisc(i, out Vector2 center, out float radius)) continue;
                float d = Vector2.Distance(center, p) - radius - probeRadius;
                if (d <= 0f && d < best) { best = d; hit = i; }
            }
            return hit;
        }

        public int HitTestSegment(Vector3 from, Vector3 to, float probeRadius, Vector2 flightDir, float minAlong = 0f)
        {
            if (_data == null) return -1;

            Vector2 a = new Vector2(from.x, from.z);
            Vector2 b = new Vector2(to.x, to.z);
            Vector2 dir = flightDir.sqrMagnitude > 1e-6f ? flightDir.normalized : (b - a).normalized;
            float best = float.MaxValue;
            int hit = -1;

            for (int i = 0; i < _data.Length; i++)
            {
                if (!_data[i].alive) continue;
                if (!TryGetHitDisc(i, out Vector2 center, out float radius)) continue;
                float along = Vector2.Dot(center - a, dir);
                if (along < minAlong) continue;

                float d = DistPointSegment(center, a, b) - radius - probeRadius;
                if (d <= 0f && d < best) { best = d; hit = i; }
            }
            return hit;
        }

        public IObstacleCloneable Clone()
        {
            var c = new ObstacleFieldData
            {
                data = new Obstacle[_data.Length],
                neighbours = _neighbours,
                goalZ = _goalZ,
                fieldMinX = _fieldMinX,
                fieldMaxX = _fieldMaxX
            };
            System.Array.Copy(_data, c.data, _data.Length);
            return c;
        }

        class ObstacleFieldData : IObstacleCloneable
        {
            public Obstacle[] data;
            public List<int>[] neighbours;
            public float goalZ, fieldMinX, fieldMaxX;
            public int Count => data.Length;
            public float GoalZ => goalZ;
            public float FieldMinX => fieldMinX;
            public float FieldMaxX => fieldMaxX;
            public bool IsAlive(int i) => data[i].alive;
            public Vec3 GetPosition(int i) => data[i].pos;
            public float GetRadius(int i) => data[i].radius;
            public IEnumerable<int> GetNeighbours(int i) => neighbours[i];
            public void MarkDead(int index) => data[index].alive = false;
            public IObstacleCloneable Clone()
            {
                var c = new ObstacleFieldData
                {
                    data = new Obstacle[data.Length],
                    neighbours = neighbours,
                    goalZ = goalZ, fieldMinX = fieldMinX, fieldMaxX = fieldMaxX
                };
                System.Array.Copy(data, c.data, data.Length);
                return c;
            }
        }
    }
}
