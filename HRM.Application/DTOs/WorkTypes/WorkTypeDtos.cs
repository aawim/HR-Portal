namespace HRM.DTOs.WorkTypes;
public sealed record WorkTypeStaffOption(int JobId, string Name, string Department);

public sealed class WorkTypeDto
{
    public int WorkTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsValid { get; set; }

    public int AssignmentCount { get; set; }
}

public sealed class CreateWorkTypeDto
{
    public string Name { get; set; } = string.Empty;

    public bool IsValid { get; set; } = true;
}

public sealed class UpdateWorkTypeDto
{
    public int WorkTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsValid { get; set; } = true;
}

public sealed class AssignedWorkTypeDto
{
    public int AssignedWorkTypeId { get; set; }

    public int WorkTypeId { get; set; }

    public string WorkTypeName { get; set; } = string.Empty;

    public int JobId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int OrganisationId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }

    public bool IsValid { get; set; }

    public int AssignedByUserId { get; set; }

    public DateOnly CreatedDate { get; set; }
}

public sealed class CreateAssignedWorkTypeDto
{
    public bool IsValid { get; set; } = true;
    public int WorkTypeId { get; set; }

    public int JobId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }
}

public sealed class UpdateAssignedWorkTypeDto
{
    public int AssignedWorkTypeId { get; set; }

    public int WorkTypeId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }

    public bool IsValid { get; set; } = true;
}

public sealed record WorkTypeGeofenceDto(
    int AssignedWorkTypeId,
    string WorkTypeName,
    double Latitude,
    double Longitude,
    int RadiusMeters,
    string Label);

public static class WorkTypeGeofenceHelper
{
    public const double DefaultLatitude = 4.1755;
    public const double DefaultLongitude = 73.5093;
    public const int DefaultRadiusMeters = 150;
    public const int MinRadiusMeters = 25;
    public const int MaxRadiusMeters = 5000;

    public static bool IsGeoWorkTypeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim();
        return n.Contains("geo", StringComparison.OrdinalIgnoreCase)
            || n.Contains("gps", StringComparison.OrdinalIgnoreCase);
    }

    public static string Format(double latitude, double longitude, int radiusMeters, string? label = null)
    {
        var clampedRadius = Math.Clamp(radiusMeters, MinRadiusMeters, MaxRadiusMeters);
        var coords = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "geo:{0:F6},{1:F6},{2}",
            latitude,
            longitude,
            clampedRadius);
        var cleanLabel = (label ?? string.Empty).Replace("|", " ").Trim();
        if (string.IsNullOrEmpty(cleanLabel))
            return coords;

        var maxLabelLen = Math.Max(0, 250 - coords.Length - 1);
        if (cleanLabel.Length > maxLabelLen)
            cleanLabel = cleanLabel[..maxLabelLen].Trim();

        return string.IsNullOrEmpty(cleanLabel) ? coords : $"{coords}|{cleanLabel}";
    }

    public static bool TryParse(string? raw, out double latitude, out double longitude, out int radiusMeters, out string label)
    {
        latitude = 0;
        longitude = 0;
        radiusMeters = DefaultRadiusMeters;
        label = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("geo:", StringComparison.OrdinalIgnoreCase))
            return false;

        var body = trimmed[4..];
        var pipeIdx = body.IndexOf('|');
        var coordPart = pipeIdx >= 0 ? body[..pipeIdx] : body;
        label = pipeIdx >= 0 ? body[(pipeIdx + 1)..].Trim() : string.Empty;

        var parts = coordPart.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;

        if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out latitude)
            || !double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out longitude))
            return false;

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return false;

        if (parts.Length >= 3 && int.TryParse(parts[2], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsedRadius))
            radiusMeters = Math.Clamp(parsedRadius, MinRadiusMeters, MaxRadiusMeters);

        return true;
    }

    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6_371_000.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}
