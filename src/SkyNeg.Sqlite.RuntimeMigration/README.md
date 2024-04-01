# SkyNeg.Sqlite.RuntimeMigration
Allow create and update Sqlite database context at runtime

## Usage
1. Create `Data/Scripts/Create` and `Data/Scripts/Update` folders in project root
1. Keep `Tables.sql` file with latest version of the script database tables
1. Add update scripts for your database to `Data/Scripts/Update` in format `{from_version}_{to_version}.sql`. Example `1.0_2.0.sql` 
