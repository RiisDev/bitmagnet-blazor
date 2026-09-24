using Bitmagnet.WebUI.GraphQL;

namespace Bitmagnet.WebUI.Classes.Services
{
	public sealed class BitmagnetQueryData
	{
		public TorrentContentQuery? TorrentContent { get; set; }
		public TorrentQuery? Torrent { get; set; }
	}

	public sealed class BitmagnetService(GraphQlClient client)
	{
		// Grid column property name -> GraphQL sort field (see TorrentContentOrderByField).
		private static readonly Dictionary<string, TorrentContentOrderByField> SortFields = new()
		{
			[nameof(Models.TorrentModel.Title)] = TorrentContentOrderByField.Name,
			[nameof(Models.TorrentModel.SizeBytes)] = TorrentContentOrderByField.Size,
			[nameof(Models.TorrentModel.Published)] = TorrentContentOrderByField.PublishedAt,
			[nameof(Models.TorrentModel.Seeders)] = TorrentContentOrderByField.Seeders
		};

		// contentType is the UI's slug (see ContentTypeSlugs) -- null/unrecognized means "all".
		// fileTypes/sources/languages are the raw GraphQL facet values (FileType/Language enum
		// member names, source keys); null or empty means "no filter, but still aggregate".
		// sortBy is a TorrentModel property name (see SortFields); null keeps the server default (relevance).
		// The backend holds ~6M torrents, so this always page-limits server-side rather than
		// ever pulling the full result set; totalCount lets the grid show/paginate the real total.
		public async Task<TorrentContentSearchResult?> SearchAsync(
			string queryString,
			string? contentType = null,
			IReadOnlyCollection<string>? fileTypes = null,
			IReadOnlyCollection<string>? sources = null,
			IReadOnlyCollection<string>? languages = null,
			string? sortBy = null,
			bool sortDescending = false,
			int limit = 25,
			int page = 1,
			CancellationToken cancellationToken = default)
		{
			ContentType? parsedContentType = ContentTypeSlugs.FromSlug(contentType);

			ContentTypeFacetInput contentTypeFacet = new() { Aggregate = true };
			if (parsedContentType is { } ct)
			{
				contentTypeFacet.Filter = new List<ContentType?> { ct };
			}

			TorrentFileTypeFacetInput fileTypeFacet = new() { Aggregate = true, Logic = FacetLogic.Or };
			if (fileTypes is { Count: > 0 })
			{
				fileTypeFacet.Filter = fileTypes.Select(f => Enum.Parse<FileType>(f, ignoreCase: true)).ToList();
			}

			TorrentSourceFacetInput sourceFacet = new() { Aggregate = true };
			if (sources is { Count: > 0 })
			{
				sourceFacet.Filter = sources.ToList();
			}

			LanguageFacetInput languageFacet = new() { Aggregate = true };
			if (languages is { Count: > 0 })
			{
				languageFacet.Filter = languages.Select(l => Enum.Parse<Language>(l, ignoreCase: true)).ToList();
			}

			TorrentContentSearchQueryInput input = new()
			{
				QueryString = queryString,
				Limit = limit,
				Page = page,
				TotalCount = true,
				Facets = new TorrentContentFacetsInput
				{
					ContentType = contentTypeFacet,
					TorrentFileType = fileTypeFacet,
					TorrentSource = sourceFacet,
					Language = languageFacet
				}
			};

			if (sortBy is not null && SortFields.TryGetValue(sortBy, out TorrentContentOrderByField sortField))
			{
				input.OrderBy = new List<TorrentContentOrderByInput>
				{
					new() { Field = sortField, Descending = sortDescending }
				};
			}

			TorrentContentSearchResultQueryBuilder searchBuilder =
				new TorrentContentSearchResultQueryBuilder()
					.WithTotalCount()
					.WithHasNextPage()
					.WithAggregations(
						new TorrentContentAggregationsQueryBuilder()
							.WithContentType(
								new ContentTypeAggQueryBuilder()
									.WithValue()
									.WithCount())
							.WithTorrentFileType(
								new TorrentFileTypeAggQueryBuilder()
									.WithValue()
									.WithLabel()
									.WithCount())
							.WithTorrentSource(
								new TorrentSourceAggQueryBuilder()
									.WithValue()
									.WithLabel()
									.WithCount())
							.WithLanguage(
								new LanguageAggQueryBuilder()
									.WithValue()
									.WithLabel()
									.WithCount()))
					.WithItems(
						new TorrentContentQueryBuilder()
							.WithId()
							.WithTitle()
							.WithContentType()
							.WithSeeders()
							.WithLeechers()
							.WithPublishedAt()
							.WithLanguages(new LanguageInfoQueryBuilder().WithName())
							.WithTorrent(
								new TorrentQueryBuilder()
									.WithInfoHash()
									.WithMagnetUri()
									.WithSize()
									.WithFileTypes()
									.WithSources(new TorrentSourceInfoQueryBuilder().WithName())));

			TorrentContentQueryQueryBuilder torrentContentBuilder =
				new TorrentContentQueryQueryBuilder()
					.WithSearch(
						searchBuilder,
						input);

			QueryQueryBuilder queryBuilder =
				new QueryQueryBuilder()
					.WithTorrentContent(torrentContentBuilder);

			string query = queryBuilder.Build(Formatting.None);

			BitmagnetQueryData? data =
				await client.SendAsync<BitmagnetQueryData>(
					query,
					cancellationToken);

			return data?.TorrentContent?.Search;
		}

