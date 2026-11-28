namespace HRM.Application.Contracts.Recruitment;

/// <summary>Exact scoring metadata transcribed from the supplied CSC A2 source workbooks. Values are evidence, not editable policy defaults.</summary>
public static class CscA2TemplateDefinitions
{
    public static readonly IReadOnlyDictionary<string, string> ByTemplateCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["A2-CS14-CS16"] = "{\"total\":100,\"passMark\":65,\"components\":[[\"basicQualification\",35],[\"higherQualification\",5],[\"additionalExperience\",5],[\"interview\",15],[\"presentation\",10],[\"assessment\",30]]}",
        ["A2-CS10-CS14"] = "{\"total\":100,\"passMark\":60,\"components\":[[\"basicQualification\",30],[\"higherQualification\",5],[\"additionalExperience\",5],[\"presentation\",10],[\"interview\",20],[\"assessment\",30]]}",
        ["A2-CS2-CS8"] = "{\"total\":100,\"passMark\":55,\"components\":[[\"basicQualification\",25],[\"higherQualification\",5],[\"additionalExperience\",5],[\"presentation\",20],[\"interview\",45]]}",
        ["A2-GS3-GS4"] = "{\"total\":100,\"passMark\":60,\"components\":[[\"basicQualification\",30],[\"higherQualification\",5],[\"additionalExperience\",5],[\"assessment\",25],[\"interview\",35]]}",
        ["A2-GS4-TECHNICAL"] = "{\"total\":100,\"passMark\":60,\"components\":[[\"basicQualification\",30],[\"coreDiscipline\",5],[\"higherQualification\",5],[\"additionalExperience\",5],[\"assessment\",20],[\"interview\",35]]}",
        ["A2-MS1-MS2"] = "{\"total\":100,\"passMark\":60,\"components\":[[\"basicQualification\",30],[\"coreDiscipline\",5],[\"higherQualification\",5],[\"additionalExperience\",5],[\"assessment\",20],[\"interview\",35]]}",
        ["A2-SS1-SS3"] = "{\"total\":100,\"passMark\":70,\"components\":[[\"basicQualification\",50],[\"higherQualification\",5],[\"additionalExperience\",5],[\"interview\",40]]}",
        ["A2-MS3-EX5"] = "{\"total\":100,\"passMark\":60,\"components\":[[\"basicQualification\",30],[\"coreDiscipline\",5],[\"higherQualification\",5],[\"additionalExperience\",5],[\"assessment\",15],[\"presentation\",10],[\"interview\",30]]}"
    };
}
