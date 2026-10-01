# Dentiva Pro — Requirement Traceability Register

**Baseline:** Phase 0, 2026-10-01<br>
**Current implementation status:** Phase 1 Foundation scaffold is in progress; see `docs/implementation/phase-1-foundation.md`. No product workflow requirement is fully accepted.<br>
**Source of truth:** The 112 numbered sections of the Dentiva Pro Master Production Build Prompt, plus each nested bullet and final checklist item.

## How this register is used

1. Each numbered prompt section maps to one stable requirement ID (`DP-001`–`DP-112`). All nested bullets/checklist items under that section inherit the ID until implementation expands them into child IDs (`DP-015.1`, etc.). High-risk/repeated requirements (RBAC, money, backup/restore, printing, Bengali, high-DPI, destructive actions, installer/release) must be split into individual child rows before that phase is accepted.
2. An ID may be closed only when the feature is reachable by the right role, the real operation works, persisted data and related entities are correct, permission checks pass through direct service calls, error/empty/loading paths work, and restart behavior is verified where applicable. A screen or a passing build is not acceptance.
3. Each phase report will record status (`Not started`, `In progress`, `Blocked`, `Verified`), implementation location, test-case IDs/results, reviewer evidence, known limitations, and the commit/PR containing the change. `Verified` requires reproducible evidence; untested work remains open.
4. Tests are identified at Phase 0 by suite. During implementation, concrete test case IDs and reports are added next to each child ID. Final audit searches for any unmapped requirement, failed test, unexplained limitation, fake UI, or missing evidence.
5. Requirements duplicated by the final acceptance checklist (§103) link to the original feature ID as well as the release-gate ID. They are not counted as independently satisfied merely because the same screen exists.

## Test / evidence catalogue (roadmap; Phase 1 foundation suites are tracked in the implementation report)

| Suite ID | Evidence type | Minimum coverage |
|---|---|---|
| `PLAN` | Architecture/process review | Decision records, phase gate and documented scope/assumptions. |
| `DOM` | Domain/unit tests | Money, dates, validation, clinical/appointment/queue transitions, FDI tooth mapping, permissions and invariants. |
| `DB` | Database/integration tests | Encrypted SQLite, migrations, Unicode, foreign keys, unique constraints, transactions/rollback, concurrency, query/index behavior, restart. |
| `SEC` | Security/adversarial tests | Authentication, password hash, RBAC direct-call bypass, auto-lock, activation, attachment, financial, audit, restore and destructive-action controls. |
| `UIA` | Windows UI Automation/manual interaction | Navigation, buttons/tabs/dialogs, focus, keyboard, forms, filters, scroll, empty/loading/error states, layout and accessible names. |
| `CLIN` | Clinical workflow integration | Patient → visit → chart/findings/treatment/prescription/referral/timeline/appointment/queue flows and history preservation. |
| `FIN` | Financial workflow tests | Invoice/payment allocation, BDT poisha arithmetic, reversals, reports, inventory valuation/expenses, role restrictions and printed totals. |
| `PRINT` | Windows print/PDF evidence | Same-renderer preview and output, A4/A5/small/custom supported paper, Bengali, long content, margins, tables, logo, signature, real PDF inspection. |
| `FILE` | File/attachment tests | Type/content/size/path validation, encryption/hash, duplicate handling, missing/corrupt files, permission, preview and cleanup. |
| `BACKUP` | Backup/restore integration | Real package, timestamp/destination, read-back/integrity, scheduled and failed jobs, pre-restore safety copy, staging/swap/rollback, restart. |
| `PERF` | Performance/stress evidence | Synthetic large datasets, pagination/search, profile history, memory/latency, lock contention, attachment volume and responsiveness. |
| `DPI` | Windows display evidence | 100/125/150/200% at supported effective workspaces; minimum and larger common resolutions; overflow, clipping, scaling and monitor changes. |
| `INST` | Windows installer/artifact tests | Clean install, first run, upgrade, activation, icon/shortcuts, self-contained dependencies, data retention, uninstall and restart. |
| `DEP` | Release/dependency audit | Direct/transitive dependency versions, licenses, native assets, network behavior, vulnerabilities, notices, SBOM and checksums. |
| `REL` | Final release gate | Exact artifact-level end-to-end run, full requirement matrix, PR/release authorization, GitHub Release or documented fallback. |

