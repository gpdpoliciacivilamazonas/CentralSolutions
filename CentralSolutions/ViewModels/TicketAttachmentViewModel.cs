namespace CentralSolutions.ViewModels;

public class TicketAttachmentViewModel
{
	public Guid Id { get; set; }

	public string FileName { get; set; } = string.Empty;

	public string ContentType { get; set; } = string.Empty;

	public long FileSize { get; set; }

	public DateTime CreatedAt { get; set; }

	public string FormattedSize
	{
		get
		{
			if (FileSize < 1024)
				return $"{FileSize} B";
			if (FileSize < 1024 * 1024)
				return $"{FileSize / 1024.0:F1} KB";
			return $"{FileSize / (1024.0 * 1024.0):F1} MB";
		}
	}

	public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

	public bool IsVideo => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

	public bool IsPdf => ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
}
