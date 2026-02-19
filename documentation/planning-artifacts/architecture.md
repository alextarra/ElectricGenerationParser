---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - documentation/planning-artifacts/prd.md
  - documentation/planning-artifacts/product-brief-ElectricGenerationParser-2026-02-18.md
  - documentation/5919893_custom_report.csv
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-02-18'
project_name: 'ElectricGenerationParser'
user_name: 'Sas'
date: '2026-02-18'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## 1. Context Analysis & Data Structure

### 1.1 Input Data Analysis
**Source:** `5919893_custom_report.csv`
**Format:** CSV
**Key Columns Identified:**
- `Date/Time`: Timestamp (e.g., `01/01/2026 00:00`)
- `Energy Produced (Wh)`: Integer/Float
- `Energy Consumed (Wh)`: Integer/Float
- `Exported to Grid (Wh)`: Integer/Float
- `Imported from Grid (Wh)`: Integer/Float

### 1.2 Core Architectural Challenge
The primary architectural complexity lies in the **Time-of-Use Logic Engine**. The system must accurately classify every row's timestamp into a specific rate bucket based on a hierarchy of rules:
1.  **Holiday Logic:** Is the date a fixed holiday? Is it an *observed* holiday (e.g., Sunday holiday observed on Monday)?
2.  **Weekend Logic:** Is it a Saturday or Sunday?
3.  **Time-of-Day Logic:** Is the hour within the Peak window?

This requires a robust, testable, and configurable **Date/Time Evaluation Strategy**.

## 4. Core Implementation Decisions

### 4.1 Date/Time Logic Engine (The "Brain")
*   **Pattern:** **Strategy Pattern** via `IRateCalculator` interface.
    *   `WeekdayRateStrategy`: Checks if date is Mon-Fri AND outside holiday list. Applies time-based window.
    *   `WeekendRateStrategy`: Checks if date is Sat/Sun. Always returns Off-Peak.
    *   `HolidayRateStrategy`: Highest priority. Checks if date is in the calculated holiday list. Always returns Off-Peak.
*   **Time Window:** Configurable start/end hour (e.g., `07:00` to `19:00`) for weekdays.

### 4.2 Holiday Configuration Strategy
*   **Requirement:** Holidays must be defined in `appsettings.json`, not hardcoded.
*   **Schema Support:**
    *   **Fixed Date:** `{ "Name": "Christmas", "Month": 12, "Day": 25 }`
    *   **Floating Date:** `{ "Name": "Memorial Day", "Month": 5, "DayOfWeek": "Monday", "occurrence": -1 }` (Last Monday)
    *   **Observed Rule:** `{ "Name": "New Year Observed", "Month": 1, "Day": 1, "ObserveWeekend": true }`
*   **Implementation:** A `HolidayService` will parse these rules at startup and generate a `HashSet<DateOnly>` for O(1) lookups during processing.

### 4.3 CSV Mapping Strategy
*   **Strict Mapping:** We will rely on `CsvHelper` and strict ClassMaps. If the provider changes the header format, the application should halt to avoid processing garbage data.

### 4.4 Output Handling
*   **Strategy:** Standard `Console.WriteLine`.
*   **Rationale:** Simpler dependency footprint. The focus is on raw data output (potentially redirectable to a file via `> output.txt`), rather than rich UI.

## 5. Implementation Patterns

