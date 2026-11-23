# Darabico consultation and leads refinement

## Goal and users
- Audience: Persian-speaking padel project owners and Darabico inquiry staff.
- Screen jobs: Home and Inquiry collect project/contact details; Thanks confirms direct visits; Leads filters and updates persisted inquiries.
- Mood: precise, calm, architectural.
- Direction: existing Precision Workspace, Dana typography, restrained blue actions, RTL alignment.
- Memorable detail: a two-stage project brief that resolves into a quiet, same-page confirmation dialog.

## Scope
In scope: same-page success dialog, reset and validation/error states, both public steps, direct Thanks route, shared admin button contrast, responsive lead filters/cards, regression and browser verification.
Out of scope: new business features, new dependencies, auth changes, database schema changes, deployment.

## Architecture
- Preserve .NET 9 MVC/Razor and EF Core SQL Server: the existing data and authentication boundaries remain authoritative.
- POST /inquiry keeps antiforgery, rate limits, validation and attachment handling. Requests explicitly accepting JSON receive success or field errors; browser JavaScript owns step navigation and dialog presentation only.
- Non-JavaScript POST uses existing TempData and a local source-derived redirect back to Home or Inquiry, not Thanks. The partial consumes the success flag.
- Existing InquiryLead and attachments persist unchanged; no migration. Admin remains behind existing Administrator authorization.
- Shared inquiry partial serves Home and Inquiry (and existing project embedding); native dialog supplies focus containment, Escape dismissal and a close action. Projects link uses the existing /projects route.
- Local verification uses the existing SQL Server container and development app only. No production credentials or data changes.

## Design tokens and cascade audit
- Six core hex colors: background #F5F5F7, surface #FFFFFF, text #2C2C2E, muted #626268, accent #0066CC, hover #0052A3. Borders/tints/overlay derive from these; existing semantic error colors remain in shared workspace tokens.
- Blue/white contrast is approximately 5.57:1 (hover 7.7:1); muted/white approximately 6:1. Confirm numerically and inspect rendered controls.
- Dana regular/bold; public scale 14, 15, 18, 22, 28px independent of legacy root rem sizing. Spacing 4, 8, 12, 16, 24, 32, 48px; radii 10/16px; two shared shadow levels.
- Consultation component uses unique dc-* classes and data-inquiry-form selectors in consultation.css loaded after legacy CSS. Remove legacy button/layout classes from the partial rather than adding !important overrides.
- Form width min(100%,760px), two equal minmax(0,1fr) columns above phone widths, one column on phones. Labels and controls 15px, hints 14px, 44px touch targets.
- Admin shared button rules use :where() to keep variant specificity equal; primary variants must not lose to the later generic .btn background rule. Leads uses explicit pw-btn variants and the existing workspace tokens.
- Lead filters: four balanced columns on wide content, two intermediate, one phone; actions span the grid. Lead cards group identity/status and compact inline update controls, stacked on phones.
- Audit result: preserve actual branding/layout/assets from this repository; no external brand substitution, new imagery or decorative template cards. All links already exist. Check RTL, long names, empty results, keyboard focus, disabled/loading, errors, dialog close and reduced motion.

## Risks and assumptions
- Assumed JSON progressive enhancement with native POST fallback; no dependencies required.
- Existing SQL Server is the local development database. Create uniquely identifiable temporary records and remove only those records after verification.
- Managed preview does not recognize this .NET root; use a supervised .NET development job and the built-in browser, then stop it.
- Browser tool controls available may not expose an explicit networkidle wait; report this limit rather than claim unsupported verification.
- Keep existing build/license warnings visible; do not change unrelated modules.

## Phase 1: Complete the requested public and admin slice
Goal: improve the existing real create/list/update workflow end to end.
- [x] Implement same-page JSON success, accessible dialog/reset, and native POST fallback with regression tests.
- [x] Restyle both public form steps and direct Thanks route.
- [x] Correct shared admin button cascade and restructure Leads filters/cards.
- [x] Build and test, exercise all requested screens at 390x844 and 1280x800, verify database persistence, clean temporary data and stop processes.
Acceptance: create a real inquiry without leaving the page, dismiss confirmation, reload and find it in admin; both widths remain legible and usable.

Verification: Debug build has zero errors; 91 tests pass, including five new regression cases that failed before the implementation. JavaScript syntax and git whitespace checks pass. Home and Inquiry submissions open/reset/close the dialog without navigating to Thanks at both requested widths. Thanks and its projects link work at both widths. Final admin filter/empty-state/card checks pass at both widths with zero reported clipping, overlap, low contrast, overflow, console errors or network failures. Blue/white button contrast is 5.57:1 (hover 7.68:1). SQL confirmed six browser submissions plus a native Home submission and the admin update; all seven QA records were removed transactionally, preserving the six unrelated leads. Application job, SQL container and build servers were stopped; no test screenshots or upload artifacts were created in the repository.

Limitations/findings: the browser tool cannot explicitly request networkidle. Public whole-page layout scans still flag legacy header/footer/about content and intentionally clipped accessibility/honeypot content outside the redesigned controls. Earlier status assertions matched hidden select options; SQL confirmed the actual update. Repeated QA requests hit the existing login/form 429 limits; final admin checks passed after the window expired, but the additional native Inquiry HTTP replay remained rate-limited (the native route contract is covered by passing tests). ImageSharp reports a missing commercial license warning for Release builds.

## Repository cleanup and layer alignment
- [x] Move project query handlers and database/storage abstractions into Application.
- [x] Move project image/video and private inquiry attachment storage into Infrastructure; adapt HTTP uploads in MVC.
- [x] Standardize active assets, view models and shared partials; preserve legacy image URLs, migrations and uploads.
- [x] Audit local asset references and record every deletion/move in `CLEANUP-FILES.md`.
- Verification: regression coverage is in the existing inquiry and SEO integration test files. Managed preview cannot host this ASP.NET application, so browser visual verification remains unavailable.
- Scope: unrelated legacy controller persistence and post-media helpers are unchanged; optional admin visual expansion remains deferred.

## Phase 2: Optional future expansion
Goal: extend coverage only if requested later.
- [ ] Expand automated visual coverage to unrelated admin modules.
Acceptance: no additional product work is included in this run.
