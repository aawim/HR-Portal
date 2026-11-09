using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class IndividualPhotoTests
{
    private static readonly byte[] ValidPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==");

    private static (IndividualRegistryService Service, QueryFactory Factory, FakeAccess Access) CreateService()
    {
        var factory = new QueryFactory();
        var access = new FakeAccess();
        access.Permissions.Add("Individual.View");
        access.Permissions.Add("Individual.Edit");
        access.Permissions.Add("Admin");

        var logs = StubProxy.For<IOperationLogService>((method, args) =>
        {
            if (method == "CreateAsync")
            {
                var db = (HrmTeContext)args[0]!;
                var log = new OperationLog();
                db.OperationLogs.Add(log);
                return Task.FromResult(log);
            }
            throw new NotImplementedException(method);
        });

        var service = new IndividualRegistryService(factory, access, logs);
        return (service, factory, access);
    }

    [Fact]
    public async Task GetPhotoAsync_ReturnsDataUrl_WhenPhotoExists()
    {
        var (service, factory, _) = CreateService();
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 501,
                FirstNameEnglish = "Test",
                ImageData = ValidPng.ToArray(),
                ImageContentType = "image/png"
            });
            await db.SaveChangesAsync();
        }

        var url = await service.GetPhotoAsync(501);
        Assert.NotNull(url);
        Assert.StartsWith("data:image/png;base64,", url);
    }

    [Fact]
    public async Task GetPhotoAsync_ReturnsNull_WhenPhotoDoesNotExist()
    {
        var (service, factory, _) = CreateService();
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 502,
                FirstNameEnglish = "Test"
            });
            await db.SaveChangesAsync();
        }

        var url = await service.GetPhotoAsync(502);
        Assert.Null(url);
    }

    [Fact]
    public async Task UpdatePhotoAsync_UpdatesPhotoSuccessfully()
    {
        var (service, factory, _) = CreateService();
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 503,
                FirstNameEnglish = "Test"
            });
            await db.SaveChangesAsync();
        }

        await service.UpdatePhotoAsync(503, ValidPng);

        await using (var db = factory.CreateDbContext())
        {
            var ind = await db.Individuals.SingleAsync(x => x.BusinessEntityId == 503);
            Assert.NotNull(ind.ImageData);
            Assert.Equal("image/png", ind.ImageContentType);
            Assert.True(ind.ImageData.SequenceEqual(ValidPng));
            Assert.True(await db.OperationLogs.AnyAsync());
        }
    }

    [Fact]
    public async Task UpdatePhotoAsync_RemovesPhoto_WhenNullProvided()
    {
        var (service, factory, _) = CreateService();
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 504,
                FirstNameEnglish = "Test",
                ImageData = ValidPng.ToArray(),
                ImageContentType = "image/png"
            });
            await db.SaveChangesAsync();
        }

        await service.UpdatePhotoAsync(504, null);

        await using (var db = factory.CreateDbContext())
        {
            var ind = await db.Individuals.SingleAsync(x => x.BusinessEntityId == 504);
            Assert.Null(ind.ImageData);
            Assert.Null(ind.ImageContentType);
        }
    }

    [Fact]
    public async Task UpdatePhotoAsync_Throws_OnCorruptedImage()
    {
        var (service, factory, _) = CreateService();
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 505,
                FirstNameEnglish = "Test"
            });
            await db.SaveChangesAsync();
        }

        var corruptBytes = System.Text.Encoding.UTF8.GetBytes("not-an-image-payload");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdatePhotoAsync(505, corruptBytes));
    }

    [Fact]
    public async Task UpdatePhotoAsync_Throws_WhenUnauthorized()
    {
        var (service, factory, access) = CreateService();
        access.Permissions.Clear();
        access.Actor = new HRM.DTOs.UserContext.UserContextDto { UserId = 99, IndividualId = 99, IsSuperAdministrator = false };

        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 506,
                FirstNameEnglish = "Test"
            });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdatePhotoAsync(506, ValidPng));
    }

    [Fact]
    public async Task UpdatePhotoAsync_Allows_Self_Without_HRPermissions()
    {
        var (service, factory, access) = CreateService();
        access.Permissions.Clear();
        access.Actor = new HRM.DTOs.UserContext.UserContextDto { UserId = 88, IndividualId = 508, IsSuperAdministrator = false };

        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 508,
                FirstNameEnglish = "SelfUser"
            });
            await db.SaveChangesAsync();
        }

        await service.UpdatePhotoAsync(508, ValidPng);

        await using (var db = factory.CreateDbContext())
        {
            var ind = await db.Individuals.SingleAsync(x => x.BusinessEntityId == 508);
            Assert.NotNull(ind.ImageData);
            Assert.Equal("image/png", ind.ImageContentType);
        }
    }

    [Fact]
    public async Task SearchAsync_IncludesPhotoUrl_WhenIndividualHasPhoto()
    {
        var (service, factory, _) = CreateService();
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = 507,
                FirstNameEnglish = "Aishath",
                LastNameEnglish = "Ahmed",
                ImageData = ValidPng.ToArray(),
                ImageContentType = "image/png"
            });
            await db.SaveChangesAsync();
        }

        var results = await service.SearchAsync("Aishath", "Name");
        Assert.Single(results);
        Assert.NotNull(results[0].ImageUrl);
        Assert.StartsWith("data:image/png;base64,", results[0].ImageUrl);
    }
}
