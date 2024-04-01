using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SkyNeg.Sqlite.RuntimeMigration
{
    public class ResourceScriptProvider : IScriptProvider
    {
        private const string SchemaScriptName = "Data.Scripts.Create.Schema.sql";
        private const string TablesScriptName = "Data.Scripts.Create.Tables.sql";
        private const string StoredProceduresScriptPrefix = "Data.Scripts.Create.StoredProcedures.";
        private const string UpdateScriptPrefix = "Data.Scripts.Update.";
        private const string ScriptExtension = ".sql";
        private const string ScriptBatchGroupName = "script";
        private static readonly Version DefaultVersion = new Version(1, 0);

        private readonly ILogger _logger;

        public ResourceScriptProvider(ILogger<ResourceScriptProvider> logger)
        {
            _logger = logger;
        }

        public List<UpdateScript> GetUpdateScripts(Type dbType)
        {
            Dictionary<UpdateBatchId, string> updateBatches = new Dictionary<UpdateBatchId, string>();
            List<UpdateScript> updateScripts = new List<UpdateScript>();
            Assembly? assembly = Assembly.GetAssembly(dbType);
            var assemblyName = assembly?.GetName()?.Name;

            if (assembly != null && !string.IsNullOrEmpty(assemblyName))
            {
                var resources = assembly.GetManifestResourceNames();

                // Update Scripts
                var pattern = Regex.Escape($"{assemblyName}.{UpdateScriptPrefix}") + $@"(?<{ScriptBatchGroupName}>.+{Regex.Escape(ScriptExtension)})";
                Regex r = new Regex(pattern);
                foreach (var resource in resources)
                {
                    var match = r.Match(resource);
                    if (match.Success)
                    {
                        if (match.Groups.ContainsKey(ScriptBatchGroupName))
                        {
                            var updateScriptResource = match.Groups[ScriptBatchGroupName].Value;
                            var batchId = GetUpdateBatchId(updateScriptResource);

                            if (batchId != null)
                            {
                                var sqlCommand = GetSqlCommandFromAssembpyResource(assembly, resource);
                                if (sqlCommand != null)
                                {
                                    updateBatches.Add(batchId, sqlCommand);
                                }
                            }
                        }
                    }
                }
                updateScripts = updateBatches.GroupBy(q => new { q.Key.FromVersion, q.Key.ToVersion }).Select(q => new UpdateScript()
                {
                    FromVersion = q.Key.FromVersion,
                    ToVersion = q.Key.ToVersion,
                    SqlCommands = q.OrderBy(c => c.Key.Priority).Select(c => c.Value).ToList()
                }).ToList();

                // Create Script based on the highest version from update scripts
                try
                {
                    Dictionary<int, string> createBatches = new Dictionary<int, string>();
                    int batchIndex = 0;
                    // Schema
                    var schemaCommand = GetSqlCommandFromAssembpyResource(assembly, $"{assemblyName}.{SchemaScriptName}");
                    if (schemaCommand != null)
                    {
                        createBatches.Add(batchIndex, schemaCommand);
                        batchIndex++;
                    }

                    // Tables
                    var tablesCommand = GetSqlCommandFromAssembpyResource(assembly, $"{assemblyName}.{TablesScriptName}");
                    if (tablesCommand != null)
                    {
                        createBatches.Add(batchIndex, tablesCommand);
                        batchIndex++;
                    }

                    // Stored procedures
                    var storedProcedureResourcePattern = Regex.Escape($"{assemblyName}.{StoredProceduresScriptPrefix}") + $@"(?<{ScriptBatchGroupName}>.+{Regex.Escape(ScriptExtension)})";
                    Regex createResourceRegex = new Regex(storedProcedureResourcePattern);
                    foreach (var resource in resources)
                    {
                        var match = createResourceRegex.Match(resource);
                        if (match.Success)
                        {
                            if (match.Groups.ContainsKey(ScriptBatchGroupName))
                            {
                                var updateScriptResource = match.Groups[ScriptBatchGroupName].Value;
                                var sqlCommand = string.Empty;
                                var storedProcedureCommand = GetSqlCommandFromAssembpyResource(assembly, resource);
                                if (storedProcedureCommand != null)
                                {
                                    createBatches.Add(batchIndex, storedProcedureCommand);
                                    batchIndex++;
                                }
                            }
                        }
                    }
                    if (createBatches.Any())
                    {
                        var createScript = new UpdateScript()
                        {
                            ToVersion = updateScripts.Max(q => q.ToVersion) ?? DefaultVersion,
                            SqlCommands = createBatches.OrderBy(q => q.Key).Select(q => q.Value).ToList(),
                        };
                        updateScripts.Add(createScript);
                    }
                    else
                    {
                        _logger.LogWarning($"Create script not found for {dbType.Name} in assembly {assemblyName}");
                    }
                }
                catch
                {
                    _logger.LogWarning($"Create script not found for {dbType.Name} in assembly {assemblyName}");
                }
            }
            return updateScripts;
        }

        public async Task<List<UpdateScript>> GetUpdateScriptsAsync(Type dbType, CancellationToken cancellationToken = default) => await Task.FromResult(GetUpdateScripts(dbType));

        private UpdateBatchId? GetUpdateBatchId(string resource)
        {
            if (!resource.EndsWith(ScriptExtension))
            {
                _logger.LogWarning($"Update script {resource} is malformed. Must end with {ScriptExtension}");
                return null;
            }

            // Remove extension
            resource = resource.Substring(0, resource.LastIndexOf(ScriptExtension));

            // Split versions
            var idParts = resource.Split('_');
            if (idParts.Length < 2)
            {
                _logger.LogWarning($"Update script {resource} is malformed. Name of the script must consist of two versions separated with dots");
                return null;
            }

            try
            {
                var batchId = new UpdateBatchId()
                {
                    FromVersion = new Version(idParts[0]),
                    ToVersion = new Version(idParts[1])
                };
                if (idParts.Length > 2)
                {
                    batchId.Priority = int.Parse(idParts[2]);
                }
                return batchId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Update script {resource} is malformed. Can not parse version parts. {ex.Message}");
                return null;
            }
        }

        private string? GetSqlCommandFromAssembpyResource(Assembly assembly, string resourceName)
        {
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
