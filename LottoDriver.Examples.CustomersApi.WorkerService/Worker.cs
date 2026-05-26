using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using LottoDriver.CustomersApi.Dto;
using LottoDriver.CustomersApi.Sdk;

using LottoDriver.Examples.CustomersApi.Common;
using LottoDriver.Examples.CustomersApi.Common.DataAccess;

namespace LottoDriver.Examples.CustomersApi.WorkerService
{
    /// <summary>
    /// Hosted service that subscribes to the LottoDriver change feed and persists
    /// every delivered batch to a local SQLite database.
    /// <para>
    /// Lifecycle:
    /// </para>
    /// <list type="number">
    ///   <item><description>On <see cref="ExecuteAsync"/>, the SQLite schema is migrated and the persisted <c>lastSeqNo</c> is loaded.</description></item>
    ///   <item><description><see cref="ICustomersApiClient.Connect"/> is called once; from that point the SDK polls every 15 seconds.</description></item>
    ///   <item><description>Each <see cref="ICustomersApiClient.DataReceived"/> callback writes the new countries, lotteries, draws, and <c>lastSeqNo</c> in a single transaction.</description></item>
    ///   <item><description>On shutdown (cancellation token), <c>Disconnect()</c> is called and the event handler is detached.</description></item>
    /// </list>
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ICustomersApiClient _apiClient;
        private readonly IDatabase _database;
        private readonly ILogger<Worker> _logger;

        public Worker(ICustomersApiClient apiClient, IDatabase database, ILogger<Worker> logger)
        {
            _apiClient = apiClient;
            _database = database;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            int lastSeqNo;

            // Startup transaction: migrate the schema and load the resume point.
            _database.BeginTransaction();
            try
            {
                _database.UpgradeDb();

                lastSeqNo = _database.GetLastSeqNo();

                _database.CommitTransaction();
            }
            catch
            {
                _database.RollbackTransaction();
                throw;
            }

            _apiClient.DataReceived += ApiClientOnDataReceived;

            // Connect is called once. The SDK handles reconnect, token refresh, and
            // catch-up polling without further input from this code.
            _apiClient.Connect(lastSeqNo);

            // Idle loop. The actual work runs inside the SDK's polling timer and
            // dispatches through ApiClientOnDataReceived.
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _apiClient.Disconnect();
            _apiClient.DataReceived -= ApiClientOnDataReceived;
        }

        /// <summary>
        /// SDK callback. Upserts the delivered hierarchy into the local SQLite
        /// database, persists the new <c>lastSeqNo</c>, and dispatches per-status
        /// hooks on draws whose status changed.
        /// <para>
        /// Returns <c>true</c> on a successful commit, which advances the SDK's
        /// sequence pointer. If the transaction rolls back, the exception bubbles
        /// out of the SDK timer (it will be reported on the <c>Error</c> event)
        /// and the same range will be redelivered on the next poll.
        /// </para>
        /// </summary>
        private bool ApiClientOnDataReceived(ICustomersApiClient source, DtoLotteriesResponse data)
        {
            _database.BeginTransaction();
            try
            {
                foreach (var dtoCountry in data.Countries)
                {
                    var country = GetOrCreateCountry(dtoCountry);

                    foreach (var dtoLotto in dtoCountry.Lotteries)
                    {
                        var lotto = GetOrCreateLotto(dtoLotto, country);

                        // A betting company may choose to skip lotteries it does
                        // not carry. GetOrCreate returning null is the hook for
                        // that decision; if null, the draws inside are ignored.
                        if (lotto == null) continue;

                        foreach (var dtoDraw in dtoLotto.Draws)
                        {
                            UpdateOrCreateDraw(dtoDraw, lotto);
                        }
                    }
                }

                // Persist the resume point in the same transaction as the data.
                _database.SetLastSeqNo(data.To);

                _database.CommitTransaction();
            }
            catch
            {
                _database.RollbackTransaction();
                throw;
            }

            // The DTO graph is bi-directionally linked (DtoLotto.Country and
            // DtoLottoDraw.Lotto are populated by the SDK). Disable reference loop
            // serialization or ignore the read-only navigation properties when
            // logging the payload.
            _logger.LogWarning("Data received: {0}", JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));

