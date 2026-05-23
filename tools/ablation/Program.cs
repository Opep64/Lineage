using System.Globalization;
using System.Text.Json;
using Lineage.Core;

var options = AblationOptions.Parse(args);
if (options.ShowHelp)
{
    PrintHelp();
    return;
}

try
{
    var snapshot = SimulationSnapshotJson.Load(options.SnapshotPath);
    var report = MemoryAblationReport.From(snapshot);

    if (options.Json)
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        WriteOutput(options.OutputPath, json);
    }
    else
    {
        WriteOutput(options.OutputPath, report.ToMarkdown());
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Lineage.Ablation - snapshot behavior ablation tools

        Usage:
          dotnet run --project tools/ablation -- --snapshot out/godot_launcher_snapshot.json

        Options:
          --snapshot <path>  Required snapshot JSON to analyze.
          --output <path>    Optional output file. Defaults to stdout.
          --json             Emit JSON instead of Markdown.
          --help             Show this help.
        """);
}

static void WriteOutput(string? outputPath, string text)
{
    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine(text);
        return;
    }

    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(outputPath, text);
    Console.WriteLine($"Wrote {Path.GetFullPath(outputPath)}");
}

internal sealed record AblationOptions(string SnapshotPath, string? OutputPath, bool Json, bool ShowHelp)
{
    public static AblationOptions Parse(string[] args)
    {
        var snapshotPath = "";
        string? outputPath = null;
        var json = false;
        var showHelp = args.Length == 0;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--snapshot":
                    snapshotPath = RequireValue(args, ref i, "--snapshot");
                    break;
                case "--output":
                    outputPath = RequireValue(args, ref i, "--output");
                    break;
                case "--json":
                    json = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'. Use --help for usage.");
            }
        }

        if (!showHelp && string.IsNullOrWhiteSpace(snapshotPath))
        {
            throw new ArgumentException("--snapshot is required. Use --help for usage.");
        }

        return new AblationOptions(snapshotPath, outputPath, json, showHelp);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        return args[index];
    }
}

internal sealed record MemoryAblationReport(
    string Scenario,
    long Tick,
    double ElapsedSeconds,
    int CreatureCount,
    int EvaluatedCreatureCount,
    int HiddenNodeCount,
    Summary MemoryStrength,
    Summary MemoryForwardInput,
    Summary MemoryRightInput,
    Summary MemoryWriteStrength,
    Summary MemoryWriteForward,
    Summary MemoryWriteRight,
    AblationGateChanges GateChanges,
    IReadOnlyList<OutputAblationSummary> OutputSummaries,
    IReadOnlyList<AlignmentSummary> Alignments)
{
    public static MemoryAblationReport From(SimulationSnapshot snapshot)
    {
        var evaluator = new SnapshotMemoryAblation(snapshot);
        return evaluator.Run();
    }

    public string ToMarkdown()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine($"# Memory Ablation Report");
        writer.WriteLine();
        writer.WriteLine($"Scenario: `{Scenario}`");
        writer.WriteLine($"Tick: `{Tick}`  Elapsed: `{ElapsedSeconds:0.###}s`");
        writer.WriteLine($"Creatures evaluated: `{EvaluatedCreatureCount}` / `{CreatureCount}`");
        writer.WriteLine($"Hidden nodes: `{HiddenNodeCount}`");
        writer.WriteLine();
        writer.WriteLine("## Memory State");
        writer.WriteLine();
        writer.WriteLine("| Metric | Mean | Median | P10 | P90 |");
        writer.WriteLine("|---|---:|---:|---:|---:|");
        WriteSummaryRow(writer, "Strength", MemoryStrength);
        WriteSummaryRow(writer, "Forward input", MemoryForwardInput);
        WriteSummaryRow(writer, "Right input", MemoryRightInput);
        WriteSummaryRow(writer, "Write strength", MemoryWriteStrength);
        WriteSummaryRow(writer, "Write forward", MemoryWriteForward);
        WriteSummaryRow(writer, "Write right", MemoryWriteRight);
        writer.WriteLine();
        writer.WriteLine("## Output Changes");
        writer.WriteLine();
        writer.WriteLine("| Output | Actual mean | Zero-memory mean | Memory-only mean | Mean delta | Mean abs delta | Changed > 0.1 |");
        writer.WriteLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var output in OutputSummaries)
        {
            writer.WriteLine(
                $"| {output.Name} | {output.Actual.Mean:0.###} | {output.ZeroMemory.Mean:0.###} | {output.MemoryOnly.Mean:0.###} | {output.SignedDelta.Mean:0.###} | {output.AbsoluteDelta.Mean:0.###} | {output.ChangedAbovePointOne} |");
        }

        writer.WriteLine();
        writer.WriteLine("## Gate Changes");
        writer.WriteLine();
        writer.WriteLine("| Gate | Memory turned on | Memory turned off |");
        writer.WriteLine("|---|---:|---:|");
        writer.WriteLine($"| Eat | {GateChanges.EatOn} | {GateChanges.EatOff} |");
        writer.WriteLine($"| Reproduce | {GateChanges.ReproduceOn} | {GateChanges.ReproduceOff} |");
        writer.WriteLine($"| Attack | {GateChanges.AttackOn} | {GateChanges.AttackOff} |");
        writer.WriteLine();
        writer.WriteLine("## Direction Alignments");
        writer.WriteLine();
        writer.WriteLine("| Pair | N | Mean | Median | Same > 0.5 | Opposite < -0.5 |");
        writer.WriteLine("|---|---:|---:|---:|---:|---:|");
        foreach (var alignment in Alignments)
        {
            writer.WriteLine(
                $"| {alignment.Name} | {alignment.Count} | {alignment.Values.Mean:0.###} | {alignment.Values.Median:0.###} | {alignment.ShareSame:0.###} | {alignment.ShareOpposite:0.###} |");
        }

        return writer.ToString();
    }

    private static void WriteSummaryRow(TextWriter writer, string name, Summary summary)
    {
        writer.WriteLine($"| {name} | {summary.Mean:0.###} | {summary.Median:0.###} | {summary.P10:0.###} | {summary.P90:0.###} |");
    }
}

internal sealed record OutputAblationSummary(
    string Name,
    Summary Actual,
    Summary ZeroMemory,
    Summary MemoryOnly,
    Summary SignedDelta,
    Summary AbsoluteDelta,
    int ChangedAbovePointOne);

internal sealed record AblationGateChanges(
    int EatOn,
    int EatOff,
    int ReproduceOn,
    int ReproduceOff,
    int AttackOn,
    int AttackOff);

internal sealed record AlignmentSummary(
    string Name,
    int Count,
    Summary Values,
    float ShareSame,
    float ShareOpposite);

internal sealed record Summary(float Mean, float Median, float P10, float P90)
{
    public static Summary From(IReadOnlyList<float> values)
    {
        if (values.Count == 0)
        {
            return new Summary(0f, 0f, 0f, 0f);
        }

        var sorted = values.Order().ToArray();
        return new Summary(
            values.Average(),
            Quantile(sorted, 0.5f),
            Quantile(sorted, 0.1f),
            Quantile(sorted, 0.9f));
    }

    private static float Quantile(float[] sorted, float quantile)
    {
        var index = Math.Clamp((int)MathF.Floor((sorted.Length - 1) * quantile), 0, sorted.Length - 1);
        return sorted[index];
    }
}

internal sealed class SnapshotMemoryAblation(SimulationSnapshot snapshot)
{
    private const float EatThreshold = 0f;
    private const float ReproduceThreshold = 0.25f;
    private const float AttackThreshold = 0.25f;
    private const float MeaningfulDelta = 0.1f;

    private static readonly string[] OutputNames =
    [
        "Move",
        "Turn",
        "Eat",
        "Reproduce",
        "Attack",
        "Memory forward write",
        "Memory right write"
    ];

    public MemoryAblationReport Run()
    {
        var outputStats = OutputNames.Select(_ => new OutputStats()).ToArray();
        var memoryStrengths = new List<float>();
        var memoryForwardInputs = new List<float>();
        var memoryRightInputs = new List<float>();
        var memoryWriteStrengths = new List<float>();
        var memoryWriteForwards = new List<float>();
        var memoryWriteRights = new List<float>();
        var alignments = new AlignmentBuckets();

        var eatOn = 0;
        var eatOff = 0;
        var reproduceOn = 0;
        var reproduceOff = 0;
        var attackOn = 0;
        var attackOff = 0;
        var evaluated = 0;
        var hiddenNodeCount = 0;

        Span<float> actualInputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> zeroMemoryInputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> memoryOnlyInputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> actualOutputs = stackalloc float[NeuralBrainSchema.OutputCount];
        Span<float> zeroMemoryOutputs = stackalloc float[NeuralBrainSchema.OutputCount];
        Span<float> memoryOnlyOutputs = stackalloc float[NeuralBrainSchema.OutputCount];

        foreach (var creature in snapshot.Creatures)
        {
            if ((uint)creature.GenomeId >= (uint)snapshot.Genomes.Length ||
                (uint)creature.BrainId >= (uint)snapshot.BrainWeights.Length)
            {
                continue;
            }

            var genome = snapshot.Genomes[creature.GenomeId];
            var brain = new NeuralBrainGenome(snapshot.BrainWeights[creature.BrainId]);
            hiddenNodeCount = Math.Max(hiddenNodeCount, brain.HiddenNodeCount);
            evaluated++;

            FillInputs(creature.Senses, genome, actualInputs);
            FillInputs(creature.Senses, genome, zeroMemoryInputs);
            FillInputs(creature.Senses, genome, memoryOnlyInputs);
            ZeroMemoryInputs(zeroMemoryInputs);
            KeepOnlyMemoryInputs(memoryOnlyInputs);

            brain.Evaluate(actualInputs, actualOutputs);
            brain.Evaluate(zeroMemoryInputs, zeroMemoryOutputs);
            brain.Evaluate(memoryOnlyInputs, memoryOnlyOutputs);

            for (var i = 0; i < NeuralBrainSchema.OutputCount; i++)
            {
                outputStats[i].Add(actualOutputs[i], zeroMemoryOutputs[i], memoryOnlyOutputs[i]);
            }

            var actualGate = Gates.From(actualOutputs);
            var zeroMemoryGate = Gates.From(zeroMemoryOutputs);
            eatOn += actualGate.Eat && !zeroMemoryGate.Eat ? 1 : 0;
            eatOff += !actualGate.Eat && zeroMemoryGate.Eat ? 1 : 0;
            reproduceOn += actualGate.Reproduce && !zeroMemoryGate.Reproduce ? 1 : 0;
            reproduceOff += !actualGate.Reproduce && zeroMemoryGate.Reproduce ? 1 : 0;
            attackOn += actualGate.Attack && !zeroMemoryGate.Attack ? 1 : 0;
            attackOff += !actualGate.Attack && zeroMemoryGate.Attack ? 1 : 0;

            var memoryForward = creature.Senses.MemoryDirectionForward;
            var memoryRight = creature.Senses.MemoryDirectionRight;
            var memoryWriteForward = actualOutputs[NeuralBrainSchema.MemoryForwardOutput];
            var memoryWriteRight = actualOutputs[NeuralBrainSchema.MemoryRightOutput];

            memoryStrengths.Add(creature.Senses.MemoryStrength);
            memoryForwardInputs.Add(memoryForward);
            memoryRightInputs.Add(memoryRight);
            memoryWriteForwards.Add(memoryWriteForward);
            memoryWriteRights.Add(memoryWriteRight);
            memoryWriteStrengths.Add(VectorLength(memoryWriteForward, memoryWriteRight));

            alignments.Add(creature.Senses, memoryForward, memoryRight, memoryWriteForward, memoryWriteRight);
        }

        return new MemoryAblationReport(
            snapshot.Scenario.Name,
            snapshot.Tick,
            snapshot.ElapsedSeconds,
            snapshot.Creatures.Length,
            evaluated,
            hiddenNodeCount,
            Summary.From(memoryStrengths),
            Summary.From(memoryForwardInputs),
            Summary.From(memoryRightInputs),
            Summary.From(memoryWriteStrengths),
            Summary.From(memoryWriteForwards),
            Summary.From(memoryWriteRights),
            new AblationGateChanges(eatOn, eatOff, reproduceOn, reproduceOff, attackOn, attackOff),
            outputStats.Select((stats, index) => stats.ToSummary(OutputNames[index])).ToArray(),
            alignments.ToSummaries());
    }

    private static void FillInputs(CreatureSenseState senses, CreatureGenome genome, Span<float> inputs)
    {
        inputs.Clear();
        inputs[NeuralBrainSchema.BiasInput] = 1f;
        inputs[NeuralBrainSchema.EnergyRatioInput] = senses.EnergyRatio;
        inputs[NeuralBrainSchema.HungerInput] = senses.Hunger;
        inputs[NeuralBrainSchema.FoodProximityInput] = senses.FoodProximity;
        inputs[NeuralBrainSchema.FoodForwardInput] = senses.FoodDirectionForward;
        inputs[NeuralBrainSchema.FoodRightInput] = senses.FoodDirectionRight;
        inputs[NeuralBrainSchema.VisibleFoodDensityInput] = senses.VisibleFoodDensity;
        inputs[NeuralBrainSchema.VisiblePlantDensityInput] = senses.VisiblePlantDensity;
        inputs[NeuralBrainSchema.PlantProximityInput] = senses.PlantProximity;
        inputs[NeuralBrainSchema.PlantForwardInput] = senses.PlantDirectionForward;
        inputs[NeuralBrainSchema.PlantRightInput] = senses.PlantDirectionRight;
        inputs[NeuralBrainSchema.VisibleMeatDensityInput] = senses.VisibleMeatDensity;
        inputs[NeuralBrainSchema.MeatProximityInput] = senses.MeatProximity;
        inputs[NeuralBrainSchema.MeatForwardInput] = senses.MeatDirectionForward;
        inputs[NeuralBrainSchema.MeatRightInput] = senses.MeatDirectionRight;
        inputs[NeuralBrainSchema.DietaryMeatBiasInput] = genome.DietaryAdaptation;
        inputs[NeuralBrainSchema.EggReserveRatioInput] = senses.EggReserveRatio;
        inputs[NeuralBrainSchema.ReproductionReadinessInput] = senses.ReproductionReadiness;
        inputs[NeuralBrainSchema.VisibleCreatureDensityInput] = senses.VisibleCreatureDensity;
        inputs[NeuralBrainSchema.CreatureProximityInput] = senses.CreatureProximity;
        inputs[NeuralBrainSchema.CreatureForwardInput] = senses.CreatureDirectionForward;
        inputs[NeuralBrainSchema.CreatureRightInput] = senses.CreatureDirectionRight;
        inputs[NeuralBrainSchema.MeatScentDensityInput] = senses.MeatScentDensity;
        inputs[NeuralBrainSchema.MeatScentForwardInput] = senses.MeatScentDirectionForward;
        inputs[NeuralBrainSchema.MeatScentRightInput] = senses.MeatScentDirectionRight;
        inputs[NeuralBrainSchema.CreatureRelativeBodySizeInput] = senses.CreatureRelativeBodySize;
        inputs[NeuralBrainSchema.CreatureRelativeSpeedInput] = senses.CreatureRelativeSpeed;
        inputs[NeuralBrainSchema.CreatureApproachRateInput] = senses.CreatureApproachRate;
        inputs[NeuralBrainSchema.CreatureFacingAlignmentInput] = senses.CreatureFacingAlignment;
        inputs[NeuralBrainSchema.CurrentTerrainDragInput] = senses.CurrentTerrainDrag;
        inputs[NeuralBrainSchema.ForwardTerrainDragInput] = senses.ForwardTerrainDrag;
        inputs[NeuralBrainSchema.LeftTerrainDragInput] = senses.LeftTerrainDrag;
        inputs[NeuralBrainSchema.RightTerrainDragInput] = senses.RightTerrainDrag;
        inputs[NeuralBrainSchema.EnergySurplusInput] = senses.EnergySurplusRatio;
        inputs[NeuralBrainSchema.RecentFoodSuccessInput] = senses.RecentFoodSuccess;
        inputs[NeuralBrainSchema.MemoryForwardInput] = senses.MemoryDirectionForward;
        inputs[NeuralBrainSchema.MemoryRightInput] = senses.MemoryDirectionRight;
        inputs[NeuralBrainSchema.MemoryStrengthInput] = senses.MemoryStrength;
    }

    private static void ZeroMemoryInputs(Span<float> inputs)
    {
        inputs[NeuralBrainSchema.MemoryForwardInput] = 0f;
        inputs[NeuralBrainSchema.MemoryRightInput] = 0f;
        inputs[NeuralBrainSchema.MemoryStrengthInput] = 0f;
    }

    private static void KeepOnlyMemoryInputs(Span<float> inputs)
    {
        var memoryForward = inputs[NeuralBrainSchema.MemoryForwardInput];
        var memoryRight = inputs[NeuralBrainSchema.MemoryRightInput];
        var memoryStrength = inputs[NeuralBrainSchema.MemoryStrengthInput];
        inputs.Clear();
        inputs[NeuralBrainSchema.BiasInput] = 1f;
        inputs[NeuralBrainSchema.MemoryForwardInput] = memoryForward;
        inputs[NeuralBrainSchema.MemoryRightInput] = memoryRight;
        inputs[NeuralBrainSchema.MemoryStrengthInput] = memoryStrength;
    }

    private static float VectorLength(float x, float y)
    {
        return MathF.Sqrt(x * x + y * y);
    }

    private readonly record struct Gates(bool Eat, bool Reproduce, bool Attack)
    {
        public static Gates From(ReadOnlySpan<float> outputs)
        {
            return new Gates(
                outputs[NeuralBrainSchema.EatOutput] > EatThreshold,
                outputs[NeuralBrainSchema.ReproduceOutput] > ReproduceThreshold,
                outputs[NeuralBrainSchema.AttackOutput] > AttackThreshold);
        }
    }

    private sealed class OutputStats
    {
        private readonly List<float> _actual = [];
        private readonly List<float> _zeroMemory = [];
        private readonly List<float> _memoryOnly = [];
        private readonly List<float> _signedDelta = [];
        private readonly List<float> _absoluteDelta = [];
        private int _changedAbovePointOne;

        public void Add(float actual, float zeroMemory, float memoryOnly)
        {
            var delta = actual - zeroMemory;
            _actual.Add(actual);
            _zeroMemory.Add(zeroMemory);
            _memoryOnly.Add(memoryOnly);
            _signedDelta.Add(delta);
            _absoluteDelta.Add(Math.Abs(delta));
            _changedAbovePointOne += Math.Abs(delta) > MeaningfulDelta ? 1 : 0;
        }

        public OutputAblationSummary ToSummary(string name)
        {
            return new OutputAblationSummary(
                name,
                Summary.From(_actual),
                Summary.From(_zeroMemory),
                Summary.From(_memoryOnly),
                Summary.From(_signedDelta),
                Summary.From(_absoluteDelta),
                _changedAbovePointOne);
        }
    }

    private sealed class AlignmentBuckets
    {
        private readonly Dictionary<string, List<float>> _values = new()
        {
            ["Memory vs food"] = [],
            ["Memory vs plant"] = [],
            ["Memory vs meat"] = [],
            ["Memory vs meat scent"] = [],
            ["Memory vs creature"] = [],
            ["Write vs food"] = [],
            ["Write vs plant"] = [],
            ["Write vs meat"] = [],
            ["Write vs meat scent"] = [],
            ["Write vs creature"] = []
        };

        public void Add(CreatureSenseState senses, float memoryForward, float memoryRight, float writeForward, float writeRight)
        {
            if (senses.FoodDetected || senses.VisibleFoodDensity > 0f)
            {
                Add("Memory vs food", memoryForward, memoryRight, senses.FoodDirectionForward, senses.FoodDirectionRight);
                Add("Write vs food", writeForward, writeRight, senses.FoodDirectionForward, senses.FoodDirectionRight);
            }

            if (senses.PlantDetected || senses.VisiblePlantDensity > 0f)
            {
                Add("Memory vs plant", memoryForward, memoryRight, senses.PlantDirectionForward, senses.PlantDirectionRight);
                Add("Write vs plant", writeForward, writeRight, senses.PlantDirectionForward, senses.PlantDirectionRight);
            }

            if (senses.MeatDetected || senses.VisibleMeatDensity > 0f)
            {
                Add("Memory vs meat", memoryForward, memoryRight, senses.MeatDirectionForward, senses.MeatDirectionRight);
                Add("Write vs meat", writeForward, writeRight, senses.MeatDirectionForward, senses.MeatDirectionRight);
            }

            if (senses.MeatScentDetected || senses.MeatScentDensity > 0f)
            {
                Add("Memory vs meat scent", memoryForward, memoryRight, senses.MeatScentDirectionForward, senses.MeatScentDirectionRight);
                Add("Write vs meat scent", writeForward, writeRight, senses.MeatScentDirectionForward, senses.MeatScentDirectionRight);
            }

            if (senses.CreatureDetected || senses.VisibleCreatureDensity > 0f)
            {
                Add("Memory vs creature", memoryForward, memoryRight, senses.CreatureDirectionForward, senses.CreatureDirectionRight);
                Add("Write vs creature", writeForward, writeRight, senses.CreatureDirectionForward, senses.CreatureDirectionRight);
            }
        }

        public IReadOnlyList<AlignmentSummary> ToSummaries()
        {
            return _values.Select(pair =>
            {
                var same = pair.Value.Count == 0
                    ? 0f
                    : pair.Value.Count(value => value > 0.5f) / (float)pair.Value.Count;
                var opposite = pair.Value.Count == 0
                    ? 0f
                    : pair.Value.Count(value => value < -0.5f) / (float)pair.Value.Count;
                return new AlignmentSummary(pair.Key, pair.Value.Count, Summary.From(pair.Value), same, opposite);
            }).ToArray();
        }

        private void Add(string name, float ax, float ay, float bx, float by)
        {
            var dot = NormalizedDot(ax, ay, bx, by);
            if (dot is not null)
            {
                _values[name].Add(dot.Value);
            }
        }

        private static float? NormalizedDot(float ax, float ay, float bx, float by)
        {
            var aLength = MathF.Sqrt(ax * ax + ay * ay);
            var bLength = MathF.Sqrt(bx * bx + by * by);
            if (aLength <= 0.000001f || bLength <= 0.000001f)
            {
                return null;
            }

            return (ax * bx + ay * by) / (aLength * bLength);
        }
    }
}
