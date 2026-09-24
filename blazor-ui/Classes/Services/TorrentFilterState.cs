using MudBlazor;

namespace Bitmagnet.WebUI.Classes.Services
{
	// TorrentFilterState.cs
	public class TorrentFilterState
	{
		public event Action? OnChange;

		// Separate from OnChange: MainLayout's sidebar (where these counts render) is a
		// different component than the page calling UpdateCounts, so it needs its own signal
		// to repaint. Reusing OnChange here would make Home reload itself after every reload.
		public event Action? CountsChanged;

		public string? SelectedContentType { get; set; } = "all";
		public string LanguageFilterText { get; set; } = string.Empty;

		// Values match the GraphQL ContentType enum's EnumMember slugs (see GraphQL.cs),
		// so TorrentModel.ContentType can be compared directly without a translation layer.
		public List<ContentTypeOption> ContentTypes { get; } =
		[
			new("all", "All", Icons.Material.Filled.Emergency),
			new("movie", "Movies", Icons.Material.Outlined.Movie),
			new("tv_show", "TV Shows", Icons.Material.Outlined.LiveTv),
			new("music", "Music", Icons.Material.Outlined.MusicNote),
			new("ebook", "E-Books", Icons.Material.Outlined.AutoStories),
			new("comic", "Comics", Icons.Material.Outlined.Book),
			new("audiobook", "Audiobooks", Icons.Material.Outlined.Mic),
			new("game", "Games", Icons.Material.Outlined.SportsEsports),
			new("software", "Software", Icons.Material.Outlined.DesktopWindows),
			new("xxx", "XXX", Icons.Material.Outlined._18UpRating),
			new("unknown", "Unknown", Icons.Material.Outlined.QuestionMark)
		];

		// Sources/FileTypes/Languages are no longer a fixed guess at what exists -- they're
		// populated (and re-counted) from the GraphQL facet aggregations on every search, which
		// cover the whole ~6M-row library, not just the current page. See UpdateFacetOptions.
		public List<FacetOption> Sources { get; } = [];
		public List<FacetOption> FileTypes { get; } = [];
		public List<FacetOption> Languages { get; } = [];

		public IEnumerable<FacetOption> FilteredLanguages =>
			string.IsNullOrWhiteSpace(LanguageFilterText)
				? Languages
				: Languages.Where(l => l.Label.Contains(LanguageFilterText, StringComparison.OrdinalIgnoreCase));

		public void SelectContentType(string value)
		{
			SelectedContentType = value;
			NotifyChanged();
		}

		// Called after every grid page load. All counts here come from GraphQL facet
		// aggregations (real counts across the whole matching set, not just the current page).
		// Raises CountsChanged (not OnChange): this is a result of a reload, not a request
		// for one -- firing OnChange here would make Home reload itself in a loop.
		public void UpdateCounts(IEnumerable<(string Value, int Count)> contentTypeCounts, FacetAggregations facets)
		{
			Dictionary<string, int> byValue = contentTypeCounts.ToDictionary(c => c.Value, c => c.Count);
			foreach (var ct in ContentTypes)
				ct.Count = ct.Value == "all" ? byValue.Values.Sum() : byValue.GetValueOrDefault(ct.Value);

			MergeFacetOptions(Sources, facets.Sources);
			MergeFacetOptions(FileTypes, facets.FileTypes);
			MergeFacetOptions(Languages, facets.Languages);

			CountsChanged?.Invoke();
		}

		// Adds newly-seen values and refreshes counts/labels for known ones. Existing entries
		// are never removed -- a value that drops out of the current search's aggregation
		// (e.g. filtered away) would otherwise vanish from the checkbox list mid-interaction,
		// including ones the user just unchecked.
		private static void MergeFacetOptions(List<FacetOption> existing, IEnumerable<(string Value, string Label, int Count)> latest)
		{
			Dictionary<string, FacetOption> byValue = existing.ToDictionary(o => o.Value);
			foreach (var (value, label, count) in latest)
			{
				if (byValue.TryGetValue(value, out FacetOption? option))
				{
					option.Label = label;
					option.Count = count;
				}
				else
				{
					existing.Add(new FacetOption(value, label) { Count = count });
				}
			}
		}

		public void NotifyChanged() => OnChange?.Invoke();

		public sealed class ContentTypeOption
		{
			public ContentTypeOption(string value, string label, string icon)
			{
				Value = value; Label = label; Icon = icon;
			}
			public string Value { get; }
			public string Label { get; }
			public string Icon { get; }
			public int Count { get; set; }
		}

		public sealed class FacetOption
		{
			public FacetOption(string value, string label) { Value = value; Label = label; }
			public string Value { get; }
			public string Label { get; set; }
			public bool Checked { get; set; } = true;
			public int Count { get; set; }
		}

		public sealed record FacetAggregations(
			List<(string Value, string Label, int Count)> Sources,
			List<(string Value, string Label, int Count)> FileTypes,
			List<(string Value, string Label, int Count)> Languages);
	}
}
