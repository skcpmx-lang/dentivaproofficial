# Dentiva Pro

Dentiva Pro is being built as an offline-first, Windows desktop clinic-management application for Bangladesh. The approved baseline is a .NET 10 / WPF modular monolith with a locally encrypted SQLite database, Bengali Unicode support, and BDT money represented in poisha.

## Current phase: Phase 1 — Foundation

This repository currently contains the architecture and requirement-traceability baseline plus the Phase 1 solution foundation. It does **not** yet contain patient, appointment, clinical, prescription, invoice, inventory, backup/restore, or other clinic workflows. Do not use this development scaffold with real clinic data.

## Build and test

- Install the .NET 10 SDK (the repository pins the .NET 10 feature band in `global.json`).
- On Windows, run `dotnet tool restore`, `dotnet restore DentivaPro.sln`, `dotnet build DentivaPro.sln --configuration Release --no-restore`, and `dotnet test DentivaPro.sln --configuration Release --no-build`.
- The WPF desktop target is `win-x64`; shared domain, application, infrastructure, migration-tool, and test projects target `net10.0`.
- Add database migrations with `dotnet ef migrations add <Name> --project src/DentivaPro.Infrastructure/DentivaPro.Infrastructure.csproj --startup-project tools/DentivaPro.DbMigrations/DentivaPro.DbMigrations.csproj`. The design-time project uses a disposable encrypted in-memory database and must not be used to update clinic data.
- GitHub Actions validates the solution on a Windows runner. Native SQLite encryption, DPAPI, WPF rendering, and printer behavior still require supported-Windows verification.

## Data/security boundaries

The Phase 1 scaffold defines DPAPI CurrentUser key protection, an encrypted SQLite connection boundary, an initial EF Core migration, Argon2id password-hash primitives, and privacy-restricted local JSON logging. The app shell does not initialize the database and does not expose clinic workflows in this phase. A successful build is not product acceptance; consult `docs/implementation/phase-1-foundation.md` and the traceability register for scope and evidence.
