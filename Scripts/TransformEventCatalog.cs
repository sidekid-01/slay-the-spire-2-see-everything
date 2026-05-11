using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Advisor.Scripts;

internal static class TransformEventCatalog
{
    // TypeName, EN, ZH, transformCount
    private static readonly (string TypeName, string En, string Zh, int Count)[] KnownTransformEvents =
    {
        ("Symbiote", "Symbiote", "共生体", 1),
        ("AromaOfChaos", "Aroma of Chaos", "混沌之香", 1),
        ("WhisperingHollow", "Whispering Hollow", "低语空洞", 1),
        ("MorphicGrove", "Morphic Grove", "拟态林地", 2),
    };

    public static IEnumerable<IEventPredictor> CreatePredictors()
    {
        foreach (var item in KnownTransformEvents)
        {
            Type? eventType = ResolveEventTypeByName(item.TypeName);
            if (eventType == null)
                continue;

            yield return new GenericTransformPredictor(
                eventType,
                STS2AdvisorI18n.Pick(item.En, item.Zh),
                item.Count);
        }
    }

    private static Type? ResolveEventTypeByName(string typeName)
    {
        return typeof(EventModel).Assembly
            .GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract &&
                typeof(EventModel).IsAssignableFrom(t) &&
                string.Equals(t.Name, typeName, StringComparison.Ordinal));
    }
}
