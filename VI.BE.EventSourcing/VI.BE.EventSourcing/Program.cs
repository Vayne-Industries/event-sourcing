// ReSharper disable InvertIf
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ReplaceAutoPropertyWithComputedProperty

using VI.BE.EventSourcing;

#pragma warning disable CS8321

// fetch the aggregate object and create snapshots on go
Article aggregateObject = BuildAggregate<Article>(Guid.NewGuid());

// use the aggregateObject in BL
Console.WriteLine($"Article Name: {aggregateObject.ArticleName}");

return;

// fetch the aggregate object and create snapshots on go
static T BuildAggregate<T>(Guid aggregateId) where T : AggregateRoot, new()
{
	const string snapshot = "SnapshotCreatedEvent";
	IList<RawEventStoreEvent> events = ReadEvents<T>(aggregateId).ToList();

	// get the latest snapshot from the last 6 events
	T latestAggregateFromSnapshot =
		events.Last(@event => @event.Name == snapshot).Data.Deserialize<SnapshotCreatedEvent<T>>()!.Data;

	Console.WriteLine($"Aggregate from snapshot retrieved: {latestAggregateFromSnapshot.LatestEventApplied}");

	// get the events after the latest snapshot
	IList<RawEventStoreEvent> eventsToApply = events.SkipWhile(@event => @event.Name != snapshot).Skip(1).ToList();

	// apply remaining events
	latestAggregateFromSnapshot.ApplyEvents(eventsToApply);

	// the last 5 events has no SnapshotEvent, therefor we need to create a new SnapshotEvent
	if (events.TakeLast(5).Any(@event => @event.Name == snapshot) == false)
	{
		var snapshotEvent = new SnapshotCreatedEvent<T>
		{
			AggregateId = aggregateId,
			Data = latestAggregateFromSnapshot
		};

		WriteEvent<T>(snapshotEvent);
	}

	return latestAggregateFromSnapshot;
}

// dummy Framework implementations

static IEnumerable<RawEventStoreEvent> ReadEvents<T>(Guid aggregateId) =>
	// read last 6 events from the event store
	Stream.ReadLastEvents($"{nameof(T)}-{aggregateId}", 6);

static void WriteEvent<T>(DomainEvent @event)
{
	Console.WriteLine($"Writing {@event.Name} {@event.AggregateId} to {typeof(T).Name}");
	Stream.WriteEvent(typeof(T).Name, @event.Name, @event.Serialize());
}

// dummy Domain implementations

internal class Article : AggregateRoot
{
	internal string? ArticleName { get; init; }
}

// dummy Framework implementations

internal class AggregateRoot
{
	internal ulong LatestEventApplied { get; set; }

	internal void ApplyEvents(IEnumerable<RawEventStoreEvent> enumerableEvents)
	{
		List<RawEventStoreEvent> events = enumerableEvents.ToList();

		// Logic to apply events to the aggregate
		foreach ((ulong id, string? name, string? data) in events)
		{
			Console.WriteLine($"Applying event {id}:{name} with data: {data}");
		}

		this.LatestEventApplied = events.Last().Id;
	}
}

internal record RawEventStoreEvent(ulong Id, string Name, string Data);

internal abstract class DomainEvent
{
	public string Name => this.GetType().Name.Contains('`')
		? this.GetType().Name[..this.GetType().Name.IndexOf('`')]
		: this.GetType().Name;

	public Guid AggregateId { get; init; }
}

internal class SnapshotCreatedEvent<T> : DomainEvent where T : AggregateRoot, new()
{
	public T Data { get; init; } = new();
}

internal static class Stream
{
	internal static IEnumerable<RawEventStoreEvent> ReadLastEvents(string streamName, int count) =>
		new RawEventStoreEvent[]
		{
			new(1, "ArticleCreatedEvent", "{article create data}"),
			new(2, "ArticleChangedEvent", "{article change data}"),
			new(3, "ArticlePriceChangedEvent", "{article price change data}"),
			new(4, "SnapshotCreatedEvent", "{snapshot data}"),
			new(5, "ArticleNameChangedEvent", "{article name change data}"),
			new(6, "ArticleChangedEvent", "{article change data}"),
			new(7, "ArticleChangedEvent", "{article change data}"),
			new(8, "ArticleChangedEvent", "{article change data}"),
			new(9, "ArticleChangedEvent", "{article change data}")
		};

	internal static void WriteEvent(string streamName, string eventName, object @event)
	{
	}
}
