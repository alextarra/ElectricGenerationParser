# Code Review - Story 1.2: Implement Holiday Configuration Service

## Summary
**Status:** Changes Requested
**Severity:** Medium

The implementation meets the functional Acceptance Criteria but has a potential performance concern regarding the `IsHoliday` method and NFR-01.

## Findings

### 1. Performance: Inefficient `IsHoliday` Implementation
**Severity:** Medium
**File:** `src/ElectricGenerationParser/Services/HolidayService.cs`

The `IsHoliday(DateOnly date)` method calls `GetHolidays(date.Year)` every time.
```csharp
    public bool IsHoliday(DateOnly date)
    {
        return GetHolidays(date.Year).Contains(date);
    }
```
For a dataset with 3000 rows (NFR-01), this will trigger holiday calculation 3000 times. While the calculation is lightweight, it involves iterating settings, DateTime math, and allocations (HashSet).
**Recommendation:** Implement caching for the holiday list by year. A `Dictionary<int, HashSet<DateOnly>>` cache would solve this.

### 2. Edge Case: WeekInstance Out of Range
**Severity:** Low
**File:** `src/ElectricGenerationParser/Services/HolidayService.cs`

In `CalculateFloatingHoliday`:
```csharp
            if (config.WeekInstance > 0)
            {
                if (config.WeekInstance <= dates.Count)
```
If `WeekInstance` is very large (e.g., 5th Monday when there are only 4), it returns `null`. This is handled correctly (returns null), but explicit logging or handling might be useful if the user misconfigures it.
**Recommendation:** Current behavior (ignore) is acceptable but a comment confirming this design choice would be nice. The current `catch` block swallows exceptions which covers some cases, but logic errors like "5th Monday" are silent.

### 3. Missing `IOptions` usage note
**Severity:** Low
**File:** `src/ElectricGenerationParser/Services/HolidayService.cs`

The code has comments about `IOptions` which is good. Just noting that when Story 2.1 is implemented, this file will need to change to accept `IOptions<HolidaySettings>`.
**Recommendation:** Keep the comments as existing TODOs.

## Action Plan
1.  Add caching to `HolidayService`.
2.  (Optional) Add unit test for `IsHoliday` caching or performance (e.g. calling it 10,000 times).
