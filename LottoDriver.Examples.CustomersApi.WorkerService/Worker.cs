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

            _database.BeginTransaction();
            try
            {
                _database.UpgradeDb();

                // Read the last seen sequence number on startup only.
                lastSeqNo = _database.GetLastSeqNo();

                _database.CommitTransaction();
            }
            catch
            {
                _database.RollbackTransaction();
                throw;
            }

            _apiClient.DataReceived += ApiClientOnDataReceived;

            // Connect only once, on startup. The SDK will handle reconnection and recovery.
            _apiClient.Connect(lastSeqNo);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _apiClient.Disconnect();
            _apiClient.DataReceived -= ApiClientOnDataReceived;
        }

        private bool ApiClientOnDataReceived(ICustomersApiClient source, DtoLotteriesResponse data)
        {
            _database.BeginTransaction();
            try
            {
                foreach (var dtoCountry in data.Countries)
                {
                    // Find existing Country in the local (betting company's database).
                    // If the Country does not exist, it will be created.
                    var country = GetOrCreateCountry(dtoCountry);

                    foreach (var dtoLotto in dtoCountry.Lotteries)
                    {
                        // Find existing Lotto in the local (betting company's) database.
                        // If this Lotto does not exist, it will be created.
                        var lotto = GetOrCreateLotto(dtoLotto, country);

                        // A betting company can decide not to create lotteries it does not
                        // already have in their database. In that case, GetOrCreate should return
                        // null, and the processing of the draws will be skipped.
                        if (lotto == null) continue;

                        foreach (var dtoDraw in dtoLotto.Draws)
                        {
                            // Updates an existing LottoDraw in the local (betting company's) database.
                            // If the draw does not exist it will be created.
                            UpdateOrCreateDraw(dtoDraw, lotto);
                        }
                    }
                }

                // Write the last sequence number seen to the database.
                _database.SetLastSeqNo(data.To);

                _database.CommitTransaction();
            }
            catch
            {
                _database.RollbackTransaction();
                throw;
            }

            // Data object hierarchy is bi-directionally connected. If the hierarchy is to be serialized, the simplest way is to set loop handling to ignore.
            // Alternatively, read-only properties could be ignored since references from child to parent are not publicly writable.
            _logger.LogWarning("Data received: {0}", JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));

            // return true on successful processing, false otherwise
            // write data.To value as last seq no to DB
            return true;
        }

        /// <summary>
        /// Gets an existing Country instance from the local database,
        /// or creates a new row in the database with data from LottoDriver's dto.
        /// </summary>
        /// <param name="dtoCountry">LottoDriver's DataTransferObject instance for the country</param>
        /// <returns>Local (betting company's) Country as saved in the local database</returns>
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
        /// Gets an existing Lotto instance from the local database,
        /// or creates a new row in the local database with data from LottoDriver's dto.
        /// </summary>
        /// <param name="dtoLotto">LottoDriver's DataTransferObject (dto) instance for the lottery</param>
        /// <param name="country">Local (betting company's) representation of the country where this lottery should belong</param>
        /// <returns>Local (betting company's) Lottery as saved in the local database</returns>
        private Lotto GetOrCreateLotto(DtoLotto dtoLotto, Country country)
        {
            var lotto = _database.LottoFindByLottoDriverId(dtoLotto.Id);
            if (lotto == null)
            {
                lotto = new Lotto
                {
                    CountryId = country.Id, // connect to local (betting company's) country id
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
        /// Updates an existing LottoDraw instance in the local database,
        /// or creates a new row in the local database with data from LottoDriver's dto.
        /// </summary>
        /// <param name="dtoDraw">LottoDriver's DataTransferObject (dto) instance for the lotto draw</param>
        /// <param name="lotto">Local (betting company's) representation of the Lottery where this draw should belong</param>
        private void UpdateOrCreateDraw(DtoLottoDraw dtoDraw, Lotto lotto)
        {
            bool isStatusChanged;

            // Find the draw in the local (betting company's) database 
            // by providing LottoDriver's identifier.
            var draw = _database.LottoDrawFindByLottoDriverId(dtoDraw.Id);

            if (draw == null)
            {
                isStatusChanged = true;

                draw = new LottoDraw
                {
                    LottoId = lotto.Id, // connect to local (betting company's) lotto id
                    ScheduledTimeUtc = dtoDraw.ScheduledTimeUtc,
                    DrawTimeUtc = dtoDraw.DrawTimeUtc,
                    RecommendedClosingTimeUtc = dtoDraw.RecommendedClosingTimeUtc,
                    Status = (LottoDrawStatus)dtoDraw.Status,
                    LottoDriverDrawId = dtoDraw.Id,
                    Result = dtoDraw.Result.Count > 0 ? string.Join(",", dtoDraw.Result) : null
                };

                // insert the new draw in the local database
                _database.LottoDrawInsert(draw);
            }
            else
            {
                isStatusChanged = draw.Status != (LottoDrawStatus) dtoDraw.Status;

                draw.DrawTimeUtc = dtoDraw.DrawTimeUtc;
                draw.RecommendedClosingTimeUtc = dtoDraw.RecommendedClosingTimeUtc;
                draw.Status = (LottoDrawStatus)dtoDraw.Status;
                draw.Result = dtoDraw.Result.Count > 0 ? string.Join(",", dtoDraw.Result) : null;
                draw.ExtraResult = dtoDraw.ExtraResult != null 
                    ? System.Text.Json.JsonSerializer.Serialize(dtoDraw.ExtraResult)
                    : null;

                // update the draw in the local database
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
                        // Betting Companies should handle any other status
                        // that isn't listed here as "Unpublished".
                        HandleUnpublished(draw);
                        break;
                }
            }
        }

        private void HandlePublished(LottoDraw draw)
        {
            // TODO: To be implemented by the customer (Betting Company).
            // Normally, the draw would be marked for publishing to all distribution channels.
        }

        private void HandleUnpublished(LottoDraw draw)
        {
            // TODO: To be implemented by the customer (Betting Company).
            // Normally, this draw would be marked as blocked for further betting.
        }

        private void HandleCleared(LottoDraw draw)
        {
            // TODO: To be implemented by the customer (Betting Company).
            // This means the result has arrived and the bets can be cleared as winning or losing.
        }

        private void HandleUndoCleared(LottoDraw draw)
        {
            // TODO: To be implemented by the customer (Betting Company).
            // This means that previously cleared bets (marked as winning or losing) should
            // be reverted to "not cleared" state. If that is not possible, at least payouts
            // of winning bets should be blocked until the draw transitions to 
            // either Cleared or Cancel state.
        }

        private void HandleCanceled(LottoDraw draw)
        {
            // TODO: To be implemented by the customer (Betting Company).
            // Lottery draw did not take place.
            // All bets for this draw should be voided (money returned to players)
        }
    }
}
