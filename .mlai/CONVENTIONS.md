# Darabico conventions

- ASP.NET Core 9 MVC with EF Core; preserve Persian RTL copy and existing server-owned form state.
- Build: `dotnet build final_p_01.sln --no-restore --disable-build-servers`. Tests: `dotnet test final_p_01.sln --no-build --disable-build-servers`; tests use SQLite.
- Shared Precision Workspace tokens live in `final_p_01/wwwroot/CSS-SOURCE/workspace.css`; navigation in `wwwroot/JS-SOURCE/workspace.js`. Reuse Dana fonts and local Bootstrap assets; no jQuery asset is present.
- Public consultation uses namespaced `dc-*` classes in `CSS-SOURCE/consultation.css`, loaded after legacy styles, and progressive `JS-SOURCE/inquiry.js`. Keep pixel typography independent of legacy root font sizing.
- `POST /inquiry` returns JSON success or 422 field errors for `Accept: application/json`. Native POST uses one-time TempData and redirects to Home/Inquiry, not Thanks. The native dialog resets the form on success; server validation, antiforgery and persistence remain authoritative.
- Shared admin primary buttons use blue/white `pw-btn--primary`; lead filters and inline editors use responsive CSS grids.
- Do not enable shared output caching on pages with antiforgery tokens or TempData. Independent visitors must receive separate tokens/cookies.
- PhaseModules settings and pillar actions use explicit view names. Invalid submissions return the editor with ModelState before persistence. Shared admin layout owns validation and TempData alerts.
- Auth and TempData cookies are secure in production; HTTP Development uses SameAsRequest. TempData is essential so status feedback does not depend on analytics consent.
- Check browser flows at 390 and 1280px. Login limiter defaults to five requests per ten minutes (`AdminAuth:RateLimitPermitLimit` / `RateLimitWindowMinutes`); public forms allow ten requests per ten minutes. Preserve protection and budget fresh-browser logins during QA.
- Existing build warnings include nullable/stream-read warnings and the SixLabors license warning; Release verification needs an appropriate license.

## Guards
- Before finishing, stop the servers and background commands you started; long-lived work goes in a job or the preview.

## Guards (added after a mistake repeated)
- Report a test, build or fix as passing only after running it in the same task; quote the command and its result, and write "Not verified:" otherwise.
