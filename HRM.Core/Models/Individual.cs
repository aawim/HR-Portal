using HRM.Models.Archives;
using System;
using System.Collections.Generic;
namespace HRM.Models;

public partial class Individual
{
    public int BusinessEntityId { get; set; }

    public bool? IsForeigner { get; set; }

    public byte[]? ImageData { get; set; }

    public string? ImageContentType { get; set; }

    public string FirstNameEnglish { get; set; } = null!;

    public string? MiddleNameEnglish { get; set; }

    public string? LastNameEnglish { get; set; }

    public string? FirstNameDhivehi { get; set; }

    public string? MiddleNameDhivehi { get; set; }

    public string? LastNameDhivehi { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public int GenderTypeId { get; set; }

    public int? CountryId { get; set; }

    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual Country? Country { get; set; }

    public virtual ICollection<Idcard> Idcards { get; set; } = new List<Idcard>();

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<Passport> Passports { get; set; } = new List<Passport>();

    public virtual Staff? Staff { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
