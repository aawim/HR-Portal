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

public sealed class StaffEmergencyContactService(
    IDbContextFactory<HrmTeContext> factory,
    ILogger<StaffEmergencyContactService> logger) : StaffEmergencyContactServiceContracts.IOperations
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
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EC_StaffEmergencyContacts')
                BEGIN
                    CREATE TABLE dbo.EC_StaffEmergencyContacts (
                        Id INT IDENTITY(1,1) NOT NULL,
                        IndividualId INT NOT NULL,
                        ContactName NVARCHAR(200) NOT NULL,
                        Relationship NVARCHAR(100) NOT NULL,
                        PrimaryPhone NVARCHAR(50) NOT NULL,
                        SecondaryPhone NVARCHAR(50) NULL,
                        Email NVARCHAR(150) NULL,
                        Address NVARCHAR(500) NULL,
                        IsPrimary BIT NOT NULL CONSTRAINT DF_EC_StaffEmergencyContacts_IsPrimary DEFAULT (0),
                        Notes NVARCHAR(500) NULL,
                        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_EC_StaffEmergencyContacts_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2(0) NULL,
                        CONSTRAINT PK_EC_StaffEmergencyContacts PRIMARY KEY CLUSTERED (Id),
                        CONSTRAINT FK_EC_StaffEmergencyContacts_Individual FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID) ON DELETE CASCADE
                    );
                    CREATE NONCLUSTERED INDEX IX_EC_StaffEmergencyContacts_IndividualId ON dbo.EC_StaffEmergencyContacts (IndividualId);
                END;");
            _tableEnsured = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed ensuring EC_StaffEmergencyContacts table");
        }
    }

    public async Task<List<StaffEmergencyContactDto>> GetContactsAsync(int individualId)
    {
        if (individualId <= 0) return [];

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var list = await db.StaffEmergencyContacts
            .AsNoTracking()
            .Where(c => c.IndividualId == individualId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.ContactName)
            .Select(c => new StaffEmergencyContactDto
            {
                Id = c.Id,
                IndividualId = c.IndividualId,
                ContactName = c.ContactName,
                Relationship = c.Relationship,
                PrimaryPhone = c.PrimaryPhone,
                SecondaryPhone = c.SecondaryPhone,
                Email = c.Email,
                Address = c.Address,
                IsPrimary = c.IsPrimary,
                Notes = c.Notes,
                CreatedAtUtc = c.CreatedAtUtc,
                UpdatedAtUtc = c.UpdatedAtUtc
            })
            .ToListAsync();

        return list;
    }

    public async Task<ServiceResult> SaveContactAsync(StaffEmergencyContactInputDto input)
    {
        if (input.IndividualId <= 0)
            return ServiceResult.Fail("A valid staff individual ID is required.");
        if (string.IsNullOrWhiteSpace(input.ContactName))
            return ServiceResult.Fail("Contact name is required.");
        if (string.IsNullOrWhiteSpace(input.Relationship))
            return ServiceResult.Fail("Relationship is required.");
        if (string.IsNullOrWhiteSpace(input.PrimaryPhone))
            return ServiceResult.Fail("Primary phone number is required.");

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var existingContacts = await db.StaffEmergencyContacts
            .Where(c => c.IndividualId == input.IndividualId)
            .ToListAsync();

        var shouldBePrimary = input.IsPrimary || existingContacts.Count == 0;

        if (input.Id > 0)
        {
            var entity = existingContacts.FirstOrDefault(c => c.Id == input.Id);
            if (entity == null)
                return ServiceResult.Fail("Emergency contact record not found.");

            if (shouldBePrimary)
            {
                foreach (var c in existingContacts.Where(x => x.Id != input.Id))
                    c.IsPrimary = false;
            }

            entity.ContactName = input.ContactName.Trim();
            entity.Relationship = input.Relationship.Trim();
            entity.PrimaryPhone = input.PrimaryPhone.Trim();
            entity.SecondaryPhone = input.SecondaryPhone?.Trim();
            entity.Email = input.Email?.Trim();
            entity.Address = input.Address?.Trim();
            entity.IsPrimary = shouldBePrimary;
            entity.Notes = input.Notes?.Trim();
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            if (shouldBePrimary)
            {
                foreach (var c in existingContacts)
                    c.IsPrimary = false;
            }

            var newEntity = new StaffEmergencyContact
            {
                IndividualId = input.IndividualId,
                ContactName = input.ContactName.Trim(),
                Relationship = input.Relationship.Trim(),
                PrimaryPhone = input.PrimaryPhone.Trim(),
                SecondaryPhone = input.SecondaryPhone?.Trim(),
                Email = input.Email?.Trim(),
                Address = input.Address?.Trim(),
                IsPrimary = shouldBePrimary,
                Notes = input.Notes?.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.StaffEmergencyContacts.Add(newEntity);
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Emergency contact saved successfully.");
    }

    public async Task<ServiceResult> DeleteContactAsync(int id)
    {
        if (id <= 0) return ServiceResult.Fail("Invalid contact ID.");

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var entity = await db.StaffEmergencyContacts.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return ServiceResult.Fail("Contact not found.");

        var individualId = entity.IndividualId;
        var wasPrimary = entity.IsPrimary;

        db.StaffEmergencyContacts.Remove(entity);
        await db.SaveChangesAsync();

        if (wasPrimary)
        {
            var remaining = await db.StaffEmergencyContacts.FirstOrDefaultAsync(c => c.IndividualId == individualId);
            if (remaining != null)
            {
                remaining.IsPrimary = true;
                await db.SaveChangesAsync();
            }
        }

        return ServiceResult.Ok("Emergency contact deleted successfully.");
    }

    public async Task<ServiceResult> SetPrimaryAsync(int id, int individualId)
    {
        if (id <= 0 || individualId <= 0) return ServiceResult.Fail("Invalid parameters.");

        await using var db = await factory.CreateDbContextAsync();
        await EnsureTableAsync(db);

        var contacts = await db.StaffEmergencyContacts
            .Where(c => c.IndividualId == individualId)
            .ToListAsync();

        if (!contacts.Any(c => c.Id == id))
            return ServiceResult.Fail("Emergency contact not found.");

        foreach (var c in contacts)
        {
            c.IsPrimary = (c.Id == id);
            c.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Primary emergency contact updated successfully.");
    }
}
