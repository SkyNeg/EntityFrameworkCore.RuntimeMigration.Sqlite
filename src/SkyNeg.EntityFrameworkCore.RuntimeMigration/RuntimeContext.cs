using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.Sqlite;

namespace SkyNeg.EntityFrameworkCore.RuntimeMigration.Sqlite
{
    public abstract class RuntimeContext : DbContext
    {
        protected readonly IScriptProvider scriptProvider;

        protected abstract string Component { get; }

        public DbSet<ComponentVersion> ComponentVersions { get; set; }

        protected virtual string ConnectionString { get => $"Data Source=data.db"; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ConnectionString);
        }

        public RuntimeContext(IScriptProvider scriptProvider) : base()
        {
            this.scriptProvider = scriptProvider;
        }

        #region Database Updates
        protected virtual Version? GetVersion() => GetVersion(Component);

        private Version? GetVersion(string componentName)
        {
            try
            {
                var component = Find<ComponentVersion>(componentName);
                return component != null ? new Version(component.Version) : null;
            }
            catch (SqliteException)
            {
                return null;
            }
        }

        protected DatabaseUpdateResult UpdateDatabase()
        {
            var coreUpdateResult = UpdateComponentDatabase("_core", typeof(RuntimeContext));
            return UpdateComponentDatabase(Component, GetType());
        }

        private DatabaseUpdateResult UpdateComponentDatabase(string componentName, Type type)
        {
            if (type == null)
            {
                type = GetType();
            }

            var updates = scriptProvider.GetUpdateScripts(type);
            var currentVersion = GetVersion(componentName);
            var maxVersion = currentVersion;
            foreach (var updateToApply in updates.OrderBy(q => q.FromVersion))
            {
                if (maxVersion < updateToApply.ToVersion)
                {
                    maxVersion = updateToApply.ToVersion;
                }

                if (currentVersion == updateToApply.FromVersion)
                {
                    if (!ApplyUpdateScript(componentName, updateToApply))
                    {
                        return new DatabaseUpdateResult() { NewVersion = currentVersion, Error = $"Can not apply script {updateToApply}" };
                    }

                    currentVersion = updateToApply.ToVersion;
                }
            }

            if (currentVersion != maxVersion)
            {
                return new DatabaseUpdateResult() { NewVersion = currentVersion, Error = $"Can not find a script to update from {currentVersion} to {maxVersion}" };
            }

            return new DatabaseUpdateResult() { IsSuccess = true, NewVersion = currentVersion };
        }

        private bool ApplyUpdateScript(string component, UpdateScript updateScript)
        {
            try
            {
                var transaction = Database.BeginTransaction();
                foreach (var sqlCommand in updateScript.SqlCommands.Where(q => !string.IsNullOrWhiteSpace(q)))
                {
                    Database.ExecuteSqlRaw(sqlCommand);
                }

                //Update component version in database
                var componentVersion = Set<ComponentVersion>().FirstOrDefault(q => q.Component == component);
                if (componentVersion == null)
                {
                    componentVersion = new ComponentVersion();
                    componentVersion.Component = component;
                    Add(componentVersion);
                }
                componentVersion.Version = updateScript.ToVersion?.ToString() ?? string.Empty;
                SaveChanges();
                Database.CommitTransaction();
                return true;
            }
            catch
            {
                if (Database.CurrentTransaction != null)
                {
                    var rollbackTransactionId = Database.CurrentTransaction.TransactionId;
                    Database.RollbackTransactionAsync(CancellationToken.None);
                }
                throw;
            }
        }
        #endregion

        public bool EnsureDatabaseCreated()
        {
            if (!IsDatabaseExist())
            {
                CreateDatabase();
                if (!IsDatabaseExist())
                {
                    throw new Exception($"Can not access/create database {Database.GetDbConnection().Database}");
                }
            }

            UpdateDatabase();

            return true;
        }

        /// <summary>
        /// Check if database exist. Need to find more elegant method to check it.
        /// </summary>
        /// <returns></returns>
        private bool IsDatabaseExist() => Database.CanConnect();

        private void CreateDatabase()
        {
            using (var sqlConnection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    sqlConnection.Open();
                }
                catch (SqliteException)
                {
                    throw;
                }
                finally
                {
                    if (sqlConnection.State == ConnectionState.Open)
                    {
                        sqlConnection.Close();
                    }
                }
            }
        }
    }
}