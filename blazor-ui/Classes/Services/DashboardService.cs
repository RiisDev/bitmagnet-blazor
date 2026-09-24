using Bitmagnet.WebUI.GraphQL;

namespace Bitmagnet.WebUI.Classes.Services
{
	public sealed class DashboardQueryData
	{
		public string? Version { get; set; }
		public WorkersQuery? Workers { get; set; }
		public HealthQuery? Health { get; set; }
		public QueueQuery? Queue { get; set; }
		public TorrentContentQuery? TorrentContent { get; set; }
	}

	public sealed class DashboardService(GraphQlClient client)
	{
		// One round-trip, and every sub-query uses limit:0 -- only aggregations/counts are
		// fetched, never row data, so this stays cheap no matter how large the library gets.
		public async Task<DashboardQueryData?> GetAsync(CancellationToken cancellationToken = default)
		{
			QueueJobsQueryInput jobsInput = new()
			{
				Limit = 0,
				TotalCount = true,
				Facets = new QueueJobsFacetsInput
				{
					Status = new QueueJobStatusFacetInput { Aggregate = true },
					Queue = new QueueJobQueueFacetInput { Aggregate = true }
				}
			};

			TorrentContentSearchQueryInput contentInput = new()
			{
				QueryString = "",
				Limit = 0,
				TotalCount = true,
				Facets = new TorrentContentFacetsInput
				{
					ContentType = new ContentTypeFacetInput { Aggregate = true }
				}
			};

			QueryQueryBuilder queryBuilder = new QueryQueryBuilder()
				.WithVersion()
				.WithWorkers(
					new WorkersQueryQueryBuilder()
						.WithListAll(
							new WorkersListAllQueryResultQueryBuilder()
								.WithWorkers(new WorkerQueryBuilder().WithKey().WithStarted())))
				.WithHealth(
					new HealthQueryQueryBuilder()
						.WithStatus()
						.WithChecks(new HealthCheckQueryBuilder().WithKey().WithStatus().WithError()))
				.WithQueue(
					new QueueQueryQueryBuilder()
						.WithJobs(
							new QueueJobsQueryResultQueryBuilder()
								.WithTotalCount()
								.WithAggregations(
									new QueueJobsAggregationsQueryBuilder()
										.WithStatus(new QueueJobStatusAggQueryBuilder().WithValue().WithLabel().WithCount())
										.WithQueue(new QueueJobQueueAggQueryBuilder().WithValue().WithLabel().WithCount())),
							jobsInput))
				.WithTorrentContent(
					new TorrentContentQueryQueryBuilder()
						.WithSearch(
							new TorrentContentSearchResultQueryBuilder()
								.WithTotalCount()
								.WithAggregations(
									new TorrentContentAggregationsQueryBuilder()
										.WithContentType(new ContentTypeAggQueryBuilder().WithValue().WithLabel().WithCount())),
							contentInput));

			string query = queryBuilder.Build(Formatting.None);

			return await client.SendAsync<DashboardQueryData>(query, cancellationToken);
		}
	}
}
