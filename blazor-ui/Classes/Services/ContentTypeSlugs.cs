using Bitmagnet.WebUI.GraphQL;

namespace Bitmagnet.WebUI.Classes.Services
{
	// Single source of truth for GraphQL ContentType enum <-> the URL/filter-friendly
	// slugs used throughout the UI (TorrentFilterState, TorrentModel.ContentType).
	internal static class ContentTypeSlugs
	{
		private static readonly Dictionary<ContentType, string> ToSlugMap = new()
		{
			[ContentType.Movie] = "movie",
			[ContentType.TvShow] = "tv_show",
			[ContentType.Music] = "music",
			[ContentType.Ebook] = "ebook",
			[ContentType.Comic] = "comic",
			[ContentType.Audiobook] = "audiobook",
			[ContentType.Game] = "game",
			[ContentType.Software] = "software",
			[ContentType.Xxx] = "xxx"
		};

		private static readonly Dictionary<string, ContentType> FromSlugMap =
			ToSlugMap.ToDictionary(kv => kv.Value, kv => kv.Key);

		public static string ToSlug(ContentType? value) =>
			value is { } ct && ToSlugMap.TryGetValue(ct, out string? slug) ? slug : "unknown";

		public static ContentType? FromSlug(string? slug) =>
			slug is not null && FromSlugMap.TryGetValue(slug, out ContentType ct) ? ct : null;
	}
}
