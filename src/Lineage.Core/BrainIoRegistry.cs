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
    Sound,
    Terrain,
    Habitat,
    Climate,
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
            Input("internal.energy_ratio", "Energy ratio", NeuralBrainSchema.EnergyRatioInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current working energy divided by reproduction threshold."),
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
            Input("contact.movement_blocked", "Movement blocked", NeuralBrainSchema.MovementBlockedInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Recent hard movement block contact cue."),
            Input("vision.food.proximity", "Food proximity", NeuralBrainSchema.FoodProximityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest or best visible food-like target proximity.", introducedVersion: 13),
            Input("vision.food.direction_forward", "Food direction forward", NeuralBrainSchema.FoodDirectionForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Food direction projected onto the creature's forward axis.", introducedVersion: 13),
            Input("vision.food.direction_right", "Food direction right", NeuralBrainSchema.FoodDirectionRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Food direction projected onto the creature's right axis.", introducedVersion: 13),
            Input("vision.plant.proximity", "Plant proximity", NeuralBrainSchema.PlantProximityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible plant proximity.", introducedVersion: 13),
            Input("vision.plant.direction_forward", "Plant direction forward", NeuralBrainSchema.PlantDirectionForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Plant direction projected onto the creature's forward axis.", introducedVersion: 13),
            Input("vision.plant.direction_right", "Plant direction right", NeuralBrainSchema.PlantDirectionRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Plant direction projected onto the creature's right axis.", introducedVersion: 13),
            Input("vision.meat.proximity", "Meat proximity", NeuralBrainSchema.MeatProximityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible meat-like target proximity.", introducedVersion: 13),
            Input("vision.meat.direction_forward", "Meat direction forward", NeuralBrainSchema.MeatDirectionForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Meat direction projected onto the creature's forward axis.", introducedVersion: 13),
            Input("vision.meat.direction_right", "Meat direction right", NeuralBrainSchema.MeatDirectionRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Meat direction projected onto the creature's right axis.", introducedVersion: 13),
            Input("vision.egg_density", "Visible egg density", NeuralBrainSchema.VisibleEggDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Visible egg mass.", introducedVersion: 13),
            Input("vision.egg.proximity", "Egg proximity", NeuralBrainSchema.EggProximityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible egg proximity.", introducedVersion: 13),
            Input("vision.egg.direction_forward", "Egg direction forward", NeuralBrainSchema.EggDirectionForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Egg direction projected onto the creature's forward axis.", introducedVersion: 13),
            Input("vision.egg.direction_right", "Egg direction right", NeuralBrainSchema.EggDirectionRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Egg direction projected onto the creature's right axis.", introducedVersion: 13),
            Input("vision.egg.lineage_similarity", "Egg lineage similarity", NeuralBrainSchema.EggVisualLineageSimilarityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Founder-lineage match of the nearest visible egg.", introducedVersion: 13),
            Input("vision.egg.identity_similarity", "Egg identity similarity", NeuralBrainSchema.EggVisualIdentitySimilarityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Heritable scent-signature match of the nearest visible egg.", introducedVersion: 13),
            Input("vision.small_prey_density", "Visible small prey density", NeuralBrainSchema.VisibleSmallPreyDensityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Visible small mobile prey mass.", introducedVersion: 13),
            Input("vision.small_prey.proximity", "Small prey proximity", NeuralBrainSchema.SmallPreyProximityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible small prey proximity.", introducedVersion: 13),
            Input("vision.small_prey.direction_forward", "Small prey direction forward", NeuralBrainSchema.SmallPreyDirectionForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Small prey direction projected onto the creature's forward axis.", introducedVersion: 13),
            Input("vision.small_prey.direction_right", "Small prey direction right", NeuralBrainSchema.SmallPreyDirectionRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Small prey direction projected onto the creature's right axis.", introducedVersion: 13),
            Input("vision.small_prey.grab_opportunity", "Small prey grab opportunity", NeuralBrainSchema.SmallPreyGrabOpportunityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Close-range opportunity to grab or pin visible small prey.", introducedVersion: 13),
            Input("vision.creature.proximity", "Creature proximity", NeuralBrainSchema.CreatureProximityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible creature proximity.", introducedVersion: 13),
            Input("vision.creature.direction_forward", "Creature direction forward", NeuralBrainSchema.CreatureDirectionForwardInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Creature direction projected onto the creature's forward axis.", introducedVersion: 13),
            Input("vision.creature.direction_right", "Creature direction right", NeuralBrainSchema.CreatureDirectionRightInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Creature direction projected onto the creature's right axis.", introducedVersion: 13),
            Input("vision.creature.relative_body_size", "Creature relative body size", NeuralBrainSchema.CreatureRelativeBodySizeInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible creature body size relative to self.", introducedVersion: 13),
            Input("vision.creature.relative_speed", "Creature relative speed", NeuralBrainSchema.CreatureRelativeSpeedInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Nearest visible creature speed relative to self.", introducedVersion: 13),
            Input("vision.creature.approach_rate", "Creature approach rate", NeuralBrainSchema.CreatureApproachRateInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Closing rate of the nearest visible creature.", introducedVersion: 13),
            Input("vision.creature.facing_alignment", "Creature facing alignment", NeuralBrainSchema.CreatureFacingAlignmentInput, BrainIoSignalGroup.Vision, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Facing alignment of the nearest visible creature.", introducedVersion: 13),
            Input("vision.creature.trait_similarity", "Creature trait similarity", NeuralBrainSchema.CreatureVisualTraitSimilarityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Trait similarity of the nearest visible creature.", introducedVersion: 13),
            Input("vision.creature.lineage_similarity", "Creature lineage similarity", NeuralBrainSchema.CreatureVisualLineageSimilarityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Founder-lineage match of the nearest visible creature.", introducedVersion: 13),
            Input("vision.creature.identity_similarity", "Creature identity similarity", NeuralBrainSchema.CreatureVisualIdentitySimilarityInput, BrainIoSignalGroup.Vision, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Heritable scent-signature match of the nearest visible creature.", introducedVersion: 13)
        };

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
            Input("scent.creature_lineage_density", "Creature lineage scent density", NeuralBrainSchema.CreatureLineageScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Scent strength from same-founder creatures.", introducedVersion: 8),
            Input("scent.creature_lineage_forward", "Creature lineage scent forward", NeuralBrainSchema.CreatureLineageScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Lineage scent direction along the creature's forward axis.", introducedVersion: 8),
            Input("scent.creature_lineage_right", "Creature lineage scent right", NeuralBrainSchema.CreatureLineageScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Lineage scent direction along the creature's right axis.", introducedVersion: 8),
            Input("contact.creature_lineage_similarity", "Creature contact lineage similarity", NeuralBrainSchema.CreatureContactLineageSimilarityInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Founder-lineage match of a contacted creature.", introducedVersion: 8),
            Input("contact.egg_lineage_similarity", "Egg contact lineage similarity", NeuralBrainSchema.EggContactLineageSimilarityInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Founder-lineage match of a contacted egg's parent.", introducedVersion: 8),
            Input("habitat.current_quality", "Current habitat quality", NeuralBrainSchema.CurrentHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality under the creature."),
            Input("habitat.forward_quality", "Forward habitat quality", NeuralBrainSchema.ForwardHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality sampled ahead."),
            Input("habitat.left_quality", "Left habitat quality", NeuralBrainSchema.LeftHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality sampled to the left."),
            Input("habitat.right_quality", "Right habitat quality", NeuralBrainSchema.RightHabitatQualityInput, BrainIoSignalGroup.Habitat, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Productive habitat quality sampled to the right."),
            Input("contact.grab_pressure", "Grab pressure", NeuralBrainSchema.GrabPressureInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Strength of another creature's hold on this creature.", introducedVersion: 3),
            Input("contact.grab_forward", "Grab direction forward", NeuralBrainSchema.GrabDirectionForwardInput, BrainIoSignalGroup.Contact, -1f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Direction toward the grabbing creature projected forward.", introducedVersion: 3),
            Input("contact.grab_right", "Grab direction right", NeuralBrainSchema.GrabDirectionRightInput, BrainIoSignalGroup.Contact, -1f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Direction toward the grabbing creature projected right.", introducedVersion: 3),
            Input("contact.can_grab_creature", "Can grab creature", NeuralBrainSchema.CanGrabCreatureInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "A creature is close enough to grab.", introducedVersion: 3),
            Input("contact.is_holding_creature", "Is holding creature", NeuralBrainSchema.IsHoldingCreatureInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "This creature is currently holding another creature.", introducedVersion: 3),
            Input("sound.density", "Sound density", NeuralBrainSchema.SoundDensityInput, BrainIoSignalGroup.Sound, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Strength of nearby intentional communication sound.", introducedVersion: 4),
            Input("sound.forward", "Sound direction forward", NeuralBrainSchema.SoundDirectionForwardInput, BrainIoSignalGroup.Sound, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Sound direction projected onto the creature's forward axis.", introducedVersion: 4),
            Input("sound.right", "Sound direction right", NeuralBrainSchema.SoundDirectionRightInput, BrainIoSignalGroup.Sound, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Sound direction projected onto the creature's right axis.", introducedVersion: 4),
            Input("sound.tone", "Sound tone", NeuralBrainSchema.SoundToneInput, BrainIoSignalGroup.Sound, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Weighted average tone of nearby intentional sound.", introducedVersion: 4),
            Input("sound.tone_clarity", "Sound tone clarity", NeuralBrainSchema.SoundToneClarityInput, BrainIoSignalGroup.Sound, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Confidence that heard sound is a coherent tone rather than a mixed signal.", introducedVersion: 4),
            Input("internal.fat_ratio", "Fat ratio", NeuralBrainSchema.FatRatioInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current stored fat divided by fat capacity.", introducedVersion: 5),
            Input("internal.mass_burden", "Mass burden", NeuralBrainSchema.MassBurdenInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current fat-derived movement burden.", introducedVersion: 5),
            Input("climate.current_temperature", "Current temperature", NeuralBrainSchema.CurrentTemperatureInput, BrainIoSignalGroup.Climate, 0f, 1f, 0.5f, BrainInputFreshnessPolicy.WorldSenseStale, "Normalized temperature under the creature.", introducedVersion: 6),
            Input("climate.forward_temperature", "Forward temperature", NeuralBrainSchema.ForwardTemperatureInput, BrainIoSignalGroup.Climate, 0f, 1f, 0.5f, BrainInputFreshnessPolicy.WorldSenseStale, "Normalized temperature sampled ahead.", introducedVersion: 6),
            Input("climate.left_temperature", "Left temperature", NeuralBrainSchema.LeftTemperatureInput, BrainIoSignalGroup.Climate, 0f, 1f, 0.5f, BrainInputFreshnessPolicy.WorldSenseStale, "Normalized temperature sampled to the left.", introducedVersion: 6),
            Input("climate.right_temperature", "Right temperature", NeuralBrainSchema.RightTemperatureInput, BrainIoSignalGroup.Climate, 0f, 1f, 0.5f, BrainInputFreshnessPolicy.WorldSenseStale, "Normalized temperature sampled to the right.", introducedVersion: 6),
            Input("climate.current_thermal_mismatch", "Current thermal mismatch", NeuralBrainSchema.CurrentThermalMismatchInput, BrainIoSignalGroup.Climate, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Mismatch between local temperature and the creature's thermal optimum.", introducedVersion: 6),
            Input("climate.forward_thermal_mismatch", "Forward thermal mismatch", NeuralBrainSchema.ForwardThermalMismatchInput, BrainIoSignalGroup.Climate, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Thermal mismatch sampled ahead.", introducedVersion: 6),
            Input("climate.left_thermal_mismatch", "Left thermal mismatch", NeuralBrainSchema.LeftThermalMismatchInput, BrainIoSignalGroup.Climate, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Thermal mismatch sampled to the left.", introducedVersion: 6),
            Input("climate.right_thermal_mismatch", "Right thermal mismatch", NeuralBrainSchema.RightThermalMismatchInput, BrainIoSignalGroup.Climate, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Thermal mismatch sampled to the right.", introducedVersion: 6),
            Input("internal.energy_fullness", "Energy fullness", NeuralBrainSchema.EnergyFullnessInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current working energy divided by working energy capacity.", introducedVersion: 7),
            Input("internal.gut_fullness", "Gut fullness", NeuralBrainSchema.GutFullnessInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Current gut contents divided by gut capacity.", introducedVersion: 7),
            Input("scent.egg_lineage_density", "Egg lineage scent density", NeuralBrainSchema.EggLineageScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Scent strength from same-founder eggs.", introducedVersion: 9),
            Input("scent.egg_lineage_forward", "Egg lineage scent forward", NeuralBrainSchema.EggLineageScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Egg lineage scent direction along the creature's forward axis.", introducedVersion: 9),
            Input("scent.egg_lineage_right", "Egg lineage scent right", NeuralBrainSchema.EggLineageScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Egg lineage scent direction along the creature's right axis.", introducedVersion: 9),
            Input("scent.creature_identity_density", "Creature identity scent density", NeuralBrainSchema.CreatureIdentityScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Scent strength from creatures with similar heritable scent signatures.", introducedVersion: 10),
            Input("scent.creature_identity_forward", "Creature identity scent forward", NeuralBrainSchema.CreatureIdentityScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Identity scent direction along the creature's forward axis.", introducedVersion: 10),
            Input("scent.creature_identity_right", "Creature identity scent right", NeuralBrainSchema.CreatureIdentityScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Identity scent direction along the creature's right axis.", introducedVersion: 10),
            Input("scent.egg_identity_density", "Egg identity scent density", NeuralBrainSchema.EggIdentityScentDensityInput, BrainIoSignalGroup.Scent, 0f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Scent strength from eggs with similar heritable scent signatures.", introducedVersion: 10),
            Input("scent.egg_identity_forward", "Egg identity scent forward", NeuralBrainSchema.EggIdentityScentForwardInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Egg identity scent direction along the creature's forward axis.", introducedVersion: 10),
            Input("scent.egg_identity_right", "Egg identity scent right", NeuralBrainSchema.EggIdentityScentRightInput, BrainIoSignalGroup.Scent, -1f, 1f, 0f, BrainInputFreshnessPolicy.WorldSenseStale, "Egg identity scent direction along the creature's right axis.", introducedVersion: 10),
            Input("contact.creature_identity_similarity", "Creature contact identity similarity", NeuralBrainSchema.CreatureContactIdentitySimilarityInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Heritable scent-signature match of a contacted creature.", introducedVersion: 10),
            Input("contact.egg_identity_similarity", "Egg contact identity similarity", NeuralBrainSchema.EggContactIdentitySimilarityInput, BrainIoSignalGroup.Contact, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Heritable scent-signature match of a contacted egg.", introducedVersion: 10),
            Input("internal.injury_memory_forward", "Injury memory forward", NeuralBrainSchema.InjuryMemoryForwardInput, BrainIoSignalGroup.Internal, -1f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Decaying memory of the recent injury source projected forward.", introducedVersion: 11),
            Input("internal.injury_memory_right", "Injury memory right", NeuralBrainSchema.InjuryMemoryRightInput, BrainIoSignalGroup.Internal, -1f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Decaying memory of the recent injury source projected right.", introducedVersion: 11),
            Input("internal.injury_memory_strength", "Injury memory strength", NeuralBrainSchema.InjuryMemoryStrengthInput, BrainIoSignalGroup.Internal, 0f, 1f, 0f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Magnitude of the decaying recent injury source memory.", introducedVersion: 11),
            Input("internal.maturity_progress", "Maturity progress", NeuralBrainSchema.MaturityProgressInput, BrainIoSignalGroup.Internal, 0f, 1f, 1f, BrainInputFreshnessPolicy.InternalOrContactFresh, "Juvenile-to-adult development progress after metabolic pace scaling.", introducedVersion: 12)
        });

        return ValidateInputs(inputs);
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
            Output("action.sound_amplitude", "Sound amplitude", NeuralBrainSchema.SoundAmplitudeOutput, BrainIoSignalGroup.Sound, 0f, 1f, 0f, BrainOutputScope.PhysicalAction, "Intentional communication sound volume.", introducedVersion: 3),
            Output("action.sound_tone", "Sound tone", NeuralBrainSchema.SoundToneOutput, BrainIoSignalGroup.Sound, -1f, 1f, 0f, BrainOutputScope.PhysicalAction, "Intentional communication tone in a continuous -1..1 range.", introducedVersion: 3),
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

}
