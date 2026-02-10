# Test Coverage Analysis

## Current State

The project has **2 test projects** covering **2 of 6 source projects**, with **~11 test classes** and approximately **68 test cases** across **~1,684 lines of test code**.

### What IS Tested

| Test Project | Covers | Test Files | Focus |
|---|---|---|---|
| `GameCompanion.Core.Tests` | `GameCompanion.Core` | 3 files | `Result<T>`, `ValidationResult` |
| `GameCompanion.Module.StarRupture.Tests` | `GameCompanion.Module.StarRupture` | 8 files | Badges, CataclysmTimer, MapData, ProductionData, ProgressionAnalyzer, ResearchTree, SaveHealth, SessionTracking |

### What is NOT Tested (4 Entire Projects)

| Project | Type | Testability |
|---|---|---|
| `GameCompanion.Engine.Tasks` | Models + interfaces | High — pure data models and interface contracts |
| `GameCompanion.Engine.SaveSafety` | `SaveGuard` service | High — injectable dependencies, no WPF coupling |
| `GameCompanion.Engine.UI` | ViewModels + converters | Medium — ViewModels are testable; WPF converters less so |
| `ArcadiaTracker.App` | Application + ViewModels | Low-Medium — WPF-coupled, but ViewModel logic is testable |

---

## Priority 1: High-Value, High-Testability Gaps

These are untested areas with meaningful logic and no framework coupling — the highest ROI improvements.

### 1. SaveGuard (Engine.SaveSafety)

`src/Engine/GameCompanion.Engine.SaveSafety/Services/SaveGuard.cs`

This service governs edit safety for save files. It has clear, well-defined rules that are straightforward to unit test:

- **Backup enforcement by risk level**: LOW risk fields have optional backups; MEDIUM and above require mandatory backups before edits.
- **Field validation rules**: CRITICAL fields should always be rejected. HIGH fields should require advanced mode. MEDIUM/LOW fields should pass validation.
- **Advanced mode gating**: The confirmation code `"I UNDERSTAND THE RISKS"` enables high-risk edits; incorrect codes should fail.
- **Edge cases**: Unknown field IDs, null values, cancellation token handling.

**Suggested test class**: `SaveGuardTests` (~15-20 test cases)

### 2. NotificationService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/NotificationService.cs`

This service contains complex change-detection logic comparing previous and current player progress. It has no external dependencies beyond file I/O (which can be abstracted):

- **Phase transition detection**: When `CurrentPhase` changes between states.
- **Milestone thresholds**: Blueprint milestones at 10, 25, 50, 100; playtime milestones at 1, 5, 10, 25, 50, 100 hours.
- **Corporation level-up detection**: Comparing levels across corporation lists.
- **Badge earning detection**: HashSet-based O(1) lookup for new badge IDs.
- **Cataclysm urgency mapping**: Converting `CataclysmUrgency` to `NotificationSeverity`.
- **Edge cases**: Null previous progress (first load), identical progress (no changes), empty corporation lists.

**Suggested test class**: `NotificationServiceTests` (~20-25 test cases)

### 3. PlayStatisticsService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/PlayStatisticsService.cs`

This service aggregates statistics with injectable dependencies (`ProgressionAnalyzerService`, `SessionTrackingService`):

- **Building state aggregation**: Counting operational, disabled, and malfunctioning buildings from save data.
- **Session duration estimation**: 30-minute gap threshold for identifying separate play sessions.
- **Average session length calculation**: TimeSpan arithmetic across snapshot history.
- **Edge cases**: Zero buildings, zero snapshots, single snapshot, no session history.

**Suggested test class**: `PlayStatisticsServiceTests` (~12-15 test cases)

### 4. WikiCacheService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/WikiCacheService.cs`

All lookup logic uses case-insensitive matching and cross-entity search — purely in-memory operations with a mockable `WikiDataService` dependency:

- **Case-insensitive item/building/blueprint lookup**: By both Name and ID.
- **Cross-entity search**: Searching items, buildings, blueprints, and corporations simultaneously.
- **Tooltip text generation**: Formatted multi-line text from entity data.
- **Not-found handling**: Returning null for missing entities.
- **Lazy initialization**: First query triggers cache load.

