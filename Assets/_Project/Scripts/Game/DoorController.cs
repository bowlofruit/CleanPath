using CleanPath.Config;
using UnityEngine;

namespace CleanPath.Game
{
    public class DoorController : MonoBehaviour
    {
        [SerializeField] float _openDistance = 5f;

        Transform _crown;
        Transform _marker;
        Camera _camera;
        bool _open;
        bool _hasCrown;
        bool _hasMarker;
        Vector3 _crownClosedScale;

        public Vector3 Center => transform.position;
        public float GoalZ => transform.position.z;
        public float OpenDistance => _openDistance;

        public bool HitSegment(Vector3 from, Vector3 to, float probeRadius, float catchRadius = -1f)
        {
            if (catchRadius < 0f) catchRadius = EstimateCatchRadius();
            Vector2 a = new Vector2(from.x, from.z);
            Vector2 b = new Vector2(to.x, to.z);
            Vector2 door = new Vector2(Center.x, Center.z);
            return DistPointSegment(door, a, b) <= catchRadius + probeRadius;
        }

        float EstimateCatchRadius()
        {
            float maxR = 0.6f;
            foreach (var rend in GetComponentsInChildren<Renderer>())
                maxR = Mathf.Max(maxR, rend.bounds.extents.x, rend.bounds.extents.z);
            return maxR;
        }

        static float DistPointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-8f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + ab * t);
        }

        public void Init(Camera camera)
        {
            _camera = camera;
            _crown = transform.Find("Crown");
            _marker = transform.Find("Marker");
            _hasCrown = _crown != null;
            _hasMarker = _marker != null;
            if (_hasCrown) _crownClosedScale = _crown.localScale;
        }

        public void Tick(Vector3 playerCenter)
        {
            if (_hasMarker)
                _marker.rotation = Quaternion.LookRotation(_marker.position - _camera.transform.position);

            if (_open) return;
            if (Vector3.Distance(playerCenter, Center) <= _openDistance)
                Open();
        }

        void Open()
        {
            _open = true;
            if (_hasCrown)
                _crown.localScale = new Vector3(_crownClosedScale.x * 1.15f, _crownClosedScale.y * 0.7f, _crownClosedScale.z * 1.15f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _openDistance);
        }
    }
}
