# Story 1.4: Refactor to Core Library

Status: done

## Story

As an Architect,
I want to extract the business logic into a `ElectricGenerationParser.Core` library,
So that it can be shared between the CLI and the future Web Interface (Epic 3).

## Acceptance Criteria

1.  **Given** the current solution, **When** I refactor, **Then** a new project `src/ElectricGenerationParser.Core` (Class Library) exists.
2.  **Given** the existing logic, **When** moved, **Then** `Models`, `Services` (except Console/CLI specific), and `Interfaces` are in `Core`.
3.  **Given** the `ElectricGenerationParser` console app, **When** updated, **Then** it references `ElectricGenerationParser.Core`.
4.  **Given** the `ElectricGenerationParser.Tests`, **When** updated, **Then** it references `Core` and tests pass.
5.  **Given** the requirement `AR-03`, **When** implemented, **Then** `Core` includes an `IServiceCollectionExtension` with `AddElectricGenerationCore`.

## Tasks / Subtasks

- [x] Task 1: Create Core Project
    - [x] Create `src/ElectricGenerationParser.Core` (Class Lib).
    - [x] Add to solution.
- [x] Task 2: Migrate specialized Logic to Core
    - [x] Move `Models` folder to Core.
    - [x] Move `Interfaces` (IHolidayService, IRateStrategy, etc) to Core.
    - [x] Move `Services` (HolidayService, RateCalculator, CsvParserService) to Core.
    - [x] Ensure namespaces are updated to `ElectricGenerationParser.Core.*`.
- [x] Task 3: Implement Dependency Injection Extension
    - [x] Create `ServiceCollectionExtensions.cs` in Core.
    - [x] Implement `AddElectricGenerationCore(this IServiceCollection services, IConfiguration config)`.
    - [x] Register `HolidayService`, `RateCalculator`, `CsvParserService` inside the extension.
- [x] Task 4: Update Console App (CLI)
    - [x] Add reference to `Core`.
    - [x] Remove moved files from Console App.
    - [x] Update `Program.cs` to use `AddElectricGenerationCore`.
    - [x] Update namespaces in `Program.cs`.
- [x] Task 5: Update Tests
    - [x] Add reference to `Core`.
    - [x] Update namespaces in tests.
    - [x] Verify all tests pass.

## Dev Notes
- Be careful with `CsvHelper` dependency - it might need to move to core if `CsvParserService` moves.
- `ConsoleService` and `Spectre.Console` logic should STAY in the Console App (or move to a new `Cli` project if we rename, but for now keeping it in the entry project is fine as long as logic is out).
- Verify `appsettings.json` behavior.

## Dev Agent Record
### Agent Model Used
Gemini 3 Pro (Preview)

### Completion Notes
Refactoring complete.
- Created `ElectricGenerationParser.Core`.
- Moved all domain logic.
- Updated namespaces and references.
- Verified with 30/30 passing tests.
- `Program.cs` now properly uses DI extension method `AddElectricGenerationCore`.
