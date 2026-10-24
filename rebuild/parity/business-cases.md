# Parity – business contract (check strictly)

Each case becomes an acceptance test with a frozen time source ("now"). The expected values come from the original's tests (`test_server.py`, `test_protocol.py`).

## B-NOW Current time

| ID | now (UTC) | `timezone` | `datetime` | `day_of_week` | `is_dst` |
|---|---|---|---|---|---|
| B-NOW-1 | 2024-01-01 12:00:00 | UTC | 2024-01-01T12:00:00+00:00 | Monday | false |
| B-NOW-2 | 2024-01-01 12:00:00 | Europe/Warsaw | 2024-01-01T13:00:00+01:00 | Monday | false |
| B-NOW-3 | 2024-07-01 12:00:00 | Europe/Warsaw | 2024-07-01T14:00:00+02:00 | Monday | true |
| B-NOW-4 | 2024-01-01 12:00:00 | America/New_York | 2024-01-01T07:00:00-05:00 | Monday | false |
| B-NOW-5 | 2024-01-01 12:00:00 | Australia/Sydney | 2024-01-01T23:00:00+11:00 | Monday | true |
| B-NOW-6 | 2024-07-01 12:00:00 | Australia/Sydney | 2024-07-01T22:00:00+10:00 | Monday | false |
| B-NOW-7 | 2024-03-10 06:59:59 | America/New_York | 2024-03-10T01:59:59-05:00 | Sunday | false |
| B-NOW-8 | 2024-03-10 07:00:00 | America/New_York | 2024-03-10T03:00:00-04:00 | Sunday | true |
| B-NOW-9 | 2024-10-27 00:30:00 | Europe/London | 2024-10-27T01:30:00+01:00 | Sunday | true |
| B-NOW-10 | 2024-10-27 01:30:00 | Europe/London | 2024-10-27T01:30:00+00:00 | Sunday | false |
| B-NOW-11 | 2024-01-01 12:00:00 | Australia/Lord_Howe | 2024-01-01T23:00:00+11:00 | Monday | true |
| B-NOW-12 | 2024-01-01 12:00:00 | Asia/Kathmandu | 2024-01-01T17:45:00+05:45 | Monday | false |
| B-NOW-13 | 2024-01-01 12:00:00 | Pacific/Kiritimati | 2024-01-02T02:00:00+14:00 | Tuesday | false |
| B-NOW-14 | 2024-01-01 05:00:00 | Pacific/Pago_Pago | 2023-12-31T18:00:00-11:00 | Sunday | false |
| B-NOW-15 | 2024-07-01 12:00:00 | Etc/GMT+5 | 2024-07-01T07:00:00-05:00 | Monday | false |

Note on B-NOW-11: Lord Howe has a 30-minute daylight saving shift. `is_dst` is still true there.

## B-CONV Conversion

| ID | now (UTC) | source | `time` | target | `source.datetime` | `target.datetime` | `target.is_dst` | `time_difference` |
|---|---|---|---|---|---|---|---|---|
| B-CONV-1 | 2024-01-01 00:00 | Europe/London | 12:00 | Europe/Warsaw | 2024-01-01T12:00:00+00:00 | 2024-01-01T13:00:00+01:00 | false | +1.0h |
| B-CONV-2 | 2024-01-01 00:00 | UTC | 12:00 | UTC | 2024-01-01T12:00:00+00:00 | 2024-01-01T12:00:00+00:00 | false | +0.0h |
| B-CONV-3 | 2024-01-01 00:00 | UTC | 12:00 | Asia/Kathmandu | 2024-01-01T12:00:00+00:00 | 2024-01-01T17:45:00+05:45 | false | +5.75h |
| B-CONV-4 | 2024-01-01 00:00 | UTC | 12:00 | Asia/Kolkata | 2024-01-01T12:00:00+00:00 | 2024-01-01T17:30:00+05:30 | false | +5.5h |
| B-CONV-5 | 2024-01-01 00:00 | Asia/Kathmandu | 12:00 | Europe/Warsaw | 2024-01-01T12:00:00+05:45 | 2024-01-01T07:15:00+01:00 | false | -4.75h |
| B-CONV-6 | 2024-10-28 00:00 | Europe/Warsaw | 12:00 | America/New_York | 2024-10-28T12:00:00+01:00 | 2024-10-28T07:00:00-04:00 | true | -5.0h |
| B-CONV-7 | 2024-01-01 00:00 | Europe/Warsaw | 23:00 | Pacific/Kiritimati | 2024-01-01T23:00:00+01:00 | 2024-01-02T12:00:00+14:00 | false | +13.0h |
| B-CONV-8 | 2024-01-01 00:00 | UTC | 05:00 | Pacific/Pago_Pago | 2024-01-01T05:00:00+00:00 | 2023-12-31T18:00:00-11:00 | false | -11.0h |
| B-CONV-9 | 2024-01-01 00:00 | UTC | 9:5 | UTC | 2024-01-01T09:05:00+00:00 | 2024-01-01T09:05:00+00:00 | false | +0.0h |
| B-CONV-10 | 2024-01-01 00:00 | UTC | 00:00 | UTC | 2024-01-01T00:00:00+00:00 | 2024-01-01T00:00:00+00:00 | false | +0.0h |
| B-CONV-11 | 2024-01-01 00:00 | UTC | 23:59 | UTC | 2024-01-01T23:59:00+00:00 | 2024-01-01T23:59:00+00:00 | false | +0.0h |

