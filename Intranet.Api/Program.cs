using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Intranet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = builder.Configuration["Auth:Authority"];
        o.Audience = builder.Configuration["Auth:Audience"];
        o.TokenValidationParameters.ValidateIssuer = true;
        o.TokenValidationParameters.ValidateAudience = true;
        o.TokenValidationParameters.ValidateLifetime = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Intranet.Application.Security.IOrganisationContext, Intranet.Infrastructure.Security.HttpOrganisationContext>();
builder.Services.AddScoped<Intranet.Application.Services.IPostService, Intranet.Infrastructure.Services.PostService>();
builder.Services.AddScoped<Intranet.Application.Services.INewJoinerService, Intranet.Infrastructure.Services.NewJoinerService>();
var cs = builder.Configuration.GetConnectionString("IntranetDb");
builder.Services.AddDbContext<IntranetDbContext>(o => o.UseSqlServer(cs));
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok("healthy"));
app.Run();