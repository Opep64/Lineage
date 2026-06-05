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
    CommunicationInputFrame Communication,
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
                senses.PlantPreferenceDensity,
                senses.PlantPreferenceDirectionForward,
                senses.PlantPreferenceDirectionRight,
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
                    senses.RottenMeatScentDirectionRight),
                new DirectionalGradientSignal(
                    senses.CreatureSimilarityScentDetected,
                    senses.CreatureSimilarityScentDensity,
                    senses.CreatureSimilarityScentDirectionForward,
                    senses.CreatureSimilarityScentDirectionRight),
                new DirectionalGradientSignal(
                    senses.CreatureLineageScentDetected,
                    senses.CreatureLineageScentDensity,
                    senses.CreatureLineageScentDirectionForward,
                    senses.CreatureLineageScentDirectionRight),
                new DirectionalGradientSignal(
                    senses.EggLineageScentDetected,
                    senses.EggLineageScentDensity,
                    senses.EggLineageScentDirectionForward,
                    senses.EggLineageScentDirectionRight),
                new DirectionalGradientSignal(
                    senses.CreatureIdentityScentDetected,
                    senses.CreatureIdentityScentDensity,
                    senses.CreatureIdentityScentDirectionForward,
                    senses.CreatureIdentityScentDirectionRight),
                new DirectionalGradientSignal(
                    senses.EggIdentityScentDetected,
                    senses.EggIdentityScentDensity,
                    senses.EggIdentityScentDirectionForward,
                    senses.EggIdentityScentDirectionRight)),
            new CommunicationInputFrame(
                new DirectionalToneSignal(
                    senses.SoundDetected,
                    senses.SoundDensity,
                    senses.SoundDirectionForward,
                    senses.SoundDirectionRight,
                    senses.SoundTone,
                    senses.SoundToneClarity)),
            new BodyInputFrame(
                senses.CurrentTerrainDrag,
                senses.ForwardTerrainDrag,
                senses.LeftTerrainDrag,
                senses.RightTerrainDrag,
                senses.CurrentHabitatQuality,
                senses.ForwardHabitatQuality,
                senses.LeftHabitatQuality,
                senses.RightHabitatQuality,
                senses.CurrentTemperature,
                senses.ForwardTemperature,
                senses.LeftTemperature,
                senses.RightTemperature,
                senses.CurrentThermalMismatch,
                senses.ForwardThermalMismatch,
                senses.LeftThermalMismatch,
                senses.RightThermalMismatch,
                senses.ForwardObstacle,
                senses.LeftObstacle,
                senses.RightObstacle,
                senses.MovementBlocked,
                senses.FoodContact,
                senses.PlantFoodContact,
                senses.PlantFoodContactEnergyQuality,
                senses.PlantFoodContactBiteEase,
                senses.PlantFoodContactPreference,
                senses.MeatFoodContact,
                senses.EggFoodContact,
                senses.CreatureContact,
                senses.CreatureContactSimilarity,
                senses.CreatureContactLineageSimilarity,
                senses.EggContactLineageSimilarity,
                senses.CreatureContactIdentitySimilarity,
                senses.EggContactIdentitySimilarity,
                senses.GrabPressure,
                senses.GrabDirectionForward,
                senses.GrabDirectionRight,
                senses.CanGrabCreature,
                senses.IsHoldingCreature),
            new InternalInputFrame(
                senses.EnergyRatio,
                senses.HealthRatio,
                senses.Hunger,
                senses.MaturityProgress,
                genome.DietaryAdaptation,
                senses.EggReserveRatio,
                senses.ReproductionReadiness,
                senses.EnergySurplusRatio,
                senses.EnergyFullnessRatio,
                senses.GutFullnessRatio,
                senses.FatRatio,
                senses.MassBurdenRatio,
                senses.RecentFoodSuccess,
                senses.RecentPlantRawYield,
                senses.RecentPlantEnergyYield,
                senses.RecentTenderPlantEnergyYield,
                senses.RecentRichPlantEnergyYield,
                senses.RecentToughPlantEnergyYield,
                senses.TenderPlantPayoffTrace,
                senses.RichPlantPayoffTrace,
                senses.ToughPlantPayoffTrace,
                senses.RecentFoodEnergyYield,
                senses.InjuryMemoryDirectionForward,
                senses.InjuryMemoryDirectionRight,
                senses.InjuryMemoryStrength));
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
/// Directional intentional communication signal with a continuous tone channel.
/// </summary>
public readonly record struct DirectionalToneSignal(
    bool Detected,
    float Density,
    float DirectionForward,
    float DirectionRight,
    float Tone,
    float ToneClarity);

