# Arcadia Tracker

An unofficial companion app for [StarRupture](https://store.steampowered.com/app/1631270/StarRupture/) -- track your progression, explore your world map, analyze saves, and optimize your gameplay.

> **Disclaimer:** This is a fan-made project. It is not affiliated with, endorsed by, or sponsored by the developers of StarRupture. See [DISCLAIMER.md](DISCLAIMER.md) for full details.

## Features

- **Dashboard** -- Overview of playtime, game phase, data points, corporation levels, and earned badges
- **Progression Tracker** -- Visual timeline of Early Game, Mid Game, End Game, and Mastery phases with percentage breakdowns
- **World Map** -- Native 2D map rendered from save data showing bases, connections, player position, and building status
- **Interactive Map** -- Embedded WebView2 browser for starrupture.tools with built-in ad blocking
- **Production Tracker** -- Machine efficiency monitoring, power grid analysis, and base-to-base comparison
- **Research Tree** -- Tech tree visualization with wiki-enriched blueprint data and category filtering
- **Session History** -- Play session snapshots with progression trends over time
- **Play Statistics** -- Comprehensive gameplay metrics including playtime, efficiency, and progress breakdowns
- **Achievements** -- Local badge tracking with Steam API integration and graceful fallback
- **Notifications** -- In-app alert system with severity-based filtering (Info, Success, Warning, Critical)
- **Data Export** -- Export game stats to CSV and Excel formats
- **Roadmap** -- Personalized recommendations based on your current progress
- **Settings** -- Configure auto-refresh, notification preferences, and save file locations

## Installation

### Prerequisites

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (Desktop Runtime)
- WebView2 Runtime (usually pre-installed on Windows 10/11)

### Building from Source

```bash
git clone https://github.com/GrizzwaldHouse/GameCompanionPlatform.git
cd GameCompanionPlatform
dotnet build GameCompanionPlatform.sln
dotnet run --project src/Apps/ArcadiaTracker.App/ArcadiaTracker.App/ArcadiaTracker.App.csproj
```

### Windows SmartScreen Warning

When running the app for the first time, Windows SmartScreen may show a warning because the executable is not code-signed. Click **"More info"** then **"Run anyway"** to proceed. This is normal for unsigned applications.

## Architecture

Arcadia Tracker is built on the GameCompanionPlatform framework, a modular .NET 8 architecture designed to support companion apps for multiple games.

```
GameCompanionPlatform/
  src/
    Core/           -- Shared interfaces, Result<T>, IGameModule
    Engine/         -- Reusable services (Tasks, SaveSafety, UI)
    Modules/        -- Game-specific implementations
      StarRupture/  -- Save parsing, progression analysis, map data
    Apps/
      ArcadiaTracker.App/  -- WPF desktop application
```

### Key Technologies

- **.NET 8** / C# 12
- **WPF** with custom sci-fi themed UI
- **CommunityToolkit.Mvvm** for MVVM pattern
- **WebView2** for embedded web content
- **Serilog** for structured logging
- **Microsoft.Extensions.DependencyInjection** for IoC

## Testing

```bash
dotnet test GameCompanionPlatform.sln
```

Test coverage includes Result<T> type validation, progression phase detection, badge earning logic, map data clustering, production analysis, session tracking, research tree building, save health checks, and cataclysm timer calculations.

## Save File Locations

Arcadia Tracker automatically detects save files from:

- **Steam Cloud:** `%Steam%/userdata/[ID]/1631270/remote/Saved/SaveGames/`
- **Local:** `%LocalAppData%/StarRupture/Saved/SaveGames/`

The application only **reads** save files -- it never modifies or writes to them.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

Game intellectual property belongs to the respective owners. See [DISCLAIMER.md](DISCLAIMER.md).
