using CleanPath.Config;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    public class AimRing : MonoBehaviour
    {
        static readonly Color LineColor = new Color(0.93f, 0.28f, 0.55f, 0.85f);
        static readonly string[] ShaderCandidates =
        {
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "Unlit/Color"
        };

        [SerializeField] Material _lineMaterial;
        LineRenderer _lr;
        bool _ready;

        public void Init(BallConfig ball)
        {
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.loop = false;
            _lr.positionCount = 2;
            _lr.numCapVertices = 4;
            _lr.numCornerVertices = 2;
            _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var material = CreateLineMaterial();
            if (material == null)
            {
                Debug.LogWarning("AimRing: no line material; aim preview disabled.");
                return;
            }

            _lr.material = material;
            _ready = true;
            gameObject.SetActive(false);
        }

        Material CreateLineMaterial()
        {
            if (_lineMaterial != null)
            {
                var copy = new Material(_lineMaterial);
                copy.color = LineColor;
                return copy;
            }

            foreach (var shaderName in ShaderCandidates)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) continue;

                var material = new Material(shader);
                material.color = LineColor;
                return material;
            }

            return null;
        }

        public void Show(Vector3 playerCenter, Vector3 goalCenter, float playerRadius, float shotRadius)
        {
            if (!_ready || shotRadius <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            Vector3 dir = goalCenter - playerCenter;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 start = playerCenter + dir * playerRadius;
            Vector3 end = playerCenter + dir * (playerRadius + shotRadius + 0.05f);
            start.y = end.y = 0.06f;

            float width = Mathf.Max(shotRadius * 2f, 0.12f);
            _lr.startWidth = width;
            _lr.endWidth = width;
            _lr.SetPosition(0, start);
            _lr.SetPosition(1, end);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
