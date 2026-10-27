namespace HRM.Helpers;

public sealed record WorkHourCoverageResult(bool HasOfficialSchedule, int OfficialMinutes, int DutyMinutes,
    int DutyWithinOfficialMinutes, int PotentialOvertimeMinutes);

public static class WorkHourCoverage
{
    public static List<(DateTime Start, DateTime End)> Merge(IEnumerable<(DateTime Start, DateTime End)> periods)
    {
        var result = new List<(DateTime Start, DateTime End)>();
        foreach (var item in periods.Where(x => x.End > x.Start).OrderBy(x => x.Start))
        {
            if (result.Count == 0 || item.Start > result[^1].End) result.Add(item);
            else if (item.End > result[^1].End) result[^1] = (result[^1].Start, item.End);
        }
        return result;
    }

    public static int Minutes(IEnumerable<(DateTime Start, DateTime End)> periods) =>
        (int)Merge(periods).Sum(x => (x.End - x.Start).TotalMinutes);

    public static WorkHourCoverageResult Calculate(IEnumerable<(DateTime Start, DateTime End)> official,
        IEnumerable<(DateTime Start, DateTime End)> duty)
    {
        var offices = Merge(official);
        var duties = Merge(duty);
        var covered = from d in duties from o in offices
                      let start = d.Start > o.Start ? d.Start : o.Start
                      let end = d.End < o.End ? d.End : o.End
                      where end > start select (start, end);
        var dutyMinutes = Minutes(duties);
        var inside = Minutes(covered);
        // Without an official baseline, HR must configure a schedule before OT can be determined.
        return new(offices.Count > 0, Minutes(offices), dutyMinutes, inside, offices.Count == 0 ? 0 : dutyMinutes - inside);
    }
}
