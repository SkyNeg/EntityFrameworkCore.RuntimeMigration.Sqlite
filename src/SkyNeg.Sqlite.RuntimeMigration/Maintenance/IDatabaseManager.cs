namespace SkyNeg.Sqlite.RuntimeMigration
{
    public interface IDatabaseManager
    {
        /// <summary>
        /// Retrieves current version of the database for the component.
        /// </summary>
        /// <param name="component">Name of the component (core, genetec, ccure, etc)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="Version"/> of the component or null for unknown version</returns>
        Task<Version> GetVersionAsync(string component, CancellationToken cancellationToken = default);

        /// <summary>
        /// Apply schema and data updates to upgrade database to the latest version
        /// </summary>
        /// <param name="component">Component that requires to update database</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<DatabaseUpdateResult> UpdateAsync(string component, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates schema and tables for the component. Database is created up to date and does not require to call <see cref="UpdateAsync(string, CancellationToken)"/> method
        /// </summary>
        /// <param name="component">Component that requires to update database</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<DatabaseUpdateResult> CreateAsync(string component, CancellationToken cancellationToken = default);
    }
}