### 5.1 Error Handling Pattern
*   **Fail Fast:**
    *   Invalid Config -> Throw `ConfigurationException` on statup.
    *   Invalid CSV Header -> Throw `HeaderValidationException` immediately.
    *   Malformed Data Row -> Log warning to `stderr`, skip row, continue processing (don't crash the whole batch for one bad line unless critical).
*   **Global Handler:** `Program.cs` wraps execution in a `try/catch` block to print user-friendly error messages to `stderr` and exit with code 1.

### 5.2 Configuration Pattern
*   **Strong Typing:** strict usage of `IOptions<T>`.
    *   Classes: `HolidaySettings`, `PeakHoursSettings`.
    *   **No** direct `IConfiguration` injection into services.

### 5.3 Logging vs Output
*   **Separation of Concerns:**
    *   **Logs (Diagnostics):** Use `ILogger` (Microsoft.Extensions.Logging). configured to write to `Debug` or a File, or `stderr` if verbose.
    *   **Report (User Output):** Use `Console.WriteLine` strictly for the final data table. This ensures `STDOUT` is clean for piping.

### 5.4 File System Abstraction
*   **Path Safety:** Use `System.IO.Path.Combine()` exclusively.
*   **I/O Abstraction:** Services should take `Stream` or `TextReader` where possible (via `IFileSystem` wrapper or just passing streams) to allow easy unit testing without touching distinct disk files.

## 6. Project Structure

### 6.1 Solution Overview
**Solution:** `ElectricGenerationParser.sln`

### 6.2 Component Mapping
*   **Ingestion:** `Services/CsvParserService.cs`, `Models/GenerationRecord.cs`, `Models/GenerationRecordMap.cs`
*   **Logic (Brain):** `Services/RateCalculator.cs`, `Strategies/WeekdayStrategy.cs`, `Strategies/WeekendStrategy.cs`, `Strategies/HolidayStrategy.cs`, `Services/HolidayService.cs`
*   **Calc & Validate:** `Services/ReportGenerator.cs`, `Services/ValidatorService.cs`
*   **Output:** `Services/ConsoleOutputService.cs`
*   **config:** `Configuration/PeakHoursSettings.cs`, `Configuration/HolidaySettings.cs`

### 6.3 Detailed File Tree
```text
ElectricGenerationParser/
├── ElectricGenerationParser/
│   ├── ElectricGenerationParser.csproj
│   ├── Program.cs                  <-- DI Setup, Entry Point, Global Error Handling
│   ├── appsettings.json            <-- The Rules (Holidays, Peak Hours)
│   ├── Configuration/              <-- Strongly Typed Settings
│   │   ├── HolidaySettings.cs
│   │   └── PeakHoursSettings.cs
│   ├── Models/                     <-- Data Structures
│   │   ├── GenerationRecord.cs     <-- CSV Row Model
│   │   ├── GenerationRecordMap.cs  <-- CsvHelper Mapping Class
│   │   └── RatePeriodSummary.cs    <-- Output DTO
│   ├── Services/                   <-- Business Logic Interfaces & Implementations
│   │   ├── Interfaces/
│   │   │   ├── ICsvParser.cs
│   │   │   ├── IHolidayService.cs
│   │   │   ├── IRateCalculator.cs
│   │   │   └── IReportGenerator.cs
│   │   ├── CsvParserService.cs
│   │   ├── HolidayService.cs       <-- Calculates dynamic holidays
│   │   ├── RateCalculator.cs       <-- The Strategy Context
│   │   └── ReportGenerator.cs      <-- Orchestrates the flow
│   └── Strategies/                 <-- The "Brains" of the Rate Logic
│       ├── IRateStrategy.cs
│       ├── WeekdayStrategy.cs
│       ├── WeekendStrategy.cs
│       └── HolidayStrategy.cs
└── ElectricGenerationParser.Tests/
    ├── ElectricGenerationParser.Tests.csproj
    ├── Services/                   <-- Unit Tests mirror structure
    │   ├── CsvParserTests.cs
    │   ├── HolidayServiceTests.cs
    │   └── RateCalculatorTests.cs
    └── Strategies/
        └── StrategyTests.cs
```

## 7. Architecture Validation

### 7.1 Coherence Check
*   **Tech Stack:** .NET 8 + CsvHelper + Spectre.Console + Microsoft.Extensions.* is a standard, robust, modern .NET CLI stack.
*   **Logic Pattern:** Strategy Pattern (Rate Logic) + Holiday Service (Data) separates concerns effectively for testing.
*   **Configuration:** `IOptions<T>` + `appsettings.json` is the standard .NET approach.

### 7.2 Requirements Coverage
*   **Ingestion (FR-01 to FR-04):** Covered by `CsvHelper` and strict ClassMaps.
*   **Logic (FR-05 to FR-08):** Covered by `IRateStrategy` and `HolidayService`.
*   **Calculations (FR-09 to FR-11):** Covered by `ReportGenerator` aggregations and `ValidatorService` checksums.
*   **Output (FR-12 to FR-15):** Covered by `Spectre.Console` table rendering.
*   **NFR Coverage:**
    *   **Performance:** High-performance CSV parsing and O(1) holiday lookups.
    *   **Maintainability:** Clean Architecture with DI.
    *   **Portability:** SCD Deployment.

### 7.3 Gap Analysis & Mitigation
*   **Data Integrity:** The PRD requires strict checksum validation (`Input Sum == Output Sum`).
*   **Mitigation:** `ValidatorService` must explicitly calculate `Sum(OnPeak + OffPeak)` for all metric columns (Produced, Consumed, Import, Export) and compare against `Sum(TotalInput)`. Any deviation halts the process.


