# Handy Backend

C# Handy Backend is a .NET ASP Core Backend for the Handy application.
It picks up API requests and stores deliveryRecords in the database.

## Useful commands

`scp -r xerographixoffice@192.168.10.247:Projects/Dotnet/HandyBackend/publish .`
Executed on the Windows machine (elevated Powershell) to pull things over to
the Windows machine.

`sc.exe create HandyBackend binPath= "C:¥tmp¥publish¥win-x64¥HandyBackend.exe"`
Creates a Windows service.
