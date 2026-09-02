using CleanPath.Game;
using UnityEngine;
using UnityEngine.UI;

namespace CleanPath.UI
{
    [DisallowMultipleComponent]
    public class Hud : MonoBehaviour, IHudView
    {
        [SerializeField] GameObject _massPanel;
        [SerializeField] Slider _massBar;
        [SerializeField] Text _massText;
        [SerializeField] MassBarFollower _massFollower;
        [SerializeField] GameObject _resultPanel;
        [SerializeField] Text _resultText;
        [SerializeField] Button _restartBtn;

        GameFlow _flow;
        Text _restartLabel;
        bool _initialized;
        bool _ready;
        bool _followMassBar;

        void Awake() => EnsureInitialized();

        void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _ready = ValidateUi();
            if (!_ready) return;

            _massBar.direction = Slider.Direction.BottomToTop;
            _massBar.minValue = 0f;
            _massBar.maxValue = 1f;
            _restartLabel = _restartBtn.GetComponentInChildren<Text>();
            if (!SceneValidation.Require(_restartLabel, "Hud.RestartBtn label"))
                _ready = false;
        }

        bool ValidateUi()
        {
            bool ok = true;
            ok &= SceneValidation.Require(_massBar, "Hud.MassBar");
            ok &= SceneValidation.Require(_massText, "Hud.MassText");
            ok &= SceneValidation.Require(_massPanel, "Hud.MassPanel");
            ok &= SceneValidation.Require(_resultPanel, "Hud.ResultPanel");
            ok &= SceneValidation.Require(_resultText, "Hud.ResultText");
            ok &= SceneValidation.Require(_restartBtn, "Hud.RestartBtn");
            return ok;
        }

        public void Bind(GameFlow flow)
        {
            EnsureInitialized();
            if (!_ready) return;

            _flow = flow;
            _resultPanel.SetActive(false);

            _restartLabel.text = "Restart";
            _restartBtn.onClick.AddListener(() => _flow.Restart());

            _followMassBar = _massFollower != null
                && _massFollower.TryBind(Camera.main, flow.Player, GetComponentInParent<Canvas>());
        }

        public void Refresh()
        {
            if (!_ready || _flow == null) return;

            float mass = _flow.CurrentMass;
            float frac = _flow.StartMass > 0f ? mass / _flow.StartMass : 0f;
            _massBar.value = frac;
            _massText.text = $"{mass:F1}";

            bool ended = _flow.State == FlowState.Won || _flow.State == FlowState.Lost;
            _massPanel.SetActive(!ended);

            if (_flow.State == FlowState.Won)
                ShowResult("Win");
            else if (_flow.State == FlowState.Lost)
                ShowResult("Lose");
            else
                _resultPanel.SetActive(false);
        }

        public void LateRefresh()
        {
            if (!_ready || !_followMassBar) return;
            _massFollower.TickFollow();
        }

        void ShowResult(string title)
        {
            _resultPanel.SetActive(true);
            _resultPanel.transform.SetAsLastSibling();
            _resultText.gameObject.SetActive(true);
            _resultText.text = title;
        }
    }
}
