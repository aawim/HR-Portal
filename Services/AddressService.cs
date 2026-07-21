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

        public AddressService(IDbContextFactory<HrmTeContext> dbFactory, IOperationLogService operationLogService, IIndividual individual)
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
            _individual = individual;
        }

        public async Task<List<AddressCardDto>> GetLeaveAddressesAsync()
        {
            var individualId = await _individual.GetIndividualID();
            await using var context = await _dbFactory.CreateDbContextAsync();

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


            return await context.BusinessEntityLocations
            .Where(x => x.BusinessEntityId == individualId && x.IsValid)
            .Select(x => new AddressCardDto
            {
                LocationId = x.LocationId,

                Title = x.Location.Address.AddressInstance.AddressInstanceType.TypeName,

                Address = x.Location.Address.AddressInstance.Name,

                Floor = x.Location.Address.AddressInstance.Floor,

                Island = x.Location.Address.Island.NameEnglish,

                Atoll = x.Location.Address.Island.Atoll.NameEnglish,

                Country = x.Location.Country.NameEnglish,

                LocationType = x.BusinessEntityLocationType.TypeName,

                IsCurrent = (bool) x.Location.Address.AddressInstance.AddressBase.IsCurrent
            })
            .ToListAsync();

        }

        public async Task<ServiceResult> CreateAddressAsync(AddressCreateDto model)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            await using var transaction =
                await context.Database.BeginTransactionAsync();
            Console.WriteLine($"Before Location creation: CountryId = {model.CountryId}");
            try
            {
                Console.WriteLine($"Before Location creation: CountryId = {model.CountryId}");
                var businessEntityId =
                    await _individual.GetIndividualID();

                if (businessEntityId == 0)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                var operationLog =
                    await _operationLogService.CreateAsync(
                        context,
                        1,
                        "Address created");

                await context.SaveChangesAsync();


                Console.WriteLine($"Before Location creation: CountryId = {model.CountryId}");

                var location = new Location
                {
                    CountryId = model.CountryId,
                    OperationLogId = operationLog.OperationLogId,
                    LocationTypeId = 1
                };

                Console.WriteLine($"After Location creation: CountryId = {location.CountryId}");

                context.Locations.Add(location);

                await context.SaveChangesAsync();

                // Address
                var address = new Address
                {
                    LocationId = location.LocationId,
                    IslandId = model.IslandId
                };

                context.Addresses.Add(address);

                // Address Base
                var addressBase = new AddressBasis
                {
                    IsCurrent = model.IsCurrent,
                    AddressBaseTypeId = model.AddressBaseTypeId,


                };

                context.AddressBases.Add(addressBase);

                await context.SaveChangesAsync();

                // Address Instance
                var instance = new AddressInstance
                {
                    LocationId = location.LocationId,
                    Name = model.Name,
                    Floor = model.Floor,
                    AddressBaseId = addressBase.AddressBaseId,
                    AddressInstanceTypeId = model.AddressInstanceTypeId
                };

                context.AddressInstances.Add(instance);

                // Business Entity Location
                var bel = new BusinessEntityLocation
                {
                    BusinessEntityId = businessEntityId,
                    LocationId = location.LocationId,
                    BusinessEntityLocationTypeId =
                        model.BusinessEntityLocationTypeId,
                    IsValid = true
                };

                context.BusinessEntityLocations.Add(bel);

                if (model.IsCurrent)
                {
                    var currentAddresses = await context.BusinessEntityLocations
                        .Where(x => x.BusinessEntityId == businessEntityId)
                        .Select(x => x.Location.Address.AddressInstance.AddressBase)
                        .ToListAsync();

                    foreach (var item in currentAddresses)
                    {
                        item.IsCurrent = false;
                    }

                    await context.SaveChangesAsync();
                }

                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ServiceResult
                {
                    Success = true,
                    Message = "Address added successfully."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new ServiceResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
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
