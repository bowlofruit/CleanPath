using CleanPath.Game;
using UnityEngine;

namespace CleanPath.UI
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class MassBarFollower : MonoBehaviour
    {
        [SerializeField] float _gapPx = 32f;

        Camera _camera;
        PlayerBall _player;
        RectTransform _canvasRt;
        RectTransform _rect;
        bool _ready;

        void Awake() => _rect = GetComponent<RectTransform>();

        public bool TryBind(Camera camera, PlayerBall player, Canvas canvas)
        {
            bool ok = true;
            ok &= SceneValidation.Require(camera, "MassBarFollower.Camera");
            ok &= SceneValidation.Require(player, "MassBarFollower.Player");
            ok &= SceneValidation.Require(canvas, "MassBarFollower.Canvas");
            if (!ok) return false;

            _camera = camera;
            _player = player;
            _canvasRt = canvas.transform as RectTransform;
            if (_canvasRt == null)
            {
                Debug.LogError("CleanPath: MassBarFollower.Canvas must use a RectTransform root.");
                return false;
            }

            _ready = true;
            return true;
        }

        public void TickFollow()
        {
            if (!_ready) return;

            Vector3 screen = _camera.WorldToScreenPoint(_player.Position);
            if (screen.z < 0f) return;

            float screenRadius = ScreenRadius(_camera, _player.Position, _player.Radius);
            screen.x += screenRadius + _gapPx;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRt, screen, null, out Vector2 local))
                _rect.anchoredPosition = local;
        }

        static float ScreenRadius(Camera cam, Vector3 worldPos, float worldRadius)
        {
            Vector3 center = cam.WorldToScreenPoint(worldPos);
            Vector3 edge = cam.WorldToScreenPoint(worldPos + cam.transform.right * worldRadius);
            return Vector2.Distance(center, edge);
        }
    }
}
