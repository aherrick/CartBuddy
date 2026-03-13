# CartBuddy

CartBuddy helps build a grocery cart from a plain-text shopping list using Kroger search and checkout APIs.

## Apps

- `CartBuddy` - .NET MAUI mobile app
- `CartBuddy.Client` - Blazor WebAssembly web app
- `CartBuddy.Server` - Minimal API backend and OAuth callback host
- `CartBuddy.Data` - Kroger API client and mapping layer
- `CartBuddy.Shared` - Shared DTOs used by client/server/MAUI

## Core Features

- Search nearby stores by ZIP code
- Search products by list terms
- Add matched products to a local cart
- Start Kroger OAuth checkout and push cart lines to Kroger
- Optional AI list cleanup before searching
- API payload logging for debugging

## Run

1. Configure Kroger and Azure OpenAI secrets for `CartBuddy.Server`.
2. Run `CartBuddy.Server`.
3. Run either `CartBuddy` (MAUI) or `CartBuddy.Client` (web).

## Solution

`CartBuddy.slnx` includes all projects above.