		// The nested torrentContent.search().torrent.files field always resolves to null on
		// this server -- files for a specific torrent only come back through this separate,
		// paginated top-level query, so file listings are fetched lazily per-torrent instead.
		public async Task<TorrentFilesQueryResult?> GetFilesAsync(string infoHash, int limit = 200, CancellationToken cancellationToken = default)
		{
			TorrentFilesQueryInput input = new()
			{
				InfoHashes = new List<object> { infoHash },
				Limit = limit
			};

			TorrentQueryQueryBuilder torrentBuilder =
				new TorrentQueryQueryBuilder()
					.WithFiles(
						new TorrentFilesQueryResultQueryBuilder()
							.WithItems(
								new TorrentFileQueryBuilder()
									.WithIndex()
									.WithPath()
									.WithFileType()
									.WithSize()),
						input);

			QueryQueryBuilder queryBuilder =
				new QueryQueryBuilder()
					.WithTorrent(torrentBuilder);

			string query = queryBuilder.Build(Formatting.None);

			BitmagnetQueryData? data =
				await client.SendAsync<BitmagnetQueryData>(
					query,
					cancellationToken);

			return data?.Torrent?.Files;
		}

		// For the permalink page: a single torrent looked up by its info hash rather than
		// full-text search. torrentContent.search's infoHashes filter is an exact match, so
		// limit:1 is enough.
		public async Task<TorrentContent?> GetByInfoHashAsync(string infoHash, CancellationToken cancellationToken = default)
		{
			TorrentContentSearchQueryInput input = new()
			{
				InfoHashes = new List<object> { infoHash },
				Limit = 1
			};

			TorrentContentSearchResultQueryBuilder searchBuilder =
				new TorrentContentSearchResultQueryBuilder()
					.WithItems(
						new TorrentContentQueryBuilder()
							.WithId()
							.WithTitle()
							.WithContentType()
							.WithSeeders()
							.WithLeechers()
							.WithPublishedAt()
							.WithLanguages(new LanguageInfoQueryBuilder().WithName())
							.WithTorrent(
								new TorrentQueryBuilder()
									.WithInfoHash()
									.WithMagnetUri()
									.WithSize()
									.WithFileTypes()
									.WithSources(new TorrentSourceInfoQueryBuilder().WithName())));

			TorrentContentQueryQueryBuilder torrentContentBuilder =
				new TorrentContentQueryQueryBuilder()
					.WithSearch(searchBuilder, input);

			QueryQueryBuilder queryBuilder =
				new QueryQueryBuilder()
					.WithTorrentContent(torrentContentBuilder);

			string query = queryBuilder.Build(Formatting.None);

			BitmagnetQueryData? data =
				await client.SendAsync<BitmagnetQueryData>(
					query,
					cancellationToken);

			return data?.TorrentContent?.Search?.Items?.FirstOrDefault();
		}
	}
}