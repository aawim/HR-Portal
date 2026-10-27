using System.Data;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
internal static class RegistryPrefixSqlSmoke
{
    internal sealed record Table(string Name,int Id,long Count,string Rows);
    internal sealed record Snapshot(List<Table> Tables,List<string> ForeignKeys,List<string> Indexes,int ProcedureId,List<string> ProcedureRows);
    private static readonly (string Old,string Key)[] Names=[("Individuals","BusinessEntityID"),("BusinessEntities","BusinessEntityID"),("VerifiedStates","VerifiedStateID"),("BusinessEntityStates","BusinessEntityStateID")];
    public static async Task<Snapshot> CaptureAsync(HrmTeContext db)
    {
        await db.Database.OpenConnectionAsync();var connection=db.Database.GetDbConnection();var tables=new List<Table>();
        foreach(var item in Names)
        {
            await using var cmd=connection.CreateCommand();cmd.CommandTimeout=300;cmd.CommandText=$"SELECT COALESCE(OBJECT_ID('dbo.{item.Old}','U'),OBJECT_ID('dbo.BE_{item.Old}','U'),0)";var id=Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if(id==0)throw new InvalidOperationException("Registry catalogue missing: "+item.Old);
            cmd.CommandText=$"SELECT OBJECT_NAME({id})";var physical=Convert.ToString(await cmd.ExecuteScalarAsync())!;
            if(physical!=item.Old&&physical!="BE_"+item.Old)throw new InvalidOperationException("Unexpected registry table name.");
            cmd.CommandText=$"SELECT COUNT_BIG(*) FROM dbo.[{physical}]";var count=Convert.ToInt64(await cmd.ExecuteScalarAsync());
            cmd.CommandText=$"SELECT CONVERT(nvarchar(max),(SELECT * FROM dbo.[{physical}] ORDER BY [{item.Key}] FOR XML RAW,BINARY BASE64,ROOT('Rows')))";var rows=Convert.ToString(await cmd.ExecuteScalarAsync())!;
            tables.Add(new(item.Old,id,count,rows));
        }
        async Task<List<string>> Query(string sql)
        {await using var cmd=connection.CreateCommand();cmd.CommandText=sql;var result=new List<string>();await using var reader=await cmd.ExecuteReaderAsync();while(await reader.ReadAsync())result.Add(string.Join("|",Enumerable.Range(0,reader.FieldCount).Select(i=>Convert.ToString(reader.GetValue(i)))));return result;}
        var ids=string.Join(",",tables.Select(t=>t.Id));
        var fks=await Query($"SELECT object_id,name,parent_object_id,referenced_object_id,delete_referential_action,update_referential_action,is_disabled,is_not_trusted FROM sys.foreign_keys WHERE parent_object_id IN ({ids}) OR referenced_object_id IN ({ids}) ORDER BY object_id");
        var indexes=await Query($"SELECT object_id,index_id,name,type,is_unique,is_primary_key FROM sys.indexes WHERE object_id IN ({ids}) ORDER BY object_id,index_id");
        await using var proc=connection.CreateCommand();proc.CommandText="SELECT COALESCE(OBJECT_ID('dbo.UserGetInfo','P'),0)";var procedureId=Convert.ToInt32(await proc.ExecuteScalarAsync());var procedureRows=new List<string>();
        if(procedureId>0)
        {
            proc.CommandText="SELECT TOP(1) Username FROM dbo.Users ORDER BY UserID";var username=await proc.ExecuteScalarAsync();
            if(username!=null){proc.CommandType=CommandType.StoredProcedure;proc.CommandText="dbo.UserGetInfo";var p=proc.CreateParameter();p.ParameterName="@userName";p.Value=username;proc.Parameters.Add(p);await using var reader=await proc.ExecuteReaderAsync();while(await reader.ReadAsync())procedureRows.Add(string.Join("|",Enumerable.Range(0,reader.FieldCount).Select(i=>Convert.ToString(reader.GetValue(i)))));}
        }
        return new(tables,fks,indexes,procedureId,procedureRows);
    }
    public static async Task VerifyAsync(HrmTeContext db,Snapshot before)
    {
        var after=await CaptureAsync(db);
        if(!before.Tables.SequenceEqual(after.Tables)||before.ForeignKeys.Except(after.ForeignKeys).Any()||before.Indexes.Except(after.Indexes).Any()||before.ProcedureId!=after.ProcedureId||!before.ProcedureRows.SequenceEqual(after.ProcedureRows))throw new InvalidOperationException("Registry rename changed rows, table IDs, original foreign keys/indexes or legacy lookup results.");
        foreach(var table in Names)
        {await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.CommandText=$"SELECT CASE WHEN OBJECT_ID('dbo.{table.Old}','U') IS NULL AND OBJECT_ID('dbo.BE_{table.Old}','U') IS NOT NULL THEN 1 ELSE 0 END";if(Convert.ToInt32(await cmd.ExecuteScalarAsync())!=1)throw new InvalidOperationException("Registry rename incomplete.");}
        Console.WriteLine("PASS registry rename: exact rows, table IDs, original foreign keys/indexes and UserGetInfo results preserved across all four tables.");
    }
}
