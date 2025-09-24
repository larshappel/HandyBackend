# Handy Backend

C# Handy Backend is a .NET ASP Core Backend for the Handy application.
It picks up API requests and stores deliveryRecords in the database.

## Things to do

1. In `Data/ApplicationDbContext.cs` make sure the Identification number is in
   the model correctly -> Migration for a test DB. This should not be necessary
   if the backend is connected to a DB with the correct schema already, as we
   won't be running the migrations then.

2. In `Controllers/ProductsController.cs` make sure the identification number
   gets placed in the DB correctly. This seems to be still missing. Again, make
   sure the schema is correct, either by connecting to an up-to-date DB or
   updating the migrations. ## Useful commands

`scp -r xerographixoffice@192.168.10.247:Projects/Dotnet/HandyBackend/publish .`
Executed on the Windows machine (elevated Powershell) to pull things over to
the Windows machine.

`sc.exe create HandyBackend binPath= "C:¥tmp¥publish¥win-x64¥HandyBackend.exe"`
Creates a Windows service.

`dotnet publish -r win-x64 -c Release -o ./publish/win-x64`
Publish it for windows to the publish directory.

## Windows Security Settings
