namespace Lineage.Core;

/// <summary>
/// Broad semantic bucket for a brain input or output signal.
/// </summary>
public enum BrainIoSignalGroup
{
    Bias,
    Internal,
    Vision,
    Scent,
    Terrain,
    Habitat,
    Obstacle,
    Contact,
    Memory,
    Action
}

/// <summary>
/// Describes how often a signal is expected to be refreshed by the simulation.
/// </summary>
public enum BrainInputFreshnessPolicy
{
    AlwaysFresh,
    InternalOrContactFresh,
    WorldSenseStale,
    AdapterRuntime
}

/// <summary>
/// Separates universal physical action intents from architecture-owned adapter state.
/// </summary>
public enum BrainOutputScope
{
    PhysicalAction,
    ArchitectureInternal
}

public sealed record BrainInputDefinition(
    string Key,
    string Name,
    int FlatIndex,
    BrainIoSignalGroup Group,
    float MinimumValue,
    float MaximumValue,
    float NeutralValue,
    int IntroducedVersion,
    BrainInputFreshnessPolicy Freshness,
    string Meaning,
    float? SubstrateX = null,
    float? SubstrateY = null);

public sealed record BrainOutputDefinition(
    string Key,
    string Name,
    int FlatIndex,
    BrainIoSignalGroup Group,
    float MinimumValue,
    float MaximumValue,
    float NeutralValue,
    int IntroducedVersion,
    BrainOutputScope Scope,
    string Meaning);

/// <summary>
/// Semantic metadata for the current flat neural adapter.
/// </summary>
///
/// <remarks>
/// This registry describes the dense schema used by <see cref="LegacyNeuralBrainAdapter"/>.
/// New brain architectures should treat it as an adapter contract, not as proof that all
/// brains must expose the same internal memory slots.
/// </remarks>
public static class BrainIoRegistry
{
    public static IReadOnlyList<BrainInputDefinition> Inputs { get; } = BuildInputs();

    public static IReadOnlyList<BrainOutputDefinition> Outputs { get; } = BuildOutputs();

    public static IReadOnlyList<BrainOutputDefinition> PhysicalActionOutputs { get; } =
        Outputs.Where(output => output.Scope == BrainOutputScope.PhysicalAction).ToArray();

    public static IReadOnlyList<BrainOutputDefinition> ArchitectureInternalOutputs { get; } =
        Outputs.Where(output => output.Scope == BrainOutputScope.ArchitectureInternal).ToArray();

    public static BrainInputDefinition GetInput(int flatIndex)
    {
        if ((uint)flatIndex >= Inputs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(flatIndex));
        }

