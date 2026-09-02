using System;
using System.Collections.Generic;
using CleanPath.Config;
using CleanPath.Core;
using UnityEngine;

namespace CleanPath.Game
{
    public class ChainVisualizer : MonoBehaviour
    {
        ObstacleField _field;
        InfectionConfig _cfg;
        List<InfectionResult> _pending;
        float _timer;
        int _idx;
        int _lastDestroyedHop = -1;
        Action _onDone;
        bool _playing;

        public void Init(ObstacleField field, InfectionConfig cfg)
        {
            _field = field;
            _cfg = cfg;
        }

        public void Play(List<InfectionResult> results, Action onDone)
        {
            _pending = results;
            _idx = 0;
            _timer = 0f;
            _lastDestroyedHop = -1;
            _onDone = onDone;
            _playing = results.Count > 0;
            if (!_playing) onDone?.Invoke();
        }

        public bool Tick(float dt)
        {
            if (!_playing) return true;
            _timer -= dt;
            if (_timer > 0f) return false;

            if (_idx >= _pending.Count)
            {
                if (_lastDestroyedHop >= 0)
                    DestroyHop(_lastDestroyedHop);
                _playing = false;
                _onDone?.Invoke();
                return true;
            }

            if (_lastDestroyedHop >= 0)
                DestroyHop(_lastDestroyedHop);

            int targetHop = _pending[_idx].hopIndex;
            while (_idx < _pending.Count && _pending[_idx].hopIndex == targetHop)
            {
                _field.SetInfectedVisual(_pending[_idx].index);
                _idx++;
            }

            _lastDestroyedHop = targetHop;
            _timer = _cfg.chainDelayPerHop;
            return false;
        }

        void DestroyHop(int hop)
        {
            foreach (var r in _pending)
            {
                if (r.hopIndex != hop) continue;
                if (_field.IsAlive(r.index))
                    _field.MarkDead(r.index);
            }
        }

        public bool IsPlaying => _playing;
    }
}
