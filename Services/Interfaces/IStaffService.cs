
using HRM.DTOs.Staff;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace HRM.Services.Interfaces
{
    public interface IStaffService
    {
        Task<List<IndividualResultDto>> SearchStaffAsync(string searchTerm, string searchType, int? organisationId);
    }
}
