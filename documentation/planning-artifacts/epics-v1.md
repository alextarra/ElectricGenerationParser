---
stepsCompleted: [step-01-validate-prerequisites, step-02-design-epics, step-03-create-stories, step-04-final-validation]
inputDocuments:
  - documentation/planning-artifacts/prd.md
  - documentation/planning-artifacts/architecture.md
  - documentation/planning-artifacts/product-brief-ElectricGenerationParser-2026-02-18.md
  - documentation/5919893_custom_report.csv
project_name: ElectricGenerationParser
---

# ElectricGenerationParser - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for ElectricGenerationParser, decomposing the requirements from the PRD and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

*   **FR-01**: The system shall accept a file path as a command-line argument.
*   **FR-02**: The system shall validate that the input file exists and is accessible.
*   **FR-03**: The system shall parse standard CSV files, skipping metadata headers.
*   **FR-04**: The system shall map CSV columns to internal models by header name.
*   **FR-05**: The system shall determine if a timestamp is Weekday vs Weekend.
*   **FR-06**: The system shall identify configured Holidays.
*   **FR-07**: The system shall apply Holiday Observation logic (e.g., weekend holiday rolls to weekday).
*   **FR-08**: The system shall categorize every row as On-Peak or Off-Peak.
*   **FR-09**: The system shall aggregate data into On-Peak and Off-Peak buckets.
*   **FR-10**: The system shall perform a strict checksum validation (Input Sum == Output Sum).
*   **FR-11**: The system shall halt with an error if validation fails.
*   **FR-12**: The system shall output a formatted summary table to Console (stdout).
*   **FR-13**: Output shall include On-Peak totals.
*   **FR-14**: Output shall include Off-Peak totals.
*   **FR-15**: Output shall include Grand Totals.
*   **FR-16**: The system shall load settings from appsettings.json.
*   **FR-17**: PeakHours config shall support start/end hours per day of week.
*   **FR-18**: Output shall include aggregated totals for Weekends.
*   **FR-19**: Output shall include individual breakdowns for each Holiday.

### NonFunctional Requirements

*   **NFR-01 Performance**: Process ~3000 rows in < 1 second.
*   **NFR-02 Startup**: Cold start < 2 seconds.
*   **NFR-03 Maintainability**: Clean C# conventions for solo maintenance.
*   **NFR-04 Portability**: Windows x64 compatible, no local DB required.

### Additional Requirements (from Architecture)

*   **AR-01 Date Strategy**: Use Strategy Pattern (Weekday/Weekend/Holiday) for rate calculation.
*   **AR-02 Config Pattern**: Use IOptions<T> for strong typing; no direct IConfiguration injection.
*   **AR-03 Logging**: Use Microsoft.Extensions.Logging (ILogger).
*   **AR-04 Output**: Use standard Console.WriteLine for table rendering.
*   **AR-05 Validation**: Fail fast on invalid config or CSV headers.
*   **AR-06 CSV**: Strict CSV mapping required.
*   **AR-07 Holidays**: HolidayService must support fixed dates, relative dates (Nth day of month), and observation rules.

### FR Coverage Map

| Requirement | Epic | Story |
| :--- | :--- | :--- |
| FR-01 (Path Arg) | Epic 2 | Story 2.1 |
| FR-02 (File Check) | Epic 2 | Story 2.1 |
| FR-03 (CSV Parse) | Epic 2 | Story 2.2 |
| FR-04 (Map Cols) | Epic 2 | Story 2.2 |
| FR-05 (Weekday) | Epic 1 | Story 1.3 |
| FR-06 (Holidays) | Epic 1 | Story 1.2 |
| FR-07 (Observations) | Epic 1 | Story 1.2 |
| FR-08 (Categorize) | Epic 1 | Story 1.3 |
| FR-09 (Aggregate) | Epic 3 | Story 3.1 |
| FR-10 (Checksum) | Epic 3 | Story 3.1 |
| FR-11 (Halt Fail) | Epic 3 | Story 3.1 |
| FR-12 (Output) | Epic 3 | Story 3.2 |
| FR-13 (On-Peak) | Epic 3 | Story 3.2 |
| FR-14 (Off-Peak) | Epic 3 | Story 3.2 |
| FR-15 (Totals) | Epic 3 | Story 3.2 |
| FR-16 (Config) | Epic 2 | Story 2.1 |
| FR-17 (Peak Hours) | Epic 1 | Story 1.3 |
| NFR-01 (Perf) | All | All |
| NFR-03 (Clean Code) | All | Story 1.1 |
| NFR-04 (Portability) | Epic 4 | Story 4.1 |

## Epic 1: Core Logic Engine

**Goal:** Implement the verified business logic for Time-of-Use rates (Weekdays, Weekends, Holidays) without any UI or File I/O dependencies. This forms the "Start" of our Clean Architecture.

### Story 1.1: Create Project Structure & Domain Models

**As a** Developer,
**I want** to initialize the solution and core data models,
**So that** I have a type-safe representation of the CSV data and Rate rules.

**Acceptance Criteria:**
*   **Given** a new repository, **When** I initialize the solution, **Then** `ElectricGenerationParser.sln` exists with `src` and `test` projects.
*   **Given** the CSV schema, **Then** a `GenerationRecord` model exists with properties for Date/Time, Produced, Consumed, Export, Import.
*   **Given** the need for Rate logic, **Then** a `RateType` enum exists (OnPeak, OffPeak).
*   **Given** the need for Configuration, **Then** `HolidaySettings` and `PeakHoursSettings` POCOs exist.

