using CleanPath.Config;
using UnityEngine;

namespace CleanPath.Game
{
    public class Projectile : MonoBehaviour
    {
        public const int StillFlying = -2;
        public const int HitDoor = -3;

        const float MaxStep = 0.35f;

        Transform _visual;
        float _targetSpeed, _accel;
        Vector3 _dir;
        Vector2 _flightDirXZ;
        float _currentSpeed;
        float _radius;
        float _spawnClearance;
        bool _active;
        ObstacleField _field;
        DoorController _door;

        public bool IsActive => _active;
        public Vector3 Position => transform.position;

        public void Init(Material mat, ObstacleField field, DoorController door)
        {
            _field = field;
            _door = door;
            _visual = transform.Find("Visual");
            if (_visual == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Visual";
                go.transform.SetParent(transform, false);
                go.GetComponent<Collider>().enabled = false;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                _visual = go.transform;
            }
            gameObject.SetActive(false);
        }

        public void Fire(Vector3 origin, Vector3 direction, float radius, ShotConfig shot)
        {
            _targetSpeed = shot.projectileSpeed;
            _accel = shot.projectileAcceleration;
            _dir = direction.normalized;
            _flightDirXZ = new Vector2(_dir.x, _dir.z).normalized;
            _currentSpeed = 0f;
            _radius = radius;
            _spawnClearance = radius + 0.2f;
            _active = true;
            transform.position = origin;
            _visual.localScale = Vector3.one * radius * 2f;
            gameObject.SetActive(true);
        }

        public int Tick(float dt)
        {
            if (!_active) return -1;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, _accel * dt);
            float remaining = _currentSpeed * dt;
            Vector3 pos = transform.position;

            while (remaining > 0f && _active)
            {
                float step = Mathf.Min(remaining, MaxStep);
                Vector3 prev = pos;
                Vector3 next = prev + _dir * step;
                pos = next;
                remaining -= step;

                int hit = _field.HitTestSegment(prev, next, _radius, _flightDirXZ, _spawnClearance);
                if (hit >= 0)
                {
                    transform.position = next;
                    _active = false;
                    return hit;
                }

                if (_door.HitSegment(prev, next, _radius))
                {
                    transform.position = next;
                    _active = false;
                    return HitDoor;
                }
            }

            transform.position = pos;
            return StillFlying;
        }

        public void Despawn()
        {
            _active = false;
            gameObject.SetActive(false);
        }
    }
}
