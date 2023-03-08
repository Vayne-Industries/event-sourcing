using System.Reflection;
using JetBrains.Annotations;

namespace VI.BE.EventSourcing.Concepts;

[PublicAPI]
[Serializable]
public abstract class DomainEvent
{
	public Guid EventId { get; protected set; } = Guid.NewGuid();

	public DateTime EventOccured { get; protected set; } = DateTime.UtcNow;
}

[PublicAPI]
public abstract class AggregateRoot
{
	public Guid AggregateId { get; protected set; }

	public Queue<DomainEvent> UncommittedEvents = new();

	public void BuildFromEvents(params DomainEvent[] events)
	{
		foreach (DomainEvent domainEvent in events)
		{
			this.InvokeApplyEvent(domainEvent);
		}
	}

	private void InvokeApplyEvent(DomainEvent domainEvent)
	{
		IEnumerable<MethodInfo> eventApplyMethods = this.GetType().GetMethods().Where(e => e.Name == "Apply").ToList();

		eventApplyMethods.FirstOrDefault(
			e => e.GetParameters().Count(ee => ee.ParameterType.FullName == domainEvent.GetType().FullName) == 1
		)?.Invoke(this, new object?[] { domainEvent });
	}

	public void Update(DomainEvent e)
	{
		this.InvokeApplyEvent(e);
		this.UncommittedEvents.Enqueue(e);
	}

	public IEnumerable<DomainEvent> RetrieveUncommittedEvents()
	{
		List<DomainEvent> list = this.UncommittedEvents.ToList();
		this.UncommittedEvents.Clear();
		return list;
	}
}

[PublicAPI]
[Serializable]
public class ArticleCreatedEvent : DomainEvent
{
	public ArticleCreatedEvent(Guid aggregateId, string articleName)
	{
		this.AggregateId = aggregateId;
		this.ArticleName = articleName;
	}

	public Guid AggregateId { get; private init; }

	public string ArticleName { get; private init; }
}

[PublicAPI]
[Serializable]
public class ArticlePriceChangeEvent : DomainEvent
{
	public ArticlePriceChangeEvent(int articlePrice) => this.ArticlePrice = articlePrice;

	public int ArticlePrice { get; private init; }
}

[PublicAPI]
[Serializable]
public class SnapshotEvent<T> : DomainEvent where T : AggregateRoot, new()
{
	public SnapshotEvent(T aggregateRoot) => this.AggregateRoot = aggregateRoot;

	public T AggregateRoot { get; private init; }
}

[PublicAPI]
public class Article : AggregateRoot
{
	public string ArticleName { get; private set; } = default!;

	public int ArticlePrice { get; private set; }

	public void Apply(ArticleCreatedEvent e)
	{
		this.AggregateId = e.AggregateId;
		this.ArticleName = e.ArticleName;
	}

	public void Apply(ArticlePriceChangeEvent e)
	{
		this.ArticlePrice = e.ArticlePrice;
	}

	public void Apply(SnapshotEvent<Article> e)
	{
		this.ArticleName = e.AggregateRoot.ArticleName;
		this.ArticlePrice = e.AggregateRoot.ArticlePrice;
	}
}