B-CONV-6 shows: On 28 October, Europe has already left daylight saving time and the US has not. The difference is therefore -5.0h instead of -6.0h.

## B-EDGE Reference date, clock change

| ID | Case | Expected |
|---|---|---|
| B-EDGE-1 | now 2024-01-01 20:00Z, source Asia/Tokyo → UTC | `source.day_of_week` = Tuesday (it is already the 2nd in Tokyo) |
| B-EDGE-2 | now 2024-03-10 12:00Z, `02:30` America/New_York → UTC | `isError`; text names `02:30`, `America/New_York`, `2024-03-10` and says the time is skipped by a clock change |
| B-EDGE-3 | now 2024-11-03 12:00Z, `01:30` America/New_York → UTC | `source` = `2024-11-03T01:30:00-04:00`, Sunday, `is_dst` true; `time_difference` +4.0h |
| B-EDGE-4 | The difference uses the offsets at the converted moment, not those of "now" (Europe/London, day of the clock change 2024-10-27) | Difference matches the converted moment |

## B-ERR Business errors (check the information, not the wording)

| ID | Call | Expected (`isError: true`, text contains …) |
|---|---|---|
| B-ERR-1 | `get_current_time{timezone:""}` | "Missing required argument: timezone" |
| B-ERR-2 | `get_current_time{}` | "Missing required argument: timezone"; no fallback to the local zone |
| B-ERR-3 | `get_current_time{timezone:"Invalid/Timezone"}` | "Invalid timezone" and `Invalid/Timezone` |
| B-ERR-4 | `get_current_time{timezone:"europe/warsaw"}` | "Invalid timezone" and `europe/warsaw` (on all platforms) |
| B-ERR-5 | `get_current_time{timezone:"../etc/passwd"}` and `"/etc/localtime"` | "Invalid timezone", no file access |
| B-ERR-6 | `convert_time` with `time:"25:00"` or `"12:00 PM"` | "Invalid time format. Expected HH:MM [24-hour format]" |
| B-ERR-7 | `convert_time` with an empty source or an empty target | "Missing required argument: source_timezone" or "… target_timezone" |
| B-ERR-8 | `convert_time` with an invalid source or an invalid target | "Invalid timezone" and the value |

## B-NEW Improvements (new, no counterpart in the original)

| ID | Call | Expected |
|---|---|---|
| B-NEW-1 | now 2024-01-01 00:00Z, `convert_time{Europe/Vienna, "09:00", America/New_York, date:"2024-07-15"}` | source `2024-07-15T09:00:00+02:00`, target `2024-07-15T03:00:00-04:00`, both `is_dst` true, difference -6.0h |
| B-NEW-2 | `date:"2024-02-30"` or `"15.07.2024"` | `isError`; text names the expected format YYYY-MM-DD |
| B-NEW-3 | without `date` | Behaves like B-CONV (today in the source zone) |
