using HRM.DTOs;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class AddressService : IAddress
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _operationLogService;
        private readonly IIndividual _individual;
        private readonly IUserAccessService _access;

        public AddressService(IDbContextFactory<HrmTeContext> dbFactory, IOperationLogService operationLogService, IIndividual individual, IUserAccessService access)
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
            _individual = individual;
            _access = access;
        }

        public async Task<List<AddressCardDto>> GetLeaveAddressesAsync(int? applicantJobId = null)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var individualId = applicantJobId.HasValue
                ? (await HRM.Helpers.LeaveApplicantScope.ResolveAsync(context, _access, applicantJobId)).IndividualID
                : await _individual.GetIndividualID();

            //     return await context.BusinessEntityLocations
            //.Where(x => x.BusinessEntityId == individualId && x.IsValid)
            //.Select(x => new AddressCardDto
            //{
            //    LocationId = x.LocationId,

            //    Title = x.Location.Address != null &&
            //            x.Location.Address.AddressInstance != null &&
            //            x.Location.Address.AddressInstance.AddressInstanceType != null
            //        ? x.Location.Address.AddressInstance.AddressInstanceType.TypeName
            //        : "Address",

            //    Address = x.Location.Address != null &&
            //              x.Location.Address.AddressInstance != null
            //        ? x.Location.Address.AddressInstance.Name ?? ""
            //        : "",

            //    Floor = x.Location.Address != null &&
            //            x.Location.Address.AddressInstance != null
            //        ? x.Location.Address.AddressInstance.Floor
            //        : "",

            //    Country = x.Location.Country.NameEnglish,

            //    Island = x.Location.Address != null &&
            //             x.Location.Address.Island != null
            //        ? x.Location.Address.Island.NameEnglish
            //        : "",

            //    Atoll = x.Location.Address != null &&
            //            x.Location.Address.Island != null &&
            //            x.Location.Address.Island.Atoll != null
            //        ? x.Location.Address.Island.Atoll.NameEnglish
            //        : "",

            //    LocationType = x.BusinessEntityLocationType.TypeName == "Primary"
            //})
            //    .ToListAsync();


            var addresses = await context.BusinessEntityLocations
            .Where(x => x.BusinessEntityId == individualId && x.IsValid)
            .Select(x => new AddressCardDto
            {
                LocationId = x.LocationId,

                Title = x.Location.Address != null && x.Location.Address.AddressInstance != null && x.Location.Address.AddressInstance.AddressInstanceType != null
                    ? x.Location.Address.AddressInstance.AddressInstanceType.TypeName : "Address",
                CountryId = x.Location.CountryId,
                AtollId = x.Location.Address != null && x.Location.Address.Island != null
                    ? x.Location.Address.Island.AtollId : null,
                IslandId = x.Location.Address != null ? x.Location.Address.IslandId : null,

                Address = x.Location.Address != null && x.Location.Address.AddressInstance != null ? x.Location.Address.AddressInstance.Name ?? "" : "",

                Floor = x.Location.Address != null && x.Location.Address.AddressInstance != null ? x.Location.Address.AddressInstance.Floor : null,

                Island = x.Location.Address != null && x.Location.Address.Island != null ? x.Location.Address.Island.NameEnglish : "",

                Atoll = x.Location.Address != null && x.Location.Address.Island != null ? x.Location.Address.Island.Atoll.NameEnglish : "",

                Country = x.Location.Country.NameEnglish,

                LocationType = x.BusinessEntityLocationType.TypeName ?? "Address",

                IsCurrent = x.Location.Address != null && x.Location.Address.AddressInstance != null &&
                    x.Location.Address.AddressInstance.AddressBase.IsCurrent == true
            })
            .ToListAsync();

            var contacts = await context.BussinessEntityContactInformations.AsNoTracking()
                .Where(x => x.BussinessEntityId == individualId && x.IsValid &&
                    (x.ContactInformationTypeId == 1 || x.ContactInformationTypeId == 2 || x.ContactInformationTypeId == 4) &&
                    x.Value != null && x.Value.Trim() != "")
                .OrderBy(x => x.ContactInformationTypeId)
                .ThenByDescending(x => x.BussinessEntityContactInformationId)
                .Select(x => new HRM.DTOs.Profile.ProfileContactDto
                {
                    ContactId = x.BussinessEntityContactInformationId,
                    ContactInformationTypeId = x.ContactInformationTypeId,
                    ContactTypeName = x.ContactInformationType.TypeName,
                    Value = x.Value,
                    IsValid = true
                })
                .ToListAsync();

            foreach (var address in addresses)
            {
                address.Contacts = contacts.ToList();
                address.Phone = contacts.FirstOrDefault(x => x.ContactInformationTypeId is 1 or 2)?.Value.Trim();
                address.Email = contacts.FirstOrDefault(x => x.ContactInformationTypeId == 4)?.Value.Trim();
            }
            return addresses;

        }


        public async Task<ServiceResult> CreateAddressAsync(AddressCreateDto model, int? applicantJobId = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var owner = applicantJobId.HasValue
                ? (await HRM.Helpers.LeaveApplicantScope.ResolveAsync(db, _access, applicantJobId)).IndividualID
                : await _individual.GetIndividualID();
            if (owner <= 0) return ServiceResult.Fail("Your employee identity is unavailable.");
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var audit = await _operationLogService.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LOCATION_CREATE, $"Created personal address for Individual {owner}");
            var result = await BusinessEntityAddressWriter.SaveAsync(db, owner, new AddressMutation {
                UsageId = model.BusinessEntityLocationTypeId, BaseTypeId = model.AddressBaseTypeId,
                InstanceTypeId = model.AddressInstanceTypeId, CountryId = model.CountryId, IslandId = model.IslandId,
                Name = model.Name, Floor = model.Floor, IsCurrent = model.IsCurrent, ClearOtherCurrent = model.IsCurrent
            }, audit.OperationLogId);
            if (!result.Success) { await tx.RollbackAsync(); return result; }
            await tx.CommitAsync(); return result;
        }
        public async Task<List<Country>> GetCountriesAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Countries
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();
        }

        public async Task<List<Atoll>> GetAtollsAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Atolls
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();
        }

        public async Task<List<Island>> GetIslandsAsync(int atollId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Islands
                .Where(x => x.AtollId == atollId)
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();
        }

        public async Task<List<AddressInstanceType>> GetAddressInstanceTypesAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.AddressInstanceTypes
                .OrderBy(x => x.TypeName)
                .ToListAsync();
        }


        public async Task<List<AddressBaseType>> GetAddressBaseTypesAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.AddressBaseTypes
                .OrderBy(x => x.TypeName)
                .ToListAsync();
        }




        




        public async Task<List<BusinessEntityLocationType>> GetBusinessEntityLocationTypesAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.BusinessEntityLocationTypes
                .OrderBy(x => x.TypeName)
                .ToListAsync();
        }





    }
}
