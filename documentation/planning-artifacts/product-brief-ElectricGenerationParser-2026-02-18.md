---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments: []
date: 2026-02-18
author: Sas
---

# Product Brief: ElectricGenerationParser

## Executive Summary

ElectricGenerationParser is a specialized utility designed to automate the complex accounting of electricity generation, consumption, and import/export for solar panel owners. By parsing standard CSV reports and applying sophisticated logic for on-peak/off-peak rate schedules—including holiday and weekend adjustments—it eliminates the manual effort and potential errors associated with spreadsheet calculations. The tool provides clear, actionable summaries to track solar panel payoff and electricity costs accurately.

## Core Vision

### Problem Statement

Solar panel owners with time-of-use electricity plans face a significant challenge in accurately tracking their financial performance. Utility providers often have complex rate structures where "on-peak" and "off-peak" periods vary not just by time of day, but also by day of the week and specific holiday calendars (where holidays or weekends falling on adjacent days shift rate periods). Manually calculating these costs using Excel is tedious, prone to human error, and difficult to maintain over time.

### Problem Impact

*   **Inaccurate Financial Tracking:** Owners cannot reliably determine the true payoff period of their solar investment.
*   **Time Consumption:** Hours are wasted each billing cycle manually cross-referencing calendars and rate schedules.
*   **Data Opaqueness:** Difficult to get a quick snapshot of net generation versus consumption during the most expensive rate periods.

### Why Existing Solutions Fall Short

*   **Excel:** While flexible, it requires complex, fragile formulas to handle "floating" holidays and weekend logic, making it hard to maintain.
*   **Generic Solar Apps:** Often lack the granularity to support specific, complex utility rate schedules or local holiday calendars.

### Proposed Solution

A dedicated console application that:
1.  Ingests standard CSV generation reports from the solar monitoring system.
2.  Applies a configurable, logic-based schedule to categorize every data point as on-peak or off-peak.
3.  Automatically handles weekend and holiday logic (e.g., if a holiday falls on a weekend, the off-peak rate applies to the adjacent weekday).
4.  Outputs a concise summary of total Consumption, Generation, and Import/Export for each rate period.

### Key Differentiators

*   **Logic-First Approach:** specifically built to handle the edge cases of utility rate schedules (holidays, weekends) that trip up standard tools.
*   **Simplicity:** A focused, "drop-in" command-line tool that does one thing perfectly without bloat.
*   **Accuracy:** Eliminates formula errors common in manual spreadsheet maintenance.

## Target Users

### Primary Users

**The Data-Driven Solar Owner (Sas)**
*   **Profile:** A tech-savvy homeowner who has invested in solar panels and wants to track the financial return on investment accurately. Comfortable with command-line tools and configuration files but values efficiency over manual data entry.
*   **Goal:** To obtain a precise monthly accounting of electricity costs and savings, broken down by specific utility rate periods (on-peak vs. off-peak), without spending hours in Excel.
*   **Pain Points:** Frustrated by the "black box" of utility bills and the tedium of manually calculating rates that change based on holidays and weekends.
*   **Success Criteria:** Running a single command once a month and getting a definitive report how much energy was imported, exported, generated and consumed during on-peak and off-peak periods.

### Secondary Users

*   **N/A:** Currently, this tool is designed strictly for personal use by the developer/owner.

### User Journey

**Monthly Billing Audit**
1.  **Trigger:** User receives the monthly utility bill notification or decides it's time to "close the books" for the month.
2.  **Action:** User downloads the CSV generation report from their solar monitoring system (e.g., Enlighten, SolarEdge) to a known folder.
3.  **Execution:** User opens a terminal and runs `ElectricGenerationParser.exe` pointing to the new CSV filing.
4.  **Result:** The application immediately outputs a summary to the console (and/or a text file) showing:
    *   Total On-Peak Generation / Consumption / Export
    *   Total Off-Peak Generation / Consumption / Export
5.  **Outcome:** User uses these final numbers into their financial tracking sheet.

## Success Metrics

### User Success
*   **Data Availability:** The tool successfully parses standard CSV reports and outputs the required summaries (On-Peak/Off-Peak Generation, Consumption, Import/Export) without errors.
*   **Time Efficiency:** The process takes less than 1 minute per month, replacing manual Excel work.
*   **Consistency:** The tool consistently applies the defined logic (holidays/weekends) month-over-month, ensuring comparable data for long-term tracking.

### Business Objectives
*   **N/A:** Personal utility project focused on automating data summarization for personal accounting.

## MVP Scope

### Core Features

*   **CSV Ingestion:** Ability to read the specific CSV export format provided by the user's solar monitoring system.
*   **Time-of-Use Logic Engine:** A robust date/time evaluator that determines if a given timestamp is "On-Peak" or "Off-Peak" based on:
    *   Time of day (e.g., 7am-7pm).
    *   Day of week (Weekends = Off-peak).
    *   Specific Holiday handling (matches a provided list).
    *   Holiday observation logic (e.g., if holiday is on Sunday, Monday is off-peak).
*   **Configuration:** External configuration (e.g., `appsettings.json`) to define the Peak hours, Weekend rules, and a list of specific holiday dates, allowing the tool to be updated without recompilation.
*   **Summary Reporting:** Console output displaying the aggregate totals for:
    *   On-Peak Analysis (Generation, Consumption, Import/Export).
    *   Off-Peak Analysis (Generation, Consumption, Import/Export).

### Out of Scope for MVP

*   **Graphical User Interface (GUI):** The tool will be a pure Command Line Interface (CLI).
*   **Database Storage:** No local database or history tracking; the tool processes the input file and exits.
*   **Automated Downloading:** The user is responsible for obtaining the CSV file from the provider.
*   **Multiple Rate Plans:** The system will support a single active rate configuration.
*   **Tiered Rates:** Focus is strictly on Time-of-Use (Peak vs. Off-Peak) logic, not volume-based pricing tiers.

### MVP Success Criteria

*   **Logic Functionality:** Accurately identifies 100% of "edge case" timestamps (holidays, observed holidays, weekends) correctly according to the user's utility rules.
*   **Usability:** User can configure the holiday list for the upcoming year in less than 5 minutes using the JSON config.

### Future Vision

*   **Visualization:** Generating simple charts (ASCII or HTML export) to visualize usage patterns.
*   **History Tracking:** Appending results to a long-running "Year to Date" log file.
*   **Multi-Provider Support:** Abstracting the CSV parser to support data from different solar inverter manufacturers.
*   **Automated Holiday Sync:** Potential future integration with Google Calendar API (or iCal feed) to automatically fetch and update the holiday list for the current year, reducing manual configuration.

## Additional Requirements

### Holiday Configuration
*   **Configurable Holiday Rules:** The system must allow configuration of holidays, supporting both fixed dates (e.g., Dec 25) and relative dates (e.g., Last Monday of May).
*   **Current Holiday Schedule:**
    *   New Year’s Day: January 1
    *   Memorial Day: Last Monday of May
    *   Independence Day: July 4
    *   Labor Day: First Monday in September
    *   Thanksgiving Day: Fourth Thursday in November
    *   Christmas Day: December 25
*   **Observation Rules:**
    *   If a fixed-date holiday falls on a **Saturday**, it is observed on the preceding **Friday**.
    *   If a fixed-date holiday falls on a **Sunday**, it is observed on the following **Monday**.