            return true;
        }

        /// <summary>
        /// Looks up a country by LottoDriver id, inserting a new local row if
        /// none exists yet.
        /// </summary>
        private Country GetOrCreateCountry(DtoCountry dtoCountry)
        {
            var country = _database.CountryFindByLottoDriverId(dtoCountry.Id);
            if (country == null)
            {
                country = new Country
                {
                    Name = dtoCountry.Name,
                    LottoDriverCountryId = dtoCountry.Id
                };

                _database.CountryInsert(country);
            }

            return country;
        }

        /// <summary>
        /// Looks up a lottery by LottoDriver id, inserting a new local row if
        /// none exists. The new row is connected to the local <see cref="Country"/>
        /// already resolved by <see cref="GetOrCreateCountry"/>.
        /// </summary>
        private Lotto GetOrCreateLotto(DtoLotto dtoLotto, Country country)
        {
            var lotto = _database.LottoFindByLottoDriverId(dtoLotto.Id);
            if (lotto == null)
            {
                lotto = new Lotto
                {
                    CountryId = country.Id,
                    Name = dtoLotto.Name,
                    NumbersDrawn = dtoLotto.NumbersDrawn,
                    NumbersTotal = dtoLotto.NumbersTotal,
                    LottoDriverLottoId = dtoLotto.Id
                };

                _database.LottoInsert(lotto);
            }

            return lotto;
        }

        /// <summary>
        /// Updates an existing draw in the local database, or inserts a new one.
        /// Detects status transitions and dispatches the matching <c>Handle...</c>
        /// hook (intended to be filled in by integrators).
        /// </summary>
        private void UpdateOrCreateDraw(DtoLottoDraw dtoDraw, Lotto lotto)
        {
            bool isStatusChanged;

            var draw = _database.LottoDrawFindByLottoDriverId(dtoDraw.Id);

            if (draw == null)
            {
                // First time we see this draw: treat as a status transition so
                // that the per-status hook fires for the initial state too.
                isStatusChanged = true;

                draw = new LottoDraw
                {
                    LottoId = lotto.Id,
                    ScheduledTimeUtc = dtoDraw.ScheduledTimeUtc,
                    DrawTimeUtc = dtoDraw.DrawTimeUtc,
                    RecommendedClosingTimeUtc = dtoDraw.RecommendedClosingTimeUtc,
                    Status = (LottoDrawStatus)dtoDraw.Status,
                    LottoDriverDrawId = dtoDraw.Id,
                    Result = dtoDraw.Result.Count > 0 ? string.Join(",", dtoDraw.Result) : null
                };

                _database.LottoDrawInsert(draw);
            }
            else
            {
                isStatusChanged = draw.Status != (LottoDrawStatus) dtoDraw.Status;

                // ScheduledTimeUtc is intentionally not refreshed: it is stable by
                // contract. Everything else can move.
                draw.DrawTimeUtc = dtoDraw.DrawTimeUtc;
                draw.RecommendedClosingTimeUtc = dtoDraw.RecommendedClosingTimeUtc;
                draw.Status = (LottoDrawStatus)dtoDraw.Status;
                draw.Result = dtoDraw.Result.Count > 0 ? string.Join(",", dtoDraw.Result) : null;
                draw.ExtraResult = dtoDraw.ExtraResult != null
                    ? System.Text.Json.JsonSerializer.Serialize(dtoDraw.ExtraResult)
                    : null;

                _database.LottoDrawUpdate(draw);
            }

            if (isStatusChanged)
            {
                switch (draw.Status)
                {
                    case LottoDrawStatus.Published:
                        HandlePublished(draw);
                        break;
                    case LottoDrawStatus.Unpublished:
                        HandleUnpublished(draw);
                        break;
                    case LottoDrawStatus.Cleared:
                        HandleCleared(draw);
                        break;
                    case LottoDrawStatus.UndoCleared:
                        HandleUndoCleared(draw);
                        break;
                    case LottoDrawStatus.Canceled:
                        HandleCanceled(draw);
                        break;
                    default:
                        // Unknown statuses are conservatively treated as Unpublished
                        // (block bets) until the SDK is upgraded.
                        HandleUnpublished(draw);
                        break;
                }
            }
        }

        // Per-status hooks. Integrators fill these in with the side effects their
        // platform requires (publish to channels, void bets, settle bets, etc.).
        // They run inside the persist transaction; throw to abort the batch.

        private void HandlePublished(LottoDraw draw)
        {
            // TODO: Publish the draw to all betting channels.
        }

        private void HandleUnpublished(LottoDraw draw)
        {
            // TODO: Block further bets on this draw.
        }

        private void HandleCleared(LottoDraw draw)
        {
            // TODO: Settle the bets using draw.Result.
        }

        private void HandleUndoCleared(LottoDraw draw)
        {
            // TODO: Reverse previously-settled bets if possible. Otherwise freeze
            // payouts until the draw transitions to Cleared or Canceled.
        }

        private void HandleCanceled(LottoDraw draw)
        {
            // TODO: Void all bets on this draw and refund stakes.
        }
    }
}
