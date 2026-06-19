# Lait Plausible Analytics for Umbraco

An Umbraco **17** (Bellissima backoffice) integration for [Plausible Analytics](https://plausible.io).
It adds:

- A **Tracking** dashboard in the **Content** section (next to *Welcome*) showing visitors, page
  views, visits, bounce rate and top pages.
- A **Page tracking** block on every document's **Info** tab — e.g. *“100 views (30 unique)”* for
  the last 7 days.

The browser never talks to Plausible directly: the backoffice calls an authenticated Management API
endpoint, and the server holds the Plausible API key and proxies the request.

## Project layout

```
Lait.Umbraco.Analytics.Plausible/
├─ Lait.Umbraco.Analytics.Plausible.sln
├─ umbraco-marketplace.json
└─ src/Lait.Umbraco.Analytics.Plausible/
   ├─ Composing/PlausibleComposer.cs        # DI registration
   ├─ Configuration/PlausibleOptions.cs      # appsettings binding
   ├─ Controllers/TrackingController.cs       # /umbraco/management/api/v1/lait-tracking/*
   ├─ Models/TrackingModels.cs
   ├─ Services/PlausibleClient.cs             # typed HttpClient (Stats API v2, Bearer auth)
   ├─ Services/TrackingService.cs             # query + cache + URL resolution
   └─ wwwroot/                               # backoffice client (plain JS + Lit, no build step)
      ├─ umbraco-package.json                 # served at /App_Plugins/LaitTracking/...
      ├─ api.js
      ├─ tracking-dashboard.js
      └─ page-tracking-info.js
```

The backoffice client is intentionally **plain JavaScript** importing Lit from the backoffice's own
import map, so there is **no npm/Vite build** — it runs as-is. (You can migrate to a TypeScript + Vite
build later; the manifest just needs to point at the bundled output.)

The client lives in `wwwroot` and the project sets `StaticWebAssetBasePath = App_Plugins/LaitTracking`.
That's how Umbraco 17 discovers the `umbraco-package.json` from a referenced Razor Class Library, so
the dashboard loads over **both a project reference and a NuGet install — with no manual file copy**.

## Authenticating with Plausible

1. In Plausible: account name (top-right) → **Settings** → **API Keys** → **New API Key** → choose
   **Stats API**. Copy it (shown once).
2. The key is sent as `Authorization: Bearer <key>` to `POST {BaseUrl}/api/v2/query`. Default rate
   limit is 600 requests/hour, which is why responses are cached.
3. Self-hosting Plausible Community Edition? Same auth — just point `BaseUrl` at your instance.

## Configure

Add to your Umbraco site's `appsettings.json` (put the **key** in user-secrets or an environment
variable, not in source control):

```json
{
  "Lait": {
    "Tracking": {
      "Plausible": {
        "BaseUrl": "https://plausible.io",
        "SiteId": "your-site.com",
        "ApiKey": "your-stats-api-key",
        "CacheSeconds": 120,
        "TopPagesLimit": 5
      }
    }
  }
}
```

If `SiteId`/`ApiKey` are missing, the UI shows a friendly “not configured” state instead of erroring,
so you can install first and configure later.

## Test in a local Umbraco 17 site

You need an Umbraco 17 website to host the package. Two ways to wire it up:

**Easiest: run the helper script** (from the repo root, PowerShell): `.\test\setup-local-test.ps1`.
It installs the U17 template, creates a SQLite-backed site under `test/TestSite`, references this
project, and runs it. Login: `admin@example.com` / `P@ssw0rd1234`.

**A. Project reference (recommended while developing)**

1. Add the project to your Umbraco solution and reference it from the web project:
   ```bash
   dotnet sln <YourSite>.sln add src/Lait.Umbraco.Analytics.Plausible/Lait.Umbraco.Analytics.Plausible.csproj
   dotnet add <YourSite>/<YourSite>.csproj reference src/Lait.Umbraco.Analytics.Plausible/Lait.Umbraco.Analytics.Plausible.csproj
   ```
2. `dotnet run` the site, log in to `/umbraco`, and you'll see the **Tracking** tab in Content and the
   **Page tracking** block on a document's Info tab. (No file copying — the client ships as static web
   assets and loads via the reference.)

**B. Local NuGet feed** (install with `dotnet add package`)

Run `.\test\pack-local.ps1`, or by hand:

```bash
# Pack to a local folder feed and register it as a NuGet source (once)
dotnet pack src/Lait.Umbraco.Analytics.Plausible -c Release -o %USERPROFILE%\LocalNuget
dotnet nuget add source %USERPROFILE%\LocalNuget -n LocalFeed

# Then in any Umbraco 17 site
dotnet add package Lait.Umbraco.Analytics.Plausible
```

> **About the global-packages folder** (`%USERPROFILE%\.nuget\packages`): that's NuGet's *extraction
> cache*, not a publish target — don't copy `.nupkg` files into it. Publish to a folder feed (above);
> NuGet extracts into global-packages automatically on first restore.
>
> **Iterating?** NuGet won't re-extract a version it already cached. Bump `<Version>` in the `.csproj`
> before re-packing, or clear it: `dotnet nuget locals global-packages --clear`.

> Tip: run Umbraco in development mode so the `umbraco-package.json` cache is short-lived (~10s) while
> you iterate on the client files.

## Notes & things to verify against your install

- **Package versions** in the `.csproj` are pinned to `17.0.0`. Bump them to match your exact Umbraco
  17 patch version if a restore complains.
- **Management API base class / route attribute** (`ManagementApiControllerBase`,
  `VersionedApiBackOfficeRoute`) and the **auth policy** (`AuthorizationPolicies.BackOfficeAccess`)
  follow the stable 14–17 pattern. If a namespace moved in your build, that's the spot to adjust.
- The **document Info** integration reads the document key from `UMB_DOCUMENT_WORKSPACE_CONTEXT.unique`
  and resolves it to a URL server-side via `IPublishedUrlProvider`. If the workspace context API
  differs in your version, `page-tracking-info.js` is the only place to touch.
- This package only **reads** analytics. Your site still needs the Plausible **tracking script** in its
  `<head>` for data to exist.

## License

MIT — see [LICENSE](LICENSE).
