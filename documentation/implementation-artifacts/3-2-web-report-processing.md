# Story 3.2: Implement Web Reporting Logic

Status: done

## Story

As a User,
I want the "Process Report" button to actually run the calculation and display the results,
So that I can see my generation numbers on the web page.

## Acceptance Criteria

1.  **Given** a valid CSV upload, **When** posted, **Then** the controller should stream the file to `ICsvParserService`.
2.  **Given** parsed records, **When** processed by `IReportGenerator`, **Then** a `ReportModel` is generated.
3.  **Given** the report model, **Then** the page should render a read-only table matching the Console output (Period, Prod, Cons, Export, Import).
4.  **Given** the Grand Total row, **Then** it should be highlighted.
5.  **Given** an error (Validation/Parsing), **Then** the page should re-display the form with the error message in `Asp-Validation-Summary`.

## Tasks / Subtasks

- [x] Task 1: Implement Post Logic (AC: 1, 2)
  - [x] Update `Index.cshtml.cs`:
    - [x] `using var stream = Upload.OpenReadStream()`
    - [x] `var records = _parser.Parse(stream)`
    - [x] `Report = _generator.GenerateReport(records)`
    - [x] Return Page (with Report property populated).
- [x] Task 2: Implement Report View (AC: 3, 4)
  - [x] Add `public ReportModel Report { get; set; }` to `IndexModel`.
  - [x] Update `Index.cshtml`:
    - [x] Check `if (Model.Report != null)`.
    - [x] Render HTML table with Bootstrap classes (`table table-striped`).
    - [x] Format numbers using `N0` or `N2`.
- [x] Task 3: Error Handling (AC: 5)
  - [x] Ensure `try/catch` block catches `InvalidDataException` and `ValidationException`.
  - [x] Add exception message to ModelState.

## Dev Notes
- Remember `Record` parsing uses `Stream`.
- View should handle both "Initial State" (Form) and "Result State" (Report + Back Button?).
- Maybe keep the form visible or Replace it? PRD implies "Access via browser... process report". A simple "Results below" is fine.

## Dev Agent Record

### File List
- src/ElectricGenerationParser.Web/Pages/Index.cshtml
- src/ElectricGenerationParser.Web/Pages/Index.cshtml.cs
- test/ElectricGenerationParser.Tests/ElectricGenerationParser.Tests.csproj
- test/ElectricGenerationParser.Tests/Web/IndexModelTests.cs

### Change Log
- 2026-02-19: Implemented in-memory CSV upload processing, period-based HTML table rendering with highlighted Grand Total row, and web-friendly error handling; added unit tests for `IndexModel`.

## Senior Developer Review (AI)

Reviewer: Sas on 2026-02-19

- Fixed AC3/AC4 by switching to a Period table (On-Peak/Off-Peak/Grand Total) and styling the Grand Total row.
- Fixed Task 3 discrepancy by catching `InvalidDataException` and `ValidationException` explicitly.
- Added server-side file extension validation (`.csv`) and avoided leaking raw exception details for unknown failures.
- Added unit tests for `IndexModel` and verified `dotnet test` passes.
