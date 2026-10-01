# Dentiva Pro — Phase 0 Architecture and Engineering Plan

**Phase:** 0 — Product, architecture and engineering plan<br>
**Prepared:** 2026-10-01 (UTC)<br>
**Status:** Planning complete; implementation not started. This document is the architecture baseline for the next phase gate.<br>
**Product:** Dentiva Pro, offline-first Windows dental-clinic operations system for Bangladesh.

## 1. Executive decision

Build a **Windows-only, offline, single-workstation modular monolith** using **C# / .NET 10 LTS / WPF**, with a locally stored **encrypted SQLite** database, a protected file store, Windows-native printing, and a per-user **NSIS** installer. Keep business rules and authorization in a testable application/domain layer; treat the WPF UI as a client of those services, never as the security boundary.

This is a deliberate native-Windows choice, not a default framework choice. Dentiva Pro needs reliable Windows printer selection, paper profiles, PDF/print preview parity, Windows file dialogs, local credential protection, high-DPI behavior, and commercial packaging. WPF supplies the native document/printing and windowing path without introducing a browser runtime or an online service. .NET 10 is the current active LTS release, with support through 2028-11-14; .NET 8 and .NET 9 are close to end of support on the date of this plan, so they are not appropriate new-product targets. [1](https://dotnet.microsoft.com/en-us/platform/support/policy)

**No application code, assets, packages, or build artifacts have been created in Phase 0.** The only Phase 0 repository changes are planning and traceability documents.

## 2. Repository and environment findings

- Repository: `skcpmx-lang/dentivaproofficial`; it is currently **public**. It contains only a one-line `README.md` and the initial commit `7e560a3dbbe6856b0ffbba4655dbf2108dbac453`.
- Working branch: `arena/01a0f648-dentivaproofficial`, based on `main`; the session branch is local and is not yet present on `origin`. The working tree was clean at inspection. No application, tests, ignore rules, CI workflows, releases, issues, or PRs exist.
- GitHub authentication is available for read operations. `gh repo view`, release/issue/PR listings worked. GitHub Actions permission and branch-protection endpoints returned HTTP 403 (“Resource not accessible by integration”), so their settings could not be verified. No workflow runs or releases were found.
- Build sandbox: Debian 12 x86_64, Node 22 and Python 3.11 present; **.NET SDK, Rust, Windows, Wine, SQLite CLI, NSIS, and Windows printer drivers are absent**. This container cannot launch WPF or validate Windows printing, a Windows installer, Windows DPI behavior, or a clean Windows installation.
- GitHub-hosted `windows-2025` runners are available as a Windows build/test route; hosted runner images currently identify `windows-2025` as Windows Server 2025, not a Windows 11 desktop. They are useful for deterministic builds and Windows integration tests, but do not substitute for validation on the supported Windows client OS or a real printer. [1](https://github.com/actions/runner-images)

**Commercial confidentiality warning:** the repository is public. If Dentiva Pro source is intended to remain proprietary, the repository must be made private by an authorized owner before source is added. No setting was changed in Phase 0. A public source repository also means a fixed offline activation mechanism can only deter casual bypass; it cannot provide secrecy or prevent reverse engineering.

## 3. Technology decision record

### 3.1 Options evaluated

| Option | Strengths for this product | Material drawbacks / why not selected |
|---|---|---|
| **.NET 10 + WPF (selected)** | Native Windows app; mature printer, dialog, font, accessibility, and file-system integration; reliable SQLite/EF ecosystem; good DPI-aware layout; compact relative to a bundled browser; strong testable C# domain layer; long-supported LTS. WPF is Windows-only and offers full control of XAML, typography, layout, animation, and document rendering. [5](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/) | Cannot run in this Debian sandbox; requires Windows CI plus actual Windows client/printer acceptance. Premium appearance requires an intentional design system rather than default control styling. |
| Electron + React/TypeScript | Modern component ecosystem; current Node tooling exists in the sandbox; offline operation is straightforward; Chromium can render Unicode and HTML documents consistently. | Larger runtime and attack surface; SQLite native-module/runtime packaging complexity; printer/profile integration passes through Chromium and Electron-specific APIs; bundled Chromium is unnecessary for a Windows-only clinical workstation. |
| Tauri + web UI | Smaller package and strong Rust boundary are attractive; local/offline operation is possible. | Rust is absent here; WebView2/runtime provisioning and Windows printing/document preview add deployment variables; UI and document behavior must be validated across the installed WebView. No decisive advantage over native WPF for this Windows-only requirement set. |
| WinUI 3 / .NET MAUI | Modern Microsoft UI direction; WinUI can match Windows 11 Fluent surfaces. | More packaging/runtime and Windows App SDK considerations; MAUI adds cross-platform layers not required here. Neither gives a better-established document/print preview path for this product than WPF. |
| WinForms | Mature and native; straightforward Windows utilities. | Less suitable for the responsive, bespoke clinical shell and rich document compositions required. |

### 3.2 Selected stack (architecture baseline)

- **Runtime and language:** C# on .NET 10 LTS; app target `net10.0-windows`; shared domain/application projects target `net10.0` where possible. Use the latest supported .NET 10 servicing patch at build time, with package versions centrally pinned and locked.
- **Desktop UI:** WPF/XAML with MVVM; a custom Dentiva design system implemented through shared resource dictionaries, control templates, reusable view components, vector icons, and accessibility metadata. Use `CommunityToolkit.Mvvm` only for MVVM primitives where useful; use the .NET hosting/DI/logging abstractions for app composition. Do not add a paid UI kit.
- **Application shape:** a **modular monolith**, one local WPF process and one local clinic database. No web server, open local port, telemetry, cloud account, cloud database, external API, or mandatory update service.
- **Data access:** EF Core 10 migrations and relational mapping using `Microsoft.EntityFrameworkCore.Sqlite.Core` / `Microsoft.Data.Sqlite.Core`, paired with **`SQLite3MC.PCLRaw.bundle`** as the encrypted native SQLite provider. The upstream-maintained NuGet package is MIT-licensed, supports the .NET 10 password connection-string path, and is preferable to the deprecated `SQLitePCLRaw.bundle_e_sqlite3mc` and to a paid SQLCipher .NET package. Pin and re-audit the chosen version (2.4.0 is the Phase 0 reference version) before Phase 1 implementation. [2](https://www.nuget.org/packages/SQLite3MC.PCLRaw.bundle/2.4.0)
- **Database:** one encrypted SQLite database per Windows user profile, stored under that profile’s local application-data directory. Configure foreign keys, write-ahead logging, busy timeout, explicit indexes, and transactional writes. EF migrations are versioned and tested against both a new database and realistic upgrades. SQLite has a single-writer model; WAL improves read/write coexistence but does not turn a file database into a multi-workstation server. [2](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)
- **Cryptography:** `SQLite3MC` for database-at-rest encryption; Windows DPAPI CurrentUser to protect a randomly generated application key; HKDF-separated keys for data purposes; authenticated encryption for external attachment files; `NSec.Cryptography` with libsodium Argon2id for password verification and backup recovery-key derivation. NSec 26.4.0 and libsodium 1.0.22 are Phase 0 candidates, subject to Phase 1 compatibility, package, and security review. [1](https://www.nuget.org/packages/NSec.Cryptography/) [1](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html) [4](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata)
- **Printing:** WPF `FixedDocument` / a shared `DocumentPaginator`-based renderer; `DocumentViewer` for preview and the Windows `PrintDialog`/print queue for output. Both preview and print consume the same paginator. `PrintDialog.PrintDocument` routes to installed Windows printers; Microsoft Print to PDF (when installed/enabled) is the supported Save-as-PDF path. [1](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/documents/how-to-display-print-dialog) [3](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/documentviewer)
- **Fonts:** bundle and license an OFL Bengali-capable font family (Noto Sans + Noto Sans Bengali, subject to exact font-file audit) and use WPF’s text layout for entered Bengali content. Keep document text as Unicode; do not rasterize Bengali text into screenshots.
- **Installer:** self-contained **per-user x64 NSIS EXE** (no .NET runtime prerequisite/download), installed under the user’s profile; application data remains outside the install directory. Use NSIS’s ZLIB compression mode to avoid bringing in the optional LZMA compression module’s separate license terms. NSIS documents self-contained EXE installers and a permissive zlib/libpng license for most of its code; all shipped notices and any selected compressor/plugin terms still require audit. [4](https://nsis.sourceforge.io/Docs/Chapter1.html)
- **Build platform:** GitHub Actions on an explicitly selected `windows-2025` runner for Windows builds; do not rely on the moving `windows-latest` alias. Add Linux-only source checks only when useful; Windows build/test is authoritative.

### 3.3 Why these choices are commercially compatible in principle

The selected design avoids a paid API, runtime service, online license check, cloud account, cloud database, SQLCipher commercial package, proprietary UI control suite, and installer fee. Installer alternatives were checked: FireGiant states that revenue-generating use of WiX releases participates in its Open Source Maintenance Fee, and the current Inno Setup site asks commercial users to purchase a license; NSIS is selected to avoid making either commercial installer fee a product/build prerequisite. [2](https://docs.firegiant.com/wix/) [1](https://jrsoftware.org/isinfo.php)

Candidate project/library licenses are permissive (MIT, Apache-2.0, ISC, zlib/libpng, public-domain SQLite, and OFL fonts), but this is **not** the final dependency audit. Every resolved transitive NuGet package, native binary, font, installer compressor/plugin, and .NET redistribution notice must be inventoried and reviewed before release. No dependency is approved solely because its top-level package advertises a permissive license.

## 4. Application architecture and boundaries

Use a ports-and-adapters modular monolith with strict one-way dependencies:

```text
DentivaPro.Desktop (WPF views, view models, dialogs, printer adapter)
              ↓ commands / queries / DTOs
DentivaPro.Application (use cases, validation, permissions, transactions)
              ↓ domain contracts
DentivaPro.Domain (entities, invariants, value objects, state transitions)
              ↑ implemented ports
DentivaPro.Infrastructure (EF Core/SQLite3MC, file store, DPAPI, scheduler, logging)
```

The concrete project names may be refined when the solution is created, but the dependency direction is fixed. Views/view models do not access `DbContext`, the file store, or print queues directly. Each business workflow calls an application service/use case. The service checks authentication and all required permissions before any read or mutation, validates domain invariants, executes related writes in a transaction, and emits an audit event. UI visibility and disabled state are usability only.

Keep patient/clinical, scheduling, billing, stock, accounting, security, settings, documents, backup, search, and notifications as cohesive modules in this one process. Do not introduce microservices, a web backend, or a general-purpose local HTTP API.

### Local-user/concurrency boundary

The baseline supports multiple Dentiva Pro login identities and roles **sequentially on one Windows user profile / workstation**, with one application instance and serialized write operations. It does not claim shared live data across separate PCs, Windows profiles, or network shares. Data is kept on a local disk; SQLite WAL is not to be placed on a network share. Multiple app users, role changes, audit records, and financial/clinical workflows remain supported on the local installation. If multi-workstation LAN sharing is a product requirement, it must be agreed before implementation because it changes the data topology, deployment, and conflict model.

## 5. Domain and relational database plan

### 5.1 Relational entities

The initial normalized schema will include at least:

- **Clinic and master data:** ClinicProfile, Dentist, DentistDesignation, DentistQualification, Staff, Medicine, Treatment, ClinicalPhrase, PrescriptionTemplate, InventoryCategory, Supplier, ExpenseCategory, PaymentMethod, PrinterProfile, ApplicationSetting.
- **Identity and security:** ApplicationUser, Role, Permission, UserRole, RolePermission, Session/security events, AuditLog, Notification.
- **Patient and clinical:** Patient, PatientContact/EmergencyContact (as appropriate), Visit, ClinicalFinding, Diagnosis/Assessment, VisitToothEntry/ToothSurfaceState, VisitTreatment, Prescription, PrescriptionItem, Appointment, QueueEntry, Referral, AttachmentMetadata.
- **Financial:** Invoice, InvoiceItem, Payment, PaymentAllocation, FinancialLedgerEntry, Income/Expense records (or equivalent ledger subtypes), and any necessary category/reference tables.
- **Inventory:** InventoryItem, Purchase, PurchaseItem, StockMovement, lot/batch and expiry data where relevant.
- **Resilience:** Migration metadata, backup metadata/job results, and data/key-format versions.

### 5.2 Relationships and invariants

- A patient has many visits, appointments, prescriptions, referrals, attachments, invoices, and payment records. A visit belongs to exactly one patient and preserves its provider and encounter context.
- A visit can have many findings, diagnoses/assessments, selected teeth, performed treatments, prescriptions, attachments, and follow-up events. A historical visit’s dental chart is an encounter snapshot/event record; a newer visit never overwrites it.
- A prescription belongs to a patient and prescribing dentist, and may reference a visit. Prescription items snapshot the entered medicine/form/strength/dosage/frequency/food timing/duration/quantity/instructions so later medicine-catalog edits do not rewrite history.
- Appointments and queue entries reference a patient and provider as applicable; state changes are validated transitions and are audited.
- Invoice lines snapshot description, quantity, unit price, discount/adjustment, and line total. Invoice totals are stored as integer BDT minor units and verified within a transaction. Payments are immutable transactions allocated to one or more invoices; reversals are separate linked entries, not in-place erasure.
- Inventory changes are append-only StockMovement records tied to purchases, use/consumption, adjustment, damage, expiry, or reversal. Current stock is derived/cached from movements with reconciliation checks; purchase price/lot/expiry are retained.
- User↔Role and Role↔Permission are explicit many-to-many relationships. Dentist, staff member, and login identity remain distinct records with optional relationships.
- Use restrictive foreign keys for clinical and posted financial history; archive/cancel/void/reverse/inactivate rather than hard-delete. Cascades are limited to unposted drafts and owned child rows where deletion is safe and explicit.

### 5.3 Data conventions

- Store all BDT money as signed integer **poisha** (100 units per BDT); never use binary floating point for money. Convert/format at the view/document boundary with `৳` and locale-safe grouping.
- Use stable internal GUIDs and human-visible unique clinic codes; patient-code allocation is serialized and protected by a database unique constraint, not by UI checks alone.
- Patient age data must not fabricate a precise birth date. Store a date of birth only when known; otherwise capture the age/age precision and the date it was supplied, then preserve the clinically relevant age-as-of-visit snapshot for historical documents.
- Store event timestamps in UTC with an explicit configured clinic timezone; default clinic timezone is Asia/Dhaka. Present clinic-local date/time consistently and preserve the original timestamp/offset in history.
- Store Bengali/English text as Unicode, normalize input consistently (NFC), retain the entered spelling, and test search paths with Bengali strings. Never transliterate or ASCII-fold Bengali as a replacement for the original text.
- Prefer server-side query filtering, indexes, pagination/virtualization, and incremental profile history loading. No application-level cap on patients, visits, invoices, or audit history; only storage/OS limits and separately documented single-file upload safety limits apply.

## 6. Authentication, authorization, privacy and activation

### 6.1 Identity/RBAC

- First-run creates one administrator chosen by the clinic owner; no default username/password. Passwords use versioned Argon2id hashes with random unique salts, calibrated on supported hardware to an OWASP-aligned memory/time setting; permit long passphrases and Unicode. Do not log passwords or include them in audit payloads.
- Add failed-login throttling/backoff, lockout recovery by an authorized administrator, login/logout/failure auditing, and credential-change auditing. No cloud password reset.
- Seed protected, non-removable built-in system permissions and an initial administrator role; allow owner/admins to create roles and assign granular permissions. Deny by default.
- Required authorization is enforced in application services for both queries and commands: patient, clinical, prescription, appointment, queue, treatment, invoices, payments, accounting, reports, inventory, staff, users, backup, restore, settings, audit log, exports, and destructive actions. A receptionist’s payment/invoice permissions do not imply financial-report access.
- The session is process-bound and expires/locks after configured inactivity of 5/10/15/30 minutes. Lock enforcement invalidates the service-layer session/capability and blocks data operations; a screen cover alone is insufficient. Re-authenticate to resume. Handle OS lock/sleep and logout as lock/revoke events.

### 6.2 Data at rest, attachments and local threat model

- Generate a random 256-bit installation data key. Protect the routine key envelope using DPAPI `CurrentUser`; derive purpose-separated keys and never store a plaintext key/password in the database or settings. This scopes the baseline to one Windows user profile. DPAPI is not protection from malware already running as that user, a local administrator, a debugger attached to the app, or a determined reverse engineer.
- Encrypt the SQLite database using the selected SQLite3MC native provider and the protected key; keep the database, journal/WAL and attachments under the local profile with restrictive ACLs. Set temp-storage behavior deliberately and avoid PHI in diagnostics, crash logs, filenames, or temp paths.
- Keep attachment bytes outside the main database under generated opaque names. Validate extension, actual file signature/MIME, size, path, and duplicate identity; store metadata and checksums relationally; encrypt contents with authenticated per-file/per-chunk encryption. Preview in-process when possible. External opening/export must be authorized, explicit, audited, and use a protected temporary copy with cleanup/recovery handling.
- At setup, the owner chooses a backup/recovery passphrase. Derive a wrapping key with Argon2id and store only that derived key in a DPAPI-CurrentUser-protected keyring (never the passphrase). The manual/scheduled backup job can then wrap the random data key into each portable backup using authenticated encryption without retaining a plaintext password. A replacement machine uses the passphrase and manifest KDF parameters to unwrap the key, then re-protects it under the new Windows profile. Support an authorized passphrase change by creating a new envelope while preserving prior backups’ independent envelopes. Loss of both the Windows profile key and the applicable backup recovery passphrase means encrypted data cannot be recovered; state this clearly in setup and backup UX.
- Logging is structured and local, with patient-identifying and clinical values excluded/redacted by default. App errors are human-readable; detailed logs are permission-protected and privacy-aware.

### 6.3 Activation

Activation is fully offline and is a one-time first-run gate. Store only a salted, slow one-way verifier for the supplied fixed activation value; do not include the literal code in application/test source, UI resources, logs, or configuration. Verify in constant time, throttle repeated failures, and persist an activation marker protected with DPAPI. Test both a clean unactivated installation and successful/failed activation.

Because every install uses the same fixed offline code and the verifier ships in a local executable, activation is **not** online licensing, copy protection, or reverse-engineering-proof. Obfuscation may raise casual effort but is not a security claim. This matches the prompt’s explicit limitation; do not present the activation mechanism as unbreakable.

### 6.4 Audit

Append audit events transactionally for security, identity/RBAC, patient/clinical history, financial posting/void/reversal, stock movement, configuration, backup/restore, exports, activation, and destructive operations. Record actor, UTC/local context, action, entity key, correlation ID, and safe before/after field summaries. Never record passwords, data keys, activation values, or unnecessary full clinical narratives. Protect audit reads by permission. Use append-only application flows and tamper-evident sequencing/checks; document that a local administrator who controls the machine/process is outside absolute non-repudiation guarantees.

## 7. Backup, restore and file lifecycle

- Manual backup uses a user-selected destination. A package is a timestamped, versioned, encrypted self-contained snapshot containing a consistent SQLite online backup (not an ad hoc copy of a live `.db`), encrypted attachment objects, required clinic assets, a manifest/schema/app version, and checksums. Do not include plaintext patient names in filenames/manifests.
- After package creation, reopen/inspect it, validate manifest/checksums, open the database with the key, run SQLite integrity checks, and verify referenced attachment objects. Show success only after read-back validation.
- Automatic 7/15/30-day schedules use a per-user Windows Task Scheduler job while that profile is logged in, plus a startup “backup overdue” recovery check. The job uses the same protected local key, writes to the explicitly configured folder, records result/error, and reports missed/failed jobs in the application. Never silently claim a backup ran while Windows was off or the destination was unavailable.
- Restore is admin-only, requires explicit scope confirmation, first creates and verifies a pre-restore safety backup, stages and validates the selected package in a temporary location, checks database/schema/attachments before swapping data, and rolls back atomically on error. Audit the entire operation and verify after restart.
- A full snapshot is one coherent clinic state. Combining rows from several full backups has no safe deterministic merge semantics. The production restore flow will restore one selected snapshot; it may batch-validate multiple selected files for recovery triage, but will not merge separate snapshots or silently choose one.
- Uninstall removes program files and shortcuts but **retains user data, keys, attachments, and backup folders by default**. Provide a separate explicitly authorized, audited, backup-gated data deletion workflow. Installer upgrades must not touch user data.

## 8. Clinical/document and printing architecture

### 8.1 Clinical model and documents

Use an encounter-centered model. Patient profile is a read-oriented overview of demographics, current contact data, history, appointments, clinical timeline, dental chart, referrals, attachments, financial rollups, and links to underlying records. Each encounter retains its author/provider and clinical content as entered. Use adult and pediatric FDI/ISO tooth identifiers in persisted visit entries; chart interactions create actual encounter data rather than purely visual selection.

A prescription stores patient, visit (optional when appropriate), prescribing dentist, timestamp, clinical phrases, advice, and multiple item rows. Capture customizable medicine form/strength, dosage, frequency/schedule, food timing, duration, quantity, and free instructions. Prescription and invoice templates resolve clinic/dentist details when rendered while their clinically/financially historical content remains snapshotted.

### 8.2 Shared rendering and profiles

One document pipeline will render prescriptions and invoices into paginated WPF document objects. It will support A4, A5, printer-supported thermal/small formats (including a representative 80 mm profile), margins, orientation, custom supported dimensions, logos, headers/footers, long text, page breaks, tables, Bengali shaping, and saved printer profiles. Prescription output includes the configured clinic identity, the **actual prescribing dentist’s** multiple designations/qualifications, patient identity, clinical zone, medicine zone, and a physically measured signature area; narrow stock reflows to stacked zones. Invoice output uses clinic identity and item/totals/payment state and has no doctor prescription header/signature by default.

Print preview uses the same paginator/layout used for Windows printing; the user can select an installed printer and paper profile before printing. Save as PDF uses the Windows PDF printer workflow (or another installed PDF printer); no paid PDF SDK or remote PDF API. Validate produced PDF page dimensions, extracted text, Bengali glyph shaping, margins, table wrapping, and signature area; physical output is validated on representative supported hardware. WPF provides `DocumentViewer` and the standard Windows `PrintDialog`; printer models are not universally guaranteed. [1](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/documents/how-to-display-print-dialog) [3](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/documentviewer)

## 9. Product UX, language, supported platform and display

### 9.1 Shell and design system

Implement the requested Dentiva Pro header, clinic identity, current date, notification center, user menu, collapsible sidebar, and grouped navigation. Preserve all named modules: Dashboard; Patients; Appointments; Queue; Treatments; Prescriptions; Invoice; Payments; Inventory; Accounting; Staff & Users; Backup & Restore; Settings; About.

Create shared design tokens for color, typography, spacing, surfaces, borders, elevation, focus, status, icons, density, and reduced motion. Build reusable button/input/form/table/card/dialog/drawer/tab/toast/tooltip/empty/loading/error/validation components. Use intentional adaptive grid rules rather than arbitrary card placement. Give all icon-only controls accessible names/tooltips, visible keyboard focus, predictable tab order, logical shortcuts, and non-color status cues. Every screen must implement loading, empty, error/retry, success, and overflow behavior; no decorative or inert controls.

The application shell language is professional English. Patient/clinic entered text, clinical notes, search, prescriptions, invoices, exports, preview, and print support Bengali Unicode. The operational dashboard is permission-aware and can include today’s patients/appointments/queue/completed visits, authorized collection and outstanding dues, low/expiring stock, recent patients, upcoming appointments, and treatment activity. This Phase 0 baseline does **not** promise a Bengali-translated application chrome.

### 9.2 Windows and display support boundary

- Primary target: **Windows 11 25H2 x64 or later Windows client releases while supported and validated**. Also allow Windows 10 Enterprise/LTSC only where the current .NET 10 support matrix lists that exact edition/build and the release test matrix verifies it. Do not claim support for consumer Windows 10 Home/Pro, which reached end of support on 2025-10-14. The .NET 10 Windows matrix distinguishes supported Windows client editions and includes Windows 11 25H2 and Windows 10 Enterprise/LTSC targets. [5](https://learn.microsoft.com/en-us/dotnet/core/install/windows) [2](https://learn.microsoft.com/en-us/windows/release-health/supported-versions-windows-client)
- x64 is the initial commercial architecture; ARM64 is not advertised until its full native dependency/installer/print matrix is tested.
- Define a supported *effective workspace* of at least 1024×640 device-independent pixels. The test matrix pairs physical pixels and scale: 1280×800 @100%, 1600×1000 @125%, 1920×1200 @150%, and 2560×1600 @200%; also test 1920×1080 @100% and a larger 2560×1440 desktop layout. A smaller effective workspace receives an explicit compact/scroll layout where practical, but is not advertised as the minimum supported configuration.
- Use responsive WPF layout in device-independent units, Per-Monitor-V2 awareness where validated, vector assets, and scrollable page content. Test monitor changes and 100%, 125%, 150%, 200% scaling on real/virtual Windows environments. The Linux sandbox cannot establish these results. [2](https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context)

## 10. Validation, performance, CI and release engineering

### 10.1 Test layers

- **Domain/unit:** money arithmetic, state transitions, FDI chart values, validation, role policies, date/time, template composition, and redaction.
- **Database/integration:** migrations; encrypted database reopen; UTF-8/Bengali round trips; FK/check/unique enforcement; money precision; transactions/rollback; WAL/busy handling; audit; snapshot backup/restore; migration upgrade; damaged/missing attachment behavior.
- **Application/security:** service-level permission matrix and direct-call/indirect-route attempts; authentication/hash verification; lock timeout; archive/void/reversal invariants; restore authorization; attachment authorization; export audit; activation success/failure.
- **UI:** Windows UI Automation for navigation, accessible names, keyboard flow, dialogs, validation, filters, scrolling, focus, and major workflows. Every material button/tab/modal/route and save/error path gets a named test or manual case.
- **Documents:** output through the real Windows print pipeline to Microsoft Print to PDF and representative device drivers; inspect PDF and rendered page images at A4/A5/80 mm/custom; Bengali; long and multi-page data; signature area; logos; invoices and partial-payment statuses.
- **Quality/performance:** stress fixtures with large synthetic datasets; paginated queries and profile timelines; memory/response measurements; 100/125/150/200% DPI/resolution matrix; restart and crash/recovery; installer/upgrade/uninstall; clean-machine release-asset testing.

Use xUnit for .NET tests. Use Windows UI Automation/FlaUI only in test scope if dependency review confirms suitability. Test data is synthetic and contains no real patient data. Failures are investigated to root cause; regressions are added before rerunning.

### 10.2 GitHub Actions

- Add CI on pushes to the session branch, pull requests, and manual dispatch. Build/test on pinned `windows-2025`, use `global.json`, lock NuGet restore, enable nullable/analyzers, warnings-as-errors where feasible, deterministic Release builds, and retain test/TRX, license inventory, SBOM, and build provenance as artifacts.
- Include dependency vulnerability checks, CodeQL/SAST where the GitHub integration permits, secret scanning expectations, and a complete transitive dependency/license/notice report. Keep workflow token permissions minimal; pin third-party Actions by commit SHA once selected.
- The release workflow builds the self-contained x64 app and NSIS installer, runs artifact-level smoke tests, generates SHA-256 and SBOM/provenance, and publishes a GitHub Release only from an authorized version tag/release workflow. It must not merge PRs. The release workflow is a gate, not an automatic license or internet dependency in the product.
- This Arena session is fixed to `arena/01a0f648-dentivaproofficial`; all session work remains on that branch. Create a PR from this branch for a meaningful review point, never merge it, and wait for explicit user authorization before any merge/release action.

### 10.3 Installer, signing and release limitations

The NSIS per-user installer writes only application binaries/shortcuts to the install location; initialization and activation happen on ce without data loss, clean removal of binaries, and clear retention of user data. Release outputs include a clearly named `DentivaPro-Setup-<version>-win-x64.exe`, the app executable/publish payload where appropriate, SHA-256 checksum, SBOM, notices, and build/test metadata. The final artifact lives in GitHub Releases; `dist/` is reserved as a fallback only if release publication is technically impossible.

No Windows code-signing certificate or publisher certificate was found in the repository/environment. A commercial Windows release without Authenticode signing can trigger SmartScreen/unknown-publisher warnings. Do not create/store signing credentials in the repository; before a customer-facing signed release, the owner must provision a certificate and CI secret through approved secure GitHub settings. If no certificate is provided, report the artifact as unsigned and do not claim SmartScreen reputation.

## 11. Repository and dependency governance

- Add a `.NET`-appropriate `global.json`, central package versions, NuGet lock files, deterministic build settings, `.gitignore`, test solution, and CI before substantive modules.
- Avoid network behavior at runtime. Audit startup, logging, native package initialization, fonts, and installer scripts for unintended outbound requests.
- Before release, generate an SBOM; record direct and transitive package name/version/purpose/source/license/native assets/network behavior/security review; validate attribution and distribution terms; ship `THIRD-PARTY-NOTICES.md` and any required license texts in an About/Licenses screen and installer/docs.
- Include font licensing and SQLite/SQLite3MC/SQLitePCLRaw/libsodium/NSec/.NET/NSIS/compressor notices as applicable. Re-check license/security state at the release tag; Phase 0 decisions are candidate approvals, not blanket legal advice.

## 12. Domain-specific policy boundaries to verify before release

- **Bangladesh legal/accounting:** this plan supports clinic operations, BDT invoices, collections, payments, expenses, stock, and reports. The prompt gives no applicable VAT/tax invoice wording, tax rules, accounting-basis, retention period, or health-data regulatory interpretation. Do not claim statutory VAT/tax/accounting or legal compliance. Ask a Bangladesh clinic/accounting/legal reviewer to validate required invoice fields and policy before release; rates/clinic-specific details must be configurable, never fabricated or hard-coded.
- **Clinical templates:** the supplied clinical phrases are configurable prompts, not clinical decision support. A qualified dentist must validate clinical wording and document layout. The app must not generate diagnoses or treatment advice autonomously.
- **Network scope:** single workstation/per-Windows-profile offline data is the chosen baseline. No LAN synchronization or concurrent multi-PC editing is included. If that expectation is wrong, owner approval is required before Phase 1 because the database topology changes.
- **Repository confidentiality:** repository is presently public. The product owner must decide whether its source is intentionally public or whether it must become private before implementation for proprietary commercial distribution. The activation verifier is necessarily inspectable/reverse-engineerable in a public or distributed offline client.

## 13. Phase plan and exit gates

Every phase ends with a phase-specific test run, review against `docs/requirements/traceability.md`, root-cause fixes for found issues, a detailed report, and a hard stop. `Continue` authorizes only the next incomplete phase; it never authorizes PR merge.

| Phase | Objective and scope | Exit evidence before stopping |
|---|---|---|
| **0 — Plan (complete)** | Repository/environment inspection; architecture, stack, threat model, domain/database, print, backup, installer, test/release plan, traceability. | This plan and 112-section traceability map; no product code. |
| **1 — Foundation** | Solution/projects, configuration, database/migrations/key boundaries, logging/error handling, crypto primitives, test setup, CI, initial design-system/shell scaffold. | Clean restore/build; unit/integration/security primitive tests; CI green; no product workflow claims. |
| **2 — Design system & shell** | Design tokens/components, header/sidebar/collapse/nav, layout, focus/shortcuts, loading/empty/error states. | UI automation at the supported workspace/DPI matrix; all shell navigation/controls work; no dead controls. |
| **3 — Auth, staff, users, dentists, RBAC** | Authentication, roles/permissions, protected sessions/auto-lock, audit foundation; personnel/professional identity distinction. | Hash storage, login failure/backoff/logout/5–30-minute lock, direct service permission-denial matrix and audit tests. |
| **4 — Setup & master data** | Activation/first-run, clinic profile/logo, multiple dentists/structured qualifications/designations, medicine/treatment catalogs, suppliers/categories/phrase libraries/settings foundation. | Fresh install setup/activation scenarios, validation, atomic setup, persisted restart, permission tests. |
| **5 — Patient & clinical core** | Patients, list/filter/search, rich profiles, visits, timeline, clinical findings, FDI adult/pediatric chart, treatments/referrals/attachments. | Encounter history preserved; chart persistence; Unicode; file validation/integrity; paginated large-data profile tests. |
| **6 — Appointments & queue** | Appointment views/statuses, patient/provider flows, arrivals/no-show, persistent queue/provider views. | End-to-end transition/ordering/cancel/no-show tests and restart persistence. |
| **7 — Prescription** | Prescription workflow, medicine items/templates/phrases, clinician linkage, rendering/preview/printing/PDF profiles. | Actual A4/A5/small-format output, Bengali and long-document PDF/page-image review; signature measurement; multiple qualifications. |
| **8 — Invoice & payments** | Invoice item snapshots, BDT totals, payment methods/allocations, partial payments/due/void/reversal, print output. | Decimal/poisha property tests, transactional failure/rollback, audit/history, financial RBAC, printed/PDF invoice review. |
| **9 — Inventory & accounting** | Purchases, suppliers, stock movements/expiry/low-stock, income/expenses and period reports. | Movement reconciliation, expiry/reorder triggers, duplicate accounting prevention, financial permission tests. |
| **10 — Dashboard, search & notifications** | Permission-aware dashboard, global search, filters, notification center and operational summaries. | Large-data search performance, result permission tests, deduplicated notification scenarios and responsive layout. |
| **11 — Backup & restore** | Manual/automatic backup, folder choice, timestamp, portable recovery envelope, integrity verification, pre-restore safety backup and atomic restore. | Real file creation/read-back, offline schedule/failure behavior, damaged backup rejection, restore/restart/recovery and authorization tests. |
| **12 — Settings, About & commercial polish** | Complete settings/export/import where supported, destructive safeguards, About identity, final accessibility/shortcut/design polish. | Settings persistence, export/import Unicode/permissions/audit, destructive confirmation/backup/role tests, UI regression suite. |
| **13 — Security/data-integrity audit** | Dedicated adversarial RBAC/auth/key/activation/attachment/restore/audit/migration/transaction/input/dependency review. | Critical/high findings fixed with regression tests; no unresolved critical/high security or integrity issue. |
| **14 — Full QA** | Full unit/integration/workflow/UI/print/PDF/Unicode/DPI/performance/stress/backup/installer/regression testing. | All mandatory acceptance cases have evidence; repeat impacted suites after fixes; clean report. |
| **15 — Release audit** | Search code/config/dependencies for unfinished/fake/debug/secret/network/license/dead-code issues; full requirement recheck. | Release-blocking scan results resolved; 112-section matrix is evidence-linked and no required item is marked unknown. |
| **16 — Production build** | Versioned Release publish, installer, notices, SBOM, checksums, signing where owner-provisioned. | Build is reproducible; installer starts; binary metadata and artifact inventory captured. |
| **17 — Artifact validation** | Install exact signed/unsigned release artifact on clean supported Windows, run end-to-end flows, restart/restore/printing/uninstall. | Exact artifact passes clean-machine tests; supported Windows client and representative print/PDF evidence. If environment unavailable, stop and report, do not call production-ready. |
| **18 — GitHub release** | Prepare/verify release workflow and PR; after explicit user-authorized merge, publish the tagged artifact to GitHub Releases (or documented `dist/` fallback). | PR not merged by agent; release only after separate explicit merge authorization; checksum/artifact and final traceability report verified. |

## 14. Phase 0 risk register

| Severity | Risk / evidence | Mitigation or gate |
|---|---|---|
| **High — source confidentiality** | GitHub repository is public; project is intended for commercial sale. | Owner decides public/open-source versus proprietary/private before substantive code. Do not silently change repo visibility. |
| **High — Windows-only verification** | Current sandbox is Debian; no .NET SDK, WPF runtime, printer, or installer tool; Actions permissions cannot be inspected. | Windows CI from Phase 1; require clean Windows 11 client validation and actual PDF/printer artifacts before release. Stop if unavailable. |
| **High — signing** | No Authenticode certificate/publisher identity is present. | Request owner-provisioned certificate via secure GitHub secrets before a signed commercial release. Report unsigned builds honestly. |
| **High — local key loss / recovery** | Database and attachments are encrypted; DPAPI is profile-bound. | Require a tested portable recovery passphrase envelope, verified backup/restore, and clear owner recovery instructions. |
| **Medium — shared PCs / network** | Single-profile local SQLite cannot safely promise multi-PC shared operations. | Treat as explicit baseline; owner must request a local-server/LAN architecture before Phase 1 if needed. |
| **Medium — clinical/legal configuration** | Prompt lacks Bangladesh statutory invoice/tax/retention requirements and clinician-approved prescription language. | Local accountant/dentist/legal review; configurable values; make no unsupported compliance claims. |
| **Medium — print device variance** | Custom thermal media and margins depend on Windows driver capabilities. | Validate at least representative A4/A5 and one supported 80 mm printer/driver; document that not every model is guaranteed. |
| **Medium — encryption dependency** | SQLite3MC/NSec native package versions must match .NET 10/WPF single-file x64 deployment. | Phase 1 Windows package-load tests; lock versions; audit native assets, licenses, and vulnerabilities. Any incompatibility must be root-caused before architecture changes. |
| **Medium — scale of scope** | Specification contains 112 sections and many independently testable workflows. | Maintain traceability and phase gates; no phase is closed on screen presence/compilation alone. |
| **Low — data import ambiguity** | No explicit legacy import format or clinic-provided source data. | Build controlled CSV import/export only with preview, validation, permissions, Unicode, audit, and documented column contract; do not claim arbitrary vendor migration. |

## 15. Phase 0 completion statement

Phase 0 is complete as a planning-only phase: the repository and sandbox were inspected, a Windows-native offline architecture was selected, the domain/security/data/printing/backup/release boundaries were established, risks and external product-owner inputs were recorded, and a 112-section traceability map was created. **At the Phase 0 close, no application requirement was implemented or tested.**

The architecture remains the baseline. A change to WPF/.NET, encrypted SQLite, single-workstation data topology, or native document rendering later requires a written technical reason and phase report; convenience alone is not sufficient. Phase 1 — Foundation was subsequently authorized by the user with `Continue` on 2026-10-01; its implementation and evidence are tracked in `docs/implementation/phase-1-foundation.md`.
