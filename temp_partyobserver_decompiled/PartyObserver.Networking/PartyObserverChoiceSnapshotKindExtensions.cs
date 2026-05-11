using PartyObserver.Services;

namespace PartyObserver.Networking;

internal static class PartyObserverChoiceSnapshotKindExtensions
{
	public static string GetDisplayName(this PartyObserverChoiceSnapshotKind kind)
	{
		return PartyObserverText.GetSnapshotKindDisplayName(kind);
	}
}
