// ReSharper disable UnusedParameter.Global
#pragma warning disable CS8321
namespace VI.BE.EventSourcing;

internal static class StringExtensions
{
	internal static T? Deserialize<T>(this string json) where T : class, new()
	{
		return new T() switch
		{
			SnapshotCreatedEvent<Article> => new SnapshotCreatedEvent<Article>
			{
				GeneratedFromAggregateVersion = 1,
				Data = new Article
				{
					LatestEventApplied = 3,
					ArticleName = "Blue T-Shirt"
				},
				AggregateId = new Guid()
			} as T,
			_ => new T()
		};
	}
}
