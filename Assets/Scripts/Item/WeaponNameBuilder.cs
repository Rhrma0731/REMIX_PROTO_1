using System.Collections.Generic;
using System.Linq;

public static class WeaponNameBuilder
{
    public static string BuildName(List<ItemData> equippedItems)
    {
        if (equippedItems == null || equippedItems.Count == 0)
            return string.Empty;

        var specialKeywords = equippedItems
            .Where(i => i.Category == ItemCategory.Special && !string.IsNullOrEmpty(i.Keyword))
            .OrderByDescending(i => i.Priority)
            .Select(i => i.Keyword);

        var modifierKeywords = equippedItems
            .Where(i => i.Category == ItemCategory.Modifier && !string.IsNullOrEmpty(i.Keyword))
            .OrderByDescending(i => i.Priority)
            .Select(i => i.Keyword);

        var formKeywords = equippedItems
            .Where(i => i.Category == ItemCategory.Form && !string.IsNullOrEmpty(i.Keyword))
            .OrderByDescending(i => i.Priority)
            .Select(i => i.Keyword);

        var allKeywords = specialKeywords.Concat(modifierKeywords).Concat(formKeywords);
        return string.Join(" ", allKeywords);
    }
}
