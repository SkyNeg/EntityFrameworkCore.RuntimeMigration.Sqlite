namespace SkyNeg.EntityFrameworkCore.RuntimeMigration.Sqlite
{
    internal class UpdateBatchId
    {
        public Version FromVersion { get; set; } = new Version();
        public Version ToVersion { get; set; } = new Version();
        public int Priority { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is UpdateBatchId)) return false;
            UpdateBatchId? other = obj as UpdateBatchId;
            if (other == null) return false;
            return other.FromVersion == FromVersion && other.ToVersion == ToVersion && other.Priority == Priority;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FromVersion, ToVersion, Priority);
        }

        public override string ToString() => $"{FromVersion}_{ToVersion}_{Priority}";
    }
}
