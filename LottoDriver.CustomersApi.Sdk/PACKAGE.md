# LottoDriver Customers API SDK

.NET SDK for the LottoDriver Customers API. Receive lottery draw schedules,
draw-time changes, and draw results from LottoDriver in near real time, or
query them on demand.

The full source, example applications, and integration guide live at
<https://github.com/LottoDriver/sdk-dotnet>.

## Install

```
dotnet add package LottoDriver.CustomersApi.Sdk
```

Targets `netstandard2.0` and `net48`. The DTO assembly is bundled, so no
extra package reference is required.

## Credentials

The SDK authenticates with a `client_id` / `client_secret` pair issued by
LottoDriver. Request credentials at <info@lottodriver.com>.

## Quick start

```csharp
using LottoDriver.CustomersApi.Sdk;
using LottoDriver.CustomersApi.Dto;

var client = new CustomersApiClient(
    apiUrl: "https://api.lottodriver.com/v2/",
    clientId: "your-client-id",
    clientSecret: "your-client-secret"
);

client.Error         += (src, ex) => Console.WriteLine($"client error: {ex.Message}");
client.CallbackError += (src, ex) => Console.WriteLine($"handler error: {ex.Message}");

client.DataReceived += (src, data) =>
{
    foreach (var country in data.Countries)
    foreach (var lotto   in country.Lotteries)
    foreach (var draw    in lotto.Draws)
    {
        Console.WriteLine($"{country.Name} / {lotto.Name} / draw {draw.Id} status={draw.GetStatusType()}");
    }

    // Return true once data is persisted. Returning false re-delivers the
    // same range on the next poll.
    return true;
};

// Pass the last sequence number you have persisted. Use 0 on first run.
client.Connect(lastSeqNo: 0);
```

## Streaming with `Connect` / `DataReceived`

`Connect(int lastSeqNo)` enables the polling loop. Every 15 seconds the SDK
calls the change-feed endpoint and raises `DataReceived` if new data is
available. The handler returns a `bool`:

- `true`: data has been persisted. The SDK advances its internal sequence
  pointer to `data.To`. The next poll requests only changes after that.
- `false`: data was not persisted. The same range is re-delivered on the
  next poll. Use this for at-least-once semantics.

Empty deliveries (no countries, `From == To`) do not raise the event.
`Disconnect()` stops the loop. In-flight requests are not cancelled.

## On-demand queries

These methods work whether or not `Connect` has been called. Each one
authenticates on demand if the cached token has expired.

```csharp
// All currently active lotteries.
List<DtoLotto> active = await client.GetLotteriesAsync();

// Draws for one lottery in an explicit UTC range. Max span 31 days.
// Non-UTC DateTimes are converted to UTC before the request is sent.
List<DtoLottoDraw> range = await client.GetDrawsAsync(
    lottoId: 42,
    dateFrom: DateTime.UtcNow.AddDays(-1),
    dateTo:   DateTime.UtcNow);

// Draws for a single calendar day in a given timezone.
// If timeZoneInfo is null, TimeZoneInfo.Local is used.
List<DtoLottoDraw> today = await client.GetDrawsAsync(
    lottoId: 42,
    day: DateTime.Today,
    timeZoneInfo: TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"));

// One specific draw by id.
DtoLottoDraw one = await client.GetDrawAsync(drawId: 1234567);
```

All returned objects have their `Lotto.Country` and `Draw.Lotto`
back-references populated.

## Sequence numbers and recovery

Every change in the LottoDriver backend gets an integer sequence number.
The feed response contains:

- `From`: the lower bound the server used. Informational.
- `To`: the highest sequence number the server scanned. Persist this.
- `Countries`: the changed objects. Can be empty even when `To` advanced.

The contract:

1. Persist `To` together with the data in the same transaction. If the
   persist fails, do not advance.
2. On process start, read the saved `To` from your storage and pass it to
   `Connect`. Use `0` for a fresh install.
3. The SDK handles reconnection, retries, and token refresh. No manual
   recovery code is required.

When the gap between `From` and `To` exceeds 500, the SDK enters catch-up
mode and polls again immediately instead of waiting another 15 seconds.

## Authentication

The SDK calls `POST /token` with `grant_type=client_credentials` and
caches the returned bearer token until `expires_in` seconds before now,
defaulting to 24 hours if the server does not return `expires_in`. A
`401 Unauthorized` on the change feed clears the cache so the next call
re-authenticates.

## Draw statuses

`DtoLottoDraw.Status` is an `int` so the server can add statuses without
breaking older SDKs. Convert it with `GetStatusType()`:

| Value | Name | Meaning |
|---:|---|---|
| -1 | `Unknown` | Status the SDK does not know. Treat as `UndoCleared`. |
| 0 | `Published` | Announced. Bets allowed only if your own cutoff also permits. |
| 1 | `Unpublished` | Scheduling discrepancy before result. Block bets. |
| 2 | `Cleared` | Result is final. Settle bets. Void bets placed after `DrawTimeUtc`. |
| 3 | `UndoCleared` | A previously cleared draw is in dispute. Reverse settlements or freeze payouts until `Cleared` or `Canceled`. |
| 4 | `Canceled` | Draw did not happen. Refund all bets. |

Any unknown status should be handled as `Unpublished` (block new bets)
until the SDK is updated.

## Errors and events

| Event | When | Caller action |
|---|---|---|
| `DataReceived` | Poll returned non-empty data. | Persist and return `true`. Return `false` to retry. |
| `Error` | Internal poll error (network, HTTP, JSON). | Log only. The SDK retries on the next tick. |
| `CallbackError` | One of the caller's handlers threw. | Log only. |

The SDK never propagates exceptions out of the polling timer. The loop
keeps running until `Disconnect()`.

## DTO reference

| Type | Notable members |
|---|---|
| `DtoLotteriesResponse` | `From`, `To`, `Countries` |
| `DtoCountry` | `Id` (string), `Name`, `Lotteries` |
| `DtoLotto` | `Id` (int), `Country`, `Name`, `NumbersTotal`, `NumbersDrawn`, `Draws` |
| `DtoLottoDraw` | `Id` (long), `Lotto`, `ScheduledTimeUtc`, `DrawTimeUtc`, `RecommendedClosingTimeUtc`, `Status`, `Result`, `ExtraResult` |
| `DtoLottoDrawStatus` | Enum used by `GetStatusType()`. |

All `DateTime` properties returned by the SDK are UTC.

## License and contact

MIT. Source, example apps, and the full guide:
<https://github.com/LottoDriver/sdk-dotnet>.

Questions, credentials, integration help: <info@lottodriver.com>.