## Section-level traceability matrix

| ID | Prompt section / requirement group | Planned implementation phase(s) | Planned acceptance / evidence |
|---|---|---:|---|
| `DP-001` | Product vision: complete offline Bangladesh clinic lifecycle, English/Bengali content, BDT, no paid/cloud core dependency. | 0–18 | `PLAN`, `DEP`, `REL`: requirement coverage; offline runtime/network audit; all module workflows and BDT/Unicode tests. |
| `DP-002` | Understand repository/environment; evaluate and choose architecture based on actual constraints. | 0 | `PLAN`: repository, runtime, OS, GitHub and tool inventory plus documented architecture alternatives/decision. |
| `DP-003` | Complete and test one phase, report, stop; wait for `Continue`. | Every gate | `PLAN`: phase status and report, no next-phase work before user authorization. |
| `DP-004` | No broad implementation before architecture/planning. | 0–1 | `PLAN`: Phase 0 contains decisions and phase plan; Phase 0 diff contains documentation only. |
| `DP-005` | No knowingly incomplete/fake/dead/broken/high-severity production functionality. | All; gates 13–17 | `UIA`, `SEC`, all domain suites, release scan; no unhandled release-blocking flow. |
| `DP-006` | Dentiva Pro product identity and properly packaged, balanced Windows icon. | 2, 12, 16–17 | `UIA`, `INST`: alpha/crop/optical-centering review at all packaged sizes; actual taskbar/Start Menu test. |
| `DP-007` | Premium clinical technology design system and reduced motion. | 2, 12 | `UIA`, design review: token/component coverage, reduced-motion behavior, consistency and accessibility. |
| `DP-008` | Alignment, spacing, typography, overflow, responsive layouts, scrolling, high-DPI and states. | 2, 5–12, 14 | `UIA`, `DPI`: screen-by-screen checklist, screenshots/automation, scrolling and state verification. |
| `DP-009` | Supported Windows resolution and 100/125/150/200% scaling. | 2, 14, 17 | `DPI`: full documented matrix; no inaccessible controls/clipping at supported workspace sizes. |
| `DP-010` | Header, clinic identity/date/notifications/user area, collapsible sidebar and all navigation modules. | 2, 10 | `UIA`: every nav destination works, permission-gated, sidebar expand/collapse persists/behaves, all destinations have real screens. |
| `DP-011` | First-run activation/setup, clinic, logo, multiple dentists, structured qualifications/designations and administrator. | 3–4 | `UIA`, `DB`, `SEC`: clean install, required/invalid/incomplete wizard, multi-dentist values, atomic save/restart. |
| `DP-012` | Secure auth, strong password hashing, no default/cleartext secret, logout and selectable auto-lock. | 3, 13 | `SEC`, `DB`, `UIA`: salted Argon2id storage; correct/incorrect login, lock, logout and 5/10/15/30-minute enforcement. |
| `DP-013` | Distinct Dentist, Staff and Application User entities with optional links. | 3–4 | `DOM`, `DB`: create/profile/link/unlink scenarios prove identities are not merged or required to be one-to-one. |
| `DP-014` | Granular RBAC, role editor, business/service-layer enforcement, sensitive financial segregation. | 3, 13 | `SEC`: role/permission matrix and direct application-service calls deny every unauthorized read/write/export/restore. |
| `DP-015` | Patient registration, unique code, clinical/contact fields, time filters/newest default, advanced search; no arbitrary record cap. | 5, 10 | `CLIN`, `DB`, `PERF`, `UIA`: registration/validation, code collision, all date windows, sort/filter/search, large record fixtures. |
| `DP-016` | Complete patient profile, history/timeline/dental/appointments/referrals/attachments/financial rollups and in-context actions. | 5, 8 | `CLIN`, `FIN`, `FILE`, `UIA`: linked-history accuracy and each new-record action from patient context. |
| `DP-017` | Separate visit entity, unlimited encounter history, provider/clinical context; never overwrite a prior visit. | 5 | `CLIN`, `DB`: create successive encounters, compare stored history/snapshot, restart and chronology checks. |
| `DP-018` | Clinical timeline with dates, actors/providers, event links and navigation to source records. | 5 | `CLIN`, `UIA`: event ordering, identity/time attribution, each event link opens the correct source record. |
| `DP-019` | Interactive adult/pediatric chart, numbering, multi-tooth selection and persisted encounter-specific historical states. | 5 | `CLIN`, `DB`, `UIA`: adult/pediatric FDI fixtures, single/multiple selection, save/reopen, distinct visit snapshots. |
| `DP-020` | Treatment catalog/configuration, visit/invoice links, immutable historical pricing. | 4–5, 8 | `DOM`, `DB`, `FIN`: edit catalog after invoicing; prior invoice line/total remains unchanged; inactive treatments handled. |
| `DP-021` | Referral date/reason/destination/notes/status/follow-up and patient history. | 5 | `CLIN`, `DB`, `UIA`: referral create/edit/status/history/link and date/provider persistence. |
| `DP-022` | Provider/patient appointments, reasons/statuses, upcoming/today/history/cancelled/no-show views and profile creation. | 6 | `CLIN`, `UIA`, `DB`: scheduling, invalid time, provider conflicts/rules, every status/filter and profile flow. |
| `DP-023` | Persistent operational queue and Waiting/Called/In Consultation/Treatment/Completed/Cancelled transitions. | 6 | `CLIN`, `DB`, `UIA`: ordering, allowed/denied transitions, provider view, concurrent action and restart persistence. |
| `DP-024` | Prescription from module/profile; multiple medicines and detailed customizable dose/frequency/timing/instructions/templates. | 4, 7 | `CLIN`, `DB`, `UIA`: all medicine fields, custom entries/templates, provider/patient/visit links and restart. |
| `DP-025` | Configurable selectable chief complaint, examination/observation and advice libraries. | 4, 7 | `CLIN`, `UIA`, `DB`: seeded example phrases, add/edit/disable/custom selections and persistence. |
| `DP-026` | Prescription document header, actual dentist/qualifications, patient data, clinical/medicine composition and usable signature area. | 7 | `PRINT`: multiple designations/qualifications and provider identity; measured signature space and long-content layout. |
| `DP-027` | Physical print/PDF, preview, A4/A5/thermal/custom profiles, margins, wrapping and Bengali. | 7, 14, 17 | `PRINT`: real generated documents to Windows print/PDF; page dimensions, Bengali glyphs, overflow, signature and paper tests. |
| `DP-028` | Invoice identity, line items, totals, partial/unpaid/paid states; no prescription-only header/signature. | 8 | `FIN`, `PRINT`, `DB`: invoice arithmetic/status and reviewed PDF/print with no unintended doctor/signature content. |
| `DP-029` | Payment records, all required Bangladesh methods, partial allocation and controlled reversal/audit. | 8 | `FIN`, `SEC`, `DB`: Cash/Bank/Card/bKash/Nagad/Rocket/Upay/Other, actor/time/reference, allocation/reversal history. |
| `DP-030` | Payment date range, today default, totals and permission-appropriate reporting. | 8, 10 | `FIN`, `SEC`, `UIA`: Today/7/30/90/365/custom boundaries, totals and denied report access. |
| `DP-031` | Inventory master, supplier/purchase/batch/expiry/reorder, stock movement, damage/expiry/use, low/expiry alerts. | 9–10 | `DOM`, `DB`, `FIN`, `UIA`: movement ledger reconciliation, alerts, expiry edge cases and no silent stock overwrite. |
| `DP-032` | Configurable income/expense categories, clinic expenses, accounting periods and RBAC. | 9 | `FIN`, `SEC`, `DB`: income/payment linkage without double count, expense create/edit/reversal, period totals and permission denial. |
| `DP-033` | Exact money arithmetic, immutable historical pricing, atomic financial mutations and consistency. | 1, 8–9, 13 | `DOM`, `DB`, `FIN`: integer-poisha boundary/property tests; injected failures prove transaction rollback and invoice/payment invariants. |
| `DP-034` | Staff personnel details and status, kept separate from login identities. | 3–4 | `DB`, `UIA`, `SEC`: personnel CRUD/archive, validation, optional user link and role restrictions. |
| `DP-035` | Global search across relevant domains, filters/date ranges and permission-respecting results. | 10 | `DB`, `SEC`, `PERF`, `UIA`: Bengali/English/code/phone search, filters, pagination and no unauthorized result leakage. |
| `DP-036` | Meaningful notification center for appointments, queue, stock, dues, backup and security events without spam. | 10–11 | `DB`, `UIA`: trigger/dedupe/read/dismiss/permission tests and scheduled-backup failure warnings. |
| `DP-037` | Responsive role-aware operational dashboard and protected financial widgets. | 10 | `UIA`, `SEC`, `PERF`: widget calculations, role-specific content, loading/empty/error and balanced layouts. |
| `DP-038` | Secure external attachments with metadata, integrity, safe names, duplicate handling and access control. | 5, 13 | `FILE`, `SEC`: malformed/oversize/missing/duplicate/traversal/type mismatch, encrypted storage, permission denial and view cleanup. |
| `DP-039` | Manual destination choice, timestamped backup, automatic 7/15/30-day schedules. | 11 | `BACKUP`, `UIA`: user-selected folder, exact naming, due/missed schedule, logged-out/offline/destination-failure semantics. |
| `DP-040` | Read-back backup verification, structure, checksums, readability and restoreability. | 11 | `BACKUP`: create package, re-open, verify every checksum, encrypted DB integrity and referenced attachment integrity. |
| `DP-041` | Authorized restore, warning/confirmation, pre-restore backup, atomic staging/swap and post-restore integrity. | 11, 13 | `BACKUP`, `SEC`, `DB`: valid/invalid/damaged package, failure rollback, unauthorized denial, safety restore and restart. |
| `DP-042` | Comprehensive permission-aware Settings covering clinic, providers, roles, catalogs, printer, security, backup, notifications and preferences. | 4, 12 | `UIA`, `SEC`, `DB`: settings coverage inventory, actual persisted behavior, role enforcement and validation for each section. |
| `DP-043` | Destructive operation warnings, re-auth/scope confirmation, backup and audit. | 12–13 | `SEC`, `DB`, `UIA`: cancel/incorrect-scope/unauthorized/no-backup/success cases; verify no collateral data loss. |
| `DP-044` | Archive/void/reverse/inactivate instead of destructive deletion for historical data. | 5–9, 12–13 | `DOM`, `DB`, `FIN`, `CLIN`: lifecycle state transitions retain clinical/financial/audit history and linked-record integrity. |
| `DP-045` | Audit for identity, clinical, financial, inventory, backup, settings, activation and destructive actions. | 3, 13 | `SEC`, `DB`: actor/time/action/entity/safe before-after audit; no secrets; access restrictions and tamper-detection behavior. |
| `DP-046` | One-time offline fixed-code activation with derived verifier, persisted state and fresh-install tests. | 3–4, 13 | `SEC`, `INST`: no literal secret in source/resources/config/logs; successful/failed/rate-limited activation and persistence. |
| `DP-047` | No unbreakable reverse-engineering claim; practical defense-in-depth. | 0, 3, 13, 15 | `PLAN`, `SEC`, `DEP`: written threat boundary and binary/source inspection report; no unsupported secrecy claim. |
| `DP-048` | Reliable local relational DB/schema/entities/migration foundation. | 1, 5–11 | `DB`: ER/schema review, FK/index/migration tests, backup/restore and corruption/recovery path. |
| `DP-049` | Explicit patient/visit/prescription/appointment/invoice/payment/inventory/user relationships. | 1, 5–9 | `DB`, `CLIN`, `FIN`: ER diagram and relationship/constraint tests including restrict/cascade rules. |
| `DP-050` | Concurrent access, conflict detection, transactions and safe write serialization. | 1, 13–14 | `DB`, `PERF`: multi-user/multi-thread read/write contention, busy retry, duplicate prevention, conflict/no-silent-overwrite tests. |
| `DP-051` | Crash safety, transactionality, safe user errors and privacy-aware technical diagnostics. | 1, 13–14 | `DB`, `SEC`: injected crashes/failures across multi-step writes; reopen integrity; user-facing error and redacted logs. |
| `DP-052` | Shared print engine with paper dimensions, page breaks, Unicode, logo/header/footer and future document extensibility. | 7–8 | `PRINT`: prescription/invoice consume shared renderer; format matrix and document template regression suite. |
| `DP-053` | Preview matches production output. | 7–8 | `PRINT`: preview and printed/PDF page geometry/text comparison using the same paginator and paper ticket. |
| `DP-054` | Windows-installed printer profiles and representative compatibility without universal claims. | 7–8, 14 | `PRINT`, `UIA`: enumerate/save/select installed queues and paper; representative hardware/driver tests and limitation statement. |
| `DP-055` | PDF preserves layout, Unicode, margins, tables, logo and signature. | 7–8, 14 | `PRINT`: actual PDF files parsed/rendered/visually reviewed across document/format/content fixtures. |
| `DP-056` | About contains exactly Shohan Khan and helloiamshohan@gmail.com; no invented biography. | 12 | `UIA`: About content assertion and visual fit/accessibility test. |
| `DP-057` | Core operation offline, no telemetry/analytics/cloud/paid/mandatory online dependency. | 0–18 | `DEP`, `SEC`, `REL`: runtime network review/offline test; no mandatory licensing, update, login, or cloud request. |
| `DP-058` | Direct/transitive dependency purpose/version/license/network/security/notice audit. | 0, 1, 15–18 | `DEP`: SBOM plus human-reviewed license/notice/native asset/runtime-network/security inventory. |
| `DP-059` | Local storage; no unnecessary external/cloud DB/server. | 0–1, 13, 15 | `PLAN`, `DEP`, `SEC`: architecture and runtime process/network inspection; database location and ACL test. |
| `DP-060` | Sensible keyboard shortcuts, visible focus and logical navigation. | 2, 12 | `UIA`: shortcut map plus keyboard-only end-to-end tests for navigation/search/save/cancel/close/print. |
| `DP-061` | Labels, states, accessible keyboard flow, focus, tooltips and readable contrast. | 2, 12 | `UIA`: UI Automation names/roles, focus traversal, contrast/state review and screen-reader spot checks. |
| `DP-062` | Required/format/range/duplicate/date/financial/quantity/domain validation. | 2–12 | `DOM`, `UIA`, `DB`: invalid/valid boundary tests for every form, errors adjacent to controls, no invalid commit. |
| `DP-063` | Loading/empty/error/retry/success states for major screens and asynchronous work. | 2–12 | `UIA`: deterministic state fixtures for each major module and retry/recovery pathways. |
| `DP-064` | Upload type/size/location/duplicates/errors; malformed file must not crash; permission. | 5, 13 | `FILE`, `SEC`: adversarial extension/MIME/size/path/duplicate/corrupt/missing scenarios and recoverable errors. |
| `DP-065` | Consistent UTC/local date-time handling and no reinterpretation of historical timestamps. | 1, 4–12 | `DOM`, `DB`, `UIA`: Asia/Dhaka/DST-independent conversion, UTC ordering, appointment boundaries, restart/export round-trip. |
| `DP-066` | Correct BDT/৳ and decimal precision. | 1, 8–9 | `DOM`, `FIN`, `PRINT`: integer-poisha arithmetic/display/rounding, Bengali/English document totals and report consistency. |
| `DP-067` | Operational/financial reports (patient, appointment, visit, treatment, prescription, money, stock, expenses/income). | 8–10 | `FIN`, `SEC`, `DB`: report inventory, ranges/totals, export/print where offered, role filtering and large-data paging. |
| `DP-068` | Controlled permission-aware data export; Unicode preserved; no secrets leaked. | 12 | `FILE`, `SEC`, `DB`: UTF-8/Bengali CSV/JSON export, scope/permission/audit, no passwords/keys/activation material; imports previewed/validated if offered. |
| `DP-069` | Responsive large-data performance via pagination/indexes/lazy loading/optimized search. | 5, 10, 14 | `PERF`: declared dataset sizes and measured latency/memory; no unbounded UI history load or attachment-blocking operation. |
| `DP-070` | Realistic stress tests across all record types and large search/history. | 14 | `PERF`: reproducible synthetic fixture, observed response/memory/DB integrity, failures fixed and suites rerun. |
| `DP-071` | Dedicated auth/RBAC/password/session/destructive/restore/attachment/activation/audit/dependency security review. | 13–15 | `SEC`, `DEP`: adversarial test report; direct/indirect bypass cases; critical/high findings closed with regressions. |
| `DP-072` | Every major navigation/control/form/table/modal/scroll/print/save/delete path tested. | 2–14 | `UIA`, `PRINT`, `BACKUP`: control inventory paired with automation/manual result; no untested required flow at release. |
| `DP-073` | No fake UI or inert required controls. | All; 15–17 | `UIA`, release scan: all enabled controls have verified action; no placeholder/dead navigation. |
| `DP-074` | Professional Windows installer, fresh install, setup/activation, shortcuts/icon, data path and uninstall. | 16–17 | `INST`: exact installer installed on clean Windows; first run and data directories/shortcuts/icon verified. |
| `DP-075` | Actual clean-machine installation where available; full end-to-end verification. | 17 | `INST`, `REL`: exact release artifact on clean supported Windows, all named workflows, printing, backup/restore/restart/uninstall. |
| `DP-076` | Uninstall behavior for binaries, shortcuts, settings, database, attachments and backups; no accidental data deletion. | 16–17 | `INST`, `BACKUP`: uninstall matrix and explicit data-retention/deletion confirmation tests. |
| `DP-077` | No final production EXE before complete requirements, tests, security, license, installer, DPI, print, backup/restore and defect gates. | 15–17 | `REL`: release checklist blocks packaging/publishing until all prerequisite evidence is green. |
| `DP-078` | Git/GitHub, meaningful PRs, GitHub Actions validation/build/release. | 1, 18 | `PLAN`, `DEP`, `REL`: CI run, PR review point, protected/minimal permissions and reproducible release workflow. |
| `DP-079` | Agent must never merge a PR; `Continue` is not merge approval. | 0–18 | `PLAN`, `REL`: PR stays open; separate explicit merge authorization recorded before release publication. |
| `DP-080` | GitHub Release artifact preferred; `dist/` fallback only for documented technical impossibility. | 16–18 | `REL`: release page artifact/checksum/version verified, or documented fallback in `dist/`. |
| `DP-081` | No required feature deferred to a future version/roadmap. | 0–18 | `PLAN`, `REL`: all IDs verified or explicitly blocked and reported; no “coming soon” closure. |
| `DP-082` | Phase 0 inspection and complete architecture/plan with stop gate. | 0 | `PLAN`: this report, architecture document, traceability register; no application code. |
| `DP-083` | Phase 1 foundation: project/config/DB/security/logging/tests/CI/design shell. | 1 | `DOM`, `DB`, `SEC`, CI: clean build and foundational tests before Phase 2. |
| `DP-084` | Phase 2 design system/application shell, navigation/responsive/accessibility/DPI. | 2 | `UIA`, `DPI`: shell and component acceptance before Phase 3. |
| `DP-085` | Phase 3 authentication/users/staff/dentists/RBAC/auto-lock/audit foundation. | 3 | `SEC`, `DB`, `UIA`: full permission and session tests before Phase 4. |
| `DP-086` | Phase 4 setup, clinic/dentists, structured qualifications, catalogs, suppliers, phrase/settings foundation. | 4 | `DB`, `SEC`, `UIA`: clean first-run and master-data coverage before Phase 5. |
| `DP-087` | Phase 5 patient/clinical core, unlimited visit history, timeline, dental chart, attachments, referrals. | 5 | `CLIN`, `FILE`, `PERF`, `DB`: clinical acceptance before Phase 6. |
| `DP-088` | Phase 6 appointments and persistent operational queue. | 6 | `CLIN`, `UIA`, `DB`: full statuses/provider/queue tests before Phase 7. |
| `DP-089` | Phase 7 prescriptions, Unicode, provider identity, print preview/paper profiles/signature. | 7 | `CLIN`, `PRINT`, `SEC`: actual A4/A5/small/Bengali evidence before Phase 8. |
| `DP-090` | Phase 8 invoices/payments/partial due/reports/print and immutable history. | 8 | `FIN`, `PRINT`, `SEC`, `DB`: financial and printed totals accepted before Phase 9. |
| `DP-091` | Phase 9 inventory/purchases/movements/low-stock/expiry/accounting/income/expense. | 9 | `FIN`, `DB`, `SEC`: stock and accounting reconciliation before Phase 10. |
| `DP-092` | Phase 10 dashboard/global search/notifications/permission-aware summaries. | 10 | `UIA`, `SEC`, `PERF`: query, access, deduplication and layout tests before Phase 11. |
| `DP-093` | Phase 11 manual/scheduled backup, integrity, pre-restore safety and verified restore. | 11 | `BACKUP`, `SEC`, `DB`: actual backup/restore tests before Phase 12. |
| `DP-094` | Phase 12 settings/About/developer info/destructive safeguards/export/accessibility polish. | 12 | `UIA`, `SEC`, `FILE`: settings/export/security/polish tests before Phase 13. |
| `DP-095` | Phase 13 dedicated security/data-integrity audit and remediation. | 13 | `SEC`, `DB`, `DEP`: critical/high findings closed; no move to Phase 14 until evidence passes. |
| `DP-096` | Phase 14 full QA, performance/stress/DPI/print/PDF/Unicode/installer/regression. | 14 | All applicable suites: failures fixed at root cause and affected suites rerun. |
| `DP-097` | Phase 15 release audit for TODOs, mock/fake/dead code, secrets, licenses, network, unfinished handlers. | 15 | `DEP`, `UIA`, `SEC`, `REL`: release scan and manual audit report; no required incomplete functionality. |
| `DP-098` | Phase 16 final production build, installer, metadata/checksum and report. | 16 | `INST`, `DEP`, `REL`: versioned artifact and reproducible build/test manifest. |
| `DP-099` | Phase 17 validate exact artifact end-to-end; rebuild/retest if any defect. | 17 | `INST`, `PRINT`, `BACKUP`, `REL`: clean supported Windows test evidence; artifact checksum matches tested bits. |
| `DP-100` | Phase 18 release workflow; PR never agent-merged; release after explicit authorization. | 18 | `REL`: authorized tagged release, artifact and checksum verification, or documented `dist/` fallback. |
| `DP-101` | Continuous requirement matrix mapping implementation/test/acceptance/final verification. | All | This register maintained every phase; final audit finds no unmapped nested requirement/checklist entry. |
| `DP-102` | Feature acceptance requires access, correct behavior, persistence, relationships, permissions, errors/UI states and restart. | All | `DOM`, `DB`, `SEC`, `UIA`: evidence covers all dimensions for each feature before `Verified`. |
| `DP-103` | Final acceptance checklist: all modules, roles, auth, clinical/financial/stock, backups, documents, quality, offline, release. | 13–18 | `REL`: line-by-line checklist linked to original IDs and test artifacts; all mandatory rows pass or are transparently blocked. |
| `DP-104` | Root-cause investigation, fix and regression test for errors; no startup-only success. | All | Failure report links root cause and regression test; impacted suites rerun; no swallowed exceptions. |
| `DP-105` | Continue/resume by inspecting state and resuming first incomplete task, without repeats/skips. | Every gate | `PLAN`: each continuation begins with branch/status/phase/test/PR inspection; resume report notes exact task. |
| `DP-106` | Detailed phase report: objective, changes, architecture, schema/security/UI/tests/failures/fixes/risks/files/branch/PR/next phase. | Every gate | `PLAN`: report template completed; tests/limitations/acceptance status explicit; stop after report. |
| `DP-107` | Final production report, architecture/modules/security/printing/backup/testing/installer/release/licenses/limitations/coverage. | 18 | `REL`: final report includes artifact/hash, release state, full traceability coverage and verified limitations. |
| `DP-108` | Priority order: integrity, security, correctness, reliability, usability, maintainability, performance, accessibility, visual polish. | All | `PLAN` and phase reviews: documented trade-offs resolve in this order; no unsafe cosmetic shortcut. |
| `DP-109` | Resolve ordinary ambiguity professionally; document decisions with material architectural/security/commercial impact. | 0 and later gates | `PLAN`: assumptions/owner inputs in architecture document; approval requested for topology or commercial blockers. |
| `DP-110` | Commercial product quality: intentional screens, working interactions, traceable clinical history, trustworthy finances, meaningful permissions and tested release. | All; final 17–18 | All suites + `REL`: end-to-end customer workflows, UI review and exact-artifact evidence. |
| `DP-111` | Stop/report on incompatible dependency, unavailable platform, unresolved critical security/data issue, failed workflow, installer/artifact/PR authorization gate. | Every gate | `PLAN`, `SEC`, `DB`, `INST`, `REL`: blocker is reported with evidence; no bypass/false completion. |
| `DP-112` | First action is repository/environment inspection and Phase 0 analysis; no premature build. | 0 | `PLAN`: inspection evidence, architecture decision, risk/phase plan and this stop report. |

