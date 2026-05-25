namespace Lineage.Core;

/// <summary>
/// Architecture-neutral body/world information available to a brain for one decision step.
/// </summary>
///
/// <remarks>
/// This frame is the boundary between the simulated body and the brain implementation. It
/// deliberately groups inputs by meaning instead of by the current neural network's flat
/// input indices, so future brains can consume the same facts without pretending to be a
/// fixed-weight neural net.
/// </remarks>
public readonly record struct BrainInputFrame(
    VisionInputFrame Vision,
    ScentInputFrame Scent,
    BodyInputFrame Body,
    InternalInputFrame Internal)
{
    public static BrainInputFrame FromSenses(CreatureSenseState senses, CreatureGenome genome)
    {
        return new BrainInputFrame(
            new VisionInputFrame(
                new DirectionalObjectSignal(
                    senses.FoodDetected,
                    senses.FoodProximity,
                    senses.FoodDirectionForward,
                    senses.FoodDirectionRight,
                    senses.VisibleFoodDensity),
                new DirectionalObjectSignal(
                    senses.PlantDetected,
                    senses.PlantProximity,
                    senses.PlantDirectionForward,
                    senses.PlantDirectionRight,
                    senses.VisiblePlantDensity),
                senses.VisiblePlantEnergyQuality,
                senses.VisiblePlantBiteEase,
                new DirectionalObjectSignal(
                    senses.MeatDetected,
                    senses.MeatProximity,
                    senses.MeatDirectionForward,
                    senses.MeatDirectionRight,
                    senses.VisibleMeatDensity),
                senses.VisibleMeatFreshness,
                senses.VisionSectors,
                new DirectionalObjectSignal(
                    senses.CreatureDetected,
                    senses.CreatureProximity,
                    senses.CreatureDirectionForward,
                    senses.CreatureDirectionRight,
                    senses.VisibleCreatureDensity),
                senses.CreatureRelativeBodySize,
                senses.CreatureRelativeSpeed,
                senses.CreatureApproachRate,
                senses.CreatureFacingAlignment),
            new ScentInputFrame(
                new DirectionalGradientSignal(
                    senses.MeatScentDetected,
                    senses.MeatScentDensity,
                    senses.MeatScentDirectionForward,
                    senses.MeatScentDirectionRight),
                new DirectionalGradientSignal(
                    senses.RottenMeatScentDetected,
                    senses.RottenMeatScentDensity,
                    senses.RottenMeatScentDirectionForward,
                    senses.RottenMeatScentDirectionRight)),
            new BodyInputFrame(
                senses.CurrentTerrainDrag,
                senses.ForwardTerrainDrag,
                senses.LeftTerrainDrag,
                senses.RightTerrainDrag,
                senses.ForwardObstacle,
                senses.LeftObstacle,
                senses.RightObstacle,
                senses.MovementBlocked,
                senses.FoodContact,
                senses.PlantFoodContact,
                senses.PlantFoodContactEnergyQuality,
                senses.PlantFoodContactBiteEase,
                senses.MeatFoodContact,
                senses.EggFoodContact,
                senses.CreatureContact),
            new InternalInputFrame(
                senses.EnergyRatio,
                senses.HealthRatio,
                senses.Hunger,
                genome.DietaryAdaptation,
                senses.EggReserveRatio,
                senses.ReproductionReadiness,
                senses.EnergySurplusRatio,
                senses.RecentFoodSuccess,
                senses.RecentPlantRawYield,
                senses.RecentPlantEnergyYield));
    }
}

/// <summary>
/// Directional perception of object-like things such as plants, meat, eggs, or creatures.
/// </summary>
public readonly record struct DirectionalObjectSignal(
    bool Detected,
    float Proximity,
    float DirectionForward,
    float DirectionRight,
    float Density);

/// <summary>
/// Coarse directional gradient for chemical cues such as meat scent or rot scent.
/// </summary>
public readonly record struct DirectionalGradientSignal(
    bool Detected,
    float Density,
    float DirectionForward,
    float DirectionRight);

/// <summary>
/// Visual facts currently exposed to brains. Sector/ray vision should expand this bucket.
/// </summary>
public readonly record struct VisionInputFrame(
    DirectionalObjectSignal Food,
    DirectionalObjectSignal Plant,
    float PlantEnergyQuality,
    float PlantBiteEase,
    DirectionalObjectSignal Meat,
    float MeatFreshness,
    VisionSectorSet Sectors,
    DirectionalObjectSignal Creature,
    float CreatureRelativeBodySize,
    float CreatureRelativeSpeed,
    float CreatureApproachRate,
    float CreatureFacingAlignment);

/// <summary>
/// Chemical cues. These should remain lower-resolution than vision.
/// </summary>
public readonly record struct ScentInputFrame(
    DirectionalGradientSignal Meat,
    DirectionalGradientSignal RottenMeat);

/// <summary>
/// Touch/contact/proprioception: facts the body feels directly rather than sees or smells.
/// </summary>
public readonly record struct BodyInputFrame(
    float CurrentTerrainDrag,
    float ForwardTerrainDrag,
    float LeftTerrainDrag,
    float RightTerrainDrag,
    float ForwardObstacle,
    float LeftObstacle,
    float RightObstacle,
    float MovementBlocked,
    float FoodContact,
    float PlantFoodContact,
    float PlantFoodContactEnergyQuality,
    float PlantFoodContactBiteEase,
    float MeatFoodContact,
    float EggFoodContact,
    float CreatureContact);

/// <summary>
/// Internal body condition and drives available to the brain.
/// </summary>
public readonly record struct InternalInputFrame(
    float EnergyRatio,
    float HealthRatio,
    float Hunger,
    float DietaryMeatBias,
    float EggReserveRatio,
    float ReproductionReadiness,
    float EnergySurplusRatio,
    float RecentFoodSuccess,
    float RecentPlantRawYield,
    float RecentPlantEnergyYield);

/// <summary>
/// Architecture-neutral action intents produced by a brain before later systems resolve them.
/// </summary>
public readonly record struct BrainOutputFrame(
    float MoveForward,
    float Turn,
    float Eat,
    float Reproduce,
    float Attack);

/// <summary>
/// Temporary bridge for the current fixed neural brain's controller-managed memory.
/// </summary>
///
/// <remarks>
/// This is intentionally not part of <see cref="BrainInputFrame"/>. Future recurrent or
/// topology-evolving brains should own their own memory internally.
/// </remarks>
public readonly record struct LegacyNeuralMemoryInputFrame(
    float DirectionForward,
    float DirectionRight,
    float Strength)
{
    public static LegacyNeuralMemoryInputFrame FromSenses(CreatureSenseState senses)
    {
        return new LegacyNeuralMemoryInputFrame(
            senses.MemoryDirectionForward,
            senses.MemoryDirectionRight,
            senses.MemoryStrength);
    }
}

/// <summary>
/// Temporary bridge for memory write outputs emitted by the current fixed neural brain.
/// </summary>
public readonly record struct LegacyNeuralMemoryOutputFrame(
    float DirectionForward,
    float DirectionRight);
