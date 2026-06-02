namespace Lineage.Core;

/// <summary>
/// Selects the evolvable decision-making architecture used for creature brains.
/// </summary>
public enum BrainArchitectureKind
{
    /// <summary>
    /// Current fixed neural controller with direct input-to-output weights plus optional hidden nodes.
    /// </summary>
    HybridNeural = 0,

    /// <summary>
    /// Fixed neural controller that routes all inputs through a hidden layer before outputs.
    /// </summary>
    HiddenLayerNeural = 1,

    /// <summary>
    /// Sparse, topology-evolving graph controller inspired by rtNEAT/Bibites-style brains.
    /// </summary>
    RtNeatGraph = 2,

    /// <summary>
    /// Fixed neural controller with direct input-to-output weights plus two hidden layers of eight nodes each.
    /// </summary>
    HybridDeep8x8Neural = 3
}
