using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.Services;

public sealed record UserThemePreference(int? UserId, string ThemeMode, int FontSizePercent = 100);