**Suggested test class**: `WikiCacheServiceTests` (~15-18 test cases)

---

## Priority 2: Important Logic With Higher Test Complexity

These areas have significant logic but require more setup (mock HTTP, file system abstractions, etc.).

### 5. SaveParserService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/SaveParserService.cs`

This is the most complex service in the codebase (~high complexity) and completely untested. It handles binary decompression, JSON parsing, regex matching, and entity classification:

- **Zlib decompression**: 4-byte header read, magic byte validation (0x78 0x9C), DeflateStream decompression.
- **Timestamp parsing**: `"yyyyMMddHHmmss"` format with edge cases.
- **Path extraction**: Removing UE4 prefixes (`CR_`, `I_`) from asset paths.
- **Building state regex**: `CrBuildingStateFragment(bInitialized=X, bDisabled=Y, MalfunctionFlags=Z)`.
- **Entity classification**: PathCategoryMap-based categorization (Hub, Structure, Power, Production, Storage, etc.).
- **Deeply nested JSON traversal**: Player position, entities, base cores, electricity networks.

**Why this matters**: A bug in the parser silently produces wrong data for every downstream service. Consider integration tests with sample `.sav` file fixtures.

**Suggested test class**: `SaveParserServiceTests` (~20-25 test cases, likely needs test fixture files)

### 6. SaveSharingService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/SaveSharingService.cs`

Handles `.arcadia` package creation (zip archives with metadata):

- **Export round-trip**: Create package → inspect → import should preserve data.
- **Safe filename generation**: Stripping invalid characters from session names.
- **Metadata inclusion**: Zip entry contains correct save data and metadata.json.
- **Import validation**: Package must contain expected entries.
- **Edge cases**: Missing metadata, corrupt zip, paths with special characters.

**Suggested test class**: `SaveSharingServiceTests` (~10-12 test cases, integration-style with temp directories)

### 7. ExportService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/ExportService.cs`

Exports data to CSV and Excel — multiple injected dependencies and file I/O:

- **Format routing**: CSV vs Excel path selection.
- **Multi-sheet Excel generation**: Separate worksheets per data type.
- **Dynamic record mapping**: Correct column names and values for each export type.
- **Edge cases**: Empty data sets, missing optional data, directory creation.

**Suggested test class**: `ExportServiceTests` (~10-15 test cases, integration-style with temp files)

### 8. SteamAchievementService (Module.StarRupture)

`src/Modules/GameCompanion.Module.StarRupture/Services/SteamAchievementService.cs`

The graceful degradation pattern is the key thing to test:

- **No API key provided**: Should still return local badge data without Steam data.
- **API call failure**: Should degrade gracefully and return partial results.
- **Achievement mismatch detection**: Local badges vs Steam unlock status.
- **Unix timestamp conversion**: Steam unlock time to DateTime.
- **Global percentage parsing**: Extracting achievement rarity from API response.

**Suggested test class**: `SteamAchievementServiceTests` (~10-12 test cases, requires HttpClient mocking via `IHttpClientFactory` or similar)

---

## Priority 3: Model and Computed Property Coverage

Several models contain computed properties with division-by-zero guards and formatting logic. These are quick wins.

### 9. Model Computed Properties

These are small, fast tests that verify important edge-case behavior:

| Model | Property | Key Edge Case |
|---|---|---|
| `ProductionSummary` | `EfficiencyPercent` | Division by zero when `TotalMachines == 0` |
| `PlayStatistics` | `BuildingEfficiency` | Zero buildings placed |
| `PlayStatistics` | `BlueprintCompletion` | Zero total blueprints |
| `PlayStatistics` | `BadgeCompletion` | Zero total badges |
| `PlayStatistics` | `PlayTimeDisplay` | Time formatting (0h, large values) |
| `PlayerProgress` | `BlueprintProgress` | Zero total blueprints |
| `ResearchTreeData` | `UnlockPercent` | Zero total recipes |
| `CataclysmState` | `TimeRemainingDisplay` | < 1 minute, exactly 0 |
| `ExportResult` | `DisplaySize` | Bytes, KB, MB thresholds |
| `SteamAchievementStatus` | `HasMismatch` | Six-way nullable bool pattern |
| `SteamAchievementStatus` | `StatusDisplay` | All six display states |
| `AppNotification` | `Icon` | All 8 notification types + unknown fallback |
| `NotificationHistory` | `UnreadCount` | Empty list, all read, all unread |
| `MapBounds` | `Width/Height/Center` | Zero-size bounds, negative coordinates |

