using HRM.Models.GoodsLoans;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<GoodsLoanStore> GoodsLoanStores => Set<GoodsLoanStore>();
    public DbSet<GoodsLoanStoreAgreement> GoodsLoanStoreAgreements => Set<GoodsLoanStoreAgreement>();
    public DbSet<GoodsLoanDeductionPolicy> GoodsLoanDeductionPolicies => Set<GoodsLoanDeductionPolicy>();
    public DbSet<GoodsLoan> GoodsLoans => Set<GoodsLoan>();
    public DbSet<GoodsLoanItem> GoodsLoanItems => Set<GoodsLoanItem>();
    public DbSet<GoodsLoanInstallment> GoodsLoanInstallments => Set<GoodsLoanInstallment>();
    public DbSet<GoodsLoanPayrollDeduction> GoodsLoanPayrollDeductions => Set<GoodsLoanPayrollDeduction>();
    public DbSet<GoodsLoanStoreSettlement> GoodsLoanStoreSettlements => Set<GoodsLoanStoreSettlement>();
    public DbSet<GoodsLoanStoreSettlementAllocation> GoodsLoanStoreSettlementAllocations => Set<GoodsLoanStoreSettlementAllocation>();
    public DbSet<GoodsLoanStatusHistory> GoodsLoanStatusHistories => Set<GoodsLoanStatusHistory>();
    public DbSet<GoodsLoanDocument> GoodsLoanDocuments => Set<GoodsLoanDocument>();
}
