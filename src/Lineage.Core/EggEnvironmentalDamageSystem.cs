namespace Lineage.Core;

/// <summary>
/// Applies passive egg health damage from unsafe places in the world.
/// </summary>
///
/// <remarks>
/// This keeps the pressure environmental rather than cognitive: creatures still do
/// not sense biome identity or the void directly, but eggs laid in poor places are
/// less likely to hatch healthy offspring.
/// </remarks>
public sealed class EggEnvironmentalDamageSystem(float damagePerSecond = 0f) : ISimulationSystem
{
    private readonly float _damagePerSecond = ValidateDamage(damagePerSecond, nameof(damagePerSecond));

    public void Update(WorldState state, float deltaSeconds)
    {
        if (_damagePerSecond <= 0f || state.Eggs.Count == 0)
        {
            return;
        }

        var baseDamage = _damagePerSecond * deltaSeconds;
        for (var i = 0; i < state.Eggs.Count; i++)
        {
            var egg = state.Eggs[i];
            var exposure = GetExposureMultiplier(state.Biomes, egg.Position);
            if (exposure <= 0f)
            {
                continue;
            }

            egg.Health = Math.Max(0f, egg.Health - baseDamage * exposure);
            if (egg.Health <= 0f && egg.PendingDeathReason == EggDeathReason.Unknown)
            {
                egg.PendingDeathReason = EggDeathReason.EnvironmentalExposure;
            }

            state.Eggs[i] = egg;
        }
    }

    public static float GetExposureMultiplier(BiomeMap biomes, SimVector2 position)
    {
        if (biomes.IsInResourceVoid(position))
        {
            return 1f;
        }

        return BiomeKinds.Canonicalize(biomes.GetKindAt(position)) switch
        {
            BiomeKind.Desert => 0.5f,
            BiomeKind.Scrubland => 0.2f,
            BiomeKind.Tundra => 0.35f,
            BiomeKind.Highland => 0.15f,
            _ => 0f
        };
    }

    private static float ValidateDamage(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Egg environmental damage must be finite and non-negative.");
    }
}
