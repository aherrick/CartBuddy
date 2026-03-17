# CartBuddy

CartBuddy turns a plain-text shopping list into a Kroger cart.

It automatically runs each list item through AI cleanup before searching, normalizes the wording into a short search term, and categorizes the item into buckets like produce, dairy, meat, or pantry.

## Why that matters

Kroger search can be inconsistent with raw grocery-list wording. A simple produce example:

- `jalapeno` may miss the best produce match
- `produce jalapeno` often works better

CartBuddy handles that for you. For produce items, the server searches both the cleaned term and a `produce {term}` variant, then merges the results. This works around Kroger API search gaps without making the user write store-specific search phrases.

## Projects

- `CartBuddy` - .NET MAUI mobile app
- `CartBuddy.Client` - Blazor WebAssembly web app
- `CartBuddy.Server` - minimal API backend and OAuth callback host
- `CartBuddy.Data` - Kroger API client and mapping layer
- `CartBuddy.Shared` - shared DTOs used across the apps
- `CartBuddy.Console` - small console stub

## Current behavior

- Search nearby stores by ZIP code
- Automatically clean up and categorize list items before search
- Search Kroger products for the selected store
- Add matched products to a local cart
- Start Kroger OAuth checkout and push cart lines to Kroger
- Capture API payload logs for debugging

## Run

1. Configure Kroger and Azure OpenAI secrets for `CartBuddy.Server`.
2. Run `CartBuddy.Server`.
3. Run either `CartBuddy` (MAUI) or `CartBuddy.Client` (web).

## Solution

`CartBuddy.slnx` includes all projects above.
