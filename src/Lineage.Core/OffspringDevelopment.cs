namespace Lineage.Core;

/// <summary>
/// Shared egg and hatchling scaling formulas derived from reproductive investment.
/// </summary>
public static class OffspringDevelopment
{
    private const float MinimumInvestmentRatio = 0.25f;
    private const float MaximumInvestmentRatio = 4f;

    public static float InvestmentRatio(float investedEnergy)
    {
        if (!float.IsFinite(investedEnergy) || investedEnergy <= 0f)
        {
            return 1f;
        }

        return Math.Clamp(
            investedEnergy / CreatureGenome.Baseline.OffspringEnergyInvestment,
            MinimumInvestmentRatio,
            MaximumInvestmentRatio);
    }

    public static float EggMaxHealth(float investmentRatio)
    {
        return Math.Clamp(MathF.Sqrt(NormalizeInvestmentRatio(investmentRatio)), 0.5f, 2f);
    }

    public static float HatchlingHealth(float eggHealth, float eggMaxHealth, float investmentRatio)
    {
        var healthRatio = eggMaxHealth > 0f
            ? Math.Clamp(eggHealth / eggMaxHealth, 0.05f, 1f)
            : 1f;
        return Math.Clamp(MathF.Sqrt(NormalizeInvestmentRatio(investmentRatio)) * healthRatio, 0.1f, 2f);
    }

    public static float JuvenileGrowthScale(float birthInvestmentRatio)
    {
        return Math.Clamp(MathF.Sqrt(NormalizeInvestmentRatio(birthInvestmentRatio)), 0.5f, 2f);
    }

    public static float NormalizeInvestmentRatio(float investmentRatio)
    {
        return float.IsFinite(investmentRatio) && investmentRatio > 0f
            ? Math.Clamp(investmentRatio, MinimumInvestmentRatio, MaximumInvestmentRatio)
            : 1f;
    }
}
