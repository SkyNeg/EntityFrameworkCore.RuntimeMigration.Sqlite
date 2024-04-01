namespace SkyNeg.Sqlite.RuntimeMigration
{
    public class DatabaseUpdateResult
    {
        public bool IsSuccess { get; set; }
        public Version? NewVersion { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
