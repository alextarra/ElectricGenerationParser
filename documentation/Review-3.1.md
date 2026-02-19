# Code Review Report: Story 3.1

**Reviewer**: BMAD Agent (Adversarial Mode)
**Date**: 2026-02-19
**Status**: 🔴 CHANGES REQUESTED

## Summary

The implementation of Story 3.1 is functionally correct for the happy path but contains design flaws regarding data immutability and testability of error conditions. Acceptance Criteria 2 is implemented but **effectively unreachable** and therefore **unverified** by tests.

## Critical Findings

### 1. 🔴 AC 2 Verification Impossible (Testability)
- **Claim**: AC 2 states "When `Total OnPeak + Total OffPeak != Total Input`, Then a `ValidationException` is thrown".
- **Reality**: The `ReportGenerator` implementation guarantees internal consistency by construct. The `ValidateChecksums` method is unreachable code unless there is a bug in `decimal` arithmetic or `MetricSummary.Add`.
- **Impact**: We cannot verify that the validation logic *actually works* (i.e., throws the exception) without using reflection or refactoring.
- **Fix**: Refactor `ReportGenerator` to be more testable, perhaps by allowing injection of a "calculator" or "aggregator" strategy that can be mocked to return inconsistent results, OR use `internal` methods with `[InternalsVisibleTo]` to inject faults for testing.

### 2. 🟠 Side Effect: Mutation of Input Data (Code Quality)
- **File**: `src/ElectricGenerationParser/Services/ReportGenerator.cs`
- **Issue**: The method `GenerateReport(List<GenerationRecord> records)` **modifies** the objects within the input list:
    ```csharp
    record.Export = net; // Modifies the caller's object!
    record.Import = 0;
    ```
- **Impact**: This is a hidden side effect. If the caller reuses the `records` list (e.g., for another report or logging), the data has been silently changed. This violates the Principle of Least Surprise.
- **Fix**:
    - **Preferred**: Do not modify `GenerationRecord`. Use a local variable or a new DTO for calculation.
    - **Alternative**: Explicitly document this side effect, but better to avoid it.

### 3. 🟡 Overwriting Input Values (Logic)
- **File**: `src/ElectricGenerationParser/Services/ReportGenerator.cs`
- **Issue**: The generator *unconditionally* overwrites `Export` and `Import` based on `Produced - Consumed`.
    - If the input CSV contained pre-calculated Export/Import values, they are lost.
    - If this is intended behavior (NEM calculation rule), it should be explicit.
- **Impact**: Potential data loss if source data evolves.
- **Fix**: Clarify if this calculation is the *only* source of truth. If so, `GenerationRecord` probably shouldn't even have settable Export/Import in the parser, or they should be ignored by the parser.

## Recommendations

1.  **Refactor for Immutability**: Modify `ReportGenerator` to NOT mutate input records. Use a local `CalculatedRecord` struct or simply calculate the values on the fly without storing them back into the source list.
2.  **Enable Negative Testing**: To test the `ValidationException`, extract the validation logic into a `validator` or allow the "Summation" step to be mocked. Alternatively, use Reflection in the test to modify the `GrandTotal` of the report *before* calling validation (if validation was a separate public method, but it's private called at the end).
    *   *Hack/Fix for Test*: Make `ValidateChecksums` internal or protected and test it specifically, or use Reflection in the test to break the `ReportModel` *inside* a subclass or harness if possible. Since it's inside `GenerateReport`, we can't easily intercept.
    *   *Real Fix*: The Checksum validation is checking `ReportModel` integrity. Move verification to `ReportModel.Validate()`? No, `ReportGenerator` is ensuring *its* work is correct.
    *   *Action*: Since we can't break `decimal` math, the only way validation fails is if we "forget" to add to one bucket. We can "Mock" `IRateCalculator` to return an invalid `RateType` that isn't in the Summaries dict?
        - The code initializes `Summaries` for all Enum values.
        - If `_rateCalculator` returns a casted integer not in Enum? `(RateType)999`.
        - The code:
            ```csharp
            if (!report.Summaries.ContainsKey(rateType))
                report.Summaries[rateType] = new MetricSummary();
            ```
            It handles new keys.
            Then `ValidateChecksums` iterates `report.Summaries.Values`.
            It should still sum up correctly.
            So the logic is ROBUST.
            Therefore, the "Exception" is truly for cosmic bit flips.
            **Recommendation**: keep the exception as a sanity check, but mark the test as "Implementation details prevent failure injection without mocking internal state".

## Score
- **Functionality**: 4/5 (Works, but side effects)
- **Quality**: 3/5 (Mutation, untestable path)
- **Tests**: 3/5 (Missing negative test case for AC 2)
