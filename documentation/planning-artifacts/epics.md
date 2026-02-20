---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - documentation/planning-artifacts/prd.md
  - documentation/planning-artifacts/architecture.md
project_name: 'ElectricGenerationParser'
user_name: 'Sas'
date: '2026-02-19'
---

# ElectricGenerationParser - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for ElectricGenerationParser, decomposing the requirements from the PRD and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

*   **FR-01**: CLI - Accept file path argument.
*   **FR-02**: CLI - Validate input file existence.
*   **FR-03**: Ingestion - Parse standard CSV files (skip metadata).
*   **FR-04**: Ingestion - Map columns to internal models.
*   **FR-W1**: Web - Accessible via browser.
*   **FR-W2**: Web - Upload CSV file via form.
*   **FR-W3**: Web - Process file using Core Logic.
*   **FR-W4**: Web - Render Summary HTML Table.
*   **FR-W5**: Web - Display validation errors.
*   **FR-05**: Logic - Weekday vs Weekend determination.
*   **FR-06**: Logic - Holiday identification.
*   **FR-07**: Logic - Holiday Observation logic.
*   **FR-08**: Logic - On-Peak vs Off-Peak categorization.
*   **FR-09**: Logic - Aggregate data into buckets.
*   **FR-10**: Logic - Strict Checksum Validation.
*   **FR-11**: Logic - Throw exceptions on validation failure.
*   **FR-12**: CLI - Output summary table to Console.
*   **FR-13**: Web - Output summary table to HTML.
*   **FR-14**: Output - Include On-Peak totals.
*   **FR-15**: Output - Include Off-Peak totals.
*   **FR-16**: Output - Include Grand Totals.
*   **FR-17**: Config - Load `appsettings.json` per app.
*   **FR-18**: Config - Configurable Peak Hours.

### NonFunctional Requirements

*   **NFR-01**: Performance - Process 3000 rows < 1 sec.
*   **NFR-02**: Startup - Cold start < 2 sec.
*   **NFR-03**: Maintainability - Clean C# conventions.
*   **NFR-04**: Portability - Windows/Linux compatible (Path.Combine).

### Additional Requirements (from Architecture)

*   **AR-01**: Structure - Multi-project solution (Core, Cli, Web).
*   **AR-02**: Pattern - Strategy Pattern for Rate Logic.
*   **AR-03**: Pattern - Shared `AddElectricGenerationCore` DI extension.
*   **AR-04**: Config - Strict Options Validation.
*   **AR-05**: Web - In-Memory processing (no disk storage).
*   **AR-06**: output - Use `Spectre.Console` for CLI.

### FR Coverage Map

| Epic | Included Requirements |
| :--- | :--- |
| **Epic 1: The Core Brain** | FR-03, FR-04, FR-05, FR-06, FR-07, FR-08, FR-09, FR-10, FR-11, FR-17, FR-18, AR-01, AR-02, AR-03, AR-04, NFR-03 |
| **Epic 2: The CLI Experience** | FR-01, FR-02, FR-12, FR-14, FR-15, FR-16, AR-06, NFR-04 |
| **Epic 3: The Web Interface** | FR-W1, FR-W2, FR-W3, FR-W4, FR-W5, FR-13, AR-05, NFR-01, NFR-02 |

## Epic List

1.  **Epic 1: The Core Brain (Shared Logic Library)**
2.  **Epic 2: The CLI Experience (Console App)**
3.  **Epic 3: The Web Interface (Zero-Install Access)**

### Story 1.1: Create Project Structure & Domain Models

**As a** Developer,
**I want** to initialize the Multi-Project Solution with the Shared Core Library and Domain Models,
**So that** I have a type-safe foundation for implementing the business logic.

**Acceptance Criteria:**

*   **Given** a clean workspace, **When** I initialize the solution, **Then** `ElectricGenerationParser.sln` should exist with `ElectricGenerationParser.Core` (Class Library) and `ElectricGenerationParser.Tests` (xUnit).
*   **Given** the CSV specification, **When** I create `GenerationRecord.cs`, **Then** it should have properties matching `Date/Time`, `Energy Produced`, `Energy Consumed`, `Exported`, `Imported` with correct data types (DateTime, int/double).
*   **Given** the output requirements, **When** I create `RatePeriodSummary.cs`, **Then** it should hold aggregated totals for On-Peak, Off-Peak, and Grand Totals.
*   **Given** the shared logic requirement, **When** I create `IServiceCollectionExtensions.cs`, **Then** it should contain a placeholder `AddElectricGenerationCore` method.

