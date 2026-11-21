using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


using HRM.Models;
using HRM.Enum;


namespace HRM.Services
{

    public class PortalContext
    {
        public event Action? OnChange;

        public bool IsAdminPortal { get; private set; }
        public bool IsDeveloperPortal { get; private set; }
        public bool IsAuditPortal { get; private set; }

        public bool CanAccessAdminPortal { get; private set; }
        public bool CanAccessDeveloperPortal { get; private set; }
        public bool CanAccessAuditPortal { get; private set; }

        public int? CurrentOrganisationId { get; private set; }

        public string OrganisationName { get; private set; } = "";

        public TransactionContext CurrentContext { get; private set; }
                = TransactionContext.Online;


        public void Initialize(
            int? organisationId,
            string organisationName,
            bool canAccessAdminPortal,
            bool canAccessDeveloperPortal = false,
            bool canAccessAuditPortal = false)
        {
            CurrentOrganisationId = organisationId;
            OrganisationName = organisationName;
            CanAccessAdminPortal = canAccessAdminPortal;
            CanAccessDeveloperPortal = canAccessDeveloperPortal;
            CanAccessAuditPortal = canAccessAuditPortal;

            if (!canAccessAdminPortal) IsAdminPortal = false;
            if (!canAccessDeveloperPortal) IsDeveloperPortal = false;
            if (!canAccessAuditPortal) IsAuditPortal = false;

            CurrentContext = GetDefaultContext();
            OnChange?.Invoke();
        }

        private TransactionContext GetDefaultContext()
        {
            if (IsAdminPortal) return TransactionContext.Admin;
            if (IsDeveloperPortal) return TransactionContext.Developer;
            if (IsAuditPortal) return TransactionContext.Audit;
            return TransactionContext.Online;
        }

        public void SwitchToAdmin()
        {
            if (!CanAccessAdminPortal)
                return;

            IsAdminPortal = true;
            IsDeveloperPortal = false;
            IsAuditPortal = false;
            CurrentContext = TransactionContext.Admin;
            OnChange?.Invoke();
        }

        public void SwitchToEmployee()
        {
            IsAdminPortal = false;
            IsDeveloperPortal = false;
            IsAuditPortal = false;
            CurrentContext = TransactionContext.Online;
            OnChange?.Invoke();
        }

        public void SwitchToDeveloper()
        {
            if (!CanAccessDeveloperPortal)
                return;

            IsAdminPortal = false;
            IsDeveloperPortal = true;
            IsAuditPortal = false;
            CurrentContext = TransactionContext.Developer;
            OnChange?.Invoke();
        }

        public void SwitchToAudit()
        {
            if (!CanAccessAuditPortal)
                return;

            IsAdminPortal = false;
            IsDeveloperPortal = false;
            IsAuditPortal = true;
            CurrentContext = TransactionContext.Audit;
            OnChange?.Invoke();
        }

        public void SwitchToContext(TransactionContext context)
        {
            switch (context)
            {
                case TransactionContext.Admin:
                    SwitchToAdmin();
                    break;
                case TransactionContext.Developer:
                    SwitchToDeveloper();
                    break;
                case TransactionContext.Audit:
                    SwitchToAudit();
                    break;
                case TransactionContext.Online:
                default:
                    SwitchToEmployee();
                    break;
            }
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
