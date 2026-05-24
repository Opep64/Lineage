using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using Godot;
using Lineage.Core;

namespace Lineage.Viewer;

/// <summary>
/// Godot-side launcher/editor for shared <see cref="SimulationScenario"/> files.
/// </summary>
///
/// <remarks>
/// The panel intentionally edits the same scenario type used by the CLI. That keeps
/// visual runs, saved JSON files, and headless experiments aligned.
/// </remarks>
public sealed partial class ScenarioEditorPanel : PanelContainer
{
    private const float ExpandedPanelWidth = 520f;
    private const float CollapsedPanelWidth = 230f;
    private const float FieldLabelWidth = 250f;

    private static readonly PropertyInfo[] ScenarioProperties = typeof(SimulationScenario)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.CanRead && property.CanWrite)
        .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .Where(IsEditableScenarioProperty)
        .OrderBy(property => property.MetadataToken)
        .ToArray();

    private readonly List<ScenarioFieldBinding> _bindings = [];
    private readonly List<SpeciesScenarioSeed> _speciesSeedEntries = [];

    private MarginContainer _expandedRoot = null!;
    private MarginContainer _collapsedRoot = null!;
    private Label _statusLabel = null!;
    private Label _lastReportLabel = null!;
    private Label _lastSnapshotLabel = null!;
    private Label _lastCheckpointLabel = null!;
    private Button _mapToggleButton = null!;
    private Button _collapsedMapToggleButton = null!;
    private Button _openReportButton = null!;
    private Button _loadSnapshotButton = null!;
    private Button _loadCheckpointButton = null!;
    private SpinBox _cliTicksInput = null!;
    private SpinBox _cliCheckpointIntervalInput = null!;
    private LineEdit _cliExperimentNameInput = null!;
    private Label _cliOutputSummaryLabel = null!;
    private LineEdit _speciesNameInput = null!;
    private LineEdit _speciesNotesInput = null!;
    private SpinBox _speciesInjectCountInput = null!;
    private SpinBox _speciesInjectEnergyInput = null!;
    private OptionButton _speciesInjectRegionInput = null!;
    private CheckBox _speciesSeedEnabledInput = null!;
    private Label _speciesRosterLabel = null!;
    private Label _loadedSpeciesLabel = null!;
    private Label _lastSpeciesExportLabel = null!;
    private Button _injectSpeciesButton = null!;
    private string? _lastReportPath;
    private string? _lastSnapshotPath;
    private string? _lastCheckpointPath;
    private string? _lastCheckpointDirectory;
    private string? _loadedSpeciesProfilePath;
    private string? _lastSpeciesExportPath;

    public bool IsCollapsed { get; private set; }

    public event Action? LaunchRequested;

    public event Action? LoadRequested;

    public event Action? SaveRequested;

    public event Action? SaveAsRequested;

    public event Action? CliRunRequested;

    public event Action? ReportRequested;

    public event Action? MapToggleRequested;

    public event Action<string>? OpenReportRequested;

    public event Action? LoadSnapshotFileRequested;

    public event Action<string?>? LoadCheckpointFileRequested;

    public event Action<string>? LoadSnapshotRequested;

    public event Action? ExportSelectedSpeciesRequested;

    public event Action? ExportSelectedSpeciesClusterRequested;

    public event Action? LoadSpeciesProfileRequested;

    public event Action? InjectSpeciesRequested;

    public event Action? CloseRequested;

    public override void _Ready()
    {
        BuildUi();
        SetScenario(new SimulationScenario());
    }

    public void SetScenario(SimulationScenario scenario)
    {
        foreach (var binding in _bindings)
        {
            var value = binding.Property.GetValue(scenario);
            SetEditorValue(binding.Editor, value);
        }

        _speciesSeedEntries.Clear();
        _speciesSeedEntries.AddRange((scenario.SpeciesSeeds ?? []).Select(seed => seed.Validated()));
        UpdateSpeciesRosterLabel();
    }

    public bool TryReadScenario(out SimulationScenario scenario, out string error)
    {
        scenario = new SimulationScenario();
        error = string.Empty;

        try
        {
            foreach (var binding in _bindings)
            {
                var value = ReadEditorValue(binding.Editor, binding.Property.PropertyType);
                binding.Property.SetValue(scenario, value);
            }

            scenario = scenario with { SpeciesSeeds = _speciesSeedEntries.ToArray() };
            scenario = scenario.Validated();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public CliRunRequest ReadCliRunRequest()
    {
        var experimentName = SanitizeExperimentName(_cliExperimentNameInput.Text);
        var experimentDirectory = System.IO.Path.Combine("out", experimentName);
        return new CliRunRequest(
            Math.Max(1, (int)Math.Round(_cliTicksInput.Value)),
            experimentName,
            System.IO.Path.Combine(experimentDirectory, $"{experimentName}_scenario.json"),
            System.IO.Path.Combine(experimentDirectory, $"{experimentName}_stats.csv"),
            System.IO.Path.Combine(experimentDirectory, $"{experimentName}_report.html"),
            System.IO.Path.Combine(experimentDirectory, $"{experimentName}_snapshot.json"),
            Math.Max(0, (int)Math.Round(_cliCheckpointIntervalInput.Value)),
            System.IO.Path.Combine(experimentDirectory, "checkpoints"));
    }

    public SpeciesInjectionUiRequest ReadSpeciesInjectionRequest()
    {
        var regionText = _speciesInjectRegionInput.GetItemText(_speciesInjectRegionInput.Selected);
        return new SpeciesInjectionUiRequest(
            Math.Max(1, (int)Math.Round(_speciesInjectCountInput.Value)),
            Enum.Parse<InitialCreatureSpawnRegion>(regionText),
            _speciesInjectEnergyInput.Value <= 0
                ? null
                : (float)_speciesInjectEnergyInput.Value);
    }

    public SpeciesExportUiRequest ReadSpeciesExportRequest()
    {
        return new SpeciesExportUiRequest(
            string.IsNullOrWhiteSpace(_speciesNameInput.Text) ? null : _speciesNameInput.Text.Trim(),
            string.IsNullOrWhiteSpace(_speciesNotesInput.Text) ? null : _speciesNotesInput.Text.Trim());
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    public void SetLastReportPath(string? path)
    {
        _lastReportPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _lastReportLabel.Text = _lastReportPath ?? "No report generated yet.";
        _openReportButton.Disabled = _lastReportPath is null;
    }

    public void SetLastSnapshotPath(string? path)
    {
        _lastSnapshotPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _lastSnapshotLabel.Text = _lastSnapshotPath ?? "No snapshot generated yet.";
        _loadSnapshotButton.Disabled = _lastSnapshotPath is null;
    }

    public void SetLastCheckpointPath(string? path, string? directory)
    {
        _lastCheckpointPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _lastCheckpointDirectory = string.IsNullOrWhiteSpace(directory) ? null : directory;

        _lastCheckpointLabel.Text = _lastCheckpointPath is not null
            ? _lastCheckpointPath
            : _lastCheckpointDirectory is not null
                ? $"No checkpoint found in {_lastCheckpointDirectory}."
                : "No checkpoint generated yet.";
        _loadCheckpointButton.Disabled = _lastCheckpointPath is null;
    }

    public void SetLoadedSpeciesProfilePath(string? path)
    {
        _loadedSpeciesProfilePath = string.IsNullOrWhiteSpace(path) ? null : path;
        _loadedSpeciesLabel.Text = _loadedSpeciesProfilePath ?? "No species profile loaded.";
        _injectSpeciesButton.Disabled = _loadedSpeciesProfilePath is null;
    }

    public void SetLastSpeciesExportPath(string? path)
    {
        _lastSpeciesExportPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _lastSpeciesExportLabel.Text = _lastSpeciesExportPath ?? "No species profile exported yet.";
    }

    public void SetMapVisible(bool isVisible)
    {
        if (_mapToggleButton is not null)
        {
            _mapToggleButton.Text = isVisible ? "Hide Map" : "Show Map";
        }

        if (_collapsedMapToggleButton is not null)
        {
            _collapsedMapToggleButton.Text = "Map";
            _collapsedMapToggleButton.TooltipText = isVisible ? "Hide map drawing" : "Show map drawing";
        }
    }

    public void ToggleCollapsed()
    {
        SetCollapsed(!IsCollapsed);
    }

    public void SetCollapsed(bool isCollapsed)
    {
        IsCollapsed = isCollapsed;

        if (_expandedRoot is not null)
        {
            _expandedRoot.Visible = !isCollapsed;
        }

        if (_collapsedRoot is not null)
        {
            _collapsedRoot.Visible = isCollapsed;
        }

        CustomMinimumSize = isCollapsed
            ? new Vector2(CollapsedPanelWidth, 56f)
            : new Vector2(ExpandedPanelWidth, 620f);
    }

    private void BuildUi()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(ExpandedPanelWidth, 620f);

        _collapsedRoot = BuildCollapsedRoot();
        _expandedRoot = BuildExpandedRoot();
        AddChild(_collapsedRoot);
        AddChild(_expandedRoot);
        SetCollapsed(isCollapsed: false);
    }

    private MarginContainer BuildCollapsedRoot()
    {
        var margin = CreateMargin();
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        row.AddChild(new Label
        {
            Text = "Launcher",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        });
        row.AddChild(CreateButton("Expand", () => SetCollapsed(isCollapsed: false)));
        _collapsedMapToggleButton = CreateButton("Map", () => MapToggleRequested?.Invoke());
        _collapsedMapToggleButton.TooltipText = "Show or hide map drawing";
        row.AddChild(_collapsedMapToggleButton);
        return margin;
    }

    private MarginContainer BuildExpandedRoot()
    {
        var margin = CreateMargin();
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        root.AddChild(new Label
        {
            Text = "Scenario Launcher",
            HorizontalAlignment = HorizontalAlignment.Center
        });

        root.AddChild(BuildButtonRow(
            CreateButton("Launch/Restart", () => LaunchRequested?.Invoke()),
            CreateButton("Load", () => LoadRequested?.Invoke()),
            CreateButton("Save", () => SaveRequested?.Invoke()),
            CreateButton("Save As", () => SaveAsRequested?.Invoke())));

        _mapToggleButton = CreateButton("Hide Map", () => MapToggleRequested?.Invoke());
        root.AddChild(BuildButtonRow(
            CreateButton("Run CLI", () => CliRunRequested?.Invoke()),
            CreateButton("Export Current", () => ReportRequested?.Invoke()),
            _mapToggleButton,
            CreateButton("Collapse", () => CloseRequested?.Invoke())));

        _statusLabel = new Label
        {
            Text = "Edit settings, then launch or run CLI.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_statusLabel);

        var tabs = new TabContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(tabs);

        tabs.AddChild(BuildScenarioTab());
        tabs.AddChild(BuildCliTab());
        tabs.AddChild(BuildSpeciesTab());
        return margin;
    }

    private static MarginContainer CreateMargin()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        return margin;
    }

    private Control BuildScenarioTab()
    {
        var scroll = new ScrollContainer
        {
            Name = "Scenario",
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        var fields = new VBoxContainer();
        fields.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(fields);

        foreach (var property in ScenarioProperties)
        {
            var editor = CreateEditor(property);
            _bindings.Add(new ScenarioFieldBinding(property, editor));
            fields.AddChild(CreateFieldRow(ToDisplayName(property.Name), editor));
        }

        return scroll;
    }

    private Control BuildCliTab()
    {
        var root = new VBoxContainer
        {
            Name = "CLI Run"
        };
        root.AddThemeConstantOverride("separation", 8);

        _cliTicksInput = CreateSpinBox(1, 10_000_000, step: 100, rounded: true);
        _cliTicksInput.Value = 5_000;
        _cliExperimentNameInput = new LineEdit
        {
            Text = "godot_launcher",
            PlaceholderText = "Experiment name"
        };
        _cliExperimentNameInput.TextChanged += _ => UpdateCliOutputSummary();
        _cliOutputSummaryLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _cliCheckpointIntervalInput = CreateSpinBox(0, 10_000_000, step: 100, rounded: true);
        _cliCheckpointIntervalInput.Value = 0;

        root.AddChild(CreateFieldRow("Experiment", _cliExperimentNameInput));
        root.AddChild(CreateFieldRow("Ticks", _cliTicksInput));
        root.AddChild(CreateFieldRow("Checkpoint interval", _cliCheckpointIntervalInput));
        root.AddChild(CreateFieldRow("Outputs", _cliOutputSummaryLabel));
        root.AddChild(CreateButton("Export Current Run", () => ReportRequested?.Invoke()));
        root.AddChild(CreateButton("Load Snapshot File", () => LoadSnapshotFileRequested?.Invoke()));
        root.AddChild(CreateButton("Load Checkpoint File", () => LoadCheckpointFileRequested?.Invoke(_lastCheckpointDirectory)));

        _lastReportLabel = new Label
        {
            Text = "No report generated yet.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _lastSnapshotLabel = new Label
        {
            Text = "No snapshot generated yet.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _lastCheckpointLabel = new Label
        {
            Text = "No checkpoint generated yet.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _openReportButton = CreateButton("Open Report", OpenLastReport);
        _openReportButton.Disabled = true;
        _loadSnapshotButton = CreateButton("Load Last Snapshot", LoadLastSnapshot);
        _loadSnapshotButton.Disabled = true;
        _loadCheckpointButton = CreateButton("Load Latest Checkpoint", LoadLatestCheckpoint);
        _loadCheckpointButton.Disabled = true;
        root.AddChild(CreateFieldRow("Last HTML report", _lastReportLabel));
        root.AddChild(_openReportButton);
        root.AddChild(CreateFieldRow("Last snapshot", _lastSnapshotLabel));
        root.AddChild(_loadSnapshotButton);
        root.AddChild(CreateFieldRow("Latest checkpoint", _lastCheckpointLabel));
        root.AddChild(_loadCheckpointButton);

        var note = new Label
        {
            Text = "Run CLI reruns the edited scenario through Lineage.Cli. Export Current Run writes CSV sidecars, an HTML report, and a reloadable snapshot from the live Godot simulation.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(note);
        UpdateCliOutputSummary();

        return root;
    }

    private void UpdateCliOutputSummary()
    {
        if (_cliOutputSummaryLabel is null)
        {
            return;
        }

        var request = ReadCliRunRequest();
        _cliOutputSummaryLabel.Text =
            $"{request.OutputPath}\n" +
            $"{request.ReportPath}\n" +
            $"{request.SnapshotPath}\n" +
            $"{request.ScenarioPath}\n" +
            $"Checkpoints: {request.CheckpointDirectory}";
    }

    private Control BuildSpeciesTab()
    {
        var root = new VBoxContainer
        {
            Name = "Species"
        };
        root.AddThemeConstantOverride("separation", 8);

        _speciesNameInput = new LineEdit { PlaceholderText = "Optional exported species name" };
        _speciesNotesInput = new LineEdit { PlaceholderText = "Optional notes" };
        _speciesInjectCountInput = CreateSpinBox(1, 10_000, step: 1, rounded: true);
        _speciesInjectCountInput.Value = 10;
        _speciesInjectEnergyInput = CreateSpinBox(0, 10_000, step: 1, rounded: false);
        _speciesInjectEnergyInput.Value = 0;
        _speciesInjectEnergyInput.TooltipText = "Use 0 for profile-derived default energy.";
        _speciesInjectRegionInput = new OptionButton();
        foreach (var name in Enum.GetNames<InitialCreatureSpawnRegion>())
        {
            _speciesInjectRegionInput.AddItem(name);
        }

        _speciesSeedEnabledInput = new CheckBox { ButtonPressed = true };
        _speciesRosterLabel = new Label
        {
            Text = "No scenario species seeds.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };

        root.AddChild(CreateFieldRow("Export name", _speciesNameInput));
        root.AddChild(CreateFieldRow("Export notes", _speciesNotesInput));
        root.AddChild(BuildButtonRow(
            CreateButton("Export Selected Creature", () => ExportSelectedSpeciesRequested?.Invoke()),
            CreateButton("Export Selected Cluster", () => ExportSelectedSpeciesClusterRequested?.Invoke())));
        _lastSpeciesExportLabel = new Label
        {
            Text = "No species profile exported yet.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(CreateFieldRow("Last export", _lastSpeciesExportLabel));

        root.AddChild(new HSeparator());
        root.AddChild(CreateButton("Load Species Profile", () => LoadSpeciesProfileRequested?.Invoke()));
        _loadedSpeciesLabel = new Label
        {
            Text = "No species profile loaded.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(CreateFieldRow("Loaded profile", _loadedSpeciesLabel));
        root.AddChild(CreateFieldRow("Inject count", _speciesInjectCountInput));
        root.AddChild(CreateFieldRow("Inject region", _speciesInjectRegionInput));
        root.AddChild(CreateFieldRow("Inject energy", _speciesInjectEnergyInput));
        _injectSpeciesButton = CreateButton("Inject Loaded Species", () => InjectSpeciesRequested?.Invoke());
        _injectSpeciesButton.Disabled = true;
        root.AddChild(_injectSpeciesButton);

        root.AddChild(new HSeparator());
        root.AddChild(CreateFieldRow("Roster entry enabled", _speciesSeedEnabledInput));
        root.AddChild(CreateButton("Add Loaded To Scenario", AddLoadedSpeciesToScenarioRoster));
        root.AddChild(BuildButtonRow(
            CreateButton("Remove Last Roster Entry", RemoveLastSpeciesSeed),
            CreateButton("Clear Roster", ClearSpeciesRoster)));
        root.AddChild(CreateFieldRow("Scenario start roster", _speciesRosterLabel));

        var note = new Label
        {
            Text = "Export stores either the selected living creature or the closest representative of its species cluster. Inject adds loaded profile copies to the current world. Add Loaded To Scenario saves the loaded profile as part of this scenario's repeatable starting roster.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(note);

        return root;
    }

    private void AddLoadedSpeciesToScenarioRoster()
    {
        if (_loadedSpeciesProfilePath is null)
        {
            SetStatus("Load a species profile before adding it to the scenario roster.");
            return;
        }

        var request = ReadSpeciesInjectionRequest();
        var seed = new SpeciesScenarioSeed
        {
            ProfilePath = _loadedSpeciesProfilePath,
            Count = request.Count,
            SpawnRegion = request.SpawnRegion,
            EnergyOverride = request.EnergyOverride,
            Enabled = _speciesSeedEnabledInput.ButtonPressed
        }.Validated();
        _speciesSeedEntries.Add(seed);
        UpdateSpeciesRosterLabel();
        SetStatus($"Added {System.IO.Path.GetFileName(seed.ProfilePath)} to the scenario roster.");
    }

    private void RemoveLastSpeciesSeed()
    {
        if (_speciesSeedEntries.Count == 0)
        {
            SetStatus("Scenario roster is already empty.");
            return;
        }

        _speciesSeedEntries.RemoveAt(_speciesSeedEntries.Count - 1);
        UpdateSpeciesRosterLabel();
        SetStatus("Removed the last scenario roster entry.");
    }

    private void ClearSpeciesRoster()
    {
        _speciesSeedEntries.Clear();
        UpdateSpeciesRosterLabel();
        SetStatus("Cleared the scenario species roster.");
    }

    private void UpdateSpeciesRosterLabel()
    {
        if (_speciesRosterLabel is null)
        {
            return;
        }

        if (_speciesSeedEntries.Count == 0)
        {
            _speciesRosterLabel.Text = "No scenario species seeds. Generic initial creatures will be used.";
            return;
        }

        _speciesRosterLabel.Text = string.Join(
            "\n",
            _speciesSeedEntries.Select((seed, index) =>
            {
                var state = seed.Enabled ? "On" : "Off";
                var energy = seed.EnergyOverride is null
                    ? "profile energy"
                    : $"{seed.EnergyOverride.Value:0.###} energy";
                return $"{index + 1}. {state}: {seed.Count} x {System.IO.Path.GetFileName(seed.ProfilePath)} in {seed.SpawnRegion} ({energy})";
            }));
    }

    private void OpenLastReport()
    {
        if (_lastReportPath is not null)
        {
            OpenReportRequested?.Invoke(_lastReportPath);
        }
    }

    private void LoadLatestCheckpoint()
    {
        if (_lastCheckpointPath is not null)
        {
            LoadSnapshotRequested?.Invoke(_lastCheckpointPath);
        }
    }

    private void LoadLastSnapshot()
    {
        if (_lastSnapshotPath is not null)
        {
            LoadSnapshotRequested?.Invoke(_lastSnapshotPath);
        }
    }

    private static HBoxContainer BuildButtonRow(params Button[] buttons)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        foreach (var button in buttons)
        {
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(button);
        }

        return row;
    }

    private static Button CreateButton(string text, Action pressed)
    {
        var button = new Button { Text = text };
        button.Pressed += pressed;
        return button;
    }

    private static bool IsEditableScenarioProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        return type == typeof(string)
            || type == typeof(ulong)
            || type == typeof(int)
            || type == typeof(float)
            || type == typeof(bool)
            || type.IsEnum;
    }

    private static Control CreateEditor(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string) || property.PropertyType == typeof(ulong))
        {
            return new LineEdit();
        }

        if (property.PropertyType.IsEnum)
        {
            var option = new OptionButton();
            foreach (var name in Enum.GetNames(property.PropertyType))
            {
                option.AddItem(name);
            }

            return option;
        }

        if (property.PropertyType == typeof(int))
        {
            return CreateSpinBox(0, 10_000_000, step: 1, rounded: true);
        }

        if (property.PropertyType == typeof(float))
        {
            return CreateSpinBox(0, 100_000, step: 0.01, rounded: false);
        }

        if (property.PropertyType == typeof(bool))
        {
            return new CheckBox();
        }

        throw new NotSupportedException($"Scenario editor does not support {property.PropertyType.Name} fields.");
    }

    private static SpinBox CreateSpinBox(double min, double max, double step, bool rounded)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Rounded = rounded,
            AllowGreater = true
        };
    }

    private static Control CreateFieldRow(string label, Control editor)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var fieldLabel = new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(FieldLabelWidth, 0f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ClipText = false
        };

        editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(fieldLabel);
        row.AddChild(editor);
        return row;
    }

    private static void SetEditorValue(Control editor, object? value)
    {
        switch (editor)
        {
            case LineEdit lineEdit:
                lineEdit.Text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                break;
            case SpinBox spinBox:
                spinBox.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            case OptionButton optionButton:
                SelectOption(optionButton, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                break;
            case CheckBox checkBox:
                checkBox.ButtonPressed = value is true;
                break;
        }
    }

    private static object ReadEditorValue(Control editor, Type targetType)
    {
        if (targetType == typeof(string) && editor is LineEdit stringLineEdit)
        {
            return stringLineEdit.Text.Trim();
        }

        if (targetType == typeof(ulong) && editor is LineEdit ulongLineEdit)
        {
            return ulong.Parse(ulongLineEdit.Text.Trim(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(int) && editor is SpinBox intSpinBox)
        {
            return (int)Math.Round(intSpinBox.Value);
        }

        if (targetType == typeof(float) && editor is SpinBox floatSpinBox)
        {
            return (float)floatSpinBox.Value;
        }

        if (targetType.IsEnum && editor is OptionButton optionButton)
        {
            return Enum.Parse(targetType, optionButton.GetItemText(optionButton.Selected));
        }

        if (targetType == typeof(bool) && editor is CheckBox checkBox)
        {
            return checkBox.ButtonPressed;
        }

        throw new InvalidOperationException($"Could not read {targetType.Name} from {editor.GetType().Name}.");
    }

    private static void SelectOption(OptionButton optionButton, string value)
    {
        for (var i = 0; i < optionButton.ItemCount; i++)
        {
            if (string.Equals(optionButton.GetItemText(i), value, StringComparison.OrdinalIgnoreCase))
            {
                optionButton.Selected = i;
                return;
            }
        }

        optionButton.Selected = 0;
    }

    private static string ToDisplayName(string name)
    {
        var words = new List<string>();
        var start = 0;

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                words.Add(name[start..i]);
                start = i;
            }
        }

        words.Add(name[start..]);
        return string.Join(' ', words);
    }

    private static string SanitizeExperimentName(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value)
            ? "godot_launcher"
            : value.Trim();
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var chars = text
            .Select(ch => invalidChars.Contains(ch) || ch is '/' or '\\' || char.IsWhiteSpace(ch) ? '_' : ch)
            .ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized)
            ? "godot_launcher"
            : sanitized;
    }

    private sealed record ScenarioFieldBinding(PropertyInfo Property, Control Editor);
}

public readonly record struct CliRunRequest(
    int Ticks,
    string ExperimentName,
    string ScenarioPath,
    string OutputPath,
    string ReportPath,
    string SnapshotPath,
    int CheckpointIntervalTicks,
    string CheckpointDirectory);

public readonly record struct SpeciesInjectionUiRequest(
    int Count,
    InitialCreatureSpawnRegion SpawnRegion,
    float? EnergyOverride);

public readonly record struct SpeciesExportUiRequest(
    string? Name,
    string? Notes);
