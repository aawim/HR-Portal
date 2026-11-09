using HRM.Models;
using HRM.Services.Interfaces;
using HRM.DTOs.Registry;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

/// <summary>Registry reads bound to the authenticated HR officer, never a caller-supplied tenant.</summary>
public sealed class IndividualRegistryService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService? logs = null) : IIndividualRegistryService
{
    public async Task<List<RegistryIndividualDto>> SearchAsync(string searchTerm, string searchType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return [];
        if (searchTerm.Length > 200) throw new ArgumentException("Search text is too long.");
        var actor = await access.RequireContextAsync();
        if (!(actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin ||
            await access.HasPermissionAsync("Admin")))
            throw new UnauthorizedAccessException("Registry access requires HR permission.");
        var organisationId = actor.IsSuperAdministrator ? 0 : await access.GetCurrentOrganisationIdAsync();
        if (!actor.IsSuperAdministrator && (organisationId <= 0 || !await access.HasOrganisationAccessAsync(organisationId)))
            throw new UnauthorizedAccessException("You do not have access to the selected organisation.");

        await using var DbContext = await factory.CreateDbContextAsync(ct);
        // This registry view is for unattached individuals, including for Super Admin.
        var individuals = DbContext.Individuals.AsNoTracking().Where(ind =>
            !DbContext.Jobs.Any(j => j.IndividualID == ind.BusinessEntityId) &&
            (actor.IsSuperAdministrator || !DbContext.StaffOnboardingRequests.Any(r =>
                r.IndividualId == ind.BusinessEntityId && r.IsOpen && r.OrganisationId != organisationId)));

        var query = from ind in individuals
                    join id in DbContext.Idcards on ind.BusinessEntityId equals id.BusinessEntityId into idGroup
                    from idCard in idGroup.DefaultIfEmpty()
                    join p in DbContext.Passports on ind.BusinessEntityId equals p.BusinessEntityId into pGroup
                    from passport in pGroup.DefaultIfEmpty()
                    select new { ind, idCard, passport };

        string term = searchTerm.Trim();

        if (searchType == "ID")
        {
            query = query.Where(q => q.idCard != null && q.idCard.IdcardNumber == term);
        }
        else if (searchType == "Passport")
        {
            query = query.Where(q => q.passport != null && q.passport.PassportNumber == term);
        }
        else
        {
            // Match each word across the name fields, so a full name such as
            // "Ahmed Farish" also matches separate first/last name columns.
            foreach (var word in term.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                var namePart = word;
                query = query.Where(q =>
                    (q.ind.FirstNameEnglish != null && q.ind.FirstNameEnglish.Contains(namePart)) ||
                    (q.ind.MiddleNameEnglish != null && q.ind.MiddleNameEnglish.Contains(namePart)) ||
                    (q.ind.LastNameEnglish != null && q.ind.LastNameEnglish.Contains(namePart)) ||
                    (q.idCard != null && q.idCard.IdcardNumber == term) ||
                    (q.passport != null && q.passport.PassportNumber == term));
            }
        }

        var results = await query.OrderBy(q => q.ind.FirstNameEnglish)
            .ThenBy(q => q.ind.LastNameEnglish).Take(50).ToListAsync(ct);

        return results.Select(q => new RegistryIndividualDto
        {
            BusinessEntityId = q.ind.BusinessEntityId,
            FullName = $"{q.ind.FirstNameEnglish} {q.ind.MiddleNameEnglish} {q.ind.LastNameEnglish}".Replace("  ", " ").Trim(),
            IDNumber = q.idCard != null ? q.idCard.IdcardNumber : string.Empty,
            PassportNumber = q.passport != null ? q.passport.PassportNumber : string.Empty,
            Address = q.ind.Country?.ToString() ?? string.Empty,
            Nationality = q.ind.Country?.ToString() ?? string.Empty,
            ImageUrl = IndividualImage.DataUrl(q.ind.ImageData, q.ind.ImageContentType)
        }).ToList();
    }

    public async Task<string?> GetPhotoAsync(int individualId, CancellationToken ct = default)
    {
        if (individualId <= 0) return null;
        var actor = await access.RequireContextAsync();
        var isSelf = actor.IndividualId != 0 && actor.IndividualId == individualId;
        if (!isSelf && !(actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin || actor.IsSupervisor ||
            await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Individual.View") || await access.HasPermissionAsync("Employee.View")))
            throw new UnauthorizedAccessException("Registry access requires HR permission.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var individual = await db.Individuals.AsNoTracking()
            .FirstOrDefaultAsync(i => i.BusinessEntityId == individualId, ct);
        if (individual == null) return null;
        return IndividualImage.DataUrl(individual.ImageData, individual.ImageContentType);
    }

    public async Task UpdatePhotoAsync(int individualId, byte[]? imageData, CancellationToken ct = default)
    {
        if (individualId <= 0) throw new ArgumentException("A valid individual ID is required.", nameof(individualId));
        var actor = await access.RequireContextAsync();
        var isSelf = actor.IndividualId != 0 && actor.IndividualId == individualId;
        if (!isSelf && !(actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin ||
            await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Individual.Edit") || await access.HasPermissionAsync("Individual.Create")))
            throw new UnauthorizedAccessException("Updating an individual photo requires HR permission.");

        var imageType = IndividualImage.Validate(imageData);

        await using var db = await factory.CreateDbContextAsync(ct);
        var individual = await db.Individuals.FirstOrDefaultAsync(i => i.BusinessEntityId == individualId, ct);
        if (individual == null)
            throw new InvalidOperationException("Individual record was not found.");

        individual.ImageData = imageData?.ToArray();
        individual.ImageContentType = imageType;

        if (logs != null)
        {
            await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE,
                imageData != null ? $"Updated photo for individual {individualId}." : $"Removed photo for individual {individualId}.");
        }

        await db.SaveChangesAsync(ct);
    }
}
