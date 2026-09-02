using UnityEngine;

namespace CleanPath.Game
{
    public class BushBillboardVisual : MonoBehaviour
    {
        [SerializeField] Material _aliveMat;
        [SerializeField] Material _infectedMat;

        Renderer _renderer;
        bool _hasInfectedMat;

        void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _hasInfectedMat = _infectedMat != null;
        }

        public void Setup(Material alive, Material infected)
        {
            _aliveMat = alive;
            _infectedMat = infected;
            _hasInfectedMat = _infectedMat != null;
            if (_aliveMat != null)
                _renderer.sharedMaterial = _aliveMat;
        }

        public void SetInfected()
        {
            if (_hasInfectedMat)
                _renderer.sharedMaterial = _infectedMat;
        }
    }
}