### Story 1.2: Implement CSV Ingestion with CsvHelper

**As a** Developer,
**I want** to implement a robust CSV parser using CsvHelper,
**So that** I can reliably ingest the provider's specific file format and handle parsing errors gracefully.

**Acceptance Criteria:**

*   **Given** a valid CSV file, **When** `CsvParserService.ParseStreamAsync(stream)` is called, **Then** it should return a `List<GenerationRecord>` with all rows populated.
*   **Given** the CSV has metadata headers, **When** parsing, **Then** the service should correctly skip preamble lines to find the header row.
*   **Given** a malformed CSV (missing columns), **When** parsing, **Then** it should throw a custom `CsvValidationException`.
*   **Given** the `GenerationRecordMap`, **When** configured, **Then** it should use loose mapping for headers (case-insensitive) but strict validation for missing required columns.

### Story 1.3: Implement Time-of-Use Strategy Pattern

**As a** Developer,
**I want** to implement the `IRateStrategy` pattern for Weekday vs Weekend logic,
**So that** I can easily extensible rules for determining On-Peak vs Off-Peak periods.

**Acceptance Criteria:**

*   **Given** a date is Saturday or Sunday, **When** `WeekendStrategy` is evaluated, **Then** it should always return `RateType.OffPeak`.
*   **Given** a date is Monday-Friday, **When** `WeekdayStrategy` is evaluated, **Then** it should check the hour against configured `PeakStart` (e.g., 07:00) and `PeakEnd` (e.g., 19:00).
*   **Given** `appsettings.json`, **When** `PeakHoursSettings` are loaded, **Then** the strategy should use those dynamic values, not hardcoded hours.

### Story 1.4: Implement Holiday Configuration Service

**As a** User,
**I want** to define holidays in `appsettings.json` (Fixed, Floating, Observed),
**So that** the system correctly identifies holidays as Off-Peak without code changes.

**Acceptance Criteria:**

*   **Given** `appsettings.json` with Fixed Holidays (e.g., Dec 25), **When** `HolidayService` initializes, **Then** it should calculate the exact date for the current year.
*   **Given** a Floating Holiday (e.g., Memorial Day = Last Monday in May), **When** initialized, **Then** it should calculate the correct date for the year.
*   **Given** `ObserveWeekend: true`, **When** a holiday falls on Sunday, **Then** the following Monday should be added to the holiday list.
*   **Given** `HolidayStrategy`, **When** a date matches a calculated holiday, **Then** it should return `RateType.OffPeak` regardless of the time of day.

### Story 1.5: Data Aggregation & Checksum Validation

**As a** User,
**I want** the system to aggregate totals and strictly validate checksums,
**So that** I can trust the financial data is 100% accurate and no energy is unaccounted for.

**Acceptance Criteria:**

*   **Given** a list of classified records, **When** `ReportGenerator` runs, **Then** it should produce a `RatePeriodSummary` with correct sums for On-Peak, Off-Peak, and Total.
*   **Given** the aggregation is complete, **When** `ValidatorService` runs, **Then** it must verify: `Total Produced == Sum(OnPeak Produced + OffPeak Produced)`.
*   **Given** a discrepancy (e.g., rounding error > 1 unit), **When** validating, **Then** it should throw a `ValidationException` stopping the process.


**Goal:** Restore the original "Month End" workflow by building a robust Command Line Interface (CLI) that consumes the Core Library. It focuses on file system interactions, user feedback via Spectre.Console, and local configuration management.

### Story 2.1: Implement CLI Arguments & Configuration Loading

**As a** Developer,
**I want** to set up the CLI project to accept file path arguments and load local configuration,
**So that** the application knows what file to process and which rules to apply.

**Acceptance Criteria:**

*   **Given** the CLI application, **When** run with `--help`, **Then** it should display usage instructions.
*   **Given** a valid file path argument, **When** the application starts, **Then** it should validate the file exists before proceeding.
*   **Given** `appsettings.json` in the execution directory, **When** the app starts, **Then** it should successfully load `HolidaySettings` and `PeakHoursSettings` into the DI container using `AddElectricGenerationCore`.
*   **Given** missing configuration, **When** the app starts, **Then** it should exit immediately with a clear configuration error.

### Story 2.2: CSV Ingestion Integration

**As a** User,
**I want** the CLI to read the specified CSV file using the Core Parser,
**So that** the raw data is loaded into memory for processing.

**Acceptance Criteria:**

