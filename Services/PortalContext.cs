using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using HRM.Models;
using HRM.Enum;


namespace HRM.Services
{

    public class PortalContext
    {
        public event Action? OnChange;

        public bool IsAdminPortal { get; private set; }

        public bool CanAccessAdminPortal { get; private set; }

        public int? CurrentOrganisationId { get; private set; }

        public string OrganisationName { get; private set; } = "";

        public TransactionContext CurrentContext { get; private set; }
                = TransactionContext.Online;


        public void Initialize(
            int? organisationId,
            string organisationName,
            bool canAccessAdminPortal)
        {
            CurrentOrganisationId = organisationId;
            OrganisationName = organisationName;
            CanAccessAdminPortal = canAccessAdminPortal;

            OnChange?.Invoke();
        }

        public void SwitchToAdmin()
        {
            if (!CanAccessAdminPortal)
                return;

            IsAdminPortal = true;
            OnChange?.Invoke();
        }

        public void SwitchToEmployee()
        {
            IsAdminPortal = false;
            OnChange?.Invoke();
        }


    }


    //public class PortalContext
    //{
    //    private readonly AuthenticationStateProvider _authStateProvider;
    //    private readonly IDbContextFactory<HrmTeContext> _dbContextFactory;

    //    // Properties
    //    public bool IsAdminPortal { get; private set; }
    //    public bool CanAccessAdminPortal { get; private set; }
    //    public int CurrentOrganisationId { get; private set; }

    //    // Event to notify the UI when state changes
    //    public event Action? OnChange;

    //    // Inject Auth State and DB Factory
    //    public PortalContext(AuthenticationStateProvider authStateProvider, IDbContextFactory<HrmTeContext> dbContextFactory)
    //    {
    //        _authStateProvider = authStateProvider;
    //        _dbContextFactory = dbContextFactory;
    //    }



    //    // Call this method when your MainLayout loads
    //    public async Task InitializeSessionAsync(User user)
    //    {
    //        // Extract the Internal User ID we attached during OnTokenValidated

    //            using var db = _dbContextFactory.CreateDbContext();

    //            var userOrgId = await db.Users
    //                .Where(u => u.UserId == user.UserId)
    //                .Select(u => u.UserOrganisationId)
    //                .FirstOrDefaultAsync();

    //            bool isAdmin = await db.UserRoles
    //                .Include(ur => ur.Role)
    //                .AnyAsync(ur => ur.UserId == user.UserId
    //                             && ur.UserOrganisationId == userOrgId
    //                             && ur.Role.RoleKey == "SYS_ADMIN"
    //                             && ur.IsActive);

    //            CurrentOrganisationId = (int)userOrgId;
    //            CanAccessAdminPortal = isAdmin;
    //            IsAdminPortal = false;

    //            NotifyStateChanged();
    //        }


    //    public void TogglePortal()
    //    {
    //        if (CanAccessAdminPortal)
    //        {
    //            IsAdminPortal = !IsAdminPortal;
    //            NotifyStateChanged();
    //        }
    //    }

    //    private void NotifyStateChanged() => OnChange?.Invoke();
    //}
}
