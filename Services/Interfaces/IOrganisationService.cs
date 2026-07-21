using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.Services
{
    public interface IOrganisationService
    {
        //Task<List<OrganisationResultDto>> SearchOrganisationsAsync(string searchTerm, string searchType);
        //Task<OrganisationResultDto?> GetOrganisationDetailsAsync(int organisationId);
        //Task<List<OrgNodeDto>> GetOrganisationTreeAsync(int organisationId);
        //Task<List<DropdownItem>> GetOrganisationTypesAsync();
        //Task<List<DropdownItem>> GetCountriesAsync();
        //Task<List<DropdownItem>> GetAllOrganisationsAsync();
        //Task<OrganisationDto?> GetUserOrganisationAsync(int userId);


        Task<List<OrganisationResultDto>> SearchOrganisationsAsync(string searchTerm,string searchType);


        Task<OrganisationResultDto?> GetOrganisationDetailsAsync(int organisationId);


        Task<List<OrgNodeDto>> GetOrganisationTreeAsync(int organisationId);


        Task<List<DropdownItem>> GetOrganisationTypesAsync();


        Task<List<DropdownItem>> GetCountriesAsync();


        Task<List<DropdownItem>> GetAllOrganisationsAsync();



        // User Context related

        Task<UserOrganisationDto?> GetOrganisationAsync(int businessEntityId);


        Task<UserOrganisationDto?> GetUserOrganisationAsync(int userId);

    }
}