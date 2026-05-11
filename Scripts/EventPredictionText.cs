using MegaCrit.Sts2.Core.Models;

namespace STS2Advisor.Scripts;

internal static class EventPredictionText
{
    public static string EventDisplayName(EventModel eventModel)
    {
        string localized = LocText.Of(eventModel);
        if (!string.IsNullOrWhiteSpace(localized))
            return localized;

        return eventModel.GetType().Name;
    }

    public static string NoTransformableCards() =>
        STS2AdvisorI18n.Pick("No transformable cards in deck.", "牌组中没有可变形卡牌。");

    public static string NoTransformTargets() =>
        STS2AdvisorI18n.Pick("No valid transformation targets.", "没有可用的变形目标。");
}