*   **Given** a valid CSV file path, **When** the command executes, **Then** it should stream the file content to `ICsvParserService`.
*   **Given** a large file, **When** parsing, **Then** it should show a spinner or progress indicator (via Spectre.Console).
*   **Given** an invalid file (wrong format), **When** parsing, **Then** the CLI should catch the `CsvValidationException` and print the error message (e.g., "Missing column 'Energy Produced'").

### Story 2.3: Data Aggregation & Checksum Validation

**As a** User,
**I want** the system to process the data and validate the results,
**So that** I know the numbers remain accurate before seeing the report.

**Acceptance Criteria:**

*   **Given** parsed records, **When** passed to `IReportGenerator`, **Then** the CLI should receive a `RatePeriodSummary` object.
*   **Given** a validation failure in the Core logic, **When** processing, **Then** the CLI should catch `ValidationException` and display "Data Integrity Error: Totals do not match."
*   **Given** successful processing, **When** complete, **Then** it should proceed to the reporting step.

### Story 2.4: Console Output with Spectre.Console

**As a** User,
**I want** to see a formatted table of the results in my terminal,
**So that** I can easily read the On-Peak vs Off-Peak totals.

**Acceptance Criteria:**

*   **Given** a valid `RatePeriodSummary`, **When** the process completes, **Then** it should render a `Spectre.Console.Table` with columns: Period (On/Off), Energy Produced, Consumed, Import, Export.
*   **Given** the table is rendered, **When** specific rows are totals, **Then** they should be highlighted or formatted distinctly.
*   **Given** the Grand Total row, **When** rendered, **Then** it must match the sum of On+Off rows visually.

## Epic 3: The Web Interface (Zero-Install Access)

### Story 3.1: Web Application Setup & Upload Form

**As a** User,
**I want** to access the application via a web browser and see a file upload form,
**So that** I can process my generation report without installing any software.

**Acceptance Criteria:**

*   **Given** a browser, **When** I navigate to the homepage, **Then** I should see a simple form with a file upload input (`.csv`) and a "Process Report" button.
*   **Given** the Razor Pages project, **When** initialized, **Then** it should register `AddElectricGenerationCore` services just like the CLI.
*   **Given** `appsettings.json`, **When** the web app starts, **Then** it should load `HolidaySettings` and `PeakHoursSettings` correctly.

### Story 3.2: In-Memory File Processing

**As a** Developer,
**I want** to process the uploaded file directly from memory streams,
**So that** I avoid the complexity and latency of saving temporary files to disk.

**Acceptance Criteria:**

*   **Given** a valid file upload (`IFormFile`), **When** the form is submitted, **Then** the stream should be passed directly to `ICsvParserService`.
*   **Given** a large file, **When** parsing, **Then** it should handle the stream efficiently within the request lifecycle.
*   **Given** an invalid file type (e.g., .txt or .exe), **When** submitted, **Then** the controller should reject it before parsing.

### Story 3.3: HTML Report Rendering

**As a** User,
**I want** to see the calculation results as a formatted HTML table,
**So that** I can copy the values directly into my spreadsheets.

**Acceptance Criteria:**

*   **Given** successful processing, **When** the page reloads, **Then** it should display a `<table>` with the same columns as the CLI (Period, Produced, Consumed, Import, Export).
*   **Given** the `RatePeriodSummary` model, **When** rendering with Razor, **Then** it should iterate over the On-Peak and Off-Peak totals and display Grand Totals at the bottom.
*   **Given** the table is displayed, **When** reviewing, **Then** numeric values should be formatted for readability.

### Story 3.4: Web Error Handling

**As a** User,
**I want** friendly error messages if my file is invalid,
**So that** I know what went wrong without seeing technical crash screens.

**Acceptance Criteria:**

*   **Given** a `CsvValidationException` (bad format), **When** caught in the PageModel, **Then** it should add a `ModelState` error and re-display the form with a clear message (e.g., "Invalid File Format").
*   **Given** a `ValidationException` (checksum error), **When** caught, **Then** it should show "Data Integrity Error" prominently.
*   **Given** any other unhandled exception, **When** caught, **Then** it should show a generic "Processing Failed" message and log the details internally.

### Story 3.5: Docker Deployment Support

**As a** DevOps Engineer,
**I want** a `Dockerfile` for the Web Application,
**So that** I can easily deploy the service to a containerized environment.

**Acceptance Criteria:**

*   **Given** the solution root, **When** `docker build` is run, **Then** it should produce a minimal ASP.NET Core image containing the published Web App.
*   **Given** environment variables, **When** the container starts, **Then** it should respect overridden `PeakHoursSettings` or `HolidaySettings` configuration.
