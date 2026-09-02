using System.Collections.Generic;
using CleanPath.Config;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    public enum FlowState { Ready, Charging, Firing, Resolving, Won, Lost }

    public enum LoseReason { None, OverCharged, NotEnoughMass }

    public class GameFlow
    {
        readonly ITapInput _input;
        readonly PlayerBall _player;
        readonly ObstacleField _field;
        readonly Projectile _projectile;
        readonly InfectionSolver _solver;
        readonly ChainVisualizer _chain;
        readonly DoorController _door;
        readonly AimRing _aimRing;
        readonly GameConfig _config;

        FlowState _state = FlowState.Ready;
        LoseReason _loseReason = LoseReason.None;
        bool _wasHeld;
        float _startMass, _pendingShotMass;
        List<InfectionResult> _lastResults;
        float _sliceStep;

        public FlowState State => _state;
        public LoseReason Lose => _loseReason;
        public float StartMass => _startMass;

        public GameFlow(ITapInput input, PlayerBall player, ObstacleField field, Projectile projectile,
            InfectionSolver solver, ChainVisualizer chain, DoorController door, AimRing aimRing,
            GameConfig config, float startMass)
        {
            _input = input;
            _player = player;
            _field = field;
            _projectile = projectile;
            _solver = solver;
            _chain = chain;
            _door = door;
            _aimRing = aimRing;
            _config = config;
            _startMass = startMass;
            _sliceStep = field.SliceStep;
            AimAtGoal();
        }

        Vector3 GoalDirection()
        {
            Vector3 dir = _door.Center - _player.Position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        }

        void AimAtGoal() => _player.SetForward(GoalDirection());

        public float CurrentMass => _player.Mass;
        public PlayerBall Player => _player;

        public void Tick(float dt)
        {
            if (_state == FlowState.Won || _state == FlowState.Lost) return;

            if (CheckWin())
            {
                _state = FlowState.Won;
                return;
            }

            bool held = _input.IsHeld;

            switch (_state)
            {
                case FlowState.Ready:
                    _door.Tick(_player.Position);
                    UpdatePlayerForward();
                    if (CheckLoseNotEnoughMass())
                    {
                        SetLose(LoseReason.NotEnoughMass);
                        break;
                    }
                    if (held && !_wasHeld)
                    {
                        _player.BeginCharge();
                        _state = FlowState.Charging;
                    }
                    break;

                case FlowState.Charging:
                {
                    var shot = _config.Shot;
                    _player.TickCharge(dt, shot.chargeRatePerSec, shot.minHoldDuration);
                    AimAtGoal();
                    float previewMass = _player.CurrentShotMass(shot.chargeRatePerSec, shot.minHoldDuration);
                    if (previewMass > 0f)
                        _aimRing.Show(_player.Position, _door.Center, _player.Radius,
                            _player.ChargeRadiusForMass(previewMass));
                    else
                        _aimRing.Hide();

                    if (_player.Mass <= _player.MinCriticalMass(_startMass) && shot.minHoldDuration > 0f
                        && _player.ChargeTime >= shot.minHoldDuration)
                    {
                        _aimRing.Hide();
                        SetLose(LoseReason.OverCharged);
                        break;
                    }

                    if (!held && _wasHeld)
                    {
                        float shotMass = _player.EndCharge(shot.chargeRatePerSec, shot.minHoldDuration);
                        _aimRing.Hide();
                        if (shotMass <= 0f) { _state = FlowState.Ready; break; }
                        FireShot(shotMass);
                    }
                    break;
                }

                case FlowState.Firing:
                    int hit = _projectile.Tick(dt);
                    if (hit == Projectile.StillFlying) break;
                    if (hit == Projectile.HitDoor)
                    {
                        _projectile.Despawn();
                        _state = FlowState.Won;
                        break;
                    }
                    if (hit >= 0) BeginResolve(hit);
                    else { _projectile.Despawn(); _state = FlowState.Ready; }
                    break;

                case FlowState.Resolving:
                    if (_chain.Tick(dt))
                        _state = FlowState.Ready;
                    break;
            }

            _wasHeld = held;
        }

        void FireShot(float shotMass)
        {
            _pendingShotMass = shotMass;
            Vector3 dir = GoalDirection();
            _player.SetForward(dir);
            float shotR = _player.ChargeRadiusForMass(shotMass);
            Vector3 origin = _player.Position + dir * _player.ShotStartDistance(shotR);
            origin.y = _player.Position.y;
            _projectile.Fire(origin, dir, shotR, _config.Shot);
            _state = FlowState.Firing;
        }

        void BeginResolve(int hitIndex)
        {
            Vector3 impactWorld = _projectile.Position;
            Vec3 ip = new Vec3(impactWorld.x, impactWorld.y, impactWorld.z);
            _lastResults = _solver.Solve(_field, ip, _pendingShotMass, _config.Ball.radiusPerMassCbrt, hitIndex);
            _projectile.Despawn();
            if (_lastResults.Count == 0) { _state = FlowState.Ready; return; }
            _chain.Play(_lastResults, OnChainDone);
            _state = FlowState.Resolving;
        }

        void OnChainDone()
        {
            _state = FlowState.Ready;
            UpdatePlayerForward();
        }

        bool CheckWin()
        {
            if (_player.Position.z >= _field.GoalZ - _sliceStep) return true;
            if (Vector3.Distance(_player.Position, _door.Center) <= _door.OpenDistance &&
                _player.Position.z >= _door.GoalZ - _sliceStep * 2f) return true;
            return false;
        }

        bool CheckLoseNotEnoughMass()
        {
            if (_player.Mass > 0f) return false;
            return _player.Position.z < _field.GoalZ - _sliceStep;
        }

        void UpdatePlayerForward() => AimAtGoal();

        void SetLose(LoseReason reason)
        {
            if (CheckWin()) return;
            _loseReason = reason;
            _state = FlowState.Lost;
            _aimRing.Hide();
        }

        public void Restart()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
