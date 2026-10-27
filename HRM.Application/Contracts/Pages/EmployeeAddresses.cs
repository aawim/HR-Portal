using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public sealed record AddressDirectory(List<DirectoryOption> Countries, List<DirectoryOption> Atolls,
    List<DirectoryOption> Islands, List<DirectoryOption> InstanceTypes, List<DirectoryOption> BaseTypes,
    List<DirectoryOption> LocationTypes);

public interface IEmployeeAddresses
{
    Task<AddressDirectory> GetDirectoryAsync(int? atollId = null, CancellationToken ct = default);
    Task<List<AddressCardDto>> GetLeaveAddressesAsync(int? applicantJobId = null);
    Task<ServiceResult> CreateAddressAsync(AddressCreateDto application, int? applicantJobId = null);
}
