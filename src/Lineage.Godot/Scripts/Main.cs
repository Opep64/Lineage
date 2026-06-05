using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Lineage.Core;

namespace Lineage.Viewer;

/// <summary>
/// First Godot shell around the core simulation.
/// </summary>
///
/// <remarks>
/// This script owns viewer concerns only: stepping the core, drawing simple shapes,
/// input, and labels. The simulation state itself remains in Lineage.Core.
/// </remarks>
public partial class Main : Node2D
{
    private const string StartupScenarioFileName = "balanced-foraging.json";
    private const string SpeciesProfileDirectoryName = "species";
    private const string BrainProfileDirectoryName = "brains";
    private const string ReadableTokensThemePath = "res://Assets/SpriteThemes/readable_tokens.png";
    private const string ReadableTokensColorMaskPath = "res://Assets/SpriteThemes/readable_tokens_color_mask.png";
    private const string PillbugTokensThemePath = "res://Assets/SpriteThemes/pillbug_tokens.png";
    private const string PillbugTokensColorMaskPath = "res://Assets/SpriteThemes/pillbug_tokens_color_mask.png";
    private const string CubeTokensThemePath = "res://Assets/SpriteThemes/cube_tokens.png";
    private const string CubeTokensColorMaskPath = "res://Assets/SpriteThemes/cube_tokens_color_mask.png";
    private const float LauncherPanelWidth = 520f;
    private const float RightPanelWidth = 640f;
    private const float ViewMargin = 24f;
    private const float SelectionPanelWidth = 560f;
    private const float SelectionPanelHeight = 460f;
    private const float SummaryActionActiveThreshold = 0.33f;
    private const float CompactStatsHeight = 184f;
    private const float InstructionsPanelHeight = 88f;
    private const float MiniGraphGap = 12f;
    private const float MiniGraphPreferredSingleColumnHeight = 125f;
    private const float DeathRateSmoothingGraphShare = 0.08f;
    private const int MinDeathRateSmoothingSamples = 8;
    private const int MaxDeathRateSmoothingSamples = 40;
    private const int GraphSampleCount = 240;
    private const int MiniGraphCount = 4;
    private const int MiniGraphCompactColumnCount = 2;
    private const int MaxGraphSamplesDrawn = 8_000;
    private const int PreferredBiomeTexturePixelsPerCell = 16;
    private const int MaxBiomeTextureDimension = 2048;
    private const int SpriteAtlasColumns = 4;
    private const int SpriteAtlasRows = 6;
    private const float MinCreatureSpriteRadiusPixels = 0.5f;
    private const float MinResourceSpriteRadiusPixels = 0.5f;
    private const float CreatureSpriteScalePixels = 8.0f;
    private const float PlantSpriteScalePixels = 6.4f;
    private const float MeatSpriteScalePixels = 6.6f;
    private const float EggSpriteScalePixels = 5.4f;
    private const float SmallPreySpriteScalePixels = 6.0f;
    private const float PlantSpriteIdleWindRotationRadians = 0.070f;
    private const float PlantSpriteIdleWindStretch = 0.046f;
    private const float PlantSpriteIdleWindSquash = 0.028f;
    private const float PlantSpriteEatingWindRotationRadians = 0.045f;
    private const float PlantSpriteEatingWindStretch = 0.030f;
    private const float PlantSpriteEatingWindSquash = 0.018f;
    private const float PlantSpriteEatingStretch = 0.14f;
    private const float PlantSpriteEatingSquash = 0.07f;
    private const float PlantSpriteEatingShakeRadians = 0.11f;
    private const float FoodSpriteEatingStretch = 0.12f;
    private const float FoodSpriteEatingSquash = 0.07f;
    private const float FoodSpriteEatingShakeRadians = 0.13f;
    private const float MinCreatureSpriteSizePixels = 22f;
    private const float MaxCreatureSpriteSizePixels = 100f;
    private const float CreatureSpriteMotionStretch = 0.16f;
    private const float CreatureSpriteMotionSquash = 0.08f;
    private const float CreatureSpriteGaitStretch = 0.035f;
    private const float CreatureSpriteGaitSquash = 0.025f;
    private const float CreatureSpriteColorMaskAlpha = 0.90f;
    private const float ColorLegendWidth = 250f;
    private const float ColorLegendPadding = 10f;
    private const float ColorLegendRowHeight = 34f;
    private const float ColorLegendSampleSize = 30f;
    private const float MinResourceSpriteSizePixels = 14f;
    private const float MaxPlantSpriteSizePixels = 50f;
    private const float MaxMeatSpriteSizePixels = 54f;
    private const float MinEggSpriteSizePixels = 13f;
    private const float MaxEggSpriteSizePixels = 42f;
    private const float MinSmallPreySpriteSizePixels = 12f;
    private const float MaxSmallPreySpriteSizePixels = 38f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 80f;
    private const float KeyboardPanPixelsPerSecond = 650f;
    private const float FollowVisibleWorldWidth = 500f;
    private const float ResourceRenderChunkSize = 512f;
    private const int MaxIndividualResourcesDrawn = 6_000;
    private const float MinIndividualResourceScreenRadius = 1.25f;
    private const float AggregateResourceTargetPixels = 22f;
    private const ulong ResourceCacheRefreshMilliseconds = 250UL;
    private const float CreatureRenderChunkSize = 512f;
    private const int MaxIndividualCreaturesDrawn = 1_500;
    private const int FarZoomIndividualCreatureLimit = 400;
    private const float AggregateCreatureTargetPixels = 24f;
    private const float FarZoomVisibleWorldWidth = 3_000f;
    private const ulong CreatureCacheRefreshMilliseconds = 100UL;
    private const float MaxResourceDensityAlpha = 0.34f;
    private const float MaxCreatureDensityAlpha = 0.42f;
    private const float MinSpeedMultiplier = 0.125f;
    private const float MaxSpeedMultiplier = 32f;
    private const int MaxSimulationStepsPerFrame = 80;
    private const double HudRefreshIntervalSeconds = 0.20;

    private readonly Color _backgroundColor = new(0.07f, 0.08f, 0.075f);
    private readonly Color _worldColor = new(0.11f, 0.13f, 0.11f);
    private readonly Color _panelColor = new(0.055f, 0.06f, 0.058f);
    private readonly Color _resourceColor = new(0.24f, 0.74f, 0.36f);
    private readonly Color _tenderPlantColor = new(0.56f, 0.94f, 0.47f);
    private readonly Color _richPlantColor = new(0.90f, 0.78f, 0.26f);
    private readonly Color _toughPlantColor = new(0.30f, 0.52f, 0.22f);
    private readonly Color _meatResourceColor = new(0.72f, 0.22f, 0.20f);
    private readonly Color _eggColor = new(0.86f, 0.88f, 0.72f);
    private readonly Color _smallPreyColor = new(0.18f, 0.78f, 0.76f);
    private readonly Color _creatureColor = new(0.82f, 0.73f, 0.48f);
    private readonly Color _selectedColor = new(1.0f, 0.94f, 0.42f);
    private readonly Color _senseColor = new(0.35f, 0.62f, 0.92f, 0.18f);
    private readonly Color _memoryColor = new(0.55f, 0.8f, 1.0f, 0.78f);
    private readonly Color _grabLinkColor = new(1.0f, 0.48f, 0.08f, 0.82f);
    private readonly Color _creatureScentRangeColor = new(0.92f, 0.48f, 1.0f, 0.95f);
    private readonly Color _meatScentRangeColor = new(1.0f, 0.42f, 0.20f, 0.96f);
    private readonly Color _soundSenseRangeColor = new(0.20f, 0.92f, 1.0f, 0.98f);
    private readonly Color _visionSectorPlantColor = new(0.32f, 0.92f, 0.45f, 0.92f);
    private readonly Color _visionSectorMeatColor = new(0.94f, 0.28f, 0.22f, 0.9f);
    private readonly Color _visionSectorEggColor = new(0.94f, 0.86f, 0.42f, 0.9f);
    private readonly Color _visionSectorCreatureColor = new(0.38f, 0.82f, 1.0f, 0.9f);
    private readonly Color _obstacleColor = new(0.035f, 0.04f, 0.038f, 0.82f);
    private readonly Color _graphPopulationColor = new(0.96f, 0.78f, 0.34f);
    private readonly Color _graphResourceColor = new(0.31f, 0.82f, 0.48f);
    private readonly Color _graphDeathColor = new(0.96f, 0.32f, 0.28f);
    private readonly Color _graphSeasonColor = new(0.48f, 0.66f, 1.0f);

    private Simulation _simulation = null!;
    private SimulationScenario _scenario = new();
    private ScenarioEditorPanel _scenarioEditor = null!;
    private FileDialog _loadScenarioDialog = null!;
    private FileDialog _saveScenarioDialog = null!;
    private FileDialog _loadSnapshotDialog = null!;
    private FileDialog _loadSpeciesProfileDialog = null!;
    private FileDialog _saveSpeciesProfileDialog = null!;
    private FileDialog _saveBrainProfileDialog = null!;
    private Label _hud = null!;
    private Label _hudSecondary = null!;
    private PanelContainer _selectionPanel = null!;
    private Label _selectionTitle = null!;
    private ScrollContainer _inspectorScroll = null!;
    private RichTextLabel _inspector = null!;
    private Label _graphLegend = null!;
    private Label _graphDetails = null!;
    private readonly Label[] _miniGraphLabels = new Label[MiniGraphCount];
    private Button _runtimeStatsButton = null!;
    private PanelContainer _runtimeStatsPanel = null!;
    private ScrollContainer _runtimeStatsScroll = null!;
    private Label _runtimeStatsLabel = null!;
    private Label _scaleBarLabel = null!;
    private bool _paused;
    private float _speedMultiplier = 1f;
    private CreatureColorMode _colorMode = CreatureColorMode.Off;
    private double _stepAccumulator;
    private EntityId _selectedCreatureId;
    private EntityId _selectedEggId;
    private SelectedInspectorView _selectedInspectorView = SelectedInspectorView.Summary;
    private readonly Dictionary<SelectedInspectorView, Button> _selectionViewButtons = [];
    private ulong _currentSeed = SimulationScenario.DefaultSeed;
    private string? _currentScenarioPath;
    private bool _cliRunInProgress;
    private bool _runExportInProgress;
    private bool _runtimeStatsVisible;
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private IReadOnlyList<SpeciesInjectionResult> _scenarioSpeciesInjections = Array.Empty<SpeciesInjectionResult>();
    private SpeciesProfile? _loadedSpeciesProfile;
    private string? _loadedSpeciesProfilePath;
    private EntityId _pendingSpeciesExportCreatureId;
    private bool _pendingSpeciesExportClusterRepresentative;
    private bool _pendingSpeciesExportPairedBrain;
    private EntityId _pendingBrainExportCreatureId;
    private string? _pendingSpeciesExportName;
    private string? _pendingSpeciesExportNotes;
    private string? _pendingBrainExportName;
    private string? _pendingBrainExportNotes;
    private bool _isPanning;
    private bool _followSelected;
    private MapOverlayMode _mapOverlayMode = MapOverlayMode.Biome;
    private bool _renderMap = true;
    private bool _showVisionSectorDebug = true;
    private VisualRenderMode _visualRenderMode = VisualRenderMode.SpriteTheme;
    private int _spriteThemeIndex;
    private readonly List<SpriteTheme> _spriteThemes = [];
    private Vector2 _lastPanPosition;
    private ResourceRenderCache _resourceRenderCache = new();
    private ulong _resourceCacheLastRefreshMilliseconds;
    private ResourceRenderMode _resourceRenderMode = ResourceRenderMode.Individual;
    private int _visibleResourceEstimate;
    private CreatureRenderCache _creatureRenderCache = new();
    private ulong _creatureCacheLastRefreshMilliseconds;
    private CreatureRenderMode _creatureRenderMode = CreatureRenderMode.Individual;
    private readonly Dictionary<EntityId, CreatureState> _drawCreatureById = [];
    private readonly HashSet<EntityId> _drawEatingPlantResourceIds = [];
    private readonly HashSet<EntityId> _drawEatingMeatResourceIds = [];
    private readonly HashSet<EntityId> _drawEatingEggIds = [];
    private readonly HashSet<EntityId> _drawEatingSmallPreyIds = [];
    private ImageTexture? _biomeOverlayTexture;
    private BiomeMap? _biomeOverlaySource;
    private int _biomeOverlayPixelsPerCell = 1;
    private ImageTexture? _temperatureOverlayTexture;
    private TemperatureMap? _temperatureOverlaySource;
    private int _temperatureOverlayPixelsPerCell = 1;
    private int _visibleCreatureEstimate;
    private int _drawnResourceCount;
    private int _drawnResourceAggregateCount;
    private int _drawnCreatureCount;
    private int _drawnCreatureAggregateCount;
    private int _livingMinGeneration;
    private int _livingMaxGeneration;
    private float _drawVisualTimeSeconds;
    private double _telemetryWindowSeconds;
    private int _telemetryFrameCount;
    private int _telemetryStepCount;
    private double _visualRefreshAccumulator;
    private double _hudRefreshAccumulator;
    private bool _forceVisualRefresh = true;
    private bool _forceHudRefresh = true;
    private float _measuredTicksPerSecond;
    private float _measuredFrameMilliseconds;

    private Rect2 _worldRect;
    private Rect2 _rightPanelRect;
    private Rect2 _graphPanelRect;
    private readonly Rect2[] _miniGraphRects = new Rect2[MiniGraphCount];
    private Rect2 _scaleBarRect;
    private SimVector2 _viewCenter;
    private float _fitWorldScale = 1f;
    private float _worldScale = 1f;
    private float _viewZoom = 1f;
    private float _scaleBarUnits = 100f;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _hud = CreateLabel(new Vector2(16f, 12f), Colors.White);
        _hudSecondary = CreateLabel(Vector2.Zero, Colors.White);
        _selectionPanel = BuildSelectionPanel();
        _graphLegend = CreateLabel(Vector2.Zero, new Color(0.9f, 0.92f, 0.88f));
        _graphLegend.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _graphDetails = CreateLabel(Vector2.Zero, new Color(0.9f, 0.92f, 0.88f));
        _graphDetails.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _graphDetails.Visible = false;
        for (var i = 0; i < _miniGraphLabels.Length; i++)
        {
            _miniGraphLabels[i] = CreateLabel(Vector2.Zero, Colors.White);
            _miniGraphLabels[i].AutowrapMode = TextServer.AutowrapMode.Off;
        }

        _runtimeStatsButton = new Button
        {
            Text = "Full Stats",
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 3
        };
        _runtimeStatsButton.Pressed += ToggleRuntimeStatsOverlay;
        _runtimeStatsPanel = BuildRuntimeStatsPanel();
        _runtimeStatsPanel.ZIndex = 10;
        _scaleBarLabel = CreateLabel(Vector2.Zero, Colors.White);
        _scaleBarLabel.HorizontalAlignment = HorizontalAlignment.Center;
        LoadStartupScenario();
        _currentSeed = _scenario.Seed;
        LoadSpriteThemes();

        AddChild(_hud);
        AddChild(_hudSecondary);
        AddChild(_selectionPanel);
        AddChild(_graphLegend);
        AddChild(_graphDetails);
        for (var i = 0; i < _miniGraphLabels.Length; i++)
        {
            AddChild(_miniGraphLabels[i]);
        }

        AddChild(_runtimeStatsButton);
        AddChild(_runtimeStatsPanel);
        AddChild(_scaleBarLabel);
        CreateScenarioLauncher();
        _scenarioEditor.SetScenario(_scenario);

