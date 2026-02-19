# Code Review - Story 1.3: Implement Time-of-Use Strategy Pattern

## Summary
**Status:** Approved (with minor suggestion)
**Severity:** Low

The implementation is solid. The Strategy pattern is correctly implemented with a Chain of Responsibility style in `RateCalculator`. Null returns correctly indicate "not applicable".

## Findings

### 1. Strategy Ordering Dependency
**Severity:** Low
**File:** `src/ElectricGenerationParser/Services/RateCalculator.cs`

The `RateCalculator` relies entirely on the order of `IEnumerable<IRateStrategy>` injected.
If `WeekdayStrategy` is injected before `WeekendStrategy`, it might return `OffPeak` for weekend days (since `WeekdayStrategy` explicitly returns null for weekends now, it is safe).
However, if `WeekdayStrategy` logic changed to just check "not peak hours", it might claim weekends.
**Observation:** The current implementations are robust (`WeekdayStrategy` checks `!Weekend`, `WeekendStrategy` checks `Weekend`). This makes ordering less critical between these two, but clearly `HolidayStrategy` MUST come first.
**Recommendation:** Ensure the DI container registration (in Story 2.1) registers them in the correct prioritized order: `Holiday`, `Weekend`, `Weekday`. No code change needed here, just a note for the next story.

### 2. Loose Typing on Strategy Collection
**Severity:** Info
**File:** `src/ElectricGenerationParser/Services/RateCalculator.cs`

`IEnumerable<IRateStrategy>` is generic. Some DI containers might not guarantee order unless explicitly configured to do so (e.g. `OrderedEnumerable` or explicit list registration).
**Recommendation:** When wiring up DI, be explicit.

### 3. Missing `IRateCalculator` registration
**Severity:** Info
**File:** N/A (Future)

Remember to register `IRateCalculator` and the strategies in `Program.cs` in Story 2.1.

## Action Plan
1.  **Mark Story 1.3 Done.**
2.  Proceed to Story 2.1 (Configuration & CLI), being mindful of DI registration order.
