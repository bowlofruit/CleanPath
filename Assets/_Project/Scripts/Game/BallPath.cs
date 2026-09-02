using UnityEngine;

namespace CleanPath.Game
{
    /// <summary>
    /// Visual track strip attached to the player ball. Width scales with ball radius.
    /// </summary>
    public class BallPath : MonoBehaviour
    {
        static readonly int ColorNearId = Shader.PropertyToID("_ColorNear");
        static readonly int ColorFarId = Shader.PropertyToID("_ColorFar");
        static readonly int PlaneHalfLengthId = Shader.PropertyToID("_PlaneHalfLength");

        [SerializeField] Transform _strip;
        [SerializeField] float _widthMultiplier = 1f;
        [SerializeField] Color _colorNear = new Color(0.93f, 0.28f, 0.55f, 1f);
        [SerializeField] Color _colorFar = new Color(0.93f, 0.28f, 0.55f, 0f);

        Vector3 _baseStripScale;
        Material _material;
        Renderer _renderer;
        bool _ready;
        const float PlaneMeshWidth = 10f;
        const float PlaneMeshHalfLength = 5f;

        public void Init()
        {
            if (_strip == null)
                _strip = transform.Find("Plane") ?? transform.Find("Path");

            if (_strip == null)
            {
                Debug.LogWarning("BallPath: no Plane/Path child; track visuals disabled.");
                return;
            }

            _baseStripScale = _strip.localScale;
            _renderer = _strip.GetComponent<Renderer>();
            if (_renderer == null)
            {
                Debug.LogWarning("BallPath: strip has no Renderer; track visuals disabled.");
                return;
            }

            _material = _renderer.material;
            _material.SetColor(ColorNearId, _colorNear);
            _material.SetColor(ColorFarId, _colorFar);
            _material.SetFloat(PlaneHalfLengthId, PlaneMeshHalfLength);
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _ready = true;
        }

        public void SetWidthFromRadius(float playerRadius)
        {
            if (!_ready) return;

            float targetWidth = Mathf.Max(playerRadius * 2f * _widthMultiplier, 0.08f);
            _strip.localScale = new Vector3(
                targetWidth / PlaneMeshWidth,
                _baseStripScale.y,
                _baseStripScale.z);
        }

        public void Follow(Transform root, float playerRadius)
        {
            if (!_ready) return;
            SetWidthFromRadius(playerRadius);
        }
    }
}
