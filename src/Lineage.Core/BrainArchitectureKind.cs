namespace Lineage.Core;

/// <summary>
/// Selects the evolvable decision-making architecture used for creature brains.
/// </summary>
public enum BrainArchitectureKind
{
    /// <summary>
    /// Current fixed neural controller with direct input-to-output weights plus optional hidden nodes.
    /// </summary>
    HybridNeural = 0
}
