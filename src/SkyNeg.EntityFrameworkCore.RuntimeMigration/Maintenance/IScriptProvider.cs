namespace SkyNeg.EntityFrameworkCore.RuntimeMigration.Sqlite
{
    public interface IScriptProvider
    {
        List<UpdateScript> GetUpdateScripts(Type dbType);

        Task<List<UpdateScript>> GetUpdateScriptsAsync(Type dbType, CancellationToken cancellationToken = default);
    }
}
