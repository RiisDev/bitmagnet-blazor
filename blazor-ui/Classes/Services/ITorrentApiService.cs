using Bitmagnet.WebUI.Classes.Models;
using Bitmagnet.WebUI.GraphQL;

namespace Bitmagnet.WebUI.Classes.Services
{
	// One page of results plus the server-computed totals needed to drive the grid's
	// pager and the content-type facet counts, without ever materializing the full result set.
	public sealed class TorrentSearchPage
	{
		public List<TorrentModel> Items { get; init; } = [];
		public int TotalCount { get; init; }
		public List<(string Value, int Count)> ContentTypeCounts { get; init; } = [];
		public TorrentFilterState.FacetAggregations Facets { get; init; } = new([], [], []);
	}

	// Services/ITorrentApiService.cs
	public interface ITorrentApiService
	{
		Task<TorrentSearchPage> SearchAsync(
			string? queryString,
			string? contentType,
			IReadOnlyCollection<string>? fileTypes,
			IReadOnlyCollection<string>? sources,
			IReadOnlyCollection<string>? languages,
			string? sortBy,
			bool sortDescending,
			int page,
			int pageSize,
			CancellationToken ct = default);

		// Fetched separately/lazily, not as part of SearchAsync's rows -- the backend never
		// resolves a torrent's files when nested inside a torrentContent search.
		Task<List<Models.TorrentFile>> GetFilesAsync(TorrentModel torrent, CancellationToken ct = default);

		// For the permalink page.
		Task<TorrentModel?> GetByInfoHashAsync(string infoHash, CancellationToken ct = default);
	}

	// Services/TorrentApiService.cs
	public class TorrentApiService(BitmagnetService bitmagnet) : ITorrentApiService
	{
		public async Task<TorrentSearchPage> SearchAsync(
			string? queryString,
			string? contentType,
			IReadOnlyCollection<string>? fileTypes,
			IReadOnlyCollection<string>? sources,
			IReadOnlyCollection<string>? languages,
			string? sortBy,
			bool sortDescending,
			int page,
			int pageSize,
			CancellationToken ct = default)
		{
			TorrentContentSearchResult? result = await bitmagnet.SearchAsync(
				queryString ?? "", contentType, fileTypes, sources, languages, sortBy, sortDescending, pageSize, page, ct);

			return new TorrentSearchPage
			{
				Items = result?.Items?.Select(MapToModel).ToList() ?? [],
				TotalCount = result?.TotalCount ?? 0,
				ContentTypeCounts = result?.Aggregations?.ContentType?
					.Select(agg => (ContentTypeSlugs.ToSlug(agg.Value), agg.Count ?? 0))
					.ToList() ?? [],
				Facets = new TorrentFilterState.FacetAggregations(
					Sources: result?.Aggregations?.TorrentSource?
						.Select(agg => (agg.Value ?? "", agg.Label ?? agg.Value ?? "", agg.Count ?? 0))
						.ToList() ?? [],
					FileTypes: result?.Aggregations?.TorrentFileType?
						.Select(agg => (agg.Value?.ToString() ?? "", agg.Label ?? agg.Value?.ToString() ?? "", agg.Count ?? 0))
						.ToList() ?? [],
					Languages: result?.Aggregations?.Language?
						.Select(agg => (agg.Value?.ToString() ?? "", agg.Label ?? agg.Value?.ToString() ?? "", agg.Count ?? 0))
						.ToList() ?? [])
			};
		}

		public async Task<List<Models.TorrentFile>> GetFilesAsync(TorrentModel torrent, CancellationToken ct = default)
		{
			TorrentFilesQueryResult? result = await bitmagnet.GetFilesAsync(torrent.InfoHash, cancellationToken: ct);

			List<Models.TorrentFile> files = result?.Items?.Select(f => new Models.TorrentFile
			{
				Index = f.Index ?? 0,
				Path = f.Path ?? "",
				Type = f.FileType?.ToString() ?? "",
				SizeBytes = f.Size ?? 0
			}).ToList() ?? [];

			// Single-file torrents have no rows in the files table at all -- the torrent
			// itself IS the one file, so synthesize that file from the torrent's own name/size
			// rather than showing an empty list (matches the reference webui's behavior).
			if (files.Count == 0 && torrent.SizeBytes > 0)
			{
				files.Add(new Models.TorrentFile
				{
					Index = 0,
					Path = torrent.Title,
					Type = torrent.FileTypes.FirstOrDefault() ?? "",
					SizeBytes = torrent.SizeBytes
				});
			}

			return files;
		}

		public async Task<TorrentModel?> GetByInfoHashAsync(string infoHash, CancellationToken ct = default)
		{
			TorrentContent? content = await bitmagnet.GetByInfoHashAsync(infoHash, ct);
			return content is null ? null : MapToModel(content);
		}

		// TorrentContent.id is a composite string, not a UUID, but TorrentModel.Id (used for
		// grid row keys / routing) needs a Guid -- derive one deterministically from the info hash.
		private static Guid InfoHashToGuid(string? infoHash)
		{
			if (string.IsNullOrEmpty(infoHash) || infoHash.Length < 32)
				return Guid.NewGuid();

			return new Guid(Convert.FromHexString(infoHash[..32]));
		}

		private static TorrentModel MapToModel(TorrentContent content)
		{
			Torrent? torrent = content.Torrent;

			return new TorrentModel
			{
				Id = InfoHashToGuid(torrent?.InfoHash?.ToString() ?? content.InfoHash?.ToString()),
				Title = content.Title ?? torrent?.Name ?? "",
				ContentType = ContentTypeSlugs.ToSlug(content.ContentType),
				SizeBytes = torrent?.Size ?? 0,
				Published = content.PublishedAt ?? torrent?.CreatedAt?.UtcDateTime ?? default,
				Seeders = content.Seeders ?? torrent?.Seeders ?? 0,
				Leechers = content.Leechers ?? torrent?.Leechers ?? 0,
				InfoHash = torrent?.InfoHash?.ToString() ?? content.InfoHash?.ToString() ?? "",
				Source = torrent?.Sources?.FirstOrDefault()?.Name ?? "",
				Language = content.Languages?.FirstOrDefault()?.Name ?? "",
				FileTypes = torrent?.FileTypes?.Select(ft => ft.ToString()).ToList() ?? [],
				MagnetLink = torrent?.MagnetUri ?? ""
			};
		}
	}
}
