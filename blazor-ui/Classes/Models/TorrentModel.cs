namespace Bitmagnet.WebUI.Classes.Models
{
	public class TorrentModel
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = "";
		public string ContentType { get; set; } = "";
		public long SizeBytes { get; set; }
		public DateTime Published { get; set; }
		public int Seeders { get; set; }
		public int Leechers { get; set; }
		public string InfoHash { get; set; } = "";
		public string Source { get; set; } = "";
		public string Language { get; set; } = "";
		public List<string> FileTypes { get; set; } = new();
		public string MagnetLink { get; set; } = "";
	}

	public class TorrentFile
	{
		public int Index { get; set; }
		public string Path { get; set; } = "";
		public string Type { get; set; } = "";
		public long SizeBytes { get; set; }
	}
}
