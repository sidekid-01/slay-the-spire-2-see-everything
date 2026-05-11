using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace PartyObserver.Networking;

internal sealed class PartyObserverChoiceSnapshot : IPacketSerializable
{
	private const int OptionCountBits = 6;

	private List<PartyObserverChoiceOption> _options = new List<PartyObserverChoiceOption>();

	public PartyObserverChoiceSnapshotKind Kind { get; set; }

	public string ScreenLabel { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public IReadOnlyList<PartyObserverChoiceOption> Options => _options;

	public PartyObserverChoiceSnapshot Clone()
	{
		return new PartyObserverChoiceSnapshot
		{
			Kind = Kind,
			ScreenLabel = ScreenLabel,
			Title = Title,
			Description = Description,
			_options = _options.Select((PartyObserverChoiceOption option) => option.Clone()).ToList()
		};
	}

	public void AddOption(PartyObserverChoiceOption option)
	{
		_options.Add(option);
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteEnum<PartyObserverChoiceSnapshotKind>(Kind);
		writer.WriteString(ScreenLabel);
		writer.WriteString(Title);
		writer.WriteString(Description);
		writer.WriteList<PartyObserverChoiceOption>((IReadOnlyList<PartyObserverChoiceOption>)_options, 6);
	}

	public void Deserialize(PacketReader reader)
	{
		Kind = reader.ReadEnum<PartyObserverChoiceSnapshotKind>();
		ScreenLabel = reader.ReadString();
		Title = reader.ReadString();
		Description = reader.ReadString();
		_options = reader.ReadList<PartyObserverChoiceOption>(6);
	}
}
