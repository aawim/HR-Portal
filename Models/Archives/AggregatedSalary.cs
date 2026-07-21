using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AggregatedSalary
{
    public int AggregatedSalaryId { get; set; }

    public int StaffSalaryId { get; set; }

    public decimal CalculatedSalaryAmount { get; set; }

    public decimal Ottotal { get; set; }

    public decimal AbsentFine { get; set; }

    public decimal LateFine { get; set; }

    public decimal Additions { get; set; }

    public decimal Deductions { get; set; }

    public decimal FinalSalary { get; set; }

    public int AbsentDays { get; set; }

    public virtual StaffSalary StaffSalary { get; set; } = null!;
}
