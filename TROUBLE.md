# Troubleshooting Log for HandyBackend Local Setup

## 1. MySQL startup failures
- `mysql.server start` and direct `mysqld` invocations repeatedly quit because the PID file at `/opt/homebrew/var/mysql/Yukihiros-Macbook-AIr-M3.local.pid` could not be written.  Even after `sudo rm /opt/homebrew/var/mysql/*.pid` and `sudo chown -R xingyang:admin /opt/homebrew/var/mysql`, the service still exits with `ERROR! The server quit without updating PID file`.
- Direct `mysqld` attempts (`sudo /opt/homebrew/Cellar/mysql/9.4.0_3/bin/mysqld ...`) crashed with SIGSEGV; root launches were blocked by the built-in security check.
- The server log repeatedly shows `dyld: Library not loaded: /opt/homebrew/opt/abseil/lib/libabsl_bad_optional_access.2407.0.0.dylib`, indicating the MySQL 9.3/9.4 binaries were missing the required Abseil library.  Reinstalling `abseil` and `mysql@9.4` did not resolve it.  Switching back to MySQL 8.0 also still fails with the same PID error until ownership/permissions and log locations are corrected.

## 2. Current pain points to resolve
- Need to ensure `/opt/homebrew/var/mysql` is writable by the MySQL service (preferably user `mysql`), remove stale `*.pid`/`*.err`, and consistently start the server with `mysql.server start` (or `sudo -u mysql mysqld ...`) so that the PID file and logs are created properly.
- If Abseil-related dynamic library errors continue, installing a compatible MySQL formula (such as `mysql@8.0`) and making sure its dependencies are installed is recommended.
- Once MySQL is running, run `scripts/setup-test-db.sh` → `dotnet ef database update` → `dotnet run` to reach `/check` and verify API/DB traces.

Documenting this here should make the remaining debugging steps clearer. Let me know if you want a scripted checklist or specific commands for each stage.EOF