using System;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;

using Newtonsoft.Json;

using LottoDriver.CustomersApi.Dto;
using LottoDriver.CustomersApi.Sdk;

using LottoDriver.Examples.CustomersApi.Common;
using LottoDriver.Examples.CustomersApi.Common.DataAccess;

namespace LottoDriver.Examples.CustomersApi.WinService
{
    /// <summary>
    /// .NET Framework example service. Reads configuration from <c>App.config</c>,
    /// constructs the SDK client and the SQLite database directly (no DI), and
    /// processes feed updates exactly the same way the .NET 8 worker does.
    /// <para>
    /// The class extends <see cref="ServiceBase"/>, so <c>OnStart</c> and
    /// <c>OnStop</c> are invoked by the Windows Service Control Manager. The
    /// <see cref="ConsoleStart"/> shim lets the same instance also run from the
    /// console for development.
    /// </para>
    /// </summary>
    public partial class Service1 : ServiceBase
    {
        private readonly ICustomersApiClient _apiClient;
        private readonly IDatabase _database;

        public Service1()
        {
            InitializeComponent();

            _database = new SQLiteDatabase(ConfigurationManager.AppSettings["DatabasePath"]);

            _apiClient = new CustomersApiClient(
                ConfigurationManager.AppSettings["LottoDriverApiUrl"],
                ConfigurationManager.AppSettings["LottoDriverClientId"],
                ConfigurationManager.AppSettings["LottoDriverSecret"]
            );

            // Replace these console writers with proper logging in production
            // integrations. The SDK keeps running regardless of what these do.
            _apiClient.Error += (source, exception) => Console.WriteLine(exception.Message);
            _apiClient.CallbackError += (source, exception) => Console.WriteLine(exception.Message);
            _apiClient.DataReceived += ApiClientOnDataReceived;
        }

        protected override void OnStart(string[] args)
        {
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

            // Connect is called once. The SDK handles reconnect and recovery.
            _apiClient.Connect(lastSeqNo);
        }

        protected override void OnStop()
        {
            _apiClient.Disconnect();
        }

        /// <summary>
        /// Forwards to <see cref="OnStart"/>. Used by <c>Program.Main</c> when the
        /// process is launched interactively, because <c>OnStart</c> is protected.
        /// </summary>
        public void ConsoleStart(string[] args)
        {
            OnStart(args);
        }

        /// <summary>
        /// SDK callback. Persists the delivered hierarchy and the new sequence
        /// number in one transaction. See the .NET 8 <c>Worker.ApiClientOnDataReceived</c>
        /// for the equivalent commentary; this implementation is intentionally a
        /// near-copy so the two examples are easy to compare.
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

                        // Returning null from GetOrCreate is the hook for skipping
                        // lotteries the betting company does not carry.
                        if (lotto == null) continue;

                        foreach (var dtoDraw in dtoLotto.Draws)
                        {
                            UpdateOrCreateDraw(dtoDraw, lotto);
                        }
                    }
                }

                _database.SetLastSeqNo(data.To);

                _database.CommitTransaction();
            }
            catch
            {
                _database.RollbackTransaction();
                throw;
            }

            // The DTO graph is bi-directionally linked. Ignore reference loops on
            // serialisation, or alternatively ignore the read-only navigation
            // properties.
            Console.WriteLine(JsonConvert.SerializeObject(data, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
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
        /// none exists. The new row is connected to the local <see cref="Country"/>.
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
        /// Updates an existing draw, or inserts a new one. Detects status
        /// transitions and dispatches the matching <c>Handle...</c> hook.
        /// </summary>
        private void UpdateOrCreateDraw(DtoLottoDraw dtoDraw, Lotto lotto)
        {
            bool isStatusChanged;

            var draw = _database.LottoDrawFindByLottoDriverId(dtoDraw.Id);

            if (draw == null)
            {
                isStatusChanged = true;

                draw = new LottoDraw
                {
                    LottoId = lotto.Id,
                    ScheduledTimeUtc = dtoDraw.ScheduledTimeUtc,
                    DrawTimeUtc = dtoDraw.DrawTimeUtc,
                    RecommendedClosingTimeUtc = dtoDraw.RecommendedClosingTimeUtc,
                    Status = (LottoDrawStatus)dtoDraw.Status,
                    LottoDriverDrawId = dtoDraw.Id,
                    Result = dtoDraw.Result.Count > 0
                        ? dtoDraw.Result.Aggregate("", (acc, num) => acc + "," + num)
                            .Substring(1) // drop the leading comma added by Aggregate
                        : null
                };

                _database.LottoDrawInsert(draw);
            }
            else
            {
                isStatusChanged = draw.Status != (LottoDrawStatus) dtoDraw.Status;

                draw.DrawTimeUtc = dtoDraw.DrawTimeUtc;
                draw.RecommendedClosingTimeUtc = dtoDraw.RecommendedClosingTimeUtc;
                draw.Status = (LottoDrawStatus)dtoDraw.Status;
                draw.Result = dtoDraw.Result.Count > 0
                    ? dtoDraw.Result.Aggregate("", (acc, num) => acc + "," + num)
                        .Substring(1)
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
                        // Conservatively treat unknown statuses as Unpublished
                        // (block bets) until the SDK is upgraded.
                        HandleUnpublished(draw);
                        break;
                }
            }
        }

        // Per-status hooks. Fill these in for a real integration.

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
