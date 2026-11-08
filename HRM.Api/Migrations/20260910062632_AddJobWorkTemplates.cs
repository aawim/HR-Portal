using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWorkTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdditionDeductionTypeDependencyTimePeriodAmounts",
                columns: table => new
                {
                    PayrollItemTypeDependencyTimePeriodAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionDeductionTypeDependencyTimePeriodAmounts", x => x.PayrollItemTypeDependencyTimePeriodAmountID);
                });

            migrationBuilder.CreateTable(
                name: "AdditionOrDeductionTypes",
                columns: table => new
                {
                    AdditionOrDeductionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionOrDeductionTypes", x => x.AdditionOrDeductionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AddressBaseTypes",
                columns: table => new
                {
                    AddressBaseTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressBaseTypes", x => x.AddressBaseTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AddressInstanceTypes",
                columns: table => new
                {
                    AddressInstanceTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressInstanceTypes", x => x.AddressInstanceTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AmountTypes",
                columns: table => new
                {
                    AmountTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmountTypes", x => x.AmountTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceClientStates",
                columns: table => new
                {
                    AttendanceClientStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceClientStates", x => x.AttendanceClientStateID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDependentTypes",
                columns: table => new
                {
                    AttendanceDependentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDependentTypes", x => x.AttendanceDependentTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDeviceInOutType",
                columns: table => new
                {
                    AttendanceDeviceInOutTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceDeviceInOutTypeName = table.Column<string>(type: "nchar(20)", fixedLength: true, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__696D3B51D59EE0C4", x => x.AttendanceDeviceInOutTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDeviceStates",
                columns: table => new
                {
                    AttendanceDeviceStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDeviceStates", x => x.AttendanceDeviceStateID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogChangeRequestTypes",
                columns: table => new
                {
                    AttendanceLogChangeRequestTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogRequestTypes", x => x.AttendanceLogChangeRequestTypeID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogModes",
                columns: table => new
                {
                    AttendanceLogModeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogModes", x => x.AttendanceLogModeID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogStates",
                columns: table => new
                {
                    AttendanceLogStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogStates", x => x.AttendanceLogStateID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogsTemp",
                columns: table => new
                {
                    AttendanceLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceDeviceID = table.Column<int>(type: "int", nullable: true),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    InOutModeID = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    Minute = table.Column<int>(type: "int", nullable: false),
                    Second = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    UIDStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogsTemp", x => x.AttendanceLogID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceResolutionStatuses",
                columns: table => new
                {
                    AttendanceResolutionStatusID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceResolutionStatuses", x => x.AttendanceResolutionStatusID);
                });

            migrationBuilder.CreateTable(
                name: "BudgetItems",
                columns: table => new
                {
                    BudgetItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemTypeID = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: true),
                    ParentBudgetItemID = table.Column<int>(type: "int", nullable: true),
                    DhivehiName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAmountGiven = table.Column<bool>(type: "bit", nullable: false),
                    BudgetYear = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetCodeSet", x => x.BudgetItemID);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransactionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityLocationTypes",
                columns: table => new
                {
                    BusinessEntityLocationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressTypes", x => x.BusinessEntityLocationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityRelationStates",
                columns: table => new
                {
                    BusinessEntityRelationStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityRelationStates", x => x.BusinessEntityRelationStateID);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityStates",
                columns: table => new
                {
                    BusinessEntityStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityStates", x => x.BusinessEntityStateID);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityTypes",
                columns: table => new
                {
                    BusinessEntityTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityTypes", x => x.BusinessEntityTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.CityID);
                });

            migrationBuilder.CreateTable(
                name: "CommercialIslandTypes",
                columns: table => new
                {
                    CommercialIslandTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialIslandTypes", x => x.CommercialIslandTypeID);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurableValues",
                columns: table => new
                {
                    ConfigurableValueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystemConfigValue = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurableValues", x => x.ConfigurableValueID);
                });

            migrationBuilder.CreateTable(
                name: "ContactInformationTypes",
                columns: table => new
                {
                    ContactInformationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Regex = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Format = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactInformationTypes", x => x.ContactInformationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Context",
                columns: table => new
                {
                    ContextID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ContextKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Context", x => x.ContextID);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    currencyId = table.Column<int>(type: "int", nullable: false),
                    currencyName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    currencyAbbreviation = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ISO4217Number = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    ISO4217Exponent = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ISO4271Code = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.currencyId);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyTypes",
                columns: table => new
                {
                    CurrencyTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ISO4217Number = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    ISO4217Exponent = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ISO4271Code = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyTypes", x => x.CurrencyTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionActionTypes",
                columns: table => new
                {
                    DataCorrectionActionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionTypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionActionTypes", x => x.DataCorrectionActionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionAttributeLookupTypes",
                columns: table => new
                {
                    DataCorrectionAttributeLookupTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionAttributeLookupTypes", x => x.DataCorrectionAttributeLookupTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionRequestStates",
                columns: table => new
                {
                    DataCorrectionRequestStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsProcessingState = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsFinalState = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionRequestStates", x => x.DataCorrectionRequestStateID);
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionRequestTypes",
                columns: table => new
                {
                    DataCorrectionRequestTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsValueChange = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionRequestTypes", x => x.DataCorrectionRequestTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DayOfWeek",
                columns: table => new
                {
                    DayOfWeekId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayOfWeek", x => x.DayOfWeekId);
                });

            migrationBuilder.CreateTable(
                name: "DayTypes",
                columns: table => new
                {
                    DayTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayTypes", x => x.DayTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DeductionAmountTypes",
                columns: table => new
                {
                    DeductionAmountTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionAmountTypes", x => x.DeductionAmountTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DependencyTypes",
                columns: table => new
                {
                    DependencyTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsValid = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyType", x => x.DependencyTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DerivedAdditionDeductionTypeItems",
                columns: table => new
                {
                    DerivedAdditionDeductionTypeItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedAdditionDeductionTypeItems", x => x.DerivedAdditionDeductionTypeItemID);
                });

            migrationBuilder.CreateTable(
                name: "DerivedOnTypes",
                columns: table => new
                {
                    DerivedOnTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedOnTypes", x => x.DerivedOnTypeID);
                });

            migrationBuilder.CreateTable(
                name: "DNRLookupCache",
                columns: table => new
                {
                    IDCardNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LookupData = table.Column<string>(type: "xml", nullable: false),
                    CreatedByIPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdatedByIPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DNRLookupCache", x => x.IDCardNo);
                });

            migrationBuilder.CreateTable(
                name: "DocumentStates",
                columns: table => new
                {
                    DocumentStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStates", x => x.DocumentStateID);
                });

            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    EventTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.EventTypeID);
                });

            migrationBuilder.CreateTable(
                name: "FacilityRegistrationTypes",
                columns: table => new
                {
                    FacilityRegistrationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    RegistrationNumberFormat = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RegistrationNumberRegex = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityRegistrationTypes", x => x.FacilityRegistrationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "GenderTypes",
                columns: table => new
                {
                    GenderTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abbreviation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenderTypes", x => x.GenderTypeID);
                });

            migrationBuilder.CreateTable(
                name: "GroupConfigurableValueTypes",
                columns: table => new
                {
                    GroupConfigurableValueTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupConfigurableValueTypes", x => x.GroupConfigurableValueTypeID);
                });

            migrationBuilder.CreateTable(
                name: "IDCardStates",
                columns: table => new
                {
                    IDCardStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDCardStates", x => x.IDCardStateID);
                });

            migrationBuilder.CreateTable(
                name: "IdentityCardTypes",
                columns: table => new
                {
                    IdentityCardTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityCardTypes", x => x.IdentityCardTypeID);
                });

            migrationBuilder.CreateTable(
                name: "InOutModes",
                columns: table => new
                {
                    InOutModeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameShort = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameLong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsIn = table.Column<bool>(type: "bit", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InOutModes", x => x.InOutModeID);
                });

            migrationBuilder.CreateTable(
                name: "JobPositionStates",
                columns: table => new
                {
                    JobPositionStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositionStates", x => x.JobPositionStateID);
                });

            migrationBuilder.CreateTable(
                name: "JobStates",
                columns: table => new
                {
                    JobStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPositionStates", x => x.JobStateID);
                });

            migrationBuilder.CreateTable(
                name: "JobTerminationTypes",
                columns: table => new
                {
                    JobTerminationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTerminationTypes", x => x.JobTerminationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CultureName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveChangeRequestTypes",
                columns: table => new
                {
                    LeaveChangeRequestTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveChangeRequestTypes", x => x.LeaveChangeRequestTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveConfigurableValueTypes",
                columns: table => new
                {
                    LeaveConfigurableValueTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    isJobTypeDependant = table.Column<bool>(type: "bit", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveConfigurableValueTypes", x => x.LeaveConfigurableValueTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveDefinitions",
                columns: table => new
                {
                    LeaveDefinitionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameDhivehi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerOrganisationID = table.Column<int>(type: "int", nullable: true),
                    IsSystemType = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveDefinitions", x => x.LeaveDefinitionID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveLodgeTypes",
                columns: table => new
                {
                    LeaveLodgeTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveLodgeTypes", x => x.LeaveLodgeTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveStates",
                columns: table => new
                {
                    LeaveStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveStates", x => x.LeaveStateID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypeReasonTypes",
                columns: table => new
                {
                    LeaveTypeReasonTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveTypeID = table.Column<int>(type: "int", nullable: false),
                    ReasonTypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypeReasonTypes", x => x.LeaveTypeReasonTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LinkTypes",
                columns: table => new
                {
                    LinkTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkTypes", x => x.LinkTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LocalUserStates",
                columns: table => new
                {
                    LocalUserStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalUserStates", x => x.LocalUserStateID);
                });

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                columns: table => new
                {
                    LocationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTypes", x => x.LocationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "MIMETypes",
                columns: table => new
                {
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MIMEType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsImage = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MIMETypes", x => x.Extension);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    ModuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsDhivehiSupported = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module", x => x.ModuleID);
                });

            migrationBuilder.CreateTable(
                name: "NoPayLeaveTypes",
                columns: table => new
                {
                    NoPayLeaveTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoPayLeaveTypes", x => x.NoPayLeaveTypeID);
                });

            migrationBuilder.CreateTable(
                name: "NoteReasons",
                columns: table => new
                {
                    NoteReasonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteReasons", x => x.NoteReasonID);
                });

            migrationBuilder.CreateTable(
                name: "OfficialTripTypes",
                columns: table => new
                {
                    OfficialTripTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsBasicSalaryGiven = table.Column<bool>(type: "bit", nullable: false),
                    IsAllowancesGiven = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialTripTypes", x => x.OfficialTripTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OperationLogActionTypes",
                columns: table => new
                {
                    OperationLogActionTypes = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogActionTypes", x => x.OperationLogActionTypes);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationBudgetMonthlySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    BudgetItemId = table.Column<int>(type: "int", nullable: false),
                    BudgetItemTypeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    DhivehiName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentBudgetItemId = table.Column<int>(type: "int", nullable: false),
                    ParentBudgetCode = table.Column<int>(type: "int", nullable: false),
                    ParentDhivehiName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentEnglishName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransferTotal = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    ExpenditureThisMonth = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    ExpenditureTotal = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    RemainingTotal = table.Column<decimal>(type: "decimal(38,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationBudgetMonthlySummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationBudgetSummaries",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    year = table.Column<int>(type: "int", nullable: false),
                    organisationID = table.Column<int>(type: "int", nullable: false),
                    budgetItemId = table.Column<int>(type: "int", nullable: false),
                    budgetItemTypeId = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<int>(type: "int", nullable: false),
                    dhivehiName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    englishName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    parentCode = table.Column<int>(type: "int", nullable: false),
                    parentbudgdetItemId = table.Column<int>(type: "int", nullable: false),
                    parentDhivehiName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    parentEnglishName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationBudgetSummaries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationBudgetSummeryAggregates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    year = table.Column<int>(type: "int", nullable: false),
                    organisationID = table.Column<int>(type: "int", nullable: false),
                    parentBudgetItemID = table.Column<int>(type: "int", nullable: false),
                    sum = table.Column<decimal>(type: "decimal(18,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationBudgetSummeryAggregate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureLocationTypes",
                columns: table => new
                {
                    OrganisationStructureLocationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureLocationTypes", x => x.OrganisationStructureLocationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureRequestTypes",
                columns: table => new
                {
                    OrganisationStructureRequestTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureRequestTypes", x => x.OrganisationStructureRequestTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureStates",
                columns: table => new
                {
                    OrganisationStructureStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureStates", x => x.OrganisationStructureStateID);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureTypes",
                columns: table => new
                {
                    OrganisationStructureTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureTypes", x => x.OrganisationStructureTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationTypes",
                columns: table => new
                {
                    OrganisationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationTypes", x => x.OrganisationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OTTypes",
                columns: table => new
                {
                    OTTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTTypes", x => x.OTTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OutOfficeRequestTypes",
                columns: table => new
                {
                    OutOfOfficeRequestTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutOfficeRequestTypes", x => x.OutOfOfficeRequestTypeID);
                });

            migrationBuilder.CreateTable(
                name: "OwnerTypes",
                columns: table => new
                {
                    OwnerTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerTypes", x => x.OwnerTypeID);
                });

            migrationBuilder.CreateTable(
                name: "PassportStates",
                columns: table => new
                {
                    PassportStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportStates", x => x.PassportStateID);
                });

            migrationBuilder.CreateTable(
                name: "PayrollConfigurableValueTypes",
                columns: table => new
                {
                    PayrollConfigurableValueTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    isJobTypeDependant = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollConfigurableValueTypes", x => x.PayrollConfigurableValueTypeID);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCycleProcessingStates",
                columns: table => new
                {
                    PayrollCycleProcessingStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCycleProcessingStates", x => x.PayrollCycleProcessingStateID);
                });

            migrationBuilder.CreateTable(
                name: "PositionTypes",
                columns: table => new
                {
                    PositionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionTypes", x => x.PositionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "RecurrenceTypes",
                columns: table => new
                {
                    RecurrenceTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceType", x => x.RecurrenceTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    RegionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.RegionID);
                });

            migrationBuilder.CreateTable(
                name: "RequestStates",
                columns: table => new
                {
                    RequestStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    IsProcessingState = table.Column<bool>(type: "bit", nullable: false),
                    IsFinalState = table.Column<bool>(type: "bit", nullable: false),
                    StateNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ButtonActionName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ButtonActionNameDhivehi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ButtonClass = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CommentRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStates", x => x.RequestStateID);
                });

            migrationBuilder.CreateTable(
                name: "SalaryRateDefinitionForTypes",
                columns: table => new
                {
                    SalaryRateDefinitionForTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryRateDefinitionForTypes", x => x.SalaryRateDefinitionForTypeID);
                });

            migrationBuilder.CreateTable(
                name: "SAPExceptionActionTypes",
                columns: table => new
                {
                    SAPExceptionActionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAPExceptionActionTypes", x => x.SAPExceptionActionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "SAPExceptionTypes",
                columns: table => new
                {
                    SAPExceptionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAPExceptionTypes", x => x.SAPExceptionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "SAPSheets",
                columns: table => new
                {
                    SAPSheetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SheetName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IsGenereatedForPayroll = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAPSheets", x => x.SAPSheetID);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTypes",
                columns: table => new
                {
                    ScheduleTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTypes", x => x.ScheduleTypeID);
                });

            migrationBuilder.CreateTable(
                name: "SequenceNumberTypes",
                columns: table => new
                {
                    SequenceNumberTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    Prefix = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FormatString = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    IsResetAnnually = table.Column<bool>(type: "bit", nullable: true),
                    StartingNumber = table.Column<int>(type: "int", nullable: true),
                    NextNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceNumberTypes", x => x.SequenceNumberTypeID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStates",
                columns: table => new
                {
                    ServiceStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStates", x => x.ServiceStateID);
                });

            migrationBuilder.CreateTable(
                name: "SickLeaveLodgeTypes",
                columns: table => new
                {
                    SickLeaveLodgeTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SickLeaveLodgeTypes", x => x.SickLeaveLodgeTypeID);
                });

            migrationBuilder.CreateTable(
                name: "SSOUserStates",
                columns: table => new
                {
                    SSOUserStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SSOUserStates", x => x.SSOUserStateID);
                });

            migrationBuilder.CreateTable(
                name: "StaffDailyAttandanceSummaryIssueTypes",
                columns: table => new
                {
                    StaffDailyAttandanceSummaryIssueTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDailyAttandanceSummaryIssueTypes", x => x.StaffDailyAttandanceSummaryIssueTypeId);
                });

            migrationBuilder.CreateTable(
                name: "StaffSalaryPayrollItemSAPDetails",
                columns: table => new
                {
                    StaffSalaryPayrollItemSAPDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FixedAmount = table.Column<double>(type: "float", nullable: true),
                    Difference = table.Column<double>(type: "float", nullable: true),
                    Days = table.Column<int>(type: "int", nullable: true),
                    Hours = table.Column<int>(type: "int", nullable: true),
                    Minutes = table.Column<int>(type: "int", nullable: true),
                    Rate = table.Column<double>(type: "float", nullable: true),
                    StaffSalaryPayrollItemID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    SAPWageTypeID = table.Column<int>(type: "int", nullable: true),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffSalaryPayrollItemSAPDetails", x => x.StaffSalaryPayrollItemSAPDetailID);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedStates",
                columns: table => new
                {
                    VerifiedStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedStates", x => x.VerifiedStateID);
                });

            migrationBuilder.CreateTable(
                name: "WebServiceRequestLogs",
                columns: table => new
                {
                    RequestLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Controller = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    IP = table.Column<string>(type: "nchar(15)", fixedLength: true, maxLength: 15, nullable: true),
                    RequestHeaders = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebServiceRequestLogs", x => x.RequestLogId);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignmentStates",
                columns: table => new
                {
                    WorkAssignmentStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsFinalState = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignmentStates", x => x.WorkAssignmentStateID);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignmentTransferStates",
                columns: table => new
                {
                    WorkAssignmentTransferStateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsProcessingState = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsFinalState = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignmentTransferStates", x => x.WorkAssignmentTransferStateID);
                });

            migrationBuilder.CreateTable(
                name: "WorkSegmentTypes",
                columns: table => new
                {
                    WorkSegmentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSegmentTypes", x => x.WorkSegmentTypeID);
                });

            migrationBuilder.CreateTable(
                name: "WorkTemplateTypes",
                columns: table => new
                {
                    WorkTemplateTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTemplateTypes", x => x.WorkTemplateTypeID);
                });

            migrationBuilder.CreateTable(
                name: "WorkTypes",
                columns: table => new
                {
                    WorkTypeID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WorkType__CCC06CC09CD52C96", x => x.WorkTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Atolls",
                columns: table => new
                {
                    AtollID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AbbreviationEnglish = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AbbreviationDhivehi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AtollCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CityID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atolls", x => x.AtollID);
                    table.ForeignKey(
                        name: "FK_Atolls_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionAttributes",
                columns: table => new
                {
                    DataCorrectionAttributeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AttributeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsLookup = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCorrectionAttributeLookupTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionAttributes", x => x.DataCorrectionAttributeID);
                    table.ForeignKey(
                        name: "FK_DataCorrectionAttributes_DataCorrectionAttributeLookupTypes",
                        column: x => x.DataCorrectionAttributeLookupTypeID,
                        principalTable: "DataCorrectionAttributeLookupTypes",
                        principalColumn: "DataCorrectionAttributeLookupTypeID");
                });

            migrationBuilder.CreateTable(
                name: "LeavePolicies",
                columns: table => new
                {
                    LeavePolicyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveDefinitionID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultEntitlementDays = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    IncludeHolidays = table.Column<bool>(type: "bit", nullable: false),
                    IncludePay = table.Column<bool>(type: "bit", nullable: false),
                    PayPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsLocationRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsStaffWideAvailable = table.Column<bool>(type: "bit", nullable: false),
                    MinimumServiceMonths = table.Column<int>(type: "int", nullable: true),
                    RequestTypeID = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavePolicies", x => x.LeavePolicyID);
                    table.ForeignKey(
                        name: "FK_LeavePolicies_LeaveDefinitions_LeaveDefinitionID",
                        column: x => x.LeaveDefinitionID,
                        principalTable: "LeaveDefinitions",
                        principalColumn: "LeaveDefinitionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypeMappings",
                columns: table => new
                {
                    LeaveTypeMappingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegacyLeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    LeaveDefinitionId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypeMappings", x => x.LeaveTypeMappingId);
                    table.ForeignKey(
                        name: "FK_LeaveTypeMappings_LeaveDefinitions_LeaveDefinitionId",
                        column: x => x.LeaveDefinitionId,
                        principalTable: "LeaveDefinitions",
                        principalColumn: "LeaveDefinitionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityRelatedLocationTypes",
                columns: table => new
                {
                    BusinessEntityRelatedLocationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationTypeID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityRelatedLocationTypes", x => x.BusinessEntityRelatedLocationTypeID);
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelatedLocationTypes_BusinessEntityTypes1",
                        column: x => x.BusinessEntityTypeID,
                        principalTable: "BusinessEntityTypes",
                        principalColumn: "BusinessEntityTypeID");
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelatedLocationTypes_LocationTypes1",
                        column: x => x.LocationTypeID,
                        principalTable: "LocationTypes",
                        principalColumn: "LocationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NationalityEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ISOAlpha2Code = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: true),
                    ISOAlpha3Code = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    ISONumericCode = table.Column<int>(type: "int", nullable: true),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NationalityDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RegionID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryID);
                    table.ForeignKey(
                        name: "FK_Countries_Regions",
                        column: x => x.RegionID,
                        principalTable: "Regions",
                        principalColumn: "RegionID");
                });

            migrationBuilder.CreateTable(
                name: "SAPWageTypes",
                columns: table => new
                {
                    SAPWageTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    WageTypeCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SheetID = table.Column<int>(type: "int", nullable: false),
                    GLCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAPWageTypes", x => x.SAPWageTypeID);
                    table.ForeignKey(
                        name: "FK_SAPWageTypes_SAPSheets",
                        column: x => x.SheetID,
                        principalTable: "SAPSheets",
                        principalColumn: "SAPSheetID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceTypes",
                columns: table => new
                {
                    ServiceTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    SequenceNumberTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.ServiceTypeID);
                    table.ForeignKey(
                        name: "FK_ServiceTypes_SequenceNumberTypes",
                        column: x => x.SequenceNumberTypeID,
                        principalTable: "SequenceNumberTypes",
                        principalColumn: "SequenceNumberTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Islands",
                columns: table => new
                {
                    IslandID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AtollID = table.Column<int>(type: "int", nullable: false),
                    PostCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsInhibited = table.Column<bool>(type: "bit", nullable: false),
                    CityID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Islands", x => x.IslandID);
                    table.ForeignKey(
                        name: "FK_Islands_Atolls",
                        column: x => x.AtollID,
                        principalTable: "Atolls",
                        principalColumn: "AtollID");
                    table.ForeignKey(
                        name: "FK_Islands_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionRequestTypeAttributes",
                columns: table => new
                {
                    DataCorrectionRequestTypeID = table.Column<int>(type: "int", nullable: false),
                    DataCorrectionAttributeID = table.Column<int>(type: "int", nullable: false),
                    DataCorrectionActionTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionRequestTypeAttributes", x => new { x.DataCorrectionRequestTypeID, x.DataCorrectionAttributeID, x.DataCorrectionActionTypeID });
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequestTypeAttributes_DataCorrectionActionTypes",
                        column: x => x.DataCorrectionActionTypeID,
                        principalTable: "DataCorrectionActionTypes",
                        principalColumn: "DataCorrectionActionTypeID");
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequestTypeAttributes_DataCorrectionAttributes",
                        column: x => x.DataCorrectionAttributeID,
                        principalTable: "DataCorrectionAttributes",
                        principalColumn: "DataCorrectionAttributeID");
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequestTypeAttributes_DataCorrectionRequestTypes",
                        column: x => x.DataCorrectionRequestTypeID,
                        principalTable: "DataCorrectionRequestTypes",
                        principalColumn: "DataCorrectionRequestTypeID");
                });

            migrationBuilder.CreateTable(
                name: "LeavePolicyAccrualRules",
                columns: table => new
                {
                    LeavePolicyAccrualRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeavePolicyID = table.Column<int>(type: "int", nullable: false),
                    AccrualType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccrualAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    AccrualIntervalMonths = table.Column<int>(type: "int", nullable: true),
                    AccrualStartMonth = table.Column<int>(type: "int", nullable: true),
                    MaximumBalance = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    CarryForwardAllowed = table.Column<bool>(type: "bit", nullable: false),
                    MaximumCarryForward = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    ExpiresAfterMonths = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavePolicyAccrualRules", x => x.LeavePolicyAccrualRuleID);
                    table.ForeignKey(
                        name: "FK_LeavePolicyAccrualRules_LeavePolicies_LeavePolicyID",
                        column: x => x.LeavePolicyID,
                        principalTable: "LeavePolicies",
                        principalColumn: "LeavePolicyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypes",
                columns: table => new
                {
                    RequestTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    ServiceTypeID = table.Column<int>(type: "int", nullable: true),
                    ViewURL = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    SequenceNumberTypeID = table.Column<int>(type: "int", nullable: true),
                    IsSystemType = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypes", x => x.RequestTypeID);
                    table.ForeignKey(
                        name: "FK_RequestTypes_SequenceNumberTypes",
                        column: x => x.SequenceNumberTypeID,
                        principalTable: "SequenceNumberTypes",
                        principalColumn: "SequenceNumberTypeID");
                    table.ForeignKey(
                        name: "FK_RequestTypes_ServiceTypes",
                        column: x => x.ServiceTypeID,
                        principalTable: "ServiceTypes",
                        principalColumn: "ServiceTypeID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceTypesAllowedServiceStateTransitions",
                columns: table => new
                {
                    ServiceTypeID = table.Column<int>(type: "int", nullable: false),
                    FromServiceStateID = table.Column<int>(type: "int", nullable: false),
                    ToServiceStateID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypesAllowedServiceStateTransitions", x => new { x.ServiceTypeID, x.FromServiceStateID, x.ToServiceStateID });
                    table.ForeignKey(
                        name: "FK_ServiceTypesAllowedServiceStateTransitions_ServiceStates",
                        column: x => x.ToServiceStateID,
                        principalTable: "ServiceStates",
                        principalColumn: "ServiceStateID");
                    table.ForeignKey(
                        name: "FK_ServiceTypesAllowedServiceStateTransitions_ServiceTypes",
                        column: x => x.ServiceTypeID,
                        principalTable: "ServiceTypes",
                        principalColumn: "ServiceTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    WardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IslandID = table.Column<int>(type: "int", nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AbbreviationEnglish = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AbbreviationDhivehi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.WardID);
                    table.ForeignKey(
                        name: "FK_Wards_Islands",
                        column: x => x.IslandID,
                        principalTable: "Islands",
                        principalColumn: "IslandID");
                });

            migrationBuilder.CreateTable(
                name: "RequestTypesAllowedRequestStateTransitions",
                columns: table => new
                {
                    RequestTypeID = table.Column<int>(type: "int", nullable: false),
                    FromRequestStateID = table.Column<int>(type: "int", nullable: false),
                    ToRequestStateID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypesAllowedRequestStateTransitions", x => new { x.RequestTypeID, x.FromRequestStateID, x.ToRequestStateID });
                    table.ForeignKey(
                        name: "FK_RequestTypesAllowedRequestStateTransitions_FromRequestStates",
                        column: x => x.FromRequestStateID,
                        principalTable: "RequestStates",
                        principalColumn: "RequestStateID");
                    table.ForeignKey(
                        name: "FK_RequestTypesAllowedRequestStateTransitions_RequestTypes",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                    table.ForeignKey(
                        name: "FK_RequestTypesAllowedRequestStateTransitions_ToRequestStates",
                        column: x => x.ToRequestStateID,
                        principalTable: "RequestStates",
                        principalColumn: "RequestStateID");
                });

            migrationBuilder.CreateTable(
                name: "AddressBases",
                columns: table => new
                {
                    AddressBaseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HomeNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StreetNameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StreetNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: true),
                    MunicipalityNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressBaseTypeID = table.Column<int>(type: "int", nullable: false),
                    WardID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.AddressBaseID);
                    table.ForeignKey(
                        name: "FK_AddresseBases_AddressBaseTypes",
                        column: x => x.AddressBaseTypeID,
                        principalTable: "AddressBaseTypes",
                        principalColumn: "AddressBaseTypeID");
                    table.ForeignKey(
                        name: "FK_Addresses_Wards",
                        column: x => x.WardID,
                        principalTable: "Wards",
                        principalColumn: "WardID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineAddressBases",
                columns: table => new
                {
                    OnlineAddressBaseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StreetNameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StreetNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: true),
                    MunicipalityNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressBaseTypeID = table.Column<int>(type: "int", nullable: false),
                    WardID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineAddressBases", x => x.OnlineAddressBaseID);
                    table.ForeignKey(
                        name: "FK_OnlineAddressBases_AddressBaseTypes",
                        column: x => x.AddressBaseTypeID,
                        principalTable: "AddressBaseTypes",
                        principalColumn: "AddressBaseTypeID");
                    table.ForeignKey(
                        name: "FK_OnlineAddressBases_Wards",
                        column: x => x.WardID,
                        principalTable: "Wards",
                        principalColumn: "WardID");
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    IslandID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.LocationID);
                    table.ForeignKey(
                        name: "FK_Address_Islands",
                        column: x => x.IslandID,
                        principalTable: "Islands",
                        principalColumn: "IslandID");
                });

            migrationBuilder.CreateTable(
                name: "AddressInstances",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AddressInstanceTypeID = table.Column<int>(type: "int", nullable: true),
                    AddressBaseID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressInstances", x => x.LocationID);
                    table.ForeignKey(
                        name: "FK_AddressInstances_AddressBases",
                        column: x => x.AddressBaseID,
                        principalTable: "AddressBases",
                        principalColumn: "AddressBaseID");
                    table.ForeignKey(
                        name: "FK_AddressInstances_AddressInstanceTypes",
                        column: x => x.AddressInstanceTypeID,
                        principalTable: "AddressInstanceTypes",
                        principalColumn: "AddressInstanceTypeID");
                    table.ForeignKey(
                        name: "FK_AddressInstances_Addresses",
                        column: x => x.LocationID,
                        principalTable: "Addresses",
                        principalColumn: "LocationID");
                });

            migrationBuilder.CreateTable(
                name: "Dhafthar",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    DhaftharNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dhafthar", x => x.LocationID);
                    table.ForeignKey(
                        name: "FK_Dhafthar_Address",
                        column: x => x.LocationID,
                        principalTable: "Addresses",
                        principalColumn: "LocationID");
                });

            migrationBuilder.CreateTable(
                name: "AggregatedSalaries",
                columns: table => new
                {
                    AggregatedSalaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false),
                    CalculatedSalaryAmount = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    OTTotal = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    AbsentFine = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    LateFine = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    Additions = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    FinalSalary = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    AbsentDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregatedSalaries", x => x.AggregatedSalaryID);
                });

            migrationBuilder.CreateTable(
                name: "AssignedWorkTypes",
                columns: table => new
                {
                    AssignedWorkTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkTypeID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedByUserID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedWorkTypes", x => x.AssignedWorkTypeID);
                    table.ForeignKey(
                        name: "FK_AssignedWorkTypes_WorkTypes",
                        column: x => x.WorkTypeID,
                        principalTable: "WorkTypes",
                        principalColumn: "WorkTypeID");
                });

            migrationBuilder.CreateTable(
                name: "AttachedBreakTimes",
                columns: table => new
                {
                    AttachedBreakTimeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerTypeID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachedBreakTimes", x => x.AttachedBreakTimeID);
                    table.ForeignKey(
                        name: "FK_AttachedBreakTimes_OwnerTypes",
                        column: x => x.OwnerTypeID,
                        principalTable: "OwnerTypes",
                        principalColumn: "OwnerTypeID");
                });

            migrationBuilder.CreateTable(
                name: "AttachedBreakTimesDays",
                columns: table => new
                {
                    AttachedBreakTimesDayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttachedBreakTimeID = table.Column<int>(type: "int", nullable: false),
                    DayOfWeekID = table.Column<int>(type: "int", nullable: false),
                    BreakTimeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachedBreakTimesDays", x => x.AttachedBreakTimesDayID);
                    table.ForeignKey(
                        name: "FK_AttachedBreakTimesDays_AttachedBreakTimes",
                        column: x => x.AttachedBreakTimeID,
                        principalTable: "AttachedBreakTimes",
                        principalColumn: "AttachedBreakTimeID");
                    table.ForeignKey(
                        name: "FK_AttachedBreakTimesDays_DayOfWeeks",
                        column: x => x.DayOfWeekID,
                        principalTable: "DayOfWeek",
                        principalColumn: "DayOfWeekId");
                });

            migrationBuilder.CreateTable(
                name: "AttachedPayrollItemsProcessingWhiteLists",
                columns: table => new
                {
                    AttachedPayrollItemsProcessingWhiteListID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemsProcessingWhiteListID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachedPayrollItemsProcessingWhiteLists", x => x.AttachedPayrollItemsProcessingWhiteListID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceClientInstances",
                columns: table => new
                {
                    AttendanceClientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AttendanceClientKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    AttendanceClientStateID = table.Column<int>(type: "int", nullable: false),
                    LastAttendanceManuallyUpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastAttendanceManualUpdateRecord = table.Column<DateTime>(type: "datetime", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceClients_1", x => x.AttendanceClientID);
                    table.ForeignKey(
                        name: "FK_AttendanceClients_AttendanceClientStates1",
                        column: x => x.AttendanceClientStateID,
                        principalTable: "AttendanceClientStates",
                        principalColumn: "AttendanceClientStateID");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDevices",
                columns: table => new
                {
                    AttendanceDeviceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceDeviceStateID = table.Column<int>(type: "int", nullable: false),
                    AttendanceClientID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    AttendanceDeviceInOutTypeID = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDevices", x => x.AttendanceDeviceID);
                    table.ForeignKey(
                        name: "FK_AttendanceDevices_AttendanceClients",
                        column: x => x.AttendanceClientID,
                        principalTable: "AttendanceClientInstances",
                        principalColumn: "AttendanceClientID");
                    table.ForeignKey(
                        name: "FK_AttendanceDevices_AttendanceDeviceInOutTypeID",
                        column: x => x.AttendanceDeviceInOutTypeID,
                        principalTable: "AttendanceDeviceInOutType",
                        principalColumn: "AttendanceDeviceInOutTypeID");
                    table.ForeignKey(
                        name: "FK_AttendanceDevices_AttendanceDeviceStates",
                        column: x => x.AttendanceDeviceStateID,
                        principalTable: "AttendanceDeviceStates",
                        principalColumn: "AttendanceDeviceStateID");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDeviceStaffs",
                columns: table => new
                {
                    AttendanceDeviceStaffID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceDeviceID = table.Column<int>(type: "int", nullable: false),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    EnrollmentNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDeviceStaffs", x => x.AttendanceDeviceStaffID);
                    table.ForeignKey(
                        name: "FK_AttendanceDeviceStaffs_AttendanceDevices",
                        column: x => x.AttendanceDeviceID,
                        principalTable: "AttendanceDevices",
                        principalColumn: "AttendanceDeviceID");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogChangeRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    AttendanceLogID = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogChangeRequests", x => x.RequestID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogRequests",
                columns: table => new
                {
                    AttendanceLogRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceLogID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualAttendanceLogRequests", x => x.AttendanceLogRequestID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogResolutions",
                columns: table => new
                {
                    AttendanceLogResolutionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceLogID = table.Column<int>(type: "int", nullable: false),
                    WorkPlanID = table.Column<long>(type: "bigint", nullable: true),
                    WorkAssignmentID = table.Column<long>(type: "bigint", nullable: true),
                    WorkAssignmentSegmentID = table.Column<long>(type: "bigint", nullable: true),
                    JobID = table.Column<int>(type: "int", nullable: true),
                    AttendanceResolutionStatusID = table.Column<int>(type: "int", nullable: false),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    ResolutionMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsLocationValidated = table.Column<bool>(type: "bit", nullable: true),
                    IsDeviceValidated = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogResolutions", x => x.AttendanceLogResolutionID);
                    table.ForeignKey(
                        name: "FK_AttendanceLogResolutions_AttendanceResolutionStatuses_AttendanceResolutionStatusID",
                        column: x => x.AttendanceResolutionStatusID,
                        principalTable: "AttendanceResolutionStatuses",
                        principalColumn: "AttendanceResolutionStatusID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                columns: table => new
                {
                    AttendanceLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceDeviceID = table.Column<int>(type: "int", nullable: true),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    InOutModeID = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    Minute = table.Column<int>(type: "int", nullable: false),
                    Second = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    UIDStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttendanceLogModeID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AttendanceLogStateID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    RelatedAttendanceLogID = table.Column<int>(type: "int", nullable: true),
                    ActualInOutMode = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogss", x => x.AttendanceLogID);
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_AttendanceDevices",
                        column: x => x.AttendanceDeviceID,
                        principalTable: "AttendanceDevices",
                        principalColumn: "AttendanceDeviceID");
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_AttendanceLogModes",
                        column: x => x.AttendanceLogModeID,
                        principalTable: "AttendanceLogModes",
                        principalColumn: "AttendanceLogModeID");
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_AttendanceLogStates",
                        column: x => x.AttendanceLogStateID,
                        principalTable: "AttendanceLogStates",
                        principalColumn: "AttendanceLogStateID");
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_AttendanceLogs",
                        column: x => x.RelatedAttendanceLogID,
                        principalTable: "AttendanceLogs",
                        principalColumn: "AttendanceLogID");
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_InOutModes",
                        column: x => x.InOutModeID,
                        principalTable: "InOutModes",
                        principalColumn: "InOutModeID");
                });

            migrationBuilder.CreateTable(
                name: "BasicSalaries",
                columns: table => new
                {
                    BasicSalaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasicSalaries", x => x.BasicSalaryID);
                });

            migrationBuilder.CreateTable(
                name: "BreakTimes",
                columns: table => new
                {
                    BreakTimeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakTimes", x => x.BreakTimeID);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                columns: table => new
                {
                    BudgetTransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetItemID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LetterNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    TransactionTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAdditionDeductions", x => x.BudgetTransactionID);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_BudgetItems",
                        column: x => x.BudgetItemID,
                        principalTable: "BudgetItems",
                        principalColumn: "BudgetItemID");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_BudgetTransactionTypes",
                        column: x => x.TransactionTypeID,
                        principalTable: "BudgetTransactionTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BulkUploadedDocuments",
                columns: table => new
                {
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserID = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DocumentStateID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkUploadedDocuments", x => x.DocumentID);
                    table.ForeignKey(
                        name: "FK_BulkUploadedDocuments_DocumentStates",
                        column: x => x.DocumentStateID,
                        principalTable: "DocumentStates",
                        principalColumn: "DocumentStateID");
                });

            migrationBuilder.CreateTable(
                name: "BulkUploadedDocumentSummaries",
                columns: table => new
                {
                    BulkUploadedDocumentSummaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    RowID = table.Column<int>(type: "int", nullable: false),
                    IndexName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkUploadedDocumentSummaries", x => x.BulkUploadedDocumentSummaryID);
                    table.ForeignKey(
                        name: "FK_BulkUploadedDocumentSummaries_BulkUploadedDocuments",
                        column: x => x.DocumentID,
                        principalTable: "BulkUploadedDocuments",
                        principalColumn: "DocumentID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntities",
                columns: table => new
                {
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityTypeID = table.Column<int>(type: "int", nullable: false),
                    VerifiedStateID = table.Column<int>(type: "int", nullable: true),
                    BusinessEntityStateID = table.Column<int>(type: "int", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastStateChangedByUserID = table.Column<int>(type: "int", nullable: true),
                    LastStateChangeDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntities", x => x.BusinessEntityID);
                    table.ForeignKey(
                        name: "FK_BusinessEntities_BusinessEntityStates",
                        column: x => x.BusinessEntityStateID,
                        principalTable: "BusinessEntityStates",
                        principalColumn: "BusinessEntityStateID");
                    table.ForeignKey(
                        name: "FK_BusinessEntities_BusinessEntityTypes",
                        column: x => x.BusinessEntityTypeID,
                        principalTable: "BusinessEntityTypes",
                        principalColumn: "BusinessEntityTypeID");
                    table.ForeignKey(
                        name: "FK_BusinessEntities_VerifiedStates",
                        column: x => x.VerifiedStateID,
                        principalTable: "VerifiedStates",
                        principalColumn: "VerifiedStateID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityRelationTypes",
                columns: table => new
                {
                    BusinessEntityRelationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityRelationTypes", x => x.BusinessEntityRelationTypeID);
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelationTypes_BusinessEntities1",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "Individuals",
                columns: table => new
                {
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    FirstNameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MiddleNameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastNameEnglish = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FirstNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MiddleNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime", nullable: true),
                    GenderTypeID = table.Column<int>(type: "int", nullable: false),
                    CountryID = table.Column<int>(type: "int", nullable: true, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Individuals", x => x.BusinessEntityID);
                    table.ForeignKey(
                        name: "FK_Individuals_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Individuals_Countries",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_Individuals_GenderTypes",
                        column: x => x.GenderTypeID,
                        principalTable: "GenderTypes",
                        principalColumn: "GenderTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    OrganisationTypeID = table.Column<int>(type: "int", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrganisationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganisationNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    ParentOrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: true),
                    CSCOfficePimaryKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.BusinessEntityID);
                    table.ForeignKey(
                        name: "FK_Organisations_BusinessEntities_BusinessEntityID",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Organisations_Countries",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_Organisations_OrganisationTypes",
                        column: x => x.OrganisationTypeID,
                        principalTable: "OrganisationTypes",
                        principalColumn: "OrganisationTypeID");
                    table.ForeignKey(
                        name: "FK_Organisations_Organisations",
                        column: x => x.ParentOrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystemType = table.Column<bool>(type: "bit", nullable: false),
                    IsImage = table.Column<bool>(type: "bit", nullable: false),
                    ShouldBeColor = table.Column<bool>(type: "bit", nullable: false),
                    ImageMinimumResolution = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: true),
                    ServiceTypeID = table.Column<int>(type: "int", nullable: true),
                    TypeNameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeID);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_DocumentTypes_ServiceTypes",
                        column: x => x.ServiceTypeID,
                        principalTable: "ServiceTypes",
                        principalColumn: "ServiceTypeID");
                });

            migrationBuilder.CreateTable(
                name: "GroupTypes",
                columns: table => new
                {
                    GroupTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OrganisationBusinessEntityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupTypes", x => x.GroupTypeID);
                    table.ForeignKey(
                        name: "FK_GroupTypes_Organisations",
                        column: x => x.OrganisationBusinessEntityId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationBudgets",
                columns: table => new
                {
                    OrganisationBudgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    BudgetCodeID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.OrganisationBudgetId);
                    table.ForeignKey(
                        name: "FK_Budgets_BudgetCodes",
                        column: x => x.BudgetCodeID,
                        principalTable: "BudgetItems",
                        principalColumn: "BudgetItemID");
                    table.ForeignKey(
                        name: "FK_Budgets_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "PlanningProviders",
                columns: table => new
                {
                    PlanningProviderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningProviders", x => x.PlanningProviderID);
                    table.ForeignKey(
                        name: "FK_PlanningProviders_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypeSpecificDocumentTypes",
                columns: table => new
                {
                    RequestTypeSpecificDocumentTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTypeID = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    ContextID = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypeSpecificDocumentTypes", x => x.RequestTypeSpecificDocumentTypeId);
                    table.ForeignKey(
                        name: "FK_RequestTypeSpecificDocumentTypes_Context",
                        column: x => x.ContextID,
                        principalTable: "Context",
                        principalColumn: "ContextID");
                    table.ForeignKey(
                        name: "FK_RequestTypeSpecificDocumentTypes_DocumentTypes",
                        column: x => x.DocumentTypeID,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeID");
                    table.ForeignKey(
                        name: "FK_RequestTypeSpecificDocumentTypes_RequestTypes",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntitiesDocuments",
                columns: table => new
                {
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    LinkedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LinkedByGlobalUserID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntitiesDocuments", x => new { x.BusinessEntityID, x.DocumentID });
                    table.ForeignKey(
                        name: "FK_BusinessEntitiesDocuments_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityCalendars",
                columns: table => new
                {
                    BusinessEntityCalenderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalenderID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsLinked = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityCalenders", x => x.BusinessEntityCalenderID);
                    table.ForeignKey(
                        name: "FK_BusinessEntityCalenders_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityLocations",
                columns: table => new
                {
                    BusinessEntityLocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityLocationTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BussinessEntityAddresses", x => x.BusinessEntityLocationID);
                    table.ForeignKey(
                        name: "FK_BusinessEntityLocations_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_BusinessEntityLocations_BusinessEntityLocationTypes",
                        column: x => x.BusinessEntityLocationTypeID,
                        principalTable: "BusinessEntityLocationTypes",
                        principalColumn: "BusinessEntityLocationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityRelationAssignedRoles",
                columns: table => new
                {
                    BsinessEntityRelationAssignedRoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityRelationID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityRelationAssignedRoles", x => x.BsinessEntityRelationAssignedRoleID);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityRelations",
                columns: table => new
                {
                    BusinessEntityRelationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityRelationStateID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityRelationTypeID = table.Column<int>(type: "int", nullable: false),
                    DelegatorBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    DelegateeBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    PreviousBusinessEntityRelationSateID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityRelations", x => x.BusinessEntityRelationID);
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelations_BusinessEntityRelationStates",
                        column: x => x.BusinessEntityRelationStateID,
                        principalTable: "BusinessEntityRelationStates",
                        principalColumn: "BusinessEntityRelationStateID");
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelations_BusinessEntityRelationTypes",
                        column: x => x.BusinessEntityRelationTypeID,
                        principalTable: "BusinessEntityRelationTypes",
                        principalColumn: "BusinessEntityRelationTypeID");
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelations_Delegatee",
                        column: x => x.DelegateeBusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_BusinessEntityRelations_Delegator",
                        column: x => x.DelegatorBusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntityRequests",
                columns: table => new
                {
                    BusinessEntityRelationID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntityRequests", x => new { x.BusinessEntityRelationID, x.RequestID });
                    table.ForeignKey(
                        name: "FK_BusinessEntityRequests_BusinessEntityRelations",
                        column: x => x.BusinessEntityRelationID,
                        principalTable: "BusinessEntityRelations",
                        principalColumn: "BusinessEntityRelationID");
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntitySchedules",
                columns: table => new
                {
                    BusinessEntityScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    IsLinked = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntitySchedules", x => x.BusinessEntityScheduleID);
                    table.ForeignKey(
                        name: "FK_BusinessEntitySchedules_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "BussinessEntityContactInformations",
                columns: table => new
                {
                    BussinessEntityContactInformationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BussinessEntityID = table.Column<int>(type: "int", nullable: false),
                    ContactInformationTypeID = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BussinessEntityContactInformations", x => x.BussinessEntityContactInformationID);
                    table.ForeignKey(
                        name: "FK_BussinessEntityContactInformations_BusinessEntities",
                        column: x => x.BussinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_BussinessEntityContactInformations_ContactInformationTypes",
                        column: x => x.ContactInformationTypeID,
                        principalTable: "ContactInformationTypes",
                        principalColumn: "ContactInformationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "CalendarElements",
                columns: table => new
                {
                    CalendarID = table.Column<int>(type: "int", nullable: false),
                    ElementID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarElements", x => new { x.CalendarID, x.ElementID });
                });

            migrationBuilder.CreateTable(
                name: "CalendarInstances",
                columns: table => new
                {
                    CalendarInstanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CalendarID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarInstances", x => x.CalendarInstanceID);
                });

            migrationBuilder.CreateTable(
                name: "CalendarMonths",
                columns: table => new
                {
                    CalendarMonthID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    NumberOfDays = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    CalendarInstanceID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarMonths", x => x.CalendarMonthID);
                    table.ForeignKey(
                        name: "FK_CalendarMonths_CalendarInstances",
                        column: x => x.CalendarInstanceID,
                        principalTable: "CalendarInstances",
                        principalColumn: "CalendarInstanceID");
                });

            migrationBuilder.CreateTable(
                name: "Calendars",
                columns: table => new
                {
                    CalenderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    CalendarTypeID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calenders", x => x.CalenderID);
                });

            migrationBuilder.CreateTable(
                name: "ParentCalenders",
                columns: table => new
                {
                    CalenderID = table.Column<int>(type: "int", nullable: false),
                    ParentCalenderID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentCalenders", x => new { x.CalenderID, x.ParentCalenderID });
                    table.ForeignKey(
                        name: "FK_ParentCalenders_Child",
                        column: x => x.CalenderID,
                        principalTable: "Calendars",
                        principalColumn: "CalenderID");
                    table.ForeignKey(
                        name: "FK_ParentCalenders_Parent",
                        column: x => x.ParentCalenderID,
                        principalTable: "Calendars",
                        principalColumn: "CalenderID");
                });

            migrationBuilder.CreateTable(
                name: "CalendarSchedules",
                columns: table => new
                {
                    CalendarScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    CalendarID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarSchedules", x => x.CalendarScheduleID);
                    table.ForeignKey(
                        name: "FK_CalendarSchedules_Calendars",
                        column: x => x.CalendarID,
                        principalTable: "Calendars",
                        principalColumn: "CalenderID");
                });

            migrationBuilder.CreateTable(
                name: "CalendarTypes",
                columns: table => new
                {
                    CalendarTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarVariants", x => x.CalendarTypeID);
                });

            migrationBuilder.CreateTable(
                name: "CheckListItems",
                columns: table => new
                {
                    CheckListItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckListItemName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckListItems", x => x.CheckListItemID);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypeSpecificCheckListItems",
                columns: table => new
                {
                    CheckListItemID = table.Column<int>(type: "int", nullable: false),
                    RequestTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypeSpecificCheckListItems", x => new { x.CheckListItemID, x.RequestTypeID });
                    table.ForeignKey(
                        name: "FK_RequestTypeSpecificCheckListItems_CheckListItems",
                        column: x => x.CheckListItemID,
                        principalTable: "CheckListItems",
                        principalColumn: "CheckListItemID");
                    table.ForeignKey(
                        name: "FK_RequestTypeSpecificCheckListItems_RequestTypes",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceTypeSpecificCheckListItems",
                columns: table => new
                {
                    CheckListItemID = table.Column<int>(type: "int", nullable: false),
                    ServiceTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypeSpecificCheckListItems", x => new { x.CheckListItemID, x.ServiceTypeID });
                    table.ForeignKey(
                        name: "FK_ServiceTypeSpecificCheckListItems_CheckListItems",
                        column: x => x.CheckListItemID,
                        principalTable: "CheckListItems",
                        principalColumn: "CheckListItemID");
                    table.ForeignKey(
                        name: "FK_ServiceTypeSpecificCheckListItems_ServiceTypes",
                        column: x => x.ServiceTypeID,
                        principalTable: "ServiceTypes",
                        principalColumn: "ServiceTypeID");
                });

            migrationBuilder.CreateTable(
                name: "CurrencyExchangeRates",
                columns: table => new
                {
                    CurrencyExchangeRateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyTpeID = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyExchangeRates", x => x.CurrencyExchangeRateID);
                    table.ForeignKey(
                        name: "FK_CurrencyExchangeRates_CurrencyTypes",
                        column: x => x.CurrencyTpeID,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID");
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionRequestAttributeValues",
                columns: table => new
                {
                    DataCorrectionRequestAttributeValueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataCorrectionRequestID = table.Column<int>(type: "int", nullable: false),
                    DataCorrectionAttributeID = table.Column<int>(type: "int", nullable: false),
                    DataCorrectionActionTypeID = table.Column<int>(type: "int", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValueDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValueDetails = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionRequestAttributeValues", x => x.DataCorrectionRequestAttributeValueID);
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequestAttributeValues_DataCorrectionActionTypes",
                        column: x => x.DataCorrectionActionTypeID,
                        principalTable: "DataCorrectionActionTypes",
                        principalColumn: "DataCorrectionActionTypeID");
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequestAttributeValues_DataCorrectionAttributes",
                        column: x => x.DataCorrectionAttributeID,
                        principalTable: "DataCorrectionAttributes",
                        principalColumn: "DataCorrectionAttributeID");
                });

            migrationBuilder.CreateTable(
                name: "DataCorrectionRequests",
                columns: table => new
                {
                    DataCorrectionRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataCorrectionRequestStateID = table.Column<int>(type: "int", nullable: false),
                    DataCorrectionRequestTypeID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedByUserID = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastStateChangedByUserID = table.Column<int>(type: "int", nullable: false),
                    LastStateChangedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    StateChangeRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionRequests", x => x.DataCorrectionRequestID);
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequests_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequests_DataCorrectionRequestStates",
                        column: x => x.DataCorrectionRequestStateID,
                        principalTable: "DataCorrectionRequestStates",
                        principalColumn: "DataCorrectionRequestStateID");
                    table.ForeignKey(
                        name: "FK_DataCorrectionRequests_DataCorrectionRequestTypes",
                        column: x => x.DataCorrectionRequestTypeID,
                        principalTable: "DataCorrectionRequestTypes",
                        principalColumn: "DataCorrectionRequestTypeID");
                });

            migrationBuilder.CreateTable(
                name: "DeductionTypeAmounts",
                columns: table => new
                {
                    DeductionTypeAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeductionTypeID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionTypeAmounts", x => x.DeductionTypeAmountID);
                });

            migrationBuilder.CreateTable(
                name: "DeductionTypes",
                columns: table => new
                {
                    DeductionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeNameEnglish = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TypeNameDhivehi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    IsApplicableToAll = table.Column<bool>(type: "bit", nullable: false),
                    DerivedOnTypeID = table.Column<int>(type: "int", nullable: true),
                    DeductionAmountTypeID = table.Column<int>(type: "int", nullable: true),
                    HasSchedule = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IsDerived = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    CurrencyTypeID = table.Column<int>(type: "int", nullable: true),
                    MinAmount = table.Column<double>(type: "float", nullable: true),
                    MaxAmount = table.Column<double>(type: "float", nullable: true),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    HasMax = table.Column<bool>(type: "bit", nullable: false),
                    HasMin = table.Column<bool>(type: "bit", nullable: false),
                    IsAmountGivenWhenAssigning = table.Column<bool>(type: "bit", nullable: false),
                    OwnerOrganisationID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionTypes", x => x.DeductionTypeID);
                    table.ForeignKey(
                        name: "FK_DeductionTypes_CurrencyTypes",
                        column: x => x.CurrencyTypeID,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID");
                    table.ForeignKey(
                        name: "FK_DeductionTypes_DeductionAmountTypes",
                        column: x => x.DeductionAmountTypeID,
                        principalTable: "DeductionAmountTypes",
                        principalColumn: "DeductionAmountTypeID");
                    table.ForeignKey(
                        name: "FK_DeductionTypes_DerivedOnTypes",
                        column: x => x.DerivedOnTypeID,
                        principalTable: "DerivedOnTypes",
                        principalColumn: "DerivedOnTypeID");
                    table.ForeignKey(
                        name: "FK_DeductionTypes_Organisations",
                        column: x => x.OwnerOrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "DerivedAdditionDeductionTypes",
                columns: table => new
                {
                    DerivedAdditionDeductionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeductionTypeID = table.Column<int>(type: "int", nullable: false),
                    DerivedAdditionDeductionTypeItemID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedAdditionDeductionTypes", x => x.DerivedAdditionDeductionTypeID);
                    table.ForeignKey(
                        name: "FK_DerivedAdditionDeductionTypes_DeductionTypes",
                        column: x => x.DeductionTypeID,
                        principalTable: "DeductionTypes",
                        principalColumn: "DeductionTypeID");
                    table.ForeignKey(
                        name: "FK_DerivedAdditionDeductionTypes_DerivedAdditionDeductionTypeItems",
                        column: x => x.DerivedAdditionDeductionTypeItemID,
                        principalTable: "DerivedAdditionDeductionTypeItems",
                        principalColumn: "DerivedAdditionDeductionTypeItemID");
                });

            migrationBuilder.CreateTable(
                name: "DerivedOnPayrollItemTypes",
                columns: table => new
                {
                    DerivedOnPayrollItemTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    OtherPayrollItemTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedOnPayrollItemTypes", x => x.DerivedOnPayrollItemTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MIMEType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DocumentSize = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentID);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentTypes",
                        column: x => x.DocumentTypeID,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Elements",
                columns: table => new
                {
                    ElementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DayOfWeekID = table.Column<int>(type: "int", nullable: true),
                    SpecialDayTypeID = table.Column<int>(type: "int", nullable: false),
                    WorkShiftID = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elements", x => x.ElementID);
                    table.ForeignKey(
                        name: "FK_ScheduleElements_DayOfWeeks",
                        column: x => x.DayOfWeekID,
                        principalTable: "DayOfWeek",
                        principalColumn: "DayOfWeekId");
                });

            migrationBuilder.CreateTable(
                name: "GroupConfigurableValues",
                columns: table => new
                {
                    GroupConfigurableValueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupConfigurableValueTypeID = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupConfigurableValues", x => x.GroupConfigurableValueID);
                    table.ForeignKey(
                        name: "FK_GroupConfigurableValues_GroupConfigurableValueTypes",
                        column: x => x.GroupConfigurableValueTypeID,
                        principalTable: "GroupConfigurableValueTypes",
                        principalColumn: "GroupConfigurableValueTypeID");
                });

            migrationBuilder.CreateTable(
                name: "GroupLeaveSets",
                columns: table => new
                {
                    GroupLeaveSetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveSetID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false, defaultValue: 2)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupLeaveSets", x => x.GroupLeaveSetID);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupTypeID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BusinessEntityOrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false, defaultValue: 2)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupID);
                    table.ForeignKey(
                        name: "FK_Groups_GroupTypes",
                        column: x => x.GroupTypeID,
                        principalTable: "GroupTypes",
                        principalColumn: "GroupTypeID");
                    table.ForeignKey(
                        name: "FK_Groups_Organisations",
                        column: x => x.BusinessEntityOrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "GroupSchedules",
                columns: table => new
                {
                    GroupScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupSchedules", x => x.GroupScheduleID);
                    table.ForeignKey(
                        name: "FK_GroupSchedules_Groups",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID");
                });

            migrationBuilder.CreateTable(
                name: "GroupStaffs",
                columns: table => new
                {
                    GroupStaffID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupID = table.Column<int>(type: "int", nullable: false),
                    StaffIndividualID = table.Column<int>(type: "int", nullable: false),
                    EffectiveStartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EffectiveEndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupStaffs", x => x.GroupStaffID);
                    table.ForeignKey(
                        name: "FK_GroupStaffs_Groups",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID");
                });

            migrationBuilder.CreateTable(
                name: "IDCards",
                columns: table => new
                {
                    IDCardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    IDCardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IDCardStateID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDCards", x => x.IDCardID);
                    table.ForeignKey(
                        name: "FK_IDCards_IDCardStates",
                        column: x => x.IDCardStateID,
                        principalTable: "IDCardStates",
                        principalColumn: "IDCardStateID");
                    table.ForeignKey(
                        name: "FK_IDCards_Individuals",
                        column: x => x.BusinessEntityID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "JobLeaveTypes",
                columns: table => new
                {
                    JobLeaveTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveDefinitionID = table.Column<int>(type: "int", nullable: true),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeID = table.Column<int>(type: "int", nullable: true),
                    RemainingDays = table.Column<int>(type: "int", nullable: true),
                    LastLeaveTakenDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    RenewedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsLeaveInfoUpdated = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EffectiveToDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AutoCorrectDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AutoCorrectRemark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLeaveTypes", x => x.JobLeaveTypeID);
                    table.CheckConstraint("CK_JobLeaveTypes_ExactlyOneLeaveSource", "(\r\n    ([LeaveTypeID] IS NOT NULL\r\n     AND [LeaveDefinitionID] IS NULL)\r\n    OR\r\n    ([LeaveTypeID] IS NULL\r\n     AND [LeaveDefinitionID] IS NOT NULL)\r\n)");
                    table.ForeignKey(
                        name: "FK_JobLeaveTypes_LeaveDefinitions",
                        column: x => x.LeaveDefinitionID,
                        principalTable: "LeaveDefinitions",
                        principalColumn: "LeaveDefinitionID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobPositionBasicSalaries",
                columns: table => new
                {
                    JobPositionBasicSalaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPoistionID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositionBasicSalaries_1", x => x.JobPositionBasicSalaryID);
                });

            migrationBuilder.CreateTable(
                name: "JobPositions",
                columns: table => new
                {
                    JobPositionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    JobPositionStateID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositions", x => x.JobPositionID);
                    table.ForeignKey(
                        name: "FK_JobPositions_JobPositionStates",
                        column: x => x.JobPositionStateID,
                        principalTable: "JobPositionStates",
                        principalColumn: "JobPositionStateID");
                });

            migrationBuilder.CreateTable(
                name: "JobPositionsRequests",
                columns: table => new
                {
                    JobPositionsRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPositionID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    JobPositionRemovalDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositionsRequests", x => x.JobPositionsRequestID);
                    table.ForeignKey(
                        name: "FK_JobPositionsRequests_JobPositions",
                        column: x => x.JobPositionID,
                        principalTable: "JobPositions",
                        principalColumn: "JobPositionID");
                });

            migrationBuilder.CreateTable(
                name: "JobRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPositionRequests", x => x.RequestID);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    JobID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    JobStateID = table.Column<int>(type: "int", nullable: false),
                    JobTypeID = table.Column<int>(type: "int", nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TerminatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    BasicSalary = table.Column<decimal>(type: "decimal(38,8)", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    SAPNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.JobID);
                    table.ForeignKey(
                        name: "FK_Jobs_Individuals",
                        column: x => x.IndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Jobs_JobStates_JobStateID",
                        column: x => x.JobStateID,
                        principalTable: "JobStates",
                        principalColumn: "JobStateID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "JobSAPExceptions",
                columns: table => new
                {
                    SAPExceptionID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSAPExceptions", x => x.SAPExceptionID);
                    table.ForeignKey(
                        name: "FK_JobSAPExceptions_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                });

            migrationBuilder.CreateTable(
                name: "JobTypes",
                columns: table => new
                {
                    JobTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TypeNameDhivehi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CSCJobTypePrimaryKey = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTypes", x => x.JobTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveConfigurableValues",
                columns: table => new
                {
                    LeaveConfigurableValueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveConfigurableValueTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    JobTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveConfigurableValues", x => x.LeaveConfigurableValueID);
                    table.ForeignKey(
                        name: "FK_LeaveConfigurableValues_JobTypes",
                        column: x => x.JobTypeID,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID");
                    table.ForeignKey(
                        name: "FK_LeaveConfigurableValues_LeaveConfigurableValueTypes",
                        column: x => x.LeaveConfigurableValueTypeID,
                        principalTable: "LeaveConfigurableValueTypes",
                        principalColumn: "LeaveConfigurableValueTypeID");
                    table.ForeignKey(
                        name: "FK_LeaveConfigurableValues_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollConfigurableValues",
                columns: table => new
                {
                    PayrollConfigurableValueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollConfigurableValueTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    JobTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollConfigurableValues", x => x.PayrollConfigurableValueID);
                    table.ForeignKey(
                        name: "FK_PayrollConfigurableValues_JobTypes",
                        column: x => x.JobTypeID,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID");
                    table.ForeignKey(
                        name: "FK_PayrollConfigurableValues_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_PayrollConfigurableValues_PayrollConfigurableValueTypes",
                        column: x => x.PayrollConfigurableValueTypeID,
                        principalTable: "PayrollConfigurableValueTypes",
                        principalColumn: "PayrollConfigurableValueTypeID");
                });

            migrationBuilder.CreateTable(
                name: "JobTypesExceptions",
                columns: table => new
                {
                    SAPExceptionID = table.Column<int>(type: "int", nullable: false),
                    JobTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypesExceptions", x => x.SAPExceptionID);
                    table.ForeignKey(
                        name: "FK_JobTypesExceptions_JobTypes",
                        column: x => x.JobTypeID,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID");
                });

            migrationBuilder.CreateTable(
                name: "JobWorkTemplateAssignments",
                columns: table => new
                {
                    JobWorkTemplateAssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    WorkTemplateID = table.Column<int>(type: "int", nullable: false),
                    ScheduledStartTime = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkTemplateAssignments", x => x.JobWorkTemplateAssignmentID);
                    table.ForeignKey(
                        name: "FK_JobWorkTemplateAssignments_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobWorkTemplates",
                columns: table => new
                {
                    JobWorkTemplateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    WorkTemplateId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    Monday = table.Column<bool>(type: "bit", nullable: false),
                    Tuesday = table.Column<bool>(type: "bit", nullable: false),
                    Wednesday = table.Column<bool>(type: "bit", nullable: false),
                    Thursday = table.Column<bool>(type: "bit", nullable: false),
                    Friday = table.Column<bool>(type: "bit", nullable: false),
                    Saturday = table.Column<bool>(type: "bit", nullable: false),
                    Sunday = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkTemplates", x => x.JobWorkTemplateId);
                    table.ForeignKey(
                        name: "FK_JobWorkTemplates_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyHolders",
                columns: table => new
                {
                    KeyHolderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyHolder", x => x.KeyHolderID);
                });

            migrationBuilder.CreateTable(
                name: "KPIDocuments",
                columns: table => new
                {
                    KPIDocumentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KPIID = table.Column<int>(type: "int", nullable: false),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    LinkedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LinkedByUserID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIDocuments", x => x.KPIDocumentID);
                    table.ForeignKey(
                        name: "FK_KPIDocuments_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveChangeRequests",
                columns: table => new
                {
                    LeaveChangeRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    LeaveChangeRequestTypeID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveChangeRequests", x => x.LeaveChangeRequestID);
                    table.ForeignKey(
                        name: "FK_LeaveChangeRequests_LeaveChangeRequestTypes",
                        column: x => x.LeaveChangeRequestTypeID,
                        principalTable: "LeaveChangeRequestTypes",
                        principalColumn: "LeaveChangeRequestTypeID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveDocuments",
                columns: table => new
                {
                    LeaveDocumentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    LinkedByUserID = table.Column<int>(type: "int", nullable: false),
                    LinkedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveDocuments", x => x.LeaveDocumentID);
                    table.ForeignKey(
                        name: "FK_LeaveDocuments_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveForms",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    ReportedDateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveForms_1", x => x.LeaveID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveFrameworkConfigurations",
                columns: table => new
                {
                    LeaveFrameworkConfigurationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    FrameworkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MigrationState = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveFrameworkConfigurations", x => x.LeaveFrameworkConfigurationID);
                    table.ForeignKey(
                        name: "FK_LeaveFrameworkConfigurations_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveReasons",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LeaveTypeReasonTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveReasons", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_LeaveReasons_LeaveTypeReasonTypes",
                        column: x => x.LeaveTypeReasonTypeID,
                        principalTable: "LeaveTypeReasonTypes",
                        principalColumn: "LeaveTypeReasonTypeID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.LeaveID);
                });

            migrationBuilder.CreateTable(
                name: "Leaves",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    ChitNo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LeaveTypeID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    LeaveStateID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaves", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_Leaves_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_Leaves_LeaveStates",
                        column: x => x.LeaveStateID,
                        principalTable: "LeaveStates",
                        principalColumn: "LeaveStateID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveWorkHandOvers",
                columns: table => new
                {
                    LeaveWorkHandOverID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveWorkHandOvers_1", x => x.LeaveWorkHandOverID);
                    table.ForeignKey(
                        name: "FK_LeaveWorkHandOvers_Jobs1",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_LeaveWorkHandOvers_Leaves1",
                        column: x => x.LeaveID,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveWorkHandOverTasks",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    TaskDetails = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveWorkHandOverTasks", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_LeaveWorkHandOverTasks_Leaves",
                        column: x => x.LeaveID,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID");
                });

            migrationBuilder.CreateTable(
                name: "OfficialTripDetails",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsLocal = table.Column<bool>(type: "bit", nullable: false),
                    FocalPointContactNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FocalPointEmail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FocalPointAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialTripDetails", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_OfficialTripDetails_Leaves",
                        column: x => x.LeaveID,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID");
                    table.ForeignKey(
                        name: "FK_OfficialTripDetails_OfficialTripTypes",
                        column: x => x.TypeID,
                        principalTable: "OfficialTripTypes",
                        principalColumn: "OfficialTripTypeID");
                });

            migrationBuilder.CreateTable(
                name: "LeavesBulkUploadedDocuments",
                columns: table => new
                {
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserID = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DocumentStateID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavesBulkUploadedDocuments", x => x.DocumentID);
                    table.ForeignKey(
                        name: "FK_LeavesBulkUploadedDocuments_DocumentStates",
                        column: x => x.DocumentStateID,
                        principalTable: "DocumentStates",
                        principalColumn: "DocumentStateID");
                    table.ForeignKey(
                        name: "FK_LeavesBulkUploadedDocuments_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveSetLeaveTypes",
                columns: table => new
                {
                    LeaveSetLeaveTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveSetID = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveSetLeaveTypes", x => x.LeaveSetLeaveTypeID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveSets",
                columns: table => new
                {
                    LeaveSetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveSets", x => x.LeaveSetID);
                    table.ForeignKey(
                        name: "FK_LeaveSets_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveSpendingLocations",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    IslandID = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveSpendingLocations_1", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_LeaveSpendingLocations_Countries",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_LeaveSpendingLocations_Islands",
                        column: x => x.IslandID,
                        principalTable: "Islands",
                        principalColumn: "IslandID");
                    table.ForeignKey(
                        name: "FK_LeaveSpendingLocations_Leaves",
                        column: x => x.LeaveID,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    LeaveTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    IncludeHolidays = table.Column<bool>(type: "bit", nullable: false),
                    IncludePay = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    IsLocationRequired = table.Column<bool>(type: "bit", nullable: false),
                    ServiceDurationMonths = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    RequestTypeID = table.Column<int>(type: "int", nullable: true),
                    IsSystemType = table.Column<bool>(type: "bit", nullable: false),
                    IsRenewed = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsStaffWideAvailable = table.Column<bool>(type: "bit", nullable: false),
                    PayPercentage = table.Column<double>(type: "float", nullable: true),
                    StartInMonth = table.Column<int>(type: "int", nullable: false),
                    RepeatedEveryInMonth = table.Column<int>(type: "int", nullable: false),
                    MigratedLeaveDefinitionID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.LeaveTypeID);
                    table.ForeignKey(
                        name: "FK_LeaveTypes_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_LeaveTypes_RequestTypes",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationTypeID = table.Column<int>(type: "int", nullable: false),
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.LocationID);
                    table.ForeignKey(
                        name: "FK_Locations_Countries",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_Locations_LocationTypes",
                        column: x => x.LocationTypeID,
                        principalTable: "LocationTypes",
                        principalColumn: "LocationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "OfficialTripLocations",
                columns: table => new
                {
                    OfficialTripLocationsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialTripLeaveLocations", x => x.OfficialTripLocationsID);
                    table.ForeignKey(
                        name: "FK_OfficialTripLocations_Locations",
                        column: x => x.LocationID,
                        principalTable: "Locations",
                        principalColumn: "LocationID");
                    table.ForeignKey(
                        name: "FK_OfficialTripLocations_OfficialTripDetails",
                        column: x => x.LeaveID,
                        principalTable: "OfficialTripDetails",
                        principalColumn: "LeaveID");
                });

            migrationBuilder.CreateTable(
                name: "Months",
                columns: table => new
                {
                    MonthID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnglishName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    DhivehiName = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    MonthOrder = table.Column<int>(type: "int", nullable: false),
                    CalendarTypeID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Months", x => x.MonthID);
                    table.ForeignKey(
                        name: "FK_Months_CalendarVariants",
                        column: x => x.CalendarTypeID,
                        principalTable: "CalendarTypes",
                        principalColumn: "CalendarTypeID");
                });

            migrationBuilder.CreateTable(
                name: "NavigationLinkFacilityTypes",
                columns: table => new
                {
                    NavigationLinkFacilityTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NavigationLinkID = table.Column<int>(type: "int", nullable: false),
                    FacilityTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationLinkFacilityTypes", x => x.NavigationLinkFacilityTypeID);
                    table.ForeignKey(
                        name: "FK_NavigationLinkFacilityTypes_FacilityRegistrationTypes",
                        column: x => x.FacilityTypeID,
                        principalTable: "FacilityRegistrationTypes",
                        principalColumn: "FacilityRegistrationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "NavigationLinks",
                columns: table => new
                {
                    NavigationLinkID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LinkName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ParentNavigationLinkID = table.Column<int>(type: "int", nullable: true),
                    URL = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: true),
                    RoleID = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    LinkTypeID = table.Column<int>(type: "int", nullable: false),
                    ContextID = table.Column<int>(type: "int", nullable: false),
                    ModuleID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LinkNameDhivehiName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationLinks", x => x.NavigationLinkID);
                    table.ForeignKey(
                        name: "FK_NavigationLinks_Context",
                        column: x => x.ContextID,
                        principalTable: "Context",
                        principalColumn: "ContextID");
                    table.ForeignKey(
                        name: "FK_NavigationLinks_LinkTypes",
                        column: x => x.LinkTypeID,
                        principalTable: "LinkTypes",
                        principalColumn: "LinkTypeID");
                    table.ForeignKey(
                        name: "FK_NavigationLinks_Modules",
                        column: x => x.ModuleID,
                        principalTable: "Modules",
                        principalColumn: "ModuleID");
                    table.ForeignKey(
                        name: "FK_NavigationLinks_NavigationLinks",
                        column: x => x.ParentNavigationLinkID,
                        principalTable: "NavigationLinks",
                        principalColumn: "NavigationLinkID");
                });

            migrationBuilder.CreateTable(
                name: "NoPayLeaves",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    NoPayLeaveTypeID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoPayLeaves_1", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_NoPayLeaves_Leaves",
                        column: x => x.LeaveID,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID");
                    table.ForeignKey(
                        name: "FK_NoPayLeaves_NoPayLeaveTypes",
                        column: x => x.NoPayLeaveTypeID,
                        principalTable: "NoPayLeaveTypes",
                        principalColumn: "NoPayLeaveTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    NoteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "text", nullable: false),
                    NoteReasonID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.NoteID);
                    table.ForeignKey(
                        name: "FK_Notes_NoteReasons1",
                        column: x => x.NoteReasonID,
                        principalTable: "NoteReasons",
                        principalColumn: "NoteReasonID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineAddresses",
                columns: table => new
                {
                    OnlineLocationID = table.Column<int>(type: "int", nullable: false),
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    IslandID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineAddresses", x => x.OnlineLocationID);
                    table.ForeignKey(
                        name: "FK_OnlineAddresses_Countries",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_OnlineAddresses_Islands",
                        column: x => x.IslandID,
                        principalTable: "Islands",
                        principalColumn: "IslandID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineAddressInstances",
                columns: table => new
                {
                    OnlineLocationID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AddressInstanceTypeID = table.Column<int>(type: "int", nullable: true),
                    OnlineAddressBaseID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineAddressInstances", x => x.OnlineLocationID);
                    table.ForeignKey(
                        name: "FK_OnlineAddressInstances_AddressInstanceTypes",
                        column: x => x.AddressInstanceTypeID,
                        principalTable: "AddressInstanceTypes",
                        principalColumn: "AddressInstanceTypeID");
                    table.ForeignKey(
                        name: "FK_OnlineAddressInstances_OnlineAddressBases",
                        column: x => x.OnlineAddressBaseID,
                        principalTable: "OnlineAddressBases",
                        principalColumn: "OnlineAddressBaseID");
                    table.ForeignKey(
                        name: "FK_OnlineAddressInstances_OnlineAddresses",
                        column: x => x.OnlineLocationID,
                        principalTable: "OnlineAddresses",
                        principalColumn: "OnlineLocationID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineAreas",
                columns: table => new
                {
                    OnlineLocationID = table.Column<int>(type: "int", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    WardID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineAreas", x => x.OnlineLocationID);
                    table.ForeignKey(
                        name: "FK_OnlineAreas_OnlineAddresses",
                        column: x => x.OnlineLocationID,
                        principalTable: "OnlineAddresses",
                        principalColumn: "OnlineLocationID");
                    table.ForeignKey(
                        name: "FK_OnlineAreas_Wards",
                        column: x => x.WardID,
                        principalTable: "Wards",
                        principalColumn: "WardID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineDhafthar",
                columns: table => new
                {
                    OnlineLocationID = table.Column<int>(type: "int", nullable: false),
                    DhaftharNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineDhafthar", x => x.OnlineLocationID);
                    table.ForeignKey(
                        name: "FK_OnlineDhafthar_OnlineAddresses",
                        column: x => x.OnlineLocationID,
                        principalTable: "OnlineAddresses",
                        principalColumn: "OnlineLocationID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineBusinessEntityLocations",
                columns: table => new
                {
                    OnlineBusinessEntityLocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    OnlineLocationID = table.Column<int>(type: "int", nullable: false),
                    BusinessEntityLocationTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineBusinessEntityLocations", x => x.OnlineBusinessEntityLocationID);
                    table.ForeignKey(
                        name: "FK_OnlineBusinessEntityLocations_BusinessEntities",
                        column: x => x.BusinessEntityID,
                        principalTable: "BusinessEntities",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_OnlineBusinessEntityLocations_BusinessEntityLocationTypes",
                        column: x => x.BusinessEntityLocationTypeID,
                        principalTable: "BusinessEntityLocationTypes",
                        principalColumn: "BusinessEntityLocationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineLocations",
                columns: table => new
                {
                    OnlineLocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationTypeID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineLocations", x => x.OnlineLocationID);
                    table.ForeignKey(
                        name: "FK_OnlineLocations_LocationTypes",
                        column: x => x.LocationTypeID,
                        principalTable: "LocationTypes",
                        principalColumn: "LocationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "OnlineSignInOrganisations",
                columns: table => new
                {
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    IsEnaled = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineSignInOrganisations", x => x.OrganisationID);
                    table.ForeignKey(
                        name: "FK_OnlineSignInOrganisations_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationLogActionID = table.Column<int>(type: "int", nullable: false),
                    CreatedByIndividualID = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    CreatedByContextID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedByIndividualID = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserID = table.Column<int>(type: "int", nullable: true),
                    CreatedByIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedByIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    UpdatedContextID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserOrganisationID = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserOrganisationID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.OperationLogID);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Context",
                        column: x => x.UpdatedContextID,
                        principalTable: "Context",
                        principalColumn: "ContextID");
                    table.ForeignKey(
                        name: "FK_OperationLogs_Individuals",
                        column: x => x.CreatedByIndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_OperationLogs_OperationLogActionTypes",
                        column: x => x.OperationLogActionID,
                        principalTable: "OperationLogActionTypes",
                        principalColumn: "OperationLogActionTypes");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationDeductionTypes",
                columns: table => new
                {
                    OrganisationDeductionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeductionTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationDeductionTypes", x => x.OrganisationDeductionTypeID);
                    table.ForeignKey(
                        name: "FK_OrganisationDeductionTypes_DeductionTypes",
                        column: x => x.DeductionTypeID,
                        principalTable: "DeductionTypes",
                        principalColumn: "DeductionTypeID");
                    table.ForeignKey(
                        name: "FK_OrganisationDeductionTypes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationDeductionTypes_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationPayrollPeriods",
                columns: table => new
                {
                    OrganisationPayrollPeriodID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    StartDay = table.Column<int>(type: "int", nullable: false),
                    EndDay = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationPayrollPeriods", x => x.OrganisationPayrollPeriodID);
                    table.ForeignKey(
                        name: "FK_OrganisationPayrollPeriods_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationPayrollPeriods_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructure",
                columns: table => new
                {
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureTypeID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OrganisationStructureStateID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructure", x => x.OrganisationStructureID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructure_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructure_OrganisationStructureStates",
                        column: x => x.OrganisationStructureStateID,
                        principalTable: "OrganisationStructureStates",
                        principalColumn: "OrganisationStructureStateID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructure_OrganisationStructureTypes",
                        column: x => x.OrganisationStructureTypeID,
                        principalTable: "OrganisationStructureTypes",
                        principalColumn: "OrganisationStructureTypeID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructure_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationWorkPlanningSettings",
                columns: table => new
                {
                    OrganisationWorkPlanningSettingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    PlanningProviderID = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationWorkPlanningSettings", x => x.OrganisationWorkPlanningSettingID);
                    table.ForeignKey(
                        name: "FK_OrganisationWorkPlanningSettings_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationWorkPlanningSettings_Organisation",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationWorkPlanningSettings_Provider",
                        column: x => x.PlanningProviderID,
                        principalTable: "PlanningProviders",
                        principalColumn: "PlanningProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Passports",
                columns: table => new
                {
                    PassportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    PassportStateID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passports", x => x.PassportID);
                    table.ForeignKey(
                        name: "FK_Passports_Countries",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_Passports_Individuals",
                        column: x => x.BusinessEntityID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Passports_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Passports_PassportStates",
                        column: x => x.PassportStateID,
                        principalTable: "PassportStates",
                        principalColumn: "PassportStateID");
                });

            migrationBuilder.CreateTable(
                name: "PercentageVariableDeductionAmounts",
                columns: table => new
                {
                    PercentageVariableDeductionAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeductionTypeID = table.Column<int>(type: "int", nullable: false),
                    StartAmount = table.Column<double>(type: "float", nullable: false),
                    EndAmount = table.Column<double>(type: "float", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PercentageVariableDeductionAmounts", x => x.PercentageVariableDeductionAmountID);
                    table.ForeignKey(
                        name: "FK_PercentageVariableDeductionAmounts_DeductionTypes",
                        column: x => x.DeductionTypeID,
                        principalTable: "DeductionTypes",
                        principalColumn: "DeductionTypeID");
                    table.ForeignKey(
                        name: "FK_PercentageVariableDeductionAmounts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "PositionClassifications",
                columns: table => new
                {
                    PositionClassificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CSCClassificationPimaryKey = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionClassifications", x => x.PositionClassificationID);
                    table.ForeignKey(
                        name: "FK_PositionClassifications_OperationLogs",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_PositionClassifications_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "PositionRanks",
                columns: table => new
                {
                    PositionRankID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    NameDhivehi = table.Column<string>(type: "nvarchar(550)", maxLength: 550, nullable: true),
                    CSCRankPimaryKey = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionRanks", x => x.PositionRankID);
                    table.ForeignKey(
                        name: "FK_PositionRanks_OperationLogs",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_PositionRanks_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ParentRoleID = table.Column<int>(type: "int", nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: true),
                    ContextID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    ModuleID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                    table.ForeignKey(
                        name: "FK_Roles_Context",
                        column: x => x.ContextID,
                        principalTable: "Context",
                        principalColumn: "ContextID");
                    table.ForeignKey(
                        name: "FK_Roles_Module",
                        column: x => x.ModuleID,
                        principalTable: "Modules",
                        principalColumn: "ModuleID");
                    table.ForeignKey(
                        name: "FK_Roles_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    ScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    ScheduleTypeID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsRamazan = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.ScheduleID);
                    table.ForeignKey(
                        name: "FK_Schedules_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Schedules_ScheduleTypes",
                        column: x => x.ScheduleTypeID,
                        principalTable: "ScheduleTypes",
                        principalColumn: "ScheduleTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    ServiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceTypeID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceStateID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ServiceID);
                    table.ForeignKey(
                        name: "FK_Services_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Services_ServiceStates",
                        column: x => x.ServiceStateID,
                        principalTable: "ServiceStates",
                        principalColumn: "ServiceStateID");
                    table.ForeignKey(
                        name: "FK_Services_ServiceTypes",
                        column: x => x.ServiceTypeID,
                        principalTable: "ServiceTypes",
                        principalColumn: "ServiceTypeID");
                });

            migrationBuilder.CreateTable(
                name: "SickLeaveForms",
                columns: table => new
                {
                    LeaveID = table.Column<int>(type: "int", nullable: false),
                    ReportedDateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SickLeaveForms_1", x => x.LeaveID);
                    table.ForeignKey(
                        name: "FK_SickLeaveForms_Leaves",
                        column: x => x.LeaveID,
                        principalTable: "Leaves",
                        principalColumn: "LeaveID");
                    table.ForeignKey(
                        name: "FK_SickLeaveForms_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "SpecialDays",
                columns: table => new
                {
                    SpecialDayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CalenderID = table.Column<int>(type: "int", nullable: false),
                    CalenderDayTypeID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialDays", x => x.SpecialDayID);
                    table.ForeignKey(
                        name: "FK_SpecialDays_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs_2", x => x.IndividualID);
                    table.ForeignKey(
                        name: "FK_Staffs_Individuals1",
                        column: x => x.IndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Staffs_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    NameDhivehi = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsSuperVisor = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamID);
                    table.ForeignKey(
                        name: "FK_Teams_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Teams_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "UserOrganisations",
                columns: table => new
                {
                    UserOrganisationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganisations", x => x.UserOrganisationID);
                    table.ForeignKey(
                        name: "FK_UserOrganisations_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserOrganisations_Organisations",
                        column: x => x.BusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserPreferenceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SelectedLanguageID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    SelectedContextID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserPreferenceID);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Context",
                        column: x => x.SelectedContextID,
                        principalTable: "Context",
                        principalColumn: "ContextID");
                    table.ForeignKey(
                        name: "FK_UserPreferences_Languages",
                        column: x => x.SelectedLanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID");
                    table.ForeignKey(
                        name: "FK_UserPreferences_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "WorkingDays",
                columns: table => new
                {
                    WorkingDayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeekID = table.Column<int>(type: "int", nullable: false),
                    CalenderID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingDays", x => x.WorkingDayID);
                    table.ForeignKey(
                        name: "FK_WorkingDays_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "WorkTemplates",
                columns: table => new
                {
                    WorkTemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkTemplateTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    DefaultEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndsNextDay = table.Column<bool>(type: "bit", nullable: false),
                    DefaultGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    RequiresAttendance = table.Column<bool>(type: "bit", nullable: false),
                    RequiresCheckOut = table.Column<bool>(type: "bit", nullable: false),
                    IsRepeatable = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTemplates", x => x.WorkTemplateID);
                    table.ForeignKey(
                        name: "FK_WorkTemplates_OperationLogs_OperationLogID",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTemplates_Organisations_OrganisationBusinessEntityID",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTemplates_WorkTemplateTypes_WorkTemplateTypeID",
                        column: x => x.WorkTemplateTypeID,
                        principalTable: "WorkTemplateTypes",
                        principalColumn: "WorkTemplateTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationPayrollPeriodJobTypes",
                columns: table => new
                {
                    OrganisationPayrollPeriodJobTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    OrganisationPayrollPeriodID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationPayrollPeriodJobTypes", x => x.OrganisationPayrollPeriodJobTypeID);
                    table.ForeignKey(
                        name: "FK_OrganisationPayrollPeriodJobTypes_JobTypes",
                        column: x => x.JobTypeID,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID");
                    table.ForeignKey(
                        name: "FK_OrganisationPayrollPeriodJobTypes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationPayrollPeriodJobTypes_OrganisationPayrollPeriods",
                        column: x => x.OrganisationPayrollPeriodID,
                        principalTable: "OrganisationPayrollPeriods",
                        principalColumn: "OrganisationPayrollPeriodID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureCalendars",
                columns: table => new
                {
                    OrganisationStructureCalendarID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    CalenderID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsLinked = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureCalendars", x => x.OrganisationStructureCalendarID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureCalendars_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureCalenders_Calenders",
                        column: x => x.CalenderID,
                        principalTable: "Calendars",
                        principalColumn: "CalenderID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureCalenders_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureLocations",
                columns: table => new
                {
                    OrganisationStructureLocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationStructureLocationTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureLocations", x => x.OrganisationStructureLocationID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureLocations_Locations",
                        column: x => x.LocationID,
                        principalTable: "Locations",
                        principalColumn: "LocationID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureLocations_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureLocations_OrganisationStructureLocationTypes",
                        column: x => x.OrganisationStructureLocationTypeID,
                        principalTable: "OrganisationStructureLocationTypes",
                        principalColumn: "OrganisationStructureLocationTypeID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureRelations",
                columns: table => new
                {
                    OrganisationStructureRelationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    ParentOrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    ActiveFromDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ActiveToDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureRelations", x => x.OrganisationStructureRelationID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureRelations_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureRelations_OrganisationStructure1",
                        column: x => x.ParentOrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                });

            migrationBuilder.CreateTable(
                name: "SpecialDayTypes",
                columns: table => new
                {
                    SpecialDayTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayTypeID = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialDayTypes", x => x.SpecialDayTypeID);
                    table.ForeignKey(
                        name: "FK_SpecialDayTypes_DayTypes",
                        column: x => x.DayTypeID,
                        principalTable: "DayTypes",
                        principalColumn: "DayTypeID");
                    table.ForeignKey(
                        name: "FK_SpecialDayTypes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_SpecialDayTypes_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_SpecialDayTypes_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "WorkShifts",
                columns: table => new
                {
                    WorkShiftID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShifts", x => x.WorkShiftID);
                    table.ForeignKey(
                        name: "FK_WorkShifts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_WorkShifts_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_WorkShifts_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    PositionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionClassificationID = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DhivehiName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Descriptions = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CSCDesignationPrimaryKey = table.Column<int>(type: "int", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    PositionRankID = table.Column<int>(type: "int", nullable: true),
                    PositionTypeID = table.Column<int>(type: "int", nullable: true),
                    OrganisationID = table.Column<int>(type: "int", nullable: true),
                    IsSalaryGivenBasedOnPostion = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.PositionID);
                    table.ForeignKey(
                        name: "FK_Positions_OperationLogs",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Positions_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Positions_PositionClassifications",
                        column: x => x.PositionClassificationID,
                        principalTable: "PositionClassifications",
                        principalColumn: "PositionClassificationID");
                    table.ForeignKey(
                        name: "FK_Positions_PositionRanks1",
                        column: x => x.PositionRankID,
                        principalTable: "PositionRanks",
                        principalColumn: "PositionRankID");
                    table.ForeignKey(
                        name: "FK_Positions_PositionTypes",
                        column: x => x.PositionTypeID,
                        principalTable: "PositionTypes",
                        principalColumn: "PositionTypeID");
                });

            migrationBuilder.CreateTable(
                name: "RequestTypesStatesRequiredRoles",
                columns: table => new
                {
                    RequestTypeID = table.Column<int>(type: "int", nullable: false),
                    RequestStateID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    ShowAllRequests = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypesStatesRequiredRoles_1", x => new { x.RequestTypeID, x.RequestStateID, x.RoleID });
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRoles_RequestStates",
                        column: x => x.RequestStateID,
                        principalTable: "RequestStates",
                        principalColumn: "RequestStateID");
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRoles_RequestTypes",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRoles_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "RequestTypesStatesRequiredRolesForProcessing",
                columns: table => new
                {
                    RequestTypeID = table.Column<int>(type: "int", nullable: false),
                    RequestStateID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    ToRequestStateID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypesStatesRequiredRolesForProcessing_1", x => new { x.RequestTypeID, x.RequestStateID, x.RoleID });
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRolesForProcessing_RequestStates",
                        column: x => x.RequestStateID,
                        principalTable: "RequestStates",
                        principalColumn: "RequestStateID");
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRolesForProcessing_RequestStates1",
                        column: x => x.ToRequestStateID,
                        principalTable: "RequestStates",
                        principalColumn: "RequestStateID");
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRolesForProcessing_RequestTypes",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                    table.ForeignKey(
                        name: "FK_RequestTypesStatesRequiredRolesForProcessing_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureSchedules",
                columns: table => new
                {
                    OrganisationStructureScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    isLinked = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureSchedules", x => x.OrganisationStructureScheduleID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureSchedules_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureSchedules_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureSchedules_Schedules",
                        column: x => x.ScheduleID,
                        principalTable: "Schedules",
                        principalColumn: "ScheduleID");
                });

            migrationBuilder.CreateTable(
                name: "ScheduleElements",
                columns: table => new
                {
                    ScheduleElementsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    ElementID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleElements", x => x.ScheduleElementsID);
                    table.ForeignKey(
                        name: "FK_ScheduleElements_Elements",
                        column: x => x.ElementID,
                        principalTable: "Elements",
                        principalColumn: "ElementID");
                    table.ForeignKey(
                        name: "FK_ScheduleElements_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_ScheduleElements_Schedules1",
                        column: x => x.ScheduleID,
                        principalTable: "Schedules",
                        principalColumn: "ScheduleID");
                });

            migrationBuilder.CreateTable(
                name: "ScheduleOwners",
                columns: table => new
                {
                    ScheduleOwnerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    OwnerTypeID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleOwners", x => x.ScheduleOwnerID);
                    table.ForeignKey(
                        name: "FK_ScheduleOwners_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_ScheduleOwners_OwnerTypes",
                        column: x => x.OwnerTypeID,
                        principalTable: "OwnerTypes",
                        principalColumn: "OwnerTypeID");
                    table.ForeignKey(
                        name: "FK_ScheduleOwners_Schedules",
                        column: x => x.ScheduleID,
                        principalTable: "Schedules",
                        principalColumn: "ScheduleID");
                });

            migrationBuilder.CreateTable(
                name: "ShiftSchedules",
                columns: table => new
                {
                    ScheduleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSchedules", x => x.ScheduleID);
                    table.ForeignKey(
                        name: "FK_ShiftSchedules_Schedules",
                        column: x => x.ScheduleID,
                        principalTable: "Schedules",
                        principalColumn: "ScheduleID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceCheckListVerifications",
                columns: table => new
                {
                    ServiceCheckListVerificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckListItemID = table.Column<int>(type: "int", nullable: false),
                    IsVerificationSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    OprationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCheckListVerifications", x => x.ServiceCheckListVerificationID);
                    table.ForeignKey(
                        name: "FK_ServiceCheckListVerifications_CheckListItems",
                        column: x => x.CheckListItemID,
                        principalTable: "CheckListItems",
                        principalColumn: "CheckListItemID");
                    table.ForeignKey(
                        name: "FK_ServiceCheckListVerifications_OperationLogs",
                        column: x => x.OprationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_ServiceCheckListVerifications_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceDocuments",
                columns: table => new
                {
                    ServiceDocumentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    LinkedByUserID = table.Column<int>(type: "int", nullable: false),
                    LinkedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDocuments", x => x.ServiceDocumentID);
                    table.ForeignKey(
                        name: "FK_ServiceDocuments_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID");
                    table.ForeignKey(
                        name: "FK_ServiceDocuments_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_ServiceDocuments_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "SpecialDayShifts",
                columns: table => new
                {
                    SpecialDayShiftID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkShiftID = table.Column<int>(type: "int", nullable: false),
                    SpecialDayID = table.Column<int>(type: "int", nullable: false),
                    IsLinked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialDayShifts", x => x.SpecialDayShiftID);
                    table.ForeignKey(
                        name: "FK_SpecialDayShifts_SpecialDays",
                        column: x => x.SpecialDayID,
                        principalTable: "SpecialDays",
                        principalColumn: "SpecialDayID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureHeadIncharges",
                columns: table => new
                {
                    OrganisationStructureHeadInchargeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureHeadIncharges", x => x.OrganisationStructureHeadInchargeID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureHeadIncharges_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureHeadIncharges_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureHeadIncharges_Staffs",
                        column: x => x.IndividualID,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureSupervisors",
                columns: table => new
                {
                    OrganisationStructureSupervisorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    SupervisorStaffID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureSupervisors", x => x.OrganisationStructureSupervisorID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureSupervisors_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureSupervisors_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureSupervisors_Staffs",
                        column: x => x.SupervisorStaffID,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                });

            migrationBuilder.CreateTable(
                name: "StaffEnrollmentNumbers",
                columns: table => new
                {
                    StaffEnrollmentNumberID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    EnrollmentNumber = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffEnrollmentNumbers", x => x.StaffEnrollmentNumberID);
                    table.ForeignKey(
                        name: "FK_StaffEnrollmentNumbers_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffEnrollmentNumbers_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_StaffEnrollmentNumbers_Staffs",
                        column: x => x.IndividualID,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                });

            migrationBuilder.CreateTable(
                name: "TeamStaffs",
                columns: table => new
                {
                    TeamStaffsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamID = table.Column<int>(type: "int", nullable: false),
                    StaffID = table.Column<int>(type: "int", nullable: false),
                    IsSuperVisor = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStaffs", x => x.TeamStaffsID);
                    table.ForeignKey(
                        name: "FK_TeamStaffs_Staffs",
                        column: x => x.StaffID,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                    table.ForeignKey(
                        name: "FK_TeamStaffs_Teams",
                        column: x => x.TeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID");
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserGroupID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserOrganisationID = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ParentUserGroupID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.UserGroupID);
                    table.ForeignKey(
                        name: "FK_UserGroups_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserGroups_UserOrganisations",
                        column: x => x.UserOrganisationID,
                        principalTable: "UserOrganisations",
                        principalColumn: "UserOrganisationID");
                });

            migrationBuilder.CreateTable(
                name: "UserOrganisationRoles",
                columns: table => new
                {
                    UserOrganisationID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    UserOrganisationRoleID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganisationRoles", x => new { x.UserOrganisationID, x.RoleID });
                    table.ForeignKey(
                        name: "FK_UserOrganisationRoles_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserOrganisationRoles_Organisations_OrganisationBusinessEntityID",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_UserOrganisationRoles_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_UserOrganisationRoles_UserOrganisations",
                        column: x => x.UserOrganisationID,
                        principalTable: "UserOrganisations",
                        principalColumn: "UserOrganisationID");
                });

            migrationBuilder.CreateTable(
                name: "WorkingDayShifts",
                columns: table => new
                {
                    WorkingDayShiftID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkShiftID = table.Column<int>(type: "int", nullable: false),
                    WorkingDayID = table.Column<int>(type: "int", nullable: false),
                    IsLinked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingDayShifts", x => x.WorkingDayShiftID);
                    table.ForeignKey(
                        name: "FK_WorkingDayShifts_WorkingDays",
                        column: x => x.WorkingDayID,
                        principalTable: "WorkingDays",
                        principalColumn: "WorkingDayID");
                });

            migrationBuilder.CreateTable(
                name: "WorkTemplateSegments",
                columns: table => new
                {
                    WorkTemplateSegmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkTemplateID = table.Column<int>(type: "int", nullable: false),
                    WorkSegmentTypeID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    OffsetMinutes = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceBeforeMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceAfterMinutes = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAttendance = table.Column<bool>(type: "bit", nullable: false),
                    RequiresLocationValidation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresDeviceValidation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTemplateSegments", x => x.WorkTemplateSegmentID);
                    table.ForeignKey(
                        name: "FK_WorkTemplateSegments_WorkSegmentTypes_WorkSegmentTypeID",
                        column: x => x.WorkSegmentTypeID,
                        principalTable: "WorkSegmentTypes",
                        principalColumn: "WorkSegmentTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTemplateSegments_WorkTemplates_WorkTemplateID",
                        column: x => x.WorkTemplateID,
                        principalTable: "WorkTemplates",
                        principalColumn: "WorkTemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkShiftsBreakTimes",
                columns: table => new
                {
                    WorkShiftsBreakTimesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkShiftID = table.Column<int>(type: "int", nullable: false),
                    BreakTimeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShiftsBreakTimes", x => x.WorkShiftsBreakTimesID);
                    table.ForeignKey(
                        name: "FK_WorkShiftsBreakTimes_BreakTimes",
                        column: x => x.BreakTimeID,
                        principalTable: "BreakTimes",
                        principalColumn: "BreakTimeID");
                    table.ForeignKey(
                        name: "FK_WorkShiftsBreakTimes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_WorkShiftsBreakTimes_WorkShifts",
                        column: x => x.WorkShiftID,
                        principalTable: "WorkShifts",
                        principalColumn: "WorkShiftID");
                });

            migrationBuilder.CreateTable(
                name: "PositionBasicSalaries",
                columns: table => new
                {
                    PostionBasicSalaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionBasicSalaries", x => x.PostionBasicSalaryID);
                    table.ForeignKey(
                        name: "FK_PositionBasicSalaries_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_PositionBasicSalaries_Positions",
                        column: x => x.PositionID,
                        principalTable: "Positions",
                        principalColumn: "PositionID");
                });

            migrationBuilder.CreateTable(
                name: "ShiftScheduleDays",
                columns: table => new
                {
                    ShiftScheduleDayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: true),
                    WorkShiftID = table.Column<int>(type: "int", nullable: true),
                    SpecialDayTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftScheduleDays", x => x.ShiftScheduleDayID);
                    table.ForeignKey(
                        name: "FK_ShiftScheduleDays_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_ShiftScheduleDays_ShiftSchedules",
                        column: x => x.ScheduleID,
                        principalTable: "ShiftSchedules",
                        principalColumn: "ScheduleID");
                    table.ForeignKey(
                        name: "FK_ShiftScheduleDays_SpecialDayTypes",
                        column: x => x.SpecialDayTypeID,
                        principalTable: "SpecialDayTypes",
                        principalColumn: "SpecialDayTypeID");
                    table.ForeignKey(
                        name: "FK_ShiftScheduleDays_WorkShifts",
                        column: x => x.WorkShiftID,
                        principalTable: "WorkShifts",
                        principalColumn: "WorkShiftID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceDocumentVerifications",
                columns: table => new
                {
                    ServiceDocumentVerificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceDocumentID = table.Column<int>(type: "int", nullable: false),
                    IsVerifiedSuccessfully = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDocumentVerifications", x => x.ServiceDocumentVerificationID);
                    table.ForeignKey(
                        name: "FK_ServiceDocumentVerifications_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_ServiceDocumentVerifications_ServiceDocuments",
                        column: x => x.ServiceDocumentID,
                        principalTable: "ServiceDocuments",
                        principalColumn: "ServiceDocumentID");
                });

            migrationBuilder.CreateTable(
                name: "UserGroupRoles",
                columns: table => new
                {
                    UserGroupID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupRoles", x => new { x.UserGroupID, x.RoleID });
                    table.ForeignKey(
                        name: "FK_UserGroupRoles_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserGroupRoles_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_UserGroupRoles_UserGroups",
                        column: x => x.UserGroupID,
                        principalTable: "UserGroups",
                        principalColumn: "UserGroupID");
                });

            migrationBuilder.CreateTable(
                name: "UserOrganisationUserGroups",
                columns: table => new
                {
                    UserOrganisationID = table.Column<int>(type: "int", nullable: false),
                    UserGroupID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganisationUserGroups", x => new { x.UserOrganisationID, x.UserGroupID });
                    table.ForeignKey(
                        name: "FK_UserOrganisationUserGroups_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserOrganisationUserGroups_UserGroups",
                        column: x => x.UserGroupID,
                        principalTable: "UserGroups",
                        principalColumn: "UserGroupID");
                    table.ForeignKey(
                        name: "FK_UserOrganisationUserGroups_UserOrganisations",
                        column: x => x.UserOrganisationID,
                        principalTable: "UserOrganisations",
                        principalColumn: "UserOrganisationID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationalStructureSAPExceptions",
                columns: table => new
                {
                    SAPExceptionID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationalStructureSAPExceptions", x => x.SAPExceptionID);
                    table.ForeignKey(
                        name: "FK_OrganisationalStructureSAPExceptions_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationPayrollItemTypes",
                columns: table => new
                {
                    OrganisationPayrollItemTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationAllowanceTypes", x => x.OrganisationPayrollItemTypeID);
                    table.ForeignKey(
                        name: "FK_OrganisationAllowanceTypes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OrganisationAllowanceTypes_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationSAPExceptions",
                columns: table => new
                {
                    SAPExceptionID = table.Column<int>(type: "int", nullable: false),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationSAPExceptions", x => x.SAPExceptionID);
                    table.ForeignKey(
                        name: "FK_OrganisationSAPExceptions_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureDrafts",
                columns: table => new
                {
                    OrganisationStructureDraftID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: false),
                    OrganisationChartRequestID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganisationStructureTypeID = table.Column<int>(type: "int", nullable: false),
                    ParentOrganisationStructureID = table.Column<int>(type: "int", nullable: true),
                    OrganisationStructureStateID = table.Column<int>(type: "int", nullable: false),
                    HeadInChargeIndividualID = table.Column<int>(type: "int", nullable: true),
                    OrganisationStructureRequestTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationStructureChangeRequests", x => x.OrganisationStructureDraftID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureDrafts_OrganisationStructureTypes",
                        column: x => x.OrganisationStructureTypeID,
                        principalTable: "OrganisationStructureTypes",
                        principalColumn: "OrganisationStructureTypeID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureRequests_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureRequests_OrganisationStructureRequestTypes",
                        column: x => x.OrganisationStructureRequestTypeID,
                        principalTable: "OrganisationStructureRequestTypes",
                        principalColumn: "OrganisationStructureRequestTypeID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureRequests_OrganisationStructureStates",
                        column: x => x.OrganisationStructureStateID,
                        principalTable: "OrganisationStructureStates",
                        principalColumn: "OrganisationStructureStateID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureHistory",
                columns: table => new
                {
                    OrganisationStructureHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationStructureRelationID = table.Column<int>(type: "int", nullable: false),
                    OrganisationID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationCharts", x => x.OrganisationStructureHistoryID);
                    table.ForeignKey(
                        name: "FK_OrganisationStructureHistory_OrganisationStructureRelations",
                        column: x => x.OrganisationStructureRelationID,
                        principalTable: "OrganisationStructureRelations",
                        principalColumn: "OrganisationStructureRelationID");
                    table.ForeignKey(
                        name: "FK_OrganisationStructureHistory_Organisations",
                        column: x => x.OrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OrganisationStructureRequests",
                columns: table => new
                {
                    OrganisationStructureRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationChartRequests", x => x.OrganisationStructureRequestID);
                    table.ForeignKey(
                        name: "FK_OrganisationChartRequests_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "OT",
                columns: table => new
                {
                    OTID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OTRequestID = table.Column<int>(type: "int", nullable: false),
                    WorkDone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StartAttendanceLogID = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndAttendanceLogID = table.Column<int>(type: "int", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OT", x => x.OTID);
                    table.ForeignKey(
                        name: "FK_OT_AttendanceLogs",
                        column: x => x.StartAttendanceLogID,
                        principalTable: "AttendanceLogs",
                        principalColumn: "AttendanceLogID");
                    table.ForeignKey(
                        name: "FK_OT_AttendanceLogs1",
                        column: x => x.EndAttendanceLogID,
                        principalTable: "AttendanceLogs",
                        principalColumn: "AttendanceLogID");
                    table.ForeignKey(
                        name: "FK_OT_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "OtherSalaryRateDefinitions",
                columns: table => new
                {
                    OtherSalaryRateDefinitionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryRateDefinitionID = table.Column<int>(type: "int", nullable: false),
                    SalaryRateDefinitionForTypeID = table.Column<int>(type: "int", nullable: false),
                    NoOfDays = table.Column<int>(type: "int", nullable: false),
                    NoOfHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherSalaryRateDefinitions", x => x.OtherSalaryRateDefinitionID);
                    table.ForeignKey(
                        name: "FK_OtherSalaryRateDefinitions_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_OtherSalaryRateDefinitions_SalaryRateDefinitionForTypes",
                        column: x => x.SalaryRateDefinitionForTypeID,
                        principalTable: "SalaryRateDefinitionForTypes",
                        principalColumn: "SalaryRateDefinitionForTypeID");
                });

            migrationBuilder.CreateTable(
                name: "OTPreApprovals",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    OTTypeID = table.Column<int>(type: "int", nullable: false),
                    StaffJobID = table.Column<int>(type: "int", nullable: false),
                    SupervisorID = table.Column<int>(type: "int", nullable: false),
                    IsBulkDate = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Works = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTPreApprovals_1", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_OTPreApprovals_Jobs",
                        column: x => x.StaffJobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_OTPreApprovals_OTTypes",
                        column: x => x.OTTypeID,
                        principalTable: "OTTypes",
                        principalColumn: "OTTypeID");
                    table.ForeignKey(
                        name: "FK_OTPreApprovals_Staffs2",
                        column: x => x.SupervisorID,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                });

            migrationBuilder.CreateTable(
                name: "OTPreApprovalTimes",
                columns: table => new
                {
                    OTPreApprovalTimeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    DayTypeID = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DurationLimit = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTPreApprovalTimes", x => x.OTPreApprovalTimeID);
                    table.ForeignKey(
                        name: "FK_OTPreApprovalTimes_DayTypes",
                        column: x => x.DayTypeID,
                        principalTable: "DayTypes",
                        principalColumn: "DayTypeID");
                    table.ForeignKey(
                        name: "FK_OTPreApprovalTimes_OTPreApprovals",
                        column: x => x.RequestID,
                        principalTable: "OTPreApprovals",
                        principalColumn: "RequestID");
                    table.ForeignKey(
                        name: "FK_OTPreApprovalTimes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "OTRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsPlanned = table.Column<bool>(type: "bit", nullable: false),
                    OTPreApprovalRequestID = table.Column<int>(type: "int", nullable: true),
                    StaffJobID = table.Column<int>(type: "int", nullable: false),
                    OTTypeID = table.Column<int>(type: "int", nullable: false),
                    IsDocumentRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_OTRequests_Jobs",
                        column: x => x.StaffJobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_OTRequests_OTPreApprovals",
                        column: x => x.OTPreApprovalRequestID,
                        principalTable: "OTPreApprovals",
                        principalColumn: "RequestID");
                    table.ForeignKey(
                        name: "FK_OTRequests_OTTypes",
                        column: x => x.OTTypeID,
                        principalTable: "OTTypes",
                        principalColumn: "OTTypeID");
                });

            migrationBuilder.CreateTable(
                name: "OutOfOfficeRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureID = table.Column<int>(type: "int", nullable: true),
                    StaffID = table.Column<int>(type: "int", nullable: false),
                    SupervisorStaffId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpectedClockOutTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ExpectedClockInTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutOfOfficeRequestTypeID = table.Column<int>(type: "int", nullable: false),
                    ClockInAttendanceLogId = table.Column<int>(type: "int", nullable: true),
                    ClockOutAttendanceLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutOfOfficeRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_OutOfOfficeRequests_OrganisationStructure",
                        column: x => x.OrganisationStructureID,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID");
                    table.ForeignKey(
                        name: "FK_OutOfOfficeRequests_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_OutOfOfficeRequests_OutOfficeRequestTypes",
                        column: x => x.OutOfOfficeRequestTypeID,
                        principalTable: "OutOfficeRequestTypes",
                        principalColumn: "OutOfOfficeRequestTypeID");
                    table.ForeignKey(
                        name: "FK_OutOfOfficeRequests_Staffs",
                        column: x => x.StaffID,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                    table.ForeignKey(
                        name: "FK_OutOfOfficeRequests_Staffs1",
                        column: x => x.SupervisorStaffId,
                        principalTable: "Staffs",
                        principalColumn: "IndividualID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollCycleLinkedRequests",
                columns: table => new
                {
                    PayrollCycleLinkedRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollCycleRequestID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCycleLinkedRequests", x => x.PayrollCycleLinkedRequestID);
                    table.ForeignKey(
                        name: "FK_PayrollCycleLinkedRequests_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollCycles",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    OrganisationPayrollPeriodID = table.Column<int>(type: "int", nullable: false),
                    PayrollMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleState = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCycles", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_PayrollCycles_OrganisationPayrollPeriods",
                        column: x => x.OrganisationPayrollPeriodID,
                        principalTable: "OrganisationPayrollPeriods",
                        principalColumn: "OrganisationPayrollPeriodID");
                });

            migrationBuilder.CreateTable(
                name: "StaffSalaries",
                columns: table => new
                {
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    PayrollCycleRequestID = table.Column<int>(type: "int", nullable: false),
                    DaysAttended = table.Column<int>(type: "int", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: true),
                    Comments = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffSalaries", x => x.StaffSalaryID);
                    table.ForeignKey(
                        name: "FK_StaffSalaries_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_StaffSalaries_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffSalaries_PayrollCycles1",
                        column: x => x.PayrollCycleRequestID,
                        principalTable: "PayrollCycles",
                        principalColumn: "RequestID");
                });

            migrationBuilder.CreateTable(
                name: "StaffDailyAttendanceSummeries",
                columns: table => new
                {
                    StaffDailyAttendanceSummeryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    SalaryToPay = table.Column<bool>(type: "bit", nullable: false),
                    ReportingTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    SignedInTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    LateDuration = table.Column<TimeOnly>(type: "time", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    SalaryToPayReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsAbsent = table.Column<bool>(type: "bit", nullable: true),
                    AbsentFine = table.Column<decimal>(type: "decimal(38,8)", nullable: true),
                    LateFine = table.Column<decimal>(type: "decimal(38,8)", nullable: true),
                    SalaryAmount = table.Column<decimal>(type: "decimal(38,8)", nullable: true),
                    JobPositionID = table.Column<int>(type: "int", nullable: true),
                    OTDuration = table.Column<TimeOnly>(type: "time", nullable: true),
                    OTAmount = table.Column<decimal>(type: "decimal(38,8)", nullable: true),
                    DurationToWork = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: true),
                    Comments = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: true),
                    BreakOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakIn = table.Column<TimeOnly>(type: "time", nullable: true),
                    ScheduledOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    ClockOut = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDailyAttendanceSummeries", x => x.StaffDailyAttendanceSummeryID);
                    table.ForeignKey(
                        name: "FK_StaffDailyAttendanceSummeries_JobPositions",
                        column: x => x.JobPositionID,
                        principalTable: "JobPositions",
                        principalColumn: "JobPositionID");
                    table.ForeignKey(
                        name: "FK_StaffDailyAttendanceSummeries_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffDailyAttendanceSummeries_StaffSalaries",
                        column: x => x.StaffSalaryID,
                        principalTable: "StaffSalaries",
                        principalColumn: "StaffSalaryID");
                });

            migrationBuilder.CreateTable(
                name: "StaffDailyAttandanceSummaryIssues",
                columns: table => new
                {
                    StaffDailyAttandanceSummaryIssueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffDailyAttendanceSummeryID = table.Column<int>(type: "int", nullable: false),
                    StaffDailyAttandanceSummaryIssueTypeId = table.Column<int>(type: "int", nullable: false),
                    ReferancePrimaryKey = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    CheckedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    PayrollCycleProcessingStateID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDailyAttandanceSummaryIssues", x => x.StaffDailyAttandanceSummaryIssueID);
                    table.ForeignKey(
                        name: "FK_StaffDailyAttandanceSummaryIssues_PayrollCycleProcessingStates",
                        column: x => x.PayrollCycleProcessingStateID,
                        principalTable: "PayrollCycleProcessingStates",
                        principalColumn: "PayrollCycleProcessingStateID");
                    table.ForeignKey(
                        name: "FK_StaffDailyAttandanceSummaryIssues_StaffDailyAttandanceSummaryIssueTypes",
                        column: x => x.StaffDailyAttandanceSummaryIssueTypeId,
                        principalTable: "StaffDailyAttandanceSummaryIssueTypes",
                        principalColumn: "StaffDailyAttandanceSummaryIssueTypeId");
                    table.ForeignKey(
                        name: "FK_StaffDailyAttandanceSummaryIssues_StaffDailyAttendanceSummeries",
                        column: x => x.StaffDailyAttendanceSummeryID,
                        principalTable: "StaffDailyAttendanceSummeries",
                        principalColumn: "StaffDailyAttendanceSummeryID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemDailySAPSheetDetails",
                columns: table => new
                {
                    PayrollItemDailySAPSheetDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Days = table.Column<int>(type: "int", nullable: true),
                    Hours = table.Column<int>(type: "int", nullable: true),
                    Minutes = table.Column<int>(type: "int", nullable: true),
                    Rate = table.Column<double>(type: "float", nullable: true),
                    Weekend = table.Column<bool>(type: "bit", nullable: true),
                    Ramazan = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollItemDailySAPSheetDetails", x => x.PayrollItemDailySAPSheetDetailID);
                    table.ForeignKey(
                        name: "FK_PayrollItemDailySAPSheetDetails_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_PayrollItemDailySAPSheetDetails_StaffSalaries",
                        column: x => x.StaffSalaryID,
                        principalTable: "StaffSalaries",
                        principalColumn: "StaffSalaryID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemProcessingFlows",
                columns: table => new
                {
                    PayrollItemProcessingFlowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeProcessingCodeID = table.Column<int>(type: "int", nullable: true),
                    Operator = table.Column<int>(type: "int", nullable: true),
                    ElementOrder = table.Column<int>(type: "int", nullable: false),
                    IsEndOfFlow = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    ParentPayrollItemTypeProcessingCodeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollItemProcessingFlows", x => x.PayrollItemProcessingFlowID);
                    table.ForeignKey(
                        name: "FK_PayrollItemProcessingFlows_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemsProcessingWhiteLists",
                columns: table => new
                {
                    PayrollItemsProcessingWhiteListID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    JobTypeID = table.Column<int>(type: "int", nullable: false),
                    OwnerOrganisationID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollItemsProcessingWhiteLists", x => x.PayrollItemsProcessingWhiteListID);
                    table.ForeignKey(
                        name: "FK_PayrollItemsProcessingWhiteLists_JobTypes",
                        column: x => x.JobTypeID,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID");
                    table.ForeignKey(
                        name: "FK_PayrollItemsProcessingWhiteLists_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_PayrollItemsProcessingWhiteLists_Organisations",
                        column: x => x.OwnerOrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemTypeAmounts",
                columns: table => new
                {
                    PayrollItemTypeAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmountType", x => x.PayrollItemTypeAmountID);
                    table.ForeignKey(
                        name: "FK_AllowanceAmounts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemTypeDependencyAmounts",
                columns: table => new
                {
                    PayrollItemTypeDependencyAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    DependencyID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowanceDependencyAmount", x => x.PayrollItemTypeDependencyAmountID);
                    table.ForeignKey(
                        name: "FK_AllowanceDependencyAmounts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemTypeDependencyTimePeriodAmounts",
                columns: table => new
                {
                    PayrollItemTypeDependencyTimePeriodAmounts = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowanceDependencyTimePeriodAmounts_1", x => x.PayrollItemTypeDependencyTimePeriodAmounts);
                    table.ForeignKey(
                        name: "FK_AllowanceDependencyTimePeriodAmounts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemTypeProcessingCodes",
                columns: table => new
                {
                    PayrollItemTypeProcessingCodeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    AdditionOrDeductionTypeID = table.Column<int>(type: "int", nullable: false),
                    ProcessingOrder = table.Column<int>(type: "int", nullable: false),
                    SAPCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsSystemType = table.Column<bool>(type: "bit", nullable: false),
                    OwnerOrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: true),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollItemTypeProcessingCodes", x => x.PayrollItemTypeProcessingCodeID);
                    table.ForeignKey(
                        name: "FK_PayrollItemTypeProcessingCodes_AdditionOrDeductionTypes",
                        column: x => x.AdditionOrDeductionTypeID,
                        principalTable: "AdditionOrDeductionTypes",
                        principalColumn: "AdditionOrDeductionTypeID");
                    table.ForeignKey(
                        name: "FK_PayrollItemTypeProcessingCodes_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_PayrollItemTypeProcessingCodes_Organisations",
                        column: x => x.OwnerOrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "PayrollItemTypes",
                columns: table => new
                {
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEnglish = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameDhivehi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RecurrenceTypeID = table.Column<int>(type: "int", nullable: true),
                    ReccurencePattern = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    HasDependency = table.Column<bool>(type: "bit", nullable: true),
                    DependencyTypeID = table.Column<int>(type: "int", nullable: true),
                    AmountTypeID = table.Column<int>(type: "int", nullable: true),
                    IsAttendanceDependent = table.Column<bool>(type: "bit", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: true),
                    HasMin = table.Column<bool>(type: "bit", nullable: true),
                    HasMax = table.Column<bool>(type: "bit", nullable: true),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    DerivedOnTypeID = table.Column<int>(type: "int", nullable: true),
                    CurrencyTypeID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    IsApplicableToAll = table.Column<bool>(type: "bit", nullable: true),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: true),
                    ownerOrganisationID = table.Column<int>(type: "int", nullable: true),
                    IsAmountGivenWhenAssigning = table.Column<bool>(type: "bit", nullable: true),
                    HasSchedule = table.Column<bool>(type: "bit", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AdditionOrDeductionTypeID = table.Column<int>(type: "int", nullable: true),
                    IsSpecialType = table.Column<bool>(type: "bit", nullable: true),
                    SAPCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    AttendanceDependentTypeID = table.Column<int>(type: "int", nullable: true),
                    SAPSheetID = table.Column<int>(type: "int", nullable: true),
                    IsLateFineDeductable = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allowances", x => x.PayrollItemTypeID);
                    table.ForeignKey(
                        name: "FK_Allowances_AmountTypes",
                        column: x => x.AmountTypeID,
                        principalTable: "AmountTypes",
                        principalColumn: "AmountTypeID");
                    table.ForeignKey(
                        name: "FK_Allowances_CurrencyTypes",
                        column: x => x.CurrencyTypeID,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID");
                    table.ForeignKey(
                        name: "FK_Allowances_DependencyTypes",
                        column: x => x.DependencyTypeID,
                        principalTable: "DependencyTypes",
                        principalColumn: "DependencyTypeID");
                    table.ForeignKey(
                        name: "FK_Allowances_DerivedOnTypes",
                        column: x => x.DerivedOnTypeID,
                        principalTable: "DerivedOnTypes",
                        principalColumn: "DerivedOnTypeID");
                    table.ForeignKey(
                        name: "FK_Allowances_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Allowances_Organisations",
                        column: x => x.ownerOrganisationID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Allowances_RecurrenceTypes",
                        column: x => x.RecurrenceTypeID,
                        principalTable: "RecurrenceTypes",
                        principalColumn: "RecurrenceTypeID");
                    table.ForeignKey(
                        name: "FK_PayrollItemTypes_AdditionOrDeductionTypes",
                        column: x => x.AdditionOrDeductionTypeID,
                        principalTable: "AdditionOrDeductionTypes",
                        principalColumn: "AdditionOrDeductionTypeID");
                    table.ForeignKey(
                        name: "FK_PayrollItemTypes_AttendanceDependentTypes",
                        column: x => x.AttendanceDependentTypeID,
                        principalTable: "AttendanceDependentTypes",
                        principalColumn: "AttendanceDependentTypeID");
                    table.ForeignKey(
                        name: "FK_PayrollItemTypes_SAPSheets",
                        column: x => x.SAPSheetID,
                        principalTable: "SAPSheets",
                        principalColumn: "SAPSheetID");
                });

            migrationBuilder.CreateTable(
                name: "PrimaryPayrollItemTypeSAPWageTypes",
                columns: table => new
                {
                    SAPWageTypeID = table.Column<int>(type: "int", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrimaryPayrollItemTypeSAPWageTypes", x => new { x.SAPWageTypeID, x.PayrollItemTypeID });
                    table.ForeignKey(
                        name: "FK_PrimaryPayrollItemTypeSAPWageTypes_PayrollItemTypes",
                        column: x => x.PayrollItemTypeID,
                        principalTable: "PayrollItemTypes",
                        principalColumn: "PayrollItemTypeID");
                    table.ForeignKey(
                        name: "FK_PrimaryPayrollItemTypeSAPWageTypes_SAPWageTypes",
                        column: x => x.SAPWageTypeID,
                        principalTable: "SAPWageTypes",
                        principalColumn: "SAPWageTypeID");
                });

            migrationBuilder.CreateTable(
                name: "SAPExceptions",
                columns: table => new
                {
                    SAPExceptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    EffectiveStartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    SAPExceptionTypeID = table.Column<int>(type: "int", nullable: false),
                    SAPExceptionActionTypeID = table.Column<int>(type: "int", nullable: false),
                    SAPWageTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAPExceptions", x => x.SAPExceptionID);
                    table.ForeignKey(
                        name: "FK_SAPExceptions_NewPayrollItemTypes",
                        column: x => x.PayrollItemTypeID,
                        principalTable: "PayrollItemTypes",
                        principalColumn: "PayrollItemTypeID");
                    table.ForeignKey(
                        name: "FK_SAPExceptions_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_SAPExceptions_SAPExceptionActionTypes",
                        column: x => x.SAPExceptionActionTypeID,
                        principalTable: "SAPExceptionActionTypes",
                        principalColumn: "SAPExceptionActionTypeID");
                    table.ForeignKey(
                        name: "FK_SAPExceptions_SAPExceptionTypes",
                        column: x => x.SAPExceptionTypeID,
                        principalTable: "SAPExceptionTypes",
                        principalColumn: "SAPExceptionTypeID");
                    table.ForeignKey(
                        name: "FK_SAPExceptions_SAPWageTypes",
                        column: x => x.SAPWageTypeID,
                        principalTable: "SAPWageTypes",
                        principalColumn: "SAPWageTypeID");
                });

            migrationBuilder.CreateTable(
                name: "RequestCheckListVerifications",
                columns: table => new
                {
                    RequestCheckListVerificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckListItemID = table.Column<int>(type: "int", nullable: false),
                    IsVerificationSuccssful = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestCheckListVerifications", x => x.RequestCheckListVerificationID);
                    table.ForeignKey(
                        name: "FK_RequestCheckListVerifications_CheckListItems",
                        column: x => x.CheckListItemID,
                        principalTable: "CheckListItems",
                        principalColumn: "CheckListItemID");
                    table.ForeignKey(
                        name: "FK_RequestCheckListVerifications_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "RequestDocuments",
                columns: table => new
                {
                    RequestDocumentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    DocumentID = table.Column<int>(type: "int", nullable: false),
                    LinkedByUserID = table.Column<int>(type: "int", nullable: false),
                    LinkedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDocuments", x => x.RequestDocumentID);
                    table.ForeignKey(
                        name: "FK_RequestDocuments_Documents",
                        column: x => x.DocumentID,
                        principalTable: "Documents",
                        principalColumn: "DocumentID");
                    table.ForeignKey(
                        name: "FK_RequestDocuments_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "RequestDocumentVerifications",
                columns: table => new
                {
                    RequestDocumentVerificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDocumentID = table.Column<int>(type: "int", nullable: false),
                    IsVerifiedSuccessfully = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDocumentVerifications", x => x.RequestDocumentVerificationID);
                    table.ForeignKey(
                        name: "FK_RequestDocumentVerifications_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_RequestDocumentVerifications_RequestDocuments",
                        column: x => x.RequestDocumentID,
                        principalTable: "RequestDocuments",
                        principalColumn: "RequestDocumentID");
                });

            migrationBuilder.CreateTable(
                name: "RequestJobs",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestJobs", x => new { x.RequestID, x.JobID });
                    table.ForeignKey(
                        name: "FK_RequestJobs_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_RequestJobs_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTypeID = table.Column<int>(type: "int", nullable: false),
                    RequestStateID = table.Column<int>(type: "int", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StateChangeRemarks = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    LastStateChangedByUserID = table.Column<int>(type: "int", nullable: true),
                    LastStateChangedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    ApplicantBusinessEntityID = table.Column<int>(type: "int", nullable: true),
                    CurrentApplicationTab = table.Column<int>(type: "int", nullable: true),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: true),
                    RequestEffectiveDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_Requests_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Requests_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_Requests_RequestStates",
                        column: x => x.RequestStateID,
                        principalTable: "RequestStates",
                        principalColumn: "RequestStateID");
                    table.ForeignKey(
                        name: "FK_Requests_RequestTypes1",
                        column: x => x.RequestTypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "RequestTypeID");
                    table.ForeignKey(
                        name: "FK_Requests_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "RequestTeams",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    TeamID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTeams", x => new { x.RequestID, x.TeamID });
                    table.ForeignKey(
                        name: "FK_RequestTeams_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_RequestTeams_Requests",
                        column: x => x.RequestID,
                        principalTable: "Requests",
                        principalColumn: "RequestID");
                    table.ForeignKey(
                        name: "FK_RequestTeams_Teams",
                        column: x => x.TeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID");
                });

            migrationBuilder.CreateTable(
                name: "SalaryRateDefinitions",
                columns: table => new
                {
                    SalaryRateDefinitionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTypeID = table.Column<int>(type: "int", nullable: false),
                    NoOfDays = table.Column<int>(type: "int", nullable: false),
                    NoOfHours = table.Column<int>(type: "int", nullable: false),
                    IsSomeCalculatedDifferently = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    OrganizationID = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryRateDefinitions", x => x.SalaryRateDefinitionID);
                    table.ForeignKey(
                        name: "FK_SalaryRateDefinitions_JobTypes",
                        column: x => x.JobTypeID,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID");
                    table.ForeignKey(
                        name: "FK_SalaryRateDefinitions_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_SalaryRateDefinitions_Requests",
                        column: x => x.RequestID,
                        principalTable: "Requests",
                        principalColumn: "RequestID");
                });

            migrationBuilder.CreateTable(
                name: "StaffAssignedDeductions",
                columns: table => new
                {
                    StaffAssignedDeductionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    DeductionTypeID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    ReferenceNo = table.Column<int>(type: "int", nullable: true),
                    CurrencyTypeID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAssignedDeductions", x => x.StaffAssignedDeductionID);
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductions_CurrencyTypes",
                        column: x => x.CurrencyTypeID,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID");
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductions_DeductionTypes",
                        column: x => x.DeductionTypeID,
                        principalTable: "DeductionTypes",
                        principalColumn: "DeductionTypeID");
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductions_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductions_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductions_Requests",
                        column: x => x.RequestID,
                        principalTable: "Requests",
                        principalColumn: "RequestID");
                });

            migrationBuilder.CreateTable(
                name: "StaffPayrollItemTypes",
                columns: table => new
                {
                    StaffPayrollItemTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    ReferenceNo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAllowances", x => x.StaffPayrollItemTypeID);
                    table.ForeignKey(
                        name: "FK_StaffAllowances_Allowances",
                        column: x => x.PayrollItemTypeID,
                        principalTable: "PayrollItemTypes",
                        principalColumn: "PayrollItemTypeID");
                    table.ForeignKey(
                        name: "FK_StaffAllowances_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID");
                    table.ForeignKey(
                        name: "FK_StaffAllowances_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffPayrollItemTypes_Requests",
                        column: x => x.RequestID,
                        principalTable: "Requests",
                        principalColumn: "RequestID");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    BusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    SSOUserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedByUserID = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UserOrganisationID = table.Column<int>(type: "int", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    CreatedContextID = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedContextID = table.Column<int>(type: "int", nullable: true),
                    LastStateChangedContextID = table.Column<int>(type: "int", nullable: true),
                    LastSynchronisedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastSynchronisedByUserID = table.Column<int>(type: "int", nullable: true),
                    LastSynchronisedContextID = table.Column<int>(type: "int", nullable: true),
                    SSOUserStateID = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    LocalUserStateID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LastStateChangedByUserID = table.Column<int>(type: "int", nullable: true),
                    LastStateChangedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    SSOUserLastUpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UserPreferenceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.BusinessEntityID);
                    table.ForeignKey(
                        name: "FK_Users_Individuals",
                        column: x => x.BusinessEntityID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Users_LocalUserStates",
                        column: x => x.LocalUserStateID,
                        principalTable: "LocalUserStates",
                        principalColumn: "LocalUserStateID");
                    table.ForeignKey(
                        name: "FK_Users_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_Users_Requests",
                        column: x => x.RequestID,
                        principalTable: "Requests",
                        principalColumn: "RequestID");
                    table.ForeignKey(
                        name: "FK_Users_SSOUserStates",
                        column: x => x.SSOUserStateID,
                        principalTable: "SSOUserStates",
                        principalColumn: "SSOUserStateID");
                    table.ForeignKey(
                        name: "FK_Users_UserOrganisations",
                        column: x => x.UserOrganisationID,
                        principalTable: "UserOrganisations",
                        principalColumn: "UserOrganisationID");
                    table.ForeignKey(
                        name: "FK_Users_UserPreferences",
                        column: x => x.UserPreferenceID,
                        principalTable: "UserPreferences",
                        principalColumn: "UserPreferenceID");
                });

            migrationBuilder.CreateTable(
                name: "StaffAssignedDeductionAmounts",
                columns: table => new
                {
                    StaffAssignedDeductionAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    StaffAssignedDeductionID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAssignedDeductionAmounts", x => x.StaffAssignedDeductionAmountID);
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductionAmounts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffAssignedDeductionAmounts_StaffAssignedDeductions",
                        column: x => x.StaffAssignedDeductionID,
                        principalTable: "StaffAssignedDeductions",
                        principalColumn: "StaffAssignedDeductionID");
                });

            migrationBuilder.CreateTable(
                name: "StaffSalaryDeductions",
                columns: table => new
                {
                    StaffSalaryDeductionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffAssignedDeductionID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Details = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffSalaryDeductions", x => x.StaffSalaryDeductionID);
                    table.ForeignKey(
                        name: "FK_StaffSalaryDeductions_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffSalaryDeductions_StaffAssignedDeductions",
                        column: x => x.StaffAssignedDeductionID,
                        principalTable: "StaffAssignedDeductions",
                        principalColumn: "StaffAssignedDeductionID");
                    table.ForeignKey(
                        name: "FK_StaffSalaryDeductions_StaffSalaries",
                        column: x => x.StaffSalaryID,
                        principalTable: "StaffSalaries",
                        principalColumn: "StaffSalaryID");
                });

            migrationBuilder.CreateTable(
                name: "StaffPayrollItemDetails",
                columns: table => new
                {
                    StaffPayrollItemDetailsID = table.Column<int>(type: "int", nullable: false),
                    StaffPayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    CIFNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    BillNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RefNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FacilityType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    DealNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Installment = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPayrollItemDetails", x => x.StaffPayrollItemDetailsID);
                    table.ForeignKey(
                        name: "FK_StaffPayrollItemDetails_StaffPayrollItemTypes",
                        column: x => x.StaffPayrollItemTypeID,
                        principalTable: "StaffPayrollItemTypes",
                        principalColumn: "StaffPayrollItemTypeID");
                });

            migrationBuilder.CreateTable(
                name: "StaffPayrollItemTypeAmounts",
                columns: table => new
                {
                    StaffPayrollItemTypeAmountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    StaffPayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    CurrencyTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPayrollItemTypeAmounts", x => x.StaffPayrollItemTypeAmountID);
                    table.ForeignKey(
                        name: "FK_StaffPayrollItemTypeAmounts_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffPayrollItemTypeAmounts_StaffPayrollItemTypes",
                        column: x => x.StaffPayrollItemTypeID,
                        principalTable: "StaffPayrollItemTypes",
                        principalColumn: "StaffPayrollItemTypeID");
                });

            migrationBuilder.CreateTable(
                name: "StaffPayrollItemTypeSchedules",
                columns: table => new
                {
                    StaffPayrollItemTypeScheduleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffPayrollItemTypeID = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    CurrencyTypeID = table.Column<int>(type: "int", nullable: false),
                    PayrollCycleRequestID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionSchedules", x => x.StaffPayrollItemTypeScheduleID);
                    table.ForeignKey(
                        name: "FK_DeductionSchedules_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffPayrollItemTypeSchedules_PayrollCycles",
                        column: x => x.PayrollCycleRequestID,
                        principalTable: "PayrollCycles",
                        principalColumn: "RequestID");
                    table.ForeignKey(
                        name: "FK_StaffPayrollItemTypeSchedules_StaffPayrollItemTypes",
                        column: x => x.StaffPayrollItemTypeID,
                        principalTable: "StaffPayrollItemTypes",
                        principalColumn: "StaffPayrollItemTypeID");
                });

            migrationBuilder.CreateTable(
                name: "StaffSalaryPayrollItems",
                columns: table => new
                {
                    StaffSalaryPayrollItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffPayrollItemTypeID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(38,8)", nullable: false),
                    Details = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false),
                    StaffSalaryID = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    PayrollItemTypeID = table.Column<int>(type: "int", nullable: true),
                    IsSpecialType = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffSalaryAdditions", x => x.StaffSalaryPayrollItemID);
                    table.ForeignKey(
                        name: "FK_StaffSalaryAdditions_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_StaffSalaryAdditions_StaffSalaries",
                        column: x => x.StaffSalaryID,
                        principalTable: "StaffSalaries",
                        principalColumn: "StaffSalaryID");
                    table.ForeignKey(
                        name: "FK_StaffSalaryPayrollItems_PayrollItemTypes",
                        column: x => x.PayrollItemTypeID,
                        principalTable: "PayrollItemTypes",
                        principalColumn: "PayrollItemTypeID");
                    table.ForeignKey(
                        name: "FK_StaffSalaryPayrollItems_StaffPayrollItemTypes",
                        column: x => x.StaffPayrollItemTypeID,
                        principalTable: "StaffPayrollItemTypes",
                        principalColumn: "StaffPayrollItemTypeID");
                });

            migrationBuilder.CreateTable(
                name: "UserAssignedUserGroups",
                columns: table => new
                {
                    UserGroupID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAssignedUserGroups", x => new { x.UserGroupID, x.UserID });
                    table.ForeignKey(
                        name: "FK_UserAssignedUserGroups_OperationLogs",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserAssignedUserGroups_UserGroups",
                        column: x => x.UserGroupID,
                        principalTable: "UserGroups",
                        principalColumn: "UserGroupID");
                    table.ForeignKey(
                        name: "FK_UserAssignedUserGroups_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    UserOrganisationID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.RoleID, x.UserID, x.UserOrganisationID });
                    table.ForeignKey(
                        name: "FK_UserRoles_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_UserRoles_UserOrganisations",
                        column: x => x.UserOrganisationID,
                        principalTable: "UserOrganisations",
                        principalColumn: "UserOrganisationID");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "UserServiceRoles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    OperationLogID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserServiceRoles", x => new { x.RoleID, x.ServiceID, x.UserID });
                    table.ForeignKey(
                        name: "FK_UserServiceRoles_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID");
                    table.ForeignKey(
                        name: "FK_UserServiceRoles_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_UserServiceRoles_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                    table.ForeignKey(
                        name: "FK_UserServiceRoles_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID");
                });

            migrationBuilder.CreateTable(
                name: "WorkPlans",
                columns: table => new
                {
                    WorkPlanID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    OrganisationBusinessEntityID = table.Column<int>(type: "int", nullable: false),
                    PlanningProviderID = table.Column<int>(type: "int", nullable: false),
                    WorkTemplateId = table.Column<int>(type: "int", nullable: true),
                    WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                    GenerationSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    GeneratedByUserID = table.Column<int>(type: "int", nullable: true),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FinalizedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    FinalizedByUserID = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    PlanGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPlans", x => x.WorkPlanID);
                    table.ForeignKey(
                        name: "FK_WorkPlans_FinalizedByUser",
                        column: x => x.FinalizedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_GeneratedByUser",
                        column: x => x.GeneratedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_Individuals",
                        column: x => x.IndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_Jobs",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_OperationLogs",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_Organisations",
                        column: x => x.OrganisationBusinessEntityID,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_PlanningProviders",
                        column: x => x.PlanningProviderID,
                        principalTable: "PlanningProviders",
                        principalColumn: "PlanningProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPlans_WorkTemplates_WorkTemplateId",
                        column: x => x.WorkTemplateId,
                        principalTable: "WorkTemplates",
                        principalColumn: "WorkTemplateID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignments",
                columns: table => new
                {
                    WorkAssignmentID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkPlanID = table.Column<long>(type: "bigint", nullable: false),
                    WorkTemplateID = table.Column<int>(type: "int", nullable: false),
                    WorkTemplateTypeID = table.Column<int>(type: "int", nullable: false),
                    WorkAssignmentStateID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    GraceMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RequiresAttendance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresCheckOut = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AssignmentSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceReferenceID = table.Column<long>(type: "bigint", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    AllowedRadiusMeters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancelledByUserID = table.Column<int>(type: "int", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignments", x => x.WorkAssignmentID);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_OperationLogs_OperationLogID",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_Users_CancelledByUserID",
                        column: x => x.CancelledByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_WorkAssignmentStates_WorkAssignmentStateID",
                        column: x => x.WorkAssignmentStateID,
                        principalTable: "WorkAssignmentStates",
                        principalColumn: "WorkAssignmentStateID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_WorkPlans_WorkPlanID",
                        column: x => x.WorkPlanID,
                        principalTable: "WorkPlans",
                        principalColumn: "WorkPlanID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_WorkTemplateTypes_WorkTemplateTypeID",
                        column: x => x.WorkTemplateTypeID,
                        principalTable: "WorkTemplateTypes",
                        principalColumn: "WorkTemplateTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_WorkTemplates_WorkTemplateID",
                        column: x => x.WorkTemplateID,
                        principalTable: "WorkTemplates",
                        principalColumn: "WorkTemplateID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkPlanSegments",
                columns: table => new
                {
                    WorkPlanSegmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkPlanId = table.Column<long>(type: "bigint", nullable: false),
                    WorkTemplateSegmentId = table.Column<int>(type: "int", nullable: true),
                    WorkSegmentTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GraceBeforeMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceAfterMinutes = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAttendance = table.Column<bool>(type: "bit", nullable: false),
                    RequiresLocationValidation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresDeviceValidation = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceId = table.Column<int>(type: "int", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPlanSegments", x => x.WorkPlanSegmentId);
                    table.ForeignKey(
                        name: "FK_WorkPlanSegments_WorkPlans_WorkPlanId",
                        column: x => x.WorkPlanId,
                        principalTable: "WorkPlans",
                        principalColumn: "WorkPlanID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkPlanSegments_WorkSegmentTypes_WorkSegmentTypeId",
                        column: x => x.WorkSegmentTypeId,
                        principalTable: "WorkSegmentTypes",
                        principalColumn: "WorkSegmentTypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkPlanSegments_WorkTemplateSegments_WorkTemplateSegmentId",
                        column: x => x.WorkTemplateSegmentId,
                        principalTable: "WorkTemplateSegments",
                        principalColumn: "WorkTemplateSegmentID");
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignmentOwners",
                columns: table => new
                {
                    WorkAssignmentOwnerID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkAssignmentID = table.Column<long>(type: "bigint", nullable: false),
                    IndividualID = table.Column<int>(type: "int", nullable: false),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    OwnershipType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedByUserID = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelievedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelievedByUserID = table.Column<int>(type: "int", nullable: true),
                    ReliefReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCurrentOwner = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignmentOwners", x => x.WorkAssignmentOwnerID);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentOwners_Individuals_IndividualID",
                        column: x => x.IndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentOwners_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentOwners_OperationLogs_OperationLogID",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentOwners_Users_AssignedByUserID",
                        column: x => x.AssignedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentOwners_Users_RelievedByUserID",
                        column: x => x.RelievedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentOwners_WorkAssignments_WorkAssignmentID",
                        column: x => x.WorkAssignmentID,
                        principalTable: "WorkAssignments",
                        principalColumn: "WorkAssignmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignmentSegments",
                columns: table => new
                {
                    WorkAssignmentSegmentID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkAssignmentID = table.Column<long>(type: "bigint", nullable: false),
                    WorkTemplateSegmentID = table.Column<int>(type: "int", nullable: false),
                    WorkSegmentTypeID = table.Column<int>(type: "int", nullable: false),
                    ExpectedInOutModeId = table.Column<int>(type: "int", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowedRadiusMeters = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GraceBeforeMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceAfterMinutes = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAttendance = table.Column<bool>(type: "bit", nullable: false),
                    RequiresLocationValidation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresDeviceValidation = table.Column<bool>(type: "bit", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignmentSegments", x => x.WorkAssignmentSegmentID);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentSegments_WorkAssignments_WorkAssignmentID",
                        column: x => x.WorkAssignmentID,
                        principalTable: "WorkAssignments",
                        principalColumn: "WorkAssignmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentSegments_WorkSegmentTypes_WorkSegmentTypeID",
                        column: x => x.WorkSegmentTypeID,
                        principalTable: "WorkSegmentTypes",
                        principalColumn: "WorkSegmentTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentSegments_WorkTemplateSegments_WorkTemplateSegmentID",
                        column: x => x.WorkTemplateSegmentID,
                        principalTable: "WorkTemplateSegments",
                        principalColumn: "WorkTemplateSegmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignmentTransfers",
                columns: table => new
                {
                    WorkAssignmentTransferID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkAssignmentID = table.Column<long>(type: "bigint", nullable: false),
                    WorkAssignmentTransferStateID = table.Column<int>(type: "int", nullable: false),
                    FromWorkAssignmentOwnerID = table.Column<long>(type: "bigint", nullable: false),
                    ToWorkAssignmentOwnerID = table.Column<long>(type: "bigint", nullable: true),
                    FromIndividualID = table.Column<int>(type: "int", nullable: false),
                    FromJobID = table.Column<int>(type: "int", nullable: false),
                    ToIndividualID = table.Column<int>(type: "int", nullable: false),
                    ToJobID = table.Column<int>(type: "int", nullable: false),
                    TransferType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestID = table.Column<int>(type: "int", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedByUserID = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserID = table.Column<int>(type: "int", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByUserID = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OperationLogID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignmentTransfers", x => x.WorkAssignmentTransferID);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Individuals_FromIndividualID",
                        column: x => x.FromIndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Individuals_ToIndividualID",
                        column: x => x.ToIndividualID,
                        principalTable: "Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Jobs_FromJobID",
                        column: x => x.FromJobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Jobs_ToJobID",
                        column: x => x.ToJobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_OperationLogs_OperationLogID",
                        column: x => x.OperationLogID,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Requests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Users_ApprovedByUserID",
                        column: x => x.ApprovedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Users_RejectedByUserID",
                        column: x => x.RejectedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_Users_RequestedByUserID",
                        column: x => x.RequestedByUserID,
                        principalTable: "Users",
                        principalColumn: "BusinessEntityID");
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_WorkAssignmentOwners_FromWorkAssignmentOwnerID",
                        column: x => x.FromWorkAssignmentOwnerID,
                        principalTable: "WorkAssignmentOwners",
                        principalColumn: "WorkAssignmentOwnerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_WorkAssignmentOwners_ToWorkAssignmentOwnerID",
                        column: x => x.ToWorkAssignmentOwnerID,
                        principalTable: "WorkAssignmentOwners",
                        principalColumn: "WorkAssignmentOwnerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_WorkAssignmentTransferStates_WorkAssignmentTransferStateID",
                        column: x => x.WorkAssignmentTransferStateID,
                        principalTable: "WorkAssignmentTransferStates",
                        principalColumn: "WorkAssignmentTransferStateID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkAssignmentTransfers_WorkAssignments_WorkAssignmentID",
                        column: x => x.WorkAssignmentID,
                        principalTable: "WorkAssignments",
                        principalColumn: "WorkAssignmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddressBases_AddressBaseTypeID",
                table: "AddressBases",
                column: "AddressBaseTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_AddressBases_WardID",
                table: "AddressBases",
                column: "WardID");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_IslandID",
                table: "Addresses",
                column: "IslandID");

            migrationBuilder.CreateIndex(
                name: "IX_AddressInstances_AddressBaseID",
                table: "AddressInstances",
                column: "AddressBaseID");

            migrationBuilder.CreateIndex(
                name: "IX_AddressInstances_AddressInstanceTypeID",
                table: "AddressInstances",
                column: "AddressInstanceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_AggregatedSalaries_StaffSalaryID",
                table: "AggregatedSalaries",
                column: "StaffSalaryID");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedWorkTypes_JobID",
                table: "AssignedWorkTypes",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedWorkTypes_OrganisationID",
                table: "AssignedWorkTypes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedWorkTypes_WorkTypeID",
                table: "AssignedWorkTypes",
                column: "WorkTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Atolls_CityID",
                table: "Atolls",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedBreakTimes_OperationLogID",
                table: "AttachedBreakTimes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedBreakTimes_OwnerTypeID",
                table: "AttachedBreakTimes",
                column: "OwnerTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedBreakTimesDays_AttachedBreakTimeID",
                table: "AttachedBreakTimesDays",
                column: "AttachedBreakTimeID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedBreakTimesDays_BreakTimeID",
                table: "AttachedBreakTimesDays",
                column: "BreakTimeID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedBreakTimesDays_DayOfWeekID",
                table: "AttachedBreakTimesDays",
                column: "DayOfWeekID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedBreakTimesDays_OperationLogID",
                table: "AttachedBreakTimesDays",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedPayrollItemsProcessingWhiteLists_OperationLogID",
                table: "AttachedPayrollItemsProcessingWhiteLists",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedPayrollItemsProcessingWhiteLists_OrganisationID",
                table: "AttachedPayrollItemsProcessingWhiteLists",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_AttachedPayrollItemsProcessingWhiteLists_PayrollItemsProcessingWhiteListID",
                table: "AttachedPayrollItemsProcessingWhiteLists",
                column: "PayrollItemsProcessingWhiteListID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceClientInstances_AttendanceClientStateID",
                table: "AttendanceClientInstances",
                column: "AttendanceClientStateID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceClientInstances_OperationLogID",
                table: "AttendanceClientInstances",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceClientInstances_OrganisationID",
                table: "AttendanceClientInstances",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceClientInstances_OrganisationStructureID",
                table: "AttendanceClientInstances",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_AttendanceClientID",
                table: "AttendanceDevices",
                column: "AttendanceClientID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_AttendanceDeviceInOutTypeID",
                table: "AttendanceDevices",
                column: "AttendanceDeviceInOutTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_AttendanceDeviceStateID",
                table: "AttendanceDevices",
                column: "AttendanceDeviceStateID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_OperationLogID",
                table: "AttendanceDevices",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDeviceStaffs_AttendanceDeviceID",
                table: "AttendanceDeviceStaffs",
                column: "AttendanceDeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDeviceStaffs_IndividualID",
                table: "AttendanceDeviceStaffs",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogChangeRequests",
                table: "AttendanceLogChangeRequests",
                column: "AttendanceLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogRequests_AttendanceLogID",
                table: "AttendanceLogRequests",
                column: "AttendanceLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogRequests_RequestID",
                table: "AttendanceLogRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogResolutions_AttendanceResolutionStatusID",
                table: "AttendanceLogResolutions",
                column: "AttendanceResolutionStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogResolutions_WorkAssignment",
                table: "AttendanceLogResolutions",
                columns: new[] { "WorkAssignmentID", "AttendanceResolutionStatusID", "IsValid" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogResolutions_WorkAssignmentSegmentID",
                table: "AttendanceLogResolutions",
                column: "WorkAssignmentSegmentID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogResolutions_WorkPlanID",
                table: "AttendanceLogResolutions",
                column: "WorkPlanID");

            migrationBuilder.CreateIndex(
                name: "UX_AttendanceLogResolutions_ActiveAttendanceLog",
                table: "AttendanceLogResolutions",
                column: "AttendanceLogID",
                unique: true,
                filter: "[IsValid] = 1");

            migrationBuilder.CreateIndex(
                name: "IDX_AttendanceLogs_Date",
                table: "AttendanceLogs",
                column: "Date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_AttendanceDeviceID",
                table: "AttendanceLogs",
                column: "AttendanceDeviceID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_AttendanceLogModeID",
                table: "AttendanceLogs",
                column: "AttendanceLogModeID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_AttendanceLogStateID",
                table: "AttendanceLogs",
                column: "AttendanceLogStateID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_InOutModeID",
                table: "AttendanceLogs",
                column: "InOutModeID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_LookUpByIndividualAndDate",
                table: "AttendanceLogs",
                columns: new[] { "IndividualID", "OrganisationID", "Date", "InOutModeID" },
                descending: new[] { false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_OperationLogID",
                table: "AttendanceLogs",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_OrganisationID",
                table: "AttendanceLogs",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_OrganisationStructureID",
                table: "AttendanceLogs",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_RelatedAttendanceLogID",
                table: "AttendanceLogs",
                column: "RelatedAttendanceLogID");

            migrationBuilder.CreateIndex(
                name: "ix_IndexName_10_MARCH",
                table: "AttendanceLogs",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceLogs",
                table: "AttendanceLogs",
                column: "UIDStamp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogsTemp_LookUpByIndividualAndDate",
                table: "AttendanceLogsTemp",
                columns: new[] { "IndividualID", "OrganisationID", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceResolutionStatuses_Name",
                table: "AttendanceResolutionStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BasicSalaries_OperationLogID",
                table: "BasicSalaries",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BreakTimes_OperationLogID",
                table: "BreakTimes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BreakTimes_OrganisationID",
                table: "BreakTimes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_BreakTimes_OrganisationStructureID",
                table: "BreakTimes",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_BudgetItemID",
                table: "BudgetTransactions",
                column: "BudgetItemID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_OrganisationID",
                table: "BudgetTransactions",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionTypeID",
                table: "BudgetTransactions",
                column: "TransactionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadedDocuments_DocumentStateID",
                table: "BulkUploadedDocuments",
                column: "DocumentStateID");

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadedDocuments_OperationLogID",
                table: "BulkUploadedDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadedDocuments_UploadedByUserID",
                table: "BulkUploadedDocuments",
                column: "UploadedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadedDocumentSummaries_DocumentID",
                table: "BulkUploadedDocumentSummaries",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntities_BusinessEntityStateID",
                table: "BusinessEntities",
                column: "BusinessEntityStateID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntities_BusinessEntityTypeID",
                table: "BusinessEntities",
                column: "BusinessEntityTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntities_LastStateChangedByUserID",
                table: "BusinessEntities",
                column: "LastStateChangedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntities_OperationLogID",
                table: "BusinessEntities",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntities_VerifiedBy",
                table: "BusinessEntities",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntities_VerifiedStateID",
                table: "BusinessEntities",
                column: "VerifiedStateID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntitiesDocuments_DocumentID",
                table: "BusinessEntitiesDocuments",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntitiesDocuments_OperationLogID",
                table: "BusinessEntitiesDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityCalendars_BusinessEntityID",
                table: "BusinessEntityCalendars",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityCalendars_CalenderID",
                table: "BusinessEntityCalendars",
                column: "CalenderID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityCalendars_OperationLogID",
                table: "BusinessEntityCalendars",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityLocations_BusinessEntityID",
                table: "BusinessEntityLocations",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityLocations_BusinessEntityLocationTypeID",
                table: "BusinessEntityLocations",
                column: "BusinessEntityLocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityLocations_LocationID",
                table: "BusinessEntityLocations",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelatedLocationTypes_BusinessEntityTypeID",
                table: "BusinessEntityRelatedLocationTypes",
                column: "BusinessEntityTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelatedLocationTypes_LocationTypeID",
                table: "BusinessEntityRelatedLocationTypes",
                column: "LocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelationAssignedRoles_BusinessEntityRelationID",
                table: "BusinessEntityRelationAssignedRoles",
                column: "BusinessEntityRelationID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelationAssignedRoles_RoleID",
                table: "BusinessEntityRelationAssignedRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelations_BusinessEntityRelationStateID",
                table: "BusinessEntityRelations",
                column: "BusinessEntityRelationStateID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelations_BusinessEntityRelationTypeID",
                table: "BusinessEntityRelations",
                column: "BusinessEntityRelationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelations_DelegateeBusinessEntityID",
                table: "BusinessEntityRelations",
                column: "DelegateeBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelations_DelegatorBusinessEntityID",
                table: "BusinessEntityRelations",
                column: "DelegatorBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelations_OperationLogID",
                table: "BusinessEntityRelations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRelationTypes_BusinessEntityID",
                table: "BusinessEntityRelationTypes",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntityRequests_RequestID",
                table: "BusinessEntityRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntitySchedules_BusinessEntityID",
                table: "BusinessEntitySchedules",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntitySchedules_OperationLogID",
                table: "BusinessEntitySchedules",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntitySchedules_ScheduleID",
                table: "BusinessEntitySchedules",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_BussinessEntityContactInformations_BussinessEntityID",
                table: "BussinessEntityContactInformations",
                column: "BussinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_BussinessEntityContactInformations_ContactInformationTypeID",
                table: "BussinessEntityContactInformations",
                column: "ContactInformationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_BussinessEntityContactInformations_OperationLogID",
                table: "BussinessEntityContactInformations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarElements_ElementID",
                table: "CalendarElements",
                column: "ElementID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarInstances_CalendarID",
                table: "CalendarInstances",
                column: "CalendarID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarInstances_OperationLogID",
                table: "CalendarInstances",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarMonths_CalendarInstanceID",
                table: "CalendarMonths",
                column: "CalendarInstanceID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarMonths_MonthID",
                table: "CalendarMonths",
                column: "MonthID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarMonths_OperationLogID",
                table: "CalendarMonths",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_CalendarTypeID",
                table: "Calendars",
                column: "CalendarTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_OperationLogID",
                table: "Calendars",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarSchedules_CalendarID",
                table: "CalendarSchedules",
                column: "CalendarID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarSchedules_ScheduleID",
                table: "CalendarSchedules",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarTypes_OperationLogID",
                table: "CalendarTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_CheckListItems_OperationLogID",
                table: "CheckListItems",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_RegionID",
                table: "Countries",
                column: "RegionID");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_CurrencyTpeID",
                table: "CurrencyExchangeRates",
                column: "CurrencyTpeID");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyExchangeRates_OperationLogID",
                table: "CurrencyExchangeRates",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionAttributes_DataCorrectionAttributeLookupTypeID",
                table: "DataCorrectionAttributes",
                column: "DataCorrectionAttributeLookupTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequestAttributeValues_DataCorrectionActionTypeID",
                table: "DataCorrectionRequestAttributeValues",
                column: "DataCorrectionActionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequestAttributeValues_DataCorrectionAttributeID",
                table: "DataCorrectionRequestAttributeValues",
                column: "DataCorrectionAttributeID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequestAttributeValues_DataCorrectionRequestID",
                table: "DataCorrectionRequestAttributeValues",
                column: "DataCorrectionRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_BusinessEntityID",
                table: "DataCorrectionRequests",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_DataCorrectionRequestStateID",
                table: "DataCorrectionRequests",
                column: "DataCorrectionRequestStateID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_DataCorrectionRequestTypeID",
                table: "DataCorrectionRequests",
                column: "DataCorrectionRequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_LastStateChangedByUserID",
                table: "DataCorrectionRequests",
                column: "LastStateChangedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_LastUpdatedByUserID",
                table: "DataCorrectionRequests",
                column: "LastUpdatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_OperationLogID",
                table: "DataCorrectionRequests",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_RequestID",
                table: "DataCorrectionRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_ServiceID",
                table: "DataCorrectionRequests",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequestTypeAttributes_DataCorrectionActionTypeID",
                table: "DataCorrectionRequestTypeAttributes",
                column: "DataCorrectionActionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequestTypeAttributes_DataCorrectionAttributeID",
                table: "DataCorrectionRequestTypeAttributes",
                column: "DataCorrectionAttributeID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypeAmounts_DeductionTypeID",
                table: "DeductionTypeAmounts",
                column: "DeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypeAmounts_OperationLogID",
                table: "DeductionTypeAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_CurrencyTypeID",
                table: "DeductionTypes",
                column: "CurrencyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_DeductionAmountTypeID",
                table: "DeductionTypes",
                column: "DeductionAmountTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_DerivedOnTypeID",
                table: "DeductionTypes",
                column: "DerivedOnTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_OwnerOrganisationID",
                table: "DeductionTypes",
                column: "OwnerOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionTypes_RequestID",
                table: "DeductionTypes",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedAdditionDeductionTypes_DeductionTypeID",
                table: "DerivedAdditionDeductionTypes",
                column: "DeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedAdditionDeductionTypes_DerivedAdditionDeductionTypeItemID",
                table: "DerivedAdditionDeductionTypes",
                column: "DerivedAdditionDeductionTypeItemID");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedOnPayrollItemTypes_OperationLogID",
                table: "DerivedOnPayrollItemTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedOnPayrollItemTypes_OtherPayrollItemTypeID",
                table: "DerivedOnPayrollItemTypes",
                column: "OtherPayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedOnPayrollItemTypes_PayrollItemTypeID",
                table: "DerivedOnPayrollItemTypes",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OperationLogID",
                table: "Documents",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "ix_IndexName_10_MARCH",
                table: "Documents",
                column: "DocumentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_OrganisationID",
                table: "DocumentTypes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ServiceTypeID",
                table: "DocumentTypes",
                column: "ServiceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Elements_DayOfWeekID",
                table: "Elements",
                column: "DayOfWeekID");

            migrationBuilder.CreateIndex(
                name: "IX_Elements_OperationLogID",
                table: "Elements",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Elements_SpecialDayTypeID",
                table: "Elements",
                column: "SpecialDayTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Elements_WorkShiftID",
                table: "Elements",
                column: "WorkShiftID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupConfigurableValues_GroupConfigurableValueTypeID",
                table: "GroupConfigurableValues",
                column: "GroupConfigurableValueTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupConfigurableValues_GroupID",
                table: "GroupConfigurableValues",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLeaveSets_GroupID",
                table: "GroupLeaveSets",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLeaveSets_LeaveSetID",
                table: "GroupLeaveSets",
                column: "LeaveSetID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLeaveSets_OperationLogID",
                table: "GroupLeaveSets",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_BusinessEntityOrganisationID",
                table: "Groups",
                column: "BusinessEntityOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupTypeID",
                table: "Groups",
                column: "GroupTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OperationLogID",
                table: "Groups",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganisationStructureID",
                table: "Groups",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupSchedules_GroupID",
                table: "GroupSchedules",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupSchedules_OperationLogID",
                table: "GroupSchedules",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupSchedules_ScheduleID",
                table: "GroupSchedules",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStaffs_GroupID",
                table: "GroupStaffs",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStaffs_OperationLogID",
                table: "GroupStaffs",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStaffs_StaffIndividualID",
                table: "GroupStaffs",
                column: "StaffIndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupTypes_OrganisationBusinessEntityId",
                table: "GroupTypes",
                column: "OrganisationBusinessEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_IDCards",
                table: "IDCards",
                column: "IDCardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IDCards_BusinessEntityID",
                table: "IDCards",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_IDCards_IDCardStateID",
                table: "IDCards",
                column: "IDCardStateID");

            migrationBuilder.CreateIndex(
                name: "IX_IDCards_OperationLogID",
                table: "IDCards",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Individuals_CountryID",
                table: "Individuals",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Individuals_GenderTypeID",
                table: "Individuals",
                column: "GenderTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Islands_AtollID",
                table: "Islands",
                column: "AtollID");

            migrationBuilder.CreateIndex(
                name: "IX_Islands_CityID",
                table: "Islands",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_JobLeaveTypes",
                table: "JobLeaveTypes",
                columns: new[] { "JobID", "LeaveTypeID" });

            migrationBuilder.CreateIndex(
                name: "IX_JobLeaveTypes_JobID_LeaveDefinitionID",
                table: "JobLeaveTypes",
                columns: new[] { "JobID", "LeaveDefinitionID" });

            migrationBuilder.CreateIndex(
                name: "IX_JobLeaveTypes_LeaveDefinitionID",
                table: "JobLeaveTypes",
                column: "LeaveDefinitionID");

            migrationBuilder.CreateIndex(
                name: "IX_JobLeaveTypes_LeaveTypeID",
                table: "JobLeaveTypes",
                column: "LeaveTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_JobLeaveTypes_OperationLogID",
                table: "JobLeaveTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPositionBasicSalaries_JobPoistionID",
                table: "JobPositionBasicSalaries",
                column: "JobPoistionID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPositionBasicSalaries_OperationLogID",
                table: "JobPositionBasicSalaries",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IDX_JobPositions_FRDT_TODT",
                table: "JobPositions",
                columns: new[] { "FromDate", "ToDate" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IDX_JobPositions_PositionID",
                table: "JobPositions",
                column: "PositionID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPositions_JobPositionStateID",
                table: "JobPositions",
                column: "JobPositionStateID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPositions_OperationLogID",
                table: "JobPositions",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "NonClusteredIndex-20160804-145620",
                table: "JobPositions",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPositionsRequests_JobPositionID",
                table: "JobPositionsRequests",
                column: "JobPositionID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPositionsRequests_OperationLogID",
                table: "JobPositionsRequests",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequests_JobID",
                table: "JobRequests",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IndividualID",
                table: "Jobs",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobStateID",
                table: "Jobs",
                column: "JobStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobTypeID",
                table: "Jobs",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_OrganisationID",
                table: "Jobs",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_OrganisationStructureID",
                table: "Jobs",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ServiceID",
                table: "Jobs",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_JobSAPExceptions_JobID",
                table: "JobSAPExceptions",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_JobTypes_OperationLogId",
                table: "JobTypes",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTypesExceptions_JobTypeID",
                table: "JobTypesExceptions",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkTemplateAssignment_Job_Template",
                table: "JobWorkTemplateAssignments",
                columns: new[] { "JobID", "WorkTemplateID", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkTemplateAssignments_WorkTemplateID",
                table: "JobWorkTemplateAssignments",
                column: "WorkTemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkTemplates_JobId_IsActive_EffectiveFrom_EffectiveTo",
                table: "JobWorkTemplates",
                columns: new[] { "JobId", "IsActive", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkTemplates_WorkTemplateId",
                table: "JobWorkTemplates",
                column: "WorkTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyHolders_IndividualID",
                table: "KeyHolders",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_KeyHolders_OperationLogID",
                table: "KeyHolders",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_KPIDocuments_DocumentID",
                table: "KPIDocuments",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_KPIDocuments_LinkedByUserID",
                table: "KPIDocuments",
                column: "LinkedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_KPIDocuments_OperationLogID",
                table: "KPIDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveChangeRequests_LeaveChangeRequestTypeID",
                table: "LeaveChangeRequests",
                column: "LeaveChangeRequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveChangeRequests_LeaveID",
                table: "LeaveChangeRequests",
                column: "LeaveID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveChangeRequests_OperationLogID",
                table: "LeaveChangeRequests",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveChangeRequests_RequestID",
                table: "LeaveChangeRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveConfigurableValues_JobTypeID",
                table: "LeaveConfigurableValues",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveConfigurableValues_LeaveConfigurableValueTypeID",
                table: "LeaveConfigurableValues",
                column: "LeaveConfigurableValueTypeID");

            migrationBuilder.CreateIndex(
                name: "UQ_OrganisationID_JobTypeID_LeaveConfigurableValueTypeID",
                table: "LeaveConfigurableValues",
                columns: new[] { "OrganisationID", "JobTypeID", "LeaveConfigurableValueTypeID" },
                unique: true,
                filter: "[JobTypeID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDocuments_DocumentID",
                table: "LeaveDocuments",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDocuments_LeaveID",
                table: "LeaveDocuments",
                column: "LeaveID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDocuments_LinkedByUserID",
                table: "LeaveDocuments",
                column: "LinkedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveDocuments_OperationLogID",
                table: "LeaveDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveForms_OperationLogID",
                table: "LeaveForms",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveFrameworkConfigurations_OperationLogID",
                table: "LeaveFrameworkConfigurations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveFrameworkConfigurations_Organisation_Active_Effective",
                table: "LeaveFrameworkConfigurations",
                columns: new[] { "OrganisationID", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_LeavePolicies_LeaveDefinitionID",
                table: "LeavePolicies",
                column: "LeaveDefinitionID");

            migrationBuilder.CreateIndex(
                name: "IX_LeavePolicyAccrualRules_LeavePolicyID",
                table: "LeavePolicyAccrualRules",
                column: "LeavePolicyID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveReasons_LeaveTypeReasonTypeID",
                table: "LeaveReasons",
                column: "LeaveTypeReasonTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_RequestID",
                table: "LeaveRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IDX_LEAVES_FRDT_TODT",
                table: "Leaves",
                columns: new[] { "FromDate", "ToDate" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_IndexName_10_MARCH",
                table: "Leaves",
                columns: new[] { "LeaveTypeID", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_LeaveStateID",
                table: "Leaves",
                column: "LeaveStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_LeaveType_LeaveState",
                table: "Leaves",
                columns: new[] { "JobID", "LeaveTypeID", "LeaveStateID" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_OperationLogID",
                table: "Leaves",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeavesBulkUploadedDocuments_DocumentStateID",
                table: "LeavesBulkUploadedDocuments",
                column: "DocumentStateID");

            migrationBuilder.CreateIndex(
                name: "IX_LeavesBulkUploadedDocuments_OperationLogID",
                table: "LeavesBulkUploadedDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeavesBulkUploadedDocuments_UploadedByUserID",
                table: "LeavesBulkUploadedDocuments",
                column: "UploadedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSetLeaveTypes_LeaveSetID",
                table: "LeaveSetLeaveTypes",
                column: "LeaveSetID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSetLeaveTypes_LeaveTypeID",
                table: "LeaveSetLeaveTypes",
                column: "LeaveTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSetLeaveTypes_OperationLogID",
                table: "LeaveSetLeaveTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSets_OperationLogID",
                table: "LeaveSets",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSets_OrganisationID",
                table: "LeaveSets",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSpendingLocations_CountryID",
                table: "LeaveSpendingLocations",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSpendingLocations_IslandID",
                table: "LeaveSpendingLocations",
                column: "IslandID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveSpendingLocations_OperationLogID",
                table: "LeaveSpendingLocations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypeMappings_LeaveDefinitionId",
                table: "LeaveTypeMappings",
                column: "LeaveDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_OperationLogID",
                table: "LeaveTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_OrganisationID",
                table: "LeaveTypes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_RequestTypeID",
                table: "LeaveTypes",
                column: "RequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveWorkHandOvers_JobID",
                table: "LeaveWorkHandOvers",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveWorkHandOvers_LeaveID",
                table: "LeaveWorkHandOvers",
                column: "LeaveID");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CountryID",
                table: "Locations",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationTypeID",
                table: "Locations",
                column: "LocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OperationLogID",
                table: "Locations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Months_CalendarTypeID",
                table: "Months",
                column: "CalendarTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Months_OperationLogID",
                table: "Months",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinkFacilityTypes_FacilityTypeID",
                table: "NavigationLinkFacilityTypes",
                column: "FacilityTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinkFacilityTypes_NavigationLinkID",
                table: "NavigationLinkFacilityTypes",
                column: "NavigationLinkID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_ContextID",
                table: "NavigationLinks",
                column: "ContextID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_LinkTypeID",
                table: "NavigationLinks",
                column: "LinkTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_ModuleID",
                table: "NavigationLinks",
                column: "ModuleID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_ParentNavigationLinkID",
                table: "NavigationLinks",
                column: "ParentNavigationLinkID");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_RoleID",
                table: "NavigationLinks",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_NoPayLeaves_NoPayLeaveTypeID",
                table: "NoPayLeaves",
                column: "NoPayLeaveTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_NoPayLeaves_OperationLogID",
                table: "NoPayLeaves",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_NoteReasonID",
                table: "Notes",
                column: "NoteReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_OperationLogID",
                table: "Notes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialTripDetails_TypeID",
                table: "OfficialTripDetails",
                column: "TypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialTripLocations_LeaveID",
                table: "OfficialTripLocations",
                column: "LeaveID");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialTripLocations_LocationID",
                table: "OfficialTripLocations",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAddressBases_AddressBaseTypeID",
                table: "OnlineAddressBases",
                column: "AddressBaseTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAddressBases_WardID",
                table: "OnlineAddressBases",
                column: "WardID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAddresses_CountryID",
                table: "OnlineAddresses",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAddresses_IslandID",
                table: "OnlineAddresses",
                column: "IslandID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAddressInstances_AddressInstanceTypeID",
                table: "OnlineAddressInstances",
                column: "AddressInstanceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAddressInstances_OnlineAddressBaseID",
                table: "OnlineAddressInstances",
                column: "OnlineAddressBaseID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineAreas_WardID",
                table: "OnlineAreas",
                column: "WardID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineBusinessEntityLocations_BusinessEntityID",
                table: "OnlineBusinessEntityLocations",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineBusinessEntityLocations_BusinessEntityLocationTypeID",
                table: "OnlineBusinessEntityLocations",
                column: "BusinessEntityLocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineBusinessEntityLocations_OnlineLocationID",
                table: "OnlineBusinessEntityLocations",
                column: "OnlineLocationID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLocations_LocationTypeID",
                table: "OnlineLocations",
                column: "LocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLocations_OperationLogID",
                table: "OnlineLocations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineSignInOrganisations_OperationLogID",
                table: "OnlineSignInOrganisations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_CreatedByIndividualID",
                table: "OperationLogs",
                column: "CreatedByIndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_CreatedByUserID",
                table: "OperationLogs",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_CreatedByUserOrganisationID",
                table: "OperationLogs",
                column: "CreatedByUserOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_OperationLogActionID",
                table: "OperationLogs",
                column: "OperationLogActionID");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_UpdatedContextID",
                table: "OperationLogs",
                column: "UpdatedContextID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationalStructureSAPExceptions_OrganisationStructureID",
                table: "OrganisationalStructureSAPExceptions",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationBudgetMonthlySummaries",
                table: "OrganisationBudgetMonthlySummaries",
                column: "BudgetItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationBudgetMonthlySummaries_1",
                table: "OrganisationBudgetMonthlySummaries",
                column: "ParentBudgetItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationBudgets_BudgetCodeID",
                table: "OrganisationBudgets",
                column: "BudgetCodeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationBudgets_OrganisationID",
                table: "OrganisationBudgets",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationDeductionTypes_DeductionTypeID",
                table: "OrganisationDeductionTypes",
                column: "DeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationDeductionTypes_OperationLogID",
                table: "OrganisationDeductionTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationDeductionTypes_OrganisationID",
                table: "OrganisationDeductionTypes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollItemTypes_OperationLogID",
                table: "OrganisationPayrollItemTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollItemTypes_OrganisationID",
                table: "OrganisationPayrollItemTypes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollItemTypes_PayrollItemTypeID",
                table: "OrganisationPayrollItemTypes",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollPeriodJobTypes_JobTypeID",
                table: "OrganisationPayrollPeriodJobTypes",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollPeriodJobTypes_OperationLogID",
                table: "OrganisationPayrollPeriodJobTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollPeriodJobTypes_OrganisationPayrollPeriodID",
                table: "OrganisationPayrollPeriodJobTypes",
                column: "OrganisationPayrollPeriodID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollPeriods_OperationLogID",
                table: "OrganisationPayrollPeriods",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationPayrollPeriods_OrganisationID",
                table: "OrganisationPayrollPeriods",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_CountryID",
                table: "Organisations",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_OrganisationTypeID",
                table: "Organisations",
                column: "OrganisationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_ParentOrganisationBusinessEntityID",
                table: "Organisations",
                column: "ParentOrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationSAPExceptions_OrganisationBusinessEntityID",
                table: "OrganisationSAPExceptions",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructure_OperationLogID",
                table: "OrganisationStructure",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructure_OrganisationBusinessEntityID",
                table: "OrganisationStructure",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructure_OrganisationStructureStateID",
                table: "OrganisationStructure",
                column: "OrganisationStructureStateID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructure_OrganisationStructureTypeID",
                table: "OrganisationStructure",
                column: "OrganisationStructureTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureCalendars_CalenderID",
                table: "OrganisationStructureCalendars",
                column: "CalenderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureCalendars_OperationLogID",
                table: "OrganisationStructureCalendars",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureCalendars_OrganisationStructureID",
                table: "OrganisationStructureCalendars",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureDrafts_OrganisationChartRequestID",
                table: "OrganisationStructureDrafts",
                column: "OrganisationChartRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureDrafts_OrganisationStructureID",
                table: "OrganisationStructureDrafts",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureDrafts_OrganisationStructureRequestTypeID",
                table: "OrganisationStructureDrafts",
                column: "OrganisationStructureRequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureDrafts_OrganisationStructureStateID",
                table: "OrganisationStructureDrafts",
                column: "OrganisationStructureStateID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureDrafts_OrganisationStructureTypeID",
                table: "OrganisationStructureDrafts",
                column: "OrganisationStructureTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureHeadIncharges_IndividualID",
                table: "OrganisationStructureHeadIncharges",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureHeadIncharges_OperationLogID",
                table: "OrganisationStructureHeadIncharges",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureHeadIncharges_OrganisationStructureID",
                table: "OrganisationStructureHeadIncharges",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureHistory_OrganisationID",
                table: "OrganisationStructureHistory",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureHistory_OrganisationStructureRelationID",
                table: "OrganisationStructureHistory",
                column: "OrganisationStructureRelationID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureHistory_RequestID",
                table: "OrganisationStructureHistory",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureLocations_LocationID",
                table: "OrganisationStructureLocations",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureLocations_OrganisationStructureID",
                table: "OrganisationStructureLocations",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureLocations_OrganisationStructureLocationTypeID",
                table: "OrganisationStructureLocations",
                column: "OrganisationStructureLocationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureRelations_OrganisationStructureID",
                table: "OrganisationStructureRelations",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureRelations_ParentOrganisationStructureID",
                table: "OrganisationStructureRelations",
                column: "ParentOrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureRequests_OrganisationBusinessEntityID",
                table: "OrganisationStructureRequests",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureRequests_RequestID",
                table: "OrganisationStructureRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureSchedules_OperationLogID",
                table: "OrganisationStructureSchedules",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureSchedules_OrganisationStructureID",
                table: "OrganisationStructureSchedules",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureSchedules_ScheduleID",
                table: "OrganisationStructureSchedules",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureSupervisors_OperationLogID",
                table: "OrganisationStructureSupervisors",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureSupervisors_OrganisationStructureID",
                table: "OrganisationStructureSupervisors",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationStructureSupervisors_SupervisorStaffID",
                table: "OrganisationStructureSupervisors",
                column: "SupervisorStaffID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationWorkPlanningSettings_OperationLogID",
                table: "OrganisationWorkPlanningSettings",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationWorkPlanningSettings_OrganisationBusinessEntityID",
                table: "OrganisationWorkPlanningSettings",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationWorkPlanningSettings_PlanningProviderID",
                table: "OrganisationWorkPlanningSettings",
                column: "PlanningProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_OT_EndAttendanceLogID",
                table: "OT",
                column: "EndAttendanceLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OT_OperationLogID",
                table: "OT",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OT_OTRequestID",
                table: "OT",
                column: "OTRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_OT_StartAttendanceLogID",
                table: "OT",
                column: "StartAttendanceLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OtherSalaryRateDefinitions_OperationLogID",
                table: "OtherSalaryRateDefinitions",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OtherSalaryRateDefinitions_SalaryRateDefinitionForTypeID",
                table: "OtherSalaryRateDefinitions",
                column: "SalaryRateDefinitionForTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OtherSalaryRateDefinitions_SalaryRateDefinitionID",
                table: "OtherSalaryRateDefinitions",
                column: "SalaryRateDefinitionID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPreApprovals_OTTypeID",
                table: "OTPreApprovals",
                column: "OTTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPreApprovals_StaffJobID",
                table: "OTPreApprovals",
                column: "StaffJobID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPreApprovals_SupervisorID",
                table: "OTPreApprovals",
                column: "SupervisorID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPreApprovalTimes_DayTypeID",
                table: "OTPreApprovalTimes",
                column: "DayTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPreApprovalTimes_OperationLogID",
                table: "OTPreApprovalTimes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPreApprovalTimes_RequestID",
                table: "OTPreApprovalTimes",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IDX_OTRequests_JOBID",
                table: "OTRequests",
                column: "StaffJobID");

            migrationBuilder.CreateIndex(
                name: "IX_OTRequests_OTPreApprovalRequestID",
                table: "OTRequests",
                column: "OTPreApprovalRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_OTRequests_OTTypeID",
                table: "OTRequests",
                column: "OTTypeID");

            migrationBuilder.CreateIndex(
                name: "NonClusteredIndex-20160804-141927",
                table: "OTRequests",
                column: "Date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_OutOfOfficeRequests_OrganisationBusinessEntityID",
                table: "OutOfOfficeRequests",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfOfficeRequests_OrganisationStructureID",
                table: "OutOfOfficeRequests",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfOfficeRequests_OutOfOfficeRequestTypeID",
                table: "OutOfOfficeRequests",
                column: "OutOfOfficeRequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfOfficeRequests_SupervisorStaffId",
                table: "OutOfOfficeRequests",
                column: "SupervisorStaffId");

            migrationBuilder.CreateIndex(
                name: "missing_index_16_15_OutOfOfficeRequests",
                table: "OutOfOfficeRequests",
                column: "StaffID")
                .Annotation("SqlServer:FillFactor", 75);

            migrationBuilder.CreateIndex(
                name: "IX_ParentCalenders_ParentCalenderID",
                table: "ParentCalenders",
                column: "ParentCalenderID");

            migrationBuilder.CreateIndex(
                name: "IX_Passports_BusinessEntityID",
                table: "Passports",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_Passports_CountryID",
                table: "Passports",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Passports_OperationLogID",
                table: "Passports",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Passports_PassportStateID",
                table: "Passports",
                column: "PassportStateID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollConfigurableValues_JobTypeID",
                table: "PayrollConfigurableValues",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollConfigurableValues_OrganisationID",
                table: "PayrollConfigurableValues",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollConfigurableValues_PayrollConfigurableValueTypeID",
                table: "PayrollConfigurableValues",
                column: "PayrollConfigurableValueTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycleLinkedRequests",
                table: "PayrollCycleLinkedRequests",
                columns: new[] { "PayrollCycleRequestID", "RequestID", "StaffSalaryID" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycleLinkedRequests_OperationLogID",
                table: "PayrollCycleLinkedRequests",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycleLinkedRequests_RequestID",
                table: "PayrollCycleLinkedRequests",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycleLinkedRequests_StaffSalaryID",
                table: "PayrollCycleLinkedRequests",
                column: "StaffSalaryID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_OrganisationPayrollPeriodID",
                table: "PayrollCycles",
                column: "OrganisationPayrollPeriodID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemDailySAPSheetDetails_OperationLogID",
                table: "PayrollItemDailySAPSheetDetails",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemDailySAPSheetDetails_PayrollItemTypeID",
                table: "PayrollItemDailySAPSheetDetails",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemDailySAPSheetDetails_StaffSalaryID",
                table: "PayrollItemDailySAPSheetDetails",
                column: "StaffSalaryID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemProcessingFlows_OperationLogID",
                table: "PayrollItemProcessingFlows",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemProcessingFlows_ParentPayrollItemTypeProcessingCodeID",
                table: "PayrollItemProcessingFlows",
                column: "ParentPayrollItemTypeProcessingCodeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemProcessingFlows_PayrollItemTypeProcessingCodeID",
                table: "PayrollItemProcessingFlows",
                column: "PayrollItemTypeProcessingCodeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemsProcessingWhiteLists_JobTypeID",
                table: "PayrollItemsProcessingWhiteLists",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemsProcessingWhiteLists_OperationLogID",
                table: "PayrollItemsProcessingWhiteLists",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemsProcessingWhiteLists_OwnerOrganisationID",
                table: "PayrollItemsProcessingWhiteLists",
                column: "OwnerOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemsProcessingWhiteLists_PayrollItemTypeID",
                table: "PayrollItemsProcessingWhiteLists",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeAmounts_OperationLogID",
                table: "PayrollItemTypeAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeAmounts_PayrollItemTypeID",
                table: "PayrollItemTypeAmounts",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeDependencyAmounts_OperationLogID",
                table: "PayrollItemTypeDependencyAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeDependencyAmounts_PayrollItemTypeID",
                table: "PayrollItemTypeDependencyAmounts",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeDependencyTimePeriodAmounts_OperationLogID",
                table: "PayrollItemTypeDependencyTimePeriodAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeDependencyTimePeriodAmounts_PayrollItemTypeID",
                table: "PayrollItemTypeDependencyTimePeriodAmounts",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeProcessingCodes_AdditionOrDeductionTypeID",
                table: "PayrollItemTypeProcessingCodes",
                column: "AdditionOrDeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeProcessingCodes_OperationLogID",
                table: "PayrollItemTypeProcessingCodes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeProcessingCodes_OwnerOrganisationBusinessEntityID",
                table: "PayrollItemTypeProcessingCodes",
                column: "OwnerOrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypeProcessingCodes_PayrollItemTypeID",
                table: "PayrollItemTypeProcessingCodes",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_AdditionOrDeductionTypeID",
                table: "PayrollItemTypes",
                column: "AdditionOrDeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_AmountTypeID",
                table: "PayrollItemTypes",
                column: "AmountTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_AttendanceDependentTypeID",
                table: "PayrollItemTypes",
                column: "AttendanceDependentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_CurrencyTypeID",
                table: "PayrollItemTypes",
                column: "CurrencyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_DependencyTypeID",
                table: "PayrollItemTypes",
                column: "DependencyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_DerivedOnTypeID",
                table: "PayrollItemTypes",
                column: "DerivedOnTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_OperationLogID",
                table: "PayrollItemTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_ownerOrganisationID",
                table: "PayrollItemTypes",
                column: "ownerOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_RecurrenceTypeID",
                table: "PayrollItemTypes",
                column: "RecurrenceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_RequestID",
                table: "PayrollItemTypes",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItemTypes_SAPSheetID",
                table: "PayrollItemTypes",
                column: "SAPSheetID");

            migrationBuilder.CreateIndex(
                name: "IX_PercentageVariableDeductionAmounts_DeductionTypeID",
                table: "PercentageVariableDeductionAmounts",
                column: "DeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PercentageVariableDeductionAmounts_OperationLogID",
                table: "PercentageVariableDeductionAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningProviders_OrganisationBusinessEntityID_Code",
                table: "PlanningProviders",
                columns: new[] { "OrganisationBusinessEntityID", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_PlanningProviders_Code",
                table: "PlanningProviders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionBasicSalaries_OperationLogID",
                table: "PositionBasicSalaries",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_PositionBasicSalaries_PositionID",
                table: "PositionBasicSalaries",
                column: "PositionID");

            migrationBuilder.CreateIndex(
                name: "IX_PositionClassifications_OperationLogId",
                table: "PositionClassifications",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionClassifications_OrganisationBusinessEntityID",
                table: "PositionClassifications",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRanks_OperationLogId",
                table: "PositionRanks",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRanks_OrganisationBusinessEntityID",
                table: "PositionRanks",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OperationLogId",
                table: "Positions",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OrganisationID",
                table: "Positions",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionClassificationID",
                table: "Positions",
                column: "PositionClassificationID");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionRankID",
                table: "Positions",
                column: "PositionRankID");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionTypeID",
                table: "Positions",
                column: "PositionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PrimaryPayrollItemTypeSAPWageTypes_Unique_PayrollItemTypeID",
                table: "PrimaryPayrollItemTypeSAPWageTypes",
                column: "PayrollItemTypeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestCheckListVerifications_CheckListItemID",
                table: "RequestCheckListVerifications",
                column: "CheckListItemID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestCheckListVerifications_OperationLogID",
                table: "RequestCheckListVerifications",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestCheckListVerifications_RequestID",
                table: "RequestCheckListVerifications",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_DocumentID",
                table: "RequestDocuments",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_OperationLogID",
                table: "RequestDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocuments_RequestID",
                table: "RequestDocuments",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocumentVerifications_OperationLogID",
                table: "RequestDocumentVerifications",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDocumentVerifications_RequestDocumentID",
                table: "RequestDocumentVerifications",
                column: "RequestDocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestJobs",
                table: "RequestJobs",
                columns: new[] { "RequestID", "JobID" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestJobs_JobID",
                table: "RequestJobs",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestJobs_OperationLogID",
                table: "RequestJobs",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "ix_IndexName_10_MARCH",
                table: "Requests",
                column: "RequestStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_LastStateChangedByUserID",
                table: "Requests",
                column: "LastStateChangedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_OperationLogID",
                table: "Requests",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestType_RequestState",
                table: "Requests",
                columns: new[] { "OrganisationBusinessEntityID", "RequestTypeID", "RequestStateID", "ApplicationDate" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ServiceID",
                table: "Requests",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "missing_index_4_3_Requests",
                table: "Requests",
                column: "RequestTypeID")
                .Annotation("SqlServer:FillFactor", 75);

            migrationBuilder.CreateIndex(
                name: "IX_RequestTeams",
                table: "RequestTeams",
                columns: new[] { "RequestID", "TeamID" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTeams_OperationLogID",
                table: "RequestTeams",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTeams_TeamID",
                table: "RequestTeams",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_SequenceNumberTypeID",
                table: "RequestTypes",
                column: "SequenceNumberTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_ServiceTypeID",
                table: "RequestTypes",
                column: "ServiceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesAllowedRequestStateTransitions_FromRequestStateID",
                table: "RequestTypesAllowedRequestStateTransitions",
                column: "FromRequestStateID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesAllowedRequestStateTransitions_ToRequestStateID",
                table: "RequestTypesAllowedRequestStateTransitions",
                column: "ToRequestStateID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeSpecificCheckListItems_RequestTypeID",
                table: "RequestTypeSpecificCheckListItems",
                column: "RequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeSpecificDocumentTypes_ContextID",
                table: "RequestTypeSpecificDocumentTypes",
                column: "ContextID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeSpecificDocumentTypes_DocumentTypeID",
                table: "RequestTypeSpecificDocumentTypes",
                column: "DocumentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypeSpecificDocumentTypes_RequestTypeID",
                table: "RequestTypeSpecificDocumentTypes",
                column: "RequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesStatesRequiredRoles",
                table: "RequestTypesStatesRequiredRoles",
                columns: new[] { "RequestStateID", "RequestTypeID", "RoleID" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesStatesRequiredRoles_RoleID",
                table: "RequestTypesStatesRequiredRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesStatesRequiredRolesForProcessing_RequestStateID",
                table: "RequestTypesStatesRequiredRolesForProcessing",
                column: "RequestStateID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesStatesRequiredRolesForProcessing_RoleID",
                table: "RequestTypesStatesRequiredRolesForProcessing",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypesStatesRequiredRolesForProcessing_ToRequestStateID",
                table: "RequestTypesStatesRequiredRolesForProcessing",
                column: "ToRequestStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ContextID",
                table: "Roles",
                column: "ContextID");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ModuleID",
                table: "Roles",
                column: "ModuleID");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_OperationLogID",
                table: "Roles",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRateDefinitions_JobTypeID",
                table: "SalaryRateDefinitions",
                column: "JobTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRateDefinitions_OperationLogID",
                table: "SalaryRateDefinitions",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRateDefinitions_RequestID",
                table: "SalaryRateDefinitions",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_SAPExceptions_OperationLogID",
                table: "SAPExceptions",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_SAPExceptions_PayrollItemTypeID",
                table: "SAPExceptions",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SAPExceptions_SAPExceptionActionTypeID",
                table: "SAPExceptions",
                column: "SAPExceptionActionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SAPExceptions_SAPExceptionTypeID",
                table: "SAPExceptions",
                column: "SAPExceptionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SAPExceptions_SAPWageTypeID",
                table: "SAPExceptions",
                column: "SAPWageTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SAPWageTypes",
                table: "SAPWageTypes",
                column: "WageTypeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SAPWageTypes_SheetID",
                table: "SAPWageTypes",
                column: "SheetID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleElements_ElementID",
                table: "ScheduleElements",
                column: "ElementID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleElements_OperationLogID",
                table: "ScheduleElements",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleElements_ScheduleID",
                table: "ScheduleElements",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleOwners_OperationLogID",
                table: "ScheduleOwners",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleOwners_OwnerTypeID",
                table: "ScheduleOwners",
                column: "OwnerTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleOwners_ScheduleID",
                table: "ScheduleOwners",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_OperationLogID",
                table: "Schedules",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ScheduleTypeID",
                table: "Schedules",
                column: "ScheduleTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCheckListVerifications_CheckListItemID",
                table: "ServiceCheckListVerifications",
                column: "CheckListItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCheckListVerifications_OprationLogID",
                table: "ServiceCheckListVerifications",
                column: "OprationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCheckListVerifications_ServiceID",
                table: "ServiceCheckListVerifications",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocuments_DocumentID",
                table: "ServiceDocuments",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocuments_OperationLogID",
                table: "ServiceDocuments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocuments_ServiceID",
                table: "ServiceDocuments",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocumentVerifications_OperationLogID",
                table: "ServiceDocumentVerifications",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDocumentVerifications_ServiceDocumentID",
                table: "ServiceDocumentVerifications",
                column: "ServiceDocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_OperationLogID",
                table: "Services",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceStateID",
                table: "Services",
                column: "ServiceStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceTypeID",
                table: "Services",
                column: "ServiceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypes_SequenceNumberTypeID",
                table: "ServiceTypes",
                column: "SequenceNumberTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypesAllowedServiceStateTransitions_ToServiceStateID",
                table: "ServiceTypesAllowedServiceStateTransitions",
                column: "ToServiceStateID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypeSpecificCheckListItems_ServiceTypeID",
                table: "ServiceTypeSpecificCheckListItems",
                column: "ServiceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleDays_OperationLogID",
                table: "ShiftScheduleDays",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleDays_ScheduleID",
                table: "ShiftScheduleDays",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleDays_SpecialDayTypeID",
                table: "ShiftScheduleDays",
                column: "SpecialDayTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleDays_WorkShiftID",
                table: "ShiftScheduleDays",
                column: "WorkShiftID");

            migrationBuilder.CreateIndex(
                name: "IX_SickLeaveForms_OperationLogID",
                table: "SickLeaveForms",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDays_OperationLogID",
                table: "SpecialDays",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDayShifts_SpecialDayID",
                table: "SpecialDayShifts",
                column: "SpecialDayID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDayTypes_DayTypeID",
                table: "SpecialDayTypes",
                column: "DayTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDayTypes_OperationLogID",
                table: "SpecialDayTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDayTypes_OrganisationID",
                table: "SpecialDayTypes",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDayTypes_OrganisationStructureID",
                table: "SpecialDayTypes",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductionAmounts_OperationLogID",
                table: "StaffAssignedDeductionAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductionAmounts_StaffAssignedDeductionID",
                table: "StaffAssignedDeductionAmounts",
                column: "StaffAssignedDeductionID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductions_CurrencyTypeID",
                table: "StaffAssignedDeductions",
                column: "CurrencyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductions_DeductionTypeID",
                table: "StaffAssignedDeductions",
                column: "DeductionTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductions_JobID",
                table: "StaffAssignedDeductions",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductions_OperationLogID",
                table: "StaffAssignedDeductions",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignedDeductions_RequestID",
                table: "StaffAssignedDeductions",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyAttandanceSummaryIssues_PayrollCycleProcessingStateID",
                table: "StaffDailyAttandanceSummaryIssues",
                column: "PayrollCycleProcessingStateID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyAttandanceSummaryIssues_StaffDailyAttandanceSummaryIssueTypeId",
                table: "StaffDailyAttandanceSummaryIssues",
                column: "StaffDailyAttandanceSummaryIssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyAttandanceSummaryIssues_StaffDailyAttendanceSummeryID",
                table: "StaffDailyAttandanceSummaryIssues",
                column: "StaffDailyAttendanceSummeryID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyAttendanceSummeries_JobPositionID",
                table: "StaffDailyAttendanceSummeries",
                column: "JobPositionID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyAttendanceSummeries_OperationLogID",
                table: "StaffDailyAttendanceSummeries",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDailyAttendanceSummeries_StaffSalaryID",
                table: "StaffDailyAttendanceSummeries",
                column: "StaffSalaryID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffEnrollmentNumbers_IndividualID",
                table: "StaffEnrollmentNumbers",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffEnrollmentNumbers_OperationLogID",
                table: "StaffEnrollmentNumbers",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffEnrollmentNumbers_OrganisationID",
                table: "StaffEnrollmentNumbers",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemDetails_StaffPayrollItemTypeID",
                table: "StaffPayrollItemDetails",
                column: "StaffPayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypeAmounts_OperationLogID",
                table: "StaffPayrollItemTypeAmounts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypeAmounts_StaffPayrollItemTypeID",
                table: "StaffPayrollItemTypeAmounts",
                column: "StaffPayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypes",
                table: "StaffPayrollItemTypes",
                columns: new[] { "PayrollItemTypeID", "EffectiveStartDate", "EndDate", "IsValid", "JobID" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypes_JobID",
                table: "StaffPayrollItemTypes",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypes_OperationLogID",
                table: "StaffPayrollItemTypes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypes_RequestID",
                table: "StaffPayrollItemTypes",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypeSchedules_OperationLogID",
                table: "StaffPayrollItemTypeSchedules",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypeSchedules_PayrollCycleRequestID",
                table: "StaffPayrollItemTypeSchedules",
                column: "PayrollCycleRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPayrollItemTypeSchedules_StaffPayrollItemTypeID",
                table: "StaffPayrollItemTypeSchedules",
                column: "StaffPayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_OperationLogID",
                table: "Staffs",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaries_JobID",
                table: "StaffSalaries",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaries_OperationLogID",
                table: "StaffSalaries",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaries_PayrollCycleRequestID",
                table: "StaffSalaries",
                column: "PayrollCycleRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryDeductions_OperationLogID",
                table: "StaffSalaryDeductions",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryDeductions_StaffAssignedDeductionID",
                table: "StaffSalaryDeductions",
                column: "StaffAssignedDeductionID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryDeductions_StaffSalaryID",
                table: "StaffSalaryDeductions",
                column: "StaffSalaryID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryPayrollItems_OperationLogID",
                table: "StaffSalaryPayrollItems",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryPayrollItems_PayrollItemTypeID",
                table: "StaffSalaryPayrollItems",
                column: "PayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryPayrollItems_StaffPayrollItemTypeID",
                table: "StaffSalaryPayrollItems",
                column: "StaffPayrollItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSalaryPayrollItems_StaffSalaryID",
                table: "StaffSalaryPayrollItems",
                column: "StaffSalaryID");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OperationLogID",
                table: "Teams",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OrganisationID",
                table: "Teams",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaffs_StaffID",
                table: "TeamStaffs",
                column: "StaffID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStaffs_TeamID",
                table: "TeamStaffs",
                column: "TeamID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignedUserGroups_OperationLogId",
                table: "UserAssignedUserGroups",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignedUserGroups_UserID",
                table: "UserAssignedUserGroups",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupRoles_OperationLogID",
                table: "UserGroupRoles",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupRoles_RoleID",
                table: "UserGroupRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_OperationLogID",
                table: "UserGroups",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UserOrganisationID",
                table: "UserGroups",
                column: "UserOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisationRoles_OperationLogID",
                table: "UserOrganisationRoles",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisationRoles_OrganisationBusinessEntityID",
                table: "UserOrganisationRoles",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisationRoles_RoleID",
                table: "UserOrganisationRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisations_BusinessEntityID",
                table: "UserOrganisations",
                column: "BusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisations_OperationLogID",
                table: "UserOrganisations",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisationUserGroups_OperationLogID",
                table: "UserOrganisationUserGroups",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganisationUserGroups_UserGroupID",
                table: "UserOrganisationUserGroups",
                column: "UserGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_OperationLogID",
                table: "UserPreferences",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_SelectedContextID",
                table: "UserPreferences",
                column: "SelectedContextID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_SelectedLanguageID",
                table: "UserPreferences",
                column: "SelectedLanguageID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_LookUpByUserAndUserOrganisation",
                table: "UserRoles",
                columns: new[] { "UserID", "UserOrganisationID" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_OperationLogID",
                table: "UserRoles",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserOrganisationID",
                table: "UserRoles",
                column: "UserOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_UserName",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LocalUserStateID",
                table: "Users",
                column: "LocalUserStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OperationLogID",
                table: "Users",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RequestID",
                table: "Users",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SSOUserStateID",
                table: "Users",
                column: "SSOUserStateID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserOrganisationID",
                table: "Users",
                column: "UserOrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserPreferenceID",
                table: "Users",
                column: "UserPreferenceID");

            migrationBuilder.CreateIndex(
                name: "IX_UserServiceRoles_OperationLogID",
                table: "UserServiceRoles",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_UserServiceRoles_ServiceID",
                table: "UserServiceRoles",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_UserServiceRoles_UserID",
                table: "UserServiceRoles",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_IslandID",
                table: "Wards",
                column: "IslandID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentOwners_AssignedByUserID",
                table: "WorkAssignmentOwners",
                column: "AssignedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentOwners_AssignmentID_CurrentOwner",
                table: "WorkAssignmentOwners",
                columns: new[] { "WorkAssignmentID", "IsCurrentOwner" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentOwners_IndividualID_EffectiveDates",
                table: "WorkAssignmentOwners",
                columns: new[] { "IndividualID", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentOwners_JobID",
                table: "WorkAssignmentOwners",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentOwners_OperationLogID",
                table: "WorkAssignmentOwners",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentOwners_RelievedByUserID",
                table: "WorkAssignmentOwners",
                column: "RelievedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_CancelledByUserID",
                table: "WorkAssignments",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_CreatedByUserID",
                table: "WorkAssignments",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_OperationLogID",
                table: "WorkAssignments",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_WorkAssignmentStateID",
                table: "WorkAssignments",
                column: "WorkAssignmentStateID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_WorkPlanID",
                table: "WorkAssignments",
                column: "WorkPlanID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_WorkTemplateID",
                table: "WorkAssignments",
                column: "WorkTemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_WorkTemplateTypeID",
                table: "WorkAssignments",
                column: "WorkTemplateTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentSegments_WorkAssignmentID",
                table: "WorkAssignmentSegments",
                column: "WorkAssignmentID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentSegments_WorkSegmentTypeID",
                table: "WorkAssignmentSegments",
                column: "WorkSegmentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentSegments_WorkTemplateSegmentID",
                table: "WorkAssignmentSegments",
                column: "WorkTemplateSegmentID");

            migrationBuilder.CreateIndex(
                name: "UX_WorkAssignmentStates_Code",
                table: "WorkAssignmentStates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_ApprovedByUserID",
                table: "WorkAssignmentTransfers",
                column: "ApprovedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_AssignmentID_StateID",
                table: "WorkAssignmentTransfers",
                columns: new[] { "WorkAssignmentID", "WorkAssignmentTransferStateID" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_FromIndividualID",
                table: "WorkAssignmentTransfers",
                column: "FromIndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_FromJobID",
                table: "WorkAssignmentTransfers",
                column: "FromJobID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_FromWorkAssignmentOwnerID",
                table: "WorkAssignmentTransfers",
                column: "FromWorkAssignmentOwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_OperationLogID",
                table: "WorkAssignmentTransfers",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_RejectedByUserID",
                table: "WorkAssignmentTransfers",
                column: "RejectedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_RequestedByUserID",
                table: "WorkAssignmentTransfers",
                column: "RequestedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_RequestID",
                table: "WorkAssignmentTransfers",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_ToIndividualID_EffectiveFrom",
                table: "WorkAssignmentTransfers",
                columns: new[] { "ToIndividualID", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_ToJobID",
                table: "WorkAssignmentTransfers",
                column: "ToJobID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_ToWorkAssignmentOwnerID",
                table: "WorkAssignmentTransfers",
                column: "ToWorkAssignmentOwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignmentTransfers_WorkAssignmentTransferStateID",
                table: "WorkAssignmentTransfers",
                column: "WorkAssignmentTransferStateID");

            migrationBuilder.CreateIndex(
                name: "UX_WorkAssignmentTransferStates_Code",
                table: "WorkAssignmentTransferStates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkingDays_OperationLogID",
                table: "WorkingDays",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingDayShifts_WorkingDayID",
                table: "WorkingDayShifts",
                column: "WorkingDayID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_FinalizedByUserID",
                table: "WorkPlans",
                column: "FinalizedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_GeneratedByUserID",
                table: "WorkPlans",
                column: "GeneratedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_IndividualID",
                table: "WorkPlans",
                column: "IndividualID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_OperationLogID",
                table: "WorkPlans",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_OrganisationBusinessEntityID",
                table: "WorkPlans",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_PlanningProviderID",
                table: "WorkPlans",
                column: "PlanningProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlans_WorkTemplateId",
                table: "WorkPlans",
                column: "WorkTemplateId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkPlans_Job_WorkDate",
                table: "WorkPlans",
                columns: new[] { "JobID", "WorkDate" },
                unique: true,
                filter: "[IsValid] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanSegments_WorkPlanId",
                table: "WorkPlanSegments",
                column: "WorkPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanSegments_WorkSegmentTypeId",
                table: "WorkPlanSegments",
                column: "WorkSegmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPlanSegments_WorkTemplateSegmentId",
                table: "WorkPlanSegments",
                column: "WorkTemplateSegmentId");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkSegmentTypes_Code",
                table: "WorkSegmentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_OperationLogID",
                table: "WorkShifts",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_OrganisationID",
                table: "WorkShifts",
                column: "OrganisationID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShifts_OrganisationStructureID",
                table: "WorkShifts",
                column: "OrganisationStructureID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShiftsBreakTimes_BreakTimeID",
                table: "WorkShiftsBreakTimes",
                column: "BreakTimeID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShiftsBreakTimes_OperationLogID",
                table: "WorkShiftsBreakTimes",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkShiftsBreakTimes_WorkShiftID",
                table: "WorkShiftsBreakTimes",
                column: "WorkShiftID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTemplates_OperationLogID",
                table: "WorkTemplates",
                column: "OperationLogID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTemplates_OrganisationBusinessEntityID",
                table: "WorkTemplates",
                column: "OrganisationBusinessEntityID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTemplates_WorkTemplateTypeID",
                table: "WorkTemplates",
                column: "WorkTemplateTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTemplateSegments_WorkSegmentTypeID",
                table: "WorkTemplateSegments",
                column: "WorkSegmentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTemplateSegments_WorkTemplateID",
                table: "WorkTemplateSegments",
                column: "WorkTemplateID");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkTemplateTypes_Code",
                table: "WorkTemplateTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Locations",
                table: "Addresses",
                column: "LocationID",
                principalTable: "Locations",
                principalColumn: "LocationID");

            migrationBuilder.AddForeignKey(
                name: "FK_AggregatedSalaries_StaffSalaries",
                table: "AggregatedSalaries",
                column: "StaffSalaryID",
                principalTable: "StaffSalaries",
                principalColumn: "StaffSalaryID");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignedWorkTypes_Jobs",
                table: "AssignedWorkTypes",
                column: "JobID",
                principalTable: "Jobs",
                principalColumn: "JobID");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignedWorkTypes_Organisations",
                table: "AssignedWorkTypes",
                column: "OrganisationID",
                principalTable: "Organisations",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachedBreakTimes_OperationLogs",
                table: "AttachedBreakTimes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachedBreakTimesDays_BreakTimes",
                table: "AttachedBreakTimesDays",
                column: "BreakTimeID",
                principalTable: "BreakTimes",
                principalColumn: "BreakTimeID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachedBreakTimesDays_OperationLogs",
                table: "AttachedBreakTimesDays",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachedPayrollItemsProcessingWhiteLists_OperationLogs",
                table: "AttachedPayrollItemsProcessingWhiteLists",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachedPayrollItemsProcessingWhiteLists_Organisations",
                table: "AttachedPayrollItemsProcessingWhiteLists",
                column: "OrganisationID",
                principalTable: "Organisations",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachedPayrollItemsProcessingWhiteLists_PayrollItemsProcessingWhiteLists",
                table: "AttachedPayrollItemsProcessingWhiteLists",
                column: "PayrollItemsProcessingWhiteListID",
                principalTable: "PayrollItemsProcessingWhiteLists",
                principalColumn: "PayrollItemsProcessingWhiteListID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceClientInstances_OperationLogs",
                table: "AttendanceClientInstances",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceClients_OrganisationStructure1",
                table: "AttendanceClientInstances",
                column: "OrganisationStructureID",
                principalTable: "OrganisationStructure",
                principalColumn: "OrganisationStructureID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceClients_Organisations1",
                table: "AttendanceClientInstances",
                column: "OrganisationID",
                principalTable: "Organisations",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceDevices_OperationLogs",
                table: "AttendanceDevices",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceDeviceStaffs_Staffs",
                table: "AttendanceDeviceStaffs",
                column: "IndividualID",
                principalTable: "Staffs",
                principalColumn: "IndividualID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogRequests_AttendanceLogs",
                table: "AttendanceLogChangeRequests",
                column: "AttendanceLogID",
                principalTable: "AttendanceLogs",
                principalColumn: "AttendanceLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogRequests_Requests",
                table: "AttendanceLogChangeRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualAttendanceLogRequests_AttendanceLogs",
                table: "AttendanceLogRequests",
                column: "AttendanceLogID",
                principalTable: "AttendanceLogs",
                principalColumn: "AttendanceLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualAttendanceLogRequests_Requests",
                table: "AttendanceLogRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogResolutions_AttendanceLogs_AttendanceLogID",
                table: "AttendanceLogResolutions",
                column: "AttendanceLogID",
                principalTable: "AttendanceLogs",
                principalColumn: "AttendanceLogID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogResolutions_WorkAssignmentSegments_WorkAssignmentSegmentID",
                table: "AttendanceLogResolutions",
                column: "WorkAssignmentSegmentID",
                principalTable: "WorkAssignmentSegments",
                principalColumn: "WorkAssignmentSegmentID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogResolutions_WorkAssignments_WorkAssignmentID",
                table: "AttendanceLogResolutions",
                column: "WorkAssignmentID",
                principalTable: "WorkAssignments",
                principalColumn: "WorkAssignmentID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogResolutions_WorkPlans_WorkPlanID",
                table: "AttendanceLogResolutions",
                column: "WorkPlanID",
                principalTable: "WorkPlans",
                principalColumn: "WorkPlanID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogs_OperationLogs",
                table: "AttendanceLogs",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogs_OrganisationStructure",
                table: "AttendanceLogs",
                column: "OrganisationStructureID",
                principalTable: "OrganisationStructure",
                principalColumn: "OrganisationStructureID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogs_Organisations",
                table: "AttendanceLogs",
                column: "OrganisationID",
                principalTable: "Organisations",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogs_Staffs",
                table: "AttendanceLogs",
                column: "IndividualID",
                principalTable: "Staffs",
                principalColumn: "IndividualID");

            migrationBuilder.AddForeignKey(
                name: "FK_BasicSalaries_OperationLogs",
                table: "BasicSalaries",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BreakTimes_OperationLogs",
                table: "BreakTimes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BreakTimes_OrganisationStructure",
                table: "BreakTimes",
                column: "OrganisationStructureID",
                principalTable: "OrganisationStructure",
                principalColumn: "OrganisationStructureID");

            migrationBuilder.AddForeignKey(
                name: "FK_BreakTimes_Organisations",
                table: "BreakTimes",
                column: "OrganisationID",
                principalTable: "Organisations",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetTransactions_Organisations",
                table: "BudgetTransactions",
                column: "OrganisationID",
                principalTable: "Organisations",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_BulkUploadedDocuments_Documents",
                table: "BulkUploadedDocuments",
                column: "DocumentID",
                principalTable: "Documents",
                principalColumn: "DocumentID");

            migrationBuilder.AddForeignKey(
                name: "FK_BulkUploadedDocuments_OperationLogs",
                table: "BulkUploadedDocuments",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BulkUploadedDocuments_Users",
                table: "BulkUploadedDocuments",
                column: "UploadedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntities_LastStateChangedByUser",
                table: "BusinessEntities",
                column: "LastStateChangedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntities_VerifiedByUser",
                table: "BusinessEntities",
                column: "VerifiedBy",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntities_OperationLogs",
                table: "BusinessEntities",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntitiesDocuments_Documents",
                table: "BusinessEntitiesDocuments",
                column: "DocumentID",
                principalTable: "Documents",
                principalColumn: "DocumentID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntitiesDocuments_OperationLogs",
                table: "BusinessEntitiesDocuments",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityCalendars_OperationLogs",
                table: "BusinessEntityCalendars",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityCalenders_Calenders",
                table: "BusinessEntityCalendars",
                column: "CalenderID",
                principalTable: "Calendars",
                principalColumn: "CalenderID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityLocations_Locations",
                table: "BusinessEntityLocations",
                column: "LocationID",
                principalTable: "Locations",
                principalColumn: "LocationID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityRelationAssignedRoles_BusinessEntityRelations",
                table: "BusinessEntityRelationAssignedRoles",
                column: "BusinessEntityRelationID",
                principalTable: "BusinessEntityRelations",
                principalColumn: "BusinessEntityRelationID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityRelationAssignedRoles_Roles",
                table: "BusinessEntityRelationAssignedRoles",
                column: "RoleID",
                principalTable: "Roles",
                principalColumn: "RoleID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityRelations_OperationLogs",
                table: "BusinessEntityRelations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntityRequests_Requests",
                table: "BusinessEntityRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntitySchedules_OperationLogs",
                table: "BusinessEntitySchedules",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessEntitySchedules_Schedules",
                table: "BusinessEntitySchedules",
                column: "ScheduleID",
                principalTable: "Schedules",
                principalColumn: "ScheduleID");

            migrationBuilder.AddForeignKey(
                name: "FK_BussinessEntityContactInformations_OperationLogs",
                table: "BussinessEntityContactInformations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarElements_Calendars",
                table: "CalendarElements",
                column: "CalendarID",
                principalTable: "Calendars",
                principalColumn: "CalenderID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarElements_Elements",
                table: "CalendarElements",
                column: "ElementID",
                principalTable: "Elements",
                principalColumn: "ElementID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarInstances_Calendars",
                table: "CalendarInstances",
                column: "CalendarID",
                principalTable: "Calendars",
                principalColumn: "CalenderID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarInstances_OperationLogs",
                table: "CalendarInstances",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarMonths_Months",
                table: "CalendarMonths",
                column: "MonthID",
                principalTable: "Months",
                principalColumn: "MonthID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarMonths_OperationLogs",
                table: "CalendarMonths",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendars_CalendarTypes",
                table: "Calendars",
                column: "CalendarTypeID",
                principalTable: "CalendarTypes",
                principalColumn: "CalendarTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Calendars_OperationLogs",
                table: "Calendars",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarSchedules_Schedules",
                table: "CalendarSchedules",
                column: "ScheduleID",
                principalTable: "Schedules",
                principalColumn: "ScheduleID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarVariants_OperationLogs",
                table: "CalendarTypes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckListItems_OperationLogs",
                table: "CheckListItems",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyExchangeRates_OperationLogs",
                table: "CurrencyExchangeRates",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_DataCorrectionRequestAttributeValues_DataCorrectionRequests",
                table: "DataCorrectionRequestAttributeValues",
                column: "DataCorrectionRequestID",
                principalTable: "DataCorrectionRequests",
                principalColumn: "DataCorrectionRequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DataCorrectionRequests_OperationLogs",
                table: "DataCorrectionRequests",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_DataCorrectionRequests_Requests",
                table: "DataCorrectionRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DataCorrectionRequests_Services",
                table: "DataCorrectionRequests",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_DataCorrectionRequests_Users1",
                table: "DataCorrectionRequests",
                column: "LastUpdatedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_DataCorrectionRequests_Users2",
                table: "DataCorrectionRequests",
                column: "LastStateChangedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeductionTypeAmounts_DeductionTypes",
                table: "DeductionTypeAmounts",
                column: "DeductionTypeID",
                principalTable: "DeductionTypes",
                principalColumn: "DeductionTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeductionTypeAmounts_OperationLogs",
                table: "DeductionTypeAmounts",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_DeductionTypes_Requests",
                table: "DeductionTypes",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_DerivedOnPayrollItemTypes_OperationLogs",
                table: "DerivedOnPayrollItemTypes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_DerivedOnPayrollItemTypes_PayrollItemTypes",
                table: "DerivedOnPayrollItemTypes",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_DerivedOnPayrollItemTypes_PayrollItemTypes1",
                table: "DerivedOnPayrollItemTypes",
                column: "OtherPayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_OperationLogs",
                table: "Documents",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Elements_OperationLogs",
                table: "Elements",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleElements_SpecialDayTypes",
                table: "Elements",
                column: "SpecialDayTypeID",
                principalTable: "SpecialDayTypes",
                principalColumn: "SpecialDayTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleElements_WorkShifts",
                table: "Elements",
                column: "WorkShiftID",
                principalTable: "WorkShifts",
                principalColumn: "WorkShiftID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupConfigurableValues_Groups",
                table: "GroupConfigurableValues",
                column: "GroupID",
                principalTable: "Groups",
                principalColumn: "GroupID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupLeaveSets_Groups",
                table: "GroupLeaveSets",
                column: "GroupID",
                principalTable: "Groups",
                principalColumn: "GroupID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupLeaveSets_LeaveSets",
                table: "GroupLeaveSets",
                column: "LeaveSetID",
                principalTable: "LeaveSets",
                principalColumn: "LeaveSetID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupLeaveSets_OperationLogs",
                table: "GroupLeaveSets",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_OperationLogs",
                table: "Groups",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_OrganisationStructure",
                table: "Groups",
                column: "OrganisationStructureID",
                principalTable: "OrganisationStructure",
                principalColumn: "OrganisationStructureID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupSchedules_OperationLogs",
                table: "GroupSchedules",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupSchedules_Schedules",
                table: "GroupSchedules",
                column: "ScheduleID",
                principalTable: "Schedules",
                principalColumn: "ScheduleID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupStaffs_OperationLogs",
                table: "GroupStaffs",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupStaffs_Staffs",
                table: "GroupStaffs",
                column: "StaffIndividualID",
                principalTable: "Staffs",
                principalColumn: "IndividualID");

            migrationBuilder.AddForeignKey(
                name: "FK_IDCards_OperationLogs",
                table: "IDCards",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobLeaves_Jobs",
                table: "JobLeaveTypes",
                column: "JobID",
                principalTable: "Jobs",
                principalColumn: "JobID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobLeaves_LeaveTypes",
                table: "JobLeaveTypes",
                column: "LeaveTypeID",
                principalTable: "LeaveTypes",
                principalColumn: "LeaveTypeID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobLeaves_OperationLogs",
                table: "JobLeaveTypes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositionBasicSalaries_JobPositions",
                table: "JobPositionBasicSalaries",
                column: "JobPoistionID",
                principalTable: "JobPositions",
                principalColumn: "JobPositionID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositionBasicSalaries_OperationLogs",
                table: "JobPositionBasicSalaries",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositions_Jobs",
                table: "JobPositions",
                column: "JobID",
                principalTable: "Jobs",
                principalColumn: "JobID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositions_OperationLogs",
                table: "JobPositions",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositions_Positions",
                table: "JobPositions",
                column: "PositionID",
                principalTable: "Positions",
                principalColumn: "PositionID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositionsRequests_OperationLogs",
                table: "JobPositionsRequests",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPositionRequests_Requests",
                table: "JobRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRequests_Jobs",
                table: "JobRequests",
                column: "JobID",
                principalTable: "Jobs",
                principalColumn: "JobID");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_JobTypes",
                table: "Jobs",
                column: "JobTypeID",
                principalTable: "JobTypes",
                principalColumn: "JobTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_OrganisationStructure",
                table: "Jobs",
                column: "OrganisationStructureID",
                principalTable: "OrganisationStructure",
                principalColumn: "OrganisationStructureID");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Services",
                table: "Jobs",
                column: "ServiceID",
                principalTable: "Services",
                principalColumn: "ServiceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Staffs",
                table: "Jobs",
                column: "IndividualID",
                principalTable: "Staffs",
                principalColumn: "IndividualID");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Users_IndividualID",
                table: "Jobs",
                column: "IndividualID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSAPExceptions_SAPExceptions",
                table: "JobSAPExceptions",
                column: "SAPExceptionID",
                principalTable: "SAPExceptions",
                principalColumn: "SAPExceptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobTypes_OperationLogs",
                table: "JobTypes",
                column: "OperationLogId",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobTypesExceptions_SAPExceptions",
                table: "JobTypesExceptions",
                column: "SAPExceptionID",
                principalTable: "SAPExceptions",
                principalColumn: "SAPExceptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_JobWorkTemplateAssignments_WorkTemplates_WorkTemplateID",
                table: "JobWorkTemplateAssignments",
                column: "WorkTemplateID",
                principalTable: "WorkTemplates",
                principalColumn: "WorkTemplateID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobWorkTemplates_WorkTemplates_WorkTemplateId",
                table: "JobWorkTemplates",
                column: "WorkTemplateId",
                principalTable: "WorkTemplates",
                principalColumn: "WorkTemplateID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHolders_OperationLogs",
                table: "KeyHolders",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHolders_Staffs",
                table: "KeyHolders",
                column: "IndividualID",
                principalTable: "Staffs",
                principalColumn: "IndividualID");

            migrationBuilder.AddForeignKey(
                name: "FK_KPIDocuments_OperationLogs",
                table: "KPIDocuments",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_KPIDocuments_Users",
                table: "KPIDocuments",
                column: "LinkedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveChangeRequests_Leaves",
                table: "LeaveChangeRequests",
                column: "LeaveID",
                principalTable: "Leaves",
                principalColumn: "LeaveID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveChangeRequests_OperationLogs",
                table: "LeaveChangeRequests",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveChangeRequests_Requests",
                table: "LeaveChangeRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveDocuments_Leaves",
                table: "LeaveDocuments",
                column: "LeaveID",
                principalTable: "Leaves",
                principalColumn: "LeaveID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveDocuments_OperationLogs",
                table: "LeaveDocuments",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveDocuments_Users",
                table: "LeaveDocuments",
                column: "LinkedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveForms_Leaves",
                table: "LeaveForms",
                column: "LeaveID",
                principalTable: "Leaves",
                principalColumn: "LeaveID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveForms_OperationLogs",
                table: "LeaveForms",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveFrameworkConfigurations_OperationLogs",
                table: "LeaveFrameworkConfigurations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveReasons_Leaves",
                table: "LeaveReasons",
                column: "LeaveID",
                principalTable: "Leaves",
                principalColumn: "LeaveID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Leaves",
                table: "LeaveRequests",
                column: "LeaveID",
                principalTable: "Leaves",
                principalColumn: "LeaveID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Requests",
                table: "LeaveRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_LeaveTypes",
                table: "Leaves",
                column: "LeaveTypeID",
                principalTable: "LeaveTypes",
                principalColumn: "LeaveTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_OperationLogs",
                table: "Leaves",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeavesBulkUploadedDocuments_OperationLogs",
                table: "LeavesBulkUploadedDocuments",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeavesBulkUploadedDocuments_Users",
                table: "LeavesBulkUploadedDocuments",
                column: "UploadedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveSetLeaveTypes_LeaveSets",
                table: "LeaveSetLeaveTypes",
                column: "LeaveSetID",
                principalTable: "LeaveSets",
                principalColumn: "LeaveSetID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveSetLeaveTypes_LeaveTypes",
                table: "LeaveSetLeaveTypes",
                column: "LeaveTypeID",
                principalTable: "LeaveTypes",
                principalColumn: "LeaveTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveSetLeaveTypes_OperationLogs",
                table: "LeaveSetLeaveTypes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveSets_OperationLogs",
                table: "LeaveSets",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveSpendingLocations_OperationLogs",
                table: "LeaveSpendingLocations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_OperationLogs",
                table: "LeaveTypes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_OperationLogs",
                table: "Locations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Months_OperationLogs",
                table: "Months",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_NavigationLinkFacilityTypes_NavigationLinks",
                table: "NavigationLinkFacilityTypes",
                column: "NavigationLinkID",
                principalTable: "NavigationLinks",
                principalColumn: "NavigationLinkID");

            migrationBuilder.AddForeignKey(
                name: "FK_NavigationLinks_Roles",
                table: "NavigationLinks",
                column: "RoleID",
                principalTable: "Roles",
                principalColumn: "RoleID");

            migrationBuilder.AddForeignKey(
                name: "FK_NoPayLeaves_OperationLogs",
                table: "NoPayLeaves",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_OperationLogs1",
                table: "Notes",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineAddresses_OnlineLocations",
                table: "OnlineAddresses",
                column: "OnlineLocationID",
                principalTable: "OnlineLocations",
                principalColumn: "OnlineLocationID");

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineBusinessEntityLocations_OnlineLocations1",
                table: "OnlineBusinessEntityLocations",
                column: "OnlineLocationID",
                principalTable: "OnlineLocations",
                principalColumn: "OnlineLocationID");

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineLocations_OperationLogs",
                table: "OnlineLocations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineSignInOrganisations_OperationLogs",
                table: "OnlineSignInOrganisations",
                column: "OperationLogID",
                principalTable: "OperationLogs",
                principalColumn: "OperationLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationLogs_UserOrganisations",
                table: "OperationLogs",
                column: "CreatedByUserOrganisationID",
                principalTable: "UserOrganisations",
                principalColumn: "UserOrganisationID");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationLogs_Users",
                table: "OperationLogs",
                column: "CreatedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationalStructureSAPExceptions_SAPExceptions",
                table: "OrganisationalStructureSAPExceptions",
                column: "SAPExceptionID",
                principalTable: "SAPExceptions",
                principalColumn: "SAPExceptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationAllowanceTypes_Allowances",
                table: "OrganisationPayrollItemTypes",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationSAPExceptions_SAPExceptions",
                table: "OrganisationSAPExceptions",
                column: "SAPExceptionID",
                principalTable: "SAPExceptions",
                principalColumn: "SAPExceptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationStructureRequests_OrganisationChartRequests1",
                table: "OrganisationStructureDrafts",
                column: "OrganisationChartRequestID",
                principalTable: "OrganisationStructureRequests",
                principalColumn: "OrganisationStructureRequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationStructureHistory_Requests",
                table: "OrganisationStructureHistory",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationStructureRequests_Requests",
                table: "OrganisationStructureRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_OT_OTRequests",
                table: "OT",
                column: "OTRequestID",
                principalTable: "OTRequests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_OtherSalaryRateDefinitions_SalaryRateDefinitions",
                table: "OtherSalaryRateDefinitions",
                column: "SalaryRateDefinitionID",
                principalTable: "SalaryRateDefinitions",
                principalColumn: "SalaryRateDefinitionID");

            migrationBuilder.AddForeignKey(
                name: "FK_OTPreApprovals_Requests",
                table: "OTPreApprovals",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_OTRequests_Requests",
                table: "OTRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_OutOfOfficeRequests_Requests",
                table: "OutOfOfficeRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollCycleLinkedRequests_PayrollCycles",
                table: "PayrollCycleLinkedRequests",
                column: "PayrollCycleRequestID",
                principalTable: "PayrollCycles",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollCycleLinkedRequests_Requests",
                table: "PayrollCycleLinkedRequests",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollCycleLinkedRequests_StaffSalaries1",
                table: "PayrollCycleLinkedRequests",
                column: "StaffSalaryID",
                principalTable: "StaffSalaries",
                principalColumn: "StaffSalaryID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollCycles_Requests",
                table: "PayrollCycles",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollItemDailySAPSheetDetails_PayrollItemTypes",
                table: "PayrollItemDailySAPSheetDetails",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollItemProcessingFlows_PayrollItemTypeProcessingCodes",
                table: "PayrollItemProcessingFlows",
                column: "PayrollItemTypeProcessingCodeID",
                principalTable: "PayrollItemTypeProcessingCodes",
                principalColumn: "PayrollItemTypeProcessingCodeID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollItemProcessingFlows_PayrollItemTypeProcessingCodes1",
                table: "PayrollItemProcessingFlows",
                column: "ParentPayrollItemTypeProcessingCodeID",
                principalTable: "PayrollItemTypeProcessingCodes",
                principalColumn: "PayrollItemTypeProcessingCodeID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollItemsProcessingWhiteLists_PayrollItemTypes",
                table: "PayrollItemsProcessingWhiteLists",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_AllowanceAmounts_Allowances",
                table: "PayrollItemTypeAmounts",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_AllowanceDependencyAmounts_Allowances",
                table: "PayrollItemTypeDependencyAmounts",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollItemTypeDependencyTimePeriodAmounts_PayrollItemTypes",
                table: "PayrollItemTypeDependencyTimePeriodAmounts",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollItemTypeProcessingCodes_PayrollItemTypes",
                table: "PayrollItemTypeProcessingCodes",
                column: "PayrollItemTypeID",
                principalTable: "PayrollItemTypes",
                principalColumn: "PayrollItemTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Allowances_Requests",
                table: "PayrollItemTypes",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestCheckListVerifications_Requests",
                table: "RequestCheckListVerifications",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDocuments_Requests",
                table: "RequestDocuments",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestJobs_Requests",
                table: "RequestJobs",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_LastStateChangedByUsers",
                table: "Requests",
                column: "LastStateChangedByUserID",
                principalTable: "Users",
                principalColumn: "BusinessEntityID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Organisations",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganisations_Organisations",
                table: "UserOrganisations");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessEntities_OperationLogs",
                table: "BusinessEntities");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_OperationLogs",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_OperationLogs",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganisations_OperationLogs",
                table: "UserOrganisations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_OperationLogs",
                table: "UserPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_OperationLogs",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Requests",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessEntities_LastStateChangedByUser",
                table: "BusinessEntities");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessEntities_VerifiedByUser",
                table: "BusinessEntities");

            migrationBuilder.DropTable(
                name: "AdditionDeductionTypeDependencyTimePeriodAmounts");

            migrationBuilder.DropTable(
                name: "AddressInstances");

            migrationBuilder.DropTable(
                name: "AggregatedSalaries");

            migrationBuilder.DropTable(
                name: "AssignedWorkTypes");

            migrationBuilder.DropTable(
                name: "AttachedBreakTimesDays");

            migrationBuilder.DropTable(
                name: "AttachedPayrollItemsProcessingWhiteLists");

            migrationBuilder.DropTable(
                name: "AttendanceDeviceStaffs");

            migrationBuilder.DropTable(
                name: "AttendanceLogChangeRequests");

            migrationBuilder.DropTable(
                name: "AttendanceLogChangeRequestTypes");

            migrationBuilder.DropTable(
                name: "AttendanceLogRequests");

            migrationBuilder.DropTable(
                name: "AttendanceLogResolutions");

            migrationBuilder.DropTable(
                name: "AttendanceLogsTemp");

            migrationBuilder.DropTable(
                name: "BasicSalaries");

            migrationBuilder.DropTable(
                name: "BudgetTransactions");

            migrationBuilder.DropTable(
                name: "BulkUploadedDocumentSummaries");

            migrationBuilder.DropTable(
                name: "BusinessEntitiesDocuments");

            migrationBuilder.DropTable(
                name: "BusinessEntityCalendars");

            migrationBuilder.DropTable(
                name: "BusinessEntityLocations");

            migrationBuilder.DropTable(
                name: "BusinessEntityRelatedLocationTypes");

            migrationBuilder.DropTable(
                name: "BusinessEntityRelationAssignedRoles");

            migrationBuilder.DropTable(
                name: "BusinessEntityRequests");

            migrationBuilder.DropTable(
                name: "BusinessEntitySchedules");

            migrationBuilder.DropTable(
                name: "BussinessEntityContactInformations");

            migrationBuilder.DropTable(
                name: "CalendarElements");

            migrationBuilder.DropTable(
                name: "CalendarMonths");

            migrationBuilder.DropTable(
                name: "CalendarSchedules");

            migrationBuilder.DropTable(
                name: "CommercialIslandTypes");

            migrationBuilder.DropTable(
                name: "ConfigurableValues");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "CurrencyExchangeRates");

            migrationBuilder.DropTable(
                name: "DataCorrectionRequestAttributeValues");

            migrationBuilder.DropTable(
                name: "DataCorrectionRequestTypeAttributes");

            migrationBuilder.DropTable(
                name: "DeductionTypeAmounts");

            migrationBuilder.DropTable(
                name: "DerivedAdditionDeductionTypes");

            migrationBuilder.DropTable(
                name: "DerivedOnPayrollItemTypes");

            migrationBuilder.DropTable(
                name: "Dhafthar");

            migrationBuilder.DropTable(
                name: "DNRLookupCache");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropTable(
                name: "GroupConfigurableValues");

            migrationBuilder.DropTable(
                name: "GroupLeaveSets");

            migrationBuilder.DropTable(
                name: "GroupSchedules");

            migrationBuilder.DropTable(
                name: "GroupStaffs");

            migrationBuilder.DropTable(
                name: "IDCards");

            migrationBuilder.DropTable(
                name: "IdentityCardTypes");

            migrationBuilder.DropTable(
                name: "JobLeaveTypes");

            migrationBuilder.DropTable(
                name: "JobPositionBasicSalaries");

            migrationBuilder.DropTable(
                name: "JobPositionsRequests");

            migrationBuilder.DropTable(
                name: "JobRequests");

            migrationBuilder.DropTable(
                name: "JobSAPExceptions");

            migrationBuilder.DropTable(
                name: "JobTerminationTypes");

            migrationBuilder.DropTable(
                name: "JobTypesExceptions");

            migrationBuilder.DropTable(
                name: "JobWorkTemplateAssignments");

            migrationBuilder.DropTable(
                name: "JobWorkTemplates");

            migrationBuilder.DropTable(
                name: "KeyHolders");

            migrationBuilder.DropTable(
                name: "KPIDocuments");

            migrationBuilder.DropTable(
                name: "LeaveChangeRequests");

            migrationBuilder.DropTable(
                name: "LeaveConfigurableValues");

            migrationBuilder.DropTable(
                name: "LeaveDocuments");

            migrationBuilder.DropTable(
                name: "LeaveForms");

            migrationBuilder.DropTable(
                name: "LeaveFrameworkConfigurations");

            migrationBuilder.DropTable(
                name: "LeaveLodgeTypes");

            migrationBuilder.DropTable(
                name: "LeavePolicyAccrualRules");

            migrationBuilder.DropTable(
                name: "LeaveReasons");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeavesBulkUploadedDocuments");

            migrationBuilder.DropTable(
                name: "LeaveSetLeaveTypes");

            migrationBuilder.DropTable(
                name: "LeaveSpendingLocations");

            migrationBuilder.DropTable(
                name: "LeaveTypeMappings");

            migrationBuilder.DropTable(
                name: "LeaveWorkHandOvers");

            migrationBuilder.DropTable(
                name: "LeaveWorkHandOverTasks");

            migrationBuilder.DropTable(
                name: "MIMETypes");

            migrationBuilder.DropTable(
                name: "NavigationLinkFacilityTypes");

            migrationBuilder.DropTable(
                name: "NoPayLeaves");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "OfficialTripLocations");

            migrationBuilder.DropTable(
                name: "OnlineAddressInstances");

            migrationBuilder.DropTable(
                name: "OnlineAreas");

            migrationBuilder.DropTable(
                name: "OnlineBusinessEntityLocations");

            migrationBuilder.DropTable(
                name: "OnlineDhafthar");

            migrationBuilder.DropTable(
                name: "OnlineSignInOrganisations");

            migrationBuilder.DropTable(
                name: "OrganisationalStructureSAPExceptions");

            migrationBuilder.DropTable(
                name: "OrganisationBudgetMonthlySummaries");

            migrationBuilder.DropTable(
                name: "OrganisationBudgets");

            migrationBuilder.DropTable(
                name: "OrganisationBudgetSummaries");

            migrationBuilder.DropTable(
                name: "OrganisationBudgetSummeryAggregates");

            migrationBuilder.DropTable(
                name: "OrganisationDeductionTypes");

            migrationBuilder.DropTable(
                name: "OrganisationPayrollItemTypes");

            migrationBuilder.DropTable(
                name: "OrganisationPayrollPeriodJobTypes");

            migrationBuilder.DropTable(
                name: "OrganisationSAPExceptions");

            migrationBuilder.DropTable(
                name: "OrganisationStructureCalendars");

            migrationBuilder.DropTable(
                name: "OrganisationStructureDrafts");

            migrationBuilder.DropTable(
                name: "OrganisationStructureHeadIncharges");

            migrationBuilder.DropTable(
                name: "OrganisationStructureHistory");

            migrationBuilder.DropTable(
                name: "OrganisationStructureLocations");

            migrationBuilder.DropTable(
                name: "OrganisationStructureSchedules");

            migrationBuilder.DropTable(
                name: "OrganisationStructureSupervisors");

            migrationBuilder.DropTable(
                name: "OrganisationWorkPlanningSettings");

            migrationBuilder.DropTable(
                name: "OT");

            migrationBuilder.DropTable(
                name: "OtherSalaryRateDefinitions");

            migrationBuilder.DropTable(
                name: "OTPreApprovalTimes");

            migrationBuilder.DropTable(
                name: "OutOfOfficeRequests");

            migrationBuilder.DropTable(
                name: "ParentCalenders");

            migrationBuilder.DropTable(
                name: "Passports");

            migrationBuilder.DropTable(
                name: "PayrollConfigurableValues");

            migrationBuilder.DropTable(
                name: "PayrollCycleLinkedRequests");

            migrationBuilder.DropTable(
                name: "PayrollItemDailySAPSheetDetails");

            migrationBuilder.DropTable(
                name: "PayrollItemProcessingFlows");

            migrationBuilder.DropTable(
                name: "PayrollItemTypeAmounts");

            migrationBuilder.DropTable(
                name: "PayrollItemTypeDependencyAmounts");

            migrationBuilder.DropTable(
                name: "PayrollItemTypeDependencyTimePeriodAmounts");

            migrationBuilder.DropTable(
                name: "PercentageVariableDeductionAmounts");

            migrationBuilder.DropTable(
                name: "PositionBasicSalaries");

            migrationBuilder.DropTable(
                name: "PrimaryPayrollItemTypeSAPWageTypes");

            migrationBuilder.DropTable(
                name: "RequestCheckListVerifications");

            migrationBuilder.DropTable(
                name: "RequestDocumentVerifications");

            migrationBuilder.DropTable(
                name: "RequestJobs");

            migrationBuilder.DropTable(
                name: "RequestTeams");

            migrationBuilder.DropTable(
                name: "RequestTypesAllowedRequestStateTransitions");

            migrationBuilder.DropTable(
                name: "RequestTypeSpecificCheckListItems");

            migrationBuilder.DropTable(
                name: "RequestTypeSpecificDocumentTypes");

            migrationBuilder.DropTable(
                name: "RequestTypesStatesRequiredRoles");

            migrationBuilder.DropTable(
                name: "RequestTypesStatesRequiredRolesForProcessing");

            migrationBuilder.DropTable(
                name: "ScheduleElements");

            migrationBuilder.DropTable(
                name: "ScheduleOwners");

            migrationBuilder.DropTable(
                name: "ServiceCheckListVerifications");

            migrationBuilder.DropTable(
                name: "ServiceDocumentVerifications");

            migrationBuilder.DropTable(
                name: "ServiceTypesAllowedServiceStateTransitions");

            migrationBuilder.DropTable(
                name: "ServiceTypeSpecificCheckListItems");

            migrationBuilder.DropTable(
                name: "ShiftScheduleDays");

            migrationBuilder.DropTable(
                name: "SickLeaveForms");

            migrationBuilder.DropTable(
                name: "SickLeaveLodgeTypes");

            migrationBuilder.DropTable(
                name: "SpecialDayShifts");

            migrationBuilder.DropTable(
                name: "StaffAssignedDeductionAmounts");

            migrationBuilder.DropTable(
                name: "StaffDailyAttandanceSummaryIssues");

            migrationBuilder.DropTable(
                name: "StaffEnrollmentNumbers");

            migrationBuilder.DropTable(
                name: "StaffPayrollItemDetails");

            migrationBuilder.DropTable(
                name: "StaffPayrollItemTypeAmounts");

            migrationBuilder.DropTable(
                name: "StaffPayrollItemTypeSchedules");

            migrationBuilder.DropTable(
                name: "StaffSalaryDeductions");

            migrationBuilder.DropTable(
                name: "StaffSalaryPayrollItems");

            migrationBuilder.DropTable(
                name: "StaffSalaryPayrollItemSAPDetails");

            migrationBuilder.DropTable(
                name: "TeamStaffs");

            migrationBuilder.DropTable(
                name: "UserAssignedUserGroups");

            migrationBuilder.DropTable(
                name: "UserGroupRoles");

            migrationBuilder.DropTable(
                name: "UserOrganisationRoles");

            migrationBuilder.DropTable(
                name: "UserOrganisationUserGroups");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserServiceRoles");

            migrationBuilder.DropTable(
                name: "WebServiceRequestLogs");

            migrationBuilder.DropTable(
                name: "WorkAssignmentTransfers");

            migrationBuilder.DropTable(
                name: "WorkingDayShifts");

            migrationBuilder.DropTable(
                name: "WorkPlanSegments");

            migrationBuilder.DropTable(
                name: "WorkShiftsBreakTimes");

            migrationBuilder.DropTable(
                name: "AddressBases");

            migrationBuilder.DropTable(
                name: "WorkTypes");

            migrationBuilder.DropTable(
                name: "AttachedBreakTimes");

            migrationBuilder.DropTable(
                name: "PayrollItemsProcessingWhiteLists");

            migrationBuilder.DropTable(
                name: "AttendanceResolutionStatuses");

            migrationBuilder.DropTable(
                name: "WorkAssignmentSegments");

            migrationBuilder.DropTable(
                name: "BudgetTransactionTypes");

            migrationBuilder.DropTable(
                name: "BulkUploadedDocuments");

            migrationBuilder.DropTable(
                name: "BusinessEntityRelations");

            migrationBuilder.DropTable(
                name: "ContactInformationTypes");

            migrationBuilder.DropTable(
                name: "CalendarInstances");

            migrationBuilder.DropTable(
                name: "Months");

            migrationBuilder.DropTable(
                name: "DataCorrectionRequests");

            migrationBuilder.DropTable(
                name: "DataCorrectionActionTypes");

            migrationBuilder.DropTable(
                name: "DataCorrectionAttributes");

            migrationBuilder.DropTable(
                name: "DerivedAdditionDeductionTypeItems");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "GroupConfigurableValueTypes");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "IDCardStates");

            migrationBuilder.DropTable(
                name: "LeaveChangeRequestTypes");

            migrationBuilder.DropTable(
                name: "LeaveConfigurableValueTypes");

            migrationBuilder.DropTable(
                name: "LeavePolicies");

            migrationBuilder.DropTable(
                name: "LeaveTypeReasonTypes");

            migrationBuilder.DropTable(
                name: "LeaveSets");

            migrationBuilder.DropTable(
                name: "FacilityRegistrationTypes");

            migrationBuilder.DropTable(
                name: "NavigationLinks");

            migrationBuilder.DropTable(
                name: "NoPayLeaveTypes");

            migrationBuilder.DropTable(
                name: "NoteReasons");

            migrationBuilder.DropTable(
                name: "OfficialTripDetails");

            migrationBuilder.DropTable(
                name: "AddressInstanceTypes");

            migrationBuilder.DropTable(
                name: "OnlineAddressBases");

            migrationBuilder.DropTable(
                name: "BusinessEntityLocationTypes");

            migrationBuilder.DropTable(
                name: "OnlineAddresses");

            migrationBuilder.DropTable(
                name: "BudgetItems");

            migrationBuilder.DropTable(
                name: "SAPExceptions");

            migrationBuilder.DropTable(
                name: "OrganisationStructureRequests");

            migrationBuilder.DropTable(
                name: "OrganisationStructureRequestTypes");

            migrationBuilder.DropTable(
                name: "OrganisationStructureRelations");

            migrationBuilder.DropTable(
                name: "OrganisationStructureLocationTypes");

            migrationBuilder.DropTable(
                name: "AttendanceLogs");

            migrationBuilder.DropTable(
                name: "OTRequests");

            migrationBuilder.DropTable(
                name: "SalaryRateDefinitionForTypes");

            migrationBuilder.DropTable(
                name: "SalaryRateDefinitions");

            migrationBuilder.DropTable(
                name: "OutOfficeRequestTypes");

            migrationBuilder.DropTable(
                name: "PassportStates");

            migrationBuilder.DropTable(
                name: "PayrollConfigurableValueTypes");

            migrationBuilder.DropTable(
                name: "PayrollItemTypeProcessingCodes");

            migrationBuilder.DropTable(
                name: "RequestDocuments");

            migrationBuilder.DropTable(
                name: "Elements");

            migrationBuilder.DropTable(
                name: "ServiceDocuments");

            migrationBuilder.DropTable(
                name: "CheckListItems");

            migrationBuilder.DropTable(
                name: "ShiftSchedules");

            migrationBuilder.DropTable(
                name: "SpecialDays");

            migrationBuilder.DropTable(
                name: "PayrollCycleProcessingStates");

            migrationBuilder.DropTable(
                name: "StaffDailyAttandanceSummaryIssueTypes");

            migrationBuilder.DropTable(
                name: "StaffDailyAttendanceSummeries");

            migrationBuilder.DropTable(
                name: "StaffAssignedDeductions");

            migrationBuilder.DropTable(
                name: "StaffPayrollItemTypes");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "WorkAssignmentOwners");

            migrationBuilder.DropTable(
                name: "WorkAssignmentTransferStates");

            migrationBuilder.DropTable(
                name: "WorkingDays");

            migrationBuilder.DropTable(
                name: "BreakTimes");

            migrationBuilder.DropTable(
                name: "OwnerTypes");

            migrationBuilder.DropTable(
                name: "WorkTemplateSegments");

            migrationBuilder.DropTable(
                name: "DocumentStates");

            migrationBuilder.DropTable(
                name: "BusinessEntityRelationStates");

            migrationBuilder.DropTable(
                name: "BusinessEntityRelationTypes");

            migrationBuilder.DropTable(
                name: "Calendars");

            migrationBuilder.DropTable(
                name: "DataCorrectionRequestStates");

            migrationBuilder.DropTable(
                name: "DataCorrectionRequestTypes");

            migrationBuilder.DropTable(
                name: "DataCorrectionAttributeLookupTypes");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "GroupTypes");

            migrationBuilder.DropTable(
                name: "LeaveDefinitions");

            migrationBuilder.DropTable(
                name: "LinkTypes");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Leaves");

            migrationBuilder.DropTable(
                name: "OfficialTripTypes");

            migrationBuilder.DropTable(
                name: "AddressBaseTypes");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "OnlineLocations");

            migrationBuilder.DropTable(
                name: "SAPExceptionActionTypes");

            migrationBuilder.DropTable(
                name: "SAPExceptionTypes");

            migrationBuilder.DropTable(
                name: "SAPWageTypes");

            migrationBuilder.DropTable(
                name: "AttendanceDevices");

            migrationBuilder.DropTable(
                name: "AttendanceLogModes");

            migrationBuilder.DropTable(
                name: "AttendanceLogStates");

            migrationBuilder.DropTable(
                name: "InOutModes");

            migrationBuilder.DropTable(
                name: "OTPreApprovals");

            migrationBuilder.DropTable(
                name: "DayOfWeek");

            migrationBuilder.DropTable(
                name: "SpecialDayTypes");

            migrationBuilder.DropTable(
                name: "WorkShifts");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "JobPositions");

            migrationBuilder.DropTable(
                name: "StaffSalaries");

            migrationBuilder.DropTable(
                name: "DeductionTypes");

            migrationBuilder.DropTable(
                name: "PayrollItemTypes");

            migrationBuilder.DropTable(
                name: "WorkAssignments");

            migrationBuilder.DropTable(
                name: "WorkSegmentTypes");

            migrationBuilder.DropTable(
                name: "CalendarTypes");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "LeaveStates");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "Islands");

            migrationBuilder.DropTable(
                name: "LocationTypes");

            migrationBuilder.DropTable(
                name: "AttendanceClientInstances");

            migrationBuilder.DropTable(
                name: "AttendanceDeviceInOutType");

            migrationBuilder.DropTable(
                name: "AttendanceDeviceStates");

            migrationBuilder.DropTable(
                name: "OTTypes");

            migrationBuilder.DropTable(
                name: "DayTypes");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "ScheduleTypes");

            migrationBuilder.DropTable(
                name: "JobPositionStates");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "PayrollCycles");

            migrationBuilder.DropTable(
                name: "DeductionAmountTypes");

            migrationBuilder.DropTable(
                name: "AmountTypes");

            migrationBuilder.DropTable(
                name: "CurrencyTypes");

            migrationBuilder.DropTable(
                name: "DependencyTypes");

            migrationBuilder.DropTable(
                name: "DerivedOnTypes");

            migrationBuilder.DropTable(
                name: "RecurrenceTypes");

            migrationBuilder.DropTable(
                name: "AdditionOrDeductionTypes");

            migrationBuilder.DropTable(
                name: "AttendanceDependentTypes");

            migrationBuilder.DropTable(
                name: "SAPSheets");

            migrationBuilder.DropTable(
                name: "WorkAssignmentStates");

            migrationBuilder.DropTable(
                name: "WorkPlans");

            migrationBuilder.DropTable(
                name: "Atolls");

            migrationBuilder.DropTable(
                name: "AttendanceClientStates");

            migrationBuilder.DropTable(
                name: "PositionClassifications");

            migrationBuilder.DropTable(
                name: "PositionRanks");

            migrationBuilder.DropTable(
                name: "PositionTypes");

            migrationBuilder.DropTable(
                name: "OrganisationPayrollPeriods");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "PlanningProviders");

            migrationBuilder.DropTable(
                name: "WorkTemplates");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "JobStates");

            migrationBuilder.DropTable(
                name: "JobTypes");

            migrationBuilder.DropTable(
                name: "OrganisationStructure");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "WorkTemplateTypes");

            migrationBuilder.DropTable(
                name: "OrganisationStructureStates");

            migrationBuilder.DropTable(
                name: "OrganisationStructureTypes");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropTable(
                name: "OrganisationTypes");

            migrationBuilder.DropTable(
                name: "OperationLogs");

            migrationBuilder.DropTable(
                name: "OperationLogActionTypes");

            migrationBuilder.DropTable(
                name: "Requests");

            migrationBuilder.DropTable(
                name: "RequestStates");

            migrationBuilder.DropTable(
                name: "RequestTypes");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "ServiceStates");

            migrationBuilder.DropTable(
                name: "ServiceTypes");

            migrationBuilder.DropTable(
                name: "SequenceNumberTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Individuals");

            migrationBuilder.DropTable(
                name: "LocalUserStates");

            migrationBuilder.DropTable(
                name: "SSOUserStates");

            migrationBuilder.DropTable(
                name: "UserOrganisations");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "BusinessEntities");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "GenderTypes");

            migrationBuilder.DropTable(
                name: "Context");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "BusinessEntityStates");

            migrationBuilder.DropTable(
                name: "BusinessEntityTypes");

            migrationBuilder.DropTable(
                name: "VerifiedStates");

            migrationBuilder.DropTable(
                name: "Regions");
        }
    }
}
