using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HRM.Caching
{
    public class UserContextCache : IUserContextCache
    {
        private readonly IMemoryCache _cache;


        public UserContextCache(
            IMemoryCache cache)
        {
            _cache = cache;
        }


        private string GetKey(int userId)
        {
            return $"USER_CONTEXT_{userId}";
        }

        public Task<UserContextDto?> GetAsync(
            int userId)
        {
            _cache.TryGetValue(
                GetKey(userId),
                out UserContextDto? context);


            return Task.FromResult(context);
        }



        public Task SetAsync(
            int userId,
            UserContextDto context)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(30),


                SlidingExpiration =
                    TimeSpan.FromMinutes(10)
            };


            _cache.Set(
                GetKey(userId),
                context,
                options);


            return Task.CompletedTask;
        }



        public Task RemoveAsync(
            int userId)
        {
            _cache.Remove(
                GetKey(userId));


            return Task.CompletedTask;
        }



        public Task ClearAsync()
        {
            // IMemoryCache does not expose Clear()
            // We will handle this with a key strategy later

            return Task.CompletedTask;
        }
    }
}
