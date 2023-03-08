namespace VI.BE.EventSourcing;

internal interface IGuid
{
	public Guid Id { get; }
}

internal interface IEvent : IGuid
{
	public string Name { get; }
}

internal interface IReadModel<out T> where T : new()
{
	public ulong LatestEventIdApplied { get; }
	public T Model { get; }
}

public record ReadModel<T>(ulong LatestEventIdApplied, T Model) : IReadModel<T> where T : new()
{
	public ReadModel() : this(default, default!)
	{
	}

	public static ReadModel<T>? FromJson(string json) => json.Deserialize<ReadModel<T>>();
}

