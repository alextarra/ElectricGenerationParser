# Story 3.0: End-to-End CLI Verification & Pipeline Implementation

Status: done

## Story

As a Developer,
I want to wire up the `Application` class to execute the full pipeline,
So that the CLI successfully reads a file, processes it, and outputs the report.

## Acceptance Criteria

1.  **Given** the `Application.Run(path)` method, **When** executed, **Then** it should invoke `ICsvParserService` to get records.
2.  **Given** parsed records, **When** passed to `IReportGenerator`, **Then** it should receive a `ReportModel`.
3.  **Given** a report model, **When** passed to `IConsoleService`, **Then** it should display the summary table.
4.  **Given** the full pipeline, **When** run with a valid CSV, **Then** it should exit with code 0.
5.  **Given** an invalid file, **When** run, **Then** it should catch the exception, print the error, and exit with code 1.

## Tasks / Subtasks

- [ ] Task 1: Refactor ReportGenerator to Core
  - [ ] Move `ReportGenerator.cs` to `src/ElectricGenerationParser.Core/Services/`.
  - [ ] Update namespace to `ElectricGenerationParser.Core.Services`.
  - [ ] Update `ServiceCollectionExtensions` to register `IReportGenerator`.
- [ ] Task 2: Refactor CsvParser to Support Streams
  - [ ] Update `ICsvParserService` to include `Parse(Stream source)`.
  - [ ] Implement `Parse(Stream)` in `CsvParserService`.
  - [ ] Keep `Parse(string path)` as a convenience wrapper (or move to CLI layer if strict).
- [ ] Task 3: Implement Application Logic (AC: 1, 2, 3)
  - [ ] Update `Application.cs` to use Core services.
  - [ ] Implement `Run(string filePath)` using `File.OpenRead` then `parser.Parse(stream)`.
  - [ ] `var report = generator.Generate(records)`
  - [ ] `consoleService.RenderReport(report)`
- [ ] Task 4: Verify Error Handling (AC: 5)
  - [ ] Ensure `Program.cs` global try/catch wraps `app.Run`.
- [ ] Task 5: Manual Verification (AC: 4)
  - [ ] Run the tool against `test-data.csv` (need to create/locate one).
  - [ ] Verify output matches expectation.

## Dev Notes
- `Application.cs` is currently a shell or singleton registered in `Program.cs`.
- Ensure `CsvParserService` is using the `Core` logic correctly.

## Dev Agent Record
