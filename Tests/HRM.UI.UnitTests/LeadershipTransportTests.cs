using HRM.Application.Transport;
using HRM.Services.Contracts;
using Xunit;
namespace HRM.UI.UnitTests;
public sealed class LeadershipTransportTests
{
 [Fact]
 public void Every_leadership_operation_resolves_the_fixed_catalog()
 {
  foreach(var contract in new[]{typeof(LeadershipAdministrationServiceContracts.IOperations),typeof(LeadershipAuthorityServiceContracts.IOperations)})
  foreach(var method in contract.GetMethods())Assert.StartsWith("/api/v1/leadership-authority/",ApiEndpointCatalog.Resolve(contract,method).Path);
 }
}
