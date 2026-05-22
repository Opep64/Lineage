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
    private const float LauncherPanelWidth = 520f;
    private const float CollapsedLauncherPanelWidth = 190f;
    private const float RightPanelWidth = 300f;
    private const float ViewMargin = 24f;
    private const int GraphSampleCount = 240;
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

    private readonly Color _backgroundColor = new(0.07f, 0.08f, 0.075f);
    private readonly Color _worldColor = new(0.11f, 0.13f, 0.11f);
    private readonly Color _panelColor = new(0.055f, 0.06f, 0.058f);
    private readonly Color _resourceColor = new(0.24f, 0.74f, 0.36f);
    private readonly Color _meatResourceColor = new(0.72f, 0.22f, 0.20f);
    private readonly Color _eggColor = new(0.86f, 0.88f, 0.72f);
    private readonly Color _creatureColor = new(0.82f, 0.73f, 0.48f);
    private readonly Color _selectedColor = new(1.0f, 0.94f, 0.42f);
    private readonly Color _senseColor = new(0.35f, 0.62f, 0.92f, 0.18f);
    private readonly Color _graphPopulationColor = new(0.96f, 0.78f, 0.34f);
    private readonly Color _graphResourceColor = new(0.31f, 0.82f, 0.48f);
    private readonly Color _graphDeathColor = new(0.96f, 0.32f, 0.28f);

    private Simulation _simulation = null!;
    private SimulationScenario _scenario = new();
    private ScenarioEditorPanel _scenarioEditor = null!;
    private FileDialog _loadScenarioDialog = null!;
    private FileDialog _saveScenarioDialog = null!;
    private FileDialog _loadSnapshotDialog = null!;
    private Label _hud = null!;
    private ScrollContainer _inspectorScroll = null!;
    private Label _inspector = null!;
    private Label _graphLegend = null!;
    private Label _scaleBarLabel = null!;
    private bool _paused;
    private float _speedMultiplier = 1f;
    private CreatureColorMode _colorMode = CreatureColorMode.Generation;
    private double _stepAccumulator;
    private EntityId _selectedCreatureId;
    private EntityId _selectedEggId;
    private ulong _currentSeed = SimulationScenario.DefaultSeed;
    private string? _currentScenarioPath;
    private bool _cliRunInProgress;
    private bool _isPanning;
    private bool _followSelected;
    private bool _showBiomeOverlay = true;
    private Vector2 _lastPanPosition;
    private ResourceRenderCache _resourceRenderCache = new();
    private ulong _resourceCacheLastRefreshMilliseconds;
    private ResourceRenderMode _resourceRenderMode = ResourceRenderMode.Individual;
    private int _visibleResourceEstimate;
    private CreatureRenderCache _creatureRenderCache = new();
    private ulong _creatureCacheLastRefreshMilliseconds;
    private CreatureRenderMode _creatureRenderMode = CreatureRenderMode.Individual;
    private int _visibleCreatureEstimate;
    private int _drawnResourceCount;
    private int _drawnResourceAggregateCount;
    private int _drawnCreatureCount;
    private int _drawnCreatureAggregateCount;
    private double _telemetryWindowSeconds;
    private int _telemetryFrameCount;
    private int _telemetryStepCount;
    private float _measuredTicksPerSecond;
    private float _measuredFrameMilliseconds;

    private Rect2 _worldRect;
    private Rect2 _graphRect;
    private Rect2 _scaleBarRect;
    private SimVector2 _viewCenter;
    private float _fitWorldScale = 1f;
    private float _worldScale = 1f;
    private float _viewZoom = 1f;
    private float _scaleBarUnits = 100f;

    public override void _Ready()
    {
        _hud = CreateLabel(new Vector2(16f, 12f), Colors.White);
        _inspectorScroll = new ScrollContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _inspector = CreateLabel(Vector2.Zero, new Color(0.9f, 0.92f, 0.88f));
        _inspector.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _graphLegend = CreateLabel(Vector2.Zero, new Color(0.9f, 0.92f, 0.88f));
        _scaleBarLabel = CreateLabel(Vector2.Zero, Colors.White);
        _scaleBarLabel.HorizontalAlignment = HorizontalAlignment.Center;
        LoadStartupScenario();
        _currentSeed = _scenario.Seed;

        AddChild(_hud);
        AddChild(_inspectorScroll);
        _inspectorScroll.AddChild(_inspector);
        AddChild(_graphLegend);
        AddChild(_scaleBarLabel);
        CreateScenarioLauncher();
        _scenarioEditor.SetScenario(_scenario);

        ResetSimulation(resetView: true);
    }

    public override void _Process(double delta)
    {
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
        HandleKeyboardCamera((float)delta);
        UpdateLayout();
        UpdateFollowCamera();
        UpdateLabels();
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(GetViewportRect(), _backgroundColor, filled: true);
        DrawRect(_worldRect, _worldColor, filled: true);
        if (_showBiomeOverlay)
        {
            DrawBiomeOverlay();
        }

        DrawRect(new Rect2(_worldRect.Position + new Vector2(_worldRect.Size.X + 12f, 0f), new Vector2(RightPanelWidth, _worldRect.Size.Y)), _panelColor, filled: true);

        DrawResources();
        DrawEggs();
        DrawCreatures();
        DrawSelectedEggOverlay();

        DrawStatsGraph();
        DrawScaleBar();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            switch (keyEvent.Keycode)
            {
                case Key.P:
                case Key.Space:
                    _paused = !_paused;
                    break;
                case Key.R:
                    ResetSimulation(resetView: false);
                    break;
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
                case Key.F:
                    _followSelected = false;
                    ResetView();
                    break;
                case Key.G:
                    ToggleFollowSelected();
                    break;
                case Key.B:
                    _showBiomeOverlay = !_showBiomeOverlay;
                    break;
                case Key.C:
                    CycleColorMode();
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
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Left when mouseButton.Pressed:
                    SelectEntityAt(mouseButton.Position);
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
                    break;
                case MouseButton.WheelDown when mouseButton.Pressed:
                    ZoomAt(mouseButton.Position, 1f / 1.2f);
                    break;
            }
        }

        if (@event is InputEventMouseMotion mouseMotion && _isPanning)
        {
            var delta = mouseMotion.Position - _lastPanPosition;
            _viewCenter -= ToWorldDelta(delta);
            ClampViewCenter();
            _lastPanPosition = mouseMotion.Position;
        }
    }

    private void ResetSimulation(bool resetView)
    {
        _scenario = _scenario with { Seed = _currentSeed };
        _simulation = SimulationScenarioFactory.CreateSimulation(_scenario);

        ClearSelection();
        _followSelected = false;
        _stepAccumulator = 0;
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
    }

    private void SetSpeedMultiplier(float speedMultiplier)
    {
        var previousSpeed = _speedMultiplier;
        _speedMultiplier = Math.Clamp(speedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);

        if (_speedMultiplier < previousSpeed)
        {
            _stepAccumulator = Math.Min(_stepAccumulator, _simulation.Config.FixedDeltaSeconds);
        }
    }

    private void LaunchScenarioFromEditor()
    {
        if (!_scenarioEditor.TryReadScenario(out var scenario, out var error))
        {
            _scenarioEditor.SetStatus($"Launch failed: {error}");
            return;
        }

        _scenario = scenario;
        _currentSeed = scenario.Seed;
        _paused = false;
        ResetSimulation(resetView: true);
        _scenarioEditor.SetStatus($"Launched {scenario.Name}.");
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
        try
        {
            _scenario = SimulationScenarioJson.Load(path);
            _currentSeed = _scenario.Seed;
            _currentScenarioPath = path;
            _scenarioEditor.SetScenario(_scenario);
            ResetSimulation(resetView: true);
            _scenarioEditor.SetStatus($"Loaded {System.IO.Path.GetFileName(path)}.");
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
        var temporaryScenarioPath = System.IO.Path.Combine(workspaceRoot, "out", "godot_cli_scenario.json");
        var outputPath = ResolveWorkspacePath(request.OutputPath, workspaceRoot);
        var reportPath = ResolveWorkspacePath(request.ReportPath, workspaceRoot);
        var snapshotPath = ResolveWorkspacePath(request.SnapshotPath, workspaceRoot);
        var checkpointDirectory = request.CheckpointIntervalTicks > 0
            ? ResolveWorkspacePath(request.CheckpointDirectory, workspaceRoot)
            : string.Empty;

        _scenario = scenario;
        _currentSeed = scenario.Seed;
        _scenarioEditor.SetScenario(_scenario);
        _scenarioEditor.SetStatus("CLI running...");
        _scenarioEditor.SetLastReportPath(null);
        _scenarioEditor.SetLastSnapshotPath(null);
        _scenarioEditor.SetLastCheckpointPath(null, request.CheckpointIntervalTicks > 0 ? checkpointDirectory : null);
        _cliRunInProgress = true;

        _ = RunCliAsync(
            scenario,
            request.Ticks,
            workspaceRoot,
            cliProjectPath,
            temporaryScenarioPath,
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
        var leftEdge = _scenarioEditor.Visible
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
        var rightPanelContentX = rightPanelPosition.X + 12f;
        var rightPanelContentWidth = RightPanelWidth - 24f;
        var graphHeight = 150f;
        var graphLegendHeight = 54f;
        var graphBottomMargin = 10f;
        _graphRect = new Rect2(
            new Vector2(rightPanelContentX, rightPanelPosition.Y + _worldRect.Size.Y - graphHeight - graphBottomMargin),
            new Vector2(rightPanelContentWidth, graphHeight));
        _graphLegend.Position = new Vector2(rightPanelContentX, _graphRect.Position.Y - graphLegendHeight);
        _graphLegend.Size = new Vector2(rightPanelContentWidth, graphLegendHeight);

        _inspectorScroll.Position = new Vector2(rightPanelContentX, rightPanelPosition.Y + 8f);
        _inspectorScroll.Size = new Vector2(
            rightPanelContentWidth,
            MathF.Max(120f, _graphLegend.Position.Y - _inspectorScroll.Position.Y - 10f));
        _inspector.Size = new Vector2(rightPanelContentWidth - 18f, 900f);
        _inspector.CustomMinimumSize = new Vector2(rightPanelContentWidth - 18f, 0f);

        if (_scenarioEditor.Visible && _scenarioEditor.IsCollapsed)
        {
            _hud.Position = new Vector2(16f, 84f);
            _hud.Size = new Vector2(CollapsedLauncherPanelWidth - 24f, 340f);
        }
        else if (_scenarioEditor.Visible)
        {
            _hud.Position = _worldRect.Position + new Vector2(12f, 12f);
            _hud.Size = new Vector2(300f, 340f);
        }
        else
        {
            _hud.Position = new Vector2(16f, 12f);
            _hud.Size = new Vector2(300f, 340f);
        }

        _scenarioEditor.Position = new Vector2(12f, 12f);
        _scenarioEditor.Size = _scenarioEditor.IsCollapsed
            ? new Vector2(CollapsedLauncherPanelWidth, 56f)
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
    }

    private void UpdateLabels()
    {
        var state = _simulation.State;
        var snapshot = state.Stats.Snapshots.Count > 0 ? state.Stats.Snapshots[^1] : default;
        var worldArea = MathF.Max(1f, state.Bounds.Width * state.Bounds.Height);
        var resourceDensity = state.Resources.Count / worldArea * 1_000_000f;
        var centerBiome = state.Biomes.GetKindAt(_viewCenter);
        var centerVoidText = state.Biomes.IsInResourceVoid(_viewCenter) ? " void" : string.Empty;
        var launcherHint = _scenarioEditor.IsCollapsed
            ? "S expands launcher"
            : "S collapses launcher";

        _hud.Text =
            $"Lineage\n" +
            $"{(_paused ? "Paused" : "Running")}  {FormatSpeed(_speedMultiplier)}\n" +
            $"TPS {_measuredTicksPerSecond:0.0}  Frame {_measuredFrameMilliseconds:0.0}ms\n" +
            $"Seed {_currentSeed}\n" +
            $"Tick {state.Tick}  Time {state.ElapsedSeconds:0.0}s\n" +
            $"World {state.Bounds.Width:0}x{state.Bounds.Height:0}\n" +
            $"Creatures {state.Creatures.Count}  Eggs {state.Eggs.Count}  Food {state.Resources.Count}\n" +
            $"Plants {snapshot.PlantResourceCount}  Meat {snapshot.MeatResourceCount}\n" +
            $"Resources/M {resourceDensity:0.00}\n" +
            $"Births {state.Stats.CreatureBirthCount}  Eggs laid {state.Stats.EggLaidCount}\n" +
            $"Hatched {state.Stats.EggHatchedCount}  Egg deaths {state.Stats.EggDeathCount}  Pred {state.Stats.EggPredationDeathCount}\n" +
            $"Egg health {snapshot.AverageEggHealthRatio * 100f:0}%  Birth inv {snapshot.AverageBirthInvestmentRatio:0.00}x\n" +
            $"Deaths {state.Stats.CreatureDeathCount}  Starved {state.Stats.StarvationDeathCount}\n" +
            $"Max gen {snapshot.MaxGeneration}\n" +
            $"Food seen {FormatPercent(Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount))}  P {FormatPercent(Share(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount))}  M {FormatPercent(Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount))}\n" +
            $"Meat scent {FormatPercent(Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount))}  density {snapshot.AverageMeatScentDensity:0.00}\n" +
            $"Eating {FormatPercent(Share(snapshot.EatingCreatureCount, snapshot.CreatureCount))}  Raw {snapshot.TotalCaloriesEatenPerSecond:0.0}/s  Digest {snapshot.TotalCaloriesDigestedPerSecond:0.0}/s\n" +
            $"Food src P {snapshot.TotalPlantCaloriesEatenPerSecond:0.0}/s  C {snapshot.TotalCarcassCaloriesEatenPerSecond:0.0}/s  Egg {snapshot.TotalEggCaloriesEatenPerSecond:0.0}/s\n" +
            $"Creatures seen {FormatPercent(Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount))}  density {snapshot.AverageVisibleCreatureDensity:0.00}\n" +
            $"Attacking {FormatPercent(Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount))}  Dmg {snapshot.TotalAttackDamagePerSecond:0.00}/s  Fresh kill {snapshot.TotalLivePreyCaloriesEatenPerSecond:0.0}/s\n" +
            $"Meal gap {snapshot.AverageSecondsSinceLastMeal:0.0}s  Vision {snapshot.AverageVisionRange:0}/{ToDegrees(snapshot.AverageVisionAngleRadians):0}deg\n" +
            $"Search {snapshot.TotalDistanceTraveledPerSecond:0}u/s  meal dist {snapshot.AverageDistanceSinceLastMeal:0}u  kcal/u {snapshot.CaloriesEatenPerDistance:0.00}\n" +
            $"Zoom {_viewZoom:0.00}x  Follow {(_followSelected ? "on" : "off")}\n" +
            $"Food {FormatResourceRenderMode(_resourceRenderMode)} v{_visibleResourceEstimate} d{FormatDrawCount(_drawnResourceCount, _drawnResourceAggregateCount)}\n" +
            $"Creatures {FormatCreatureRenderMode(_creatureRenderMode)} v{_visibleCreatureEstimate} d{FormatDrawCount(_drawnCreatureCount, _drawnCreatureAggregateCount)}\n" +
            $"Biome {FormatBiomeKind(centerBiome)}{centerVoidText} {(_showBiomeOverlay ? "shown" : "hidden")}\n" +
            $"Biome pop B {FormatPercent(Share(snapshot.BarrenCreatureCount, snapshot.CreatureCount))} S {FormatPercent(Share(snapshot.SparseCreatureCount, snapshot.CreatureCount))} G {FormatPercent(Share(snapshot.GrasslandCreatureCount, snapshot.CreatureCount))} R {FormatPercent(Share(snapshot.RichCreatureCount, snapshot.CreatureCount))}\n" +
            $"Biome cost move {snapshot.AverageBiomeMovementCostMultiplier:0.00}x basal {snapshot.AverageBiomeBasalCostMultiplier:0.00}x\n" +
            $"Color {FormatColorMode(_colorMode)}\n" +
            $"Arrows pan  G follows\n" +
            $"B toggles biomes\n" +
            $"C changes color mode\n" +
            $"{launcherHint}";

        _inspector.Text = BuildInspectorText();
        _graphLegend.Text =
            $"Population {state.Creatures.Count}\n" +
            $"Food kcal {snapshot.TotalResourceCalories:0}\n" +
            $"Plant kcal {snapshot.TotalPlantCalories:0}  Meat kcal {snapshot.TotalMeatCalories:0}\n" +
            $"Digested {snapshot.TotalCaloriesDigestedPerSecond:0.0}/s  Gut {snapshot.AverageGutFillRatio * 100f:0}%\n" +
            $"Seen F {FormatPercent(Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount))}  C {FormatPercent(Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount))}\n" +
            $"Attack {FormatPercent(Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount))}  Dmg {snapshot.TotalAttackDamagePerSecond:0.0}/s\n" +
            $"Eat {FormatPercent(Share(snapshot.EatingCreatureCount, snapshot.CreatureCount))}  Fresh kill {snapshot.TotalLivePreyCaloriesEatenPerSecond:0.0}/s\n" +
            $"Deaths {state.Stats.CreatureDeathCount}";
    }

    private string BuildInspectorText()
    {
        if (_selectedCreatureId == default && _selectedEggId == default)
        {
            return "Selected\nNone";
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
            return "Selected\nNone";
        }

        if (_selectedEggId != default && TryGetSelectedEgg(out var egg))
        {
            return BuildEggInspectorText(egg);
        }

        ClearSelection();
        return "Selected\nNone";
    }

    private string BuildCreatureInspectorText(CreatureState creature)
    {
        var genome = _simulation.State.GetGenome(creature.GenomeId);
        var senses = creature.Senses;
        var maturityProgress = CreatureGrowth.MaturityProgress(creature, genome);
        var growthFactor = CreatureGrowth.GrowthFactor(creature, genome);
        var gutCapacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
        var gutTotal = creature.GutPlantCalories + creature.GutMeatCalories;
        var gutFillRatio = gutCapacity > 0f
            ? Math.Clamp(gutTotal / gutCapacity, 0f, 1f)
            : 0f;
        _simulation.State.TryGetLineageRecord(creature.Id, out var lineage);
        var parentText = lineage.IsFounder ? "Founder" : $"Parent #{lineage.ParentId.Value}";
        var maturityText = CreatureGrowth.IsMature(creature, genome)
            ? "adult"
            : $"juvenile {maturityProgress:P0}";
        var biome = _simulation.State.Biomes.GetKindAt(creature.Position);
        var movementCostMultiplier = _scenario.CreateBiomeMovementCostProfile().For(biome);
        var basalCostMultiplier = _scenario.CreateBiomeBasalCostProfile().For(biome);

        return
            $"Selected #{creature.Id.Value}\n" +
            $"{parentText}\n" +
            $"Generation {creature.Generation}\n" +
            $"Genome {creature.GenomeId}  Brain {creature.BrainId}\n" +
            $"Energy {creature.Energy:0.0}\n" +
            $"Health {creature.Health:0.00}\n" +
            $"Age {creature.AgeSeconds:0.0}s\n" +
            $"Growth {maturityText} ({growthFactor:P0})\n" +
            $"Birth inv {creature.BirthInvestmentRatio:0.00}x\n" +
            $"Biome {FormatBiomeKind(biome)}  move {movementCostMultiplier:0.00}x basal {basalCostMultiplier:0.00}x\n" +
            $"Max speed {CreatureGrowth.EffectiveMaxSpeed(creature, genome):0.0}/{genome.MaxSpeed:0.0}\n" +
            $"Actual speed {creature.Velocity.Length:0.0}\n" +
            $"Desired speed {creature.DesiredVelocity.Length:0.0}\n" +
            $"Speed cost {MovementSystem.CalculateSpeedCostMultiplier(creature.Velocity.Length, _scenario.MovementSpeedCostExponent):0.00}x\n" +
            $"Turn {CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome):0.0}/{genome.MaxTurnRadiansPerSecond:0.0}\n" +
            $"Vision range {CreatureGrowth.EffectiveSenseRadius(creature, genome):0.0}/{genome.SenseRadius:0.0}\n" +
            $"Vision angle {ToDegrees(CreatureGrowth.EffectiveVisionAngleRadians(creature, genome)):0}deg/{ToDegrees(genome.VisionAngleRadians):0}deg\n" +
            $"Body {CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}/{genome.BodyRadius:0.0}\n" +
            $"Eat rate {CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome):0.0}/{genome.EatCaloriesPerSecond:0.0}\n" +
            $"Diet meat bias {genome.DietaryAdaptation:0.00}\n" +
            $"Digest plant {CreatureDigestion.PlantEfficiency(genome):P0}  meat {CreatureDigestion.MeatEfficiency(genome):P0}\n" +
            $"Digest rate {CreatureGrowth.EffectiveDigestionCaloriesPerSecond(creature, genome):0.0}/{genome.DigestionCaloriesPerSecond:0.0}\n" +
            $"Gut cap {gutCapacity:0.0}/{genome.GutCapacityCalories:0.0}\n" +
            $"Gut {gutTotal:0.0}/{gutCapacity:0.0} ({gutFillRatio:P0})\n" +
            $"Gut plant {creature.GutPlantCalories:0.0}  meat {creature.GutMeatCalories:0.0}\n" +
            $"Bite str {CreatureGrowth.EffectiveBiteStrength(creature, genome):0.00}/{genome.BiteStrength:0.00}\n" +
            $"Damage resist {CreatureGrowth.EffectiveDamageResistance(creature, genome):0.00}/{genome.DamageResistance:0.00}\n" +
            $"Egg reserve {creature.ReproductiveEnergy:0.0}/{genome.OffspringEnergyInvestment:0.0}\n" +
            $"Egg build {genome.EggProductionEnergyPerSecond:0.0}/s\n" +
            $"Lay ready {(senses.ReproductionReadiness > 0.5f ? "yes" : "no")}\n" +
            $"Egg incubation {genome.EggIncubationSeconds:0.0}s\n" +
            $"Food contact {(creature.IsTouchingFood ? "yes" : "no")}\n" +
            BuildFoodContactText(creature, genome) +
            $"Last meal {BuildLastMealSourceText(creature)}\n" +
            $"Swallowed this tick {creature.LastCaloriesEaten:0.00} raw ({FormatCaloriesPerSecond(creature.LastCaloriesEaten)}/s)\n" +
            $"Source P {creature.LastPlantCaloriesEaten:0.00}  C {creature.LastCarcassCaloriesEaten:0.00}  Egg {creature.LastEggCaloriesEaten:0.00}  FK {creature.LastLivePreyCaloriesEaten:0.00}\n" +
            $"Digested this tick {creature.LastCaloriesDigested:0.00} energy ({FormatCaloriesPerSecond(creature.LastCaloriesDigested)}/s)\n" +
            $"Energy P {creature.LastPlantDigestedEnergy:0.00}  M {creature.LastMeatDigestedEnergy:0.00}\n" +
            $"Creature contact {(creature.IsTouchingCreature ? $"#{creature.CreatureContactId.Value} edge {creature.CreatureContactEdgeDistance:0.0}" : "no")}\n" +
            $"Attack dmg {creature.LastAttackDamageDealt:0.000}\n" +
            $"Since meal {creature.SecondsSinceLastMeal:0.0}s  {creature.DistanceSinceLastMeal:0.0}u\n" +
            $"Moved last tick {creature.LastDistanceTraveled:0.00}u\n" +
            $"Repro at {genome.ReproductionEnergyThreshold:0.0}\n" +
            $"Mature at {genome.MaturityAgeSeconds:0.0}s\n" +
            $"Mutation {genome.MutationStrength:0.000}\n" +
            $"Trait mut {genome.TraitMutationRate:P0}\n" +
            $"Brain mut {genome.BrainMutationRate:P0}\n" +
            $"Cooldown {creature.ReproductionCooldownSeconds:0.0}s\n\n" +
            $"Food {(senses.FoodDetected ? "yes" : "no")}\n" +
            $"Visible density {senses.VisibleFoodDensity:0.00}\n" +
            $"Proximity {senses.FoodProximity:0.00}\n" +
            $"Forward {senses.FoodDirectionForward:0.00}\n" +
            $"Right {senses.FoodDirectionRight:0.00}\n" +
            $"Plants {(senses.PlantDetected ? "yes" : "no")}  density {senses.VisiblePlantDensity:0.00}\n" +
            $"Plant prox {senses.PlantProximity:0.00}  fwd {senses.PlantDirectionForward:0.00}  right {senses.PlantDirectionRight:0.00}\n" +
            $"Meat {(senses.MeatDetected ? "yes" : "no")}  density {senses.VisibleMeatDensity:0.00}\n" +
            $"Meat prox {senses.MeatProximity:0.00}  fwd {senses.MeatDirectionForward:0.00}  right {senses.MeatDirectionRight:0.00}\n" +
            $"Meat scent {(senses.MeatScentDetected ? "yes" : "no")}  density {senses.MeatScentDensity:0.00}\n" +
            $"Scent fwd {senses.MeatScentDirectionForward:0.00}  right {senses.MeatScentDirectionRight:0.00}\n" +
            $"Creature {(senses.CreatureDetected ? "yes" : "no")}  density {senses.VisibleCreatureDensity:0.00}\n" +
            $"Creature prox {senses.CreatureProximity:0.00}  fwd {senses.CreatureDirectionForward:0.00}  right {senses.CreatureDirectionRight:0.00}\n" +
            $"Creature size {senses.CreatureRelativeBodySize:0.00}  speed {senses.CreatureRelativeSpeed:0.00}  approach {senses.CreatureApproachRate:0.00}\n" +
            $"Creature facing {senses.CreatureFacingAlignment:0.00}\n\n" +
            $"Move {creature.Actions.MoveForward:0.00}\n" +
            $"Turn {creature.Actions.Turn:0.00}\n" +
            $"Eat intent {creature.Actions.WantsEat}\n" +
            $"Attack intent {creature.Actions.WantsAttack}\n" +
            $"Reproduce {creature.Actions.WantsReproduce}";
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

        return
            $"Selected egg #{egg.Id.Value}\n" +
            $"Parent #{egg.ParentId.Value}\n" +
            $"Generation {egg.Generation}\n" +
            $"Genome {egg.GenomeId}  Brain {egg.BrainId}\n" +
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

                return
                    $"Food type {FormatResourceKind(resource.Kind)}\n" +
                    $"Digest eff {CreatureDigestion.EfficiencyFor(genome, resource.Kind):P0}\n" +
                    $"Food edge {creature.FoodContactEdgeDistance:0.0}/{CreatureGrowth.EffectiveBodyRadius(creature, genome):0.0}\n" +
                    $"Food kcal {creature.FoodContactCalories:0.00}\n";
            }
        }

        if (creature.FoodContactKind == FoodContactKind.Egg)
        {
            return
                $"Food type egg\n" +
                $"Digest eff {CreatureDigestion.MeatEfficiency(genome):P0}\n" +
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

        if (creature.LastLivePreyCaloriesEaten > amount)
        {
            source = "fresh kill";
            amount = creature.LastLivePreyCaloriesEaten;
        }

        return $"{source} {amount:0.00} raw";
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
                var color = ColorForResource(resource.Kind, fullness);
                var screenPosition = ToScreen(resource.Position);
                var radius = MathF.Max(2f, resource.Radius * _worldScale);
                DrawCircle(screenPosition, radius, color);
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
                var color = new Color(0.17f + fullness * 0.08f, 0.72f, 0.30f, alpha)
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

            DrawCircle(screenPosition, radius, color);
            DrawArc(screenPosition, radius + 1.5f, -MathF.PI * 0.5f, -MathF.PI * 0.5f + MathF.Tau * hatchProgress, 18, _selectedColor, width: 1f);
        }
    }

    private void DrawCreatures()
    {
        UpdateCreatureRenderCache();
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
                var color = new Color(0.88f, 0.76f - generationRatio * 0.18f, 0.28f + densityTint * 0.18f, alpha);
                var center = clippedScreenRect.Position + clippedScreenRect.Size * 0.5f;
                var maxRadius = MathF.Max(3f, MathF.Min(clippedScreenRect.Size.X, clippedScreenRect.Size.Y) * 0.42f);
                var radius = Math.Clamp(3f + MathF.Sqrt(summary.CreatureCount) * 0.55f, 3f, maxRadius);
                DrawCircle(center, radius, color);
                _drawnCreatureAggregateCount++;
            }
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

        DrawCircle(screenPosition, radius, color);
        DrawLine(
            screenPosition,
            screenPosition + ToGodot(SimVector2.FromAngle(creature.HeadingRadians)) * (radius + 7f),
            Colors.Black,
            width: 1.5f);

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
        if (!IsVisibleInWorldRect(screenPosition, senseRadius * _worldScale + 8f))
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
        DrawArc(screenPosition, radius + 5f, 0f, MathF.Tau, 40, _selectedColor, width: 2f);
        DrawSelectedFoodContact(creature, screenPosition);
        DrawSelectedCreatureContact(creature, screenPosition);
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
        var fixedDelta = MathF.Max(_simulation.Config.FixedDeltaSeconds, 0.0001f);
        return (calories / fixedDelta).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private Color ColorForResource(ResourceKind kind, float fullness)
    {
        return kind switch
        {
            ResourceKind.Meat => _meatResourceColor.Lerp(new Color(0.20f, 0.10f, 0.09f), 1f - fullness),
            _ => _resourceColor.Lerp(new Color(0.14f, 0.26f, 0.16f), 1f - fullness)
        };
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
            CreatureColorMode.Energy => ColorForEnergy(creature.Energy, genome.ReproductionEnergyThreshold),
            CreatureColorMode.Age => ColorForAge(creature.AgeSeconds),
            _ => ColorForGeneration(creature.Generation)
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
        var ratio = reproductionThreshold > 0f
            ? Mathf.Clamp(energy / reproductionThreshold, 0f, 1.5f) / 1.5f
            : 0f;
        return new Color(0.9f, 0.24f, 0.22f).Lerp(new Color(0.25f, 0.9f, 0.42f), ratio);
    }

    private static Color ColorForAge(float ageSeconds)
    {
        var ratio = Mathf.Clamp(ageSeconds / 900f, 0f, 1f);
        return new Color(0.53f, 0.88f, 0.94f).Lerp(new Color(0.94f, 0.56f, 0.86f), ratio);
    }

    private void DrawBiomeOverlay()
    {
        var map = _simulation.State.Biomes;
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                var topLeft = ToScreen(new SimVector2(cell.X, cell.Y));
                var bottomRight = ToScreen(new SimVector2(cell.X + cell.Width, cell.Y + cell.Height));
                var rect = RectFromPoints(topLeft, bottomRight);
                if (!TryClipRect(rect, _worldRect, out var clipped))
                {
                    continue;
                }

                DrawRect(clipped, ColorForBiome(map.GetKind(x, y)), filled: true);
                if (clipped.Size.X > 14f && clipped.Size.Y > 14f)
                {
                    DrawRect(clipped, new Color(0f, 0f, 0f, 0.055f), filled: false, width: 1f);
                }
            }
        }

        DrawResourceVoidOverlay(map);
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
        return biome switch
        {
            BiomeKind.Barren => new Color(0.42f, 0.29f, 0.16f, 0.30f),
            BiomeKind.Sparse => new Color(0.34f, 0.39f, 0.17f, 0.26f),
            BiomeKind.Rich => new Color(0.05f, 0.48f, 0.23f, 0.30f),
            _ => new Color(0.12f, 0.34f, 0.17f, 0.22f)
        };
    }

    private void DrawStatsGraph()
    {
        DrawRect(_graphRect, new Color(0.035f, 0.04f, 0.038f), filled: true);
        DrawRect(_graphRect, new Color(0.22f, 0.25f, 0.23f), filled: false, width: 1f);

        var snapshots = _simulation.State.Stats.Snapshots;
        if (snapshots.Count < 2)
        {
            return;
        }

        var sampleCount = Math.Min(GraphSampleCount, snapshots.Count);
        var startIndex = snapshots.Count - sampleCount;
        var maxPopulation = 1f;
        var maxResourceCalories = 1f;
        var maxDeaths = 1f;

        for (var i = startIndex; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            maxPopulation = MathF.Max(maxPopulation, snapshot.CreatureCount);
            maxResourceCalories = MathF.Max(maxResourceCalories, snapshot.TotalResourceCalories);
            maxDeaths = MathF.Max(maxDeaths, snapshot.CreatureDeathCount);
        }

        DrawGraphSeries(startIndex, sampleCount, maxResourceCalories, _graphResourceColor, GraphMetric.ResourceCalories);
        DrawGraphSeries(startIndex, sampleCount, maxPopulation, _graphPopulationColor, GraphMetric.Population);
        DrawGraphSeries(startIndex, sampleCount, maxDeaths, _graphDeathColor, GraphMetric.Deaths);
    }

    private void DrawGraphSeries(int startIndex, int sampleCount, float maxValue, Color color, GraphMetric metric)
    {
        if (sampleCount < 2 || maxValue <= 0f)
        {
            return;
        }

        var previous = GetGraphPoint(startIndex, startIndex, sampleCount, maxValue, metric);
        for (var sample = 1; sample < sampleCount; sample++)
        {
            var index = startIndex + sample;
            var current = GetGraphPoint(index, startIndex, sampleCount, maxValue, metric);
            DrawLine(previous, current, color, width: 2f);
            previous = current;
        }
    }

    private Vector2 GetGraphPoint(int snapshotIndex, int startIndex, int sampleCount, float maxValue, GraphMetric metric)
    {
        var snapshot = _simulation.State.Stats.Snapshots[snapshotIndex];
        var sampleOffset = snapshotIndex - startIndex;
        var xRatio = sampleCount <= 1 ? 0f : sampleOffset / (float)(sampleCount - 1);
        var value = metric switch
        {
            GraphMetric.Population => snapshot.CreatureCount,
            GraphMetric.ResourceCalories => snapshot.TotalResourceCalories,
            GraphMetric.Deaths => snapshot.CreatureDeathCount,
            _ => 0f
        };

        var yRatio = Math.Clamp(value / maxValue, 0f, 1f);
        return new Vector2(
            _graphRect.Position.X + xRatio * _graphRect.Size.X,
            _graphRect.Position.Y + _graphRect.Size.Y - yRatio * _graphRect.Size.Y);
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
        for (var i = 0; i < _simulation.State.Creatures.Count; i++)
        {
            if (_simulation.State.Creatures[i].Id == _selectedCreatureId)
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
        if (_worldScale <= 0f || _worldRect.Size.X <= 0f)
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
        _scenarioEditor.CloseRequested += _scenarioEditor.ToggleCollapsed;
        AddChild(_scenarioEditor);

        var scenarioDirectory = System.IO.Path.Combine(GetRepositoryRoot(), "scenarios");
        _loadScenarioDialog = CreateScenarioDialog(FileDialog.FileModeEnum.OpenFile, "Load Scenario", scenarioDirectory);
        _saveScenarioDialog = CreateScenarioDialog(FileDialog.FileModeEnum.SaveFile, "Save Scenario", scenarioDirectory);
        _loadSnapshotDialog = CreateSnapshotDialog(System.IO.Path.Combine(GetRepositoryRoot(), "out"));
        _loadScenarioDialog.FileSelected += LoadScenarioFromPath;
        _saveScenarioDialog.FileSelected += SaveScenarioToPath;
        _loadSnapshotDialog.FileSelected += LoadSnapshotFromPath;
        AddChild(_loadScenarioDialog);
        AddChild(_saveScenarioDialog);
        AddChild(_loadSnapshotDialog);
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
        try
        {
            var restored = SimulationSnapshotJson.LoadSimulation(path);
            _scenario = restored.Scenario;
            _simulation = restored.Simulation;
            _currentSeed = _scenario.Seed;
            ClearSelection();
            _followSelected = false;
            _stepAccumulator = 0;
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

    private void WriteCurrentReportFromEditor()
    {
        try
        {
            var workspaceRoot = GetRepositoryRoot();
            var request = _scenarioEditor.ReadCliRunRequest();
            var reportPath = ResolveWorkspacePath(request.ReportPath, workspaceRoot);

            ViewerReportWriter.Write(reportPath, _scenario, _simulation);
            _scenarioEditor.SetLastReportPath(reportPath);
            _scenarioEditor.SetStatus($"Viewer report written: {System.IO.Path.GetFileName(reportPath)}");
        }
        catch (Exception ex)
        {
            _scenarioEditor.SetLastReportPath(null);
            _scenarioEditor.SetStatus($"Report failed: {ex.Message}");
        }
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

    private static string GetRepositoryRoot()
    {
        var projectDirectory = ProjectSettings.GlobalizePath("res://");
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDirectory, "..", ".."));
    }

    private float GetLauncherWidth()
    {
        if (!_scenarioEditor.Visible)
        {
            return 0f;
        }

        return _scenarioEditor.IsCollapsed
            ? CollapsedLauncherPanelWidth
            : LauncherPanelWidth;
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

    private static Color ColorForGeneration(int generation)
    {
        var hue = Mathf.PosMod(generation * 0.075f + 0.13f, 1f);
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

    private static string FormatBiomeKind(BiomeKind biome)
    {
        return biome.ToString().ToLowerInvariant();
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
    }

    private readonly record struct ResourceChunkRange(int MinX, int MinY, int MaxX, int MaxY);

    private struct ResourceAggregateSummary
    {
        public int DrawableResourceCount;

        public float TotalCalories;

        public float TotalMaxCalories;

        public float MeatCalories;
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

    private enum CreatureColorMode
    {
        Generation,
        FounderLineage,
        Energy,
        Age
    }

    private enum GraphMetric
    {
        Population,
        ResourceCalories,
        Deaths
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
}
