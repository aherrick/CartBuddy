# CartBuddy 🛒✨

CartBuddy is a tiny Blazor app that helps you:

- Find nearby Kroger stores by ZIP
- Search products
- Build a cart
- Kick off checkout (OAuth) and send items to your Kroger cart

It’s meant to be simple, fast, and a little fun.

---

## What’s in here

- `CartBuddy.Client` — Blazor WebAssembly UI
- `CartBuddy.Server` — Minimal API + Kroger OAuth/cart calls
- `CartBuddy.Shared` — Shared models

---

## Quick start (local dev)

### 1) Prereqs

- .NET SDK (the repo targets .NET `net10.0`)

### 2) Set your Kroger API secrets (User Secrets)

Secrets are not stored in `appsettings.json` anymore.

From the repo root:

```powershell
cd .\CartBuddy.Server

dotnet user-secrets set "Kroger:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Kroger:ClientSecret" "YOUR_CLIENT_SECRET"
```

Optional sanity check:

```powershell
dotnet user-secrets list
```

### 3) Run it

```powershell
cd ..

dotnet run --project .\CartBuddy.Server\CartBuddy.Server.csproj
```

Then open the URL shown in the console (typically `https://localhost:7xxx`).

---

## Notes / gotchas

- **Client uses SweetAlert2** for friendly popups. If you ever see a console error about `CurrieTechnologies.Razor.SweetAlert2.*` missing, make sure the static assets are referenced in `CartBuddy.Client/wwwroot/index.html`.
- **Production secrets**: use environment variables / your secret store of choice. User Secrets are intended for local development.

---

## Roadmap ideas (aka “things future-you will thank you for”)

- Add a real shopping list import/export
- Better cart UX (quantities, remove, totals)
- Friendly errors when Kroger creds are missing

PRs welcome. Bugs… also welcome (they make the app stronger).