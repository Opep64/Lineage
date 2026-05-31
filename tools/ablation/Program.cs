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
    int SkippedCreatureCount,
    int SkippedUnsupportedBrainCount,
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
        if (SkippedCreatureCount > 0)
        {
            writer.WriteLine($"Creatures skipped: `{SkippedCreatureCount}` (unsupported brain architecture: `{SkippedUnsupportedBrainCount}`)");
        }

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
    private const float AttackThreshold = NeuralControllerSystem.DefaultAttackThreshold;
    private const float MeaningfulDelta = 0.1f;

    private static readonly BrainInputDefinition[] MemoryInputs = BrainIoRegistry.Inputs
        .Where(static input => input.Group == BrainIoSignalGroup.Memory)
        .ToArray();

    private static readonly BrainOutputDefinition[] Outputs = BrainIoRegistry.Outputs.ToArray();

    public MemoryAblationReport Run()
    {
        var outputStats = Outputs.Select(_ => new OutputStats()).ToArray();
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
        var skipped = 0;
        var skippedUnsupported = 0;
        var hiddenNodeCount = 0;
        var brains = CreateBrains(snapshot);

        var actualInputs = new float[BrainIoRegistry.Inputs.Count];
        var zeroMemoryInputs = new float[BrainIoRegistry.Inputs.Count];
        var memoryOnlyInputs = new float[BrainIoRegistry.Inputs.Count];
        var actualOutputs = new float[BrainIoRegistry.Outputs.Count];
        var zeroMemoryOutputs = new float[BrainIoRegistry.Outputs.Count];
        var memoryOnlyOutputs = new float[BrainIoRegistry.Outputs.Count];

        foreach (var creature in snapshot.Creatures)
        {
            if ((uint)creature.GenomeId >= (uint)snapshot.Genomes.Length ||
                (uint)creature.BrainId >= (uint)brains.Length)
            {
                skipped++;
                continue;
            }

            var genome = snapshot.Genomes[creature.GenomeId];
            var brain = brains[creature.BrainId];
            if (brain.ArchitectureKind == BrainArchitectureKind.RtNeatGraph)
            {
                skipped++;
                skippedUnsupported++;
                continue;
            }

            hiddenNodeCount = Math.Max(hiddenNodeCount, brain.HiddenNodeCount);
            evaluated++;

            FillInputs(creature.Senses, genome, actualInputs);
            actualInputs.CopyTo(zeroMemoryInputs, 0);
            actualInputs.CopyTo(memoryOnlyInputs, 0);
            ZeroMemoryInputs(zeroMemoryInputs);
            KeepOnlyMemoryInputs(memoryOnlyInputs);

            brain.Evaluate(actualInputs, actualOutputs);
            brain.Evaluate(zeroMemoryInputs, zeroMemoryOutputs);
            brain.Evaluate(memoryOnlyInputs, memoryOnlyOutputs);

            var actualResult = ReadResult(actualOutputs);
            var zeroMemoryResult = ReadResult(zeroMemoryOutputs);
            var memoryOnlyResult = ReadResult(memoryOnlyOutputs);

            for (var i = 0; i < Outputs.Length; i++)
            {
                outputStats[i].Add(
                    ReadOutput(Outputs[i], actualResult),
                    ReadOutput(Outputs[i], zeroMemoryResult),
                    ReadOutput(Outputs[i], memoryOnlyResult));
            }

            var actualGate = Gates.From(actualResult);
            var zeroMemoryGate = Gates.From(zeroMemoryResult);
            eatOn += actualGate.Eat && !zeroMemoryGate.Eat ? 1 : 0;
            eatOff += !actualGate.Eat && zeroMemoryGate.Eat ? 1 : 0;
            reproduceOn += actualGate.Reproduce && !zeroMemoryGate.Reproduce ? 1 : 0;
            reproduceOff += !actualGate.Reproduce && zeroMemoryGate.Reproduce ? 1 : 0;
            attackOn += actualGate.Attack && !zeroMemoryGate.Attack ? 1 : 0;
            attackOff += !actualGate.Attack && zeroMemoryGate.Attack ? 1 : 0;

            var memoryForward = creature.Senses.MemoryDirectionForward;
            var memoryRight = creature.Senses.MemoryDirectionRight;
            var memoryWriteForward = actualResult.Memory.DirectionForward;
            var memoryWriteRight = actualResult.Memory.DirectionRight;

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
            skipped,
            skippedUnsupported,
            hiddenNodeCount,
            Summary.From(memoryStrengths),
            Summary.From(memoryForwardInputs),
            Summary.From(memoryRightInputs),
            Summary.From(memoryWriteStrengths),
            Summary.From(memoryWriteForwards),
            Summary.From(memoryWriteRights),
            new AblationGateChanges(eatOn, eatOff, reproduceOn, reproduceOff, attackOn, attackOff),
            outputStats.Select((stats, index) => stats.ToSummary(Outputs[index].Key)).ToArray(),
            alignments.ToSummaries());
    }

    private static void FillInputs(CreatureSenseState senses, CreatureGenome genome, Span<float> inputs)
    {
        var inputFrame = BrainInputFrame.FromSenses(senses, genome);
        var memoryFrame = LegacyNeuralMemoryInputFrame.FromSenses(senses);
        LegacyNeuralBrainAdapter.FillInputs(inputFrame, memoryFrame, inputs);
    }

    private static void ZeroMemoryInputs(Span<float> inputs)
    {
        foreach (var input in MemoryInputs)
        {
            inputs[input.FlatIndex] = input.NeutralValue;
        }
    }

    private static void KeepOnlyMemoryInputs(Span<float> inputs)
    {
        var memoryValues = new float[MemoryInputs.Length];
        for (var i = 0; i < MemoryInputs.Length; i++)
        {
            memoryValues[i] = inputs[MemoryInputs[i].FlatIndex];
        }

        inputs.Clear();
        foreach (var input in BrainIoRegistry.Inputs)
        {
            if (input.Group == BrainIoSignalGroup.Bias)
            {
                inputs[input.FlatIndex] = input.NeutralValue;
            }
        }

        for (var i = 0; i < MemoryInputs.Length; i++)
        {
            inputs[MemoryInputs[i].FlatIndex] = memoryValues[i];
        }
    }

    private static BrainGenome[] CreateBrains(SimulationSnapshot snapshot)
    {
        if (snapshot.BrainPayloads.Length > 0)
        {
            return snapshot.BrainPayloads.Select(static payload => payload.CreateBrain()).ToArray();
        }

        var brains = new BrainGenome[snapshot.BrainWeights.Length];
        for (var i = 0; i < brains.Length; i++)
        {
            var architectureKind = snapshot.BrainArchitectureKinds.Length == snapshot.BrainWeights.Length
                ? snapshot.BrainArchitectureKinds[i]
                : snapshot.Scenario.BrainArchitectureKind;
            brains[i] = BrainGenome.FromNeural(
                architectureKind,
                new NeuralBrainGenome(snapshot.BrainWeights[i]));
        }

        return brains;
    }

    private static BrainEvaluationResult ReadResult(ReadOnlySpan<float> outputs)
    {
        return new BrainEvaluationResult(
            LegacyNeuralBrainAdapter.ReadStandardOutputs(outputs),
            LegacyNeuralBrainAdapter.ReadMemoryOutputs(outputs));
    }

    private static float ReadOutput(BrainOutputDefinition output, in BrainEvaluationResult result)
    {
        return output.Key switch
        {
            "action.move_forward" => result.Actions.MoveForward,
            "action.turn" => result.Actions.Turn,
            "action.eat" => result.Actions.Eat,
            "action.reproduce" => result.Actions.Reproduce,
            "action.attack" => result.Actions.Attack,
            "action.grab" => result.Actions.Grab,
            "action.sound_amplitude" => result.Actions.SoundAmplitude,
            "action.sound_tone" => result.Actions.SoundTone,
            "dense_memory.write_forward" => result.Memory.DirectionForward,
            "dense_memory.write_right" => result.Memory.DirectionRight,
            _ => throw new ArgumentOutOfRangeException(nameof(output), output.Key, "Unknown brain output key.")
        };
    }

    private static float VectorLength(float x, float y)
    {
        return MathF.Sqrt(x * x + y * y);
    }

    private readonly record struct Gates(bool Eat, bool Reproduce, bool Attack)
    {
        public static Gates From(in BrainEvaluationResult result)
        {
            return new Gates(
                result.Actions.Eat > EatThreshold,
                result.Actions.Reproduce > ReproduceThreshold,
                result.Actions.Attack > AttackThreshold);
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