        return Inputs[flatIndex];
    }

    public static BrainOutputDefinition GetOutput(int flatIndex)
    {
        if ((uint)flatIndex >= Outputs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(flatIndex));
        }

        return Outputs[flatIndex];
    }

    private static BrainInputDefinition[] BuildInputs()
    {
        var inputs = new List<BrainInputDefinition>(NeuralBrainSchema.InputCount)
        {
            Input("bias", "Bias", NeuralBrainSchema.BiasInput, BrainIoSignalGroup.Bias, 1f, 1f, 1f, BrainInputFreshnessPolicy.AlwaysFresh, "Constant baseline input."),
            Input("internal.energy_ratio", "Energy ratio", NeuralBrainSchema.EnergyRatioInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current energy divided by energy capacity."),
            Input("internal.hunger", "Hunger", NeuralBrainSchema.HungerInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current hunger pressure."),
            Input("vision.food_density", "Visible food density", NeuralBrainSchema.VisibleFoodDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Blended visible food-like mass."),
            Input("vision.plant_density", "Visible plant density", NeuralBrainSchema.VisiblePlantDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Visible plant mass."),
            Input("vision.meat_density", "Visible meat density", NeuralBrainSchema.VisibleMeatDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Visible meat-like mass."),
            Input("internal.dietary_meat_bias", "Dietary meat bias", NeuralBrainSchema.DietaryMeatBiasInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.AlwaysFresh, "Inherited meat-versus-plant dietary adaptation."),
            Input("internal.egg_reserve_ratio", "Egg reserve ratio", NeuralBrainSchema.EggReserveRatioInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Progress toward enough reserve to lay an egg."),
            Input("internal.reproduction_readiness", "Reproduction readiness", NeuralBrainSchema.ReproductionReadinessInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Combined maturity, cooldown, reserve, and health reproduction readiness."),
            Input("vision.creature_density", "Visible creature density", NeuralBrainSchema.VisibleCreatureDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Visible nearby creature mass."),
            Input("scent.meat_density", "Meat scent density", NeuralBrainSchema.MeatScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Meat scent strength."),
            Input("scent.meat_forward", "Meat scent forward", NeuralBrainSchema.MeatScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Meat scent direction along the creature's forward axis."),
            Input("scent.meat_right", "Meat scent right", NeuralBrainSchema.MeatScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Meat scent direction along the creature's right axis."),
            Input("terrain.current_drag", "Current terrain drag", NeuralBrainSchema.CurrentTerrainDragInput, BrainIoSignalGroup.Terrain, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Traversal drag under the creature."),
            Input("terrain.forward_drag", "Forward terrain drag", NeuralBrainSchema.ForwardTerrainDragInput, BrainIoSignalGroup.Terrain, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Traversal drag sampled ahead."),
            Input("terrain.left_drag", "Left terrain drag", NeuralBrainSchema.LeftTerrainDragInput, BrainIoSignalGroup.Terrain, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Traversal drag sampled to the left."),
            Input("terrain.right_drag", "Right terrain drag", NeuralBrainSchema.RightTerrainDragInput, BrainIoSignalGroup.Terrain, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Traversal drag sampled to the right."),
            Input("internal.energy_surplus", "Energy surplus", NeuralBrainSchema.EnergySurplusInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Energy available beyond basic survival needs."),
            Input("internal.recent_food_success", "Recent food success", NeuralBrainSchema.RecentFoodSuccessInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Short-term trace of successful eating."),
            Input("dense_memory.forward", "Dense memory forward", NeuralBrainSchema.MemoryForwardInput, BrainIoSignalGroup.Memory, -1f, 1f, 0f, BrainInputFreshnessPolicy.AdapterRuntime, "Legacy dense-adapter memory vector projected forward."),
            Input("dense_memory.right", "Dense memory right", NeuralBrainSchema.MemoryRightInput, BrainIoSignalGroup.Memory, -1f, 1f, 0f, BrainInputFreshnessPolicy.AdapterRuntime, "Legacy dense-adapter memory vector projected right."),
            Input("dense_memory.strength", "Dense memory strength", NeuralBrainSchema.MemoryStrengthInput, BrainIoSignalGroup.Memory, 0f, 1f, 0f, BrainInputFreshnessPolicy.AdapterRuntime, "Legacy dense-adapter memory vector magnitude."),
            Input("vision.meat_freshness", "Visible meat freshness", NeuralBrainSchema.VisibleMeatFreshnessInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Freshness of visible meat-like food."),
            Input("scent.rotten_meat_density", "Rotten meat scent density", NeuralBrainSchema.RottenMeatScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Rotten meat scent strength."),
            Input("scent.rotten_meat_forward", "Rotten meat scent forward", NeuralBrainSchema.RottenMeatScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Rotten meat scent direction along the creature's forward axis."),
            Input("scent.rotten_meat_right", "Rotten meat scent right", NeuralBrainSchema.RottenMeatScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Rotten meat scent direction along the creature's right axis."),
            Input("obstacle.forward", "Forward obstacle", NeuralBrainSchema.ForwardObstacleInput, BrainIoSignalGroup.Obstacle, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Obstacle proximity sampled ahead."),
            Input("obstacle.left", "Left obstacle", NeuralBrainSchema.LeftObstacleInput, BrainIoSignalGroup.Obstacle, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Obstacle proximity sampled to the left."),
            Input("obstacle.right", "Right obstacle", NeuralBrainSchema.RightObstacleInput, BrainIoSignalGroup.Obstacle, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Obstacle proximity sampled to the right."),
            Input("contact.movement_blocked", "Movement blocked", NeuralBrainSchema.MovementBlockedInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent hard movement block contact cue.")
        };

        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            AddVisionSectorInputs(inputs, sectorIndex);
        }

        inputs.AddRange(new[]
        {
            Input("contact.food", "Food contact", NeuralBrainSchema.FoodContactInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Any edible food touching the body."),
            Input("contact.plant_food", "Plant food contact", NeuralBrainSchema.PlantFoodContactInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Plant food touching the body."),
            Input("contact.meat_food", "Meat food contact", NeuralBrainSchema.MeatFoodContactInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Meat food touching the body."),
            Input("contact.egg_food", "Egg food contact", NeuralBrainSchema.EggFoodContactInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Egg food touching the body."),
            Input("contact.creature", "Creature contact", NeuralBrainSchema.CreatureContactInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Another creature touching the body."),
            Input("internal.health_ratio", "Health ratio", NeuralBrainSchema.HealthRatioInput, BrainIoSignalGroup.Internal, 0f, 1f, 1f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current health divided by maximum health."),
            Input("vision.plant_energy_quality", "Visible plant energy quality", NeuralBrainSchema.VisiblePlantEnergyQualityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Close visual estimate of visible plant energy payoff."),
            Input("vision.plant_bite_ease", "Visible plant bite ease", NeuralBrainSchema.VisiblePlantBiteEaseInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Close visual estimate of visible plant bite ease."),
            Input("contact.plant_energy_quality", "Contact plant energy quality", NeuralBrainSchema.PlantFoodContactEnergyQualityInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Taste/contact estimate of touched plant energy payoff."),
            Input("contact.plant_bite_ease", "Contact plant bite ease", NeuralBrainSchema.PlantFoodContactBiteEaseInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Taste/contact estimate of touched plant bite ease."),
            Input("internal.recent_plant_raw_yield", "Recent plant raw yield", NeuralBrainSchema.RecentPlantRawYieldInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent raw plant calories transferred into the gut."),
            Input("internal.recent_plant_energy_yield", "Recent plant energy yield", NeuralBrainSchema.RecentPlantEnergyYieldInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent plant digestion energy released."),
            Input("internal.recent_food_energy_yield", "Recent food energy yield", NeuralBrainSchema.RecentFoodEnergyYieldInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent energy released from any gut contents."),
            Input("internal.recent_tender_plant_energy_yield", "Recent tender plant energy yield", NeuralBrainSchema.RecentTenderPlantEnergyYieldInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent tender plant digestion energy released."),
            Input("internal.recent_rich_plant_energy_yield", "Recent rich plant energy yield", NeuralBrainSchema.RecentRichPlantEnergyYieldInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent rich plant digestion energy released."),
            Input("internal.recent_tough_plant_energy_yield", "Recent tough plant energy yield", NeuralBrainSchema.RecentToughPlantEnergyYieldInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent tough plant digestion energy released."),
            Input("internal.tender_plant_payoff_trace", "Tender plant payoff trace", NeuralBrainSchema.TenderPlantPayoffTraceInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Decaying trace of tender plant digestion payoff."),
            Input("internal.rich_plant_payoff_trace", "Rich plant payoff trace", NeuralBrainSchema.RichPlantPayoffTraceInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Decaying trace of rich plant digestion payoff."),
            Input("internal.tough_plant_payoff_trace", "Tough plant payoff trace", NeuralBrainSchema.ToughPlantPayoffTraceInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Decaying trace of tough plant digestion payoff."),
            Input("vision.plant_preference_density", "Plant preference density", NeuralBrainSchema.PlantPreferenceDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Visible plant cue weighted by recent typed plant payoff."),
            Input("vision.plant_preference_forward", "Plant preference forward", NeuralBrainSchema.PlantPreferenceForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Plant preference cue direction along the creature's forward axis."),
            Input("vision.plant_preference_right", "Plant preference right", NeuralBrainSchema.PlantPreferenceRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Plant preference cue direction along the creature's right axis."),
            Input("contact.plant_preference", "Contact plant preference", NeuralBrainSchema.PlantFoodContactPreferenceInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Touched plant preference cue weighted by recent typed plant payoff."),
            Input("scent.creature_similarity_density", "Creature similarity scent density", NeuralBrainSchema.CreatureSimilarityScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Scent strength from trait-similar creatures."),
            Input("scent.creature_similarity_forward", "Creature similarity scent forward", NeuralBrainSchema.CreatureSimilarityScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Similarity scent direction along the creature's forward axis."),
            Input("scent.creature_similarity_right", "Creature similarity scent right", NeuralBrainSchema.CreatureSimilarityScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Similarity scent direction along the creature's right axis."),
            Input("contact.creature_similarity", "Creature contact similarity", NeuralBrainSchema.CreatureContactSimilarityInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Trait similarity of a contacted creature."),
            Input("habitat.current_quality", "Current habitat quality", NeuralBrainSchema.CurrentHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality under the creature."),
            Input("habitat.forward_quality", "Forward habitat quality", NeuralBrainSchema.ForwardHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality sampled ahead."),
            Input("habitat.left_quality", "Left habitat quality", NeuralBrainSchema.LeftHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality sampled to the left."),
            Input("habitat.right_quality", "Right habitat quality", NeuralBrainSchema.RightHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality sampled to the right."),
            Input("contact.grab_pressure", "Grab pressure", NeuralBrainSchema.GrabPressureInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Strength of another creature's hold on this creature.", introducedVersion: 3),
            Input("contact.grab_forward", "Grab direction forward", NeuralBrainSchema.GrabDirectionForwardInput, BrainIoSignalGroup.Contact, -1f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Direction toward the grabbing creature projected forward.", introducedVersion: 3),
            Input("contact.grab_right", "Grab direction right", NeuralBrainSchema.GrabDirectionRightInput, BrainIoSignalGroup.Contact, -1f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Direction toward the grabbing creature projected right.", introducedVersion: 3),
            Input("contact.can_grab_creature", "Can grab creature", NeuralBrainSchema.CanGrabCreatureInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "A creature is close enough to grab.", introducedVersion: 3),
            Input("contact.is_holding_creature", "Is holding creature", NeuralBrainSchema.IsHoldingCreatureInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "This creature is currently holding another creature.", introducedVersion: 3)
        });

        return ValidateInputs(inputs);
    }

    private static void AddVisionSectorInputs(List<BrainInputDefinition> inputs, int sectorIndex)
    {
        var x = SectorSubstrateX(sectorIndex);
        inputs.Add(SectorInput("plant_density", "plant density", sectorIndex, NeuralBrainSchema.VisionSectorPlantDensityOffset, 0f, 1f, "Plant mass visible in the sector.", x));
        inputs.Add(SectorInput("plant_proximity", "plant proximity", sectorIndex, NeuralBrainSchema.VisionSectorPlantProximityOffset, 0f, 1f, "Nearest plant proximity in the sector.", x));
        inputs.Add(SectorInput("plant_energy_quality", "plant energy quality", sectorIndex, NeuralBrainSchema.VisionSectorPlantEnergyQualityOffset, 0f, 1f, "Visible plant energy payoff in the sector.", x));
        inputs.Add(SectorInput("plant_bite_ease", "plant bite ease", sectorIndex, NeuralBrainSchema.VisionSectorPlantBiteEaseOffset, 0f, 1f, "Visible plant bite ease in the sector.", x));
        inputs.Add(SectorInput("meat_density", "meat density", sectorIndex, NeuralBrainSchema.VisionSectorMeatDensityOffset, 0f, 1f, "Meat-like mass visible in the sector.", x));
        inputs.Add(SectorInput("meat_proximity", "meat proximity", sectorIndex, NeuralBrainSchema.VisionSectorMeatProximityOffset, 0f, 1f, "Nearest meat-like food proximity in the sector.", x));
        inputs.Add(SectorInput("egg_density", "egg density", sectorIndex, NeuralBrainSchema.VisionSectorEggDensityOffset, 0f, 1f, "Egg mass visible in the sector.", x));
        inputs.Add(SectorInput("egg_proximity", "egg proximity", sectorIndex, NeuralBrainSchema.VisionSectorEggProximityOffset, 0f, 1f, "Nearest egg proximity in the sector.", x));
        inputs.Add(SectorInput("creature_density", "creature density", sectorIndex, NeuralBrainSchema.VisionSectorCreatureDensityOffset, 0f, 1f, "Creature mass visible in the sector.", x));
        inputs.Add(SectorInput("creature_proximity", "creature proximity", sectorIndex, NeuralBrainSchema.VisionSectorCreatureProximityOffset, 0f, 1f, "Nearest creature proximity in the sector.", x));
        inputs.Add(SectorInput("smaller_creature_density", "smaller creature density", sectorIndex, NeuralBrainSchema.VisionSectorSmallerCreatureDensityOffset, 0f, 1f, "Smaller creature mass visible in the sector.", x));
        inputs.Add(SectorInput("smaller_creature_proximity", "smaller creature proximity", sectorIndex, NeuralBrainSchema.VisionSectorSmallerCreatureProximityOffset, 0f, 1f, "Nearest smaller creature proximity in the sector.", x));
        inputs.Add(SectorInput("similar_creature_density", "similar creature density", sectorIndex, NeuralBrainSchema.VisionSectorSimilarCreatureDensityOffset, 0f, 1f, "Similar-sized creature mass visible in the sector.", x));
        inputs.Add(SectorInput("similar_creature_proximity", "similar creature proximity", sectorIndex, NeuralBrainSchema.VisionSectorSimilarCreatureProximityOffset, 0f, 1f, "Nearest similar-sized creature proximity in the sector.", x));
        inputs.Add(SectorInput("larger_creature_density", "larger creature density", sectorIndex, NeuralBrainSchema.VisionSectorLargerCreatureDensityOffset, 0f, 1f, "Larger creature mass visible in the sector.", x));
        inputs.Add(SectorInput("larger_creature_proximity", "larger creature proximity", sectorIndex, NeuralBrainSchema.VisionSectorLargerCreatureProximityOffset, 0f, 1f, "Nearest larger creature proximity in the sector.", x));
        inputs.Add(SectorInput("creature_approach_rate", "creature approach rate", sectorIndex, NeuralBrainSchema.VisionSectorCreatureApproachRateOffset, -1f, 1f, "Closing rate of the nearest creature in the sector.", x));
        inputs.Add(SectorInput("creature_facing_alignment", "creature facing alignment", sectorIndex, NeuralBrainSchema.VisionSectorCreatureFacingAlignmentOffset, -1f, 1f, "Facing alignment of the nearest creature in the sector.", x));
    }

    private static BrainOutputDefinition[] BuildOutputs()
    {
        var outputs = new[]
        {
            Output("action.move_forward", "Move forward", NeuralBrainSchema.MoveForwardOutput, BrainIoSignalGroup.Action, -1f, 1f, 0f, BrainOutputScope.PhysicalAction, "Forward movement strength, clamped to 0..1 by the dense adapter."),
            Output("action.turn", "Turn", NeuralBrainSchema.TurnOutput, BrainIoSignalGroup.Action, -1f, 1f, 0f, BrainOutputScope.PhysicalAction, "Left/right turn intent."),
            Output("action.eat", "Eat intent", NeuralBrainSchema.EatOutput, BrainIoSignalGroup.Action, -1f, 1f, 0f, BrainOutputScope.PhysicalAction, "Gate for eating when touching food."),
            Output("action.reproduce", "Reproduce intent", NeuralBrainSchema.ReproduceOutput, BrainIoSignalGroup.Action, -1f, 1f, 0f, BrainOutputScope.PhysicalAction, "Gate for laying an egg when reproduction rules allow it."),
            Output("action.attack", "Attack intent", NeuralBrainSchema.AttackOutput, BrainIoSignalGroup.Action, -1f, 1f, 0f, BrainOutputScope.PhysicalAction, "Gate for biting/damaging a contacted creature."),
            Output("action.grab", "Grab intent", NeuralBrainSchema.GrabOutput, BrainIoSignalGroup.Action, 0f, 1f, 0f, BrainOutputScope.PhysicalAction, "Continuous hold strength for grabbing a contacted creature.", introducedVersion: 2),
            Output("dense_memory.write_forward", "Dense memory write forward", NeuralBrainSchema.MemoryForwardOutput, BrainIoSignalGroup.Memory, -1f, 1f, 0f, BrainOutputScope.ArchitectureInternal, "Legacy dense-adapter memory write along the creature's forward axis."),
            Output("dense_memory.write_right", "Dense memory write right", NeuralBrainSchema.MemoryRightOutput, BrainIoSignalGroup.Memory, -1f, 1f, 0f, BrainOutputScope.ArchitectureInternal, "Legacy dense-adapter memory write along the creature's right axis.")
        };

        return ValidateOutputs(outputs);
    }

    private static BrainInputDefinition Input(
        string key,
        string name,
        int flatIndex,
        BrainIoSignalGroup group,
        float minimum,
        float maximum,
        float neutral,
        BrainInputFreshnessPolicy freshness,
        string meaning,
        int introducedVersion = 1)
    {
        return new BrainInputDefinition(
            key,
            name,
            flatIndex,
            group,
            minimum,
            maximum,
            neutral,
            introducedVersion,
            freshness,
            meaning);
    }

    private static BrainInputDefinition SectorInput(
        string keySuffix,
        string nameSuffix,
        int sectorIndex,
        int channelOffset,
        float minimum,
        float maximum,
        string meaning,
        float substrateX)
    {
        return new BrainInputDefinition(
            $"vision.sector.{sectorIndex}.{keySuffix}",
            $"Vision sector {sectorIndex} {nameSuffix}",
            NeuralBrainSchema.GetVisionSectorInput(sectorIndex, channelOffset),
            BrainIoSignalGroup.Vision,
            minimum,
            maximum,
            0f,
            1,
            BrainInputFreshnessPolicy.WorldSenseStale,
            meaning,
            substrateX,
            0f);
    }

    private static BrainOutputDefinition Output(
        string key,
        string name,
        int flatIndex,
        BrainIoSignalGroup group,
        float minimum,
        float maximum,
        float neutral,
        BrainOutputScope scope,
        string meaning,
        int introducedVersion = 1)
    {
        return new BrainOutputDefinition(
            key,
            name,
            flatIndex,
            group,
            minimum,
            maximum,
            neutral,
            introducedVersion,
            scope,
            meaning);
    }

    private static BrainInputDefinition[] ValidateInputs(List<BrainInputDefinition> inputs)
    {
        if (inputs.Count != NeuralBrainSchema.InputCount)
        {
            throw new InvalidOperationException($"Brain input registry has {inputs.Count} inputs but schema expects {NeuralBrainSchema.InputCount}.");
        }

        ValidateUniqueInputs(inputs);
        return inputs.OrderBy(input => input.FlatIndex).ToArray();
    }

    private static BrainOutputDefinition[] ValidateOutputs(BrainOutputDefinition[] outputs)
    {
        if (outputs.Length != NeuralBrainSchema.OutputCount)
        {
            throw new InvalidOperationException($"Brain output registry has {outputs.Length} outputs but schema expects {NeuralBrainSchema.OutputCount}.");
        }

        var indexSet = new HashSet<int>();
        var keySet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var output in outputs)
        {
            if (!indexSet.Add(output.FlatIndex))
            {
                throw new InvalidOperationException($"Duplicate brain output index {output.FlatIndex}.");
            }

            if (!keySet.Add(output.Key))
            {
                throw new InvalidOperationException($"Duplicate brain output key {output.Key}.");
            }

            if ((uint)output.FlatIndex >= NeuralBrainSchema.OutputCount)
            {
                throw new InvalidOperationException($"Brain output {output.Key} has invalid index {output.FlatIndex}.");
            }
        }

        return outputs.OrderBy(output => output.FlatIndex).ToArray();
    }

    private static void ValidateUniqueInputs(IReadOnlyList<BrainInputDefinition> inputs)
    {
        var indexSet = new HashSet<int>();
        var keySet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            if (!indexSet.Add(input.FlatIndex))
            {
                throw new InvalidOperationException($"Duplicate brain input index {input.FlatIndex}.");
            }

            if (!keySet.Add(input.Key))
            {
                throw new InvalidOperationException($"Duplicate brain input key {input.Key}.");
            }

            if ((uint)input.FlatIndex >= NeuralBrainSchema.InputCount)
            {
                throw new InvalidOperationException($"Brain input {input.Key} has invalid index {input.FlatIndex}.");
            }
        }
    }

    private static float SectorSubstrateX(int sectorIndex)
    {
        return (sectorIndex - VisionSectorSet.CenterSectorIndex) / (float)VisionSectorSet.CenterSectorIndex;
    }
}
