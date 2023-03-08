// ReSharper disable InvertIf
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ReplaceAutoPropertyWithComputedProperty

using System.Reflection;
using VI.BE.EventSourcing;
using VI.BE.EventSourcing.Concepts;

#pragma warning disable CS8321

var article = new VI.BE.EventSourcing.Concepts.Article();
article.BuildFromEvents(
	new ArticleCreatedEvent(Guid.NewGuid(), "Blue T-Shirt"),
	new ArticlePriceChangeEvent(10),
	new ArticlePriceChangeEvent(20)
);
Console.WriteLine(article.AggregateId + " " + article.ArticleName + " " + article.ArticlePrice);

article.Update(new ArticlePriceChangeEvent(30));
article.Update(new ArticlePriceChangeEvent(20));
article.Update(new ArticlePriceChangeEvent(5));

// write events to EventStore
// Stream.WriteEvents(article)

// fetch the aggregate object and create snapshots on go
Article aggregateObject = BuildAggregate<Article>(Guid.NewGuid());

// use the aggregateObject in BL
Console.WriteLine($"Article Name: {aggregateObject.ArticleName}");

return;

// fetch the aggregate object and create snapshots on go
static T BuildAggregate<T>(Guid aggregateId) where T : AggregateRoot, new()
{
	const string snapshot = nameof(SnapshotCreatedEvent<T>);
	IList<RawEventStoreEvent> events = ReadEvents<T>(aggregateId).ToList();

	int aggregateRootVersion = GetVersion<T>();

	// contains the SnapshotCreatedEvent of required version
	IList<(SnapshotCreatedEvent<T> @event, long id)> snapshots = events
		.Where(@event => @event.Name == snapshot)
		.Select(@event => (@event: @event.Data.Deserialize<SnapshotCreatedEvent<T>>()!, id: @event.Id))
		.Where(@event => @event.@event.GeneratedFromAggregateVersion == aggregateRootVersion)
		.ToList();

	// get the latest snapshot from the last 6 events
	T latestAggregateFromSnapshot = snapshots.Any() == false ? new T() : snapshots.Last().@event.Data;

	Console.WriteLine($"Aggregate from snapshot retrieved: {latestAggregateFromSnapshot.LatestEventApplied}");

	// get the events after the latest snapshot
	IList<RawEventStoreEvent> eventsToApply = events;

	if (snapshots.Any() == true)
	{
		eventsToApply = eventsToApply.SkipWhile(@event => @event.Id != snapshots.Last().id).Skip(1).ToList();
	}

	// apply remaining events
	latestAggregateFromSnapshot.ApplyEvents(eventsToApply);

	// the last 5 events has no SnapshotEvent, therefor we need to create a new SnapshotEvent
	if (
		events
			.TakeLast(5)
			.Where(@event => @event.Name == snapshot)
			.Select(@event => (@event: @event.Data.Deserialize<SnapshotCreatedEvent<T>>()!, id: @event.Id))
			.Any(@event => @event.@event.GeneratedFromAggregateVersion == aggregateRootVersion) == false
	)
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

static int GetVersion<T>() where T : AggregateRoot, new()
{
	MethodInfo? v =
		typeof(T).GetMethod(
			nameof(AggregateRoot.RequiredSnapshotVersion), BindingFlags.Static | BindingFlags.NonPublic
		) ??
		typeof(T).BaseType?.GetMethod(
			nameof(AggregateRoot.RequiredSnapshotVersion), BindingFlags.Static | BindingFlags.NonPublic
		);
	return (int)(v?.Invoke(null, null) ?? 1);
}

// dummy Domain implementations

internal class Article : AggregateRoot
{
	internal string? ArticleName { get; init; }

	internal new static int RequiredSnapshotVersion() => 2;
}

// dummy Framework implementations

internal abstract class AggregateRoot
{
	internal long LatestEventApplied { get; set; } = -1;

	internal static int RequiredSnapshotVersion() => 1;

	internal void ApplyEvents(IEnumerable<RawEventStoreEvent> enumerableEvents)
	{
		List<RawEventStoreEvent> events = enumerableEvents.ToList();

		// Logic to apply events to the aggregate
		foreach ((long id, string? name, string? data) in events)
		{
			Console.WriteLine($"Applying event {id}:{name} with data: {data}");
		}

		this.LatestEventApplied = events.Last().Id;
	}
}

internal record RawEventStoreEvent(long Id, string Name, string Data);

internal abstract class DomainEvent
{
	public string Name => this.GetType().Name.Contains('`')
		? this.GetType().Name[..this.GetType().Name.IndexOf('`')]
		: this.GetType().Name;

	public Guid AggregateId { get; init; }
}

internal class SnapshotCreatedEvent<T> : DomainEvent where T : AggregateRoot, new()
{
	public int GeneratedFromAggregateVersion { get; init; } = AggregateRoot.RequiredSnapshotVersion();
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
