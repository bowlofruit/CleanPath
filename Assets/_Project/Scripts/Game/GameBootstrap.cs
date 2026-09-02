using CleanPath.Config;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] ObstacleField _field;
        [SerializeField] PlayerBall _player;
        [SerializeField] Projectile _projectile;
        [SerializeField] ChainVisualizer _chain;
        [SerializeField] DoorController _door;
        [SerializeField] AimRing _aimRing;
        [SerializeField] MonoBehaviour _hudSource;
        [SerializeField] Camera _camera;
        [SerializeField] Material _ballMat;
        [SerializeField] Transform _sceneBall;

        ITapInput _input;
        GameFlow _flow;
        BallPath _track;
        IHudView _hud;
        BillboardFaceCamera[] _billboards = System.Array.Empty<BillboardFaceCamera>();

        void Awake()
        {
            ResolveReferences();
            if (!ValidateRequired())
            {
                enabled = false;
                return;
            }

            _track = ResolveTrack();
            _input = new PointerTapInput();
            _hud = (IHudView)_hudSource;

            var inf = _config.Infection;
            var solver = new InfectionSolver(inf.energyPerMass, inf.blastMultiplier, inf.linkCost,
                inf.costPerMeter, inf.maxLinkGap);

            _field.Init(inf.maxLinkGap);
            _field.PrepareScene(_door.GoalZ);
            _billboards = _field.GetComponentsInChildren<BillboardFaceCamera>(true);

            _camera.clearFlags = CameraClearFlags.SolidColor;

            float startMass = _config.Ball.startMass;
            _player.Init(startMass, _config.Ball, _ballMat, _track);
            AttachSceneBall();

            _projectile.Init(_ballMat, _field, _door);
            _chain.Init(_field, _config.Infection);
            _door.Init(_camera);
            _aimRing.Init(_config.Ball);

            _flow = new GameFlow(_input, _player, _field, _projectile, solver, _chain, _door,
                _aimRing, _config, startMass);
            _hud.Bind(_flow);
        }

        void ResolveReferences()
        {
#if UNITY_EDITOR
            if (_config == null)
                _config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(
                    "Assets/_Project/Config/GameConfig.asset");
#endif
            if (_sceneBall == null)
            {
                var ball = GameObject.Find("Ball");
                if (ball != null) _sceneBall = ball.transform;
            }

            if (_ballMat == null && _sceneBall != null)
            {
                var rend = _sceneBall.GetComponent<Renderer>();
                if (rend != null) _ballMat = rend.sharedMaterial;
            }

#if UNITY_EDITOR
            if (_ballMat == null)
                _ballMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Materials/Ball.mat");
#endif
        }

        bool ValidateRequired()
        {
            bool ok = true;
            ok &= SceneValidation.Require(_config, "GameConfig");
            ok &= SceneValidation.Require(_field, "ObstacleField");
            ok &= SceneValidation.Require(_player, "PlayerBall");
            ok &= SceneValidation.Require(_projectile, "Projectile");
            ok &= SceneValidation.Require(_chain, "ChainVisualizer");
            ok &= SceneValidation.Require(_door, "DoorController");
            ok &= SceneValidation.Require(_aimRing, "AimRing");
            ok &= SceneValidation.Require(_camera, "Camera");
            ok &= SceneValidation.Require(_sceneBall, "Scene Ball (assign or add GameObject named 'Ball')");
            ok &= SceneValidation.Require(_ballMat, "Ball material");
            ok &= SceneValidation.Require(_hudSource, "HUD");
            if (_hudSource != null && _hudSource is not IHudView)
            {
                Debug.LogError("CleanPath: HUD source must implement IHudView.");
                ok = false;
            }
            return ok;
        }

        BallPath ResolveTrack()
        {
            var track = _sceneBall.GetComponent<BallPath>();
            if (track == null) track = _sceneBall.gameObject.AddComponent<BallPath>();
            track.Init();
            return track;
        }

        void AttachSceneBall()
        {
            Vector3 startPos = _sceneBall.position;

            var staleVisual = _sceneBall.Find("Visual");
            if (staleVisual != null)
                staleVisual.gameObject.SetActive(false);

            var ballRenderer = _sceneBall.GetComponent<Renderer>();
            if (ballRenderer != null)
            {
                ballRenderer.enabled = true;
                ballRenderer.sharedMaterial = _ballMat;
            }

            _player.SetMoveRoot(_sceneBall);
            _player.UseVisual(_sceneBall, _ballMat);
            _player.SetPosition(startPos);
            _player.BindTrack(_track);
            _track.Follow(_sceneBall, _player.Radius);
            _player.transform.SetParent(_sceneBall, true);
            _player.transform.localPosition = Vector3.zero;

            var childVisual = _player.transform.Find("Visual");
            if (childVisual != null)
                childVisual.gameObject.SetActive(false);
        }

        void Update()
        {
            _flow.Tick(Time.deltaTime);
            _hud.Refresh();
        }

        void LateUpdate()
        {
            for (int i = 0; i < _billboards.Length; i++)
                _billboards[i].Tick(_camera);

            _hud.LateRefresh();
        }
    }
}
