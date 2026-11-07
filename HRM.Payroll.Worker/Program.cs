using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Payroll.Worker;
using HRM.Services.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       builder.Configuration.GetConnectionString("HrmTeContext") ??
                       "Server=(localdb)\\mssqllocaldb;Database=HRM_Database;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContextFactory<HrmTeContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Register Payroll services
builder.Services.AddSingleton<IPayrollCalculationEngine, PayrollCalculationEngine>();
builder.Services.AddScoped<IPayrollQueueService, PayrollQueueService>();
builder.Services.AddScoped<IPayrollSnapshotService, PayrollSnapshotService>();
builder.Services.AddScoped<IPayrollProcessor, PayrollProcessor>();
builder.Services.AddScoped<IPayrollApprovalService, PayrollApprovalService>();
builder.Services.AddScoped<IPayrollPaymentService, PayrollPaymentService>();
builder.Services.AddScoped<IPayslipService, PayslipService>();

// Register Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
