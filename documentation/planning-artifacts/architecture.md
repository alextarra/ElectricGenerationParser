---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - documentation/planning-artifacts/prd.md
  - documentation/planning-artifacts/product-brief-ElectricGenerationParser-2026-02-18.md
  - documentation/5919893_custom_report.csv
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-02-19'
project_name: 'ElectricGenerationParser'
user_name: 'Sas'
date: '2026-02-19'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## 1. Context Analysis & Project Scope

### 1.1 Project Overview
**ElectricGenerationParser** is a suite of two distinct applications (CLI & Web) designed to automate the complex financial accounting of electricity generation, consumption, and grid interaction.

### 1.2 Core Architectural Challenge
The primary architectural complexity lies in the **Time-of-Use Logic Engine**. The system must accurately classify every row's timestamp into a specific rate bucket based on a hierarchy of rules. This logic must be **identical** across both the CLI and Web interfaces.

### 1.3 Scope of Work
*   **Shared Core Library:** Encapsulating the "Logic Engine" (Parsing, Peak/Off-Peak Classification, Validation).
*   **CLI Application:** Simple wrapper for the Core Library (file I/O).
*   **Web Application:** New interface for the Core Library (HTTP Upload/Response).

### 1.4 Complexity Assessment
*   **Project Type:** Application Suite (CLI + Web)
*   **Domain:** Energy Data Processing & Personal Finance
*   **Complexity:** Low (Single-user, logic-heavy)
*   **Data Volume:** ~750 rows per file (Monthly report).
*   **Performance:** Sub-second processing required.

## 2. Technology Selection & Starter Template

### 2.1 Framework Decision
*   **Platform:** .NET 8 (LTS)
*   **Rationale:** Long-term support, stability, and broad compatibility. While .NET 9/10 are available, .NET 8 is the industry standard for stable deployments.

### 2.2 Solution Structure (The "Starter")
We will implement a custom multi-project solution structure rather than a monolithic template.
*   **Core (Class Library):**
    *   `ElectricGenerationParser.Core`
    *   *Purpose:* Pure business logic, standard reference.
    *   *Dependencies:* `CsvHelper`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection.Abstractions`.
*   **CLI (Console):**
    *   `ElectricGenerationParser.Cli`
    *   *Purpose:* Command-line interface.
    *   *Dependencies:* `Core`, `Spectre.Console`, `Microsoft.Extensions.Hosting`.
*   **Web (ASP.NET Core):**
    *   `ElectricGenerationParser.Web`
    *   *Pattern:* **Razor Pages** (Server-side rendering).
    *   *Rationale:* Simplest model for input-processing-output workflows. No client-side state management complexity required.
    *   *Dependencies:* `Core`.

### 2.3 Dependency Strategy
*   **CsvHelper:** For robust CSV parsing (industry standard).
*   **Spectre.Console:** For rich CLI output (tables, progress).
*   **Microsoft.Extensions.Hosting/DI:** To standardize dependency injection across both Console and Web apps.
*   **No Database:** State is transient (per request/run). Configuration is static (json).

## 3. Core Architectural Decisions

### 3.1 Date/Time Logic Engine (The "Brain")
*   **Pattern:** **Strategy Pattern** via `IRateCalculator` interface.
    *   `WeekdayRateStrategy`: Checks if date is Mon-Fri AND outside holiday list. Applies time-based window.
    *   `WeekendRateStrategy`: Checks if date is Sat/Sun. Always returns Off-Peak.
    *   `HolidayRateStrategy`: Highest priority. Checks if date is in the calculated holiday list. Always returns Off-Peak.
*   **Time Window:** Configurable start/end hour (e.g., `07:00` to `19:00`) for weekdays.

### 3.2 Web Upload Architecture
*   **Processing Model:** **In-Memory Processing**.
    *   *Mechanism:* Uploaded file stream is read directly into memory (`IFormFile.OpenReadStream()`).
    *   *Rationale:* Files are small (~50KB) and transient. Storing on disk introduces unnecessary I/O latency, permission complexity, and cleanup overhead.
    *   *State:* Stateless Request/Response cycle.

### 3.3 Error Handling & Communication
*   **Pattern:** **Typed Exceptions**.
    *   *Core Library:* Throws specific exceptions (`CsvValidationException`, `ConfigurationException`) for logical failures.
    *   *CLI Handling:* Catches exceptions -> Prints user-friendly localized error message to `stderr` -> Exits with non-zero code.
    *   *Web Handling:* Catches exceptions -> Returns HTTP 400 Bad Request with the exception message in the response body or UI alert.

### 3.4 Shared Configuration Strategy
*   **Schema:** Defined in `ElectricGenerationParser.Core.Configuration`.
    *   `HolidaySettings`: List of fixed/floating definitions.
    *   `PeakHoursSettings`: Daily start/end times.
*   **Deployment:** Each app (`Cli` and `Web`) has its own `appsettings.json`, but they adhere to the exact same schema. This allows environment-specific overrides (e.g., Docker env vars for Web) while keeping logic identical.

## 4. Implementation Patterns

### 4.1 Dependency Injection (DI) Pattern
*   **Pattern:** **Shared Service Extension** via `AddElectricGenerationCore`.
*   **Implementation:** The Core library will expose a single extension method `public static IServiceCollection AddElectricGenerationCore(this IServiceCollection services, IConfiguration config)`.
*   **Benefit:** Ensures that `RateCalculator`, `CsvParser`, `HolidayService`, and strict `IOptions` binding are **identically configured** in both `Cli/Program.cs` and `Web/Program.cs`. Reduces risk of "it works in CLI but fails in Web".

### 4.2 Configuration Pattern
*   **Pattern:** **BIO (Bind, Validate, Options)**.
*   **Rule:** Configuration classes (`HolidaySettings`) must use `[Required]` or `[Range]` data annotations.
*   **Validation:** Service registration must use `.ValidateDataAnnotations().ValidateOnStart()` to prevent the application from even starting if `appsettings.json` is missing or malformed.
*   **Strict Keys:** Section keys `"HolidaySettings"` and `"PeakHoursSettings"` are constant strings in the Core library.

### 4.3 Logging Pattern
*   **Pattern:** **ILogger Abstraction**.
*   **Rule:** The Core library must **NEVER** use `Console.WriteLine`. It must exclusively use `ILogger<T>` for all diagnostic output.
*   **Adaptation:**
    *   **CLI:** Configures a Console Logger (writing to Stderr) during host startup.
    *   **Web:** Uses default ASP.NET Core logging.
*   **Separation:** This keeps specific "UI" output (the final table) separate from "Logic" logs (parsing warnings).

### 4.4 File Path Handling
*   **Pattern:** **Cross-Platform Path Safety**.
*   **Rule:** Use `System.IO.Path.Combine()` exclusively. Never usage string concatenation for paths (`"folder\\" + file`).
*   **Rationale:** Ensures the application runs correctly on both Windows (dev) and Linux (potential web host).

## 5. Project Structure

### 5.1 Solution Layout
**Solution:** `ElectricGenerationParser.sln`

```text
src/
├── ElectricGenerationParser.Core/       <-- The Shared Brain (Class Library)
│   ├── Configuration/                   <-- Settings Models (HolidaySettings.cs)
│   ├── Exceptions/                      <-- Typed Exceptions
│   ├── Interfaces/                      <-- Service Contracts
│   ├── Models/                          <-- Data Models (GenerationRecord.cs)
│   ├── Services/                        <-- Business Logic (RateCalculator.cs)
│   ├── Strategies/                      <-- Rate Strategy Implementations
│   └── ServiceCollectionExtensions.cs   <-- Shared DI Setup
├── ElectricGenerationParser.Cli/        <-- The Console App
│   ├── Services/                        <-- ConsoleOutputService.cs
│   ├── Program.cs                       <-- Entry Point & Command Parsing
│   └── appsettings.json                 <-- Local CLI Config
└── ElectricGenerationParser.Web/        <-- The Web App (ASP.NET Core)
    ├── Pages/                           <-- Razor Pages (Index.cshtml)
    ├── Program.cs                       <-- Web Entry Point
    └── appsettings.json                 <-- Web Config
