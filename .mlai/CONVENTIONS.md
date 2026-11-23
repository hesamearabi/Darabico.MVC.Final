# Darabico conventions

- ASP.NET Core 9 MVC with EF Core; preserve Persian RTL copy and existing server-owned form state.
- Build: `dotnet build final_p_01.sln --no-restore`. Tests: `dotnet test final_p_01.sln --no-build`; tests use SQLite.
- Shared Precision Workspace design tokens live in `final_p_01/wwwroot/CSS-SOURCE/workspace.css`; navigation and progressive form behavior in `wwwroot/JS-SOURCE/workspace.js`. Reuse Dana fonts and local Bootstrap assets; no jQuery asset is present.
- Do not enable shared output caching on pages with antiforgery tokens or TempData. Independent visitors must receive separate tokens/cookies.
- PhaseModules settings and pillar actions use explicit view names. Invalid submissions return the editor with ModelState before persistence. Shared admin layout owns validation and TempData alerts.
- Auth and TempData cookies are secure in production; HTTP Development uses SameAsRequest. TempData is essential so status feedback does not depend on analytics consent.
- Check browser flows at 390 and 1280px. Login rate limiting is five requests per minute; preserve the protection during testing.
- Existing build warnings include nullable/stream-read warnings and the SixLabors license warning; Release verification needs an appropriate license.

## Guards (added after a mistake repeated)
- Before finishing, stop the servers and background commands you started; long-lived work goes in a job or the preview.
