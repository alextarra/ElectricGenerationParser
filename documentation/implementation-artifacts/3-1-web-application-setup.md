# Story 3.1: Web Application Setup & Upload Form

Status: done

## Story

As a User,
I want to access the application via a web browser and see a file upload form,
So that I can process my generation report without installing any software.

## Acceptance Criteria

1.  **Given** a browser, **When** I navigate to the homepage, **Then** I should see a simple form with a file upload input (`.csv`) and a "Process Report" button.
2.  **Given** the project structure, **When** I initialize the web project, **Then** `ElectricGenerationParser.Web` should exist and reference `ElectricGenerationParser.Core`.
3.  **Given** the form, **When** mapped, **Then** it should post to a controller/endpoint that accepts `IFormFile`.
4.  **Given** the Core `IServiceCollectionExtensions`, **When** configuring Services, **Then** `Program.cs` (Web) should call `AddElectricGenerationCore`.

## Tasks / Subtasks

- [x] Task 1: Create Web Project (AC: 2)
  - [x] `dotnet new webapp -o src/ElectricGenerationParser.Web`
  - [x] Add to solution.
  - [x] Add reference to `ElectricGenerationParser.Core`.
- [x] Task 2: Configure Startup (AC: 4)
  - [x] Update `Program.cs` to use `AddElectricGenerationCore`.
  - [x] Ensure `appsettings.json` has `Holiday` and `PeakHours` sections (copy from CLI/Core or use defaults).
- [x] Task 3: Implement Upload Form (AC: 1, 3)
  - [x] Update `Index.cshtml` to include `<form>` with `enctype="multipart/form-data"`.
  - [x] Add file input and submit button.
  - [x] Create `OnPostAsync` handler in `Index.cshtml.cs` to accept `IFormFile`.

## Dev Notes
- Use Razor Pages for simplicity (default `webapp` template).
- Do not implement processing logic yet (next story). Just the form and binding.
- Verify `Core` services are resolvable.

## Dev Agent Record
