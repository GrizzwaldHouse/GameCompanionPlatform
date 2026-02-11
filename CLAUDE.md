# Arcadia Tracker — Claude Code Project Instructions

## Project Overview
WPF game companion app (.NET 8.0 / C# 12) for Star Rupture. Tracks save data,
provides analytics, and includes a paid feature system with activation codes.

## Architecture
- `src/Core/` — Interfaces, models, Result<T> monad
- `src/Engine/` — Reusable services (Entitlements, SaveSafety, Tasks, UI)
- `src/Modules/` — Game-specific (StarRupture, SaveModifier)
- `src/Apps/ArcadiaTracker.App/` — WPF application (MVVM)
- `tests/` — xunit + FluentAssertions test projects

## Key Conventions
- Use `Result<T>` monad for all domain error handling (no thrown exceptions)
- Use `required init` properties for immutable models
- Use `Unit` type for void success results
- Capabilities are HMAC-SHA256 signed tokens, not boolean flags
- Admin access: separate namespace (`admin.*`), never piggybacks on paid features
- Non-discoverability: premium UI only appears after capability check passes
- Atomic file writes: temp file + File.Move(overwrite: true)

## Build & Test
- No dotnet CLI in this environment — edit .csproj and .sln files manually
- Tests: xunit + FluentAssertions, test projects in `tests/`
- To verify code: review for compile errors, run existing test patterns

## Security Rules
- All crypto uses System.Security.Cryptography (no custom implementations)
- HMAC comparison: always use CryptographicOperations.FixedTimeEquals()
- Encryption: AES-GCM only (not CBC)
- Key derivation: HKDF with domain-separated contexts
- Activation codes: HMAC-verified, one-time use
- Admin tokens: signed, encrypted at rest, time-bound (max 30 days)
- Never store secrets in plaintext

## Important Files
- Solution: `GameCompanionPlatform.sln` (root)
- DI wiring: `src/Apps/ArcadiaTracker.App/ArcadiaTracker.App/App.xaml.cs`
- Main window: `src/Apps/ArcadiaTracker.App/ArcadiaTracker.App/MainWindow.xaml.cs`
- Entitlements: `src/Engine/GameCompanion.Engine.Entitlements/`
- Admin architecture: `docs/ADMIN-RELEASE-ARCHITECTURE.md`
- Admin setup guide: `docs/ADMIN-SETUP-GUIDE.md`
- UX/monetization report: `docs/UX-MONETIZATION-REPORT.md`

## When Adding Features
1. Define capability action in `CapabilityActions` if paid
2. Create view in `Views/` with capability gating
3. Wire into `MainWindow.xaml` (nav item) and `MainWindow.xaml.cs` (routing)
4. Register services in `App.xaml.cs` ConfigureEntitlementServices()
5. Write tests in corresponding test project
6. Keep premium nav items `Visibility="Collapsed"` by default
