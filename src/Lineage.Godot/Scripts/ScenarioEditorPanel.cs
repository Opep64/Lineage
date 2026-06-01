using System.Globalization;
using System.Reflection;
using System.Text.Json;
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
    private const float FieldLabelWidth = 205f;
    private const string SpeciesProfileBrainOption = "Profile/default brain";
    private const string SpeciesScenarioInitialBrainOption = "Scenario initial brain";
    private const string NoCatalogBrainOption = "Profile/default brain";

    private static readonly string[] ScenarioGroupOrder =
    [
        "Basics",
        "Mutation",
        "Brain & Vision",
        "World & Terrain",
        "Plants",
        "Seasons",
        "Energy & Movement",
        "Reproduction",
        "Diet & Combat",
        "Performance",
        "Species",
        "Advanced"
    ];

    private static readonly PropertyInfo[] ScenarioProperties = typeof(SimulationScenario)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.CanRead && property.CanWrite)
        .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .Where(IsEditableScenarioProperty)
        .OrderBy(property => property.MetadataToken)
        .ToArray();

    private readonly List<ScenarioFieldBinding> _bindings = [];
    private readonly Dictionary<string, Label> _scenarioGroupEmptyLabels = [];
    private readonly List<SpeciesScenarioSeed> _speciesSeedEntries = [];
    private readonly List<ScenarioRecipeOption> _scenarioRecipes = [];
    private readonly List<BrainCatalogOption> _brainCatalogOptions = [];
    private int _incompatibleBrainCatalogProfileCount;

    private MarginContainer _expandedRoot = null!;
    private MarginContainer _collapsedRoot = null!;
    private TabContainer _scenarioGroupTabs = null!;
    private ScrollContainer _scenarioSearchScroll = null!;
    private VBoxContainer _scenarioSearchResults = null!;
    private Label _scenarioSearchEmptyLabel = null!;
    private Label _statusLabel = null!;
    private OptionButton _recipeInput = null!;
    private Button _applyRecipeButton = null!;
    private Label _recipeSummaryLabel = null!;
    private LineEdit _scenarioSearchInput = null!;
    private OptionButton _scenarioScopeInput = null!;
    private Label _lastReportLabel = null!;
    private Label _lastSnapshotLabel = null!;
    private Label _lastCheckpointLabel = null!;
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
    private OptionButton _speciesBrainOverrideInput = null!;
    private OptionButton _speciesBrainProfileInput = null!;
    private Label _brainCatalogSummaryLabel = null!;
    private CheckBox _speciesExportBrainInput = null!;
    private CheckBox _speciesSeedEnabledInput = null!;
    private Label _speciesRosterLabel = null!;
    private Label _loadedSpeciesLabel = null!;
    private Label _lastSpeciesExportLabel = null!;
    private Label _lastBrainExportLabel = null!;
    private Button _injectSpeciesButton = null!;
    private string? _lastReportPath;
    private string? _lastSnapshotPath;
    private string? _lastCheckpointPath;
    private string? _lastCheckpointDirectory;
    private string? _loadedSpeciesProfilePath;
    private string? _loadedSpeciesDefaultBrainPath;
    private string? _lastSpeciesExportPath;
    private string? _lastBrainExportPath;
    private string? _brainCatalogDirectory;

    public bool IsCollapsed { get; private set; }

    public event Action? LaunchRequested;

    public event Action? LoadRequested;

    public event Action? SaveRequested;

    public event Action? SaveAsRequested;

    public event Action? CliRunRequested;

    public event Action? ReportRequested;

    public event Action<string>? OpenReportRequested;

    public event Action? LoadSnapshotFileRequested;

    public event Action<string?>? LoadCheckpointFileRequested;

    public event Action<string>? LoadSnapshotRequested;

    public event Action? ExportSelectedSpeciesRequested;

    public event Action? ExportSelectedSpeciesClusterRequested;

    public event Action? ExportSelectedBrainRequested;

    public event Action? LoadSpeciesProfileRequested;

    public event Action? InjectSpeciesRequested;

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

    public void SetScenarioRecipeDirectory(string recipeDirectory)
    {
        LoadScenarioRecipes(recipeDirectory);
    }

    public void SetBrainCatalogDirectory(string brainCatalogDirectory)
    {
        _brainCatalogDirectory = brainCatalogDirectory;
        LoadBrainCatalog(brainCatalogDirectory);
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
        var brainProfilePath = SelectedSpeciesBrainProfilePath();
        return new SpeciesInjectionUiRequest(
            Math.Max(1, (int)Math.Round(_speciesInjectCountInput.Value)),
            Enum.Parse<InitialCreatureSpawnRegion>(regionText),
            _speciesInjectEnergyInput.Value <= 0
                ? null
                : (float)_speciesInjectEnergyInput.Value,
            brainProfilePath is null ? ReadSpeciesBrainOverrideKind() : null,
            brainProfilePath);
    }

    public SpeciesExportUiRequest ReadSpeciesExportRequest()
    {
        return new SpeciesExportUiRequest(
            string.IsNullOrWhiteSpace(_speciesNameInput.Text) ? null : _speciesNameInput.Text.Trim(),
            string.IsNullOrWhiteSpace(_speciesNotesInput.Text) ? null : _speciesNotesInput.Text.Trim(),
            _speciesExportBrainInput.ButtonPressed);
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

    public void SetLoadedSpeciesProfilePath(string? path, string? defaultBrainPath = null)
    {
        _loadedSpeciesProfilePath = string.IsNullOrWhiteSpace(path) ? null : path;
        _loadedSpeciesDefaultBrainPath = string.IsNullOrWhiteSpace(defaultBrainPath) ? null : defaultBrainPath;
        _loadedSpeciesLabel.Text = _loadedSpeciesProfilePath ?? "No species profile loaded.";
        _injectSpeciesButton.Disabled = _loadedSpeciesProfilePath is null;
        SelectBrainCatalogPath(_loadedSpeciesDefaultBrainPath);
    }

    public void SetLastSpeciesExportPath(string? path)
    {
        _lastSpeciesExportPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _lastSpeciesExportLabel.Text = _lastSpeciesExportPath ?? "No species profile exported yet.";
    }

    public void SetLastBrainExportPath(string? path)
    {
        _lastBrainExportPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _lastBrainExportLabel.Text = _lastBrainExportPath ?? "No brain profile exported yet.";
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
            _collapsedRoot.Visible = false;
        }

        CustomMinimumSize = isCollapsed
            ? Vector2.Zero
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

        root.AddChild(BuildButtonRow(
            CreateButton("Run CLI", () => CliRunRequested?.Invoke()),
            CreateButton("Export Current", () => ReportRequested?.Invoke())));

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
        var root = new VBoxContainer
        {
            Name = "Scenario",
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 8);

        root.AddChild(BuildRecipeRow());

        _scenarioSearchInput = new LineEdit
        {
            PlaceholderText = "Find setting, group, or help text"
        };
        _scenarioSearchInput.TextChanged += _ => RefreshScenarioFieldVisibility();
        root.AddChild(CreateFieldRow("Find", _scenarioSearchInput));

        _scenarioScopeInput = new OptionButton();
        _scenarioScopeInput.AddItem("All");
        _scenarioScopeInput.AddItem("Basic");
        _scenarioScopeInput.Selected = 0;
        _scenarioScopeInput.ItemSelected += _ => RefreshScenarioFieldVisibility();
        root.AddChild(CreateFieldRow("Scope", _scenarioScopeInput));

        _scenarioSearchScroll = new ScrollContainer
        {
            Visible = false,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _scenarioSearchResults = new VBoxContainer();
        _scenarioSearchResults.AddThemeConstantOverride("separation", 6);
        _scenarioSearchScroll.AddChild(_scenarioSearchResults);
        _scenarioSearchEmptyLabel = new Label
        {
            Text = "No matching settings.",
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(320f, 0f)
        };
        _scenarioSearchResults.AddChild(_scenarioSearchEmptyLabel);
        root.AddChild(_scenarioSearchScroll);

        _scenarioGroupTabs = new TabContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(_scenarioGroupTabs);

        var editableFields = ScenarioProperties
            .Select(property => new ScenarioFieldDescriptor(property, MetadataFor(property)))
            .OrderBy(field => ScenarioGroupSortKey(field.Metadata.Group))
            .ThenBy(field => field.Property.MetadataToken)
            .GroupBy(field => field.Metadata.Group);

        foreach (var group in editableFields)
        {
            var scroll = new ScrollContainer
            {
                Name = group.Key,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            var fields = new VBoxContainer();
            fields.AddThemeConstantOverride("separation", 6);
            scroll.AddChild(fields);

            var emptyLabel = new Label
            {
                Text = "No matching settings in this group.",
                Visible = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(320f, 0f)
            };
            _scenarioGroupEmptyLabels[group.Key] = emptyLabel;
            fields.AddChild(emptyLabel);

            foreach (var field in group)
            {
                var editor = CreateEditor(field.Property, field.Metadata);
                var row = CreateFieldRow(FieldLabelText(field.Metadata), editor);
                row.TooltipText = FieldTooltip(field.Metadata);
                editor.TooltipText = row.TooltipText;
                var groupRowIndex = fields.GetChildCount();
                _bindings.Add(new ScenarioFieldBinding(field.Property, editor, field.Metadata, row, fields, groupRowIndex));
                fields.AddChild(row);
            }

            _scenarioGroupTabs.AddChild(scroll);
        }

        RefreshScenarioFieldVisibility();
        return root;
    }

    private Control BuildRecipeRow()
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 4);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var label = new Label
        {
            Text = "Recipe",
            CustomMinimumSize = new Vector2(FieldLabelWidth, 0f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _recipeInput = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _recipeInput.ItemSelected += _ => UpdateRecipeControls();
        _applyRecipeButton = CreateButton("Apply", ApplySelectedRecipe);
        _applyRecipeButton.Disabled = true;

        row.AddChild(label);
        row.AddChild(_recipeInput);
        row.AddChild(_applyRecipeButton);
        root.AddChild(row);

        _recipeSummaryLabel = new Label
        {
            Text = "No recipes loaded.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(320f, 0f)
        };
        root.AddChild(_recipeSummaryLabel);
        RefreshRecipeOptions();
        return root;
    }

    private void LoadScenarioRecipes(string recipeDirectory)
    {
        _scenarioRecipes.Clear();
        if (System.IO.Directory.Exists(recipeDirectory))
        {
            foreach (var path in System.IO.Directory.EnumerateFiles(recipeDirectory, "*.json", System.IO.SearchOption.TopDirectoryOnly))
            {
                if (TryReadScenarioRecipe(path, out var recipe) && recipe is not null)
                {
                    _scenarioRecipes.Add(recipe);
                }
            }
        }

        _scenarioRecipes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        RefreshRecipeOptions();
    }

    private void LoadBrainCatalog(string brainCatalogDirectory)
    {
        _brainCatalogOptions.Clear();
        _incompatibleBrainCatalogProfileCount = 0;
        if (System.IO.Directory.Exists(brainCatalogDirectory))
        {
            var workspaceRoot = System.IO.Directory.GetParent(System.IO.Path.GetFullPath(brainCatalogDirectory))?.FullName
                ?? System.IO.Path.GetFullPath(brainCatalogDirectory);
            foreach (var path in System.IO.Directory.EnumerateFiles(
                brainCatalogDirectory,
                BrainProfileJson.FilePattern,
                System.IO.SearchOption.AllDirectories))
            {
                if (TryReadBrainCatalogOption(path, workspaceRoot, out var option) && option is not null)
                {
                    _brainCatalogOptions.Add(option);
                    if (!option.IsCompatible)
                    {
                        _incompatibleBrainCatalogProfileCount++;
                    }
                }
            }
        }

        _brainCatalogOptions.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        RefreshBrainCatalogOptions();
    }

    private static bool TryReadBrainCatalogOption(string path, string workspaceRoot, out BrainCatalogOption? option)
    {
        option = default;
        try
        {
            var profile = BrainProfileJson.LoadRaw(path);
            var compatibility = BrainProfileCompatibility.Assess(profile);
            var catalogProfile = compatibility.IsCompatible
                ? profile.Validated()
                : profile;
            var relativePath = System.IO.Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/');
            option = new BrainCatalogOption(
                string.IsNullOrWhiteSpace(catalogProfile.Name)
                    ? System.IO.Path.GetFileNameWithoutExtension(path)
                    : catalogProfile.Name.Trim(),
                relativePath,
                catalogProfile.BrainArchitectureKind.ToString(),
                catalogProfile.HiddenNodeCount,
                catalogProfile.WeightCount,
                compatibility.IsCompatible,
                compatibility.Status);
            return true;
        }
        catch (Exception ex)
        {
            var relativePath = System.IO.Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/');
            option = new BrainCatalogOption(
                System.IO.Path.GetFileNameWithoutExtension(path),
                relativePath,
                "unknown",
                0,
                0,
                false,
                $"Cannot load brain profile: {ex.Message}");
            return true;
        }
    }

    private void RefreshBrainCatalogOptions()
    {
        if (_speciesBrainProfileInput is null)
        {
            return;
        }

        _speciesBrainProfileInput.Clear();
        _speciesBrainProfileInput.AddItem(NoCatalogBrainOption);
        foreach (var brain in _brainCatalogOptions)
        {
            _speciesBrainProfileInput.AddItem(
                $"{brain.Name} ({brain.BrainArchitectureKind}, hidden {brain.HiddenNodeCount}){(brain.IsCompatible ? string.Empty : " [incompatible]")}");
            if (!brain.IsCompatible)
            {
                _speciesBrainProfileInput.SetItemDisabled(_speciesBrainProfileInput.ItemCount - 1, true);
            }
        }

        SelectBrainCatalogPath(_loadedSpeciesDefaultBrainPath);
    }

    private void SelectBrainCatalogPath(string? brainPath)
    {
        if (_speciesBrainProfileInput is null)
        {
            return;
        }

        _speciesBrainProfileInput.Selected = 0;
        if (!string.IsNullOrWhiteSpace(brainPath))
        {
            for (var index = 0; index < _brainCatalogOptions.Count; index++)
            {
                var option = _brainCatalogOptions[index];
                if (option.IsCompatible && SameCatalogPath(option.Path, brainPath))
                {
                    _speciesBrainProfileInput.Selected = index + 1;
                    break;
                }
            }
        }

        UpdateBrainCatalogSummary();
    }

    private void UpdateBrainCatalogSummary()
    {
        if (_brainCatalogSummaryLabel is null)
        {
            return;
        }

        var selected = SelectedBrainCatalogOption();
        if (selected is not null)
        {
            _brainCatalogSummaryLabel.Text = $"Catalog brain: {selected.Path} | {selected.WeightCount} weights | {selected.CompatibilityStatus}";
            return;
        }

        if (!string.IsNullOrWhiteSpace(_loadedSpeciesDefaultBrainPath)
            && _brainCatalogOptions.Count > 0
            && !_brainCatalogOptions.Any(option => option.IsCompatible && SameCatalogPath(option.Path, _loadedSpeciesDefaultBrainPath)))
        {
            _brainCatalogSummaryLabel.Text = $"Species default brain is not available as a compatible catalog profile: {_loadedSpeciesDefaultBrainPath}";
            return;
        }

        _brainCatalogSummaryLabel.Text = _brainCatalogOptions.Count == 0
            ? "No brain catalog profiles loaded."
            : $"{_brainCatalogOptions.Count - _incompatibleBrainCatalogProfileCount} compatible brain catalog profile{(_brainCatalogOptions.Count - _incompatibleBrainCatalogProfileCount == 1 ? string.Empty : "s")} available. {_incompatibleBrainCatalogProfileCount} incompatible profile{(_incompatibleBrainCatalogProfileCount == 1 ? string.Empty : "s")} shown but disabled.";
    }

    private static bool SameCatalogPath(string? left, string? right)
    {
        return string.Equals(
            (left ?? string.Empty).Replace('\\', '/'),
            (right ?? string.Empty).Replace('\\', '/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadScenarioRecipe(string path, out ScenarioRecipeOption? recipe)
    {
        recipe = default;
        try
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var root = document.RootElement;
            if (!root.TryGetProperty("changes", out var changesElement)
                || changesElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var name = root.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            var description = root.TryGetProperty("description", out var descriptionElement)
                ? descriptionElement.GetString() ?? string.Empty
                : string.Empty;
            var tags = root.TryGetProperty("tags", out var tagsElement)
                && tagsElement.ValueKind == JsonValueKind.Array
                    ? tagsElement.EnumerateArray()
                        .Where(element => element.ValueKind == JsonValueKind.String)
                        .Select(element => element.GetString() ?? string.Empty)
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .ToArray()
                    : [];
            var changes = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var change in changesElement.EnumerateObject())
            {
                changes[change.Name] = change.Value.Clone();
            }

            recipe = new ScenarioRecipeOption(
                name.Trim(),
                path,
                description.Trim(),
                tags,
                changes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RefreshRecipeOptions()
    {
        if (_recipeInput is null || _recipeSummaryLabel is null)
        {
            return;
        }

        _recipeInput.Clear();
        _recipeInput.AddItem("Choose recipe");
        foreach (var recipe in _scenarioRecipes)
        {
            var tagText = recipe.Tags.Count == 0
                ? string.Empty
                : $" [{string.Join(", ", recipe.Tags)}]";
            _recipeInput.AddItem($"{recipe.Name}{tagText}");
        }

        _recipeInput.Selected = 0;
        UpdateRecipeControls();
    }

    private void UpdateRecipeControls()
    {
        if (_recipeInput is null || _applyRecipeButton is null || _recipeSummaryLabel is null)
        {
            return;
        }

        var recipe = SelectedRecipe();
        _applyRecipeButton.Disabled = recipe is null;
        _recipeSummaryLabel.Text = recipe is null
            ? _scenarioRecipes.Count == 0
                ? "No recipes loaded."
                : $"{_scenarioRecipes.Count} recipe{(_scenarioRecipes.Count == 1 ? string.Empty : "s")} available."
            : $"{recipe.Changes.Count} setting{(recipe.Changes.Count == 1 ? string.Empty : "s")}: {string.Join(", ", recipe.Changes.Keys.Select(RecipeFieldLabel))}";
    }

    private ScenarioRecipeOption? SelectedRecipe()
    {
        if (_recipeInput is null || _recipeInput.Selected <= 0)
        {
            return null;
        }

        var index = _recipeInput.Selected - 1;
        return index >= 0 && index < _scenarioRecipes.Count
            ? _scenarioRecipes[index]
            : null;
    }

    private string RecipeFieldLabel(string jsonName)
    {
        return _bindings.FirstOrDefault(binding =>
            string.Equals(binding.Metadata.JsonName, jsonName, StringComparison.OrdinalIgnoreCase))?.Metadata.Label
            ?? jsonName;
    }

    private void ApplySelectedRecipe()
    {
        var recipe = SelectedRecipe();
        if (recipe is null)
        {
            SetStatus("Choose a recipe before applying it.");
            return;
        }

        var applied = 0;
        var skipped = 0;
        try
        {
            foreach (var (jsonName, value) in recipe.Changes)
            {
                var binding = _bindings.FirstOrDefault(candidate =>
                    string.Equals(candidate.Metadata.JsonName, jsonName, StringComparison.OrdinalIgnoreCase));
                if (binding is null)
                {
                    skipped++;
                    continue;
                }

                SetEditorValue(binding.Editor, ConvertRecipeValue(value, binding.Property.PropertyType, jsonName));
                applied++;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Recipe failed: {ex.Message}");
            return;
        }

        RefreshScenarioFieldVisibility();
        var skippedText = skipped == 0 ? string.Empty : $" Skipped {skipped} unsupported setting{(skipped == 1 ? string.Empty : "s")}.";
        SetStatus($"Applied recipe {recipe.Name}: {applied} setting{(applied == 1 ? string.Empty : "s")}.{skippedText}");
    }

    private static object? ConvertRecipeValue(JsonElement value, Type targetType, string jsonName)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return targetType == typeof(string)
                ? string.Empty
                : throw new InvalidOperationException($"{jsonName} cannot be null.");
        }

        if (targetType == typeof(string))
        {
            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : value.GetRawText();
        }

        if (targetType == typeof(ulong))
        {
            return value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var number)
                ? number
                : ulong.Parse(value.GetString() ?? value.GetRawText(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(int))
        {
            return value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : int.Parse(value.GetString() ?? value.GetRawText(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(float))
        {
            return value.ValueKind == JsonValueKind.Number
                ? value.GetSingle()
                : float.Parse(value.GetString() ?? value.GetRawText(), CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(bool))
        {
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.Parse(value.GetString() ?? string.Empty),
                _ => throw new InvalidOperationException($"{jsonName} must be true or false.")
            };
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value.GetString() ?? value.GetRawText(), ignoreCase: true);
        }

        throw new InvalidOperationException($"Recipe field {jsonName} has unsupported type {targetType.Name}.");
    }

    private void RefreshScenarioFieldVisibility()
    {
        if (_bindings.Count == 0)
        {
            return;
        }

        var query = _scenarioSearchInput?.Text.Trim() ?? string.Empty;
        var showAll = _scenarioScopeInput is null || _scenarioScopeInput.Selected == 0;
        var isSearching = !string.IsNullOrWhiteSpace(query);

        if (_scenarioGroupTabs is not null)
        {
            _scenarioGroupTabs.Visible = !isSearching;
        }

        if (_scenarioSearchScroll is not null)
        {
            _scenarioSearchScroll.Visible = isSearching;
        }

        if (isSearching)
        {
            RefreshScenarioSearchResults(query, showAll);
            return;
        }

        var visibleCounts = _scenarioGroupEmptyLabels.Keys.ToDictionary(group => group, _ => 0);

        foreach (var binding in _bindings)
        {
            MoveScenarioRow(binding.Row, binding.GroupContainer);
            binding.GroupContainer.MoveChild(binding.Row, binding.GroupRowIndex);
            var visible = (showAll || !binding.Metadata.Advanced)
                && FieldMatches(binding.Metadata, query);
            binding.Row.Visible = visible;

            if (visible)
            {
                visibleCounts[binding.Metadata.Group] = visibleCounts.GetValueOrDefault(binding.Metadata.Group) + 1;
            }
        }

        foreach (var (group, label) in _scenarioGroupEmptyLabels)
        {
            label.Visible = visibleCounts.GetValueOrDefault(group) == 0;
        }
    }

    private void RefreshScenarioSearchResults(string query, bool showAll)
    {
        var matchCount = 0;

        foreach (var label in _scenarioGroupEmptyLabels.Values)
        {
            label.Visible = false;
        }

        foreach (var binding in _bindings)
        {
            var matches = (showAll || !binding.Metadata.Advanced)
                && FieldMatches(binding.Metadata, query);

            if (!matches)
            {
                MoveScenarioRow(binding.Row, binding.GroupContainer);
                binding.GroupContainer.MoveChild(binding.Row, binding.GroupRowIndex);
                binding.Row.Visible = false;
                continue;
            }

            MoveScenarioRow(binding.Row, _scenarioSearchResults);
            binding.Row.Visible = true;
            _scenarioSearchResults.MoveChild(binding.Row, matchCount);
            matchCount++;
        }

        _scenarioSearchEmptyLabel.Text = $"No settings match \"{query}\".";
        _scenarioSearchEmptyLabel.Visible = matchCount == 0;
        _scenarioSearchResults.MoveChild(_scenarioSearchEmptyLabel, matchCount);
    }

    private static void MoveScenarioRow(Control row, Container target)
    {
        if (row.GetParent() == target)
        {
            return;
        }

        row.GetParent()?.RemoveChild(row);
        target.AddChild(row);
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

        _speciesBrainOverrideInput = new OptionButton();
        _speciesBrainOverrideInput.AddItem(SpeciesProfileBrainOption);
        _speciesBrainProfileInput = new OptionButton();
        _speciesBrainProfileInput.ItemSelected += _ => UpdateBrainCatalogSummary();
        RefreshBrainCatalogOptions();

        _speciesSeedEnabledInput = new CheckBox { ButtonPressed = true };
        _speciesRosterLabel = new Label
        {
            Text = "No scenario species seeds.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };

        root.AddChild(CreateFieldRow("Export name", _speciesNameInput));
        root.AddChild(CreateFieldRow("Export notes", _speciesNotesInput));
        _speciesExportBrainInput = new CheckBox
        {
            ButtonPressed = true,
            Text = "Save paired brain profile"
        };
        root.AddChild(CreateFieldRow("Paired brain", _speciesExportBrainInput));
        root.AddChild(BuildButtonRow(
            CreateButton("Export Selected Creature", () => ExportSelectedSpeciesRequested?.Invoke()),
            CreateButton("Export Selected Cluster", () => ExportSelectedSpeciesClusterRequested?.Invoke())));
        root.AddChild(CreateButton("Export Selected Brain", () => ExportSelectedBrainRequested?.Invoke()));
        _lastSpeciesExportLabel = new Label
        {
            Text = "No species profile exported yet.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(CreateFieldRow("Last export", _lastSpeciesExportLabel));
        _lastBrainExportLabel = new Label
        {
            Text = "No brain profile exported yet.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(CreateFieldRow("Last brain export", _lastBrainExportLabel));

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
        root.AddChild(CreateFieldRow("Brain", _speciesBrainProfileInput));
        _brainCatalogSummaryLabel = new Label
        {
            Text = "No brain catalog profiles loaded.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(CreateFieldRow("Brain catalog", _brainCatalogSummaryLabel));
        root.AddChild(CreateButton("Refresh Brain Catalog", RefreshBrainCatalog));
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
            BrainOverrideKind = request.BrainProfilePath is null ? request.BrainOverrideKind : null,
            BrainProfilePath = request.BrainProfilePath,
            Enabled = _speciesSeedEnabledInput.ButtonPressed
        }.Validated();
        _speciesSeedEntries.Add(seed);
        UpdateSpeciesRosterLabel();
        SetStatus($"Added {System.IO.Path.GetFileName(seed.ProfilePath)} to the scenario roster with {FormatSpeciesBrain(seed.BrainOverrideKind, seed.BrainProfilePath)}.");
    }

    private void RefreshBrainCatalog()
    {
        if (string.IsNullOrWhiteSpace(_brainCatalogDirectory))
        {
            SetStatus("No brain catalog directory is configured.");
            return;
        }

        LoadBrainCatalog(_brainCatalogDirectory);
        var compatibleCount = _brainCatalogOptions.Count - _incompatibleBrainCatalogProfileCount;
        SetStatus($"Loaded {compatibleCount} compatible brain catalog profile{(compatibleCount == 1 ? string.Empty : "s")}.");
    }

    private InitialBrainKind? ReadSpeciesBrainOverrideKind()
    {
        if (_speciesBrainOverrideInput.Selected <= 0)
        {
            return null;
        }

        var brainText = _speciesBrainOverrideInput.GetItemText(_speciesBrainOverrideInput.Selected);
        if (string.Equals(brainText, SpeciesScenarioInitialBrainOption, StringComparison.Ordinal))
        {
            return ReadScenarioInitialBrainKind();
        }

        return Enum.Parse<InitialBrainKind>(brainText);
    }

    private BrainCatalogOption? SelectedBrainCatalogOption()
    {
        if (_speciesBrainProfileInput is null || _speciesBrainProfileInput.Selected <= 0)
        {
            return null;
        }

        var index = _speciesBrainProfileInput.Selected - 1;
        if (index < 0 || index >= _brainCatalogOptions.Count)
        {
            return null;
        }

        var option = _brainCatalogOptions[index];
        return option.IsCompatible ? option : null;
    }

    private string? SelectedSpeciesBrainProfilePath()
    {
        return SelectedBrainCatalogOption()?.Path;
    }

    private InitialBrainKind ReadScenarioInitialBrainKind()
    {
        var binding = _bindings.FirstOrDefault(candidate =>
            candidate.Property.Name == nameof(SimulationScenario.InitialBrainKind));
        return binding is null
            ? InitialBrainKind.SectorForager
            : (InitialBrainKind)ReadEditorValue(binding.Editor, typeof(InitialBrainKind));
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
                var brain = !string.IsNullOrWhiteSpace(seed.BrainProfilePath)
                    ? $"{System.IO.Path.GetFileName(seed.BrainProfilePath)} brain profile"
                    : seed.BrainOverrideKind is null
                        ? "profile/default brain"
                        : FormatSpeciesBrain(seed.BrainOverrideKind);
                var name = string.IsNullOrWhiteSpace(seed.Label)
                    ? System.IO.Path.GetFileName(seed.ProfilePath)
                    : seed.Label;
                return $"{index + 1}. {state}: {seed.Count} x {name} in {seed.SpawnRegion} ({energy}, {brain})";
            }));
    }

    private static string FormatSpeciesBrain(InitialBrainKind? brainOverrideKind, string? brainProfilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(brainProfilePath))
        {
            return $"{System.IO.Path.GetFileName(brainProfilePath)} brain profile";
        }

        return brainOverrideKind is null
            ? "profile/default brain"
            : $"{brainOverrideKind.Value} brain";
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

    private static Control CreateEditor(PropertyInfo property, SimulationScenarioFieldMetadata metadata)
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
            return CreateSpinBox(
                metadata.Minimum ?? 0,
                metadata.Maximum ?? 10_000_000,
                metadata.Step ?? 1,
                rounded: true);
        }

        if (property.PropertyType == typeof(float))
        {
            return CreateSpinBox(
                metadata.Minimum ?? 0,
                metadata.Maximum ?? 100_000,
                metadata.Step ?? 0.01,
                rounded: false);
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

    private static SimulationScenarioFieldMetadata MetadataFor(PropertyInfo property)
    {
        return SimulationScenarioMetadata.Fields.FirstOrDefault(field => field.Name == property.Name)
            ?? new SimulationScenarioFieldMetadata(
                property.Name,
                property.Name,
                property.Name,
                "Advanced",
                "text",
                [],
                true,
                null,
                null,
                null,
                null,
                null);
    }

    private static int ScenarioGroupSortKey(string group)
    {
        var index = Array.IndexOf(ScenarioGroupOrder, group);
        return index >= 0 ? index : ScenarioGroupOrder.Length;
    }

    private static string FieldLabelText(SimulationScenarioFieldMetadata metadata)
    {
        return metadata.Units is null
            ? metadata.Label
            : $"{metadata.Label} ({metadata.Units})";
    }

    private static string FieldTooltip(SimulationScenarioFieldMetadata metadata)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            details.Add(metadata.Description);
        }

        details.Add($"Group: {metadata.Group}");
        details.Add($"JSON: {metadata.JsonName}");
        if (metadata.Advanced)
        {
            details.Add("Advanced setting");
        }

        return string.Join("\n", details);
    }

    private static bool FieldMatches(SimulationScenarioFieldMetadata metadata, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(metadata.Label, query)
            || Contains(metadata.Group, query)
            || Contains(metadata.JsonName, query)
            || Contains(metadata.Name, query)
            || Contains(metadata.Description, query)
            || Contains(metadata.Units, query);
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
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

    private sealed record ScenarioFieldDescriptor(PropertyInfo Property, SimulationScenarioFieldMetadata Metadata);

    private sealed record ScenarioFieldBinding(
        PropertyInfo Property,
        Control Editor,
        SimulationScenarioFieldMetadata Metadata,
        Control Row,
        VBoxContainer GroupContainer,
        int GroupRowIndex);

    private sealed record ScenarioRecipeOption(
        string Name,
        string Path,
        string Description,
        IReadOnlyList<string> Tags,
        IReadOnlyDictionary<string, JsonElement> Changes);

    private sealed record BrainCatalogOption(
        string Name,
        string Path,
        string BrainArchitectureKind,
        int HiddenNodeCount,
        int WeightCount,
        bool IsCompatible,
        string CompatibilityStatus);
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
    float? EnergyOverride,
    InitialBrainKind? BrainOverrideKind,
    string? BrainProfilePath);

public readonly record struct SpeciesExportUiRequest(
    string? Name,
    string? Notes,
    bool ExportPairedBrain);
