namespace Lineage.Core;

/// <summary>
/// Controller outputs resolved by later action systems.
/// </summary>
public struct CreatureActionState
{
    public float MoveForward { get; set; }

    public float Turn { get; set; }

    public float EatOutput { get; set; }

    public float ReproduceOutput { get; set; }

    public float AttackOutput { get; set; }

    public float GrabOutput { get; set; }

    public float SoundAmplitude { get; set; }

    public float SoundTone { get; set; }

    public bool WantsEat { get; set; }

    public bool WantsReproduce { get; set; }

    public bool WantsAttack { get; set; }

    public bool WantsGrab { get; set; }

    public float MemoryForward { get; set; }

    public float MemoryRight { get; set; }
}
