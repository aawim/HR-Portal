using HRM.DTOs.Leave;
using HRM.Models;

namespace HRM.Services.Interfaces
{
    public interface IIndividual
    {
        Task<int> GetIndividualID();
    }
}
