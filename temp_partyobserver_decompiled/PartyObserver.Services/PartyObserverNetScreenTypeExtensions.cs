using MegaCrit.Sts2.Core.Entities.Multiplayer;

namespace PartyObserver.Services;

internal static class PartyObserverNetScreenTypeExtensions
{
	public static string GetDisplayName(this NetScreenType screenType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return PartyObserverText.GetNetScreenDisplayName(screenType);
	}
}
