# Parity – what must be preserved

The original is the template, not the yardstick. These cases make sure the rebuild loses nothing that users and clients rely on. Everything else may get better.

## Three kinds

| Kind | File | In the rebuild |
|---|---|---|
| Business contract | `business-cases.md` (times, offsets, daylight saving time, error cases, new `date` cases) | check strictly |
| Interface contract | `interface-cases.md` (protocol, tool list, schemas, result shape, CLI) | keep compatible; change only on purpose, and record it in the deviation log |
| Tied to the implementation | – | do not carry over: wording of the Python error messages, pydantic schema titles, Python entry points |

## From case to test

- **Business cases**: unit tests against the business logic, "now" frozen with `FakeTimeProvider`, one table row = one test case (parameterized).
- **Interface cases**: protocol tests with the SDK's MCP client, which starts the built server over stdio, once per protocol generation.
- **Error texts**: The test checks that the named information is present (field name, value, expected format), not the exact sentence.
- The original is neither built nor started. All expected values are in these files.

## Normalization

Not needed for business cases (frozen time). In protocol tests: server version → `<VERSION>`.

## Deviation log

`deviations.md`: case | original (per the analysis) | rebuild | verdict (bug in the rebuild / improvement on purpose + PRD reference).
