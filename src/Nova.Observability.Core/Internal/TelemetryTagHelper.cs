using System.Collections.Generic;
using System.Diagnostics;

namespace Nova.Observability.Core;

internal static class TelemetryTagHelper
{
    internal static void Apply(
        Activity? activity,
        IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        if (activity == null || tags == null)
            return;

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Key))
                continue;

            activity.SetTag(tag.Key, tag.Value);
        }
    }

    internal static ActivityTagsCollection CreateActivityTags(
        IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        var result = new ActivityTagsCollection();

        if (tags == null)
            return result;

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Key))
                continue;

            // Indexer kullanıldığı için aynı anahtar tekrar gelirse
            // son değer geçerli olur.
            result[tag.Key] = tag.Value;
        }

        return result;
    }
}