tests/
└── ElectricGenerationParser.Tests/      <-- Unit Tests
    ├── Core/                            <-- Tests for Logic
    └── Integration/                     <-- End-to-End Tests
```

### 5.2 Component Mapping
*   **Core Logic (FR-05 to FR-11):** Lives strictly in `ElectricGenerationParser.Core`.
*   **CLI UX (FR-01 to FR-04):** Lives in `ElectricGenerationParser.Cli`.
*   **Web UX (FR-W1 to FR-W5):** Lives in `ElectricGenerationParser.Web`.
*   **Unit Tests:** Focus 90% of testing effort on `Core`. If Core is correct, both apps are likely correct.

## 6. Architecture Validation

### 6.1 Coherence Check
*   **Ecosystem:** .NET 8 + CsvHelper + Spectre.Console + Razor Pages forms a cohesive, modern .NET ecosystem.
*   **Logic Isolation:** The "Shared Brain" approach (Core Library) perfectly addresses the critical risk of diverging logic between CLI and Web.
*   **Configuration:** The "Shared DI Extension" pattern prevents "it works on my machine" issues by ensuring configuration binding code is identical across apps.

### 6.2 Requirements Coverage
*   **Logic (FR-05 - FR-11):** Fully covered by `ElectricGenerationParser.Core`.
*   **CLI (FR-01 - FR-04):** Fully covered by `ElectricGenerationParser.Cli`.
*   **Web (FR-W1 - FR-W5):** Fully covered by `ElectricGenerationParser.Web`.
*   **Shared Config (FR-17, FR-18):** Fully covered by `Core.Configuration` and `AddElectricGenerationCore`.

### 6.3 Gap Analysis & Mitigation
*   **Gap: Web UI Details:** The rendering strategy for the Web UI is high-level.
    *   **Mitigation:** We will implement a standard HTML table in `Index.cshtml` that iterates over the `RatePeriodSummary` model, mirroring the Console output structure.
*   **Gap: Docker Support:** No explicit requirement for containerization, but helpful for web deployment.
    *   **Mitigation:** A `Dockerfile` will be added to the `ElectricGenerationParser.Web` project as a "Recommended" artifact for easy deployment, though not strictly required for local dev.
*   **Gap: Testing Web UI:** The plan focuses heavily on Core testing.
    *   **Mitigation:** Since the Web UI is a "Thin Client" (no logic in Razor pages), Core unit tests provide 90% confidence. Integration tests can be added later if UI complexity grows.
