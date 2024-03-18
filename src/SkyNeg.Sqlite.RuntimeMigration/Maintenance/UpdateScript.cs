namespace SkyNeg.Sqlite.RuntimeMigration
{
    public class UpdateScript
    {
        public Version? FromVersion { get; set; }
        public Version? ToVersion { get; set; }
        public List<string> SqlCommands { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"{FromVersion}-{ToVersion}";
        }
    }
}
