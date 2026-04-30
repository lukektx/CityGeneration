#nullable enable
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Traffic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityGenerator.Runtime.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class CityHUDController : MonoBehaviour
    {
        [SerializeField] private CityGeneratorController _cityController = null!;
        [SerializeField] private TrafficController _trafficController = null!;
        private CityParameters _cityParameters = null!;
        private TrafficParameters _trafficParameters = null!;

        private VisualElement _genPanel = null!;
        private VisualElement _simPanel = null!;
        private Button _genPauseButton = null!;
        private Button _simPauseButton = null!;

        private void Start()
        {
            _cityParameters = _cityController.Parameters;
            _trafficParameters = _trafficController.Parameters;

            InitializeUI();
        }

        private void InitializeUI()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _genPanel = root.Q("gen-panel");
            _simPanel = root.Q("sim-panel");

            // generation sliders
            BindSliderInt(
                root.Q<SliderInt>("max-segments-slider"),
                root.Q<Label>("max-segments-value"),
                _cityParameters.MaxSegments,
                v => _cityParameters.MaxSegments = v, "0");

            BindSlider(
                root.Q<Slider>("highway-length-slider"),
                root.Q<Label>("highway-length-value"),
                _cityParameters.MajorStreetLength,
                v => _cityParameters.MajorStreetLength = v, "0");

            BindSlider(
                root.Q<Slider>("street-length-slider"),
                root.Q<Label>("street-length-value"),
                _cityParameters.MinorStreetLength,
                v => _cityParameters.MinorStreetLength = v, "0");

            BindSlider(
                root.Q<Slider>("min-length-slider"),
                root.Q<Label>("min-length-value"),
                _cityParameters.MinLengthFactor,
                v => _cityParameters.MinLengthFactor = v, "0");

            BindSlider(
                root.Q<Slider>("branch-prob-slider"),
                root.Q<Label>("branch-prob-value"),
                _cityParameters.TargetBranchRatio,
                v => _cityParameters.TargetBranchRatio = v, "F2");

            var stepThroughToggle = root.Q<Toggle>("step-through-toggle");
            stepThroughToggle.SetValueWithoutNotify(_cityParameters.StepThrough);
            stepThroughToggle.RegisterValueChangedCallback(evt =>
                _cityParameters.StepThrough = evt.newValue);

            BindSlider(
                root.Q<Slider>("step-delay-slider"),
                root.Q<Label>("step-delay-value"),
                _cityParameters.StepDelay,
                v => _cityParameters.StepDelay = v, "F2");

            BindSliderInt(
                root.Q<SliderInt>("steps-per-frame-slider"),
                root.Q<Label>("steps-per-frame-value"),
                _cityParameters.StepsPerFrame,
                v => _cityParameters.StepsPerFrame = v, "0");

            _genPauseButton = root.Q<Button>("gen-pause-button");
            _genPauseButton.clicked += OnGenPauseClicked;

            root.Q<Button>("regenerate-button").clicked += () => _cityController.Regenerate();

            // simulation sliders
            BindSliderInt(
                root.Q<SliderInt>("trips-slider"),
                root.Q<Label>("trips-value"),
                _trafficParameters.TripsToSpawn,
                v => _trafficParameters.TripsToSpawn = v, "0");

            BindSlider(
                root.Q<Slider>("speed-slider"),
                root.Q<Label>("speed-value"),
                _trafficParameters.BaseSpeed,
                v => _trafficParameters.BaseSpeed = v, "0");

            BindSlider(
                root.Q<Slider>("intersection-delay-slider"),
                root.Q<Label>("intersection-delay-value"),
                _trafficParameters.ReleaseInterval,
                v => _trafficParameters.ReleaseInterval = v, "F2");

            BindSlider(
                root.Q<Slider>("respawn-delay-slider"),
                root.Q<Label>("respawn-delay-value"),
                _trafficParameters.RespawnDelay,
                v => _trafficParameters.RespawnDelay = v, "F2");

            _simPauseButton = root.Q<Button>("pause-button");
            _simPauseButton.clicked += OnSimPauseClicked;

            SyncToMode(_trafficController.Mode);
        }

        private void OnGenPauseClicked()
        {
            bool nowPaused = !_cityController.Paused;
            _cityController.Paused = nowPaused;
            _genPauseButton.text = nowPaused ? "RESUME" : "PAUSE";
        }

        private void OnSimPauseClicked()
        {
            bool nowPaused = !_trafficController.Paused;
            _trafficController.Paused = nowPaused;
            _simPauseButton.text = nowPaused ? "RESUME" : "PAUSE";
        }

        public void SyncToMode(CityMode mode)
        {
            _genPanel.style.display = mode == CityMode.Generation
                ? DisplayStyle.Flex : DisplayStyle.None;
            _simPanel.style.display = mode == CityMode.Simulation
                ? DisplayStyle.Flex : DisplayStyle.None;

            // Sync pause button text in case state carried over
            _genPauseButton.text = _cityController.Paused ? "RESUME" : "PAUSE";
            _simPauseButton.text = _trafficController.Paused ? "RESUME" : "PAUSE";
        }

        private static void BindSlider(Slider slider, Label valueLabel,
            float initialValue, System.Action<float> onChanged, string format)
        {
            slider.SetValueWithoutNotify(initialValue);
            valueLabel.text = initialValue.ToString(format);
            slider.RegisterValueChangedCallback(evt =>
            {
                valueLabel.text = evt.newValue.ToString(format);
                onChanged(evt.newValue);
            });
        }

        private static void BindSliderInt(SliderInt slider, Label valueLabel,
            int initialValue, System.Action<int> onChanged, string format)
        {
            slider.SetValueWithoutNotify(initialValue);
            valueLabel.text = initialValue.ToString(format);
            slider.RegisterValueChangedCallback(evt =>
            {
                valueLabel.text = evt.newValue.ToString(format);
                onChanged(evt.newValue);
            });
        }
    }
}