        ResetSimulation(resetView: true);
    }

    private void LoadSpriteThemes()
    {
        _spriteThemes.Clear();
        AddSpriteTheme("Readable Tokens", ReadableTokensThemePath, ReadableTokensColorMaskPath, drawProceduralCreatureEyes: false);
        AddSpriteTheme("Pillbug Tokens", PillbugTokensThemePath, PillbugTokensColorMaskPath, drawProceduralCreatureEyes: false);
        AddSpriteTheme("Cube Tokens", CubeTokensThemePath, CubeTokensColorMaskPath, drawProceduralCreatureEyes: false);

        if (_spriteThemes.Count == 0)
        {
            _visualRenderMode = VisualRenderMode.LegacyShapes;
            _spriteThemeIndex = 0;
        }
    }

    private void AddSpriteTheme(string name, string resourcePath, string? colorMaskResourcePath, bool drawProceduralCreatureEyes)
    {
        var filePath = ProjectSettings.GlobalizePath(resourcePath);
        if (!System.IO.File.Exists(filePath))
        {
            GD.PushWarning($"Sprite theme not found: {resourcePath}");
            return;
        }

        var image = Image.LoadFromFile(filePath);
        if (image is null || image.IsEmpty())
        {
            GD.PushWarning($"Sprite theme could not be loaded: {resourcePath}");
            return;
        }

        var texture = ImageTexture.CreateFromImage(image);
        Texture2D? colorMaskTexture = null;
        if (!string.IsNullOrWhiteSpace(colorMaskResourcePath))
        {
            var maskFilePath = ProjectSettings.GlobalizePath(colorMaskResourcePath);
            if (System.IO.File.Exists(maskFilePath))
            {
                var maskImage = Image.LoadFromFile(maskFilePath);
                if (maskImage is not null && !maskImage.IsEmpty())
                {
                    colorMaskTexture = ImageTexture.CreateFromImage(maskImage);
                }
                else
                {
                    GD.PushWarning($"Sprite color mask could not be loaded: {colorMaskResourcePath}");
                }
            }
            else
            {
                GD.PushWarning($"Sprite color mask not found: {colorMaskResourcePath}");
            }
        }

        _spriteThemes.Add(new SpriteTheme(
            name,
            resourcePath,
            texture,
            colorMaskTexture,
            BuildSpriteAtlasRegions(texture.GetSize()),
            drawProceduralCreatureEyes));
    }

    private static Rect2[] BuildSpriteAtlasRegions(Vector2 atlasSize)
    {
        var regions = new Rect2[SpriteAtlasColumns * SpriteAtlasRows];
        var cellSize = new Vector2(atlasSize.X / SpriteAtlasColumns, atlasSize.Y / SpriteAtlasRows);
        for (var row = 0; row < SpriteAtlasRows; row++)
        {
            for (var column = 0; column < SpriteAtlasColumns; column++)
            {
                var index = row * SpriteAtlasColumns + column;
                regions[index] = new Rect2(
                    new Vector2(column * cellSize.X, row * cellSize.Y),
                    cellSize);
            }
        }

        return regions;
    }

    private PanelContainer BuildSelectionPanel()
    {
        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        _selectionTitle = new Label
        {
            Text = "Selected",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_selectionTitle);

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 6);
        root.AddChild(buttonRow);
        buttonRow.AddChild(CreateInspectorViewButton("Summary", SelectedInspectorView.Summary));
        buttonRow.AddChild(CreateInspectorViewButton("State", SelectedInspectorView.State));
        buttonRow.AddChild(CreateInspectorViewButton("Body", SelectedInspectorView.Body));
        buttonRow.AddChild(CreateInspectorViewButton("Senses", SelectedInspectorView.Senses));
        buttonRow.AddChild(CreateInspectorViewButton("Brain", SelectedInspectorView.Brain));

        _inspectorScroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _inspector = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _inspector.AddThemeColorOverride("default_color", new Color(0.9f, 0.92f, 0.88f));
        _inspectorScroll.AddChild(_inspector);
        root.AddChild(_inspectorScroll);

        RefreshSelectionViewButtons();
        return panel;
    }

    private PanelContainer BuildRuntimeStatsPanel()
    {
        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);

        header.AddChild(new Label
        {
            Text = "Runtime Stats",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });

        var closeButton = new Button
        {
            Text = "Close",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        closeButton.Pressed += ToggleRuntimeStatsOverlay;
        header.AddChild(closeButton);

        _runtimeStatsScroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _runtimeStatsLabel = CreateLabel(Vector2.Zero, new Color(0.9f, 0.92f, 0.88f));
        _runtimeStatsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _runtimeStatsLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _runtimeStatsScroll.AddChild(_runtimeStatsLabel);
        root.AddChild(_runtimeStatsScroll);

        return panel;
    }

    private void ToggleRuntimeStatsOverlay()
    {
        _runtimeStatsVisible = !_runtimeStatsVisible;
        _runtimeStatsPanel.Visible = _runtimeStatsVisible;
        _runtimeStatsButton.Text = _runtimeStatsVisible ? "Hide Stats" : "Full Stats";
        UpdateLabels();
    }

    private Button CreateInspectorViewButton(string text, SelectedInspectorView view)
    {
        var button = new Button
        {
            Text = text,
            ToggleMode = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        button.Pressed += () =>
        {
            _selectedInspectorView = view;
            RefreshSelectionViewButtons();
            UpdateLabels();
        };
        _selectionViewButtons[view] = button;
        return button;
    }

    private void RefreshSelectionViewButtons()
    {
        foreach (var (view, button) in _selectionViewButtons)
        {
            button.ButtonPressed = view == _selectedInspectorView;
        }
    }

    public override void _Process(double delta)
    {
        DrainMainThreadActions();

        var stepsThisFrame = 0;
        if (!_paused)
        {
            var fixedDelta = _simulation.Config.FixedDeltaSeconds;
            var maxAccumulatedSeconds = fixedDelta * MaxSimulationStepsPerFrame;
            _stepAccumulator = Math.Min(_stepAccumulator + delta * _speedMultiplier, maxAccumulatedSeconds);

            var maxStepsPerFrame = MaxSimulationStepsPerFrame;
            while (_stepAccumulator >= fixedDelta && maxStepsPerFrame-- > 0)
            {
                _simulation.Step();
                _stepAccumulator -= fixedDelta;
                stepsThisFrame++;
            }
        }

        UpdateTelemetry(delta, stepsThisFrame);
        _visualRefreshAccumulator += delta;
        _hudRefreshAccumulator += delta;

        var previousViewCenter = _viewCenter;
        var previousViewZoom = _viewZoom;
        HandleKeyboardCamera((float)delta);
        UpdateLayout();
        UpdateFollowCamera();
        var cameraChanged = ViewChanged(previousViewCenter, previousViewZoom);

        if (ShouldRefreshHud(cameraChanged))
        {
            UpdateLabels();
            _hudRefreshAccumulator = 0.0;
            _forceHudRefresh = false;
        }

        if (ShouldRedrawVisuals(stepsThisFrame, cameraChanged))
        {
            QueueRedraw();
            _visualRefreshAccumulator = 0.0;
            _forceVisualRefresh = false;
        }
    }

    private void DrainMainThreadActions()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            action();
        }
    }

    private bool ShouldRefreshHud(bool cameraChanged)
    {
        return _forceHudRefresh
            || cameraChanged
            || _speedMultiplier <= 4f
            || _hudRefreshAccumulator >= HudRefreshIntervalSeconds;
    }

    private bool ShouldRedrawVisuals(int stepsThisFrame, bool cameraChanged)
    {
        if (_forceVisualRefresh || cameraChanged)
        {
            return true;
        }

        if (_paused || stepsThisFrame <= 0)
        {
            return false;
        }

        var interval = GetVisualRefreshIntervalSeconds();
        return interval <= 0.0 || _visualRefreshAccumulator >= interval;
    }

    private double GetVisualRefreshIntervalSeconds()
    {
        if (_speedMultiplier <= 4f)
        {
            return 0.0;
        }

        if (_speedMultiplier <= 8f)
        {
            return 1.0 / 30.0;
        }

        if (_speedMultiplier <= 16f)
        {
            return 1.0 / 20.0;
        }

        return 1.0 / 12.0;
    }

    private bool ViewChanged(SimVector2 previousCenter, float previousZoom)
    {
        return MathF.Abs(previousCenter.X - _viewCenter.X) > 0.001f
            || MathF.Abs(previousCenter.Y - _viewCenter.Y) > 0.001f
            || MathF.Abs(previousZoom - _viewZoom) > 0.0001f;
    }

    private void RequestVisualRefresh(bool refreshHud = true)
    {
        _forceVisualRefresh = true;
        if (refreshHud)
        {
            _forceHudRefresh = true;
        }
    }

    public override void _Draw()
    {
        DrawRect(GetViewportRect(), _backgroundColor, filled: true);

        DrawRect(_rightPanelRect, _panelColor, filled: true);

        if (_renderMap)
        {
            DrawRect(_worldRect, _worldColor, filled: true);
            DrawMapOverlay();

            DrawObstacleOverlay();
            _drawVisualTimeSeconds = Time.GetTicksMsec() * 0.001f;
            UpdateFoodEatingAnimationSignals();
            DrawResources();
            DrawSmallPrey();
            DrawEggs();
            DrawCreatures();
            DrawSelectedEggOverlay();
            DrawScaleBar();
            DrawColorLegend();
        }
        else
        {
            ClearMapRenderStats();
        }

        DrawStatsGraph();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            if (keyEvent.Keycode is Key.P or Key.Space && !IsTextInputFocused())
            {
                _paused = !_paused;
                RequestVisualRefresh();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            switch (keyEvent.Keycode)
            {
                case Key.N:
                    _currentSeed = CreateNewSeed();
                    _scenario = _scenario with { Seed = _currentSeed };
                    _scenarioEditor.SetScenario(_scenario);
                    ResetSimulation(resetView: true);
                    break;
                case Key.S:
                    if (!_scenarioEditor.Visible)
                    {
                        _scenarioEditor.Visible = true;
                    }
                    else
                    {
                        _scenarioEditor.ToggleCollapsed();
                    }
                    break;
                case Key.G:
                    ToggleFollowSelected();
                    break;
                case Key.B:
                    CycleMapOverlayMode();
                    break;
                case Key.M:
                    SetMapVisible(!_renderMap);
                    break;
                case Key.C:
                    CycleColorMode();
                    break;
                case Key.V:
                    _showVisionSectorDebug = !_showVisionSectorDebug;
                    break;
                case Key.T:
                    CycleVisualRenderMode();
                    break;
                case Key.Equal:
                case Key.Plus:
                case Key.KpAdd:
                    SetSpeedMultiplier(_speedMultiplier * 2f);
                    break;
                case Key.Minus:
                case Key.KpSubtract:
                    SetSpeedMultiplier(_speedMultiplier * 0.5f);
                    break;
            }

            RequestVisualRefresh();
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Left when mouseButton.Pressed:
                    SelectEntityAt(mouseButton.Position);
                    RequestVisualRefresh();
                    break;
                case MouseButton.Right:
                case MouseButton.Middle:
                    _isPanning = mouseButton.Pressed;
                    if (mouseButton.Pressed)
                    {
                        _followSelected = false;
                    }
                    _lastPanPosition = mouseButton.Position;
                    break;
                case MouseButton.WheelUp when mouseButton.Pressed:
                    ZoomAt(mouseButton.Position, 1.2f);
                    RequestVisualRefresh();
                    break;
                case MouseButton.WheelDown when mouseButton.Pressed:
                    ZoomAt(mouseButton.Position, 1f / 1.2f);
                    RequestVisualRefresh();
                    break;
            }
        }

        if (@event is InputEventMouseMotion mouseMotion && _isPanning)
        {
            var delta = mouseMotion.Position - _lastPanPosition;
            _viewCenter -= ToWorldDelta(delta);
            ClampViewCenter();
            _lastPanPosition = mouseMotion.Position;
            RequestVisualRefresh();
        }
    }

    private void ResetSimulation(bool resetView)
    {
        _scenario = _scenario with { Seed = _currentSeed };
        _simulation = SimulationScenarioFactory.CreateSimulation(_scenario);
        _scenarioSpeciesInjections = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            _scenario,
            _simulation.State,
            _currentScenarioPath,
            GetRepositoryRoot());

        ClearSelection();
        _followSelected = false;
        _stepAccumulator = 0;
        InvalidateTerrainOverlayCache();
        InvalidateResourceRenderCache();
        InvalidateCreatureRenderCache();
        ResetTelemetry();

        if (resetView || _viewCenter == default)
        {
            ResetView();
        }
        else
        {
            ClampViewCenter();
        }

        RequestVisualRefresh();
    }

    private void SetSpeedMultiplier(float speedMultiplier)
    {
        var previousSpeed = _speedMultiplier;
        _speedMultiplier = Math.Clamp(speedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);

        if (_speedMultiplier < previousSpeed)
        {
            _stepAccumulator = Math.Min(_stepAccumulator, _simulation.Config.FixedDeltaSeconds);
        }

        RequestVisualRefresh();
    }

    private void SetMapVisible(bool renderMap)
    {
        _renderMap = renderMap;
        _scaleBarLabel.Visible = _renderMap;

        if (!_renderMap)
        {
            ClearMapRenderStats();
        }

        RequestVisualRefresh();
    }

    private void CycleMapOverlayMode()
    {
        var next = (int)_mapOverlayMode + 1;
        var count = Enum.GetValues<MapOverlayMode>().Length;
        _mapOverlayMode = (MapOverlayMode)(next % count);
        RequestVisualRefresh();
    }

    private void CycleVisualRenderMode()
    {
        if (_spriteThemes.Count == 0)
        {
            _visualRenderMode = VisualRenderMode.LegacyShapes;
            return;
        }

        if (_visualRenderMode == VisualRenderMode.LegacyShapes)
        {
            _visualRenderMode = VisualRenderMode.SpriteTheme;
            _spriteThemeIndex = 0;
            return;
        }

        _spriteThemeIndex++;
        if (_spriteThemeIndex >= _spriteThemes.Count)
        {
            _spriteThemeIndex = 0;
            _visualRenderMode = VisualRenderMode.LegacyShapes;
        }
    }

    private void ClearMapRenderStats()
    {
        _visibleResourceEstimate = 0;
        _drawnResourceCount = 0;
        _drawnResourceAggregateCount = 0;
        _resourceRenderMode = ResourceRenderMode.Individual;
        _visibleCreatureEstimate = 0;
        _drawnCreatureCount = 0;
        _drawnCreatureAggregateCount = 0;
        _creatureRenderMode = CreatureRenderMode.Individual;
    }

    private void LaunchScenarioFromEditor()
    {
        if (IsCurrentRunExportBusy("launch a scenario"))
        {
            return;
        }

        if (!_scenarioEditor.TryReadScenario(out var scenario, out var error))
        {
            _scenarioEditor.SetStatus($"Launch failed: {error}");
            return;
        }

        _scenario = scenario;
        _currentSeed = scenario.Seed;
        _paused = false;
        try
        {
            ResetSimulation(resetView: true);
            _scenarioEditor.SetStatus($"Launched {scenario.Name}.{FormatScenarioSpeciesSeedStatus()}");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Launch failed: {ex.Message}");
        }
    }

    private void SaveScenario()
    {
        if (_currentScenarioPath is null)
        {
            OpenSaveScenarioDialog();
            return;
        }

        SaveScenarioToPath(_currentScenarioPath);
    }

    private void OpenLoadScenarioDialog()
    {
        _loadScenarioDialog.PopupCenteredRatio(0.75f);
    }

    private void OpenSaveScenarioDialog()
    {
        _saveScenarioDialog.PopupCenteredRatio(0.75f);
    }

    private void LoadScenarioFromPath(string path)
    {
        if (IsCurrentRunExportBusy("load a scenario"))
        {
            return;
        }

        try
        {
            _scenario = SimulationScenarioJson.Load(path);
            _currentSeed = _scenario.Seed;
            _currentScenarioPath = path;
            _scenarioEditor.SetScenario(_scenario);
            ResetSimulation(resetView: true);
            _scenarioEditor.SetStatus($"Loaded {System.IO.Path.GetFileName(path)}.{FormatScenarioSpeciesSeedStatus()}");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Load failed: {ex.Message}");
        }
    }

    private void SaveScenarioToPath(string path)
    {
        if (!_scenarioEditor.TryReadScenario(out var scenario, out var error))
        {
            _scenarioEditor.SetStatus($"Save failed: {error}");
            return;
        }

        try
        {
            SimulationScenarioJson.Save(path, scenario);
            _scenario = scenario;
            _currentSeed = scenario.Seed;
            _currentScenarioPath = path;
            _scenarioEditor.SetStatus($"Saved {System.IO.Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Save failed: {ex.Message}");
        }
    }

    private void RunCliFromEditor()
    {
        if (_cliRunInProgress)
        {
            _scenarioEditor.SetStatus("CLI run already in progress.");
            return;
        }

        if (!_scenarioEditor.TryReadScenario(out var scenario, out var error))
        {
            _scenarioEditor.SetStatus($"CLI run failed: {error}");
            return;
        }

        var request = _scenarioEditor.ReadCliRunRequest();
        var workspaceRoot = GetRepositoryRoot();
        var cliProjectPath = System.IO.Path.Combine(workspaceRoot, "src", "Lineage.Cli", "Lineage.Cli.csproj");
        var cliScenarioPath = ResolveWorkspacePath(request.ScenarioPath, workspaceRoot);
        var outputPath = ResolveWorkspacePath(request.OutputPath, workspaceRoot);
        var reportPath = ResolveWorkspacePath(request.ReportPath, workspaceRoot);
        var snapshotPath = ResolveWorkspacePath(request.SnapshotPath, workspaceRoot);
        var checkpointDirectory = request.CheckpointIntervalTicks > 0
            ? ResolveWorkspacePath(request.CheckpointDirectory, workspaceRoot)
            : string.Empty;

        _scenario = scenario;
        _currentSeed = scenario.Seed;
        _scenarioEditor.SetScenario(_scenario);
        _scenarioEditor.SetStatus("CLI running (Release)...");
        _scenarioEditor.SetLastReportPath(null);
        _scenarioEditor.SetLastSnapshotPath(null);
        _scenarioEditor.SetLastCheckpointPath(null, request.CheckpointIntervalTicks > 0 ? checkpointDirectory : null);
        _cliRunInProgress = true;

        _ = RunCliAsync(
            scenario,
            request.Ticks,
            workspaceRoot,
            cliProjectPath,
            cliScenarioPath,
            outputPath,
            reportPath,
            snapshotPath,
            request.CheckpointIntervalTicks,
            checkpointDirectory);
    }

    private async Task RunCliAsync(
        SimulationScenario scenario,
        int ticks,
        string workspaceRoot,
        string cliProjectPath,
        string temporaryScenarioPath,
        string outputPath,
        string reportPath,
        string snapshotPath,
        int checkpointIntervalTicks,
        string checkpointDirectory)
    {
        var result = await Task.Run(() => RunCliProcess(
            scenario,
            ticks,
            workspaceRoot,
            cliProjectPath,
            temporaryScenarioPath,
            outputPath,
            reportPath,
            snapshotPath,
            checkpointIntervalTicks,
            checkpointDirectory));

        CallDeferred(
            nameof(CompleteCliRun),
            result.ExitCode,
            result.Message,
            result.ReportPath,
            result.SnapshotPath,
            result.CheckpointDirectory,
            result.LatestCheckpointPath);
    }

    public void CompleteCliRun(
        int exitCode,
        string message,
        string reportPath,
        string snapshotPath,
        string checkpointDirectory,
        string latestCheckpointPath)
    {
        _cliRunInProgress = false;
        _scenarioEditor.SetLastReportPath(exitCode == 0 ? reportPath : null);
        _scenarioEditor.SetLastSnapshotPath(exitCode == 0 ? snapshotPath : null);
        _scenarioEditor.SetLastCheckpointPath(
            exitCode == 0 ? latestCheckpointPath : null,
            exitCode == 0 ? checkpointDirectory : null);
        _scenarioEditor.SetStatus(exitCode == 0
            ? $"CLI complete. {message}"
            : $"CLI failed ({exitCode}). {message}");
    }

    private void OpenReportInBrowser(string path)
    {
        try
        {
            var uri = new Uri(path).AbsoluteUri;
            OS.ShellOpen(uri);
            _scenarioEditor.SetStatus($"Opened report: {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Open report failed: {ex.Message}");
        }
    }

    private static CliRunResult RunCliProcess(
        SimulationScenario scenario,
        int ticks,
        string workspaceRoot,
        string cliProjectPath,
        string temporaryScenarioPath,
        string outputPath,
        string reportPath,
        string snapshotPath,
        int checkpointIntervalTicks,
        string checkpointDirectory)
    {
        try
        {
            SimulationScenarioJson.Save(temporaryScenarioPath, scenario);

            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workspaceRoot
            };

            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("Release");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(cliProjectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--scenario");
            startInfo.ArgumentList.Add(temporaryScenarioPath);
            startInfo.ArgumentList.Add("--ticks");
            startInfo.ArgumentList.Add(ticks.ToString(CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(outputPath);
            startInfo.ArgumentList.Add("--report");
            startInfo.ArgumentList.Add(reportPath);
            startInfo.ArgumentList.Add("--save-snapshot");
            startInfo.ArgumentList.Add(snapshotPath);
            if (checkpointIntervalTicks > 0)
            {
                startInfo.ArgumentList.Add("--checkpoint-interval");
                startInfo.ArgumentList.Add(checkpointIntervalTicks.ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add("--checkpoint-dir");
                startInfo.ArgumentList.Add(checkpointDirectory);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start dotnet.");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return new CliRunResult(process.ExitCode, LastLine(stderr, stdout), string.Empty, string.Empty, string.Empty, string.Empty);
            }

            var latestCheckpointPath = checkpointIntervalTicks > 0
                ? FindLatestCheckpoint(checkpointDirectory) ?? string.Empty
                : string.Empty;
            var checkpointText = checkpointIntervalTicks > 0
                ? string.IsNullOrWhiteSpace(latestCheckpointPath)
                    ? $" Checkpoint dir: {checkpointDirectory}"
                    : $" Latest checkpoint: {latestCheckpointPath}"
                : string.Empty;
            return new CliRunResult(
                0,
                $"Report: {reportPath} Snapshot: {snapshotPath}{checkpointText}",
                reportPath,
                snapshotPath,
                checkpointIntervalTicks > 0 ? checkpointDirectory : string.Empty,
                latestCheckpointPath);
        }
        catch (Exception ex)
        {
            return new CliRunResult(-1, ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
        }
    }

    private void UpdateLayout()
    {
        var viewport = GetViewportRect();
        var launcherWidth = GetLauncherWidth();
        var launcherExpanded = _scenarioEditor.Visible && !_scenarioEditor.IsCollapsed;
        var leftEdge = launcherExpanded
            ? launcherWidth + ViewMargin * 2f
            : ViewMargin;
        var worldScreenWidth = MathF.Max(100f, viewport.Size.X - leftEdge - RightPanelWidth - ViewMargin * 2f);
        var worldScreenHeight = MathF.Max(100f, viewport.Size.Y - ViewMargin * 2f);
        _fitWorldScale = MathF.Min(
            worldScreenWidth / _simulation.State.Bounds.Width,
            worldScreenHeight / _simulation.State.Bounds.Height);
        _worldScale = _fitWorldScale * _viewZoom;

        _worldRect = new Rect2(new Vector2(leftEdge, ViewMargin), new Vector2(worldScreenWidth, worldScreenHeight));
        var rightPanelPosition = _worldRect.Position + new Vector2(_worldRect.Size.X + 12f, 0f);
        _rightPanelRect = new Rect2(rightPanelPosition, new Vector2(RightPanelWidth, _worldRect.Size.Y));
        var rightPanelContentX = rightPanelPosition.X + 12f;
        var rightPanelContentWidth = RightPanelWidth - 24f;
        var rightPanelTop = rightPanelPosition.Y + 8f;
        var statsColumnGap = 18f;
        var statsColumnWidth = (rightPanelContentWidth - statsColumnGap) * 0.5f;

        _hud.Position = new Vector2(rightPanelContentX, rightPanelTop);
        _hud.Size = new Vector2(statsColumnWidth, CompactStatsHeight);
        _hudSecondary.Position = new Vector2(rightPanelContentX + statsColumnWidth + statsColumnGap, rightPanelTop + 34f);
        _hudSecondary.Size = new Vector2(statsColumnWidth, CompactStatsHeight - 34f);

        _runtimeStatsButton.Position = new Vector2(rightPanelPosition.X + RightPanelWidth - 116f, rightPanelTop);
        _runtimeStatsButton.Size = new Vector2(104f, 30f);

        var instructionsTop = rightPanelTop + CompactStatsHeight + 8f;
        _graphLegend.Position = new Vector2(rightPanelContentX, instructionsTop);
        _graphLegend.Size = new Vector2(rightPanelContentWidth, InstructionsPanelHeight);
        _graphDetails.Visible = false;

        var graphTop = instructionsTop + InstructionsPanelHeight + 10f;
        var graphAreaHeight = MathF.Max(240f, _worldRect.End.Y - graphTop - 12f);
        _graphPanelRect = new Rect2(
            new Vector2(rightPanelContentX, graphTop),
            new Vector2(rightPanelContentWidth, graphAreaHeight));
        var singleColumnHeight = (_graphPanelRect.Size.Y - MiniGraphGap * (MiniGraphCount - 1)) / MiniGraphCount;
        var graphColumnCount = singleColumnHeight >= MiniGraphPreferredSingleColumnHeight
            ? 1
            : MiniGraphCompactColumnCount;
        var graphRows = Math.Max(1, (int)Math.Ceiling(MiniGraphCount / (float)graphColumnCount));
        var miniGraphWidth = MathF.Max(
            120f,
            (_graphPanelRect.Size.X - MiniGraphGap * (graphColumnCount - 1)) / graphColumnCount);
        var miniGraphHeight = MathF.Max(
            54f,
            (_graphPanelRect.Size.Y - MiniGraphGap * (graphRows - 1)) / graphRows);
        for (var i = 0; i < MiniGraphCount; i++)
        {
            var column = i % graphColumnCount;
            var row = i / graphColumnCount;
            var rect = new Rect2(
                _graphPanelRect.Position + new Vector2(
                    column * (miniGraphWidth + MiniGraphGap),
                    row * (miniGraphHeight + MiniGraphGap)),
                new Vector2(miniGraphWidth, miniGraphHeight));
            _miniGraphRects[i] = rect;
            _miniGraphLabels[i].Position = rect.Position + new Vector2(12f, 6f);
            _miniGraphLabels[i].Size = new Vector2(rect.Size.X - 24f, 24f);
        }

        var overlayWidth = MathF.Min(460f, MathF.Max(320f, _worldRect.Size.X * 0.36f));
        _runtimeStatsPanel.Position = new Vector2(_worldRect.End.X - overlayWidth - 12f, _worldRect.Position.Y + 12f);
        _runtimeStatsPanel.Size = new Vector2(overlayWidth, MathF.Max(260f, _worldRect.Size.Y - 24f));
        _runtimeStatsLabel.CustomMinimumSize = new Vector2(overlayWidth - 44f, 0f);

        var hasSelection = HasSelectedEntity();
        _selectionPanel.Visible = hasSelection;
        if (hasSelection)
        {
            var selectionWidth = MathF.Min(SelectionPanelWidth, MathF.Max(280f, _worldRect.Size.X - 24f));
            var selectionHeight = MathF.Min(SelectionPanelHeight, MathF.Max(260f, _worldRect.Size.Y - 24f));
            _selectionPanel.Position = _worldRect.Position + new Vector2(12f, 12f);
            _selectionPanel.Size = new Vector2(selectionWidth, selectionHeight);
            _inspector.CustomMinimumSize = new Vector2(selectionWidth - 44f, 0f);
        }

        _scenarioEditor.Position = new Vector2(12f, 12f);
        _scenarioEditor.Size = _scenarioEditor.IsCollapsed
            ? Vector2.Zero
            : new Vector2(LauncherPanelWidth, MathF.Max(240f, viewport.Size.Y - 24f));
        ClampViewCenter();
        UpdateScaleBarLayout();
    }

    private void UpdateTelemetry(double frameSeconds, int stepsThisFrame)
    {
        _telemetryWindowSeconds += frameSeconds;
        _telemetryFrameCount++;
        _telemetryStepCount += stepsThisFrame;

        if (_telemetryWindowSeconds < 0.5)
        {
            return;
        }

        _measuredTicksPerSecond = (float)(_telemetryStepCount / _telemetryWindowSeconds);
        _measuredFrameMilliseconds = (float)(_telemetryWindowSeconds / Math.Max(1, _telemetryFrameCount) * 1_000.0);
        _telemetryWindowSeconds = 0;
        _telemetryFrameCount = 0;
        _telemetryStepCount = 0;
    }

    private void ResetTelemetry()
    {
        _telemetryWindowSeconds = 0;
        _telemetryFrameCount = 0;
        _telemetryStepCount = 0;
        _measuredTicksPerSecond = 0f;
        _measuredFrameMilliseconds = 0f;
        _visualRefreshAccumulator = 0.0;
        _hudRefreshAccumulator = 0.0;
        RequestVisualRefresh();
    }

    private void UpdateLabels()
    {
        var state = _simulation.State;
        var snapshot = state.Stats.Snapshots.Count > 0 ? state.Stats.Snapshots[^1] : default;
        var activeResourceCount = state.Stats.Snapshots.Count > 0
            ? snapshot.ResourceCount
            : CountActiveResources(state.Resources);
        var worldArea = MathF.Max(1f, state.Bounds.Width * state.Bounds.Height);
        var resourceDensity = activeResourceCount / worldArea * 1_000_000f;
        var centerBiome = state.Biomes.GetKindAt(_viewCenter);
        var centerTemperature = state.Temperature.GetTemperatureAt(_viewCenter);
        var season = SeasonalFertility.CalculateAt(
            _scenario.EnableSeasons,
            state.ElapsedSeconds,
            _scenario.SeasonLengthSeconds,
            _scenario.SeasonFertilityAmplitude,
            _scenario.SeasonPhaseOffsetSeconds,
            _scenario.SeasonPhaseMode,
            state.Bounds,
            _viewCenter);
        var biomeSeason = SeasonalFertility.CalculateBiomeMultiplierAt(
            _scenario.EnableSeasons,
            state.ElapsedSeconds,
            _scenario.SeasonLengthSeconds,
            _scenario.SeasonFertilityAmplitude,
            _scenario.SeasonPhaseOffsetSeconds,
            _scenario.SeasonPhaseMode,
            state.Bounds,
            _viewCenter,
            centerBiome,
            _scenario.CreateBiomeSeasonalAmplitudeProfile());
        var seasonText = _scenario.EnableSeasons
            ? $"Season {season.Phase * 100f:0}%  Here {season.FertilityMultiplier:0.00}x  Biome {biomeSeason:0.00}x\n"
            : string.Empty;
        var centerVoidText = state.Biomes.IsInResourceVoid(_viewCenter) ? " void" : string.Empty;
        var grabStats = CalculateLiveGrabStats(state.Creatures);

        _hud.Text =
            $"Lineage\n" +
            $"{(_paused ? "Paused" : "Running")}  {FormatSpeed(_speedMultiplier)}\n" +
            $"TPS {_measuredTicksPerSecond:0.0}  Frame {_measuredFrameMilliseconds:0.0}ms\n" +
            $"Seed {_currentSeed}\n" +
            $"Tick {state.Tick}  Time {state.ElapsedSeconds:0.0}s\n" +
            $"World {state.Bounds.Width:0}x{state.Bounds.Height:0}\n" +
            $"Color {FormatColorMode(_colorMode)}";

        _hudSecondary.Text =
            $"Life avg {snapshot.AverageLifespanSeconds:0}s  med {snapshot.MedianLifespanSeconds:0}s\n" +
            $"Max gen {snapshot.MaxGeneration}\n" +
            $"Creatures {state.Creatures.Count}  Eggs {state.Eggs.Count}  Food {activeResourceCount}\n" +
            $"Plants {snapshot.PlantResourceCount}  Meat {snapshot.MeatResourceCount}  Prey {snapshot.SmallPreyCount}\n" +
            $"Energy full {FormatPercent(snapshot.AverageEnergyFullnessRatio)}  Fat {snapshot.TotalFatCalories:0} kcal  reserve {FormatPercent(snapshot.AverageFatRatio)}\n" +
            $"Deaths {state.Stats.CreatureDeathCount}  Starved {state.Stats.StarvationDeathCount}\n" +
            $"Visual {FormatVisualRenderMode()}\n" +
            (seasonText.Length > 0 ? seasonText.TrimEnd() : "Season off");

        _runtimeStatsLabel.Text =
            $"Lineage\n" +
            $"{(_paused ? "Paused" : "Running")}  {FormatSpeed(_speedMultiplier)}\n" +
            $"TPS {_measuredTicksPerSecond:0.0}  Frame {_measuredFrameMilliseconds:0.0}ms\n" +
            $"Seed {_currentSeed}\n" +
            $"Tick {state.Tick}  Time {state.ElapsedSeconds:0.0}s\n" +
            $"World {state.Bounds.Width:0}x{state.Bounds.Height:0}\n" +
            $"Creatures {state.Creatures.Count}  Eggs {state.Eggs.Count}  Food {activeResourceCount}\n" +
            $"Plants {snapshot.PlantResourceCount}  Meat {snapshot.MeatResourceCount}  Prey {snapshot.SmallPreyCount}\n" +
            $"Resources/M {resourceDensity:0.00}\n" +
            seasonText +
            $"Births {state.Stats.CreatureBirthCount}  Eggs laid {state.Stats.EggLaidCount}\n" +
            $"Repro attempts {state.Stats.ReproductionAttemptCount}  success {FormatPercent(Share(state.Stats.EggLaidCount, state.Stats.ReproductionAttemptCount))}\n" +
            $"Hatched {state.Stats.EggHatchedCount}  Egg deaths {state.Stats.EggDeathCount}  Pred {state.Stats.EggPredationDeathCount}\n" +
            $"Egg health {snapshot.AverageEggHealthRatio * 100f:0}%  Birth inv {snapshot.AverageBirthInvestmentRatio:0.00}x\n" +
            $"Pace {snapshot.AverageMetabolicPace:0.00} avg  low/norm/high {snapshot.LowMetabolicPaceCreatureCount}/{snapshot.NormalMetabolicPaceCreatureCount}/{snapshot.HighMetabolicPaceCreatureCount}\n" +
            $"Energy full {FormatPercent(snapshot.AverageEnergyFullnessRatio)}  cap {snapshot.AverageEnergyCapacityCalories:0}  overflow {snapshot.TotalEnergyOverflowCaloriesPerSecond:0.0}/s\n" +
            $"Fat {snapshot.TotalFatCalories:0} kcal  reserve {FormatPercent(snapshot.AverageFatRatio)}  burden {FormatPercent(snapshot.AverageMassBurdenRatio)}  speed {snapshot.AverageFatSpeedMultiplier:0.00}x  cap {snapshot.AverageFatStorageCapacityCalories:0} eff {FormatPercent(snapshot.AverageFatStorageEfficiency)}\n" +
            $"Deaths {state.Stats.CreatureDeathCount}  Starved {state.Stats.StarvationDeathCount}  Rotten {state.Stats.RottenMeatDeathCount}\n" +
            $"Repro intent {FormatPercent(Share(snapshot.ReproductionIntentCreatureCount, snapshot.CreatureCount))}  ready {FormatPercent(Share(snapshot.ReproductionReadyCreatureCount, snapshot.CreatureCount))}\n" +
            $"Life avg {snapshot.AverageLifespanSeconds:0}s  med {snapshot.MedianLifespanSeconds:0}s\n" +
            $"Max gen {snapshot.MaxGeneration}\n" +
            $"Food seen {FormatPercent(Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount))}  P {FormatPercent(Share(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount))}  M {FormatPercent(Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount))}\n" +
            $"Meat scent {FormatPercent(Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount))}  rot {FormatPercent(Share(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount))}  density {snapshot.AverageMeatScentDensity:0.00}/{snapshot.AverageRottenMeatScentDensity:0.00}\n" +
            $"Meat seen fresh {FormatPercent(Share(snapshot.FreshMeatDetectedCreatureCount, snapshot.CreatureCount))} stale {FormatPercent(Share(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount))} avoided {FormatPercent(Share(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount))}\n" +
            $"Eating {FormatPercent(Share(snapshot.EatingCreatureCount, snapshot.CreatureCount))}  Raw {snapshot.TotalCaloriesEatenPerSecond:0.0}/s  Digest {snapshot.TotalCaloriesDigestedPerSecond:0.0}/s\n" +
            $"Food src P {snapshot.TotalPlantCaloriesEatenPerSecond:0.0}/s  C {snapshot.TotalCarcassCaloriesEatenPerSecond:0.0}/s  Egg {snapshot.TotalEggCaloriesEatenPerSecond:0.0}/s  Prey {snapshot.TotalSmallPreyCaloriesEatenPerSecond:0.0}/s\n" +
            $"Small prey kcal {snapshot.TotalSmallPreyCalories:0}  spawn/kill/eat {snapshot.SmallPreySpawnedCount}/{snapshot.SmallPreyKilledCount}/{snapshot.SmallPreyEatenCount}\n" +
            $"Creatures seen {FormatPercent(Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount))}  density {snapshot.AverageVisibleCreatureDensity:0.00}\n" +
            $"Kin scent {FormatPercent(Share(snapshot.CreatureSimilarityScentDetectedCreatureCount, snapshot.CreatureCount))}  lineage {FormatPercent(Share(snapshot.CreatureLineageScentDetectedCreatureCount, snapshot.CreatureCount))}  density {snapshot.AverageCreatureSimilarityScentDensity:0.00}/{snapshot.AverageCreatureLineageScentDensity:0.00}\n" +
            $"Contact {FormatPercent(Share(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount))}  intent {FormatPercent(Share(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount))}  touch+intent {FormatPercent(Share(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount))}\n" +
            $"Similar/lineage touch {FormatPercent(Share(snapshot.SimilarCreatureContactCreatureCount, snapshot.CreatureCount))}/{FormatPercent(Share(snapshot.LineageCreatureContactCreatureCount, snapshot.CreatureCount))}  intent {FormatPercent(Share(snapshot.AttackIntentWhileTouchingSimilarCreatureCount, snapshot.CreatureCount))}/{FormatPercent(Share(snapshot.AttackIntentWhileTouchingLineageCreatureCount, snapshot.CreatureCount))}  avg {snapshot.AverageCreatureContactSimilarity:0.00}/{snapshot.AverageCreatureContactLineageSimilarity:0.00}\n" +
            $"Attack raw {snapshot.AverageAttackOutput:0.00}  near touch {FormatPercent(Share(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount))}  dmg {snapshot.TotalAttackDamagePerSecond:0.00}/s\n" +
            $"Grab raw {grabStats.AverageOutput:0.00}  intent {FormatPercent(grabStats.IntentShare)}  can {FormatPercent(grabStats.CanGrabShare)}\n" +
            $"Grab held {grabStats.HoldingCount} grabbed {grabStats.GrabbedCount}  pressure {grabStats.AveragePressure:0.00}/{grabStats.MaxPressure:0.00}  strength {grabStats.AverageStrength:0.00}/{grabStats.MaxStrength:0.00}\n" +
            $"Meal gap {snapshot.AverageSecondsSinceLastMeal:0.0}s  Vision {snapshot.AverageVisionRange:0}/{ToDegrees(snapshot.AverageVisionAngleRadians):0}deg\n" +
            $"Search {snapshot.TotalDistanceTraveledPerSecond:0}u/s  meal dist {snapshot.AverageDistanceSinceLastMeal:0}u  kcal/u {snapshot.CaloriesEatenPerDistance:0.00}\n" +
            $"Zoom {_viewZoom:0.00}x  Follow {(_followSelected ? "on" : "off")}  Map {(_renderMap ? "on" : "off")}\n" +
            $"Food {FormatResourceRenderMode(_resourceRenderMode)} v{_visibleResourceEstimate} d{FormatDrawCount(_drawnResourceCount, _drawnResourceAggregateCount)}\n" +
            $"Plant colors generic/tender/rich/tough\n" +
            $"Creatures {FormatCreatureRenderMode(_creatureRenderMode)} v{_visibleCreatureEstimate} d{FormatDrawCount(_drawnCreatureCount, _drawnCreatureAggregateCount)}\n" +
            $"Biome {FormatBiomeKind(centerBiome)}{centerVoidText} {FormatBiomeMapKind(_scenario.BiomeMapKind)} overlay {FormatMapOverlayMode(_mapOverlayMode)}\n" +
            $"Temperature here {FormatTemperatureIndex(centerTemperature)}  map avg {FormatTemperatureIndex(snapshot.AverageMapTemperature)}  creature avg {FormatTemperatureIndex(snapshot.AverageCreatureTemperature)}\n" +
            $"Thermal opt {FormatTemperatureIndex(snapshot.AverageThermalOptimum)}  tol {FormatTemperatureIndex(snapshot.AverageThermalTolerance)}  mismatch {FormatPercent(snapshot.AverageCreatureThermalMismatch)}  hot/cold {snapshot.HotThermalMismatchCreatureCount}/{snapshot.ColdThermalMismatchCreatureCount}\n" +
            $"Obstacles {FormatObstacleMapKind(_scenario.ObstacleMapKind)} cells {_simulation.State.Obstacles.BlockedCellCount}\n" +
            $"Obstacle sensed {FormatPercent(Share(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount))}  blocked {FormatPercent(Share(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount))}  fwd {snapshot.AverageForwardObstacle:0.00}\n" +
            $"Biome pop D {FormatPercent(Share(snapshot.BarrenCreatureCount, snapshot.CreatureCount))} Sc {FormatPercent(Share(snapshot.SparseCreatureCount, snapshot.CreatureCount))} G {FormatPercent(Share(snapshot.GrasslandCreatureCount, snapshot.CreatureCount))} F {FormatPercent(Share(snapshot.RichCreatureCount, snapshot.CreatureCount))}\n" +
            $"Biome pop Fo {FormatPercent(Share(snapshot.ForestCreatureCount, snapshot.CreatureCount))} W {FormatPercent(Share(snapshot.WetlandCreatureCount, snapshot.CreatureCount))} T {FormatPercent(Share(snapshot.TundraCreatureCount, snapshot.CreatureCount))} H {FormatPercent(Share(snapshot.HighlandCreatureCount, snapshot.CreatureCount))}\n" +
            $"Biome move {snapshot.AverageBiomeMovementCostMultiplier:0.00}x basal {snapshot.AverageBiomeBasalCostMultiplier:0.00}x speed {snapshot.AverageBiomeSpeedMultiplier:0.00}x\n" +
            $"Color {FormatColorMode(_colorMode)}";

        _selectionTitle.Text = BuildSelectionTitle();
        _inspector.Text = BuildInspectorText();
        RefreshSelectionViewButtons();
        _graphLegend.Text =
            $"{FormatGraphTickSpan(state.Stats.Snapshots, GraphMetric.Population)}\n" +
            $"{FormatGraphTickSpan(state.Stats.Snapshots, GraphMetric.Season)}\n" +
            "Keys: Space/P pause  +/- speed  N seed  S scenario\n" +
            $"Move: Arrows/Wheel/Drag  Click select  G follow  T visual  B overlay  C/V/M toggles";

        _miniGraphLabels[0].Text = $"Population {state.Creatures.Count}";
        _miniGraphLabels[0].AddThemeColorOverride("font_color", _graphPopulationColor);
        _miniGraphLabels[1].Text = $"Food {snapshot.TotalResourceCalories:0} kcal";
        _miniGraphLabels[1].AddThemeColorOverride("font_color", _graphResourceColor);
        var deathRate = state.Stats.Snapshots.Count > 1
            ? GetGraphMetricValue(state.Stats.Snapshots[^1], state.Stats.Snapshots.Count - 1, GraphMetric.Deaths)
            : 0f;
        _miniGraphLabels[2].Text = $"Deaths {deathRate:0.00}/s avg";
        _miniGraphLabels[2].AddThemeColorOverride("font_color", _graphDeathColor);
        _miniGraphLabels[3].Text = _scenario.EnableSeasons
            ? $"Season {snapshot.SeasonPhase * 100f:0}%  {snapshot.SeasonFertilityMultiplier:0.00}x"
            : "Season off";
        _miniGraphLabels[3].AddThemeColorOverride("font_color", _graphSeasonColor);
    }

    private static int CountActiveResources(IReadOnlyList<ResourcePatchState> resources)
    {
        var count = 0;
        for (var i = 0; i < resources.Count; i++)
        {
            if (resources[i].Calories > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private static LiveGrabStats CalculateLiveGrabStats(IReadOnlyList<CreatureState> creatures)
    {
        if (creatures.Count == 0)
        {
            return default;
        }

        var intentCount = 0;
        var canGrabCount = 0;
        var holdingCount = 0;
        var grabbedCount = 0;
        var totalOutput = 0f;
        var totalPressure = 0f;
        var totalStrength = 0f;
        var maxPressure = 0f;
        var maxStrength = 0f;

        for (var i = 0; i < creatures.Count; i++)
        {
            var creature = creatures[i];
            totalOutput += creature.Actions.GrabOutput;
            if (creature.Actions.WantsGrab)
            {
                intentCount++;
            }

            if (creature.Senses.CanGrabCreature > 0f)
            {
                canGrabCount++;
            }

            if (creature.HeldCreatureId != default)
            {
                holdingCount++;
                totalStrength += creature.GrabStrength;
                maxStrength = MathF.Max(maxStrength, creature.GrabStrength);
            }

            if (creature.GrabbedByCreatureId != default)
            {
                grabbedCount++;
                totalPressure += creature.GrabPressure;
                maxPressure = MathF.Max(maxPressure, creature.GrabPressure);
            }
        }

        return new LiveGrabStats(
            totalOutput / creatures.Count,
            intentCount / (float)creatures.Count,
            canGrabCount / (float)creatures.Count,
            holdingCount,
            grabbedCount,
            grabbedCount > 0 ? totalPressure / grabbedCount : 0f,
            maxPressure,
            holdingCount > 0 ? totalStrength / holdingCount : 0f,
            maxStrength);
    }

    private string FormatGraphTickSpan(IReadOnlyList<SimulationStatsSnapshot> snapshots, GraphMetric metric)
    {
        if (snapshots.Count < 2)
        {
            return $"Graph: waiting for samples, records every {_scenario.StatsSnapshotIntervalTicks} ticks";
        }

        var sampleCount = GetGraphSampleCount(metric, snapshots.Count);
        var first = snapshots[^sampleCount];
        var last = snapshots[^1];
        var tickSpan = Math.Max(0L, last.Tick - first.Tick);
        return metric == GraphMetric.Season && _scenario.EnableSeasons
            ? $"Season graph: last {tickSpan:N0} ticks ({sampleCount} samples, 1 season target)"
            : $"Graphs: last {tickSpan:N0} ticks ({sampleCount} samples)";
    }

    private int GetGraphSampleCount(GraphMetric metric, int availableSnapshotCount)
    {
        if (availableSnapshotCount < 2)
        {
            return availableSnapshotCount;
        }

        var sampleCount = GraphSampleCount;
        if (metric == GraphMetric.Season && _scenario.EnableSeasons)
        {
            var fixedDeltaSeconds = MathF.Max(0.0001f, _scenario.FixedDeltaSeconds);
            var snapshotIntervalTicks = Math.Max(1, _scenario.StatsSnapshotIntervalTicks);
            var seasonTicks = Math.Max(1.0, _scenario.SeasonLengthSeconds / (double)fixedDeltaSeconds);
            var seasonSnapshots = Math.Ceiling(seasonTicks / snapshotIntervalTicks) + 1.0;
            var seasonSnapshotCount = seasonSnapshots >= int.MaxValue
                ? int.MaxValue
                : (int)seasonSnapshots;
            sampleCount = Math.Max(sampleCount, seasonSnapshotCount);
        }

        var maxAllowed = Math.Min(availableSnapshotCount, MaxGraphSamplesDrawn);
        return Math.Clamp(sampleCount, 2, maxAllowed);
    }

    private bool HasSelectedEntity()
    {
        return _selectedCreatureId != default || _selectedEggId != default;
    }

    private string BuildSelectionTitle()
    {
        if (_selectedCreatureId != default && TryGetSelectedCreature(out var creature))
        {
            var genome = _simulation.State.GetGenome(creature.GenomeId);
            return $"Creature #{creature.Id.Value} ({CreatureRoleName(genome)}) - {_selectedInspectorView}";
        }

        if (_selectedEggId != default && TryGetSelectedEgg(out var egg))
        {
            return $"Egg #{egg.Id.Value}";
        }

        return "Selected";
    }

    private static string CreatureRoleName(CreatureGenome genome)
    {
        return SpriteSlotForCreature(genome) switch
        {
            SpriteAtlasSlot.CreatureScavenger => "Scavenger",
            SpriteAtlasSlot.CreaturePredator => "Predator",
            SpriteAtlasSlot.CreatureOmnivore => "Omnivore",
            SpriteAtlasSlot.CreaturePlantSpecialist => "Plant Specialist",
            SpriteAtlasSlot.CreatureGrazer => "Grazer",
            SpriteAtlasSlot.CreatureArmored => "Armored",
            SpriteAtlasSlot.CreatureFast => "Fast",
            SpriteAtlasSlot.CreatureScout => "Scout",
            SpriteAtlasSlot.CreatureTiny => "Tiny",
            _ => "Generalist"
        };
    }

    private string BuildInspectorText()
    {
        if (_selectedCreatureId == default && _selectedEggId == default)
        {
            return "None";
        }

        if (_selectedCreatureId != default)
        {
            for (var i = 0; i < _simulation.State.Creatures.Count; i++)
            {
                var creature = _simulation.State.Creatures[i];
                if (creature.Id != _selectedCreatureId)
                {
                    continue;
                }

                return BuildCreatureInspectorText(creature);
            }

            ClearSelection();
            return "None";
        }

        if (_selectedEggId != default && TryGetSelectedEgg(out var egg))
        {
            return BuildEggInspectorText(egg);
        }

        ClearSelection();
        return "None";
    }

    private string BuildCreatureInspectorText(CreatureState creature)
    {
        if (_selectedInspectorView == SelectedInspectorView.Summary)
        {
            return BuildCreatureSummaryInspectorText(creature);
        }

        if (_selectedInspectorView == SelectedInspectorView.State)
        {
            return BuildCreatureStateInspectorText(creature);
        }

        if (_selectedInspectorView == SelectedInspectorView.Body)
        {
            return BuildCreatureBodyInspectorText(creature);
        }

        if (_selectedInspectorView == SelectedInspectorView.Senses)
        {
            return BuildCreatureSensesInspectorText(creature);
        }

        if (_selectedInspectorView == SelectedInspectorView.Brain)
        {
            return BuildCreatureBrainInspectorText(creature);
        }

        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var senses = creature.Senses;
        var maturityProgress = CreatureGrowth.MaturityProgress(creature, genome);
        var growthFactor = CreatureGrowth.GrowthFactor(creature, genome);
        var gutCapacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
        var gutTotal = creature.GutPlantCalories + creature.GutMeatCalories;
        var gutFillRatio = gutCapacity > 0f
            ? Math.Clamp(gutTotal / gutCapacity, 0f, 1f)
            : 0f;
        var energyCapacity = CreatureGrowth.EffectiveEnergyCapacityCalories(creature, genome);
        var energyFullness = CreatureGrowth.EnergyFullnessRatio(creature, genome);
        var fatCapacity = CreatureGrowth.EffectiveFatStorageCapacityCalories(creature, genome);
        var fatRatio = CreatureGrowth.FatStorageRatio(creature, genome);
        var fatBurden = CreatureGrowth.FatMassBurdenRatio(creature, genome);
        var fatSpeedMultiplier = CreatureGrowth.FatSpeedMultiplier(creature, genome);
        _simulation.State.TryGetLineageRecord(creature.Id, out var lineage);
        var parentText = lineage.IsFounder ? "Founder" : $"Parent #{lineage.ParentId.Value}";
        var maturityText = CreatureGrowth.IsMature(creature, genome)
            ? "adult"
            : $"juvenile {maturityProgress:P0}";
        var biome = _simulation.State.Biomes.GetKindAt(creature.Position);
        var movementCostMultiplier = _scenario.CreateBiomeMovementCostProfile().For(biome);
        var basalCostMultiplier = _scenario.CreateBiomeBasalCostProfile().For(biome);
        var speedMultiplier = _scenario.CreateBiomeSpeedProfile().For(biome);
        var visionMultiplier = _scenario.CreateBiomeVisionRangeProfile().For(biome);
        var seasonalFertility = SeasonalFertility.CalculateBiomeMultiplierAt(
            _scenario.EnableSeasons,
            _simulation.State.ElapsedSeconds,
            _scenario.SeasonLengthSeconds,
            _scenario.SeasonFertilityAmplitude,
            _scenario.SeasonPhaseOffsetSeconds,
            _scenario.SeasonPhaseMode,
            _simulation.State.Bounds,
            creature.Position,
            biome,
            _scenario.CreateBiomeSeasonalAmplitudeProfile());
        var brainText = FormatBrainText(creature.BrainId);

        return
            $"Selected #{creature.Id.Value}\n" +
            $"{parentText}\n" +
            $"Generation {creature.Generation}\n" +
            $"Genome {creature.GenomeId}  Brain {brainText}\n" +
            $"Energy {creature.Energy:0.0}/{energyCapacity:0.0} ({energyFullness:P0})\n" +
            $"Fat {creature.FatCalories:0.0}/{fatCapacity:0.0} ({fatRatio:P0})  burden {fatBurden:P0}\n" +
            $"Health {creature.Health:0.00} ({senses.HealthRatio:P0})\n" +
            $"Age {creature.AgeSeconds:0.0}s\n" +
            $"Growth {maturityText} ({growthFactor:P0})\n" +
            $"Birth inv {creature.BirthInvestmentRatio:0.00}x\n" +
            $"Biome {FormatBiomeKind(biome)}  move {movementCostMultiplier:0.00}x basal {basalCostMultiplier:0.00}x speed {speedMultiplier:0.00}x vision {visionMultiplier:0.00}x season {seasonalFertility:0.00}x\n" +
            $"Max speed {CreatureGrowth.EffectiveMaxSpeed(creature, genome):0.0}/{genome.MaxSpeed:0.0}\n" +
            $"Actual speed {creature.Velocity.Length:0.0}\n" +
            $"Desired speed {creature.DesiredVelocity.Length:0.0}\n" +
            $"Speed cost {MovementSystem.CalculateSpeedCostMultiplier(creature.Velocity.Length, _scenario.MovementSpeedCostExponent):0.00}x  fat speed {fatSpeedMultiplier:0.00}x\n" +
            $"Turn {CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome):0.0}/{genome.MaxTurnRadiansPerSecond:0.0}\n" +
            $"Vision range {CreatureGrowth.EffectiveSenseRadius(creature, genome):0.0}/{genome.SenseRadius:0.0}\n" +
            $"Vision angle {ToDegrees(CreatureGrowth.EffectiveVisionAngleRadians(creature, genome)):0}deg/{ToDegrees(genome.VisionAngleRadians):0}deg\n" +
            $"Sector hits {BuildVisionSectorHitSummary(senses.VisionSectors)}\n" +
            $"Body {CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}/{genome.BodyRadius:0.0}\n" +
            $"Terrain drag now {senses.CurrentTerrainDrag:0.00}  ahead {senses.ForwardTerrainDrag:0.00}  L {senses.LeftTerrainDrag:0.00}  R {senses.RightTerrainDrag:0.00}\n" +
            $"Habitat now {senses.CurrentHabitatQuality:0.00}  ahead {senses.ForwardHabitatQuality:0.00}  L {senses.LeftHabitatQuality:0.00}  R {senses.RightHabitatQuality:0.00}\n" +
            $"Obstacle fwd {senses.ForwardObstacle:0.00}  L {senses.LeftObstacle:0.00}  R {senses.RightObstacle:0.00}  blocked {senses.MovementBlocked:0.00}\n" +
            $"Eat rate {CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome):0.0}/{genome.EatCaloriesPerSecond:0.0}\n" +
            $"Diet meat bias {genome.DietaryAdaptation:0.00}\n" +
            $"Carrion bias {genome.CarrionAdaptation:0.00}\n" +
            $"Digest plant {CreatureDigestion.PlantEfficiency(genome):P0}  meat {CreatureDigestion.MeatEfficiency(genome):P0}\n" +
            $"Plant adapt T {genome.TenderPlantAdaptation:0.00}  R {genome.RichPlantAdaptation:0.00}  Tough {genome.ToughPlantAdaptation:0.00}\n" +
            $"Plant yield T {CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Tender):P0}  R {CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Rich):P0}  Tough {CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Tough):P0}\n" +
            $"Meat digest fresh {CreatureDigestion.FreshMeatEnergyEfficiency(genome):P0}  stale {CreatureDigestion.StaleMeatEnergyEfficiency(genome):P0}\n" +
            $"Digest rate {CreatureGrowth.EffectiveDigestionCaloriesPerSecond(creature, genome):0.0}/{genome.DigestionCaloriesPerSecond:0.0}\n" +
            $"Gut cap {gutCapacity:0.0}/{genome.GutCapacityCalories:0.0}\n" +
            $"Gut {gutTotal:0.0}/{gutCapacity:0.0} ({gutFillRatio:P0})\n" +
            $"Gut plant {creature.GutPlantCalories:0.0}  meat {creature.GutMeatCalories:0.0}\n" +
            $"Bite str {CreatureGrowth.EffectiveBiteStrength(creature, genome):0.00}/{genome.BiteStrength:0.00}\n" +
            $"Damage resist {CreatureGrowth.EffectiveDamageResistance(creature, genome):0.00}/{genome.DamageResistance:0.00}\n" +
            $"Egg reserve {creature.ReproductiveEnergy:0.0}/{genome.OffspringEnergyInvestment:0.0}\n" +
            $"Energy surplus {senses.EnergySurplusRatio:0.00}  full {senses.EnergyFullnessRatio:0.00}  gut full {senses.GutFullnessRatio:0.00}  food success {senses.RecentFoodSuccess:0.00}\n" +
            $"Food energy yield {senses.RecentFoodEnergyYield:0.00}  plant energy {senses.RecentPlantEnergyYield:0.00}\n" +
            $"Plant yield raw {senses.RecentPlantRawYield:0.00}\n" +
            $"Plant payoff trace T {senses.TenderPlantPayoffTrace:0.00}  R {senses.RichPlantPayoffTrace:0.00}  Tough {senses.ToughPlantPayoffTrace:0.00}\n" +
            $"Memory {senses.MemoryStrength:0.00}  fwd {senses.MemoryDirectionForward:0.00}  right {senses.MemoryDirectionRight:0.00}\n" +
            $"Memory write fwd {creature.Actions.MemoryForward:0.00}  right {creature.Actions.MemoryRight:0.00}\n" +
            $"Egg build {genome.EggProductionEnergyPerSecond:0.0}/s\n" +
            $"Lay ready {(senses.ReproductionReadiness > 0.5f ? "yes" : "no")}\n" +
            $"Egg incubation {genome.EggIncubationSeconds:0.0}s\n" +
            $"Food contact {(creature.IsTouchingFood ? "yes" : "no")}\n" +
            $"Plant taste energy {senses.PlantFoodContactEnergyQuality:0.00}  bite {senses.PlantFoodContactBiteEase:0.00}\n" +
            BuildFoodContactText(creature, genome) +
            $"Last meal {BuildLastMealSourceText(creature)}\n" +
            $"Swallowed this tick {creature.LastCaloriesEaten:0.00} raw ({FormatCaloriesPerSecond(creature.LastCaloriesEaten)}/s)\n" +
            $"Source P {creature.LastPlantCaloriesEaten:0.00}  C {creature.LastCarcassCaloriesEaten:0.00}  Egg {creature.LastEggCaloriesEaten:0.00}  FK {creature.LastLivePreyCaloriesEaten:0.00}\n" +
            $"Digested this tick {creature.LastCaloriesDigested:0.00} energy ({FormatPerSecond(creature.LastCaloriesDigested)}/s)\n" +
            $"Energy P {creature.LastPlantDigestedEnergy:0.00}  M {creature.LastMeatDigestedEnergy:0.00}\n" +
            $"Energy overflow {creature.LastEnergyOverflowCalories:0.00}  fat stored {creature.LastFatStoredCalories:0.00}  released {creature.LastFatReleasedCalories:0.00}\n" +
            $"Rotten dmg {creature.LastRottenMeatDamage:0.000} health ({FormatPerSecond(creature.LastRottenMeatDamage)}/s)\n" +
            $"Creature contact {(creature.IsTouchingCreature ? $"#{creature.CreatureContactId.Value} edge {creature.CreatureContactEdgeDistance:0.0}" : "no")}\n" +
            $"Attack dmg {creature.LastAttackDamageDealt:0.000}\n" +
            $"Since meal {creature.SecondsSinceLastMeal:0.0}s  {creature.DistanceSinceLastMeal:0.0}u\n" +
            $"Moved last tick {creature.LastDistanceTraveled:0.00}u\n" +
            $"Repro at {genome.ReproductionEnergyThreshold:0.0}\n" +
            $"Pace {genome.MetabolicPace:0.00}  mature at {CreatureMetabolism.EffectiveMaturityAgeSeconds(genome):0.0}s ({genome.MaturityAgeSeconds:0.0}s base)\n" +
            $"World mutation {_scenario.MutationStrength:0.000}\n" +
            $"World trait mut {_scenario.TraitMutationRate:P0}\n" +
            $"World brain mut {_scenario.BrainMutationRate:P0}\n" +
            $"Cooldown {creature.ReproductionCooldownSeconds:0.0}s\n\n" +
            $"Food {(senses.FoodDetected ? "yes" : "no")}\n" +
            $"Visible density {senses.VisibleFoodDensity:0.00}\n" +
            $"Proximity {senses.FoodProximity:0.00}\n" +
            $"Forward {senses.FoodDirectionForward:0.00}\n" +
            $"Right {senses.FoodDirectionRight:0.00}\n" +
            $"Plants {(senses.PlantDetected ? "yes" : "no")}  density {senses.VisiblePlantDensity:0.00}\n" +
            $"Plant prox {senses.PlantProximity:0.00}  fwd {senses.PlantDirectionForward:0.00}  right {senses.PlantDirectionRight:0.00}\n" +
            $"Plant quality energy {senses.VisiblePlantEnergyQuality:0.00}  bite {senses.VisiblePlantBiteEase:0.00}\n" +
            $"Meat {(senses.MeatDetected ? "yes" : "no")}  density {senses.VisibleMeatDensity:0.00}\n" +
            $"Meat prox {senses.MeatProximity:0.00}  fwd {senses.MeatDirectionForward:0.00}  right {senses.MeatDirectionRight:0.00}\n" +
            $"Meat fresh {senses.VisibleMeatFreshness:P0}\n" +
            $"Meat scent {(senses.MeatScentDetected ? "yes" : "no")}  density {senses.MeatScentDensity:0.00}\n" +
            $"Scent fwd {senses.MeatScentDirectionForward:0.00}  right {senses.MeatScentDirectionRight:0.00}\n" +
            $"Rot scent {(senses.RottenMeatScentDetected ? "yes" : "no")}  density {senses.RottenMeatScentDensity:0.00}\n" +
            $"Rot fwd {senses.RottenMeatScentDirectionForward:0.00}  right {senses.RottenMeatScentDirectionRight:0.00}\n" +
            $"Creature {(senses.CreatureDetected ? "yes" : "no")}  density {senses.VisibleCreatureDensity:0.00}\n" +
            $"Creature prox {senses.CreatureProximity:0.00}  fwd {senses.CreatureDirectionForward:0.00}  right {senses.CreatureDirectionRight:0.00}\n" +
            $"Creature size {senses.CreatureRelativeBodySize:0.00}  speed {senses.CreatureRelativeSpeed:0.00}  approach {senses.CreatureApproachRate:0.00}\n" +
            $"Creature facing {senses.CreatureFacingAlignment:0.00}\n\n" +
            $"Move {creature.Actions.MoveForward:0.00}\n" +
            $"Turn {creature.Actions.Turn:0.00}\n" +
            $"Eat output {creature.Actions.EatOutput:0.00}\n" +
            $"Repro output {creature.Actions.ReproduceOutput:0.00}\n" +
            $"Attack output {creature.Actions.AttackOutput:0.00}\n" +
            $"Eat intent {creature.Actions.WantsEat}\n" +
            $"Attack intent {creature.Actions.WantsAttack}\n" +
            $"Reproduce {creature.Actions.WantsReproduce}";
    }

    private string BuildCreatureSummaryInspectorText(CreatureState creature)
    {
        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var senses = creature.Senses;
        var maturityProgress = CreatureGrowth.MaturityProgress(creature, genome);
        var growthFactor = CreatureGrowth.GrowthFactor(creature, genome);
        var maturityText = CreatureGrowth.IsMature(creature, genome)
            ? "adult"
            : $"juvenile {maturityProgress:P0}";
        var effectiveMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
        var speedRatio = effectiveMaxSpeed > 0f
            ? creature.Velocity.Length / effectiveMaxSpeed
            : 0f;
        var energyRatio = genome.ReproductionEnergyThreshold > 0f
            ? creature.Energy / genome.ReproductionEnergyThreshold
            : 1f;
        var energyCapacity = CreatureGrowth.EffectiveEnergyCapacityCalories(creature, genome);
        var energyFullness = CreatureGrowth.EnergyFullnessRatio(creature, genome);
        var healthRatio = senses.HealthRatio > 0f
            ? senses.HealthRatio
            : creature.Health;
        var fatCapacity = CreatureGrowth.EffectiveFatStorageCapacityCalories(creature, genome);
        var fatRatio = CreatureGrowth.FatStorageRatio(creature, genome);
        var memoryOutput = MathF.Max(
            MathF.Abs(creature.Actions.MemoryForward),
            MathF.Abs(creature.Actions.MemoryRight));
        var currentTemperature = _simulation.State.Temperature.GetTemperatureAt(creature.Position);
        var thermalOptimum = CreatureThermal.NormalizeOptimum(genome.ThermalOptimum);
        var thermalTolerance = CreatureThermal.NormalizeTolerance(genome.ThermalTolerance);
        var thermalMismatch = CreatureThermal.ThermalMismatch(currentTemperature, genome);
        var thermalBasalCostMultiplier = 1f + thermalMismatch * _scenario.ThermalMismatchBasalCostMultiplier;

        return
            $"{ColorText("[b]Vitals[/b]", "#f3f0d0")}\n" +
            $"{SummaryMetric("Energy", $"{creature.Energy:0.0}", energyRatio, "vs repro")}\n" +
            $"{SummaryMetric("Fullness", $"{creature.Energy:0.0}/{energyCapacity:0.0}", energyFullness, "cap")}\n" +
            $"{SummaryMetric("Health", $"{creature.Health:0.00}", healthRatio, "of max")}\n" +
            $"{SummaryMetric("Fat", $"{creature.FatCalories:0.0}/{fatCapacity:0.0}", fatRatio, "stored")}\n" +
            $"{ColorText("Age", "#b8c7bd")} {creature.AgeSeconds:0.0}s    " +
            $"{ColorText("Growth", "#b8c7bd")} {maturityText} ({growthFactor:P0})\n" +
            $"{ColorText("Egg reserve", "#b8c7bd")} {creature.ReproductiveEnergy:0.0}/{genome.OffspringEnergyInvestment:0.0} ({senses.EggReserveRatio:P0})    " +
            $"{ColorText("Speed", "#b8c7bd")} {creature.Velocity.Length:0.0} ({speedRatio:P0})\n\n" +
            $"{ColorText("[b]Thermal[/b]", "#f3f0d0")}\n" +
            $"{ColorText("Now", "#b8c7bd")} {FormatTemperatureIndex(currentTemperature)}    " +
            $"{ColorText("Opt", "#b8c7bd")} {FormatTemperatureIndex(thermalOptimum)}    " +
            $"{ColorText("Tol", "#b8c7bd")} {FormatTemperatureIndex(thermalTolerance)}    " +
            $"{ColorText("Mismatch", "#b8c7bd")} {FormatPercent(thermalMismatch)}    " +
            $"{ColorText("Basal", "#b8c7bd")} {thermalBasalCostMultiplier:0.00}x\n\n" +
            $"{ColorText("[b]World Outputs[/b]", "#f3f0d0")}\n" +
            $"{SummaryAction("Move", creature.Actions.MoveForward, MathF.Abs(creature.Actions.MoveForward) >= SummaryActionActiveThreshold, "#72cfff")}  " +
            $"{SummaryAction("Turn", creature.Actions.Turn, MathF.Abs(creature.Actions.Turn) >= SummaryActionActiveThreshold, "#72cfff")}\n" +
            $"{SummaryAction("Eat", creature.Actions.EatOutput, creature.Actions.WantsEat || creature.Actions.EatOutput >= SummaryActionActiveThreshold, "#7ee37a")}  " +
            $"{SummaryAction("Repro", creature.Actions.ReproduceOutput, creature.Actions.WantsReproduce || creature.Actions.ReproduceOutput >= SummaryActionActiveThreshold, "#d7e86c")}\n" +
            $"{SummaryAction("Attack", creature.Actions.AttackOutput, creature.Actions.WantsAttack || creature.Actions.AttackOutput >= SummaryActionActiveThreshold, "#ff6b4a")}  " +
            $"{SummaryAction("Grab", creature.Actions.GrabOutput, creature.Actions.WantsGrab || creature.Actions.GrabOutput >= SummaryActionActiveThreshold, "#ff9b45")}\n" +
            $"{SummaryAction("Sound", creature.Actions.SoundAmplitude, creature.Actions.SoundAmplitude >= SummaryActionActiveThreshold, "#b88cff")}  " +
            $"{SummaryAction("Memory", memoryOutput, memoryOutput >= SummaryActionActiveThreshold, "#8fd7ff")}\n\n" +
            $"{ColorText("[b]Contacts & Recent Effects[/b]", "#f3f0d0")}\n" +
            $"{SummaryBoolean("Food", creature.IsTouchingFood, "#7ee37a")}  " +
            $"{SummaryBoolean("Creature", creature.IsTouchingCreature, "#ff9b45")}  " +
            $"{SummaryBoolean("Holding", creature.HeldCreatureId != default, "#ff9b45")}  " +
            $"{SummaryBoolean("Grabbed", creature.GrabbedByCreatureId != default, "#ff6b4a")}\n" +
            $"{ColorText("Swallowed", "#b8c7bd")} {creature.LastCaloriesEaten:0.00} raw    " +
            $"{ColorText("Digested", "#b8c7bd")} {creature.LastCaloriesDigested:0.00} energy\n" +
            $"{ColorText("Attack dealt", "#b8c7bd")} {creature.LastAttackDamageDealt:0.000}    " +
            $"{ColorText("Damage taken", "#b8c7bd")} {creature.LastAttackDamageTaken:0.000}\n" +
            $"{ColorText("Energy overflow", "#b8c7bd")} {creature.LastEnergyOverflowCalories:0.00}    " +
            $"{ColorText("Fat stored", "#b8c7bd")} {creature.LastFatStoredCalories:0.00}    " +
            $"{ColorText("Fat released", "#b8c7bd")} {creature.LastFatReleasedCalories:0.00}\n" +
            $"{ColorText("Sound tone", "#b8c7bd")} {creature.Actions.SoundTone:0.00}    " +
            $"{ColorText("Grab pressure", "#b8c7bd")} {creature.GrabPressure:0.00}\n";
    }

    private static string SummaryMetric(string label, string value, float ratio, string suffix)
    {
        var clampedRatio = Math.Clamp(ratio, 0f, 9.99f);
        var color = RatioStatusColor(clampedRatio);
        return $"{ColorText(label, "#b8c7bd")} {ColorText($"{value} ({clampedRatio:P0} {suffix})", color)}";
    }

    private static string SummaryAction(string label, float value, bool active, string activeColor)
    {
        var color = active ? activeColor : "#68716b";
        var state = active ? "ON" : "off";
        return ColorText($"{label} {value:0.00} {state}", color);
    }

    private static string SummaryBoolean(string label, bool active, string activeColor)
    {
        return ColorText($"{label} {(active ? "yes" : "no")}", active ? activeColor : "#68716b");
    }

    private static string RatioStatusColor(float ratio)
    {
        if (ratio <= 0.20f)
        {
            return "#ff6b4a";
        }

        if (ratio <= 0.50f)
        {
            return "#ffd45e";
        }

        return "#a7f28b";
    }

    private static string ColorText(string text, string color)
    {
        return $"[color={color}]{text}[/color]";
    }

    private string BuildCreatureStateInspectorText(CreatureState creature)
    {
        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var senses = creature.Senses;
        var maturityProgress = CreatureGrowth.MaturityProgress(creature, genome);
        var growthFactor = CreatureGrowth.GrowthFactor(creature, genome);
        var maturityText = CreatureGrowth.IsMature(creature, genome)
            ? "adult"
            : $"juvenile {maturityProgress:P0}";
        _simulation.State.TryGetLineageRecord(creature.Id, out var lineage);
        var parentText = lineage.IsFounder ? "founder" : $"parent #{lineage.ParentId.Value}";
        var biome = _simulation.State.Biomes.GetKindAt(creature.Position);
        var movementCostMultiplier = _scenario.CreateBiomeMovementCostProfile().For(biome);
        var basalCostMultiplier = _scenario.CreateBiomeBasalCostProfile().For(biome);
        var speedMultiplier = _scenario.CreateBiomeSpeedProfile().For(biome);
        var visionMultiplier = _scenario.CreateBiomeVisionRangeProfile().For(biome);
        var currentTemperature = _simulation.State.Temperature.GetTemperatureAt(creature.Position);
        var thermalMismatch = CreatureThermal.ThermalMismatch(currentTemperature, genome);
        var thermalBasalCostMultiplier = 1f + thermalMismatch * _scenario.ThermalMismatchBasalCostMultiplier;
        var fatCapacity = CreatureGrowth.EffectiveFatStorageCapacityCalories(creature, genome);
        var fatRatio = CreatureGrowth.FatStorageRatio(creature, genome);
        var fatBurden = CreatureGrowth.FatMassBurdenRatio(creature, genome);
        var fatSpeedMultiplier = CreatureGrowth.FatSpeedMultiplier(creature, genome);
        var seasonalFertility = SeasonalFertility.CalculateBiomeMultiplierAt(
            _scenario.EnableSeasons,
            _simulation.State.ElapsedSeconds,
            _scenario.SeasonLengthSeconds,
            _scenario.SeasonFertilityAmplitude,
            _scenario.SeasonPhaseOffsetSeconds,
            _scenario.SeasonPhaseMode,
            _simulation.State.Bounds,
            creature.Position,
            biome,
            _scenario.CreateBiomeSeasonalAmplitudeProfile());

        return
            $"Identity\n" +
            $"Lineage {parentText}   Gen {creature.Generation}\n" +
            $"Genome {creature.GenomeId}   Brain {FormatBrainText(creature.BrainId)}\n\n" +
            $"Vitals\n" +
            $"Energy {creature.Energy:0.0}   Health {creature.Health:0.00} ({senses.HealthRatio:P0})\n" +
            $"Fat {creature.FatCalories:0.0}/{fatCapacity:0.0} ({fatRatio:P0})   burden {fatBurden:P0}\n" +
            $"Age {creature.AgeSeconds:0.0}s   Growth {maturityText} ({growthFactor:P0})\n" +
            $"Birth investment {creature.BirthInvestmentRatio:0.00}x\n\n" +
            $"Place\n" +
            $"Biome {FormatBiomeKind(biome)}   season {seasonalFertility:0.00}x\n" +
            $"Temperature {FormatTemperatureIndex(currentTemperature)}   thermal mismatch {FormatPercent(thermalMismatch)}   thermal basal {thermalBasalCostMultiplier:0.00}x\n" +
            $"Move {movementCostMultiplier:0.00}x   basal {basalCostMultiplier:0.00}x   speed {speedMultiplier:0.00}x   vision {visionMultiplier:0.00}x\n" +
            $"Position {creature.Position.X:0}, {creature.Position.Y:0}\n\n" +
            $"Movement\n" +
            $"Actual speed {creature.Velocity.Length:0.0}   desired {creature.DesiredVelocity.Length:0.0}\n" +
            $"Max speed {CreatureGrowth.EffectiveMaxSpeed(creature, genome):0.0}/{genome.MaxSpeed:0.0}\n" +
            $"Speed cost {MovementSystem.CalculateSpeedCostMultiplier(creature.Velocity.Length, _scenario.MovementSpeedCostExponent):0.00}x   fat speed {fatSpeedMultiplier:0.00}x   grab move {CreatureGrabSystem.MovementMultiplierForGrabPressure(creature.GrabPressure):0.00}x\n\n" +
            $"Creature Interaction\n" +
            $"Contact {(creature.IsTouchingCreature ? $"#{creature.CreatureContactId.Value} edge {creature.CreatureContactEdgeDistance:0.0}" : "no")}\n" +
            $"Grab output {creature.Actions.GrabOutput:0.00}   intent {(creature.Actions.WantsGrab ? "yes" : "no")}\n" +
            $"Holding {FormatCreatureReference(creature.HeldCreatureId)}   strength {creature.GrabStrength:0.00}\n" +
            $"Grabbed by {FormatCreatureReference(creature.GrabbedByCreatureId)}   pressure {creature.GrabPressure:0.00}\n" +
            $"Sound amp {creature.Actions.SoundAmplitude:0.00}   tone {creature.Actions.SoundTone:0.00}\n\n" +
            $"Food\n" +
            $"Last meal {BuildLastMealSourceText(creature)}\n" +
            $"Since meal {creature.SecondsSinceLastMeal:0.0}s   distance {creature.DistanceSinceLastMeal:0.0}u\n" +
            $"Touching food {(creature.IsTouchingFood ? "yes" : "no")}\n" +
            BuildFoodContactText(creature, genome) +
            $"Swallowed {creature.LastCaloriesEaten:0.00} raw ({FormatCaloriesPerSecond(creature.LastCaloriesEaten)}/s)\n" +
            $"Digested {creature.LastCaloriesDigested:0.00} energy ({FormatPerSecond(creature.LastCaloriesDigested)}/s)\n\n" +
            $"Energy overflow {creature.LastEnergyOverflowCalories:0.00}   fat stored {creature.LastFatStoredCalories:0.00}   released {creature.LastFatReleasedCalories:0.00}\n\n" +
            $"Reproduction\n" +
            $"Egg reserve {creature.ReproductiveEnergy:0.0}/{genome.OffspringEnergyInvestment:0.0}\n" +
            $"Ready {(senses.ReproductionReadiness > 0.5f ? "yes" : "no")}   cooldown {creature.ReproductionCooldownSeconds:0.0}s\n";
    }

    private string BuildCreatureBodyInspectorText(CreatureState creature)
    {
        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var gutCapacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
        var gutTotal = creature.GutPlantCalories + creature.GutMeatCalories;
        var gutFillRatio = gutCapacity > 0f
            ? Math.Clamp(gutTotal / gutCapacity, 0f, 1f)
            : 0f;
        var energyCapacity = CreatureGrowth.EffectiveEnergyCapacityCalories(creature, genome);
        var energyFullness = CreatureGrowth.EnergyFullnessRatio(creature, genome);
        var fatCapacity = CreatureGrowth.EffectiveFatStorageCapacityCalories(creature, genome);
        var fatRatio = CreatureGrowth.FatStorageRatio(creature, genome);
        var fatBurden = CreatureGrowth.FatMassBurdenRatio(creature, genome);
        var fatSpeedMultiplier = CreatureGrowth.FatSpeedMultiplier(creature, genome);
        var currentTemperature = _simulation.State.Temperature.GetTemperatureAt(creature.Position);
        var thermalOptimum = CreatureThermal.NormalizeOptimum(genome.ThermalOptimum);
        var thermalTolerance = CreatureThermal.NormalizeTolerance(genome.ThermalTolerance);
        var thermalMismatch = CreatureThermal.ThermalMismatch(currentTemperature, genome);
        var thermalBasalCostMultiplier = 1f + thermalMismatch * _scenario.ThermalMismatchBasalCostMultiplier;

        return
            $"Body\n" +
            $"Radius {CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}/{genome.BodyRadius:0.0}\n" +
            $"Max speed {CreatureGrowth.EffectiveMaxSpeed(creature, genome):0.0}/{genome.MaxSpeed:0.0}\n" +
            $"Turn {CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome):0.0}/{genome.MaxTurnRadiansPerSecond:0.0}\n" +
            $"Vision range {CreatureGrowth.EffectiveSenseRadius(creature, genome):0.0}/{genome.SenseRadius:0.0}\n" +
            $"Vision angle {ToDegrees(CreatureGrowth.EffectiveVisionAngleRadians(creature, genome)):0}deg/{ToDegrees(genome.VisionAngleRadians):0}deg\n\n" +
            $"Thermal\n" +
            $"Optimum {FormatTemperatureIndex(thermalOptimum)}   tolerance {FormatTemperatureIndex(thermalTolerance)}\n" +
            $"Current {FormatTemperatureIndex(currentTemperature)}   mismatch {FormatPercent(thermalMismatch)}   basal {thermalBasalCostMultiplier:0.00}x\n\n" +
            $"Diet & Digestion\n" +
            $"Working energy {creature.Energy:0.0}/{energyCapacity:0.0} ({energyFullness:P0})   overflow last {creature.LastEnergyOverflowCalories:0.00}\n" +
            $"Diet meat bias {genome.DietaryAdaptation:0.00}   carrion {genome.CarrionAdaptation:0.00}\n" +
            $"Plant efficiency {CreatureDigestion.PlantEfficiency(genome):P0}   meat {CreatureDigestion.MeatEfficiency(genome):P0}\n" +
            $"Fresh meat {CreatureDigestion.FreshMeatEnergyEfficiency(genome):P0}   stale {CreatureDigestion.StaleMeatEnergyEfficiency(genome):P0}\n" +
            $"Eat rate {CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome):0.0}/{genome.EatCaloriesPerSecond:0.0}\n" +
            $"Digest rate {CreatureGrowth.EffectiveDigestionCaloriesPerSecond(creature, genome):0.0}/{genome.DigestionCaloriesPerSecond:0.0}\n" +
            $"Gut {gutTotal:0.0}/{gutCapacity:0.0} ({gutFillRatio:P0})\n" +
            $"Gut plant {creature.GutPlantCalories:0.0}   meat {creature.GutMeatCalories:0.0}\n\n" +
            $"Fat Storage\n" +
            $"Stored {creature.FatCalories:0.0}/{fatCapacity:0.0} ({fatRatio:P0})   gene cap {genome.FatStorageCapacityCalories:0.0}\n" +
            $"Efficiency {genome.FatStorageEfficiency:P0}   mass burden {fatBurden:P0}   speed retained {fatSpeedMultiplier:P0}\n" +
            $"Last stored {creature.LastFatStoredCalories:0.00}   released {creature.LastFatReleasedCalories:0.00}\n\n" +
            $"Plant Specialization\n" +
            $"Adapt T {genome.TenderPlantAdaptation:0.00}   R {genome.RichPlantAdaptation:0.00}   Tough {genome.ToughPlantAdaptation:0.00}\n" +
            $"Yield T {CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Tender):P0}   R {CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Rich):P0}   Tough {CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Tough):P0}\n\n" +
            $"Combat\n" +
            $"Bite strength {CreatureGrowth.EffectiveBiteStrength(creature, genome):0.00}/{genome.BiteStrength:0.00}\n" +
            $"Damage resistance {CreatureGrowth.EffectiveDamageResistance(creature, genome):0.00}/{genome.DamageResistance:0.00}\n" +
            $"Creature contact {(creature.IsTouchingCreature ? $"#{creature.CreatureContactId.Value} edge {creature.CreatureContactEdgeDistance:0.0}" : "no")}\n" +
            $"Holding {FormatCreatureReference(creature.HeldCreatureId)}   strength {creature.GrabStrength:0.00}\n" +
            $"Grabbed by {FormatCreatureReference(creature.GrabbedByCreatureId)}   pressure {creature.GrabPressure:0.00}\n" +
            $"Attack damage last tick {creature.LastAttackDamageDealt:0.000}\n\n" +
            $"Development & Mutation\n" +
            $"Pace {genome.MetabolicPace:0.00}   mature at {CreatureMetabolism.EffectiveMaturityAgeSeconds(genome):0.0}s ({genome.MaturityAgeSeconds:0.0}s base)\n" +
            $"Egg build {genome.EggProductionEnergyPerSecond * CreatureMetabolism.EggProductionRateMultiplier(genome):0.0}/s ({genome.EggProductionEnergyPerSecond:0.0}/s base)   incubation {genome.EggIncubationSeconds:0.0}s\n" +
            $"Cooldown recovery {CreatureMetabolism.CooldownRecoveryMultiplier(genome):0.00}x   repro threshold {genome.ReproductionEnergyThreshold:0.0}\n" +
            $"World mutation strength {_scenario.MutationStrength:0.000}\n" +
            $"World trait mutation {_scenario.TraitMutationRate:P0}   brain mutation {_scenario.BrainMutationRate:P0}\n";
    }

    private string BuildCreatureSensesInspectorText(CreatureState creature)
    {
        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var senses = creature.Senses;
        var senseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
        var biome = _simulation.State.Biomes.GetKindAt(creature.Position);
        var visionRadius = senseRadius * _scenario.CreateBiomeVisionRangeProfile().For(biome);
        var creatureScentRadius = senseRadius * CreatureSensingSystem.CreatureSimilarityScentRangeMultiplier;
        var meatScentRadius = senseRadius * _scenario.MeatScentRangeMultiplier;
        var soundRadius = senseRadius * _scenario.SoundRangeMultiplier;

        return
            $"Vision\n" +
            $"Ranges vision {visionRadius:0}   sound {soundRadius:0}\n" +
            $"Scent ranges creature {creatureScentRadius:0}   meat/rot {meatScentRadius:0}\n" +
            $"Sector hits {BuildVisionSectorHitSummary(senses.VisionSectors)}\n" +
            $"Food {(senses.FoodDetected ? "yes" : "no")}   density {senses.VisibleFoodDensity:0.00}\n" +
            $"Food prox {senses.FoodProximity:0.00}   fwd {senses.FoodDirectionForward:0.00}   right {senses.FoodDirectionRight:0.00}\n" +
            $"Plants {(senses.PlantDetected ? "yes" : "no")}   density {senses.VisiblePlantDensity:0.00}\n" +
            $"Plant prox {senses.PlantProximity:0.00}   fwd {senses.PlantDirectionForward:0.00}   right {senses.PlantDirectionRight:0.00}\n" +
            $"Plant quality {senses.VisiblePlantEnergyQuality:0.00}   bite ease {senses.VisiblePlantBiteEase:0.00}\n" +
            $"Meat {(senses.MeatDetected ? "yes" : "no")}   density {senses.VisibleMeatDensity:0.00}\n" +
            $"Meat prox {senses.MeatProximity:0.00}   fwd {senses.MeatDirectionForward:0.00}   right {senses.MeatDirectionRight:0.00}\n" +
            $"Visible meat fresh {senses.VisibleMeatFreshness:P0}\n\n" +
            $"Scent\n" +
            $"Meat scent {(senses.MeatScentDetected ? "yes" : "no")}   density {senses.MeatScentDensity:0.00}\n" +
            $"Meat scent fwd {senses.MeatScentDirectionForward:0.00}   right {senses.MeatScentDirectionRight:0.00}\n" +
            $"Rot scent {(senses.RottenMeatScentDetected ? "yes" : "no")}   density {senses.RottenMeatScentDensity:0.00}\n" +
            $"Rot fwd {senses.RottenMeatScentDirectionForward:0.00}   right {senses.RottenMeatScentDirectionRight:0.00}\n\n" +
            $"Communication\n" +
            $"Sound {(senses.SoundDetected ? "yes" : "no")}   density {senses.SoundDensity:0.00}   clarity {senses.SoundToneClarity:0.00}\n" +
            $"Sound tone {senses.SoundTone:0.00}   fwd {senses.SoundDirectionForward:0.00}   right {senses.SoundDirectionRight:0.00}\n\n" +
            $"Creatures\n" +
            $"Seen {(senses.CreatureDetected ? "yes" : "no")}   density {senses.VisibleCreatureDensity:0.00}\n" +
            $"Prox {senses.CreatureProximity:0.00}   fwd {senses.CreatureDirectionForward:0.00}   right {senses.CreatureDirectionRight:0.00}\n" +
            $"Size {senses.CreatureRelativeBodySize:0.00}   speed {senses.CreatureRelativeSpeed:0.00}\n" +
            $"Approach {senses.CreatureApproachRate:0.00}   facing {senses.CreatureFacingAlignment:0.00}\n\n" +
            $"Touch & Terrain\n" +
            $"Food contact {(creature.IsTouchingFood ? "yes" : "no")}\n" +
            BuildFoodContactText(creature, genome) +
            $"Creature contact {senses.CreatureContact:0.00}   can grab {senses.CanGrabCreature:0.00}   holding {senses.IsHoldingCreature:0.00}\n" +
            $"Grab pressure {senses.GrabPressure:0.00}   fwd {senses.GrabDirectionForward:0.00}   right {senses.GrabDirectionRight:0.00}\n" +
            $"Plant taste energy {senses.PlantFoodContactEnergyQuality:0.00}   bite {senses.PlantFoodContactBiteEase:0.00}\n" +
            $"Terrain now {senses.CurrentTerrainDrag:0.00}   ahead {senses.ForwardTerrainDrag:0.00}   L {senses.LeftTerrainDrag:0.00}   R {senses.RightTerrainDrag:0.00}\n" +
            $"Habitat now {senses.CurrentHabitatQuality:0.00}   ahead {senses.ForwardHabitatQuality:0.00}   L {senses.LeftHabitatQuality:0.00}   R {senses.RightHabitatQuality:0.00}\n" +
            $"Temp now {FormatTemperatureIndex(senses.CurrentTemperature)}   ahead {FormatTemperatureIndex(senses.ForwardTemperature)}   L {FormatTemperatureIndex(senses.LeftTemperature)}   R {FormatTemperatureIndex(senses.RightTemperature)}\n" +
            $"Thermal mismatch now {FormatPercent(senses.CurrentThermalMismatch)}   ahead {FormatPercent(senses.ForwardThermalMismatch)}   L {FormatPercent(senses.LeftThermalMismatch)}   R {FormatPercent(senses.RightThermalMismatch)}\n" +
            $"Obstacle fwd {senses.ForwardObstacle:0.00}   L {senses.LeftObstacle:0.00}   R {senses.RightObstacle:0.00}   blocked {senses.MovementBlocked:0.00}\n\n" +
            $"Internal Feedback\n" +
            $"Energy surplus {senses.EnergySurplusRatio:0.00}   full {senses.EnergyFullnessRatio:0.00}   gut full {senses.GutFullnessRatio:0.00}\n" +
            $"Fat {senses.FatRatio:0.00}   mass {senses.MassBurdenRatio:0.00}\n" +
            $"Food success {senses.RecentFoodSuccess:0.00}\n" +
            $"Food yield {senses.RecentFoodEnergyYield:0.00}   plant energy {senses.RecentPlantEnergyYield:0.00}   raw {senses.RecentPlantRawYield:0.00}\n" +
            $"Payoff trace T {senses.TenderPlantPayoffTrace:0.00}   R {senses.RichPlantPayoffTrace:0.00}   Tough {senses.ToughPlantPayoffTrace:0.00}\n" +
            $"Memory {senses.MemoryStrength:0.00}   fwd {senses.MemoryDirectionForward:0.00}   right {senses.MemoryDirectionRight:0.00}\n";
    }

    private string BuildCreatureBrainInspectorText(CreatureState creature)
    {
        var senses = creature.Senses;
        var architecture = creature.BrainId < 0
            ? "none"
            : FormatBrainArchitectureKind(_simulation.State.GetBrainArchitectureKind(creature.BrainId));

        return
            $"Brain\n" +
            $"{FormatBrainText(creature.BrainId)}\n" +
            $"Architecture {architecture}\n\n" +
            $"Outputs\n" +
            $"Move {creature.Actions.MoveForward:0.00}   turn {creature.Actions.Turn:0.00}\n" +
            $"Eat output {creature.Actions.EatOutput:0.00}   intent {creature.Actions.WantsEat}\n" +
            $"Repro output {creature.Actions.ReproduceOutput:0.00}   intent {creature.Actions.WantsReproduce}\n" +
            $"Attack output {creature.Actions.AttackOutput:0.00}   intent {creature.Actions.WantsAttack}\n" +
            $"Grab output {creature.Actions.GrabOutput:0.00}   intent {creature.Actions.WantsGrab}\n" +
            $"Sound amp {creature.Actions.SoundAmplitude:0.00}   tone {creature.Actions.SoundTone:0.00}\n" +
            $"Memory write fwd {creature.Actions.MemoryForward:0.00}   right {creature.Actions.MemoryRight:0.00}\n\n" +
            $"Action Context\n" +
            $"Touching food {(creature.IsTouchingFood ? "yes" : "no")}   touching creature {(creature.IsTouchingCreature ? "yes" : "no")}\n" +
            $"Repro ready {(senses.ReproductionReadiness > 0.5f ? "yes" : "no")}   egg reserve {senses.EggReserveRatio:P0}\n" +
            $"Energy full {senses.EnergyFullnessRatio:P0}   gut full {senses.GutFullnessRatio:P0}\n" +
            $"Fat {senses.FatRatio:P0}   mass burden {senses.MassBurdenRatio:P0}\n" +
            $"Holding {FormatCreatureReference(creature.HeldCreatureId)}   grabbed by {FormatCreatureReference(creature.GrabbedByCreatureId)}   pressure {creature.GrabPressure:0.00}\n" +
            $"Attack near gate {creature.Actions.AttackOutput:0.00}   last damage {creature.LastAttackDamageDealt:0.000}\n\n" +
            $"Recent Reward Signals\n" +
            $"Food success {senses.RecentFoodSuccess:0.00}   food energy {senses.RecentFoodEnergyYield:0.00}\n" +
            $"Plant energy {senses.RecentPlantEnergyYield:0.00}   raw plant {senses.RecentPlantRawYield:0.00}\n" +
            $"Typed payoff T {senses.RecentTenderPlantEnergyYield:0.00}   R {senses.RecentRichPlantEnergyYield:0.00}   Tough {senses.RecentToughPlantEnergyYield:0.00}\n" +
            $"Trace T {senses.TenderPlantPayoffTrace:0.00}   R {senses.RichPlantPayoffTrace:0.00}   Tough {senses.ToughPlantPayoffTrace:0.00}\n\n" +
            $"Memory\n" +
            $"Stored vector strength {senses.MemoryStrength:0.00}\n" +
            $"Input fwd {senses.MemoryDirectionForward:0.00}   right {senses.MemoryDirectionRight:0.00}\n" +
            $"Output fwd {creature.Actions.MemoryForward:0.00}   right {creature.Actions.MemoryRight:0.00}\n";
    }

    private static string BuildVisionSectorHitSummary(VisionSectorSet sectors)
    {
        if (!sectors.HasAnySignal)
        {
            return "none";
        }

        var parts = new List<string>();
        for (var i = 0; i < VisionSectorSet.SectorCount; i++)
        {
            var sample = sectors.Get(i);
            var labels = new List<string>();
            AddVisionSectorLabel(labels, "P", sample.PlantDensity, sample.PlantProximity);
            AddVisionSectorLabel(labels, "M", sample.MeatDensity, sample.MeatProximity);
            AddVisionSectorLabel(labels, "E", sample.EggDensity, sample.EggProximity);
            AddVisionSectorLabel(labels, "C", sample.CreatureDensity, sample.CreatureProximity);
            if (labels.Count > 0)
            {
                parts.Add($"{i}:{string.Join('/', labels)}");
            }
        }

        if (parts.Count == 0)
        {
            return "none";
        }

        const int maxParts = 6;
        var summary = string.Join(' ', parts.Take(maxParts));
        return parts.Count > maxParts
            ? $"{summary} +{parts.Count - maxParts}"
            : summary;
    }

    private static void AddVisionSectorLabel(List<string> labels, string prefix, float density, float proximity)
    {
        if (VisionSignal(density, proximity) > 0.001f)
        {
            labels.Add($"{prefix}{proximity:0.0}");
        }
    }

    private string BuildEggInspectorText(EggState egg)
    {
        var healthRatio = EggHealthRatio(egg);
        var hatchProgress = EggHatchProgress(egg);
        var remainingSeconds = MathF.Max(0f, egg.IncubationSeconds - egg.AgeSeconds);
        var exposureMultiplier = EggEnvironmentalDamageSystem.GetExposureMultiplier(_simulation.State.Biomes, egg.Position);
        var damagePerSecond = _scenario.EggEnvironmentalDamagePerSecond * exposureMultiplier;
        var hatchlingHealth = OffspringDevelopment.HatchlingHealth(egg.Health, egg.MaxHealth, egg.InvestmentRatio);
        var biome = _simulation.State.Biomes.GetKindAt(egg.Position);
        var inVoid = _simulation.State.Biomes.IsInResourceVoid(egg.Position);
        var exposureText = damagePerSecond > 0f
            ? $"damage {damagePerSecond:0.000}/s"
            : "safe";
        var survivalText = damagePerSecond > 0f && egg.Health / damagePerSecond < remainingSeconds
            ? $"fails in {egg.Health / damagePerSecond:0.0}s"
            : "viable at current exposure";
        var brainText = FormatBrainText(egg.BrainId);

        return
            $"Selected egg #{egg.Id.Value}\n" +
            $"Parent #{egg.ParentId.Value}\n" +
            $"Generation {egg.Generation}\n" +
            $"Genome {egg.GenomeId}  Brain {brainText}\n" +
            $"Energy {egg.Energy:0.0}\n" +
            $"Health {egg.Health:0.00}/{egg.MaxHealth:0.00} ({healthRatio:P0})\n" +
            $"Investment {egg.InvestmentRatio:0.00}x\n" +
            $"Hatchling health {hatchlingHealth:0.00}\n" +
            $"Age {egg.AgeSeconds:0.0}s\n" +
            $"Incubation {egg.IncubationSeconds:0.0}s ({hatchProgress:P0})\n" +
            $"Hatches in {remainingSeconds:0.0}s\n" +
            $"Biome {FormatBiomeKind(biome)}\n" +
            $"Void {(inVoid ? "yes" : "no")}\n" +
            $"Exposure {exposureMultiplier:0.00}x  {exposureText}\n" +
            $"Status {survivalText}";
    }

    private string BuildFoodContactText(CreatureState creature, CreatureGenome genome)
    {
        if (!creature.IsTouchingFood)
        {
            return string.Empty;
        }

        if (creature.FoodContactKind == FoodContactKind.Resource)
        {
            for (var i = 0; i < _simulation.State.Resources.Count; i++)
            {
                var resource = _simulation.State.Resources[i];
                if (resource.Id != creature.FoodContactResourceId)
                {
                    continue;
                }

                var digestionEfficiency = resource.Kind == ResourceKind.Meat
                    ? CreatureDigestion.MeatEnergyEfficiency(genome, MeatQuality.Freshness(resource))
                    : CreatureDigestion.PlantTypeEnergyEfficiency(genome, resource.PlantKind);
                var plantTypeText = resource.Kind == ResourceKind.Plant
                    ? $"Plant type {resource.PlantKind.ToString().ToLowerInvariant()}\n"
                    : string.Empty;
                var freshnessText = resource.Kind == ResourceKind.Meat
                    ? $"Food fresh {MeatQuality.Freshness(resource):P0}\n"
                    : string.Empty;

                return
                    $"Food type {FormatResourceKind(resource.Kind)}\n" +
                    plantTypeText +
                    freshnessText +
                    $"Digest eff {digestionEfficiency:P0}\n" +
                    $"Food edge {creature.FoodContactEdgeDistance:0.0}/{CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}\n" +
                    $"Food kcal {creature.FoodContactCalories:0.00}\n";
            }
        }

        if (creature.FoodContactKind == FoodContactKind.Egg)
        {
            return
                $"Food type egg\n" +
                $"Digest eff {CreatureDigestion.FreshMeatEnergyEfficiency(genome):P0}\n" +
                $"Food edge {creature.FoodContactEdgeDistance:0.0}/{CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}\n" +
                $"Food kcal {creature.FoodContactCalories:0.00}\n";
        }

        if (creature.FoodContactKind == FoodContactKind.SmallPrey)
        {
            return
                $"Food type small prey\n" +
                $"Digest eff {CreatureDigestion.FreshMeatEnergyEfficiency(genome):P0}\n" +
                $"Food edge {creature.FoodContactEdgeDistance:0.0}/{CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}\n" +
                $"Food kcal {creature.FoodContactCalories:0.00}\n";
        }

        return
            $"Food edge {creature.FoodContactEdgeDistance:0.0}/{CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}\n" +
            $"Food kcal {creature.FoodContactCalories:0.00}\n";
    }

    private static string BuildLastMealSourceText(CreatureState creature)
    {
        if (creature.LastCaloriesEaten <= 0f)
        {
            return "none";
        }

        var source = "plant";
        var amount = creature.LastPlantCaloriesEaten;
        if (creature.LastCarcassCaloriesEaten > amount)
        {
            source = "carcass";
            amount = creature.LastCarcassCaloriesEaten;
        }

        if (creature.LastEggCaloriesEaten > amount)
        {
            source = "egg";
            amount = creature.LastEggCaloriesEaten;
        }

        if (creature.LastSmallPreyCaloriesEaten > amount)
        {
            source = "small prey";
            amount = creature.LastSmallPreyCaloriesEaten;
        }

        if (creature.LastLivePreyCaloriesEaten > amount)
        {
            source = "fresh kill";
            amount = creature.LastLivePreyCaloriesEaten;
        }

        return $"{source} {amount:0.00} raw";
    }

    private bool TryGetSpriteTheme(out SpriteTheme theme)
    {
        theme = null!;
        if (_visualRenderMode != VisualRenderMode.SpriteTheme || _spriteThemes.Count == 0)
        {
            return false;
        }

        _spriteThemeIndex = Math.Clamp(_spriteThemeIndex, 0, _spriteThemes.Count - 1);
        theme = _spriteThemes[_spriteThemeIndex];
        return true;
    }

    private bool TryDrawSpriteRegion(SpriteTheme theme, SpriteAtlasSlot slot, Vector2 center, Vector2 size, float rotation, Color modulate)
    {
        var index = (int)slot;
        if (index < 0 || index >= theme.Regions.Length || theme.Texture is null)
        {
            return false;
        }

        DrawSetTransform(center, rotation, Vector2.One);
        DrawTextureRectRegion(
            theme.Texture,
            new Rect2(size * -0.5f, size),
            theme.Regions[index],
            modulate);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        return true;
    }

    private bool TryDrawSpriteColorMask(SpriteTheme theme, SpriteAtlasSlot slot, Vector2 center, Vector2 size, float rotation, Color color)
    {
        var index = (int)slot;
        if (index < 0 || index >= theme.Regions.Length || theme.ColorMaskTexture is null)
        {
            return false;
        }

        DrawSetTransform(center, rotation, Vector2.One);
        DrawTextureRectRegion(
            theme.ColorMaskTexture,
            new Rect2(size * -0.5f, size),
            theme.Regions[index],
            new Color(color.R, color.G, color.B, CreatureSpriteColorMaskAlpha));
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        return true;
    }

    private void DrawColorLegend()
    {
        var entries = GetColorLegendEntries();
        if (entries.Length == 0 || _worldRect.Size.X < 260f || _worldRect.Size.Y < 180f)
        {
            return;
        }

        var titleHeight = 24f;
        var height = ColorLegendPadding * 2f + titleHeight + entries.Length * ColorLegendRowHeight;
        var position = new Vector2(
            _worldRect.End.X - ColorLegendWidth - 12f,
            _worldRect.Position.Y + 12f);
        var panel = new Rect2(position, new Vector2(ColorLegendWidth, height));

        DrawRect(panel, new Color(0.02f, 0.025f, 0.024f, 0.80f), filled: true);
        DrawRect(panel, new Color(0.86f, 0.90f, 0.82f, 0.32f), filled: false, width: 1f);

        DrawLegendText(
            position + new Vector2(ColorLegendPadding, ColorLegendPadding + 15f),
            $"{FormatColorMode(_colorMode)} colors",
            new Color(0.96f, 0.98f, 0.92f));

        for (var i = 0; i < entries.Length; i++)
        {
            var rowTop = position.Y + ColorLegendPadding + titleHeight + i * ColorLegendRowHeight;
            var sampleCenter = new Vector2(position.X + ColorLegendPadding + 18f, rowTop + ColorLegendRowHeight * 0.5f);
            DrawColorLegendSample(sampleCenter, entries[i].Color);
            DrawLegendText(
                new Vector2(position.X + ColorLegendPadding + 44f, rowTop + 22f),
                entries[i].Label,
                new Color(0.90f, 0.93f, 0.88f));
        }
    }

    private ColorLegendEntry[] GetColorLegendEntries()
    {
        return _colorMode switch
        {
            CreatureColorMode.Generation => GetGenerationLegendEntries(),
            CreatureColorMode.Energy => new[]
            {
                new ColorLegendEntry("red = low energy", ColorForEnergy(0f, 1f)),
                new ColorLegendEntry("repro threshold", ColorForEnergy(1f, 1f)),
                new ColorLegendEntry("green = high reserve", ColorForEnergy(1.5f, 1f))
            },
            CreatureColorMode.Age => new[]
            {
                new ColorLegendEntry("cyan = young", ColorForAge(0f)),
                new ColorLegendEntry("yellow = middle", ColorForAge(450f)),
                new ColorLegendEntry("magenta = old", ColorForAge(900f))
            },
            _ => Array.Empty<ColorLegendEntry>()
        };
    }

    private ColorLegendEntry[] GetGenerationLegendEntries()
    {
        if (_simulation.State.Creatures.Count == 0)
        {
            return Array.Empty<ColorLegendEntry>();
        }

        var entries = new List<ColorLegendEntry>(3);
        AddGenerationLegendEntry(entries, _livingMinGeneration, "low");
        if (_livingMaxGeneration > _livingMinGeneration + 1)
        {
            AddGenerationLegendEntry(entries, (_livingMinGeneration + _livingMaxGeneration) / 2, "mid");
        }

        AddGenerationLegendEntry(entries, _livingMaxGeneration, "max");
        return entries.ToArray();
    }

    private void AddGenerationLegendEntry(List<ColorLegendEntry> entries, int generation, string label)
    {
        foreach (var entry in entries)
        {
            if (entry.Generation == generation)
            {
                return;
            }
        }

        entries.Add(new ColorLegendEntry(
            $"Gen {generation} ({label})",
            ColorForGeneration(generation, _livingMinGeneration, _livingMaxGeneration),
            generation));
    }

    private void DrawColorLegendSample(Vector2 center, Color color)
    {
        if (_visualRenderMode == VisualRenderMode.SpriteTheme && TryGetSpriteTheme(out var theme))
        {
            var size = new Vector2(ColorLegendSampleSize, ColorLegendSampleSize);
            if (TryDrawSpriteRegion(theme, SpriteAtlasSlot.CreatureGeneralist, center, size, 0f, Colors.White))
            {
                TryDrawSpriteColorMask(theme, SpriteAtlasSlot.CreatureGeneralist, center, size, 0f, color);
                return;
            }
        }

        var radius = ColorLegendSampleSize * 0.34f;
        DrawCircle(center, radius, color);
        DrawLine(center, center + new Vector2(radius + 7f, 0f), Colors.Black, width: 1.2f);
    }

    private void DrawLegendText(Vector2 baseline, string text, Color color)
    {
        var font = ThemeDB.FallbackFont;
        if (font is null)
        {
            return;
        }

        DrawString(font, baseline, text, HorizontalAlignment.Left, -1f, 14, color);
    }

    private void DrawSpriteBackplate(Vector2 center, float radius, Color color, float alpha)
    {
        if (radius <= 0f)
        {
            return;
        }

        var clampedAlpha = Math.Clamp(alpha, 0f, 0.8f);
        DrawCircle(center + new Vector2(1.25f, 1.5f), radius * 1.08f, new Color(0.0f, 0.0f, 0.0f, clampedAlpha * 0.72f));
        DrawCircle(center, radius, new Color(color.R, color.G, color.B, clampedAlpha));
    }

    private void DrawSpriteShadow(Vector2 center, float radius, float alpha)
    {
        if (radius <= 0f)
        {
            return;
        }

        DrawCircle(center + new Vector2(1.25f, 1.5f), radius * 1.08f, new Color(0.0f, 0.0f, 0.0f, Math.Clamp(alpha, 0f, 0.7f)));
    }

    private static float SpriteSizeFromScreenRadius(float radius, float scale, float minSize, float maxSize)
    {
        var softenedRadius = MathF.Pow(MathF.Max(0f, radius), 0.72f);
        return Math.Clamp(minSize + softenedRadius * scale, minSize, maxSize);
    }

    private bool TryDrawSpriteResource(ResourcePatchState resource, Vector2 screenPosition, float radius, Color color, float fullness)
    {
        if (!TryGetSpriteTheme(out var theme) || radius < MinResourceSpriteRadiusPixels)
        {
            return false;
        }

        var slot = SpriteSlotForResource(resource, fullness);
        var sizePixels = resource.Kind == ResourceKind.Meat
            ? SpriteSizeFromScreenRadius(radius, MeatSpriteScalePixels, MinResourceSpriteSizePixels, MaxMeatSpriteSizePixels)
            : SpriteSizeFromScreenRadius(radius, PlantSpriteScalePixels, MinResourceSpriteSizePixels, MaxPlantSpriteSizePixels);
        var animation = resource.Kind == ResourceKind.Plant
            ? PlantSpriteAnimation(resource, fullness, sizePixels)
            : FoodSpriteEatingAnimation(resource.Id, _drawEatingMeatResourceIds, sizePixels);
        var animatedCenter = screenPosition + animation.Offset;
        var animatedSize = new Vector2(sizePixels * animation.Scale.X, sizePixels * animation.Scale.Y);
        var modulate = ResourceSpriteTint(resource, color, fullness);
        var rotation = resource.Kind == ResourceKind.Meat
            ? StableAngle(resource.Id.Value)
            : 0f;
        DrawSpriteBackplate(
            animatedCenter,
            sizePixels * 0.30f * MathF.Max(animation.Scale.X, animation.Scale.Y),
            color,
            resource.Kind == ResourceKind.Meat ? 0.32f : animation.IsEating ? 0.34f : 0.22f);
        return TryDrawSpriteRegion(theme, slot, animatedCenter, animatedSize, rotation + animation.Rotation, modulate);
    }

    private (Vector2 Offset, Vector2 Scale, float Rotation, bool IsEating) PlantSpriteAnimation(
        ResourcePatchState resource,
        float fullness,
        float sizePixels)
    {
        var phase = StableAngle(resource.Id.Value);
        var growth = Math.Clamp(fullness, 0.15f, 1f);
        var isEating = _drawEatingPlantResourceIds.Contains(resource.Id);
        var wind = (MathF.Sin(_drawVisualTimeSeconds * 1.15f + phase)
            + MathF.Sin(_drawVisualTimeSeconds * 2.05f + phase * 1.7f) * 0.45f)
            * 0.55f
            * (0.35f + growth * 0.65f);
        var sway = MathF.Abs(wind);
        var rotation = wind * (isEating ? PlantSpriteEatingWindRotationRadians : PlantSpriteIdleWindRotationRadians);
        var scaleX = 1f + sway * (isEating ? PlantSpriteEatingWindStretch : PlantSpriteIdleWindStretch);
        var scaleY = 1f - sway * (isEating ? PlantSpriteEatingWindSquash : PlantSpriteIdleWindSquash);
        var offsetFactor = isEating ? 0.025f : 0.038f;
        var maxOffset = isEating ? 1.8f : 2.7f;
        var offset = new Vector2(
            wind * Math.Clamp(sizePixels * offsetFactor, 0.4f, maxOffset),
            0f);

        if (isEating)
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(_drawVisualTimeSeconds * 18f + phase);
            rotation += MathF.Sin(_drawVisualTimeSeconds * 28f + phase) * PlantSpriteEatingShakeRadians;
            scaleX += PlantSpriteEatingStretch * (0.55f + pulse * 0.45f);
            scaleY -= PlantSpriteEatingSquash * (0.55f + pulse * 0.45f);
            offset += new Vector2(
                MathF.Sin(_drawVisualTimeSeconds * 32f + phase) * 1.5f,
                MathF.Cos(_drawVisualTimeSeconds * 24f + phase) * 0.65f);
        }

        return (
            offset,
            new Vector2(Math.Clamp(scaleX, 0.78f, 1.28f), Math.Clamp(scaleY, 0.76f, 1.18f)),
            rotation,
            isEating);
    }

    private (Vector2 Offset, Vector2 Scale, float Rotation, bool IsEating) FoodSpriteEatingAnimation(
        EntityId foodId,
        HashSet<EntityId> eatingIds,
        float sizePixels)
    {
        if (!eatingIds.Contains(foodId))
        {
            return (Vector2.Zero, Vector2.One, 0f, false);
        }

        var phase = StableAngle(foodId.Value);
        var pulse = 0.5f + 0.5f * MathF.Sin(_drawVisualTimeSeconds * 18f + phase);
        var shake = MathF.Sin(_drawVisualTimeSeconds * 30f + phase);
        var offset = new Vector2(
            shake * Math.Clamp(sizePixels * 0.030f, 0.45f, 1.8f),
            MathF.Cos(_drawVisualTimeSeconds * 22f + phase) * Math.Clamp(sizePixels * 0.014f, 0.25f, 0.85f));

        return (
            offset,
            new Vector2(
                Math.Clamp(1f + FoodSpriteEatingStretch * (0.55f + pulse * 0.45f), 0.86f, 1.20f),
                Math.Clamp(1f - FoodSpriteEatingSquash * (0.55f + pulse * 0.45f), 0.84f, 1.12f)),
            shake * FoodSpriteEatingShakeRadians,
            true);
    }

    private bool TryDrawSpriteEgg(EggState egg, Vector2 screenPosition, float radius, Color color)
    {
        if (!TryGetSpriteTheme(out var theme) || radius < MinResourceSpriteRadiusPixels)
        {
            return false;
        }

        var slot = egg.Id.Value % 2 == 0
            ? SpriteAtlasSlot.EggA
            : SpriteAtlasSlot.EggB;
        var sizePixels = SpriteSizeFromScreenRadius(radius, EggSpriteScalePixels, MinEggSpriteSizePixels, MaxEggSpriteSizePixels);
        var animation = FoodSpriteEatingAnimation(egg.Id, _drawEatingEggIds, sizePixels);
        var animatedCenter = screenPosition + animation.Offset;
        var animatedSize = new Vector2(sizePixels * animation.Scale.X, sizePixels * animation.Scale.Y);
        var modulate = Colors.White.Lerp(color, 0.10f);
        DrawSpriteBackplate(
            animatedCenter,
            sizePixels * 0.34f * MathF.Max(animation.Scale.X, animation.Scale.Y),
            color,
            animation.IsEating ? 0.38f : 0.26f);
        return TryDrawSpriteRegion(
            theme,
            slot,
            animatedCenter,
            animatedSize,
            StableAngle(egg.Id.Value) + animation.Rotation,
            modulate);
    }

    private bool TryDrawSpriteSmallPrey(SmallPreyState prey, Vector2 screenPosition, float radius, Color color)
    {
        if (!TryGetSpriteTheme(out var theme) || radius < MinResourceSpriteRadiusPixels)
        {
            return false;
        }

        var sizePixels = SpriteSizeFromScreenRadius(
            radius,
            SmallPreySpriteScalePixels,
            MinSmallPreySpriteSizePixels,
            MaxSmallPreySpriteSizePixels);
        var animation = FoodSpriteEatingAnimation(prey.Id, _drawEatingSmallPreyIds, sizePixels);
        var animatedCenter = screenPosition + animation.Offset;
        var animatedSize = new Vector2(sizePixels * animation.Scale.X, sizePixels * animation.Scale.Y);
        var modulate = prey.HeldByCreatureId == default
            ? Colors.White.Lerp(color, 0.08f)
            : Colors.White.Lerp(_selectedColor, 0.18f);
        DrawSpriteBackplate(
            animatedCenter,
            sizePixels * 0.30f * MathF.Max(animation.Scale.X, animation.Scale.Y),
            color,
            prey.HeldByCreatureId == default ? animation.IsEating ? 0.34f : 0.24f : 0.42f);
        return TryDrawSpriteRegion(
            theme,
            SpriteAtlasSlot.SmallPrey,
            animatedCenter,
            animatedSize,
            prey.HeadingRadians + animation.Rotation,
            modulate);
    }

    private bool TryDrawSpriteCreature(CreatureState creature, CreatureGenome genome, Vector2 screenPosition, float radius, Color color)
    {
        if (!TryGetSpriteTheme(out var theme) || radius < MinCreatureSpriteRadiusPixels)
        {
            return false;
        }

        var slot = SpriteSlotForCreature(genome);
        var sizePixels = SpriteSizeFromScreenRadius(radius, CreatureSpriteScalePixels, MinCreatureSpriteSizePixels, MaxCreatureSpriteSizePixels);
        var animationScale = CreatureSpriteAnimationScale(creature, genome);
        var animatedSize = new Vector2(sizePixels * animationScale.X, sizePixels * animationScale.Y);
        var selected = creature.Id == _selectedCreatureId;
        var colorModeEnabled = _colorMode != CreatureColorMode.Off;
        var backplateRadius = sizePixels * 0.34f * MathF.Max(animationScale.X, animationScale.Y);
        if (selected)
        {
            DrawSpriteBackplate(screenPosition, backplateRadius, _selectedColor, 0.54f);
        }

        var drawn = TryDrawSpriteRegion(
            theme,
            slot,
            screenPosition,
            animatedSize,
            creature.HeadingRadians,
            Colors.White);
        if (drawn && colorModeEnabled)
        {
            TryDrawSpriteColorMask(theme, slot, screenPosition, animatedSize, creature.HeadingRadians, color);
        }

        if (drawn && theme.DrawProceduralCreatureEyes)
        {
            DrawCreatureSpriteEyes(creature, screenPosition, sizePixels, animationScale);
        }

        return drawn;
    }

    private Vector2 CreatureSpriteAnimationScale(CreatureState creature, CreatureGenome genome)
    {
        var effectiveMaxSpeed = MathF.Max(0.001f, CreatureGrowth.EffectiveMaxSpeed(creature, genome));
        var speedRatio = Math.Clamp(creature.Velocity.Length / effectiveMaxSpeed, 0f, 1.35f);
        var phase = StableAngle(creature.Id.Value);
        var gait = MathF.Sin(_drawVisualTimeSeconds * (5.5f + speedRatio * 5.5f) + phase) * speedRatio;

        var forwardScale = 1f
            + speedRatio * CreatureSpriteMotionStretch
            + gait * CreatureSpriteGaitStretch;
        var sideScale = 1f
            - speedRatio * CreatureSpriteMotionSquash
            - gait * CreatureSpriteGaitSquash;

        var turnAmount = Math.Clamp(MathF.Abs(creature.Actions.Turn), 0f, 1f);
        forwardScale -= turnAmount * 0.035f;
        sideScale += turnAmount * 0.055f;

        if (creature.Actions.WantsEat || creature.LastCaloriesEaten > 0f)
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(_drawVisualTimeSeconds * 12f + phase);
            forwardScale -= 0.025f + pulse * 0.025f;
            sideScale += 0.045f + pulse * 0.030f;
        }

        if (creature.LastAttackDamageDealt > 0f)
        {
            forwardScale += 0.18f;
            sideScale -= 0.08f;
        }

        if (creature.LastAttackDamageTaken > 0f)
        {
            forwardScale -= 0.10f;
            sideScale += 0.10f;
        }

        var grabPressure = Math.Clamp(creature.GrabPressure, 0f, 1f);
        if (grabPressure > 0f)
        {
            forwardScale -= grabPressure * 0.055f;
            sideScale += grabPressure * 0.045f;
        }

        return new Vector2(
            Math.Clamp(forwardScale, 0.72f, 1.40f),
            Math.Clamp(sideScale, 0.72f, 1.32f));
    }

    private void DrawCreatureSpriteEyes(CreatureState creature, Vector2 screenPosition, float sizePixels, Vector2 animationScale)
    {
        if (sizePixels < 18f)
        {
            return;
        }

        var forward = ToGodot(SimVector2.FromAngle(creature.HeadingRadians));
        var side = new Vector2(-forward.Y, forward.X);
        var eyeRadius = Math.Clamp(sizePixels * 0.075f, 1.4f, 4.4f);
        var pupilRadius = Math.Clamp(eyeRadius * 0.42f, 0.7f, 1.8f);
        var eyeBase = screenPosition + forward * sizePixels * animationScale.X * 0.22f;
        var spacing = Math.Clamp(sizePixels * animationScale.Y * 0.105f, 2.0f, 7.5f);
        var lookOffset = forward * eyeRadius * 0.32f;
        var left = eyeBase + side * spacing;
        var right = eyeBase - side * spacing;

        DrawCircle(left, eyeRadius, Colors.White);
        DrawCircle(right, eyeRadius, Colors.White);
        DrawCircle(left + lookOffset, pupilRadius, Colors.Black);
        DrawCircle(right + lookOffset, pupilRadius, Colors.Black);
    }

    private SpriteAtlasSlot SpriteSlotForResource(ResourcePatchState resource, float fullness)
    {
        if (resource.Kind == ResourceKind.Meat)
        {
            return MeatQuality.Freshness(resource) > 0.45f
                ? SpriteAtlasSlot.MeatFresh
                : SpriteAtlasSlot.MeatStale;
        }

        if (fullness < 0.16f)
        {
            return SpriteAtlasSlot.PlantDormant;
        }

        return resource.PlantKind switch
        {
            PlantResourceKind.Tender => SpriteAtlasSlot.PlantTender,
            PlantResourceKind.Rich => SpriteAtlasSlot.PlantRich,
            PlantResourceKind.Tough => SpriteAtlasSlot.PlantTough,
            _ => SpriteAtlasSlot.PlantGeneric
        };
    }

    private static SpriteAtlasSlot SpriteSlotForCreature(CreatureGenome genome)
    {
        var plantScore = genome.TenderPlantAdaptation;
        var secondPlantScore = MathF.Max(genome.RichPlantAdaptation, genome.ToughPlantAdaptation);
        if (genome.RichPlantAdaptation > plantScore)
        {
            secondPlantScore = MathF.Max(plantScore, genome.ToughPlantAdaptation);
            plantScore = genome.RichPlantAdaptation;
        }
        else if (genome.ToughPlantAdaptation > plantScore)
        {
            secondPlantScore = MathF.Max(plantScore, genome.RichPlantAdaptation);
            plantScore = genome.ToughPlantAdaptation;
        }

        var animalScore = MathF.Max(genome.DietaryAdaptation, genome.CarrionAdaptation);
        var predatorScore = MathF.Max(genome.DietaryAdaptation, genome.BiteStrength / 2.5f);
        if (genome.CarrionAdaptation >= 0.48f && genome.CarrionAdaptation >= predatorScore * 0.8f)
        {
            return SpriteAtlasSlot.CreatureScavenger;
        }

        if (predatorScore >= 0.62f)
        {
            return SpriteAtlasSlot.CreaturePredator;
        }

        if (plantScore >= 0.38f && animalScore >= 0.38f)
        {
            return SpriteAtlasSlot.CreatureOmnivore;
        }

        if (plantScore >= 0.56f && plantScore - secondPlantScore >= 0.18f)
        {
            return SpriteAtlasSlot.CreaturePlantSpecialist;
        }

        if (plantScore >= 0.45f && plantScore >= predatorScore)
        {
            return SpriteAtlasSlot.CreatureGrazer;
        }

        if (genome.DamageResistance >= 1.35f || genome.BodyRadius >= 4.4f)
        {
            return SpriteAtlasSlot.CreatureArmored;
        }

        if (genome.MaxSpeed >= 34f && genome.BodyRadius <= 4.0f)
        {
            return SpriteAtlasSlot.CreatureFast;
        }

        if (genome.SenseRadius >= 130f || genome.VisionAngleRadians >= MathF.PI * 1.25f)
        {
            return SpriteAtlasSlot.CreatureScout;
        }

        if (genome.BodyRadius <= 2.25f)
        {
            return SpriteAtlasSlot.CreatureTiny;
        }

        return SpriteAtlasSlot.CreatureGeneralist;
    }

    private static Color ResourceSpriteTint(ResourcePatchState resource, Color color, float fullness)
    {
        var tintAmount = resource.Kind == ResourceKind.Meat ? 0.14f : 0.08f + fullness * 0.06f;
        return Colors.White.Lerp(color, tintAmount);
    }

    private static float StableAngle(int id)
    {
        var hash = unchecked((ulong)id) * 11400714819323198485UL;
        return (hash & 0xffff) / 65535f * MathF.Tau;
    }

    private void DrawResources()
    {
        UpdateResourceRenderCache();
        _drawnResourceCount = 0;
        _drawnResourceAggregateCount = 0;

        var visibleWorldRect = GetVisibleWorldRect(_scenario.ResourceRadiusMax);
        _visibleResourceEstimate = _resourceRenderCache.CountVisibleDrawableResources(
            visibleWorldRect,
            MaxIndividualResourcesDrawn + 1);

        var canDrawIndividuals = _visibleResourceEstimate <= MaxIndividualResourcesDrawn
            && _scenario.ResourceRadiusMax * _worldScale >= MinIndividualResourceScreenRadius;
        _resourceRenderMode = canDrawIndividuals
            ? ResourceRenderMode.Individual
            : ResourceRenderMode.Aggregate;

        if (_resourceRenderMode == ResourceRenderMode.Individual)
        {
            DrawIndividualResources(visibleWorldRect);
        }
        else
        {
            DrawResourceAggregates(visibleWorldRect);
        }
    }

    private void UpdateResourceRenderCache()
    {
        var now = Time.GetTicksMsec();
        if (!_resourceRenderCache.NeedsRebuild(_simulation.State.Bounds, _simulation.State.Resources.Count)
            && now - _resourceCacheLastRefreshMilliseconds < ResourceCacheRefreshMilliseconds)
        {
            return;
        }

        _resourceRenderCache.Rebuild(
            _simulation.State.Bounds,
            _simulation.State.Resources,
            _simulation.State.Tick,
            ResourceRenderChunkSize);
        _resourceCacheLastRefreshMilliseconds = now;
    }

    private void InvalidateResourceRenderCache()
    {
        _resourceRenderCache = new ResourceRenderCache();
        _resourceCacheLastRefreshMilliseconds = 0;
        _visibleResourceEstimate = 0;
        _drawnResourceCount = 0;
        _drawnResourceAggregateCount = 0;
        _resourceRenderMode = ResourceRenderMode.Individual;
    }

    private void InvalidateTerrainOverlayCache()
    {
        _biomeOverlayTexture = null;
        _biomeOverlaySource = null;
        _temperatureOverlayTexture = null;
        _temperatureOverlaySource = null;
    }

    private void DrawIndividualResources(Rect2 visibleWorldRect)
    {
        foreach (var chunk in _resourceRenderCache.VisibleChunks(visibleWorldRect))
        {
            if (chunk.ResourceIndices is null)
            {
                continue;
            }

            foreach (var resourceIndex in chunk.ResourceIndices)
            {
                var resource = _simulation.State.Resources[resourceIndex];
                if (resource.Calories <= 0f || !WorldRectIntersectsCircle(visibleWorldRect, resource.Position, resource.Radius))
                {
                    continue;
                }

                var fullness = resource.MaxCalories > 0f
                    ? Mathf.Clamp(resource.Calories / resource.MaxCalories, 0f, 1f)
                    : 0f;
                var color = ColorForResource(resource, fullness);
                var screenPosition = ToScreen(resource.Position);
                var radius = MathF.Max(2f, resource.Radius * _worldScale);
                if (!TryDrawSpriteResource(resource, screenPosition, radius, color, fullness))
                {
                    DrawCircle(screenPosition, radius, color);
                    DrawPlantTypeMarker(resource, screenPosition, radius, fullness);
                }

                _drawnResourceCount++;
            }
        }
    }

    private void DrawResourceAggregates(Rect2 visibleWorldRect)
    {
        if (!_resourceRenderCache.TryGetVisibleRange(visibleWorldRect, out var visibleRange))
        {
            return;
        }

        var chunkPixels = MathF.Max(0.001f, _resourceRenderCache.ChunkSize * _worldScale);
        var chunksPerAggregate = Math.Max(1, (int)MathF.Ceiling(AggregateResourceTargetPixels / chunkPixels));
        var aggregateMinX = visibleRange.MinX / chunksPerAggregate;
        var aggregateMaxX = visibleRange.MaxX / chunksPerAggregate;
        var aggregateMinY = visibleRange.MinY / chunksPerAggregate;
        var aggregateMaxY = visibleRange.MaxY / chunksPerAggregate;

        for (var aggregateY = aggregateMinY; aggregateY <= aggregateMaxY; aggregateY++)
        {
            for (var aggregateX = aggregateMinX; aggregateX <= aggregateMaxX; aggregateX++)
            {
                var chunkMinX = aggregateX * chunksPerAggregate;
                var chunkMinY = aggregateY * chunksPerAggregate;
                var chunkMaxX = Math.Min(chunkMinX + chunksPerAggregate - 1, _resourceRenderCache.ChunkCountX - 1);
                var chunkMaxY = Math.Min(chunkMinY + chunksPerAggregate - 1, _resourceRenderCache.ChunkCountY - 1);
                if (chunkMaxX < visibleRange.MinX || chunkMaxY < visibleRange.MinY)
                {
                    continue;
                }

                chunkMinX = Math.Max(chunkMinX, visibleRange.MinX);
                chunkMinY = Math.Max(chunkMinY, visibleRange.MinY);
                chunkMaxX = Math.Min(chunkMaxX, visibleRange.MaxX);
                chunkMaxY = Math.Min(chunkMaxY, visibleRange.MaxY);

                var summary = _resourceRenderCache.SummarizeChunks(chunkMinX, chunkMinY, chunkMaxX, chunkMaxY);
                if (summary.DrawableResourceCount == 0 || summary.TotalCalories <= 0f)
                {
                    continue;
                }

                var worldRect = _resourceRenderCache.GetChunkWorldRect(chunkMinX, chunkMinY, chunkMaxX, chunkMaxY);
                if (!TryClipWorldRect(worldRect, visibleWorldRect, out var clippedWorldRect))
                {
                    continue;
                }

                var screenRect = WorldRectToScreenRect(clippedWorldRect);
                if (!TryClipRect(screenRect, _worldRect, out var clippedScreenRect))
                {
                    continue;
                }

                var area = MathF.Max(1f, clippedWorldRect.Size.X * clippedWorldRect.Size.Y);
                var expectedCaloriesPerMillion = MathF.Max(1f, _scenario.InitialResourcesPerMillionArea * _scenario.ResourceMaxCalories);
                var caloriesPerMillion = summary.TotalCalories / area * SimulationScenario.ResourceDensityAreaUnits;
                var density = Mathf.Clamp(caloriesPerMillion / expectedCaloriesPerMillion, 0f, 2f) * 0.5f;
                var fullness = summary.TotalMaxCalories > 0f
                    ? Mathf.Clamp(summary.TotalCalories / summary.TotalMaxCalories, 0f, 1f)
                    : 0f;
                var alpha = Mathf.Clamp(0.06f + density * 0.18f + fullness * 0.08f, 0.06f, MaxResourceDensityAlpha);
                var meatShare = summary.TotalCalories > 0f
                    ? Mathf.Clamp(summary.MeatCalories / summary.TotalCalories, 0f, 1f)
                    : 0f;
                var color = PlantAggregateColor(summary, fullness, alpha)
                    .Lerp(new Color(0.72f, 0.22f, 0.18f, alpha), meatShare);
                DrawRect(clippedScreenRect, color, filled: true);
                _drawnResourceAggregateCount++;
            }
        }
    }

    private void DrawEggs()
    {
        var visibleWorldRect = GetVisibleWorldRect(paddingWorld: 8f / MathF.Max(_worldScale, 0.001f));
        for (var i = 0; i < _simulation.State.Eggs.Count; i++)
        {
            var egg = _simulation.State.Eggs[i];
            if (!WorldRectIntersectsCircle(visibleWorldRect, egg.Position, 4f))
            {
                continue;
            }

            var screenPosition = ToScreen(egg.Position);
            if (!IsVisibleInWorldRect(screenPosition, 5f))
            {
                continue;
            }

            var hatchProgress = EggHatchProgress(egg);
            var healthRatio = EggHealthRatio(egg);
            var isSelected = egg.Id == _selectedEggId;
            var radius = Math.Clamp(2f + MathF.Sqrt(MathF.Max(0f, egg.MaxHealth)) * 1.2f * _worldScale, 2f, 7f);
            var color = _eggColor.Lerp(new Color(0.98f, 0.96f, 0.72f), hatchProgress * 0.45f)
                .Lerp(new Color(0.95f, 0.35f, 0.28f), 1f - healthRatio);
            if (isSelected)
            {
                color = color.Lerp(_selectedColor, 0.4f);
            }

            if (!TryDrawSpriteEgg(egg, screenPosition, radius, color))
            {
                DrawCircle(screenPosition, radius, color);
            }

            DrawArc(screenPosition, radius + 1.5f, -MathF.PI * 0.5f, -MathF.PI * 0.5f + MathF.Tau * hatchProgress, 18, _selectedColor, width: 1f);
        }
    }

    private void DrawSmallPrey()
    {
        var visibleWorldRect = GetVisibleWorldRect(paddingWorld: 8f / MathF.Max(_worldScale, 0.001f));
        for (var i = 0; i < _simulation.State.SmallPrey.Count; i++)
        {
            var prey = _simulation.State.SmallPrey[i];
            if (prey.Calories <= 0f
                || prey.Health <= 0f
                || !WorldRectIntersectsCircle(visibleWorldRect, prey.Position, prey.Radius))
            {
                continue;
            }

            var screenPosition = ToScreen(prey.Position);
            var radius = MathF.Max(2f, prey.Radius * _worldScale);
            if (!IsVisibleInWorldRect(screenPosition, radius + 4f))
            {
                continue;
            }

            var healthRatio = prey.MaxHealth > 0f
                ? Mathf.Clamp(prey.Health / prey.MaxHealth, 0f, 1f)
                : 0f;
            var caloriesRatio = prey.MaxCalories > 0f
                ? Mathf.Clamp(prey.Calories / prey.MaxCalories, 0f, 1f)
                : 0f;
            var color = _smallPreyColor
                .Lerp(_meatResourceColor, (1f - healthRatio) * 0.38f)
                .Lerp(Colors.White, (1f - caloriesRatio) * 0.18f);
            if (prey.HeldByCreatureId != default)
            {
                color = color.Lerp(_selectedColor, 0.24f);
            }

            if (!TryDrawSpriteSmallPrey(prey, screenPosition, radius, color))
            {
                DrawCircle(screenPosition, radius, color);
                var heading = new Vector2(MathF.Cos(prey.HeadingRadians), MathF.Sin(prey.HeadingRadians));
                DrawLine(screenPosition, screenPosition + heading * MathF.Max(4f, radius * 2.2f), Colors.White.Lerp(color, 0.35f), width: 1f);
            }
        }
    }

    private void DrawCreatures()
    {
        UpdateCreatureRenderCache();
        UpdateLivingGenerationRange();
        _drawnCreatureCount = 0;
        _drawnCreatureAggregateCount = 0;

        var visibleWorldRect = GetVisibleWorldRect(paddingWorld: 24f / MathF.Max(_worldScale, 0.001f));
        _visibleCreatureEstimate = _creatureRenderCache.CountVisibleCreatures(
            visibleWorldRect,
            MaxIndividualCreaturesDrawn + 1);

        var farZoomWithManyCreatures = visibleWorldRect.Size.X >= FarZoomVisibleWorldWidth
            && _visibleCreatureEstimate > FarZoomIndividualCreatureLimit;
        var tooManyVisible = _visibleCreatureEstimate > MaxIndividualCreaturesDrawn;
        _creatureRenderMode = tooManyVisible || farZoomWithManyCreatures
            ? CreatureRenderMode.Aggregate
            : CreatureRenderMode.Individual;

        if (_creatureRenderMode == CreatureRenderMode.Individual)
        {
            DrawCreatureSoundSignals(visibleWorldRect);
            DrawCreatureGrabLinks(visibleWorldRect);
            DrawIndividualCreatures(visibleWorldRect);
        }
        else
        {
            DrawCreatureAggregates(visibleWorldRect);
            DrawSelectedCreatureOverlay();
        }
    }

    private void UpdateCreatureRenderCache(bool force = false)
    {
        var now = Time.GetTicksMsec();
        if (!force
            && !_creatureRenderCache.NeedsRebuild(_simulation.State.Bounds, _simulation.State.Creatures.Count)
            && now - _creatureCacheLastRefreshMilliseconds < CreatureCacheRefreshMilliseconds)
        {
            return;
        }

        _creatureRenderCache.Rebuild(
            _simulation.State.Bounds,
            _simulation.State.Creatures,
            _simulation.State.Tick,
            CreatureRenderChunkSize);
        _creatureCacheLastRefreshMilliseconds = now;
    }

    private void InvalidateCreatureRenderCache()
    {
        _creatureRenderCache = new CreatureRenderCache();
        _creatureCacheLastRefreshMilliseconds = 0;
        _visibleCreatureEstimate = 0;
        _drawnCreatureCount = 0;
        _drawnCreatureAggregateCount = 0;
        _creatureRenderMode = CreatureRenderMode.Individual;
    }

    private void DrawIndividualCreatures(Rect2 visibleWorldRect)
    {
        foreach (var chunk in _creatureRenderCache.VisibleChunks(visibleWorldRect))
        {
            if (chunk.CreatureIndices is null)
            {
                continue;
            }

            foreach (var creatureIndex in chunk.CreatureIndices)
            {
                if (DrawCreature(_simulation.State.Creatures[creatureIndex]))
                {
                    _drawnCreatureCount++;
                }
            }
        }
    }

    private void DrawCreatureAggregates(Rect2 visibleWorldRect)
    {
        if (!_creatureRenderCache.TryGetVisibleRange(visibleWorldRect, out var visibleRange))
        {
            return;
        }

        var chunkPixels = MathF.Max(0.001f, _creatureRenderCache.ChunkSize * _worldScale);
        var chunksPerAggregate = Math.Max(1, (int)MathF.Ceiling(AggregateCreatureTargetPixels / chunkPixels));
        var aggregateMinX = visibleRange.MinX / chunksPerAggregate;
        var aggregateMaxX = visibleRange.MaxX / chunksPerAggregate;
        var aggregateMinY = visibleRange.MinY / chunksPerAggregate;
        var aggregateMaxY = visibleRange.MaxY / chunksPerAggregate;

        for (var aggregateY = aggregateMinY; aggregateY <= aggregateMaxY; aggregateY++)
        {
            for (var aggregateX = aggregateMinX; aggregateX <= aggregateMaxX; aggregateX++)
            {
                var chunkMinX = aggregateX * chunksPerAggregate;
                var chunkMinY = aggregateY * chunksPerAggregate;
                var chunkMaxX = Math.Min(chunkMinX + chunksPerAggregate - 1, _creatureRenderCache.ChunkCountX - 1);
                var chunkMaxY = Math.Min(chunkMinY + chunksPerAggregate - 1, _creatureRenderCache.ChunkCountY - 1);
                if (chunkMaxX < visibleRange.MinX || chunkMaxY < visibleRange.MinY)
                {
                    continue;
                }

                chunkMinX = Math.Max(chunkMinX, visibleRange.MinX);
                chunkMinY = Math.Max(chunkMinY, visibleRange.MinY);
                chunkMaxX = Math.Min(chunkMaxX, visibleRange.MaxX);
                chunkMaxY = Math.Min(chunkMaxY, visibleRange.MaxY);

                var summary = _creatureRenderCache.SummarizeChunks(chunkMinX, chunkMinY, chunkMaxX, chunkMaxY);
                if (summary.CreatureCount == 0)
                {
                    continue;
                }

                var worldRect = _creatureRenderCache.GetChunkWorldRect(chunkMinX, chunkMinY, chunkMaxX, chunkMaxY);
                if (!TryClipWorldRect(worldRect, visibleWorldRect, out var clippedWorldRect))
                {
                    continue;
                }

                var screenRect = WorldRectToScreenRect(clippedWorldRect);
                if (!TryClipRect(screenRect, _worldRect, out var clippedScreenRect))
                {
                    continue;
                }

                var area = MathF.Max(1f, clippedWorldRect.Size.X * clippedWorldRect.Size.Y);
                var creaturesPerMillion = summary.CreatureCount / area * SimulationScenario.ResourceDensityAreaUnits;
                var alpha = Mathf.Clamp(0.10f + MathF.Log2(summary.CreatureCount + 1f) * 0.035f, 0.10f, MaxCreatureDensityAlpha);
                var generationRatio = Mathf.Clamp(summary.MaxGeneration / 10f, 0f, 1f);
                var densityTint = Mathf.Clamp(creaturesPerMillion / 200f, 0f, 1f);
                var color = _colorMode == CreatureColorMode.Generation
                    ? WithAlpha(ColorForGeneration(summary.MaxGeneration, _livingMinGeneration, _livingMaxGeneration), alpha)
                    : new Color(0.88f, 0.76f - generationRatio * 0.18f, 0.28f + densityTint * 0.18f, alpha);
                var center = clippedScreenRect.Position + clippedScreenRect.Size * 0.5f;
                var maxRadius = MathF.Max(3f, MathF.Min(clippedScreenRect.Size.X, clippedScreenRect.Size.Y) * 0.42f);
                var radius = Math.Clamp(3f + MathF.Sqrt(summary.CreatureCount) * 0.55f, 3f, maxRadius);
                DrawCircle(center, radius, color);
                _drawnCreatureAggregateCount++;
            }
        }
    }

    private void UpdateFoodEatingAnimationSignals()
    {
        _drawEatingPlantResourceIds.Clear();
        _drawEatingMeatResourceIds.Clear();
        _drawEatingEggIds.Clear();
        _drawEatingSmallPreyIds.Clear();

        foreach (var creature in _simulation.State.Creatures)
        {
            if (creature.FoodContactResourceId == default || creature.LastCaloriesEaten <= 0f)
            {
                continue;
            }

            if (creature.FoodContactKind == FoodContactKind.Resource)
            {
                if (creature.FoodContactResourceKind == ResourceKind.Plant && creature.LastPlantCaloriesEaten > 0f)
                {
                    _drawEatingPlantResourceIds.Add(creature.FoodContactResourceId);
                }
                else if (creature.FoodContactResourceKind == ResourceKind.Meat
                    && (creature.LastCarcassCaloriesEaten > 0f
                        || creature.LastFreshMeatCaloriesEaten > 0f
                        || creature.LastStaleMeatCaloriesEaten > 0f
                        || creature.LastCaloriesEaten > 0f))
                {
                    _drawEatingMeatResourceIds.Add(creature.FoodContactResourceId);
                }
            }
            else if (creature.FoodContactKind == FoodContactKind.Egg && creature.LastEggCaloriesEaten > 0f)
            {
                _drawEatingEggIds.Add(creature.FoodContactResourceId);
            }
            else if (creature.FoodContactKind == FoodContactKind.SmallPrey && creature.LastSmallPreyCaloriesEaten > 0f)
            {
                _drawEatingSmallPreyIds.Add(creature.FoodContactResourceId);
            }
        }
    }

    private void DrawCreatureSoundSignals(Rect2 visibleWorldRect)
    {
        foreach (var chunk in _creatureRenderCache.VisibleChunks(visibleWorldRect))
        {
            if (chunk.CreatureIndices is null)
            {
                continue;
            }

            foreach (var creatureIndex in chunk.CreatureIndices)
            {
                var creature = _simulation.State.Creatures[creatureIndex];
                var amplitude = Math.Clamp(creature.Actions.SoundAmplitude, 0f, 1f);
                if (amplitude <= 0.05f)
                {
                    continue;
                }

                var screenPosition = ToScreen(creature.Position);
                var pulseRadius = 7f + amplitude * 25f;
                if (!IsVisibleInWorldRect(screenPosition, pulseRadius + 8f))
                {
                    continue;
                }

                var color = ColorForSoundTone(creature.Actions.SoundTone, 0.16f + amplitude * 0.46f);
                DrawArc(screenPosition, pulseRadius, 0f, MathF.Tau, 32, color, width: 1.0f + amplitude * 2.0f);
                DrawCircle(screenPosition, 1.8f + amplitude * 2.8f, WithAlpha(color, 0.38f + amplitude * 0.42f));
            }
        }
    }

    private void DrawCreatureGrabLinks(Rect2 visibleWorldRect)
    {
        var creatures = _simulation.State.Creatures;
        _drawCreatureById.Clear();
        for (var i = 0; i < creatures.Count; i++)
        {
            _drawCreatureById[creatures[i].Id] = creatures[i];
        }

        for (var i = 0; i < creatures.Count; i++)
        {
            var grabber = creatures[i];
            if (grabber.HeldCreatureId == default || !_drawCreatureById.TryGetValue(grabber.HeldCreatureId, out var target))
            {
                continue;
            }

            if (!WorldRectIntersectsCircle(visibleWorldRect, grabber.Position, 8f)
                && !WorldRectIntersectsCircle(visibleWorldRect, target.Position, 8f))
            {
                continue;
            }

            var grabberScreenPosition = ToScreen(grabber.Position);
            var targetScreenPosition = ToScreen(target.Position);
            if (!IsVisibleInWorldRect(grabberScreenPosition, 16f) && !IsVisibleInWorldRect(targetScreenPosition, 16f))
            {
                continue;
            }

            var pressure = Math.Clamp(target.GrabPressure, 0f, 1f);
            var color = WithAlpha(_grabLinkColor, 0.42f + pressure * 0.42f);
            DrawLine(grabberScreenPosition, targetScreenPosition, color, width: 1.2f + pressure * 2.2f);
            DrawCircle(targetScreenPosition, 2.5f + pressure * 3.5f, WithAlpha(color, 0.60f));
        }
    }

    private bool DrawCreature(CreatureState creature)
    {
        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var screenPosition = ToScreen(creature.Position);
        var radius = MathF.Max(2.5f, CreatureGrowth.EffectiveBodyRadius(creature, genome) * _worldScale);
        if (!IsVisibleInWorldRect(screenPosition, radius + 2f))
        {
            return false;
        }

        var isSelected = creature.Id == _selectedCreatureId;
        var color = isSelected ? _selectedColor : ColorForCreature(creature, genome);

        if (!TryDrawSpriteCreature(creature, genome, screenPosition, radius, color))
        {
            DrawCircle(screenPosition, radius, color);
            DrawLine(
                screenPosition,
                screenPosition + ToGodot(SimVector2.FromAngle(creature.HeadingRadians)) * (radius + 7f),
                Colors.Black,
                width: 1.5f);
        }

        if (isSelected)
        {
            DrawSelectedCreatureDetails(creature, genome, screenPosition, radius);
        }

        return true;
    }

    private void DrawSelectedCreatureOverlay()
    {
        if (_selectedCreatureId == default || !TryGetSelectedCreature(out var selected))
        {
            return;
        }

        var genome = _simulation.State.GetGenome(selected.GenomeId);
        var screenPosition = ToScreen(selected.Position);
        var radius = MathF.Max(4f, CreatureGrowth.EffectiveBodyRadius(selected, genome) * _worldScale);
        var senseRadius = CreatureGrowth.EffectiveSenseRadius(selected, genome);
        var maxRangeRadius = senseRadius * MathF.Max(
            _scenario.SoundRangeMultiplier,
            MathF.Max(_scenario.MeatScentRangeMultiplier, CreatureSensingSystem.CreatureSimilarityScentRangeMultiplier));
        if (!IsVisibleInWorldRect(screenPosition, maxRangeRadius * _worldScale + 8f))
        {
            return;
        }

        DrawCircle(screenPosition, radius, _selectedColor);
        DrawLine(
            screenPosition,
            screenPosition + ToGodot(SimVector2.FromAngle(selected.HeadingRadians)) * (radius + 7f),
            Colors.Black,
            width: 1.5f);
        DrawSelectedCreatureDetails(selected, genome, screenPosition, radius);
        _drawnCreatureCount++;
    }

    private void DrawSelectedCreatureDetails(
        CreatureState creature,
        CreatureGenome genome,
        Vector2 screenPosition,
        float radius)
    {
        DrawVisionCone(creature, genome, screenPosition);
        DrawVisionSectorDebug(creature, genome, screenPosition);
        DrawArc(screenPosition, radius + 5f, 0f, MathF.Tau, 40, _selectedColor, width: 2f);
        DrawSelectedMemoryVector(creature, screenPosition);
        DrawSelectedSenseRangeRings(creature, genome, screenPosition);
        DrawSelectedSoundOverlay(creature, genome, screenPosition);
        DrawSelectedFoodContact(creature, screenPosition);
        DrawSelectedCreatureContact(creature, screenPosition);
        DrawSelectedGrabLinks(creature, screenPosition);
    }

    private void DrawSelectedSenseRangeRings(CreatureState creature, CreatureGenome genome, Vector2 screenPosition)
    {
        var senseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
        DrawSelectedRangeRing(
            screenPosition,
            senseRadius * CreatureSensingSystem.CreatureSimilarityScentRangeMultiplier,
            _creatureScentRangeColor,
            3.0f);
        DrawSelectedRangeRing(
            screenPosition,
            senseRadius * _scenario.MeatScentRangeMultiplier,
            _meatScentRangeColor,
            3.6f);
        DrawSelectedRangeRing(
            screenPosition,
            senseRadius * _scenario.SoundRangeMultiplier,
            _soundSenseRangeColor,
            4.2f);
    }

    private void DrawSelectedRangeRing(Vector2 screenPosition, float worldRadius, Color color, float width)
    {
        var radiusPixels = worldRadius * _worldScale;
        if (radiusPixels <= 3f || radiusPixels >= 12000f)
        {
            return;
        }

        var shadowColor = new Color(0f, 0f, 0f, 0.70f);
        DrawArc(screenPosition, radiusPixels, 0f, MathF.Tau, 128, shadowColor, width: width + 2.4f);
        DrawArc(screenPosition, radiusPixels, 0f, MathF.Tau, 128, color, width: width);

        if (radiusPixels < 16f)
        {
            return;
        }

        const int tickCount = 12;
        var tickLength = Math.Clamp(radiusPixels * 0.018f, 5f, 14f);
        for (var i = 0; i < tickCount; i++)
        {
            var angle = MathF.Tau * i / tickCount;
            var direction = ToGodot(SimVector2.FromAngle(angle));
            var inner = screenPosition + direction * (radiusPixels - tickLength * 0.5f);
            var outer = screenPosition + direction * (radiusPixels + tickLength * 0.5f);
            DrawLine(inner, outer, shadowColor, width: width + 2.0f);
            DrawLine(inner, outer, color, width: width);
        }
    }

    private void DrawSelectedMemoryVector(CreatureState creature, Vector2 screenPosition)
    {
        var memory = creature.MemoryVector.ClampedLength(1f);
        var strength = memory.Length;
        if (strength <= 0.02f)
        {
            return;
        }

        var direction = ToGodot(memory.Normalized());
        var end = screenPosition + direction * (28f + 46f * strength);
        DrawLine(screenPosition, end, _memoryColor, width: 2f);
        DrawCircle(end, 3f + 2f * strength, _memoryColor);
    }

    private void DrawSelectedSoundOverlay(CreatureState creature, CreatureGenome genome, Vector2 screenPosition)
    {
        var amplitude = Math.Clamp(creature.Actions.SoundAmplitude, 0f, 1f);
        if (amplitude > 0.05f)
        {
            var rangePixels = CreatureGrowth.EffectiveSenseRadius(creature, genome)
                * _scenario.SoundRangeMultiplier
                * _worldScale;
            if (rangePixels > 3f && rangePixels < 12000f)
            {
                var emissionRangePixels = rangePixels + Math.Clamp(4f + amplitude * 10f, 5f, 14f);
                var rangeColor = ColorForSoundTone(creature.Actions.SoundTone, 0.58f + amplitude * 0.28f);
                DrawArc(screenPosition, emissionRangePixels, 0f, MathF.Tau, 128, new Color(0f, 0f, 0f, 0.72f), width: 5.2f);
                DrawArc(screenPosition, emissionRangePixels, 0f, MathF.Tau, 128, rangeColor, width: 1.8f + amplitude * 2.4f);
                DrawSoundEmissionTicks(screenPosition, emissionRangePixels, rangeColor, 2.0f + amplitude * 2.2f);
            }

            var pulseRadius = 14f + amplitude * 38f;
            DrawArc(screenPosition, pulseRadius, 0f, MathF.Tau, 48, new Color(0f, 0f, 0f, 0.68f), width: 4.6f);
            DrawArc(
                screenPosition,
                pulseRadius,
                0f,
                MathF.Tau,
                48,
                ColorForSoundTone(creature.Actions.SoundTone, 0.72f + amplitude * 0.22f),
                width: 2.2f + amplitude * 2.8f);
        }

        var senses = creature.Senses;
        if (!senses.SoundDetected)
        {
            return;
        }

        var forward = SimVector2.FromAngle(creature.HeadingRadians);
        var right = new SimVector2(-forward.Y, forward.X);
        var heardVector = forward * senses.SoundDirectionForward + right * senses.SoundDirectionRight;
        var heardStrength = heardVector.Length;
        if (heardStrength <= 0.001f)
        {
            return;
        }

        var direction = ToGodot(heardVector / heardStrength);
        var signal = Math.Clamp(MathF.Max(senses.SoundDensity, heardStrength), 0f, 1f);
        var end = screenPosition + direction * (34f + 82f * signal);
        var color = ColorForSoundTone(senses.SoundTone, 0.46f + signal * 0.38f);
        DrawLine(screenPosition, end, color, width: 1.6f + signal * 2.2f);
        DrawCircle(end, 4f + signal * 6f, WithAlpha(color, 0.42f + signal * 0.28f));
    }

    private void DrawSoundEmissionTicks(Vector2 screenPosition, float radiusPixels, Color color, float width)
    {
        if (radiusPixels < 18f)
        {
            return;
        }

        const int tickCount = 16;
        var tickLength = Math.Clamp(radiusPixels * 0.012f, 4f, 12f);
        var shadowColor = new Color(0f, 0f, 0f, 0.72f);
        for (var i = 0; i < tickCount; i++)
        {
            var angle = MathF.Tau * i / tickCount;
            var direction = ToGodot(SimVector2.FromAngle(angle));
            var inner = screenPosition + direction * (radiusPixels - tickLength);
            var outer = screenPosition + direction * (radiusPixels + tickLength);
            DrawLine(inner, outer, shadowColor, width: width + 2.4f);
            DrawLine(inner, outer, color, width: width);
        }
    }

    private void DrawSelectedEggOverlay()
    {
        if (_selectedEggId == default || !TryGetSelectedEgg(out var egg))
        {
            return;
        }

        var screenPosition = ToScreen(egg.Position);
        if (!IsVisibleInWorldRect(screenPosition, 12f))
        {
            return;
        }

        var radius = Math.Clamp(3f + MathF.Sqrt(MathF.Max(0f, egg.MaxHealth)) * 1.3f * _worldScale, 4f, 10f);
        var exposure = EggEnvironmentalDamageSystem.GetExposureMultiplier(_simulation.State.Biomes, egg.Position);
        var exposureColor = exposure > 0f
            ? new Color(1f, 0.48f, 0.28f)
            : _selectedColor;

        DrawArc(screenPosition, radius + 4f, 0f, MathF.Tau, 40, _selectedColor, width: 2f);
        DrawArc(screenPosition, radius + 7f, -MathF.PI * 0.5f, -MathF.PI * 0.5f + MathF.Tau * EggHealthRatio(egg), 40, exposureColor, width: 2f);
        if (exposure > 0f)
        {
            DrawArc(screenPosition, radius + 10f, 0f, MathF.Tau, 40, new Color(1f, 0.25f, 0.18f, 0.45f), width: 1.5f);
        }
    }

    private void DrawVisionCone(CreatureState creature, CreatureGenome genome, Vector2 screenPosition)
    {
        const int segmentCount = 36;

        var visionRange = CreatureGrowth.EffectiveSenseRadius(creature, genome);
        var visionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
        var halfAngle = visionAngle * 0.5f;
        var start = creature.HeadingRadians - halfAngle;
        var step = segmentCount > 0 ? visionAngle / segmentCount : 0f;
        var points = new Vector2[segmentCount + 2];
        var colors = new Color[points.Length];

        points[0] = screenPosition;
        colors[0] = _senseColor;
        for (var i = 0; i <= segmentCount; i++)
        {
            var angle = start + step * i;
            points[i + 1] = ToScreen(creature.Position + SimVector2.FromAngle(angle) * visionRange);
            colors[i + 1] = _senseColor;
        }

        DrawPolygon(points, colors);
        DrawPolyline(points[1..], new Color(_senseColor.R, _senseColor.G, _senseColor.B, 0.42f), width: 1.2f);
        DrawLine(
            screenPosition,
            points[1],
            new Color(_senseColor.R, _senseColor.G, _senseColor.B, 0.42f),
            width: 1.2f);
        DrawLine(
            screenPosition,
            points[^1],
            new Color(_senseColor.R, _senseColor.G, _senseColor.B, 0.42f),
            width: 1.2f);
    }

    private void DrawVisionSectorDebug(CreatureState creature, CreatureGenome genome, Vector2 screenPosition)
    {
        if (!_showVisionSectorDebug)
        {
            return;
        }

        var visionRange = CreatureGrowth.EffectiveSenseRadius(creature, genome);
        var visionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
        var halfAngle = visionAngle * 0.5f;
        var sectorAngle = visionAngle / VisionSectorSet.SectorCount;
        var start = creature.HeadingRadians - halfAngle;
        var boundaryColor = new Color(0.82f, 0.9f, 1f, 0.16f);

        for (var i = 0; i <= VisionSectorSet.SectorCount; i++)
        {
            var angle = start + sectorAngle * i;
            DrawLine(
                screenPosition,
                ToScreen(creature.Position + SimVector2.FromAngle(angle) * visionRange),
                boundaryColor,
                width: i == VisionSectorSet.CenterSectorIndex ? 1.2f : 0.8f);
        }

        var sectors = creature.Senses.VisionSectors;
        for (var i = 0; i < VisionSectorSet.SectorCount; i++)
        {
            DrawVisionSectorSample(
                creature.Position,
                screenPosition,
                start + sectorAngle * (i + 0.5f),
                visionRange,
                sectors.Get(i));
        }
    }

    private void DrawVisionSectorSample(
        SimVector2 creaturePosition,
        Vector2 screenPosition,
        float angle,
        float visionRange,
        VisionSectorSample sample)
    {
        var plantSignal = VisionSignal(sample.PlantDensity, sample.PlantProximity);
        var meatSignal = VisionSignal(sample.MeatDensity, sample.MeatProximity);
        var eggSignal = VisionSignal(sample.EggDensity, sample.EggProximity);
        var creatureSignal = VisionSignal(sample.CreatureDensity, sample.CreatureProximity);
        var strongestSignal = MathF.Max(MathF.Max(plantSignal, meatSignal), MathF.Max(eggSignal, creatureSignal));
        if (strongestSignal <= 0.001f)
        {
            return;
        }

        var direction = SimVector2.FromAngle(angle);
        var dominantColor = DominantVisionSectorColor(plantSignal, meatSignal, eggSignal, creatureSignal);
        DrawLine(
            screenPosition,
            ToScreen(creaturePosition + direction * visionRange),
            WithAlpha(dominantColor, 0.16f + strongestSignal * 0.3f),
            width: 1f + strongestSignal * 2.2f);

        var screenDirection = ToGodot(direction).Normalized();
        var markerOffset = new Vector2(-screenDirection.Y, screenDirection.X);
        DrawVisionSignalMarker(creaturePosition, direction, markerOffset, visionRange, sample.PlantDensity, sample.PlantProximity, _visionSectorPlantColor, -9f);
        DrawVisionSignalMarker(creaturePosition, direction, markerOffset, visionRange, sample.MeatDensity, sample.MeatProximity, _visionSectorMeatColor, 0f);
        DrawVisionSignalMarker(creaturePosition, direction, markerOffset, visionRange, sample.EggDensity, sample.EggProximity, _visionSectorEggColor, 9f);
        DrawVisionSignalMarker(creaturePosition, direction, markerOffset, visionRange, sample.CreatureDensity, sample.CreatureProximity, _visionSectorCreatureColor, 18f);

        if (sample.CreatureDensity > 0f && MathF.Abs(sample.CreatureApproachRate) > 0.05f)
        {
            var proximity = Math.Clamp(sample.CreatureProximity, 0f, 1f);
            var distance = Math.Clamp(visionRange * (1f - proximity), 0f, visionRange);
            var marker = ToScreen(creaturePosition + direction * distance) + markerOffset * 18f;
            var approachColor = sample.CreatureApproachRate > 0f
                ? new Color(1f, 0.52f, 0.18f, 0.86f)
                : new Color(0.25f, 0.52f, 1f, 0.72f);
            DrawArc(marker, 8f + 5f * MathF.Abs(sample.CreatureApproachRate), 0f, MathF.Tau, 28, approachColor, width: 1.4f);
        }
    }

    private void DrawVisionSignalMarker(
        SimVector2 creaturePosition,
        SimVector2 direction,
        Vector2 markerOffset,
        float visionRange,
        float density,
        float proximity,
        Color color,
        float offsetPixels)
    {
        var signal = VisionSignal(density, proximity);
        if (signal <= 0.001f)
        {
            return;
        }

        var distance = Math.Clamp(visionRange * (1f - Math.Clamp(proximity, 0f, 1f)), 0f, visionRange);
        var marker = ToScreen(creaturePosition + direction * distance) + markerOffset * offsetPixels;
        var radius = 2.5f + density * 6f + proximity * 3f;
        DrawCircle(marker, radius, WithAlpha(color, 0.35f + signal * 0.45f));
        DrawArc(marker, radius + 1.5f, 0f, MathF.Tau, 20, WithAlpha(color, 0.75f), width: 1f);
    }

    private Color DominantVisionSectorColor(float plantSignal, float meatSignal, float eggSignal, float creatureSignal)
    {
        if (creatureSignal >= plantSignal && creatureSignal >= meatSignal && creatureSignal >= eggSignal)
        {
            return _visionSectorCreatureColor;
        }

        if (meatSignal >= plantSignal && meatSignal >= eggSignal)
        {
            return _visionSectorMeatColor;
        }

        if (eggSignal >= plantSignal)
        {
            return _visionSectorEggColor;
        }

        return _visionSectorPlantColor;
    }

    private static float VisionSignal(float density, float proximity)
    {
        return MathF.Max(Math.Clamp(density, 0f, 1f), Math.Clamp(proximity, 0f, 1f));
    }

    private static Color ColorForSoundTone(float tone, float alpha)
    {
        var neutral = new Color(0.88f, 0.96f, 0.82f, alpha);
        var clampedTone = Math.Clamp(tone, -1f, 1f);
        if (clampedTone < 0f)
        {
            return LerpColor(new Color(0.18f, 0.78f, 1.0f, alpha), neutral, clampedTone + 1f);
        }

        return LerpColor(neutral, new Color(1.0f, 0.55f, 0.12f, alpha), clampedTone);
    }

    private static Color LerpColor(Color from, Color to, float amount)
    {
        var t = Math.Clamp(amount, 0f, 1f);
        return new Color(
            from.R + (to.R - from.R) * t,
            from.G + (to.G - from.G) * t,
            from.B + (to.B - from.B) * t,
            from.A + (to.A - from.A) * t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, Math.Clamp(alpha, 0f, 1f));
    }

    private void DrawSelectedGrabLinks(CreatureState creature, Vector2 creatureScreenPosition)
    {
        if (creature.HeldCreatureId != default && TryGetCreature(creature.HeldCreatureId, out var held))
        {
            DrawSelectedGrabLink(creatureScreenPosition, held, creature.GrabStrength, _grabLinkColor);
        }

        if (creature.GrabbedByCreatureId != default && TryGetCreature(creature.GrabbedByCreatureId, out var holder))
        {
            DrawSelectedGrabLink(ToScreen(holder.Position), creature, creature.GrabPressure, new Color(1f, 0.28f, 0.08f, 0.92f));
        }
    }

    private void DrawSelectedGrabLink(Vector2 holderScreenPosition, CreatureState target, float pressure, Color color)
    {
        var targetGenome = _simulation.State.GetGenome(target.GenomeId);
        var targetScreenPosition = ToScreen(target.Position);
        var targetRadius = MathF.Max(4f, CreatureGrowth.EffectiveBodyRadius(target, targetGenome) * _worldScale + 7f);
        var clampedPressure = Math.Clamp(pressure, 0f, 1f);
        var signal = Math.Clamp(0.35f + clampedPressure * 0.65f, 0f, 1f);
        var lineColor = WithAlpha(color, 0.72f + signal * 0.23f);
        var shadowColor = new Color(0f, 0f, 0f, 0.46f);
        var midpoint = (holderScreenPosition + targetScreenPosition) * 0.5f;

        DrawLine(holderScreenPosition, targetScreenPosition, shadowColor, width: 5.0f + signal * 3.0f);
        DrawLine(holderScreenPosition, targetScreenPosition, lineColor, width: 2.6f + signal * 3.2f);
        DrawArc(targetScreenPosition, targetRadius + 1f, 0f, MathF.Tau, 36, shadowColor, width: 4.8f);
        DrawArc(targetScreenPosition, targetRadius, 0f, MathF.Tau, 36, lineColor, width: 3.0f + signal * 1.8f);
        DrawCircle(midpoint, 2.5f + signal * 2.5f, lineColor);
    }

    private void DrawSelectedFoodContact(CreatureState creature, Vector2 creatureScreenPosition)
    {
        if (!creature.IsTouchingFood || creature.FoodContactResourceId == default)
        {
            return;
        }

        if (creature.FoodContactKind == FoodContactKind.Resource)
        {
            for (var i = 0; i < _simulation.State.Resources.Count; i++)
            {
                var resource = _simulation.State.Resources[i];
                if (resource.Id != creature.FoodContactResourceId)
                {
                    continue;
                }

                var resourceScreenPosition = ToScreen(resource.Position);
                var radius = MathF.Max(4f, resource.Radius * _worldScale + 3f);
                DrawLine(creatureScreenPosition, resourceScreenPosition, _selectedColor, width: 1.5f);
                DrawArc(resourceScreenPosition, radius, 0f, MathF.Tau, 40, _selectedColor, width: 2f);
                return;
            }
        }

        if (creature.FoodContactKind == FoodContactKind.Egg)
        {
            for (var i = 0; i < _simulation.State.Eggs.Count; i++)
            {
                var egg = _simulation.State.Eggs[i];
                if (egg.Id != creature.FoodContactResourceId)
                {
                    continue;
                }

                var eggScreenPosition = ToScreen(egg.Position);
                var radius = MathF.Max(4f, EggPredation.ContactRadius(egg) * _worldScale + 3f);
                DrawLine(creatureScreenPosition, eggScreenPosition, _selectedColor, width: 1.5f);
                DrawArc(eggScreenPosition, radius, 0f, MathF.Tau, 40, _selectedColor, width: 2f);
                return;
            }
        }

        if (creature.FoodContactKind == FoodContactKind.SmallPrey)
        {
            for (var i = 0; i < _simulation.State.SmallPrey.Count; i++)
            {
                var prey = _simulation.State.SmallPrey[i];
                if (prey.Id != creature.FoodContactResourceId)
                {
                    continue;
                }

                var preyScreenPosition = ToScreen(prey.Position);
                var radius = MathF.Max(4f, prey.Radius * _worldScale + 3f);
                DrawLine(creatureScreenPosition, preyScreenPosition, _selectedColor, width: 1.5f);
                DrawArc(preyScreenPosition, radius, 0f, MathF.Tau, 40, _selectedColor, width: 2f);
                return;
            }
        }
    }

    private void DrawSelectedCreatureContact(CreatureState creature, Vector2 creatureScreenPosition)
    {
        if (!creature.IsTouchingCreature || creature.CreatureContactId == default)
        {
            return;
        }

        for (var i = 0; i < _simulation.State.Creatures.Count; i++)
        {
            var target = _simulation.State.Creatures[i];
            if (target.Id != creature.CreatureContactId)
            {
                continue;
            }

            var targetGenome = _simulation.State.GetGenome(target.GenomeId);
            var targetScreenPosition = ToScreen(target.Position);
            var radius = MathF.Max(4f, CreatureGrowth.EffectiveBodyRadius(target, targetGenome) * _worldScale + 3f);
            var color = creature.LastAttackDamageDealt > 0f
                ? new Color(1f, 0.32f, 0.18f, 0.9f)
                : _selectedColor;
            DrawLine(creatureScreenPosition, targetScreenPosition, color, width: 1.5f);
            DrawArc(targetScreenPosition, radius, 0f, MathF.Tau, 32, color, width: 2f);
            return;
        }
    }

    private string FormatCaloriesPerSecond(float calories)
    {
        return FormatPerSecond(calories);
    }

    private string FormatPerSecond(float value)
    {
        var fixedDelta = MathF.Max(_simulation.Config.FixedDeltaSeconds, 0.0001f);
        return (value / fixedDelta).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string FormatCreatureReference(EntityId id)
    {
        return id == default ? "none" : $"#{id.Value}";
    }

    private string FormatBrainText(int brainId)
    {
        if (brainId < 0)
        {
            return "none";
        }

        var brain = _simulation.State.GetBrain(brainId);
        var architecture = _simulation.State.GetBrainArchitectureKind(brainId);
        return $"{brainId} ({FormatBrainArchitectureKind(architecture)}, {brain.HiddenNodeCount} hidden)";
    }

    private Color ColorForResource(ResourcePatchState resource, float fullness)
    {
        return resource.Kind switch
        {
            ResourceKind.Meat => _meatResourceColor.Lerp(new Color(0.20f, 0.10f, 0.09f), 1f - fullness),
            _ => ColorForPlantKind(resource.PlantKind).Lerp(new Color(0.14f, 0.26f, 0.16f), 1f - fullness)
        };
    }

    private Color ColorForPlantKind(PlantResourceKind plantKind)
    {
        return plantKind switch
        {
            PlantResourceKind.Tender => _tenderPlantColor,
            PlantResourceKind.Rich => _richPlantColor,
            PlantResourceKind.Tough => _toughPlantColor,
            _ => _resourceColor
        };
    }

    private Color PlantAggregateColor(ResourceAggregateSummary summary, float fullness, float alpha)
    {
        var plantCalories = summary.GenericPlantCalories
            + summary.TenderPlantCalories
            + summary.RichPlantCalories
            + summary.ToughPlantCalories;
        if (plantCalories <= 0f)
        {
            return new Color(0.17f + fullness * 0.08f, 0.72f, 0.30f, alpha);
        }

        var color =
            ColorForPlantKind(PlantResourceKind.Generic) * (summary.GenericPlantCalories / plantCalories)
            + ColorForPlantKind(PlantResourceKind.Tender) * (summary.TenderPlantCalories / plantCalories)
            + ColorForPlantKind(PlantResourceKind.Rich) * (summary.RichPlantCalories / plantCalories)
            + ColorForPlantKind(PlantResourceKind.Tough) * (summary.ToughPlantCalories / plantCalories);

        return new Color(
            Mathf.Clamp(color.R + fullness * 0.04f, 0f, 1f),
            Mathf.Clamp(color.G + fullness * 0.04f, 0f, 1f),
            Mathf.Clamp(color.B + fullness * 0.04f, 0f, 1f),
            alpha);
    }

    private void DrawPlantTypeMarker(ResourcePatchState resource, Vector2 screenPosition, float radius, float fullness)
    {
        if (resource.Kind != ResourceKind.Plant || resource.PlantKind == PlantResourceKind.Generic || radius < 3.25f)
        {
            return;
        }

        var markerAlpha = Mathf.Clamp(0.45f + fullness * 0.45f, 0f, 0.95f);
        var markerColor = resource.PlantKind switch
        {
            PlantResourceKind.Rich => new Color(1.0f, 0.95f, 0.45f, markerAlpha),
            PlantResourceKind.Tough => new Color(0.08f, 0.16f, 0.08f, markerAlpha),
            _ => new Color(0.84f, 1.0f, 0.72f, markerAlpha)
        };

        switch (resource.PlantKind)
        {
            case PlantResourceKind.Rich:
                DrawArc(screenPosition, radius + 1f, 0f, MathF.Tau, 20, markerColor, width: 1f);
                break;
            case PlantResourceKind.Tough:
                var diameter = radius * 1.35f;
                DrawRect(
                    new Rect2(screenPosition - new Vector2(diameter * 0.5f, diameter * 0.5f), new Vector2(diameter, diameter)),
                    markerColor,
                    filled: false,
                    width: 1f);
                break;
            case PlantResourceKind.Tender:
                DrawLine(
                    screenPosition + new Vector2(-radius * 0.45f, radius * 0.25f),
                    screenPosition + new Vector2(radius * 0.45f, -radius * 0.25f),
                    markerColor,
                    width: 1f);
                break;
        }
    }

    private static float EggHealthRatio(EggState egg)
    {
        return egg.MaxHealth > 0f
            ? Math.Clamp(egg.Health / egg.MaxHealth, 0f, 1f)
            : 1f;
    }

    private static float EggHatchProgress(EggState egg)
    {
        return egg.IncubationSeconds > 0f
            ? Mathf.Clamp(egg.AgeSeconds / egg.IncubationSeconds, 0f, 1f)
            : 1f;
    }

    private void CycleColorMode()
    {
        var next = (int)_colorMode + 1;
        var count = Enum.GetValues<CreatureColorMode>().Length;
        _colorMode = (CreatureColorMode)(next % count);
    }

    private Color ColorForCreature(CreatureState creature, CreatureGenome genome)
    {
        return _colorMode switch
        {
            CreatureColorMode.FounderLineage => ColorForStableId(ResolveFounderId(creature.Id).Value),
            CreatureColorMode.Generation => ColorForGeneration(creature.Generation, _livingMinGeneration, _livingMaxGeneration),
            CreatureColorMode.Energy => ColorForEnergy(creature.Energy, genome.ReproductionEnergyThreshold),
            CreatureColorMode.Age => ColorForAge(creature.AgeSeconds),
            CreatureColorMode.Off => new Color(0.92f, 0.90f, 0.82f),
            _ => new Color(0.92f, 0.90f, 0.82f)
        };
    }

    private EntityId ResolveFounderId(EntityId creatureId)
    {
        var current = creatureId;
        for (var depth = 0; depth < 512; depth++)
        {
            if (!_simulation.State.TryGetLineageRecord(current, out var record) || record.IsFounder)
            {
                return current;
            }

            current = record.ParentId;
        }

        return creatureId;
    }

    private static Color ColorForStableId(int value)
    {
        var hue = Mathf.PosMod(value * 0.6180339f, 1f);
        return Color.FromHsv(hue, 0.56f, 0.94f);
    }

    private static Color ColorForEnergy(float energy, float reproductionThreshold)
    {
        var reserveRatio = reproductionThreshold > 0f
            ? Mathf.Clamp(energy / reproductionThreshold, 0f, 1.5f)
            : 0f;
        var low = new Color(1.0f, 0.08f, 0.06f);
        var ready = new Color(1.0f, 0.86f, 0.04f);
        var high = new Color(0.08f, 1.0f, 0.24f);
        return reserveRatio <= 1f
            ? low.Lerp(ready, reserveRatio)
            : ready.Lerp(high, (reserveRatio - 1f) / 0.5f);
    }

    private static Color ColorForAge(float ageSeconds)
    {
        var ratio = Mathf.Clamp(ageSeconds / 900f, 0f, 1f);
        var young = new Color(0.08f, 0.92f, 1.0f);
        var middle = new Color(1.0f, 0.88f, 0.06f);
        var old = new Color(1.0f, 0.10f, 0.78f);
        return ratio <= 0.5f
            ? young.Lerp(middle, ratio / 0.5f)
            : middle.Lerp(old, (ratio - 0.5f) / 0.5f);
    }

    private void DrawMapOverlay()
    {
        switch (_mapOverlayMode)
        {
            case MapOverlayMode.Biome:
                DrawBiomeOverlay();
                break;
            case MapOverlayMode.Temperature:
                DrawTemperatureOverlay();
                break;
        }
    }

    private void DrawBiomeOverlay()
    {
        var map = _simulation.State.Biomes;
        DrawMapTexture(GetBiomeOverlayTexture(map), map.CellSize / Math.Max(1, _biomeOverlayPixelsPerCell));
        DrawResourceVoidOverlay(map);

        if (map.CellSize * _worldScale <= 54f)
        {
            return;
        }

        DrawBiomeCellOutlines(map);
    }

    private void DrawTemperatureOverlay()
    {
        var map = _simulation.State.Temperature;
        DrawMapTexture(GetTemperatureOverlayTexture(map), map.CellSize / Math.Max(1, _temperatureOverlayPixelsPerCell));
        DrawResourceVoidOverlay(_simulation.State.Biomes);

        if (map.CellSize * _worldScale <= 54f)
        {
            return;
        }

        DrawTemperatureCellOutlines(map);
    }

    private void DrawResourceVoidOverlay(BiomeMap map)
    {
        var width = map.ResourceVoidBorderWidth;
        if (width <= 0f)
        {
            return;
        }

        var color = new Color(0.02f, 0.025f, 0.055f, 0.42f);
        var middleHeight = MathF.Max(0f, map.Bounds.Height - width * 2f);
        DrawWorldRect(new BiomeCellBounds(0f, 0f, map.Bounds.Width, width), color);
        DrawWorldRect(new BiomeCellBounds(0f, map.Bounds.Height - width, map.Bounds.Width, width), color);
        DrawWorldRect(new BiomeCellBounds(0f, width, width, middleHeight), color);
        DrawWorldRect(new BiomeCellBounds(map.Bounds.Width - width, width, width, middleHeight), color);
    }

    private void DrawMapTexture(Texture2D texture, float worldUnitsPerTexturePixel)
    {
        var visible = GetVisibleWorldRect();
        if (visible.Size.X <= 0f || visible.Size.Y <= 0f || worldUnitsPerTexturePixel <= 0f)
        {
            return;
        }

        var destination = WorldRectToScreenRect(visible);
        var source = new Rect2(
            new Vector2(visible.Position.X / worldUnitsPerTexturePixel, visible.Position.Y / worldUnitsPerTexturePixel),
            new Vector2(visible.Size.X / worldUnitsPerTexturePixel, visible.Size.Y / worldUnitsPerTexturePixel));
        DrawTextureRectRegion(texture, destination, source);
    }

    private ImageTexture GetBiomeOverlayTexture(BiomeMap map)
    {
        if (_biomeOverlayTexture is not null && ReferenceEquals(_biomeOverlaySource, map))
        {
            return _biomeOverlayTexture;
        }

        var pixelsPerCell = CalculateBiomeTexturePixelsPerCell(map);
        var image = Image.CreateEmpty(map.CellCountX * pixelsPerCell, map.CellCountY * pixelsPerCell, false, Image.Format.Rgba8);
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var biome = map.GetKind(x, y);
                for (var localY = 0; localY < pixelsPerCell; localY++)
                {
                    for (var localX = 0; localX < pixelsPerCell; localX++)
                    {
                        image.SetPixel(
                            x * pixelsPerCell + localX,
                            y * pixelsPerCell + localY,
                            ColorForBiomeTexture(biome, x, y, localX, localY, pixelsPerCell));
                    }
                }
            }
        }

        _biomeOverlaySource = map;
        _biomeOverlayPixelsPerCell = pixelsPerCell;
        _biomeOverlayTexture = ImageTexture.CreateFromImage(image);
        return _biomeOverlayTexture;
    }

    private ImageTexture GetTemperatureOverlayTexture(TemperatureMap map)
    {
        if (_temperatureOverlayTexture is not null && ReferenceEquals(_temperatureOverlaySource, map))
        {
            return _temperatureOverlayTexture;
        }

        var pixelsPerCell = CalculateTemperatureTexturePixelsPerCell(map);
        var image = Image.CreateEmpty(map.CellCountX * pixelsPerCell, map.CellCountY * pixelsPerCell, false, Image.Format.Rgba8);
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var temperature = map.GetTemperature(x, y);
                for (var localY = 0; localY < pixelsPerCell; localY++)
                {
                    for (var localX = 0; localX < pixelsPerCell; localX++)
                    {
                        image.SetPixel(
                            x * pixelsPerCell + localX,
                            y * pixelsPerCell + localY,
                            ColorForTemperatureTexture(temperature, x, y, localX, localY, pixelsPerCell));
                    }
                }
            }
        }

        _temperatureOverlaySource = map;
        _temperatureOverlayPixelsPerCell = pixelsPerCell;
        _temperatureOverlayTexture = ImageTexture.CreateFromImage(image);
        return _temperatureOverlayTexture;
    }

    private static int CalculateBiomeTexturePixelsPerCell(BiomeMap map)
    {
        var maxCells = Math.Max(map.CellCountX, map.CellCountY);
        if (maxCells <= 0)
        {
            return 1;
        }

        return Math.Clamp(MaxBiomeTextureDimension / maxCells, 1, PreferredBiomeTexturePixelsPerCell);
    }

    private static int CalculateTemperatureTexturePixelsPerCell(TemperatureMap map)
    {
        var maxCells = Math.Max(map.CellCountX, map.CellCountY);
        if (maxCells <= 0)
        {
            return 1;
        }

        return Math.Clamp(MaxBiomeTextureDimension / maxCells, 1, PreferredBiomeTexturePixelsPerCell);
    }

    private void DrawBiomeCellOutlines(BiomeMap map)
    {
        var range = GetVisibleCellRange(map.CellSize, map.CellCountX, map.CellCountY);
        for (var y = range.MinY; y < range.MaxYExclusive; y++)
        {
            for (var x = range.MinX; x < range.MaxXExclusive; x++)
            {
                var cell = map.GetCellBounds(x, y);
                var topLeft = ToScreen(new SimVector2(cell.X, cell.Y));
                var bottomRight = ToScreen(new SimVector2(cell.X + cell.Width, cell.Y + cell.Height));
                var rect = RectFromPoints(topLeft, bottomRight);
                if (TryClipRect(rect, _worldRect, out var clipped))
                {
                    DrawRect(clipped, new Color(0f, 0f, 0f, 0.045f), filled: false, width: 1f);
                }
            }
        }
    }

    private void DrawTemperatureCellOutlines(TemperatureMap map)
    {
        var range = GetVisibleCellRange(map.CellSize, map.CellCountX, map.CellCountY);
        for (var y = range.MinY; y < range.MaxYExclusive; y++)
        {
            for (var x = range.MinX; x < range.MaxXExclusive; x++)
            {
                var cell = map.GetCellBounds(x, y);
                var topLeft = ToScreen(new SimVector2(cell.X, cell.Y));
                var bottomRight = ToScreen(new SimVector2(cell.X + cell.Width, cell.Y + cell.Height));
                var rect = RectFromPoints(topLeft, bottomRight);
                if (TryClipRect(rect, _worldRect, out var clipped))
                {
                    DrawRect(clipped, new Color(0f, 0f, 0f, 0.05f), filled: false, width: 1f);
                }
            }
        }
    }

    private void DrawObstacleOverlay()
    {
        var map = _simulation.State.Obstacles;
        if (!map.HasObstacles)
        {
            return;
        }

        var range = GetVisibleCellRange(map.CellSize, map.CellCountX, map.CellCountY);
        for (var y = range.MinY; y < range.MaxYExclusive; y++)
        {
            for (var x = range.MinX; x < range.MaxXExclusive; x++)
            {
                if (!map.IsBlocked(x, y))
                {
                    continue;
                }

                var cell = map.GetCellBounds(x, y);
                var topLeft = ToScreen(new SimVector2(cell.X, cell.Y));
                var bottomRight = ToScreen(new SimVector2(cell.X + cell.Width, cell.Y + cell.Height));
                var rect = RectFromPoints(topLeft, bottomRight);
                if (!TryClipRect(rect, _worldRect, out var clipped))
                {
                    continue;
                }

                DrawRect(clipped, _obstacleColor, filled: true);
                if (clipped.Size.X > 8f && clipped.Size.Y > 8f)
                {
                    DrawRect(clipped, new Color(0f, 0f, 0f, 0.45f), filled: false, width: 1f);
                }
            }
        }
    }

    private void DrawWorldRect(BiomeCellBounds worldBounds, Color color)
    {
        if (worldBounds.Width <= 0f || worldBounds.Height <= 0f)
        {
            return;
        }

        var topLeft = ToScreen(new SimVector2(worldBounds.X, worldBounds.Y));
        var bottomRight = ToScreen(new SimVector2(worldBounds.X + worldBounds.Width, worldBounds.Y + worldBounds.Height));
        var rect = RectFromPoints(topLeft, bottomRight);
        if (TryClipRect(rect, _worldRect, out var clipped))
        {
            DrawRect(clipped, color, filled: true);
        }
    }

    private static Rect2 RectFromPoints(Vector2 first, Vector2 second)
    {
        var left = MathF.Min(first.X, second.X);
        var top = MathF.Min(first.Y, second.Y);
        var right = MathF.Max(first.X, second.X);
        var bottom = MathF.Max(first.Y, second.Y);
        return new Rect2(new Vector2(left, top), new Vector2(right - left, bottom - top));
    }

    private Rect2 GetVisibleWorldRect(float paddingWorld = 0f)
    {
        var topLeft = ToWorld(_worldRect.Position);
        var bottomRight = ToWorld(_worldRect.Position + _worldRect.Size);
        var left = MathF.Max(0f, MathF.Min(topLeft.X, bottomRight.X) - paddingWorld);
        var top = MathF.Max(0f, MathF.Min(topLeft.Y, bottomRight.Y) - paddingWorld);
        var right = MathF.Min(_simulation.State.Bounds.Width, MathF.Max(topLeft.X, bottomRight.X) + paddingWorld);
        var bottom = MathF.Min(_simulation.State.Bounds.Height, MathF.Max(topLeft.Y, bottomRight.Y) + paddingWorld);
        return new Rect2(new Vector2(left, top), new Vector2(MathF.Max(0f, right - left), MathF.Max(0f, bottom - top)));
    }

    private Rect2 WorldRectToScreenRect(Rect2 worldRect)
    {
        var topLeft = ToScreen(new SimVector2(worldRect.Position.X, worldRect.Position.Y));
        var bottomRight = ToScreen(new SimVector2(worldRect.Position.X + worldRect.Size.X, worldRect.Position.Y + worldRect.Size.Y));
        return RectFromPoints(topLeft, bottomRight);
    }

    private static bool TryClipWorldRect(Rect2 rect, Rect2 clip, out Rect2 clipped)
    {
        return TryClipRect(rect, clip, out clipped);
    }

    private VisibleCellRange GetVisibleCellRange(float cellSize, int cellCountX, int cellCountY)
    {
        var visible = GetVisibleWorldRect();
        if (visible.Size.X <= 0f || visible.Size.Y <= 0f)
        {
            return new VisibleCellRange(0, 0, 0, 0);
        }

        var left = visible.Position.X;
        var top = visible.Position.Y;
        var right = visible.Position.X + visible.Size.X;
        var bottom = visible.Position.Y + visible.Size.Y;
        var minX = Math.Clamp((int)MathF.Floor(left / cellSize), 0, cellCountX - 1);
        var minY = Math.Clamp((int)MathF.Floor(top / cellSize), 0, cellCountY - 1);
        var maxXExclusive = Math.Clamp((int)MathF.Ceiling(right / cellSize), 0, cellCountX);
        var maxYExclusive = Math.Clamp((int)MathF.Ceiling(bottom / cellSize), 0, cellCountY);
        return new VisibleCellRange(minX, maxXExclusive, minY, maxYExclusive);
    }

    private readonly record struct VisibleCellRange(
        int MinX,
        int MaxXExclusive,
        int MinY,
        int MaxYExclusive);

    private static bool TryClipRect(Rect2 rect, Rect2 clip, out Rect2 clipped)
    {
        var left = MathF.Max(rect.Position.X, clip.Position.X);
        var top = MathF.Max(rect.Position.Y, clip.Position.Y);
        var right = MathF.Min(rect.Position.X + rect.Size.X, clip.Position.X + clip.Size.X);
        var bottom = MathF.Min(rect.Position.Y + rect.Size.Y, clip.Position.Y + clip.Size.Y);
        if (right <= left || bottom <= top)
        {
            clipped = default;
            return false;
        }

        clipped = new Rect2(new Vector2(left, top), new Vector2(right - left, bottom - top));
        return true;
    }

    private static bool WorldRectIntersectsCircle(Rect2 rect, SimVector2 center, float radius)
    {
        var closestX = Math.Clamp(center.X, rect.Position.X, rect.Position.X + rect.Size.X);
        var closestY = Math.Clamp(center.Y, rect.Position.Y, rect.Position.Y + rect.Size.Y);
        var deltaX = center.X - closestX;
        var deltaY = center.Y - closestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }

    private static Color ColorForBiome(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => new Color(0.72f, 0.65f, 0.42f, 0.60f),
            BiomeKind.Forest => new Color(0.00f, 0.20f, 0.08f, 0.64f),
            BiomeKind.Wetland => new Color(0.00f, 0.40f, 0.58f, 0.58f),
            BiomeKind.Scrubland => new Color(0.52f, 0.50f, 0.24f, 0.44f),
            BiomeKind.Fertile => new Color(0.08f, 0.54f, 0.20f, 0.44f),
            BiomeKind.Tundra => new Color(0.62f, 0.70f, 0.74f, 0.44f),
            BiomeKind.Highland => new Color(0.50f, 0.45f, 0.36f, 0.44f),
            _ => new Color(0.24f, 0.50f, 0.18f, 0.50f)
        };
    }

    private static Color ColorForTemperature(float temperature)
    {
        var value = Math.Clamp(temperature, 0f, 1f);
        var cold = new Color(0.18f, 0.34f, 0.88f, 0.58f);
        var cool = new Color(0.10f, 0.58f, 0.72f, 0.54f);
        var mild = new Color(0.26f, 0.63f, 0.28f, 0.48f);
        var warm = new Color(0.90f, 0.68f, 0.20f, 0.54f);
        var hot = new Color(0.84f, 0.20f, 0.12f, 0.60f);

        if (value < 0.30f)
        {
            return cold.Lerp(cool, value / 0.30f);
        }

        if (value < 0.55f)
        {
            return cool.Lerp(mild, (value - 0.30f) / 0.25f);
        }

        if (value < 0.75f)
        {
            return mild.Lerp(warm, (value - 0.55f) / 0.20f);
        }

        return warm.Lerp(hot, (value - 0.75f) / 0.25f);
    }

    private static Color ColorForTemperatureTexture(
        float temperature,
        int cellX,
        int cellY,
        int localX,
        int localY,
        int pixelsPerCell)
    {
        var baseColor = ColorForTemperature(temperature);
        if (pixelsPerCell <= 1)
        {
            return baseColor;
        }

        var fine = Hash01(cellX * pixelsPerCell + localX, cellY * pixelsPerCell + localY, 0x4f1bbcddu);
        var coarse = Hash01(cellX * 3 + localX / 5, cellY * 3 + localY / 5, 0x8ac5f293u);
        var variation = (fine - 0.5f) * 0.025f + (coarse - 0.5f) * 0.030f;
        var color = AdjustColorValue(baseColor, variation);
        return new Color(color.R, color.G, color.B, baseColor.A);
    }

    private static Color ColorForBiomeTexture(
        BiomeKind biome,
        int cellX,
        int cellY,
        int localX,
        int localY,
        int pixelsPerCell)
    {
        var canonical = BiomeKinds.Canonicalize(biome);
        var baseColor = ColorForBiome(canonical);
        if (pixelsPerCell <= 1)
        {
            return baseColor;
        }

        if (canonical == BiomeKind.Forest)
        {
            return ColorForForestBiomeTexture(cellX, cellY, localX, localY, pixelsPerCell, baseColor);
        }

        var u = (localX + 0.5f) / pixelsPerCell;
        var v = (localY + 0.5f) / pixelsPerCell;
        var cellHash = Hash01(cellX, cellY, 0x9e3779b9u + (uint)canonical * 101u);
        var fine = Hash01(cellX * pixelsPerCell + localX, cellY * pixelsPerCell + localY, 0x85ebca6bu);
        var coarse = Hash01(cellX * 5 + localX / 4, cellY * 5 + localY / 4, 0xc2b2ae35u + (uint)canonical * 17u);
        var angle = cellHash * MathF.Tau;
        var waveAxis = u * MathF.Cos(angle) + v * MathF.Sin(angle);
        var wave = MathF.Sin((waveAxis * (2.5f + cellHash * 2.0f) + cellHash) * MathF.Tau);
        var variation = (fine - 0.5f) * 0.045f
            + (coarse - 0.5f) * 0.055f
            + wave * BiomeTextureWaveStrength(canonical);

        var edge = MathF.Min(MathF.Min(u, 1f - u), MathF.Min(v, 1f - v));
        if (edge < 0.08f)
        {
            variation -= (0.08f - edge) * 0.28f;
        }

        var color = AdjustColorValue(baseColor, variation);
        var fleck = BiomeTextureFleckAmount(canonical, fine, coarse, u, v, cellHash);
        if (fleck > 0f)
        {
            color = color.Lerp(BiomeTextureAccentColor(canonical, baseColor), fleck);
        }

        return new Color(color.R, color.G, color.B, baseColor.A);
    }

    private static Color ColorForForestBiomeTexture(
        int cellX,
        int cellY,
        int localX,
        int localY,
        int pixelsPerCell,
        Color baseColor)
    {
        var u = (localX + 0.5f) / pixelsPerCell;
        var v = (localY + 0.5f) / pixelsPerCell;
        var cellHash = Hash01(cellX, cellY, 0x46a3f17du);
        var variant = Math.Min(5, (int)(cellHash * 6f));
        var fine = Hash01(cellX * pixelsPerCell + localX, cellY * pixelsPerCell + localY, 0x4cf5ad43u);
        var coarse = Hash01(cellX * 7 + localX / 3, cellY * 7 + localY / 3, 0x7f4a7c15u);
        var color = AdjustColorValue(baseColor, (fine - 0.5f) * 0.035f + (coarse - 0.5f) * 0.045f);

        color = variant switch
        {
            0 => ApplyForestSparseCanopyDots(color, cellX, cellY, u, v, baseColor),
            1 => ApplyForestClusteredLeafBlobs(color, cellX, cellY, u, v, baseColor),
            2 => ApplyForestBranchStrokes(color, cellX, cellY, u, v, baseColor, fine),
            3 => ApplyForestMossyPatches(color, cellX, cellY, u, v, baseColor, fine, coarse),
            4 => ApplyForestDenseCenterCanopy(color, cellX, cellY, u, v, baseColor, fine),
            _ => ApplyForestEdgeCanopy(color, cellX, cellY, u, v, baseColor, fine)
        };

        if (fine > 0.91f)
        {
            color = color.Lerp(new Color(0.12f, 0.46f, 0.16f, baseColor.A), 0.16f);
        }

        return new Color(color.R, color.G, color.B, baseColor.A);
    }

    private static Color ApplyForestSparseCanopyDots(Color color, int cellX, int cellY, float u, float v, Color baseColor)
    {
        for (var i = 0; i < 5; i++)
        {
            var cx = Hash01(cellX, cellY, 0x1001u + (uint)i * 73u) * 0.82f + 0.09f;
            var cy = Hash01(cellX, cellY, 0x2003u + (uint)i * 97u) * 0.82f + 0.09f;
            var radius = 0.055f + Hash01(cellX, cellY, 0x3005u + (uint)i * 37u) * 0.045f;
            var distance = Distance01(u, v, cx, cy);
            if (distance < radius)
            {
                var amount = 1f - distance / radius;
                color = color.Lerp(new Color(0.00f, 0.12f, 0.045f, baseColor.A), 0.18f + amount * 0.20f);
            }
        }

        return color;
    }

    private static Color ApplyForestClusteredLeafBlobs(Color color, int cellX, int cellY, float u, float v, Color baseColor)
    {
        for (var i = 0; i < 3; i++)
        {
            var cx = Hash01(cellX, cellY, 0x4211u + (uint)i * 107u) * 0.62f + 0.19f;
            var cy = Hash01(cellX, cellY, 0x5321u + (uint)i * 83u) * 0.62f + 0.19f;
            var radius = 0.13f + Hash01(cellX, cellY, 0x6431u + (uint)i * 53u) * 0.08f;
            var distance = Distance01(u, v, cx, cy);
            if (distance < radius)
            {
                var amount = 1f - distance / radius;
                var blobColor = i % 2 == 0
                    ? new Color(0.03f, 0.30f, 0.10f, baseColor.A)
                    : new Color(0.08f, 0.38f, 0.13f, baseColor.A);
                color = color.Lerp(blobColor, 0.18f + amount * 0.22f);
            }
        }

        return color;
    }

    private static Color ApplyForestBranchStrokes(Color color, int cellX, int cellY, float u, float v, Color baseColor, float fine)
    {
        var branchColor = new Color(0.18f, 0.105f, 0.045f, baseColor.A);
        var mainAngle = Hash01(cellX, cellY, 0x7801u) * MathF.Tau;
        var mainAxis = SignedLineDistance(u, v, 0.5f, 0.5f, mainAngle);
        var mainAlong = AlongLineDistance(u, v, 0.5f, 0.5f, mainAngle);
        if (MathF.Abs(mainAxis) < 0.035f && MathF.Abs(mainAlong) < 0.42f)
        {
            color = color.Lerp(branchColor, 0.26f);
        }

        for (var i = 0; i < 2; i++)
        {
            var bx = 0.36f + i * 0.22f;
            var by = 0.36f + Hash01(cellX, cellY, 0x7b21u + (uint)i * 29u) * 0.28f;
            var angle = mainAngle + (i == 0 ? 0.72f : -0.82f);
            var axis = SignedLineDistance(u, v, bx, by, angle);
            var along = AlongLineDistance(u, v, bx, by, angle);
            if (MathF.Abs(axis) < 0.028f && along is > 0f and < 0.25f)
            {
                color = color.Lerp(branchColor, 0.22f);
            }
        }

        if (fine > 0.84f)
        {
            color = color.Lerp(new Color(0.07f, 0.34f, 0.12f, baseColor.A), 0.14f);
        }

        return color;
    }

    private static Color ApplyForestMossyPatches(Color color, int cellX, int cellY, float u, float v, Color baseColor, float fine, float coarse)
    {
        var moss = new Color(0.10f, 0.42f, 0.11f, baseColor.A);
        for (var i = 0; i < 4; i++)
        {
            var cx = Hash01(cellX, cellY, 0x8141u + (uint)i * 47u) * 0.76f + 0.12f;
            var cy = Hash01(cellX, cellY, 0x9151u + (uint)i * 61u) * 0.76f + 0.12f;
            var radius = 0.11f + Hash01(cellX, cellY, 0xa161u + (uint)i * 43u) * 0.09f;
            var distance = Distance01(u, v, cx, cy);
            if (distance < radius && coarse > 0.40f)
            {
                color = color.Lerp(moss, 0.14f + (1f - distance / radius) * 0.18f);
            }
        }

        if (fine < 0.12f)
        {
            color = color.Lerp(new Color(0.00f, 0.14f, 0.05f, baseColor.A), 0.14f);
        }

        return color;
    }

    private static Color ApplyForestDenseCenterCanopy(Color color, int cellX, int cellY, float u, float v, Color baseColor, float fine)
    {
        var distance = Distance01(u, v, 0.5f, 0.5f);
        var canopy = Math.Clamp(1f - distance / 0.52f, 0f, 1f);
        color = color.Lerp(new Color(0.00f, 0.13f, 0.045f, baseColor.A), canopy * 0.34f);

        for (var i = 0; i < 4; i++)
        {
            var angle = Hash01(cellX, cellY, 0xb181u + (uint)i * 71u) * MathF.Tau;
            var radius = 0.10f + Hash01(cellX, cellY, 0xc191u + (uint)i * 37u) * 0.24f;
            var cx = 0.5f + MathF.Cos(angle) * radius;
            var cy = 0.5f + MathF.Sin(angle) * radius;
            var leafDistance = Distance01(u, v, cx, cy);
            if (leafDistance < 0.09f)
            {
                color = color.Lerp(new Color(0.06f, 0.35f, 0.12f, baseColor.A), 0.18f);
            }
        }

        if (fine > 0.82f)
        {
            color = color.Lerp(new Color(0.12f, 0.48f, 0.16f, baseColor.A), 0.12f);
        }

        return color;
    }

    private static Color ApplyForestEdgeCanopy(Color color, int cellX, int cellY, float u, float v, Color baseColor, float fine)
    {
        var edge = MathF.Min(MathF.Min(u, 1f - u), MathF.Min(v, 1f - v));
        var edgeAmount = Math.Clamp(1f - edge / 0.28f, 0f, 1f);
        color = color.Lerp(new Color(0.00f, 0.12f, 0.045f, baseColor.A), edgeAmount * 0.34f);

        for (var i = 0; i < 5; i++)
        {
            var side = (int)(Hash01(cellX, cellY, 0xd1a1u + (uint)i * 43u) * 4f);
            var along = Hash01(cellX, cellY, 0xe1b1u + (uint)i * 59u) * 0.86f + 0.07f;
            var cx = side switch
            {
                0 => 0.10f,
                1 => 0.90f,
                _ => along
            };
            var cy = side switch
            {
                2 => 0.10f,
                3 => 0.90f,
                _ => along
            };
            if (Distance01(u, v, cx, cy) < 0.085f)
            {
                color = color.Lerp(new Color(0.08f, 0.36f, 0.13f, baseColor.A), 0.18f);
            }
        }

        if (fine > 0.88f)
        {
            color = color.Lerp(new Color(0.12f, 0.42f, 0.14f, baseColor.A), 0.12f);
        }

        return color;
    }

    private static float BiomeTextureWaveStrength(BiomeKind biome)
    {
        return biome switch
        {
            BiomeKind.Desert => 0.055f,
            BiomeKind.Wetland => 0.050f,
            BiomeKind.Tundra => 0.038f,
            BiomeKind.Highland => 0.042f,
            _ => 0.032f
        };
    }

    private static float BiomeTextureFleckAmount(BiomeKind biome, float fine, float coarse, float u, float v, float cellHash)
    {
        var diagonal = MathF.Abs(((u + v + cellHash) % 1f) - 0.5f);
        return biome switch
        {
            BiomeKind.Desert => fine > 0.90f ? 0.20f : 0f,
            BiomeKind.Scrubland => fine > 0.86f || diagonal < 0.018f && coarse > 0.72f ? 0.18f : 0f,
            BiomeKind.Grassland => fine > 0.88f ? 0.16f : 0f,
            BiomeKind.Fertile => fine > 0.82f ? 0.20f : 0f,
            BiomeKind.Forest => fine > 0.80f || coarse > 0.88f ? 0.18f : 0f,
            BiomeKind.Wetland => diagonal < 0.026f && coarse > 0.58f ? 0.22f : fine > 0.93f ? 0.14f : 0f,
            BiomeKind.Tundra => fine > 0.87f ? 0.18f : 0f,
            BiomeKind.Highland => fine > 0.84f ? 0.20f : 0f,
            _ => fine > 0.88f ? 0.14f : 0f
        };
    }

    private static Color BiomeTextureAccentColor(BiomeKind biome, Color baseColor)
    {
        return biome switch
        {
            BiomeKind.Desert => new Color(0.96f, 0.82f, 0.48f, baseColor.A),
            BiomeKind.Scrubland => new Color(0.74f, 0.70f, 0.36f, baseColor.A),
            BiomeKind.Grassland => new Color(0.34f, 0.64f, 0.22f, baseColor.A),
            BiomeKind.Fertile => new Color(0.12f, 0.72f, 0.26f, baseColor.A),
            BiomeKind.Forest => new Color(0.02f, 0.32f, 0.11f, baseColor.A),
            BiomeKind.Wetland => new Color(0.06f, 0.58f, 0.70f, baseColor.A),
            BiomeKind.Tundra => new Color(0.78f, 0.86f, 0.88f, baseColor.A),
            BiomeKind.Highland => new Color(0.68f, 0.62f, 0.48f, baseColor.A),
            _ => new Color(0.34f, 0.62f, 0.22f, baseColor.A)
        };
    }

    private static Color AdjustColorValue(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp(color.R + amount, 0f, 1f),
            Mathf.Clamp(color.G + amount, 0f, 1f),
            Mathf.Clamp(color.B + amount, 0f, 1f),
            color.A);
    }

    private static float Distance01(float x, float y, float centerX, float centerY)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float SignedLineDistance(float x, float y, float centerX, float centerY, float angle)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        var normalX = -MathF.Sin(angle);
        var normalY = MathF.Cos(angle);
        return dx * normalX + dy * normalY;
    }

    private static float AlongLineDistance(float x, float y, float centerX, float centerY, float angle)
    {
        var dx = x - centerX;
        var dy = y - centerY;
        return dx * MathF.Cos(angle) + dy * MathF.Sin(angle);
    }

    private static float Hash01(int x, int y, uint salt)
    {
        unchecked
        {
            var hash = (uint)x * 374761393u + (uint)y * 668265263u + salt;
            hash = (hash ^ (hash >> 13)) * 1274126177u;
            hash ^= hash >> 16;
            return (hash & 0x00ffffffu) / 16777215f;
        }
    }

    private void DrawStatsGraph()
    {
        var instructionsRect = new Rect2(_graphLegend.Position - new Vector2(6f, 4f), _graphLegend.Size + new Vector2(12f, 8f));
        DrawRect(instructionsRect, new Color(0.035f, 0.04f, 0.038f), filled: true);
        DrawRect(instructionsRect, new Color(0.18f, 0.21f, 0.19f), filled: false, width: 1f);

        DrawMiniGraph(0, GraphMetric.Population, _graphPopulationColor);
        DrawMiniGraph(1, GraphMetric.ResourceCalories, _graphResourceColor);
        DrawMiniGraph(2, GraphMetric.Deaths, _graphDeathColor);
        DrawMiniGraph(3, GraphMetric.Season, _graphSeasonColor);
    }

    private void DrawMiniGraph(int graphIndex, GraphMetric metric, Color color)
    {
        var graphRect = _miniGraphRects[graphIndex];
        if (graphRect.Size.X <= 0f || graphRect.Size.Y <= 0f)
        {
            return;
        }

        DrawRect(graphRect, new Color(0.018f, 0.022f, 0.020f), filled: true);
        DrawRect(graphRect, new Color(color.R, color.G, color.B, 0.82f), filled: false, width: 2f);

        var plotRect = GraphPlotRect(graphRect);
        for (var i = 1; i < 4; i++)
        {
            var y = plotRect.Position.Y + plotRect.Size.Y * i / 4f;
            DrawLine(new Vector2(plotRect.Position.X, y), new Vector2(plotRect.End.X, y), new Color(0.16f, 0.18f, 0.17f, 0.75f), width: 1f);
        }

        for (var i = 1; i < 6; i++)
        {
            var x = plotRect.Position.X + plotRect.Size.X * i / 6f;
            DrawLine(new Vector2(x, plotRect.Position.Y), new Vector2(x, plotRect.End.Y), new Color(0.11f, 0.13f, 0.12f, 0.7f), width: 1f);
        }

        var snapshots = _simulation.State.Stats.Snapshots;
        if (snapshots.Count < 2)
        {
            return;
        }

        var sampleCount = GetGraphSampleCount(metric, snapshots.Count);
        var startIndex = snapshots.Count - sampleCount;
        var maxValue = GetGraphMaxValue(startIndex, snapshots.Count, metric);
        DrawGraphSeries(plotRect, startIndex, sampleCount, maxValue, color, metric);
    }

    private static Rect2 GraphPlotRect(Rect2 graphRect)
    {
        return new Rect2(graphRect.Position + new Vector2(10f, 30f), graphRect.Size - new Vector2(20f, 40f));
    }

    private float GetGraphMaxValue(int startIndex, int endIndex, GraphMetric metric)
    {
        var maxValue = metric == GraphMetric.Season
            ? MathF.Max(1f, 1f + _scenario.SeasonFertilityAmplitude)
            : 1f;
        var snapshots = _simulation.State.Stats.Snapshots;
        for (var i = startIndex; i < endIndex; i++)
        {
            maxValue = MathF.Max(maxValue, GetGraphMetricValue(snapshots[i], i, metric));
        }

        return maxValue;
    }

    private void DrawGraphSeries(Rect2 plotRect, int startIndex, int sampleCount, float maxValue, Color color, GraphMetric metric)
    {
        if (sampleCount < 2 || maxValue <= 0f)
        {
            return;
        }

        var previous = GetGraphPoint(plotRect, startIndex, startIndex, sampleCount, maxValue, metric);
        for (var sample = 1; sample < sampleCount; sample++)
        {
            var index = startIndex + sample;
            var current = GetGraphPoint(plotRect, index, startIndex, sampleCount, maxValue, metric);
            DrawLine(previous, current, color, width: 2f);
            previous = current;
        }
    }

    private Vector2 GetGraphPoint(Rect2 plotRect, int snapshotIndex, int startIndex, int sampleCount, float maxValue, GraphMetric metric)
    {
        var snapshot = _simulation.State.Stats.Snapshots[snapshotIndex];
        var sampleOffset = snapshotIndex - startIndex;
        var xRatio = sampleCount <= 1 ? 0f : sampleOffset / (float)(sampleCount - 1);
        var value = GetGraphMetricValue(snapshot, snapshotIndex, metric);

        var yRatio = Math.Clamp(value / maxValue, 0f, 1f);
        return new Vector2(
            plotRect.Position.X + xRatio * plotRect.Size.X,
            plotRect.Position.Y + plotRect.Size.Y - yRatio * plotRect.Size.Y);
    }

    private float GetGraphMetricValue(SimulationStatsSnapshot snapshot, int snapshotIndex, GraphMetric metric)
    {
        return metric switch
        {
            GraphMetric.Population => snapshot.CreatureCount,
            GraphMetric.ResourceCalories => snapshot.TotalResourceCalories,
            GraphMetric.Deaths => GetDeathRatePerSecond(snapshot, snapshotIndex),
            GraphMetric.Season => _scenario.EnableSeasons ? snapshot.SeasonFertilityMultiplier : 1f,
            _ => 0f
        };
    }

    private float GetDeathRatePerSecond(SimulationStatsSnapshot snapshot, int snapshotIndex)
    {
        var snapshots = _simulation.State.Stats.Snapshots;
        if (snapshotIndex <= 0 || snapshotIndex >= snapshots.Count)
        {
            return 0f;
        }

        var windowSamples = GetDeathRateSmoothingSampleCount(snapshots.Count);
        var previousIndex = Math.Max(0, snapshotIndex - windowSamples);
        var previous = snapshots[previousIndex];
        var elapsedSeconds = Math.Max(0.0001, snapshot.ElapsedSeconds - previous.ElapsedSeconds);
        var deaths = Math.Max(0, snapshot.CreatureDeathCount - previous.CreatureDeathCount);
        return (float)(deaths / elapsedSeconds);
    }

    private int GetDeathRateSmoothingSampleCount(int availableSnapshotCount)
    {
        if (availableSnapshotCount < 2)
        {
            return 1;
        }

        var graphSamples = GetGraphSampleCount(GraphMetric.Deaths, availableSnapshotCount);
        var smoothingSamples = (int)MathF.Round(graphSamples * DeathRateSmoothingGraphShare);
        return Math.Clamp(
            smoothingSamples,
            Math.Min(MinDeathRateSmoothingSamples, availableSnapshotCount - 1),
            Math.Min(MaxDeathRateSmoothingSamples, availableSnapshotCount - 1));
    }

    private void SelectEntityAt(Vector2 screenPosition)
    {
        if (!_worldRect.HasPoint(screenPosition))
        {
            ClearSelection();
            return;
        }

        UpdateCreatureRenderCache(force: true);
        var worldPosition = ToWorld(screenPosition);
        var bestCreatureId = default(EntityId);
        var bestEggId = default(EntityId);
        var bestDistanceSquared = float.PositiveInfinity;
        var selectionRadiusWorld = 12f / _worldScale;
        var selectionRect = new Rect2(
            new Vector2(worldPosition.X - selectionRadiusWorld, worldPosition.Y - selectionRadiusWorld),
            new Vector2(selectionRadiusWorld * 2f, selectionRadiusWorld * 2f));

        foreach (var chunk in _creatureRenderCache.VisibleChunks(selectionRect))
        {
            if (chunk.CreatureIndices is null)
            {
                continue;
            }

            foreach (var creatureIndex in chunk.CreatureIndices)
            {
                var creature = _simulation.State.Creatures[creatureIndex];
                var distanceSquared = SimVector2.DistanceSquared(creature.Position, worldPosition);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestCreatureId = creature.Id;
                    bestEggId = default;
                }
            }
        }

        for (var i = 0; i < _simulation.State.Eggs.Count; i++)
        {
            var egg = _simulation.State.Eggs[i];
            if (!selectionRect.HasPoint(new Vector2(egg.Position.X, egg.Position.Y)))
            {
                continue;
            }

            var distanceSquared = SimVector2.DistanceSquared(egg.Position, worldPosition);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestCreatureId = default;
                bestEggId = egg.Id;
            }
        }

        if (bestDistanceSquared > selectionRadiusWorld * selectionRadiusWorld)
        {
            ClearSelection();
            _followSelected = false;
            return;
        }

        _selectedCreatureId = bestCreatureId;
        _selectedEggId = bestEggId;

        if (_selectedCreatureId == default)
        {
            _followSelected = false;
        }
        else if (_followSelected && TryGetSelectedCreature(out var selected))
        {
            FocusCreature(selected, FollowVisibleWorldWidth);
        }
    }

    private void ResetView()
    {
        _followSelected = false;
        _viewZoom = 1f;
        _viewCenter = new SimVector2(_simulation.State.Bounds.Width * 0.5f, _simulation.State.Bounds.Height * 0.5f);
        ClampViewCenter();
    }

    private void ToggleFollowSelected()
    {
        if (_followSelected)
        {
            _followSelected = false;
            _scenarioEditor.SetStatus("Follow released.");
            return;
        }

        if (!TryGetSelectedCreature(out var selected))
        {
            _scenarioEditor.SetStatus(_selectedEggId != default
                ? "Eggs cannot be followed. Select a creature first, then press G."
                : "Select a creature first, then press G to follow it.");
            return;
        }

        _followSelected = true;
        FocusCreature(selected, FollowVisibleWorldWidth);
        _scenarioEditor.SetStatus($"Following creature #{selected.Id.Value}. Pan, press F, or press G again to release.");
    }

    private void UpdateFollowCamera()
    {
        if (!_followSelected)
        {
            return;
        }

        if (!TryGetSelectedCreature(out var selected))
        {
            _followSelected = false;
            return;
        }

        _viewCenter = selected.Position;
        ClampViewCenter();
    }

    private void FocusCreature(CreatureState creature, float visibleWorldWidth)
    {
        _viewCenter = creature.Position;

        if (_fitWorldScale > 0f && _worldRect.Size.X > 0f)
        {
            var requestedZoom = _worldRect.Size.X / (visibleWorldWidth * _fitWorldScale);
            _viewZoom = Math.Clamp(requestedZoom, MinZoom, MaxZoom);
            _worldScale = _fitWorldScale * _viewZoom;
        }

        ClampViewCenter();
    }

    private bool TryGetSelectedCreature(out CreatureState selected)
    {
        return TryGetCreature(_selectedCreatureId, out selected);
    }

    private bool TryGetCreature(EntityId id, out CreatureState selected)
    {
        for (var i = 0; i < _simulation.State.Creatures.Count; i++)
        {
            if (_simulation.State.Creatures[i].Id == id)
            {
                selected = _simulation.State.Creatures[i];
                return true;
            }
        }

        selected = default;
        return false;
    }

    private bool TryGetSelectedEgg(out EggState selected)
    {
        for (var i = 0; i < _simulation.State.Eggs.Count; i++)
        {
            if (_simulation.State.Eggs[i].Id == _selectedEggId)
            {
                selected = _simulation.State.Eggs[i];
                return true;
            }
        }

        selected = default;
        return false;
    }

    private void ClearSelection()
    {
        _selectedCreatureId = default;
        _selectedEggId = default;
    }

    private void HandleKeyboardCamera(float deltaSeconds)
    {
        if (IsCameraInputSuppressed())
        {
            return;
        }

        var pan = Vector2.Zero;
        if (Input.IsKeyPressed(Key.Left))
        {
            pan.X -= 1f;
        }

        if (Input.IsKeyPressed(Key.Right))
        {
            pan.X += 1f;
        }

        if (Input.IsKeyPressed(Key.Up))
        {
            pan.Y -= 1f;
        }

        if (Input.IsKeyPressed(Key.Down))
        {
            pan.Y += 1f;
        }

        if (pan == Vector2.Zero)
        {
            return;
        }

        var speedMultiplier = Input.IsKeyPressed(Key.Shift) ? 3f : 1f;
        _followSelected = false;
        _viewCenter += ToWorldDelta(pan.Normalized() * KeyboardPanPixelsPerSecond * speedMultiplier * deltaSeconds);
        ClampViewCenter();
    }

    private bool IsCameraInputSuppressed()
    {
        if (_loadScenarioDialog.Visible || _saveScenarioDialog.Visible || _loadSnapshotDialog.Visible)
        {
            return true;
        }

        var focusOwner = GetViewport().GuiGetFocusOwner();
        return focusOwner is LineEdit or SpinBox or TextEdit or OptionButton;
    }

    private bool IsTextInputFocused()
    {
        var focusOwner = GetViewport().GuiGetFocusOwner();
        return focusOwner is LineEdit or SpinBox or TextEdit;
    }

    private void ZoomAt(Vector2 screenPosition, float factor)
    {
        if (!_worldRect.HasPoint(screenPosition))
        {
            return;
        }

        var worldBeforeZoom = ToWorld(screenPosition);
        _viewZoom = Math.Clamp(_viewZoom * factor, MinZoom, MaxZoom);
        _worldScale = _fitWorldScale * _viewZoom;
        var worldAfterZoom = ToWorld(screenPosition);
        _viewCenter += worldBeforeZoom - worldAfterZoom;
        ClampViewCenter();
    }

    private void ClampViewCenter()
    {
        if (_simulation is null || _worldScale <= 0f || _worldRect.Size == Vector2.Zero)
        {
            return;
        }

        var halfVisibleWidth = _worldRect.Size.X / _worldScale * 0.5f;
        var halfVisibleHeight = _worldRect.Size.Y / _worldScale * 0.5f;
        _viewCenter = new SimVector2(
            ClampCenterAxis(_viewCenter.X, halfVisibleWidth, _simulation.State.Bounds.Width),
            ClampCenterAxis(_viewCenter.Y, halfVisibleHeight, _simulation.State.Bounds.Height));
    }

    private static float ClampCenterAxis(float value, float halfVisibleSize, float worldSize)
    {
        if (halfVisibleSize >= worldSize * 0.5f)
        {
            return worldSize * 0.5f;
        }

        return Math.Clamp(value, halfVisibleSize, worldSize - halfVisibleSize);
    }

    private SimVector2 ToWorldDelta(Vector2 screenDelta)
    {
        return new SimVector2(screenDelta.X / _worldScale, screenDelta.Y / _worldScale);
    }

    private void UpdateScaleBarLayout()
    {
        if (!_renderMap || _worldScale <= 0f || _worldRect.Size.X <= 0f)
        {
            _scaleBarLabel.Visible = false;
            _scaleBarRect = default;
            return;
        }

        var targetPixels = Math.Clamp(_worldRect.Size.X * 0.16f, 80f, 180f);
        _scaleBarUnits = NiceScaleDistance(targetPixels / _worldScale);
        var pixelLength = MathF.Max(24f, _scaleBarUnits * _worldScale);
        var origin = new Vector2(
            _worldRect.Position.X + _worldRect.Size.X - pixelLength - 18f,
            _worldRect.Position.Y + _worldRect.Size.Y - 22f);

        _scaleBarRect = new Rect2(origin, new Vector2(pixelLength, 10f));
        _scaleBarLabel.Visible = true;
        _scaleBarLabel.Text = FormatWorldDistance(_scaleBarUnits);
        _scaleBarLabel.Position = origin + new Vector2(-18f, -30f);
        _scaleBarLabel.Size = new Vector2(pixelLength + 36f, 24f);
    }

    private void DrawScaleBar()
    {
        if (_scaleBarRect.Size.X <= 0f)
        {
            return;
        }

        var left = _scaleBarRect.Position + new Vector2(0f, _scaleBarRect.Size.Y);
        var right = left + new Vector2(_scaleBarRect.Size.X, 0f);
        var tick = new Vector2(0f, -_scaleBarRect.Size.Y);
        var shadow = new Color(0f, 0f, 0f, 0.65f);
        var color = new Color(0.9f, 0.92f, 0.88f);

        DrawLine(left + new Vector2(0f, 1f), right + new Vector2(0f, 1f), shadow, width: 5f);
        DrawLine(left, right, color, width: 2f);
        DrawLine(left, left + tick, color, width: 2f);
        DrawLine(right, right + tick, color, width: 2f);
    }

    private static float NiceScaleDistance(float rawUnits)
    {
        if (!float.IsFinite(rawUnits) || rawUnits <= 0f)
        {
            return 1f;
        }

        var exponent = MathF.Floor(MathF.Log10(rawUnits));
        var unit = MathF.Pow(10f, exponent);
        var normalized = rawUnits / unit;
        var nice = normalized <= 1f
            ? 1f
            : normalized <= 2f
                ? 2f
                : normalized <= 5f
                    ? 5f
                    : 10f;

        return nice * unit;
    }

    private static string FormatWorldDistance(float units)
    {
        return units >= 10f
            ? $"{units:0}u"
            : $"{units:0.#}u";
    }

    private bool IsVisibleInWorldRect(Vector2 screenPosition, float padding)
    {
        return screenPosition.X >= _worldRect.Position.X - padding
            && screenPosition.Y >= _worldRect.Position.Y - padding
            && screenPosition.X <= _worldRect.Position.X + _worldRect.Size.X + padding
            && screenPosition.Y <= _worldRect.Position.Y + _worldRect.Size.Y + padding;
    }

    private Vector2 ToScreen(SimVector2 worldPosition)
    {
        return _worldRect.Position + _worldRect.Size * 0.5f + ToGodot(worldPosition - _viewCenter) * _worldScale;
    }

    private SimVector2 ToWorld(Vector2 screenPosition)
    {
        var local = (screenPosition - (_worldRect.Position + _worldRect.Size * 0.5f)) / _worldScale;
        return _viewCenter + new SimVector2(local.X, local.Y);
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    private static ulong CreateNewSeed()
    {
        return (ulong)DateTime.UtcNow.Ticks;
    }

    private void LoadStartupScenario()
    {
        var path = System.IO.Path.Combine(GetRepositoryRoot(), "scenarios", StartupScenarioFileName);
        if (!System.IO.File.Exists(path))
        {
            return;
        }

        _scenario = SimulationScenarioJson.Load(path);
        _currentScenarioPath = path;
    }

    private static Label CreateLabel(Vector2 position, Color color)
    {
        var label = new Label
        {
            Position = position,
            Size = new Vector2(280f, 400f)
        };
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private void CreateScenarioLauncher()
    {
        _scenarioEditor = new ScenarioEditorPanel
        {
            Position = new Vector2(12f, 12f),
            Size = new Vector2(LauncherPanelWidth, 680f)
        };
        _scenarioEditor.LaunchRequested += LaunchScenarioFromEditor;
        _scenarioEditor.LoadRequested += OpenLoadScenarioDialog;
        _scenarioEditor.SaveRequested += SaveScenario;
        _scenarioEditor.SaveAsRequested += OpenSaveScenarioDialog;
        _scenarioEditor.CliRunRequested += RunCliFromEditor;
        _scenarioEditor.ReportRequested += WriteCurrentReportFromEditor;
        _scenarioEditor.OpenReportRequested += OpenReportInBrowser;
        _scenarioEditor.LoadSnapshotFileRequested += OpenLoadSnapshotDialog;
        _scenarioEditor.LoadCheckpointFileRequested += OpenLoadCheckpointDialog;
        _scenarioEditor.LoadSnapshotRequested += LoadSnapshotFromPath;
        _scenarioEditor.ExportSelectedSpeciesRequested += OpenExportSelectedSpeciesDialog;
        _scenarioEditor.ExportSelectedSpeciesClusterRequested += OpenExportSelectedSpeciesClusterDialog;
        _scenarioEditor.ExportSelectedBrainRequested += OpenExportSelectedBrainDialog;
        _scenarioEditor.LoadSpeciesProfileRequested += OpenLoadSpeciesProfileDialog;
        _scenarioEditor.InjectSpeciesRequested += InjectLoadedSpeciesProfile;
        AddChild(_scenarioEditor);

        var repositoryRoot = GetRepositoryRoot();
        var scenarioDirectory = System.IO.Path.Combine(repositoryRoot, "scenarios");
        var outDirectory = System.IO.Path.Combine(repositoryRoot, "out");
        var speciesDirectory = System.IO.Path.Combine(repositoryRoot, SpeciesProfileDirectoryName);
        var brainDirectory = System.IO.Path.Combine(repositoryRoot, BrainProfileDirectoryName);
        var userBrainDirectory = System.IO.Path.Combine(brainDirectory, "user");
        System.IO.Directory.CreateDirectory(speciesDirectory);
        System.IO.Directory.CreateDirectory(brainDirectory);
        System.IO.Directory.CreateDirectory(userBrainDirectory);
        _scenarioEditor.SetScenarioRecipeDirectory(System.IO.Path.Combine(repositoryRoot, "scenarios", "recipes"));
        _scenarioEditor.SetBrainCatalogDirectory(brainDirectory);
        _loadScenarioDialog = CreateScenarioDialog(FileDialog.FileModeEnum.OpenFile, "Load Scenario", scenarioDirectory);
        _saveScenarioDialog = CreateScenarioDialog(FileDialog.FileModeEnum.SaveFile, "Save Scenario", scenarioDirectory);
        _loadSnapshotDialog = CreateSnapshotDialog(outDirectory);
        _loadSpeciesProfileDialog = CreateSpeciesProfileDialog(FileDialog.FileModeEnum.OpenFile, "Load Species Profile", speciesDirectory);
        _saveSpeciesProfileDialog = CreateSpeciesProfileDialog(FileDialog.FileModeEnum.SaveFile, "Export Species Profile", speciesDirectory);
        _saveBrainProfileDialog = CreateBrainProfileDialog(FileDialog.FileModeEnum.SaveFile, "Export Brain Profile", userBrainDirectory);
        _loadScenarioDialog.FileSelected += LoadScenarioFromPath;
        _saveScenarioDialog.FileSelected += SaveScenarioToPath;
        _loadSnapshotDialog.FileSelected += LoadSnapshotFromPath;
        _loadSpeciesProfileDialog.FileSelected += LoadSpeciesProfileFromPath;
        _saveSpeciesProfileDialog.FileSelected += ExportPendingSpeciesProfileToPath;
        _saveBrainProfileDialog.FileSelected += ExportPendingBrainProfileToPath;
        AddChild(_loadScenarioDialog);
        AddChild(_saveScenarioDialog);
        AddChild(_loadSnapshotDialog);
        AddChild(_loadSpeciesProfileDialog);
        AddChild(_saveSpeciesProfileDialog);
        AddChild(_saveBrainProfileDialog);
    }

    private void OpenLoadSnapshotDialog()
    {
        _loadSnapshotDialog.PopupCenteredRatio(0.75f);
    }

    private void OpenLoadCheckpointDialog(string? checkpointDirectory)
    {
        if (!string.IsNullOrWhiteSpace(checkpointDirectory))
        {
            _loadSnapshotDialog.CurrentDir = checkpointDirectory;
        }

        _loadSnapshotDialog.PopupCenteredRatio(0.75f);
    }

    private void LoadSnapshotFromPath(string path)
    {
        if (IsCurrentRunExportBusy("load a snapshot"))
        {
            return;
        }

        try
        {
            var restored = SimulationSnapshotJson.LoadSimulation(path);
            _scenario = restored.Scenario;
            _simulation = restored.Simulation;
            _currentSeed = _scenario.Seed;
            ClearSelection();
            _followSelected = false;
            _stepAccumulator = 0;
            InvalidateTerrainOverlayCache();
            InvalidateResourceRenderCache();
            InvalidateCreatureRenderCache();
            ResetTelemetry();
            _scenarioEditor.SetScenario(_scenario);
            _scenarioEditor.SetLastSnapshotPath(path);
            ResetView();
            _scenarioEditor.SetStatus($"Loaded snapshot {System.IO.Path.GetFileName(path)} at tick {_simulation.State.Tick}.");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Snapshot load failed: {ex.Message}");
        }
    }

    private void OpenExportSelectedSpeciesDialog()
    {
        if (!TryGetSelectedCreature(out var selected))
        {
            _scenarioEditor.SetStatus("Select a living creature before exporting a species profile.");
            return;
        }

        var request = _scenarioEditor.ReadSpeciesExportRequest();
        _pendingSpeciesExportCreatureId = selected.Id;
        _pendingSpeciesExportClusterRepresentative = false;
        _pendingSpeciesExportName = request.Name;
        _pendingSpeciesExportNotes = request.Notes;
        _pendingSpeciesExportPairedBrain = request.ExportPairedBrain;

        var profileName = string.IsNullOrWhiteSpace(request.Name)
            ? $"species_{selected.Id.Value}"
            : SanitizeFileName(request.Name);
        _saveSpeciesProfileDialog.CurrentFile = ToSpeciesProfileFileName(profileName);
        _saveSpeciesProfileDialog.PopupCenteredRatio(0.75f);
    }

    private void OpenExportSelectedSpeciesClusterDialog()
    {
        if (!TryGetSelectedCreature(out var selected))
        {
            _scenarioEditor.SetStatus("Select a living creature before exporting a species cluster profile.");
            return;
        }

        SpeciesClusterRepresentative representative;
        try
        {
            representative = SpeciesClusterAnalyzer.FindRepresentativeForCreature(_simulation.State, selected.Id);
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Species cluster export failed: {ex.Message}");
            return;
        }

        var request = _scenarioEditor.ReadSpeciesExportRequest();
        _pendingSpeciesExportCreatureId = selected.Id;
        _pendingSpeciesExportClusterRepresentative = true;
        _pendingSpeciesExportName = request.Name;
        _pendingSpeciesExportNotes = request.Notes;
        _pendingSpeciesExportPairedBrain = request.ExportPairedBrain;

        var profileName = string.IsNullOrWhiteSpace(request.Name)
            ? representative.Name
            : SanitizeFileName(request.Name);
        _saveSpeciesProfileDialog.CurrentFile = ToSpeciesProfileFileName(profileName);
        _scenarioEditor.SetStatus(
            $"Ready to export cluster {representative.Name}; representative creature #{representative.CreatureId.Value}.");
        _saveSpeciesProfileDialog.PopupCenteredRatio(0.75f);
    }

    private void OpenExportSelectedBrainDialog()
    {
        if (!TryGetSelectedCreature(out var selected))
        {
            _scenarioEditor.SetStatus("Select a living creature before exporting a brain profile.");
            return;
        }

        var request = _scenarioEditor.ReadSpeciesExportRequest();
        _pendingBrainExportCreatureId = selected.Id;
        _pendingBrainExportName = string.IsNullOrWhiteSpace(request.Name)
            ? $"brain_{selected.Id.Value}"
            : request.Name;
        _pendingBrainExportNotes = request.Notes;

        _saveBrainProfileDialog.CurrentFile = ToBrainProfileFileName(_pendingBrainExportName);
        _saveBrainProfileDialog.PopupCenteredRatio(0.75f);
    }

    private void OpenLoadSpeciesProfileDialog()
    {
        _loadSpeciesProfileDialog.PopupCenteredRatio(0.75f);
    }

    private void LoadSpeciesProfileFromPath(string path)
    {
        try
        {
            _loadedSpeciesProfile = SpeciesProfileJson.Load(path);
            _loadedSpeciesProfilePath = path;
            _scenarioEditor.SetLoadedSpeciesProfilePath(ToWorkspaceRelativePath(path), _loadedSpeciesProfile.DefaultBrainPath);
            _scenarioEditor.SetStatus($"Loaded species profile {_loadedSpeciesProfile.Name}.");
        }
        catch (Exception ex)
        {
            _loadedSpeciesProfile = null;
            _loadedSpeciesProfilePath = null;
            _scenarioEditor.SetLoadedSpeciesProfilePath(null, null);
            _scenarioEditor.SetStatus($"Species profile load failed: {ex.Message}");
        }
    }

    private void ExportPendingSpeciesProfileToPath(string path)
    {
        if (_pendingSpeciesExportCreatureId == default)
        {
            _scenarioEditor.SetStatus("Species export failed: no creature was selected.");
            return;
        }

        try
        {
            path = SpeciesProfileJson.WithFileExtension(path);
            var profile = _pendingSpeciesExportClusterRepresentative
                ? SpeciesProfileExporter.ExportSpeciesClusterRepresentativeForCreature(
                    _scenario,
                    _simulation.State,
                    _pendingSpeciesExportCreatureId,
                    _pendingSpeciesExportName,
                    _pendingSpeciesExportNotes)
                : SpeciesProfileExporter.ExportCreature(
                    _scenario,
                    _simulation.State,
                    _pendingSpeciesExportCreatureId,
                    _pendingSpeciesExportName,
                    _pendingSpeciesExportNotes);
            string? pairedBrainPath = null;
            if (_pendingSpeciesExportPairedBrain)
            {
                pairedBrainPath = ExportPairedBrainProfile(profile);
                profile = profile with { DefaultBrainPath = ToWorkspaceRelativePath(pairedBrainPath) };
            }

            SpeciesProfileJson.Save(path, profile);
            _loadedSpeciesProfile = profile;
            _loadedSpeciesProfilePath = path;
            _scenarioEditor.SetLastSpeciesExportPath(path);
            _scenarioEditor.SetLoadedSpeciesProfilePath(ToWorkspaceRelativePath(path), profile.DefaultBrainPath);
            _scenarioEditor.SetStatus(pairedBrainPath is null
                ? $"Exported species profile {profile.Name}."
                : $"Exported species profile {profile.Name} with paired brain {System.IO.Path.GetFileName(pairedBrainPath)}.");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Species export failed: {ex.Message}");
        }
        finally
        {
            _pendingSpeciesExportCreatureId = default;
            _pendingSpeciesExportClusterRepresentative = false;
            _pendingSpeciesExportPairedBrain = false;
            _pendingSpeciesExportName = null;
            _pendingSpeciesExportNotes = null;
        }
    }

    private string ExportPairedBrainProfile(SpeciesProfile profile)
    {
        var name = $"{profile.Name} Brain";
        var notes = string.IsNullOrWhiteSpace(profile.Notes)
            ? $"Paired controller exported with species profile {profile.Name}."
            : $"Paired controller exported with species profile {profile.Name}. {profile.Notes}";
        var brainProfile = BrainProfileExporter.ExportCreatureBrain(
            _scenario,
            _simulation.State,
            new EntityId(profile.Source.CreatureId),
            name,
            notes);
        var brainPath = GetUniqueManagedProfilePath(
            System.IO.Path.Combine(GetUserBrainCatalogDirectory(), $"{SanitizeFileName(name)}{BrainProfileJson.FileExtension}"),
            BrainProfileJson.FileExtension);
        BrainProfileJson.Save(brainPath, brainProfile);
        _scenarioEditor.SetLastBrainExportPath(brainPath);
        RefreshBrainCatalog();
        return brainPath;
    }

    private void ExportPendingBrainProfileToPath(string path)
    {
        if (_pendingBrainExportCreatureId == default)
        {
            _scenarioEditor.SetStatus("Brain export failed: no creature was selected.");
            return;
        }

        try
        {
            path = BrainProfileJson.WithFileExtension(path);
            var profile = BrainProfileExporter.ExportCreatureBrain(
                _scenario,
                _simulation.State,
                _pendingBrainExportCreatureId,
                _pendingBrainExportName,
                _pendingBrainExportNotes);
            BrainProfileJson.Save(path, profile);
            _scenarioEditor.SetLastBrainExportPath(path);
            RefreshBrainCatalog();
            _scenarioEditor.SetStatus($"Exported brain profile {profile.Name}.");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Brain export failed: {ex.Message}");
        }
        finally
        {
            _pendingBrainExportCreatureId = default;
            _pendingBrainExportName = null;
            _pendingBrainExportNotes = null;
        }
    }

    private void InjectLoadedSpeciesProfile()
    {
        if (IsCurrentRunExportBusy("inject species"))
        {
            return;
        }

        if (_loadedSpeciesProfile is null)
        {
            _scenarioEditor.SetStatus("Load a species profile before injecting.");
            return;
        }

        try
        {
            var request = _scenarioEditor.ReadSpeciesInjectionRequest();
            var brainOverrideProfile = LoadSpeciesInjectionBrainProfile(request);
            var result = SpeciesProfileInjector.Inject(
                _simulation.State,
                _loadedSpeciesProfile,
                new SpeciesInjectionOptions(
                    request.Count,
                    request.SpawnRegion,
                    request.EnergyOverride,
                    request.BrainOverrideKind,
                    brainOverrideProfile,
                    _scenario.BrainArchitectureKind,
                    _scenario.BrainHiddenNodeCount,
                    MutationProfile.FromScenario(_scenario)));

            if (result.CreatureIds.Count > 0)
            {
                _selectedCreatureId = result.CreatureIds[0];
                _selectedEggId = default;
            }

            _scenarioSpeciesInjections = _scenarioSpeciesInjections.Concat(new[] { result }).ToArray();
            InvalidateCreatureRenderCache();
            ResetTelemetry();
            _scenarioEditor.SetStatus(
                $"Injected {result.CreatureIds.Count} {result.SpeciesName} founders from {System.IO.Path.GetFileName(_loadedSpeciesProfilePath ?? "profile")}.");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetStatus($"Species injection failed: {ex.Message}");
        }
    }

    private BrainProfile? LoadSpeciesInjectionBrainProfile(SpeciesInjectionUiRequest request)
    {
        if (request.BrainOverrideKind is not null || _loadedSpeciesProfile is null)
        {
            return null;
        }

        var brainProfilePath = !string.IsNullOrWhiteSpace(request.BrainProfilePath)
            ? request.BrainProfilePath
            : _loadedSpeciesProfile.DefaultBrainPath;
        if (string.IsNullOrWhiteSpace(brainProfilePath))
        {
            return null;
        }

        var workspaceRoot = GetRepositoryRoot();
        var resolvedPath = SimulationScenarioSpeciesSeeder.ResolveBrainProfilePath(
            brainProfilePath,
            _loadedSpeciesProfilePath,
            _currentScenarioPath,
            workspaceRoot);
        return BrainProfileJson.Load(resolvedPath);
    }

    private void WriteCurrentReportFromEditor()
    {
        if (_runExportInProgress)
        {
            _scenarioEditor.SetStatus("Current run export already in progress.");
            return;
        }

        try
        {
            var workspaceRoot = GetRepositoryRoot();
            var request = _scenarioEditor.ReadCliRunRequest();
            var statsPath = ResolveWorkspacePath(request.OutputPath, workspaceRoot);
            var reportPath = ResolveWorkspacePath(request.ReportPath, workspaceRoot);
            var snapshotPath = ResolveWorkspacePath(request.SnapshotPath, workspaceRoot);
            var scenario = _scenario;
            var simulation = _simulation;
            var speciesInjections = _scenarioSpeciesInjections;
            var previousPaused = _paused;
            var stopwatch = Stopwatch.StartNew();

            _runExportInProgress = true;
            _paused = true;
            _stepAccumulator = 0;
            _scenarioEditor.SetStatus("Exporting current run... simulation paused while the bundle is written.");

            _ = Task.Run(() =>
            {
                try
                {
                    var result = GodotRunExportWriter.Write(statsPath, reportPath, snapshotPath, scenario, simulation, speciesInjections);
                    stopwatch.Stop();
                    _mainThreadActions.Enqueue(() => CompleteCurrentRunExport(result, previousPaused, stopwatch.Elapsed));
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _mainThreadActions.Enqueue(() => FailCurrentRunExport(ex.Message, previousPaused, stopwatch.Elapsed));
                }
            });
        }
        catch (Exception ex)
        {
            _runExportInProgress = false;
            _scenarioEditor.SetLastReportPath(null);
            _scenarioEditor.SetLastSnapshotPath(null);
            _scenarioEditor.SetStatus($"Export failed: {ex.Message}");
        }
    }

    private bool IsCurrentRunExportBusy(string action)
    {
        if (!_runExportInProgress)
        {
            return false;
        }

        _scenarioEditor.SetStatus($"Wait for the current run export to finish before you {action}.");
        return true;
    }

    private void CompleteCurrentRunExport(GodotRunExportResult result, bool previousPaused, TimeSpan elapsed)
    {
        _runExportInProgress = false;
        _paused = previousPaused;
        _stepAccumulator = 0;
        _scenarioEditor.SetLastReportPath(result.ReportPath);
        _scenarioEditor.SetLastSnapshotPath(result.SnapshotPath);
        _scenarioEditor.SetStatus(
            $"Current run exported: {result.FileCount} files in {elapsed.TotalSeconds:0.0}s. Open the report or load the snapshot from the CLI tab.");
    }

    private void FailCurrentRunExport(string message, bool previousPaused, TimeSpan elapsed)
    {
        _runExportInProgress = false;
        _paused = previousPaused;
        _stepAccumulator = 0;
        _scenarioEditor.SetLastReportPath(null);
        _scenarioEditor.SetLastSnapshotPath(null);
        _scenarioEditor.SetStatus($"Export failed after {elapsed.TotalSeconds:0.0}s: {message}");
    }

    private static FileDialog CreateScenarioDialog(FileDialog.FileModeEnum mode, string title, string scenarioDirectory)
    {
        return new FileDialog
        {
            Title = title,
            FileMode = mode,
            Access = FileDialog.AccessEnum.Filesystem,
            CurrentDir = scenarioDirectory,
            Filters = ["*.json;Scenario JSON"]
        };
    }

    private static FileDialog CreateSnapshotDialog(string snapshotDirectory)
    {
        return new FileDialog
        {
            Title = "Load Simulation Snapshot",
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            CurrentDir = snapshotDirectory,
            Filters = ["*.json;Snapshot JSON"]
        };
    }

    private static FileDialog CreateSpeciesProfileDialog(FileDialog.FileModeEnum mode, string title, string profileDirectory)
    {
        return new FileDialog
        {
            Title = title,
            FileMode = mode,
            Access = FileDialog.AccessEnum.Filesystem,
            CurrentDir = profileDirectory,
            Filters = [$"{SpeciesProfileJson.FilePattern};Species Profile ({SpeciesProfileJson.FileExtension})"]
        };
    }

    private static FileDialog CreateBrainProfileDialog(FileDialog.FileModeEnum mode, string title, string profileDirectory)
    {
        return new FileDialog
        {
            Title = title,
            FileMode = mode,
            Access = FileDialog.AccessEnum.Filesystem,
            CurrentDir = profileDirectory,
            Filters = [$"{BrainProfileJson.FilePattern};Brain Profile ({BrainProfileJson.FileExtension})"]
        };
    }

    private static string GetRepositoryRoot()
    {
        var projectDirectory = ProjectSettings.GlobalizePath("res://");
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDirectory, "..", ".."));
    }

    private float GetLauncherWidth()
    {
        if (!_scenarioEditor.Visible || _scenarioEditor.IsCollapsed)
        {
            return 0f;
        }

        return LauncherPanelWidth;
    }

    private static string ResolveWorkspacePath(string path, string workspaceRoot)
    {
        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectSettings.GlobalizePath(path);
        }

        return System.IO.Path.IsPathRooted(path)
            ? System.IO.Path.GetFullPath(path)
            : System.IO.Path.GetFullPath(System.IO.Path.Combine(workspaceRoot, path));
    }

    private string FormatScenarioSpeciesSeedStatus()
    {
        var seededCount = _scenarioSpeciesInjections.Sum(injection => injection.CreatureIds.Count);
        return seededCount > 0
            ? $" Seeded {seededCount} scenario roster creatures."
            : string.Empty;
    }

    private static string ToWorkspaceRelativePath(string path)
    {
        try
        {
            var root = System.IO.Path.GetFullPath(GetRepositoryRoot());
            var fullPath = System.IO.Path.GetFullPath(path);
            var relativePath = System.IO.Path.GetRelativePath(root, fullPath);
            if (!relativePath.StartsWith("..", StringComparison.Ordinal)
                && !System.IO.Path.IsPathFullyQualified(relativePath))
            {
                return relativePath.Replace('\\', '/');
            }
        }
        catch
        {
            // Keep absolute paths for files outside the workspace or unusual paths.
        }

        return path;
    }

    private static string GetBrainCatalogDirectory()
    {
        return System.IO.Path.Combine(GetRepositoryRoot(), BrainProfileDirectoryName);
    }

    private static string GetUserBrainCatalogDirectory()
    {
        var directory = System.IO.Path.Combine(GetBrainCatalogDirectory(), "user");
        System.IO.Directory.CreateDirectory(directory);
        return directory;
    }

    private void RefreshBrainCatalog()
    {
        _scenarioEditor.SetBrainCatalogDirectory(GetBrainCatalogDirectory());
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalid.Contains(character) ? '_' : character)
            .ToArray())
            .Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "species" : sanitized;
    }

    private static string ToSpeciesProfileFileName(string value)
    {
        var sanitized = SanitizeFileName(value);
        return sanitized.EndsWith(SpeciesProfileJson.FileExtension, StringComparison.OrdinalIgnoreCase)
            ? sanitized
            : $"{sanitized}{SpeciesProfileJson.FileExtension}";
    }

    private static string ToBrainProfileFileName(string value)
    {
        var sanitized = SanitizeFileName(value);
        return sanitized.EndsWith(BrainProfileJson.FileExtension, StringComparison.OrdinalIgnoreCase)
            ? sanitized
            : $"{sanitized}{BrainProfileJson.FileExtension}";
    }

    private static string GetUniqueManagedProfilePath(string preferredPath, string profileExtension)
    {
        if (!System.IO.File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var directory = System.IO.Path.GetDirectoryName(preferredPath) ?? ".";
        var fileName = System.IO.Path.GetFileName(preferredPath);
        var stem = fileName.EndsWith(profileExtension, StringComparison.OrdinalIgnoreCase)
            ? fileName[..^profileExtension.Length]
            : System.IO.Path.GetFileNameWithoutExtension(fileName);
        var extension = fileName.EndsWith(profileExtension, StringComparison.OrdinalIgnoreCase)
            ? profileExtension
            : System.IO.Path.GetExtension(fileName);

        for (var index = 2; index < 1000; index++)
        {
            var candidate = System.IO.Path.Combine(directory, $"{stem}_{index}{extension}");
            if (!System.IO.File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Could not choose a unique profile path.");
    }

    private static string LastLine(string primary, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(primary) ? fallback : primary;
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return lines.Length == 0 ? "No process output." : lines[^1];
    }

    private static string? FindLatestCheckpoint(string checkpointDirectory)
    {
        if (string.IsNullOrWhiteSpace(checkpointDirectory) || !System.IO.Directory.Exists(checkpointDirectory))
        {
            return null;
        }

        return System.IO.Directory.EnumerateFiles(checkpointDirectory, "tick_*.json")
            .Select(path => new
            {
                Path = path,
                Tick = TryParseCheckpointTick(path, out var tick) ? tick : long.MinValue,
                LastWriteTime = System.IO.File.GetLastWriteTimeUtc(path)
            })
            .OrderByDescending(candidate => candidate.Tick)
            .ThenByDescending(candidate => candidate.LastWriteTime)
            .Select(candidate => candidate.Path)
            .FirstOrDefault();
    }

    private static bool TryParseCheckpointTick(string path, out long tick)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        if (!name.StartsWith("tick_", StringComparison.OrdinalIgnoreCase))
        {
            tick = 0;
            return false;
        }

        return long.TryParse(name[5..], NumberStyles.None, CultureInfo.InvariantCulture, out tick);
    }

    private readonly record struct CliRunResult(
        int ExitCode,
        string Message,
        string ReportPath,
        string SnapshotPath,
        string CheckpointDirectory,
        string LatestCheckpointPath);

    private void UpdateLivingGenerationRange()
    {
        var creatures = _simulation.State.Creatures;
        if (creatures.Count == 0)
        {
            _livingMinGeneration = 0;
            _livingMaxGeneration = 0;
            return;
        }

        var minGeneration = int.MaxValue;
        var maxGeneration = int.MinValue;
        foreach (var creature in creatures)
        {
            minGeneration = Math.Min(minGeneration, creature.Generation);
            maxGeneration = Math.Max(maxGeneration, creature.Generation);
        }

        _livingMinGeneration = minGeneration;
        _livingMaxGeneration = maxGeneration;
    }

    private static Color ColorForGeneration(int generation, int minGeneration, int maxGeneration)
    {
        var ratio = maxGeneration > minGeneration
            ? Mathf.Clamp((generation - minGeneration) / (float)(maxGeneration - minGeneration), 0f, 1f)
            : 0f;
        var hue = Mathf.Lerp(0.13f, 0.83f, ratio);
        return Color.FromHsv(hue, 0.46f, 0.92f);
    }

    private static string FormatSpeed(float speed)
    {
        return speed >= 1f ? $"{speed:0}x" : $"{speed:0.###}x";
    }

    private static string FormatColorMode(CreatureColorMode colorMode)
    {
        return colorMode switch
        {
            CreatureColorMode.FounderLineage => "founder lineage",
            _ => colorMode.ToString().ToLowerInvariant()
        };
    }

    private string FormatVisualRenderMode()
    {
        if (_visualRenderMode == VisualRenderMode.LegacyShapes || _spriteThemes.Count == 0)
        {
            return "legacy shapes";
        }

        return _spriteThemes[Math.Clamp(_spriteThemeIndex, 0, _spriteThemes.Count - 1)].Name;
    }

    private static string FormatResourceRenderMode(ResourceRenderMode mode)
    {
        return mode switch
        {
            ResourceRenderMode.Aggregate => "density",
            _ => "individual"
        };
    }

    private static string FormatDrawCount(int individualCount, int aggregateCount)
    {
        return aggregateCount > 0
            ? $"{aggregateCount} cells"
            : individualCount.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatCreatureRenderMode(CreatureRenderMode mode)
    {
        return mode switch
        {
            CreatureRenderMode.Aggregate => "density",
            _ => "individual"
        };
    }

    private static string FormatMapOverlayMode(MapOverlayMode mode)
    {
        return mode switch
        {
            MapOverlayMode.Biome => "biome",
            MapOverlayMode.Temperature => "temperature",
            _ => "off"
        };
    }

    private static string FormatBiomeKind(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome).ToString().ToLowerInvariant();
    }

    private static string FormatTemperatureIndex(float temperature)
    {
        return $"{Math.Clamp(temperature, 0f, 1f) * 100f:0.#}";
    }

    private static string FormatBiomeMapKind(BiomeMapKind mapKind)
    {
        return mapKind switch
        {
            BiomeMapKind.NaturalClimate => "natural climate",
            BiomeMapKind.HorizontalBands => "horizontal bands",
            BiomeMapKind.VerticalBands => "vertical bands",
            BiomeMapKind.HorizontalEdgeBands => "horizontal edge bands",
            BiomeMapKind.VerticalEdgeBands => "vertical edge bands",
            BiomeMapKind.HorizontalEdgeLadderBands => "horizontal ladder bands",
            BiomeMapKind.VerticalEdgeLadderBands => "vertical ladder bands",
            BiomeMapKind.VerticalEdgeCorridorBands => "vertical corridor bands",
            BiomeMapKind.VerticalEdgeWideCorridorBands => "wide vertical corridor bands",
            _ => "noise"
        };
    }

    private static string FormatBrainArchitectureKind(BrainArchitectureKind kind)
    {
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => "hybrid neural",
            BrainArchitectureKind.HiddenLayerNeural => "hidden-layer neural",
            BrainArchitectureKind.HybridDeep8x8Neural => "hybrid deep 8x8 neural",
            BrainArchitectureKind.HiddenDeep8x8Neural => "hidden deep 8x8 neural",
            _ => kind.ToString()
        };
    }

    private static string FormatObstacleMapKind(ObstacleMapKind mapKind)
    {
        return mapKind switch
        {
            ObstacleMapKind.VerticalBarrierWithGaps => "vertical barrier",
            ObstacleMapKind.HorizontalBarrierWithGaps => "horizontal barrier",
            ObstacleMapKind.ScatteredRocks => "scattered rocks",
            _ => "none"
        };
    }

    private static string FormatResourceKind(ResourceKind kind)
    {
        return kind.ToString().ToLowerInvariant();
    }

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:0.0}%";
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    private sealed class ResourceRenderCache
    {
        private ResourceRenderChunk[] _chunks = [];
        private float _boundsWidth;
        private float _boundsHeight;
        private int _resourceCount = -1;

        public float ChunkSize { get; private set; } = ResourceRenderChunkSize;

        public int ChunkCountX { get; private set; }

        public int ChunkCountY { get; private set; }

        public long Tick { get; private set; } = -1;

        public bool NeedsRebuild(WorldBounds bounds, int resourceCount)
        {
            return _chunks.Length == 0
                || _resourceCount != resourceCount
                || !NearlyEqual(_boundsWidth, bounds.Width)
                || !NearlyEqual(_boundsHeight, bounds.Height)
                || !NearlyEqual(ChunkSize, ResourceRenderChunkSize);
        }

        public void Rebuild(WorldBounds bounds, IReadOnlyList<ResourcePatchState> resources, long tick, float chunkSize)
        {
            ChunkSize = chunkSize;
            _boundsWidth = bounds.Width;
            _boundsHeight = bounds.Height;
            _resourceCount = resources.Count;
            Tick = tick;
            ChunkCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / chunkSize));
            ChunkCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / chunkSize));
            _chunks = new ResourceRenderChunk[ChunkCountX * ChunkCountY];

            for (var i = 0; i < resources.Count; i++)
            {
                var resource = resources[i];
                var chunkX = ClampChunkIndex((int)MathF.Floor(resource.Position.X / chunkSize), ChunkCountX);
                var chunkY = ClampChunkIndex((int)MathF.Floor(resource.Position.Y / chunkSize), ChunkCountY);
                ref var chunk = ref _chunks[GetIndex(chunkX, chunkY)];
                chunk.ResourceIndices ??= new List<int>();
                chunk.ResourceIndices.Add(i);

                if (resource.Calories <= 0f)
                {
                    continue;
                }

                chunk.DrawableResourceCount++;
                chunk.TotalCalories += resource.Calories;
                chunk.TotalMaxCalories += MathF.Max(resource.MaxCalories, resource.Calories);
                if (resource.Kind == ResourceKind.Meat)
                {
                    chunk.MeatCalories += resource.Calories;
                }
                else
                {
                    switch (resource.PlantKind)
                    {
                        case PlantResourceKind.Tender:
                            chunk.TenderPlantCalories += resource.Calories;
                            break;
                        case PlantResourceKind.Rich:
                            chunk.RichPlantCalories += resource.Calories;
                            break;
                        case PlantResourceKind.Tough:
                            chunk.ToughPlantCalories += resource.Calories;
                            break;
                        default:
                            chunk.GenericPlantCalories += resource.Calories;
                            break;
                    }
                }
            }
        }

        public IEnumerable<ResourceRenderChunk> VisibleChunks(Rect2 visibleWorldRect)
        {
            if (!TryGetVisibleRange(visibleWorldRect, out var range))
            {
                yield break;
            }

            for (var y = range.MinY; y <= range.MaxY; y++)
            {
                for (var x = range.MinX; x <= range.MaxX; x++)
                {
                    yield return _chunks[GetIndex(x, y)];
                }
            }
        }

        public int CountVisibleDrawableResources(Rect2 visibleWorldRect, int limit)
        {
            if (!TryGetVisibleRange(visibleWorldRect, out var range))
            {
                return 0;
            }

            var count = 0;
            for (var y = range.MinY; y <= range.MaxY; y++)
            {
                for (var x = range.MinX; x <= range.MaxX; x++)
                {
                    count += _chunks[GetIndex(x, y)].DrawableResourceCount;
                    if (count >= limit)
                    {
                        return count;
                    }
                }
            }

            return count;
        }

        public bool TryGetVisibleRange(Rect2 visibleWorldRect, out ResourceChunkRange range)
        {
            if (_chunks.Length == 0 || visibleWorldRect.Size.X <= 0f || visibleWorldRect.Size.Y <= 0f)
            {
                range = default;
                return false;
            }

            var minX = ClampChunkIndex((int)MathF.Floor(visibleWorldRect.Position.X / ChunkSize), ChunkCountX);
            var minY = ClampChunkIndex((int)MathF.Floor(visibleWorldRect.Position.Y / ChunkSize), ChunkCountY);
            var maxX = ClampChunkIndex((int)MathF.Floor((visibleWorldRect.Position.X + visibleWorldRect.Size.X) / ChunkSize), ChunkCountX);
            var maxY = ClampChunkIndex((int)MathF.Floor((visibleWorldRect.Position.Y + visibleWorldRect.Size.Y) / ChunkSize), ChunkCountY);
            range = new ResourceChunkRange(minX, minY, maxX, maxY);
            return true;
        }

        public ResourceAggregateSummary SummarizeChunks(int minX, int minY, int maxX, int maxY)
        {
            var summary = new ResourceAggregateSummary();
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var chunk = _chunks[GetIndex(x, y)];
                    summary.DrawableResourceCount += chunk.DrawableResourceCount;
                    summary.TotalCalories += chunk.TotalCalories;
                    summary.TotalMaxCalories += chunk.TotalMaxCalories;
                    summary.MeatCalories += chunk.MeatCalories;
                    summary.GenericPlantCalories += chunk.GenericPlantCalories;
                    summary.TenderPlantCalories += chunk.TenderPlantCalories;
                    summary.RichPlantCalories += chunk.RichPlantCalories;
                    summary.ToughPlantCalories += chunk.ToughPlantCalories;
                }
            }

            return summary;
        }

        public Rect2 GetChunkWorldRect(int minX, int minY, int maxX, int maxY)
        {
            var left = minX * ChunkSize;
            var top = minY * ChunkSize;
            var right = MathF.Min(_boundsWidth, (maxX + 1) * ChunkSize);
            var bottom = MathF.Min(_boundsHeight, (maxY + 1) * ChunkSize);
            return new Rect2(new Vector2(left, top), new Vector2(MathF.Max(0f, right - left), MathF.Max(0f, bottom - top)));
        }

        private int GetIndex(int x, int y)
        {
            return y * ChunkCountX + x;
        }

        private static int ClampChunkIndex(int value, int chunkCount)
        {
            return Math.Clamp(value, 0, Math.Max(0, chunkCount - 1));
        }

        private static bool NearlyEqual(float first, float second)
        {
            return MathF.Abs(first - second) <= 0.001f;
        }
    }

    private struct ResourceRenderChunk
    {
        public List<int>? ResourceIndices;

        public int DrawableResourceCount;

        public float TotalCalories;

        public float TotalMaxCalories;

        public float MeatCalories;

        public float GenericPlantCalories;

        public float TenderPlantCalories;

        public float RichPlantCalories;

        public float ToughPlantCalories;
    }

    private readonly record struct ResourceChunkRange(int MinX, int MinY, int MaxX, int MaxY);

    private struct ResourceAggregateSummary
    {
        public int DrawableResourceCount;

        public float TotalCalories;

        public float TotalMaxCalories;

        public float MeatCalories;

        public float GenericPlantCalories;

        public float TenderPlantCalories;

        public float RichPlantCalories;

        public float ToughPlantCalories;
    }

    private sealed class CreatureRenderCache
    {
        private CreatureRenderChunk[] _chunks = [];
        private float _boundsWidth;
        private float _boundsHeight;
        private int _creatureCount = -1;

        public float ChunkSize { get; private set; } = CreatureRenderChunkSize;

        public int ChunkCountX { get; private set; }

        public int ChunkCountY { get; private set; }

        public long Tick { get; private set; } = -1;

        public bool NeedsRebuild(WorldBounds bounds, int creatureCount)
        {
            return _chunks.Length == 0
                || _creatureCount != creatureCount
                || !NearlyEqual(_boundsWidth, bounds.Width)
                || !NearlyEqual(_boundsHeight, bounds.Height)
                || !NearlyEqual(ChunkSize, CreatureRenderChunkSize);
        }

        public void Rebuild(WorldBounds bounds, IReadOnlyList<CreatureState> creatures, long tick, float chunkSize)
        {
            ChunkSize = chunkSize;
            _boundsWidth = bounds.Width;
            _boundsHeight = bounds.Height;
            _creatureCount = creatures.Count;
            Tick = tick;
            ChunkCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / chunkSize));
            ChunkCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / chunkSize));
            _chunks = new CreatureRenderChunk[ChunkCountX * ChunkCountY];

            for (var i = 0; i < creatures.Count; i++)
            {
                var creature = creatures[i];
                var chunkX = ClampChunkIndex((int)MathF.Floor(creature.Position.X / chunkSize), ChunkCountX);
                var chunkY = ClampChunkIndex((int)MathF.Floor(creature.Position.Y / chunkSize), ChunkCountY);
                ref var chunk = ref _chunks[GetIndex(chunkX, chunkY)];
                chunk.CreatureIndices ??= new List<int>();
                chunk.CreatureIndices.Add(i);
                chunk.CreatureCount++;
                chunk.TotalEnergy += creature.Energy;
                chunk.MaxGeneration = Math.Max(chunk.MaxGeneration, creature.Generation);
            }
        }

        public IEnumerable<CreatureRenderChunk> VisibleChunks(Rect2 visibleWorldRect)
        {
            if (!TryGetVisibleRange(visibleWorldRect, out var range))
            {
                yield break;
            }

            for (var y = range.MinY; y <= range.MaxY; y++)
            {
                for (var x = range.MinX; x <= range.MaxX; x++)
                {
                    yield return _chunks[GetIndex(x, y)];
                }
            }
        }

        public int CountVisibleCreatures(Rect2 visibleWorldRect, int limit)
        {
            if (!TryGetVisibleRange(visibleWorldRect, out var range))
            {
                return 0;
            }

            var count = 0;
            for (var y = range.MinY; y <= range.MaxY; y++)
            {
                for (var x = range.MinX; x <= range.MaxX; x++)
                {
                    count += _chunks[GetIndex(x, y)].CreatureCount;
                    if (count >= limit)
                    {
                        return count;
                    }
                }
            }

            return count;
        }

        public bool TryGetVisibleRange(Rect2 visibleWorldRect, out CreatureChunkRange range)
        {
            if (_chunks.Length == 0 || visibleWorldRect.Size.X <= 0f || visibleWorldRect.Size.Y <= 0f)
            {
                range = default;
                return false;
            }

            var minX = ClampChunkIndex((int)MathF.Floor(visibleWorldRect.Position.X / ChunkSize), ChunkCountX);
            var minY = ClampChunkIndex((int)MathF.Floor(visibleWorldRect.Position.Y / ChunkSize), ChunkCountY);
            var maxX = ClampChunkIndex((int)MathF.Floor((visibleWorldRect.Position.X + visibleWorldRect.Size.X) / ChunkSize), ChunkCountX);
            var maxY = ClampChunkIndex((int)MathF.Floor((visibleWorldRect.Position.Y + visibleWorldRect.Size.Y) / ChunkSize), ChunkCountY);
            range = new CreatureChunkRange(minX, minY, maxX, maxY);
            return true;
        }

        public CreatureAggregateSummary SummarizeChunks(int minX, int minY, int maxX, int maxY)
        {
            var summary = new CreatureAggregateSummary();
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var chunk = _chunks[GetIndex(x, y)];
                    summary.CreatureCount += chunk.CreatureCount;
                    summary.TotalEnergy += chunk.TotalEnergy;
                    summary.MaxGeneration = Math.Max(summary.MaxGeneration, chunk.MaxGeneration);
                }
            }

            return summary;
        }

        public Rect2 GetChunkWorldRect(int minX, int minY, int maxX, int maxY)
        {
            var left = minX * ChunkSize;
            var top = minY * ChunkSize;
            var right = MathF.Min(_boundsWidth, (maxX + 1) * ChunkSize);
            var bottom = MathF.Min(_boundsHeight, (maxY + 1) * ChunkSize);
            return new Rect2(new Vector2(left, top), new Vector2(MathF.Max(0f, right - left), MathF.Max(0f, bottom - top)));
        }

        private int GetIndex(int x, int y)
        {
            return y * ChunkCountX + x;
        }

        private static int ClampChunkIndex(int value, int chunkCount)
        {
            return Math.Clamp(value, 0, Math.Max(0, chunkCount - 1));
        }

        private static bool NearlyEqual(float first, float second)
        {
            return MathF.Abs(first - second) <= 0.001f;
        }
    }

    private struct CreatureRenderChunk
    {
        public List<int>? CreatureIndices;

        public int CreatureCount;

        public float TotalEnergy;

        public int MaxGeneration;
    }

    private readonly record struct CreatureChunkRange(int MinX, int MinY, int MaxX, int MaxY);

    private struct CreatureAggregateSummary
    {
        public int CreatureCount;

        public float TotalEnergy;

        public int MaxGeneration;
    }

    private readonly record struct LiveGrabStats(
        float AverageOutput,
        float IntentShare,
        float CanGrabShare,
        int HoldingCount,
        int GrabbedCount,
        float AveragePressure,
        float MaxPressure,
        float AverageStrength,
        float MaxStrength);

    private readonly record struct ColorLegendEntry(string Label, Color Color, int Generation = -1);

    private enum CreatureColorMode
    {
        Generation,
        FounderLineage,
        Energy,
        Age,
        Off
    }

    private enum SelectedInspectorView
    {
        Summary,
        State,
        Body,
        Senses,
        Brain
    }

    private enum GraphMetric
    {
        Population,
        ResourceCalories,
        Deaths,
        Season
    }

    private enum VisualRenderMode
    {
        LegacyShapes,
        SpriteTheme
    }

    private enum MapOverlayMode
    {
        Biome,
        Temperature,
        Off
    }

    private enum SpriteAtlasSlot
    {
        CreatureScavenger = 0,
        CreaturePredator = 1,
        CreatureOmnivore = 2,
        CreaturePlantSpecialist = 3,
        CreatureGrazer = 4,
        CreatureArmored = 5,
        CreatureFast = 6,
        CreatureScout = 7,
        CreatureTiny = 8,
        CreatureGeneralist = 9,
        PlantGeneric = 10,
        PlantTender = 11,
        PlantRich = 12,
        PlantTough = 13,
        PlantDormant = 14,
        EggA = 15,
        EggB = 16,
        MeatFresh = 17,
        MeatStale = 18,
        FoodParticleA = 19,
        FoodParticleB = 20,
        FoodParticleC = 21,
        EyeOverlay = 22,
        SmallPrey = 23
    }

    private enum ResourceRenderMode
    {
        Individual,
        Aggregate
    }

    private enum CreatureRenderMode
    {
        Individual,
        Aggregate
    }

    private sealed record SpriteTheme(
        string Name,
        string ResourcePath,
        Texture2D Texture,
        Texture2D? ColorMaskTexture,
        Rect2[] Regions,
        bool DrawProceduralCreatureEyes);
}
