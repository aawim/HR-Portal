using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace HRM.Migrations;
[DbContext(typeof(HrmTeContext))][Migration("20261113100000_WorkforcePayrollFinalization")]
public partial class WorkforcePayrollFinalization:Migration
{protected override void Up(MigrationBuilder m){m.AddColumn<string>(name:"PayrollExportReference",table:"WS_WorkTimeLedgerEntries",type:"nvarchar(100)",maxLength:100,nullable:true);m.AddColumn<DateTime>(name:"PayrollExportedAtUtc",table:"WS_WorkTimeLedgerEntries",type:"datetime2",nullable:true);m.AddColumn<long>(name:"CorrectsLedgerEntryId",table:"WS_WorkTimeLedgerEntries",type:"bigint",nullable:true);m.AddColumn<bool>(name:"IsPayrollFinalized",table:"WS_WorkTimeLedgerEntries",type:"bit",nullable:false,defaultValue:false);m.CreateIndex(name:"IX_WS_WorkTimeLedgerEntries_CorrectsLedgerEntryId",table:"WS_WorkTimeLedgerEntries",column:"CorrectsLedgerEntryId");m.AddForeignKey(name:"FK_WS_WorkTimeLedgerEntries_Corrections",table:"WS_WorkTimeLedgerEntries",column:"CorrectsLedgerEntryId",principalTable:"WS_WorkTimeLedgerEntries",principalColumn:"Id",onDelete:ReferentialAction.Restrict);}protected override void Down(MigrationBuilder m){m.DropForeignKey(name:"FK_WS_WorkTimeLedgerEntries_Corrections",table:"WS_WorkTimeLedgerEntries");m.DropIndex(name:"IX_WS_WorkTimeLedgerEntries_CorrectsLedgerEntryId",table:"WS_WorkTimeLedgerEntries");m.DropColumn(name:"PayrollExportReference",table:"WS_WorkTimeLedgerEntries");m.DropColumn(name:"PayrollExportedAtUtc",table:"WS_WorkTimeLedgerEntries");m.DropColumn(name:"CorrectsLedgerEntryId",table:"WS_WorkTimeLedgerEntries");m.DropColumn(name:"IsPayrollFinalized",table:"WS_WorkTimeLedgerEntries");}}
