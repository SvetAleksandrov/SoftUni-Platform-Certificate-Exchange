namespace SoftUni_Platform_Certificate_Exchange.Helpers;

using System;
using System.Linq;

// Обща подготовка на параметрите за филтриране
public static class FilterInputHelper
{
    // Премахваме интервалите и обработваме null
    public static string Search(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    // Проверяваме разрешените стойности за Sort и Group By
    public static string Option(
        string? value,
        string defaultValue,
        params string[] allowedValues)
    {
        var normalized = Search(value).ToLowerInvariant();

        return allowedValues.Contains(normalized)
            ? normalized
            : defaultValue;
    }
}
