using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services
{
    public class GeographyService : IGeographyService
    {

        private readonly IDbContextFactory<HrmTeContext> _contextFactory;

        public GeographyService(IDbContextFactory<HrmTeContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Country>> GetCountriesAsync()
        {
            await using var context = _contextFactory.CreateDbContext();

            return await context.Countries
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();
        }
        public async Task<List<Atoll>> GetAtollsAsync()
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Atolls
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();
        }

        public async Task<List<Island>> GetIslandsAsync(int atollId)
        {
            await using var context = _contextFactory.CreateDbContext();

            return await context.Islands
                .Where(x => x.AtollId == atollId)
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();
        }
    }
}