### Story 1.2: Implement Holiday Configuration Service

**As a** User,
**I want** to define holidays in `appsettings.json`,
**So that** I don't need to recompile the code every year.

**Acceptance Criteria:**
*   **Given** `appsettings.json`, **When** I define a fixed holiday (e.g., Dec 25), **Then** `HolidayService` correctly identifies it.
*   **Given** `appsettings.json`, **When** I define a floating holiday (e.g., "Last Monday of May"), **Then** `HolidayService` calculates the correct date for any given year.
*   **Given** an "Observed" rule (Sat->Fri, Sun->Mon), **Then** `HolidayService` marks the observed date as a holiday.
*   **Given** a year, **When** `GetHolidays(year)` is called, **Then** it returns a distinct list of `DateOnly` objects.

### Story 1.3: Implement Time-of-Use Strategy Pattern

**As a** User,
**I want** the system to accurately classify every timestamp,
**So that** I know if I was generating during peak or off-peak hours.

**Acceptance Criteria:**
*   **Given** a timestamp, **When** passed to `RateCalculator`, **Then** it returns the correct `RateType`.
*   **Given** a Weekend (Sat/Sun), **Then** `WeekendStrategy` returns `OffPeak`.
*   **Given** a Holiday (from Story 1.2), **Then** `HolidayStrategy` returns `OffPeak`.
*   **Given** a Weekday within Peak Hours (e.g., 7am-7pm), **Then** `WeekdayStrategy` returns `OnPeak`.
*   **Given** a Weekday outside Peak Hours, **Then** `WeekdayStrategy` returns `OffPeak`.
*   **Unit Tests:** Must cover all permutations (Weekday Peak, Weekday OffPeak, Weekend, Holiday, Observed Holiday).

## Epic 2: Input & Configuration

**Goal:** Enable the application to startup, ingest data, and bridge the "Core Logic" to the "Real World".

### Story 2.1: Implement CLI Arguments & Configuration Loading

**As a** User,
**I want** to run the app with a file path argument,
**So that** I can process different reports easily.

**Acceptance Criteria:**
*   **Given** no arguments, **Then** the app prints a usage message and exits with code 1.
*   **Given** a non-existent file path, **Then** the app prints an error and exits with code 1.
*   **Given** a valid file path, **Then** the app converts it to an absolute path for processing.
*   **Given** `appsettings.json`, **Then** it is loaded into `IOptions<T>` and validated.

### Story 2.2: CSV Ingestion with CsvHelper

**As a** Developer,
**I want** to parse the CSV file using a robust library,
**So that** I handle quoting, headers, and date formats correctly.

**Acceptance Criteria:**
*   **Given** a valid CSV, **When** parsed, **Then** it returns a `List<GenerationRecord>`.
*   **Given** the specific header format ("Energy Produced (Wh)"), **Then** a `ClassMap` explicitly maps it to the model.
*   **Given** a missing header, **Then** the parser throws a specific validation exception.
*   **Given** a malformed row, **Then** the specific line number is reported in the error.

## Epic 3: Reporting & Validation

**Goal:** Aggregating the data, running checksums, and rendering the final output.

### Story 3.1: Data Aggregation & Checksum Validation

**As a** User,
**I want** to trust the data and see granular details,
**So that** I don't look foolish when I use these numbers and can verify holiday credits.

**Acceptance Criteria:**
*   **Given** a list of categorized records, **When** processed, **Then** `ReportGenerator` sums up OnPeak and OffPeak totals for all 4 metrics (Prod, Cons, Imp, Exp).
*   **Given** the list of categorized records, **Then** `ReportGenerator` calculates aggregated totals for all Weekend days.
*   **Given** the list of categorized records, **Then** `ReportGenerator` calculates individual totals for EACH specific holiday encountered (e.g., "Christmas", "New Year").
*   **Given** the sums, **When** `Total OnPeak + Total OffPeak != Total Input`, **Then** a `ValidationException` is thrown.
*   **Given** valid data, **Then** a `ReportModel` is returned containing all summaries.

### Story 3.2: Console Output
 with detailed breakdowns,
**So that** I can easily key the numbers into my spreadsheet.

**Acceptance Criteria:**
*   **Given** a `ReportModel`, **Then** the application writes a text-based table with columns for Metric, On-Peak, Off-Peak, and Total.
*   **Given** the report model, **Then** a separate section displays "Weekend Totals" for all metrics.
*   **Given** the report model, **Then** a separate section list each Holiday by name with its specific metrics
**Acceptance Criteria:**
*   **Given** a `ReportModel`, **Then** the application writes a text-based table with columns for Metric, On-Peak, Off-Peak, and Total.
*   **Given** the table is rendered, **Then** numeric values are formatted (e.g., N0 or N2).
*   **Given** the table is rendered, **Then** no debug logs pollute the output (standard out).

## Epic 4: Packaging & Polish

**Goal:** Ensure the app is robust and easy to distribute.

### Story 4.1: Global Error Handling & Publishing

**As a** User,
**I want** friendly error messages,
**So that** I know what went wrong without seeing a stack trace.

**Acceptance Criteria:**
*   **Given** an unhandled exception (e.g., File Locked), **Then** `Program.cs` catches it and prints a red error message to standard error.
*   **Given** the need to ship, **Then** a publish profile creates a single `.exe` file (SCD, Win-x64, Trimmed if possible).


