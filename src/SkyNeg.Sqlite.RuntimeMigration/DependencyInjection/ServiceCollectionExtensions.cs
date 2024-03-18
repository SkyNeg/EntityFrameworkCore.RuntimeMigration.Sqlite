using Microsoft.Extensions.DependencyInjection;

namespace SkyNeg.Sqlite.RuntimeMigration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntimeDbContextFactory<TContext>(this IServiceCollection collection) where TContext : RuntimeContext
        {
            collection.AddSingleton<IScriptProvider, ResourceScriptProvider>();
            collection.AddDbContextFactory<TContext>();
            return collection;
        }
    }
}
