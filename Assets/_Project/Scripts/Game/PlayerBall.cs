using CleanPath.Config;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    public class PlayerBall : MonoBehaviour
    {
        const float MinVisualMassFrac = 0.05f;
        const float MinChargeRadiusFrac = 0.05f;

        Transform _visual;
        Transform _chargeBall;
        Transform _moveRoot;
        BallConfig _cfg;
        BallPath _track;
        Material _ballMat;
        float _mass;
        float _initialMass;
        float _basePlayerRadius;
        Vector3 _baseVisualScale = Vector3.one;
        float _chargeStartMass, _chargeTime, _chargeMaxRadius;
        bool _charging;
        bool _hasTrack;
        bool _useSceneY;

        Transform Root => _moveRoot != null ? _moveRoot : transform;

        public float Mass => _mass;
        public float Radius => _basePlayerRadius * MassScale();
        public bool IsCharging => _charging;
        public float ChargeTime => _chargeTime;

        public void Init(float startMass, BallConfig cfg, Material ballMat, BallPath track = null)
        {
            _cfg = cfg;
            _ballMat = ballMat;
            _mass = startMass;
            _initialMass = Mathf.Max(startMass, 0.0001f);
            _basePlayerRadius = cfg.playerRadius;
            BindTrack(track);
            ResolveVisual(ballMat);
            CreateChargeBall();
            ApplyProceduralScale();
        }

        void ResolveVisual(Material ballMat)
        {
            _visual = transform.Find("Visual");
            if (_visual == null && GetComponent<Renderer>() == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Visual";
                go.transform.SetParent(transform, false);
                go.GetComponent<Collider>().enabled = false;
                go.GetComponent<Renderer>().sharedMaterial = ballMat;
                _visual = go.transform;
            }
            else if (_visual == null)
            {
                _visual = transform;
            }
        }

        void CreateChargeBall()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ChargeBall";
            go.GetComponent<Collider>().enabled = false;
            go.GetComponent<Renderer>().sharedMaterial = _ballMat;
            _chargeBall = go.transform;
            _chargeBall.SetParent(null);
            _chargeBall.gameObject.SetActive(false);
        }

        public float MinCriticalMass(float startMass) => startMass * _cfg.minCriticalMassFrac;

        public void BeginCharge()
        {
            _charging = true;
            _chargeTime = 0f;
            _chargeStartMass = _mass;
            _chargeMaxRadius = Mathf.Max(Radius, _basePlayerRadius * MinVisualMassFrac);
            _chargeBall.gameObject.SetActive(false);
        }

        public void TickCharge(float dt, float chargeRate, float minHoldDuration)
        {
            if (!_charging) return;
            _chargeTime += dt;

            if (_chargeTime < minHoldDuration)
            {
                _mass = _chargeStartMass;
                float prep = _chargeTime / Mathf.Max(minHoldDuration, 0.0001f);
                float armingMass = _chargeStartMass * MinChargeRadiusFrac * prep;
                UpdateChargeBallVisual(armingMass);
                UpdatePlayerVisualScale();
                return;
            }

            float shotMass = ComputeShotMass(chargeRate, minHoldDuration);
            _mass = _chargeStartMass - shotMass;
            UpdatePlayerVisualScale();
            UpdateChargeBallVisual(shotMass);
        }

        public float EndCharge(float chargeRate, float minHoldDuration)
        {
            _charging = false;
            _chargeBall.gameObject.SetActive(false);

            if (_chargeTime < minHoldDuration)
            {
                _mass = _chargeStartMass;
                _chargeTime = 0f;
                UpdatePlayerVisualScale();
                return 0f;
            }

            float shotMass = ComputeShotMass(chargeRate, minHoldDuration);
            if (shotMass <= 0f)
            {
                _mass = _chargeStartMass;
                UpdatePlayerVisualScale();
                return 0f;
            }

            _mass = _chargeStartMass - shotMass;
            UpdatePlayerVisualScale();
            return shotMass;
        }

        public float CurrentShotMass(float chargeRate, float minHoldDuration)
        {
            if (!_charging || _chargeTime < minHoldDuration) return 0f;
            return ComputeShotMass(chargeRate, minHoldDuration);
        }

        float ComputeShotMass(float chargeRate, float minHoldDuration)
        {
            if (_chargeTime < minHoldDuration) return 0f;
            float chargedDuration = _chargeTime - minHoldDuration;
            return Mathf.Min(chargeRate * chargedDuration, _chargeStartMass);
        }

        float MassScale()
        {
            float massFrac = Mathf.Clamp(_mass / _initialMass, MinVisualMassFrac, 1f);
            return Mathf.Pow(massFrac, 1f / 3f);
        }

        void UpdatePlayerVisualScale()
        {
            float scaleMul = MassScale();
            if (IsSceneVisual())
                _visual.localScale = _baseVisualScale * scaleMul;
            else
                _visual.localScale = Vector3.one * Mathf.Max(_basePlayerRadius * 2f * scaleMul, 0.2f);

            _visual.gameObject.SetActive(true);
            UpdateTrack();
        }

        void UpdateChargeBallVisual(float shotMass)
        {
            float chargeR = ComputeChargeRadius(shotMass);
            if (chargeR <= 0f)
            {
                _chargeBall.gameObject.SetActive(false);
                return;
            }

            _chargeBall.gameObject.SetActive(true);
            _chargeBall.localScale = Vector3.one * chargeR * 2f;

            float offset = Radius + chargeR + 0.05f;
            Vector3 worldPos = Root.position + Root.forward * offset;
            worldPos.y = Root.position.y;
            _chargeBall.SetPositionAndRotation(worldPos, Quaternion.identity);
        }

        public float ChargeRadiusForMass(float shotMass) => ComputeChargeRadius(shotMass);

        float ComputeChargeRadius(float shotMass)
        {
            if (_chargeStartMass <= 0f || _chargeMaxRadius <= 0f) return 0f;
            float t = Mathf.Clamp01(shotMass / _chargeStartMass);
            float minR = _chargeMaxRadius * MinChargeRadiusFrac;
            return Mathf.Lerp(minR, _chargeMaxRadius, t);
        }

        public void SetPosition(Vector3 pos)
        {
            float y = _useSceneY ? pos.y : Radius;
            Root.position = new Vector3(pos.x, y, pos.z);
        }

        public Vector3 Position => Root.position;

        public void AdoptRadiusFromScene(Transform sceneRoot)
        {
            _baseVisualScale = sceneRoot.localScale;

            var rend = sceneRoot.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                _basePlayerRadius = Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z);
                UpdatePlayerVisualScale();
                return;
            }

            _basePlayerRadius = 0.5f * Mathf.Max(
                sceneRoot.lossyScale.x,
                sceneRoot.lossyScale.y,
                sceneRoot.lossyScale.z);
            UpdatePlayerVisualScale();
        }

        void ApplyProceduralScale() => UpdatePlayerVisualScale();

        bool IsSceneVisual() => _moveRoot != null && _visual == _moveRoot;

        void UpdateTrack()
        {
            if (!_hasTrack) return;
            _track.Follow(Root, Radius);
        }

        public void SetForward(Vector3 dir)
        {
            if (dir.sqrMagnitude <= 0.001f) return;
            Root.forward = dir.normalized;
        }

        public void BindTrack(BallPath track)
        {
            _track = track;
            _hasTrack = track != null;
        }

        public void SetMoveRoot(Transform root)
        {
            _moveRoot = root;
            _useSceneY = root != null;
        }

        public void UseVisual(Transform visual, Material ballMat)
        {
            _visual = visual;
            _ballMat = ballMat;
            var rend = visual.GetComponent<Renderer>();
            rend.enabled = true;
            rend.sharedMaterial = ballMat;

            if (IsSceneVisual())
                AdoptRadiusFromScene(visual);
            else
                UpdatePlayerVisualScale();
        }

        public float ShotStartDistance(float shotRadius) => Radius + shotRadius + 0.05f;

        void OnDestroy()
        {
            if (_chargeBall)
                Destroy(_chargeBall.gameObject);
        }
    }
}
