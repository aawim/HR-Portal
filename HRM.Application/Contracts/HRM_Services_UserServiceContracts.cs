using HRM.Services.Interfaces;
using HRM.DTOs.UserContext;
using HRM.DTOs;
namespace HRM.Services.Contracts;
public abstract class UserServiceContracts
{

public interface IOperations
{
Task<UserProfileDto?> GetByIdAsync(int userId)
;
Task<UserProfileDto?> GetByUsernameAsync(string username)
;
Task<UserProfileDto?> GetByBusinessEntityAsync(int businessEntityId)
;
Task<UserProfileDto?> GetMyProfileAsync()
;
[Obsolete("Use UserSettingsService.SaveAccountAsync.")]
Task<bool> UpdateUserProfileAsync(UserProfileDto updatedUser) ;
Task<bool> UpdateUserAsync(UserProfileDto profile)
;
}
}