/// <summary>
/// Visual facts currently exposed to brains. Sector/ray vision should expand this bucket.
/// </summary>
public readonly record struct VisionInputFrame(
    DirectionalObjectSignal Food,
    DirectionalObjectSignal Plant,
    float PlantEnergyQuality,
    float PlantBiteEase,
    float PlantPreferenceDensity,
    float PlantPreferenceDirectionForward,
    float PlantPreferenceDirectionRight,
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
    DirectionalGradientSignal RottenMeat,
    DirectionalGradientSignal CreatureSimilarity,
    DirectionalGradientSignal CreatureLineage,
    DirectionalGradientSignal EggLineage,
    DirectionalGradientSignal CreatureIdentity,
    DirectionalGradientSignal EggIdentity);

/// <summary>
/// Intentional signals emitted by creature actions.
/// </summary>
public readonly record struct CommunicationInputFrame(
    DirectionalToneSignal Sound);

/// <summary>
/// Touch/contact/proprioception: facts the body feels directly rather than sees or smells.
/// </summary>
public readonly record struct BodyInputFrame(
    float CurrentTerrainDrag,
    float ForwardTerrainDrag,
    float LeftTerrainDrag,
    float RightTerrainDrag,
    float CurrentHabitatQuality,
    float ForwardHabitatQuality,
    float LeftHabitatQuality,
    float RightHabitatQuality,
    float CurrentTemperature,
    float ForwardTemperature,
    float LeftTemperature,
    float RightTemperature,
    float CurrentThermalMismatch,
    float ForwardThermalMismatch,
    float LeftThermalMismatch,
    float RightThermalMismatch,
    float ForwardObstacle,
    float LeftObstacle,
    float RightObstacle,
    float MovementBlocked,
    float FoodContact,
    float PlantFoodContact,
    float PlantFoodContactEnergyQuality,
    float PlantFoodContactBiteEase,
    float PlantFoodContactPreference,
    float MeatFoodContact,
    float EggFoodContact,
    float CreatureContact,
    float CreatureContactSimilarity,
    float CreatureContactLineageSimilarity,
    float EggContactLineageSimilarity,
    float CreatureContactIdentitySimilarity,
    float EggContactIdentitySimilarity,
    float GrabPressure,
    float GrabDirectionForward,
    float GrabDirectionRight,
    float CanGrabCreature,
    float IsHoldingCreature);

/// <summary>
/// Internal body condition and drives available to the brain.
/// </summary>
public readonly record struct InternalInputFrame(
    float EnergyRatio,
    float HealthRatio,
    float Hunger,
    float MaturityProgress,
    float DietaryMeatBias,
    float EggReserveRatio,
    float ReproductionReadiness,
    float EnergySurplusRatio,
    float EnergyFullnessRatio,
    float GutFullnessRatio,
    float FatRatio,
    float MassBurdenRatio,
    float RecentFoodSuccess,
    float RecentPlantRawYield,
    float RecentPlantEnergyYield,
    float RecentTenderPlantEnergyYield,
    float RecentRichPlantEnergyYield,
    float RecentToughPlantEnergyYield,
    float TenderPlantPayoffTrace,
    float RichPlantPayoffTrace,
    float ToughPlantPayoffTrace,
    float RecentFoodEnergyYield,
    float InjuryMemoryDirectionForward,
    float InjuryMemoryDirectionRight,
    float InjuryMemoryStrength);

/// <summary>
/// Architecture-neutral action intents produced by a brain before later systems resolve them.
/// </summary>
public readonly record struct BrainOutputFrame(
    float MoveForward,
    float Turn,
    float Eat,
    float Reproduce,
    float Attack,
    float Grab,
    float SoundAmplitude,
    float SoundTone);

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
