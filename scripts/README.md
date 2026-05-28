# CredVault scripts

Each script does one thing. No Docker required — every command points at the database in
`backend/src/CredVault.Api/appsettings.Production.json`.

Run any of them from anywhere:

```bash
./scripts/<name>.sh
```

| Script                  | What it does                                                                  |
| ----------------------- | ----------------------------------------------------------------------------- |
| `run.sh`                | Start backend + frontend together. Ctrl+C stops both.                         |
| `run-backend.sh`        | Start just the API (`http://localhost:8080`).                                 |
| `run-frontend.sh`       | Start just the Next.js dev server (`http://localhost:3000`).                  |
| `stop.sh`               | Kill anything listening on ports 8080 / 3000.                                 |
| `migrate.sh`            | Apply EF Core migrations against the remote DB.                               |
| `update-db.sh`          | Apply migrations **and** run the idempotent initial-data seed.                |
| `test.sh`               | Backend `dotnet test` (Release) + frontend lint + build.                      |
| `master-key.sh`         | Print a fresh 32-byte master key (Base64) for `appsettings.Production.json`.  |

Environment overrides:

- `CREDVAULT_API_PORT` (default `8080`)
- `CREDVAULT_WEB_PORT` (default `3000`)
