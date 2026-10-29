using HRM.Models;
using HRM.Models.Archives;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class LegacyTableCleanupTests
{
    [Fact]
    public void Active_registry_and_legacy_shift_models_use_prefixed_tables_and_preserve_keys()
    {
        using var db=new QueryFactory().CreateDbContext();
        (Type Type,string Table)[] expected=
        [
            (typeof(BreakTime),"BT_BreakTimes"),(typeof(BusinessEntitiesDocument),"BE_BusinessEntityDocuments"),
            (typeof(BusinessEntityCalendar),"BE_BusinessEntityCalendars"),(typeof(BusinessEntityLocation),"BE_BusinessEntityLocations"),
            (typeof(BusinessEntityLocationType),"BE_BusinessEntityLocationTypes"),(typeof(BusinessEntitySchedule),"BE_BusinessEntitySchedules"),
            (typeof(BusinessEntityType),"BE_BusinessEntityTypes"),(typeof(BussinessEntityContactInformation),"BE_BusinessEntityContacts")
        ];
        foreach(var item in expected){var entity=db.Model.FindEntityType(item.Type)!;Assert.Equal(item.Table,entity.GetTableName());Assert.NotNull(entity.FindPrimaryKey());}
        Assert.Equal("PK_BusinessEntityCalenders",db.Model.FindEntityType(typeof(BusinessEntityCalendar))!.FindPrimaryKey()!.GetName());
        Assert.Equal("PK_BussinessEntityAddresses",db.Model.FindEntityType(typeof(BusinessEntityLocation))!.FindPrimaryKey()!.GetName());
        Assert.Equal(2,db.Model.FindEntityType(typeof(BusinessEntitiesDocument))!.FindPrimaryKey()!.Properties.Count);
    }
    [Fact]
    public void Retired_tables_have_no_remaining_orm_entities_or_navigations()
    {
        using var db=new QueryFactory().CreateDbContext();
        string[] retired=["AdditionDeductionTypeDependencyTimePeriodAmounts","BudgetTransactions","BudgetTransactionTypes","BusinessEntityRelatedLocationTypes","BusinessEntityRelationAssignedRoles","BusinessEntityRequests","BusinessEntityRelations","BusinessEntityRelationStates"];
        foreach(var entity in db.Model.GetEntityTypes())
        {
            Assert.DoesNotContain(entity.GetTableName(),retired);
            foreach(var navigation in entity.GetNavigations())Assert.DoesNotContain(navigation.TargetEntityType.GetTableName(),retired);
        }
    }
    [Fact]
    public void Retirement_evidence_has_a_separate_history_table_and_unique_source_key()
    {
        using var db=new QueryFactory().CreateDbContext();var entity=db.Model.FindEntityType(typeof(TableRetirementAudit))!;
        Assert.Equal("SC_TableRetirementAudits",entity.GetTableName());
        Assert.Contains(entity.GetIndexes(),i=>i.IsUnique&&i.Properties.Select(p=>p.Name).SequenceEqual(new[]{"MigrationId","OriginalTableName"}));
        Assert.False(entity.FindProperty(nameof(TableRetirementAudit.SchemaJson))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(TableRetirementAudit.DataJson))!.IsNullable);
    }
}
