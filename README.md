# LottoDriver Customers API SDK for .NET

This repository contains the .NET SDK for the LottoDriver Customers API, plus a set of example applications that show how to consume it.

The SDK is published on NuGet as `LottoDriver.CustomersApi.Sdk`. Betting companies and other integrators use it to receive lottery draw schedules, draw time changes, and draw results from LottoDriver.

## Contents

- [What the SDK does](#what-the-sdk-does)
- [Repository layout](#repository-layout)
- [Requirements](#requirements)
- [Getting credentials](#getting-credentials)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Streaming data with `Connect` / `DataReceived`](#streaming-data-with-connect--datareceived)
- [On-demand queries (`GetLotteriesAsync`, `GetDrawsAsync`, `GetDrawAsync`)](#on-demand-queries-getlotteriesasync-getdrawsasync-getdrawasync)
- [Sequence numbers and recovery](#sequence-numbers-and-recovery)
- [Authentication](#authentication)
- [Draw statuses](#draw-statuses)
- [Errors and events](#errors-and-events)
- [DTO reference](#dto-reference)
- [Example applications](#example-applications)
- [SQLite database used by the examples](#sqlite-database-used-by-the-examples)
- [Building from source](#building-from-source)
- [Versioning](#versioning)
- [License and contact](#license-and-contact)

## What the SDK does

The Customers API has two kinds of endpoints:

1. A change feed (`GET /lotteries?lastSeqNo=...`) that returns all draws whose state has changed since the caller's last seen sequence number. The SDK polls this endpoint every 15 seconds and raises `DataReceived` when there is new data.
2. Read endpoints for active lotteries, draws in a date range, and individual draws by id. These are exposed as `async` methods on the client.

The SDK handles:

- OAuth2 token acquisition (`client_credentials` grant) and bearer token reuse until expiry.
- Token reset on `401 Unauthorized` and re-authentication on the next call.
- Periodic polling of the change feed once `Connect(lastSeqNo)` has been called.
- Reconnection and retry after transient errors. The caller does not need to write recovery logic.
- Catch-up behavior after long offline periods (the poll interval is collapsed to zero until the feed has been drained).
- Wiring of parent references on returned DTOs so that `draw.Lotto.Country` is populated.

## Repository layout

| Project | Target frameworks | Purpose |
|---|---|---|
| `LottoDriver.CustomersApi.Sdk` | `netstandard2.0`, `net48` | The SDK itself. Published as a NuGet package. |
| `LottoDriver.CustomersApi.Dto` | `netstandard2.0`, `net48` | Data transfer objects returned by the API. Bundled inside the SDK package. |
| `LottoDriver.Examples.CustomersApi.Common` | `netstandard2.0`, `net48` | Sample domain model and a SQLite data access layer used by the example apps. |
| `LottoDriver.Examples.CustomersApi.WorkerService` | `net8.0` | A .NET 8 Generic Host worker. Recommended starting point for new .NET integrations. |
| `LottoDriver.Examples.CustomersApi.WinService` | `net48` | A Windows Service / console hybrid for .NET Framework integrations. |
| `LottoDriver.Examples.CustomersApi.DatabaseViewer` | `net48` | WinForms tool for inspecting the SQLite database that the two services write to. |

Two solution files are provided:

- `LottoDriver.Examples.NetCore.sln` for the .NET 8 worker (Visual Studio 2019 or later).
- `LottoDriver.Examples.NetFramework.sln` for the .NET Framework 4.8 Windows Service and DatabaseViewer (Visual Studio 2017 or later).

## Requirements

- .NET Standard 2.0 compatible runtime to consume the SDK. The package also ships a `net48` build for legacy .NET Framework hosts.
- Newtonsoft.Json 13.0.3 (transitive dependency).
- For the examples that use SQLite: `System.Data.SQLite.Core` 1.0.118 (already declared in the example projects).
- HTTPS egress to `https://api.lottodriver.com/v2/`.

## Getting credentials

The SDK authenticates with a `client_id` and `client_secret` pair issued by LottoDriver. Request credentials at <info@lottodriver.com>. Keep the secret out of source control; the example configuration files leave the values blank for that reason.

## Installation

```
dotnet add package LottoDriver.CustomersApi.Sdk
```

The package bundles `LottoDriver.CustomersApi.Dto.dll`, so the DTO types are available without a separate package reference.

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

    // Return true if the data was persisted successfully. If false is returned, the
    // SDK will not advance its internal sequence pointer and the same range will be
    // delivered again on the next poll.
    return true;
};

// Pass the last sequence number you have persisted. Use 0 on first run.
client.Connect(lastSeqNo: 0);

// ...keep the process alive for as long as you want to receive updates...

client.Disconnect();
```

## Streaming data with `Connect` / `DataReceived`

`Connect(int lastSeqNo)` enables the internal polling loop. It does not perform I/O itself. A `System.Threading.Timer` set up in the constructor wakes every second and decides whether to call the feed. It calls the feed at most once every 15 seconds, except in catch-up mode (see below).

Each successful poll deserializes a `DtoLotteriesResponse` and raises `DataReceived`. The handler returns a `bool`:

- `true` means the caller has persisted the data and the SDK should advance its in-memory `lastSeqNo` to `data.To`. The next poll will request only changes after that point.
- `false` means the caller did not persist the data. The SDK will deliver the same range again on the next poll. Use this to keep the feed at-least-once.

Empty deliveries (no countries, `From == To`) do not raise `DataReceived`.

`Disconnect()` flips an internal flag so the next timer tick skips the poll. In-flight requests are not cancelled.

## On-demand queries (`GetLotteriesAsync`, `GetDrawsAsync`, `GetDrawAsync`)

These methods work whether or not `Connect` has been called. Each one authenticates on demand if the cached token has expired.

```csharp
// All currently active lotteries.
List<DtoLotto> active = await client.GetLotteriesAsync();

// Draws for one lottery in an explicit UTC range. Maximum span is 31 days.
// Non-UTC DateTimes are converted to UTC before the request is sent.
List<DtoLottoDraw> range = await client.GetDrawsAsync(
    lottoId: 42,
    dateFrom: DateTime.UtcNow.AddDays(-1),
    dateTo:   DateTime.UtcNow);

// Draws for one lottery for a single calendar day, in a given timezone.
// If timeZoneInfo is null, TimeZoneInfo.Local is used.
List<DtoLottoDraw> today = await client.GetDrawsAsync(
    lottoId: 42,
    day: DateTime.Today,
    timeZoneInfo: TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"));

// One specific draw by id.
DtoLottoDraw one = await client.GetDrawAsync(drawId: 1234567);
```

All results have their `Lotto.Country` and `Draw.Lotto` back-references populated by the SDK.

## Sequence numbers and recovery

Every change in the LottoDriver backend is assigned an integer sequence number. The feed response always contains:

- `From`: the lower bound the server used for the query.
- `To`: the highest sequence number the server scanned. This is the value the caller must persist.
- `Countries`: the changed objects in the `[From, To]` range. Can be empty even when `To` advanced, because some changes are not relevant to clients.

The contract is:

1. Persist `To` together with the data in the same transaction. If the persist fails, do not advance.
2. On process start, read the saved `To` from your storage and pass it to `Connect`. Use `0` for a fresh install.
3. The SDK takes care of the rest. There is no manual reconnect logic for the caller to write.

If the gap between `From` and `To` exceeds 500 sequence numbers, the SDK switches to catch-up mode: it resets `_lastPollTime` so the next timer tick immediately fires another request. This continues until the gap narrows.

## Authentication

`CustomersApiClient` calls `POST /token` with `grant_type=client_credentials` and caches the returned bearer token until `expires_in` seconds before now, defaulting to 24 hours if the server does not return `expires_in`. Any call (poll or on-demand) that finds the cached token expired re-authenticates first.

A `401 Unauthorized` on the change feed clears the token cache, so the next call will re-authenticate.

## Draw statuses

`DtoLottoDraw.Status` is an `int` so that the server can add statuses without breaking older SDKs. Convert it with the `GetStatusType()` extension method to map onto the `DtoLottoDrawStatus` enum:

| Value | Name | Meaning |
|---:|---|---|
| -1 | `Unknown` | The server returned a status this SDK version does not know. Treat as `UndoCleared`. |
| 0 | `Published` | Draw is announced. Bets are allowed only if your own cutoff time is also satisfied. |
| 1 | `Unpublished` | A scheduling discrepancy was detected before the result. Do not accept bets. |
| 2 | `Cleared` | Result is final. Bets can be settled. Void any bet placed after `DrawTimeUtc`. |
| 3 | `UndoCleared` | A previously cleared draw is in dispute. If bets were already settled, reverse them or freeze payouts until the status moves to `Cleared` or `Canceled`. |
| 4 | `Canceled` | The draw did not happen. Refund all bets on it. |

Any unknown status should be handled as `Unpublished` (block new bets) until the SDK is updated.

## Errors and events

`ICustomersApiClient` exposes three events:

| Event | Raised when | Caller action |
|---|---|---|
| `DataReceived` | A poll returns a non-empty response. | Persist the data and return `true`. Return `false` to retry the same range. |
| `Error` | An internal error happens during a poll (server down, network blip, deserialization failure). | Log only. The SDK will retry on the next tick. |
| `CallbackError` | One of the other handlers threw. | Log only. This exists to surface bugs in caller code. |

The SDK never rethrows exceptions out of the timer callback. The polling loop will keep running until `Disconnect()` is called.

## DTO reference

| Type | Notable members |
|---|---|
| `DtoLotteriesResponse` | `From`, `To`, `Countries` |
| `DtoCountry` | `Id` (LottoDriver id, string), `Name`, `Lotteries` |
| `DtoLotto` | `Id` (int), `Country` (back-reference), `Name`, `NumbersTotal`, `NumbersDrawn`, `Draws` |
| `DtoLottoDraw` | `Id` (long), `Lotto` (back-reference), `ScheduledTimeUtc`, `DrawTimeUtc`, `RecommendedClosingTimeUtc`, `Status`, `Result` (drawn numbers, empty until result is known), `ExtraResult` (named bonus groups, may be null) |
| `DtoLottoDrawStatus` | The enum described above. |
| `DtoLottoDrawExtensions.GetStatusType()` | Converts `Status` to the enum, falling back to `Unknown`. |
| `TokenResponse` | Used internally for `POST /token` responses. |

All `DateTime` properties returned by the SDK are UTC.

## Example applications

### WorkerService (.NET 8)

Open `LottoDriver.Examples.NetCore.sln`. The worker:

1. Reads `LottoDriverApiUrl`, `LottoDriverClientId`, `LottoDriverSecret`, and `DatabasePath` from `appsettings.json`.
2. Registers `ICustomersApiClient` and `IDatabase` in the DI container.
3. On start, opens the SQLite database, runs schema migrations, reads the last persisted sequence number, and calls `Connect`.
4. On each `DataReceived` event, upserts countries, lotteries, and draws in a single transaction together with the new `lastSeqNo`.
5. Demonstrates how to dispatch draw-status transitions to per-status hooks (`HandlePublished`, `HandleCleared`, etc.) where a real betting operator would publish, void, or settle bets.

### WinService (.NET Framework 4.8)

Open `LottoDriver.Examples.NetFramework.sln`. The project is a `ServiceBase` that also runs as an interactive console when launched directly (handy under Visual Studio). It uses `System.Configuration.ConfigurationManager` to read `App.config`. The data-processing logic mirrors the WorkerService.

### DatabaseViewer (WinForms)

A small WinForms tool that opens the same SQLite database and shows the most recent draws (from six hours ago up to five minutes in the future). Use it to confirm that one of the services is writing rows correctly.

## SQLite database used by the examples

The examples persist data to a local SQLite file (default `Database/lotto_driver_examples.db`). The path is configurable in `appsettings.json` (WorkerService) and `App.config` (WinService, DatabaseViewer). If the file does not exist it is created on first run.

`SQLiteDatabase.UpgradeDb()` runs idempotent schema migrations in version order. Current schema (version 4):

```
config (config_key, config_value)
country (id, name, lottodriver_country_id)
lotto (id, country_id, name, numbers_total, numbers_drawn, lottodriver_lotto_id)
lotto_draw (id, lotto_id, scheduled_time_utc, draw_time_utc,
            recommended_closing_time_utc, status,
            result, extra_result, lottodriver_draw_id)
```

The `config` table stores the schema `version` and the `customer_api_last_seq_no` value that the worker hands back to `Connect` on startup.

Note that the LottoDriver ids on local rows are nullable. The example code keeps its own auto-increment ids so a betting company can also have lotteries and draws that do not come from LottoDriver.

## Building from source

```
dotnet restore LottoDriver.Examples.NetCore.sln
dotnet build   LottoDriver.Examples.NetCore.sln -c Release
dotnet run --project LottoDriver.Examples.CustomersApi.WorkerService
```

For the .NET Framework solution, use Visual Studio 2017 or later on Windows, or `msbuild LottoDriver.Examples.NetFramework.sln` from a Developer Command Prompt.

The SDK project sets `GeneratePackageOnBuild=true`. Building it in Release produces a `.nupkg` and a `.snupkg` (symbols).

## Versioning

The SDK uses semantic versioning. The current version is in `LottoDriver.CustomersApi.Sdk.csproj` (`<Version>`). Version 2.0.0 switched the default `apiUrl` to the `/v2/` base path; older 1.x integrations targeted the unversioned base URL.

## License and contact

License: see `LottoDriver.CustomersApi.Sdk/License.txt`.

Questions, credentials, integration help: <info@lottodriver.com>.
