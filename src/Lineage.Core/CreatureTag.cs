namespace Lineage.Core;

public static class CreatureTag
{
    public const int MaxLength = 64;

    public static string? Normalize(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var normalized = tag.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new InvalidOperationException($"Creature tag cannot be longer than {MaxLength} characters.");
        }

        for (var i = 0; i < normalized.Length; i++)
        {
            if (char.IsControl(normalized[i]))
            {
                throw new InvalidOperationException("Creature tag cannot contain control characters.");
            }
        }

        return normalized;
    }

    public static string Display(string? tag)
    {
        return string.IsNullOrWhiteSpace(tag) ? "untagged" : tag.Trim();
    }
}