## Phase closure checklist template

Copy this block into every phase report and update the IDs touched by that phase:

```text
Phase: [number/name]
Requirement IDs touched: [IDs + child IDs]
Implementation files/modules: [paths]
Acceptance cases: [test IDs]
Automated result: [command/run URL, counts, failures]
Manual/artifact evidence: [screenshots, PDF, installer, backup, etc.]
Security/permission result: [direct service-path results]
Data/restart result: [persistence, relationship, recovery result]
Known limitations/blockers: [none or specific]
Status: [Not started / In progress / Blocked / Verified]
```

## Phase 0 close (historical)

- `DP-001`–`DP-112`: **architecture/plan mapped; implementation had not started at the Phase 0 close; all product acceptance was unverified.**
- Planning artifacts: `docs/architecture/phase-0-architecture.md` and this register.
- Phase 0 had no product tests to run and created no app code, assets, dependency changes, installer, or production build.
- Phase 1 — Foundation was subsequently authorized by the user with `Continue` on 2026-10-01.

## Current checkpoint — Phase 1

- Phase 1 foundation changes, verification results, traceability limits, and blockers are recorded in `docs/implementation/phase-1-foundation.md`.
- The section-level requirement rows remain acceptance criteria, not completion claims. A foundation primitive or passing CI does not close an end-to-end product requirement.
- Next action: stop at the Phase 1 gate and wait for a separate `Continue` before Phase 2 — Design system & shell.
