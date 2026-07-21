namespace HRM.Services.Interfaces
{
    public interface IResourceBuilder<TResource, TKey>
    {
        Task<TResource?> BuildAsync(TKey id);
    }
}
