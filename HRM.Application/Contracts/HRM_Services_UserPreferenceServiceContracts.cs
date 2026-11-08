using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class UserPreferenceServiceContracts
{

    public static readonly IReadOnlyList<int> AllowedFontSizes = Array.AsReadOnly(new[] { 90, 100, 110, 120, 130 });

public interface IOperations
{
Task<UserThemePreference> GetThemeAsync()
;
Task SaveThemeAsync(string theme, int expectedUserId)
;
Task SaveFontSizeAsync(int percent, int expectedUserId)
;
}
}
