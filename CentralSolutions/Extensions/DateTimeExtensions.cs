namespace CentralSolutions;

public static class DateTimeExtensions
{
	private static readonly TimeZoneInfo ManausTimeZone = GetManausTimeZone();

	private static TimeZoneInfo GetManausTimeZone()
	{
		try
		{
			var id = OperatingSystem.IsWindows() ? "Central Brazilian Standard Time" : "America/Manaus";
			return TimeZoneInfo.FindSystemTimeZoneById(id);
		}
		catch
		{
			// Fallback robusto caso o container Linux não tenha o pacote tzdata instalado
			return TimeZoneInfo.CreateCustomTimeZone("America/Manaus", TimeSpan.FromHours(-4), "AMT", "AMT");
		}
	}

	public static DateTime ToManausTime(this DateTime utcDateTime)
	{
		var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
		return TimeZoneInfo.ConvertTimeFromUtc(utc, ManausTimeZone);
	}

	public static DateTime? ToManausTime(this DateTime? utcDateTime)
	{
		return utcDateTime.HasValue ? utcDateTime.Value.ToManausTime() : null;
	}

	public static string ToDisplayString(this DateTime dt, string format = "dd/MM/yyyy HH:mm")
	{
		return dt.ToManausTime().ToString(format);
	}

	public static string ToDisplayString(this DateTime? dt, string format = "dd/MM/yyyy HH:mm")
	{
		return dt.HasValue ? dt.Value.ToDisplayString(format) : "-";
	}
}
