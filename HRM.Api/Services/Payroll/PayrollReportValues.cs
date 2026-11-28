using System;
using System.Globalization;
using System.Linq;
using HRM.Models.Payroll;

namespace HRM.Services.Payroll;

public static class PayrollReportValues
{
    public static string Csv(params object?[] fields) => string.Join(",", fields.Select(value =>
    {
        if (value is decimal amount) return amount.ToString("F2", CultureInfo.InvariantCulture);
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        // Spreadsheet-facing reports must not execute employee-supplied text as formulas.
        if (text.TrimStart().StartsWith('=') || text.TrimStart().StartsWith('+') ||
            text.TrimStart().StartsWith('-') || text.TrimStart().StartsWith('@') ||
            text.StartsWith('\t') || text.StartsWith('\r') || text.StartsWith('\n')) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }));

    public static bool IsEmployeeDeduction(SalaryComponentType type) =>
        type is >= SalaryComponentType.EmployeePension and <= SalaryComponentType.OtherDeduction ||
        type is SalaryComponentType.AttendanceLatenessDeduction or SalaryComponentType.AttendanceAbsenceDeduction;

    public static decimal TaxableBase(PayrollEmployee employee)
    {
        var tax = employee.Lines.FirstOrDefault(l => l.ComponentCode == "TAX_WHT");
        if (tax?.SourceReference?.Contains("; Non-cash benefits:", StringComparison.Ordinal) == true) return tax.Units;
        return Math.Max(0, employee.Lines.Where(l => (int)l.ComponentType <= 8 && l.IsTaxable).Sum(l => l.CalculatedAmount)
            - employee.Lines.Where(l => l.ComponentType is SalaryComponentType.UnpaidLeaveDeduction or
                SalaryComponentType.AttendanceAbsenceDeduction or SalaryComponentType.AttendanceLatenessDeduction or
                SalaryComponentType.EmployeePension).Sum(l => l.CalculatedAmount));
    }

    public static decimal PensionableWage(PayrollEmployee employee)
    {
        var contribution = employee.Lines.FirstOrDefault(l => l.ComponentCode == "PEN_EMP" || l.ComponentCode == "PEN_EMPR");
        if (contribution == null) return 0;
        // Source amounts preserve the actual base even where contribution rounding loses precision.
        var employeeContribution = employee.Lines.FirstOrDefault(l => l.ComponentCode == "PEN_EMP");
        const string prefix = "Pension Base: ";
        if (employeeContribution?.SourceReference?.StartsWith(prefix, StringComparison.Ordinal) == true &&
            decimal.TryParse(employeeContribution.SourceReference[prefix.Length..], NumberStyles.Number,
                CultureInfo.InvariantCulture, out var basis)) return basis;
        if (contribution.Units != 1) return contribution.Units;
        return Math.Max(0, employee.Lines.Where(l => (int)l.ComponentType <= 8 && l.IsPensionable).Sum(l => l.CalculatedAmount)
            - employee.Lines.Where(l => l.ComponentType == SalaryComponentType.UnpaidLeaveDeduction ||
                (l.IsPensionable && IsEmployeeDeduction(l.ComponentType))).Sum(l => l.CalculatedAmount));
    }
}