**Suggested test class**: `ModelComputedPropertyTests` (~25-30 small test cases)

### 10. StarRuptureProgressionMap (Progression)

`src/Modules/GameCompanion.Module.StarRupture/Progression/StarRuptureProgressionMap.cs`

Contains the core progression logic:

- **GetCurrentPhase()**: Finding the first phase with incomplete steps.
- **GetAvailableSteps()**: Filtering by prerequisite completion.
- **GetProgressPercentage()**: Completion ratio with zero-step guard.
- **Phase definition integrity**: All 4 phases defined with correct step counts and ordering.

**Suggested test class**: `StarRuptureProgressionMapTests` (~12-15 test cases)

---

## Priority 4: Engine.UI ViewModels

Lower priority because of WPF coupling, but the state management logic in ViewModels is testable:

### 11. StepGuideViewModel (Engine.UI)

- `LoadStep()`: Correctly unpacks step data into observable collections.
- `UpdateProgress()`: Calculates completion percentage from checklist items.
- `ToggleChecklistItemAsync()`: Toggles item, calls orchestrator, reverts on failure.

### 12. ViewModelBase (Engine.UI)

- `SetStatus()` / `SetError()` / `ClearMessages()`: State toggling behavior (setting status clears error and vice versa).

---

## Summary: Recommended Test Implementation Order

| # | Area | New Test Cases | Effort | Impact |
|---|---|---|---|---|
| 1 | SaveGuard | ~18 | Low | High — protects save file safety invariants |
| 2 | NotificationService | ~22 | Low-Medium | High — complex change detection logic |
| 3 | Model computed properties | ~28 | Low | Medium — catches division-by-zero and formatting bugs |
| 4 | PlayStatisticsService | ~14 | Low-Medium | Medium — aggregation correctness |
| 5 | WikiCacheService | ~16 | Low | Medium — lookup and search correctness |
| 6 | StarRuptureProgressionMap | ~14 | Low | Medium — progression advancement logic |
| 7 | SaveParserService | ~22 | High | Critical — all downstream services depend on correct parsing |
| 8 | SaveSharingService | ~11 | Medium | Medium — data integrity during export/import |
| 9 | ExportService | ~12 | Medium | Low-Medium — output format correctness |
| 10 | SteamAchievementService | ~11 | Medium | Low — graceful degradation patterns |
| 11 | StepGuideViewModel | ~10 | Medium | Low — UI state management |

Total: ~178 new test cases across 11 areas, roughly tripling the current test count.

---

## Structural Recommendations

1. **Add test projects for Engine assemblies**: Create `GameCompanion.Engine.SaveSafety.Tests` and optionally `GameCompanion.Engine.Tasks.Tests`. The SaveGuard service is the highest-value untested code that is also the easiest to test.

2. **Introduce interface abstractions for I/O**: `NotificationService`, `WikiDataService`, and `SaveDiscoveryService` all have direct file system and registry access baked in. Extracting `IFileSystem` or similar abstractions would make these services unit-testable without touching disk.

3. **Make HttpClient injectable**: `SteamAchievementService` and `WikiDataService` use static `HttpClient` instances. Switching to `IHttpClientFactory` would enable testing API integration logic without network calls.

4. **Add test fixture data**: The `SaveParserService` would benefit most from sample `.sav` files committed to the test project as embedded resources. This enables regression testing against known save formats.

5. **Consider code coverage tooling**: Adding `coverlet.collector` to the test projects and configuring `dotnet test --collect:"XPlat Code Coverage"` would provide concrete coverage metrics to track improvement over time.
