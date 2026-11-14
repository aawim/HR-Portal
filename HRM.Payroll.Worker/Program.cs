using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Payroll.Worker;
using HRM.Services.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HRM.ErrorLogs.Extensions;
using HRM.ErrorLogs.Services;
using HRM.Services.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       builder.Configuration.GetConnectionString("HrmTeContext") ??
                       "Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=NewHRM_DB;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContextFactory<HrmTeContext>(options =>
{
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});

// Register Payroll services
builder.Services.AddSingleton<IErrorLogStore, SqlErrorLogStore>();
builder.Services.AddHrmErrorLogging("HRM.Payroll.Worker");
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
