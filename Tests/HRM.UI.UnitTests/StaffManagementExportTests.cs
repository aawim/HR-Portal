using System.Text;
using HRM.Services;
using Xunit;

namespace HRM.UI.UnitTests;

public sealed class StaffManagementExportTests
{
    [Fact]
    public void ToCsv_GeneratesValidCsv_WithHeadersAndEscapedData()
    {
        var rows = new List<StaffExportRow>
        {
            new()
            {
                IndividualId = 101,
                Name = "Ahmed Mohamed",
                EmployeeNumber = "EMP-001",
                Department = "Information Technology",
                JobType = "Software Engineer",
                Status = "Active",
                Groups = "Dev Team; IT Committee"
            },
            new()
            {
                IndividualId = 102,
                Name = "Aishath, Ali",
                EmployeeNumber = null,
                Department = "Human Resources, Planning",
                JobType = "HR Officer",
                Status = "Approved",
                Groups = ""
            }
        };

        var bytes = StaffManagementExport.ToCsv("Ministry of Test", "Status: Active", rows);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Ministry of Test", text);
        Assert.Contains("Staff Name", text);
        Assert.Contains("EMP-001", text);
        Assert.Contains("Ahmed Mohamed", text);
        Assert.Contains("Information Technology", text);
        Assert.Contains("Dev Team; IT Committee", text);
        // Escaped values with commas
        Assert.Contains("\"Aishath, Ali\"", text);
        Assert.Contains("\"Human Resources, Planning\"", text);
    }

    [Fact]
    public void ToPdf_GeneratesNonEmptyPdfBytes()
    {
        var rows = new List<StaffExportRow>
        {
            new()
            {
                IndividualId = 201,
                Name = "Ibrahim Rasheed",
                EmployeeNumber = "EMP-042",
                Department = "Finance",
                JobType = "Accountant",
                Status = "Active",
                Groups = "Audit Committee"
            }
        };

        // Locate font
        var fontPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "HRM.UI", "wwwroot", "fonts", "Faruma.ttf"));
        if (!File.Exists(fontPath))
        {
            fontPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HRM.UI", "wwwroot", "fonts", "Faruma.ttf"));
        }
        if (!File.Exists(fontPath))
        {
            fontPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "HRM.UI", "wwwroot", "fonts", "Faruma.ttf"));
        }

        Assert.True(File.Exists(fontPath), $"Faruma.ttf font must exist at {fontPath}");

        var bytes = StaffManagementExport.ToPdf("Test Organisation", "Status: All", rows, fontPath);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 500);

        // Verify PDF header %PDF-
        var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }
}
