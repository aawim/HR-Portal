using System;
using System.Linq;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Staff;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services;

public sealed class StaffIdentityDocumentService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    ILogger<StaffIdentityDocumentService> logger) : StaffIdentityDocumentServiceContracts.IOperations
{
    public async Task<StaffIdentityDocumentDto> GetIdentityDocumentsAsync(int individualId)
    {
        if (individualId <= 0) return new() { IndividualId = individualId };

        await using var db = await factory.CreateDbContextAsync();

        var idCard = await db.Idcards
            .AsNoTracking()
            .Where(c => c.BusinessEntityId == individualId)
            .OrderByDescending(c => c.IdcardId)
            .Select(c => new { c.IdcardNumber, c.ExpiryDate })
            .FirstOrDefaultAsync();

        var passport = await db.Passports
            .AsNoTracking()
            .Where(p => p.BusinessEntityId == individualId)
            .OrderByDescending(p => p.PassportId)
            .Select(p => new
            {
                p.PassportNumber,
                p.CountryId,
                CountryName = p.Country.NameEnglish,
                p.IssuedDate,
                p.ExpiryDate
            })
            .FirstOrDefaultAsync();

        return new StaffIdentityDocumentDto
        {
            IndividualId = individualId,
            IdentityCardNumber = idCard?.IdcardNumber,
            IdentityCardExpiryDate = idCard?.ExpiryDate,
            PassportNumber = passport?.PassportNumber,
            PassportCountryId = passport?.CountryId,
            PassportCountryName = passport?.CountryName,
            PassportIssuedDate = passport?.IssuedDate,
            PassportExpiryDate = passport?.ExpiryDate
        };
    }

    public async Task<ServiceResult> UpdateIdentityDocumentsAsync(StaffIdentityDocumentUpdateInput input)
    {
        if (input.IndividualId <= 0)
            return ServiceResult.Fail("A valid staff individual ID is required.");

        var actor = await access.RequireContextAsync();
        var isSelf = actor.IndividualId != 0 && actor.IndividualId == input.IndividualId;
        if (!isSelf && !(actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin ||
            await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Employee.Create") || await access.HasPermissionAsync("Staff.Manage")))
        {
            return ServiceResult.Fail("You do not have permission to update identity documents.");
        }

        await using var db = await factory.CreateDbContextAsync();

        var individual = await db.Individuals
            .FirstOrDefaultAsync(i => i.BusinessEntityId == input.IndividualId);

        if (individual == null)
            return ServiceResult.Fail("Individual record not found.");

        // 1. Process ID Card
        if (!string.IsNullOrWhiteSpace(input.IdentityCardNumber))
        {
            var cleanNid = input.IdentityCardNumber.Trim().ToUpperInvariant();
            var existingCard = await db.Idcards
                .FirstOrDefaultAsync(c => c.BusinessEntityId == input.IndividualId);

            if (existingCard != null)
            {
                // Check if another person has this ID Card
                var conflict = await db.Idcards
                    .AnyAsync(c => c.BusinessEntityId != input.IndividualId && c.IdcardNumber == cleanNid);
                if (conflict)
                    return ServiceResult.Fail($"ID Card number {cleanNid} is already registered to another individual.");

                existingCard.IdcardNumber = cleanNid;
                existingCard.ExpiryDate = input.IdentityCardExpiryDate;
                existingCard.IdcardStateId = 1;
            }
            else
            {
                var conflict = await db.Idcards
                    .AnyAsync(c => c.IdcardNumber == cleanNid);
                if (conflict)
                    return ServiceResult.Fail($"ID Card number {cleanNid} is already registered to another individual.");

                db.Idcards.Add(new Idcard
                {
                    BusinessEntityId = input.IndividualId,
                    IdcardNumber = cleanNid,
                    ExpiryDate = input.IdentityCardExpiryDate,
                    IdcardStateId = 1
                });
            }
        }
        else if (input.IdentityCardExpiryDate.HasValue)
        {
            var existingCard = await db.Idcards
                .FirstOrDefaultAsync(c => c.BusinessEntityId == input.IndividualId);
            if (existingCard != null)
            {
                existingCard.ExpiryDate = input.IdentityCardExpiryDate;
            }
        }

        // 2. Process Passport
        if (!string.IsNullOrWhiteSpace(input.PassportNumber))
        {
            var cleanPassport = input.PassportNumber.Trim().ToUpperInvariant();
            var defaultCountry = input.PassportCountryId ?? individual.CountryId ?? 1;

            var existingPassport = await db.Passports
                .FirstOrDefaultAsync(p => p.BusinessEntityId == input.IndividualId);

            if (existingPassport != null)
            {
                existingPassport.PassportNumber = cleanPassport;
                if (input.PassportCountryId.HasValue) existingPassport.CountryId = input.PassportCountryId.Value;
                existingPassport.IssuedDate = input.PassportIssuedDate;
                existingPassport.ExpiryDate = input.PassportExpiryDate;
                existingPassport.PassportStateId = 1;
            }
            else
            {
                db.Passports.Add(new Passport
                {
                    BusinessEntityId = input.IndividualId,
                    PassportNumber = cleanPassport,
                    CountryId = defaultCountry,
                    IssuedDate = input.PassportIssuedDate,
                    ExpiryDate = input.PassportExpiryDate,
                    PassportStateId = 1
                });
            }
        }
        else if (input.PassportExpiryDate.HasValue || input.PassportIssuedDate.HasValue)
        {
            var existingPassport = await db.Passports
                .FirstOrDefaultAsync(p => p.BusinessEntityId == input.IndividualId);
            if (existingPassport != null)
            {
                if (input.PassportExpiryDate.HasValue) existingPassport.ExpiryDate = input.PassportExpiryDate;
                if (input.PassportIssuedDate.HasValue) existingPassport.IssuedDate = input.PassportIssuedDate;
                if (input.PassportCountryId.HasValue) existingPassport.CountryId = input.PassportCountryId.Value;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Updated identity documents for staff/individual {IndividualId}", input.IndividualId);
        return ServiceResult.Ok("Identity documents (ID Card / Passport) updated successfully.");
    }
}
