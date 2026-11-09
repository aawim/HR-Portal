using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Staff;
using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services;

public sealed class StaffBankService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    ILogger<StaffBankService> logger) : StaffBankServiceContracts.IOperations
{
    private static bool _tableEnsured;
    private static readonly object _tableLock = new();

    private async Task EnsureTableAsync(HrmTeContext db)
    {
        if (_tableEnsured) return;
        lock (_tableLock)
        {
            if (_tableEnsured) return;
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BK_StaffBankAccounts')
                BEGIN
                    CREATE TABLE dbo.BK_StaffBankAccounts (
                        Id INT IDENTITY(1,1) NOT NULL,
                        IndividualId INT NOT NULL,
                        BankName NVARCHAR(150) NOT NULL,
                        BankCode NVARCHAR(50) NULL,
                        BranchName NVARCHAR(150) NULL,
                        AccountHolderName NVARCHAR(200) NOT NULL,
                        AccountNumber NVARCHAR(100) NOT NULL,
                        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_BK_StaffBankAccounts_Currency DEFAULT ('MVR'),
                        IsPrimary BIT NOT NULL CONSTRAINT DF_BK_StaffBankAccounts_IsPrimary DEFAULT (0),
                        Remarks NVARCHAR(500) NULL,
                        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_BK_StaffBankAccounts_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2(0) NULL,
                        CONSTRAINT PK_BK_StaffBankAccounts PRIMARY KEY CLUSTERED (Id),
                        CONSTRAINT FK_BK_StaffBankAccounts_Individual FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID) ON DELETE CASCADE
                    );
                    CREATE NONCLUSTERED INDEX IX_BK_StaffBankAccounts_IndividualId ON dbo.BK_StaffBankAccounts (IndividualId);
                    CREATE NONCLUSTERED INDEX IX_BK_StaffBankAccounts_IsPrimary ON dbo.BK_StaffBankAccounts (IndividualId, IsPrimary);
                END;");
            _tableEnsured = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed ensuring BK_StaffBankAccounts table");
        }
    }

    public async Task<List<StaffBankAccountDto>> GetAccountsAsync(int individualId)
    {
        if (individualId <= 0) return [];

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var list = await db.StaffBankAccounts
            .AsNoTracking()
            .Where(b => b.IndividualId == individualId)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.BankName)
            .Select(b => new StaffBankAccountDto
            {
                Id = b.Id,
                IndividualId = b.IndividualId,
                BankName = b.BankName,
                BankCode = b.BankCode,
                BranchName = b.BranchName,
                AccountHolderName = b.AccountHolderName,
                AccountNumber = b.AccountNumber,
                Currency = b.Currency,
                IsPrimary = b.IsPrimary,
                Remarks = b.Remarks,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            })
            .ToListAsync();

        return list;
    }

    public async Task<ServiceResult> SaveAccountAsync(StaffBankAccountInputDto input)
    {
        if (input.IndividualId <= 0)
            return ServiceResult.Fail("A valid staff individual ID is required.");
        if (string.IsNullOrWhiteSpace(input.BankName))
            return ServiceResult.Fail("Bank name is required.");
        if (string.IsNullOrWhiteSpace(input.AccountNumber))
            return ServiceResult.Fail("Account number is required.");
        if (string.IsNullOrWhiteSpace(input.AccountHolderName))
            return ServiceResult.Fail("Account holder name is required.");

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var existingAccounts = await db.StaffBankAccounts
            .Where(b => b.IndividualId == input.IndividualId)
            .ToListAsync();

        // If duplicate account number for this individual
        if (existingAccounts.Any(b => b.Id != input.Id && b.AccountNumber.Trim() == input.AccountNumber.Trim()))
            return ServiceResult.Fail("This account number is already registered for this staff member.");

        var shouldBePrimary = input.IsPrimary || existingAccounts.Count == 0;

        if (input.Id > 0)
        {
            var entity = existingAccounts.FirstOrDefault(b => b.Id == input.Id);
            if (entity == null)
                return ServiceResult.Fail("Bank account not found.");

            if (shouldBePrimary)
            {
                foreach (var a in existingAccounts.Where(b => b.Id != input.Id))
                    a.IsPrimary = false;
            }

            entity.BankName = input.BankName.Trim();
            entity.BankCode = input.BankCode?.Trim();
            entity.BranchName = input.BranchName?.Trim();
            entity.AccountHolderName = input.AccountHolderName.Trim();
            entity.AccountNumber = input.AccountNumber.Trim();
            entity.Currency = string.IsNullOrWhiteSpace(input.Currency) ? "MVR" : input.Currency.Trim();
            entity.IsPrimary = shouldBePrimary;
            entity.Remarks = input.Remarks?.Trim();
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            if (shouldBePrimary)
            {
                foreach (var a in existingAccounts)
                    a.IsPrimary = false;
            }

            var newEntity = new StaffBankAccount
            {
                IndividualId = input.IndividualId,
                BankName = input.BankName.Trim(),
                BankCode = input.BankCode?.Trim(),
                BranchName = input.BranchName?.Trim(),
                AccountHolderName = input.AccountHolderName.Trim(),
                AccountNumber = input.AccountNumber.Trim(),
                Currency = string.IsNullOrWhiteSpace(input.Currency) ? "MVR" : input.Currency.Trim(),
                IsPrimary = shouldBePrimary,
                Remarks = input.Remarks?.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.StaffBankAccounts.Add(newEntity);
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Bank account saved successfully.");
    }

    public async Task<ServiceResult> DeleteAccountAsync(int id)
    {
        if (id <= 0) return ServiceResult.Fail("Invalid bank account ID.");

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var entity = await db.StaffBankAccounts.FirstOrDefaultAsync(b => b.Id == id);
        if (entity == null) return ServiceResult.Fail("Bank account not found.");

        var individualId = entity.IndividualId;
        var wasPrimary = entity.IsPrimary;

        db.StaffBankAccounts.Remove(entity);
        await db.SaveChangesAsync();

        if (wasPrimary)
        {
            var remaining = await db.StaffBankAccounts.FirstOrDefaultAsync(b => b.IndividualId == individualId);
            if (remaining != null)
            {
                remaining.IsPrimary = true;
                await db.SaveChangesAsync();
            }
        }

        return ServiceResult.Ok("Bank account deleted successfully.");
    }

    public async Task<ServiceResult> SetPrimaryAsync(int id, int individualId)
    {
        if (id <= 0 || individualId <= 0) return ServiceResult.Fail("Invalid parameters.");

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var accounts = await db.StaffBankAccounts
            .Where(b => b.IndividualId == individualId)
            .ToListAsync();

        if (!accounts.Any(b => b.Id == id))
            return ServiceResult.Fail("Bank account not found for this individual.");

        foreach (var b in accounts)
        {
            b.IsPrimary = (b.Id == id);
            b.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Primary bank account updated successfully.");
    }
}
