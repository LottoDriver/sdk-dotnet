using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using LottoDriver.CustomersApi.Dto;

namespace LottoDriver.CustomersApi.Sdk
{
    /// <summary>
    /// Delegate signature for <see cref="ICustomersApiClient.DataReceived"/>.
    /// The handler must return <c>true</c> when the data has been persisted and the
    /// sequence pointer should advance, or <c>false</c> to have the same range
    /// re-delivered on the next poll.
    /// </summary>
    /// <param name="source">The client instance that raised the event.</param>
    /// <param name="data">The change set returned by the server.</param>
    public delegate bool DataReceivedHandler(ICustomersApiClient source, DtoLotteriesResponse data);

    /// <summary>
    /// Delegate signature for <see cref="ICustomersApiClient.Error"/> and
    /// <see cref="ICustomersApiClient.CallbackError"/>. The exception is reported
    /// for logging purposes; the client continues running.
    /// </summary>
    /// <param name="source">The client instance that raised the event.</param>
    /// <param name="exception">The captured exception.</param>
    public delegate void ErrorHandler(ICustomersApiClient source, Exception exception);


    /// <summary>
    /// Default implementation of <see cref="ICustomersApiClient"/>.
    /// <para>
    /// The client uses a single <see cref="HttpClient"/> and a <see cref="Timer"/>
    /// that fires once per second. When <see cref="Connect"/> has been called and at
    /// least 15 seconds have passed since the previous poll, the timer callback hits
    /// the change-feed endpoint and raises <see cref="DataReceived"/> on success.
    /// </para>
    /// <para>
    /// OAuth2 client_credentials authentication runs lazily. The bearer token is
    /// cached until shortly before its <c>expires_in</c> deadline, with a 24-hour
    /// fallback if the server omits the field. A <see cref="HttpStatusCode.Unauthorized"/>
    /// response from the change feed invalidates the cached token so the next call
    /// re-authenticates.
    /// </para>
    /// </summary>
    public class CustomersApiClient : ICustomersApiClient
    {
        // ReSharper disable NotAccessedField.Local
        private readonly string _clientId;
        private readonly string _clientSecret;
        // ReSharper restore NotAccessedField.Local

        private readonly Timer _timer;
        private readonly HttpClient _httpClient;

        private int _lastSeqNo;
        private bool _connected;
        private DateTime _lastPollTime = DateTime.MinValue;
        private DateTime _tokenExpiresAt = DateTime.MinValue;

        /// <inheritdoc />
        public event DataReceivedHandler DataReceived;

        /// <inheritdoc />
        public event ErrorHandler CallbackError;

        /// <inheritdoc />
        public event ErrorHandler Error;

        /// <summary>
        /// Constructs a client. Construction does not perform any I/O.
        /// </summary>
        /// <param name="apiUrl">
        /// Base URL of the Customers API, with trailing slash. Defaults to the
        /// production v2 endpoint.
        /// </param>
        /// <param name="clientId">Client id issued by LottoDriver.</param>
        /// <param name="clientSecret">Client secret issued by LottoDriver.</param>
        public CustomersApiClient(string apiUrl = "https://api.lottodriver.com/v2/", string clientId = "", string clientSecret = "")
        {
            _clientId = clientId;
            _clientSecret = clientSecret;

            _httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };

            _timer = new Timer(TimerElapsed);
            _timer.Change(1000, Timeout.Infinite);
        }

        /// <inheritdoc />
        public void Connect(int lastSeqNo)
        {
            _lastSeqNo = lastSeqNo;
            _connected = true;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            _connected = false;
        }

        /// <inheritdoc />
        public async Task<List<DtoLotto>> GetLotteriesAsync()
        {
            if (!IsTokenValid())
            {
                await Authenticate().ConfigureAwait(false);
            }

            using (var response = await _httpClient.GetAsync($"lotteries/active").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dto = JsonConvert.DeserializeObject<DtoLotteriesResponse>(json);

                ConnectHierarchy(dto);

                return dto.Countries.SelectMany(c => c.Lotteries).ToList();
            }
        }

        /// <inheritdoc />
        public async Task<List<DtoLottoDraw>> GetDrawsAsync(int lottoId, DateTime dateFrom, DateTime dateTo)
        {
            var dateFromUtc = dateFrom.ToUniversalTime();
            var dateToUtc = dateTo.ToUniversalTime();

            if (!IsTokenValid())
            {
                await Authenticate().ConfigureAwait(false);
            }

            using (var response = await _httpClient.GetAsync($"lotteries/{lottoId}/draws?dateFrom={dateFromUtc:O}&dateTo={dateToUtc:O}").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dto = JsonConvert.DeserializeObject<DtoLotteriesResponse>(json);

                ConnectHierarchy(dto);

                return dto.Countries
                    .SelectMany(c => c.Lotteries)
                    .SelectMany(l => l.Draws)
                    .ToList();
            }
        }

        /// <inheritdoc />
        public Task<List<DtoLottoDraw>> GetDrawsAsync(int lottoId, DateTime day, TimeZoneInfo timeZoneInfo = null)
        {
            timeZoneInfo = timeZoneInfo ?? TimeZoneInfo.Local;
            day = DateTime.SpecifyKind(day.Date, DateTimeKind.Unspecified);

            var dateFromUtc = TimeZoneInfo.ConvertTimeToUtc(day, timeZoneInfo);
            var dateToUtc = dateFromUtc.AddDays(1);

            return GetDrawsAsync(lottoId, dateFromUtc, dateToUtc);
        }

        /// <inheritdoc />
        public async Task<DtoLottoDraw> GetDrawAsync(long drawId)
        {
            if (!IsTokenValid())
            {
                await Authenticate().ConfigureAwait(false);
            }

            using (var response = await _httpClient.GetAsync($"lotteries/draws/{drawId}").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dto = JsonConvert.DeserializeObject<DtoLotteriesResponse>(json);

                ConnectHierarchy(dto);

                return dto.Countries
                    .SelectMany(c => c.Lotteries)
                    .SelectMany(l => l.Draws)
                    .FirstOrDefault();
            }
        }

        // Timer callback. Runs on a thread pool thread once per second. Decides
        // whether the 15-second poll interval has elapsed, drives the change-feed
        // call, dispatches DataReceived, and reschedules itself.
        private void TimerElapsed(object state)
        {
            try
            {
                if (!_connected) return;

                var utcNow = DateTime.UtcNow;
                if (_lastPollTime.AddSeconds(15) > utcNow) return;
                _lastPollTime = utcNow;

                // Synchronous .Result here is intentional: the timer callback is
                // already off the calling thread, and the polling loop is the only
                // consumer of this task.
                var data = GetLotteriesFromFeed().Result;

                ConnectHierarchy(data);

                if (OnDataReceived(data))
                {
                    _lastSeqNo = data.To;

                    // Catch-up mode: if the server reported a wide change range, the
                    // client is probably behind. Force the next tick to poll again
                    // immediately instead of waiting another 15 seconds.
                    if (data.To - data.From > 500)
                    {
                        _lastPollTime = DateTime.MinValue;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                _timer.Change(1000, Timeout.Infinite);
            }
        }

        // GET /lotteries?lastSeqNo=N. Re-authenticates on 401 so the next call has
        // a fresh token.
        private async Task<DtoLotteriesResponse> GetLotteriesFromFeed()
        {
            if (!IsTokenValid())
            {
                await Authenticate().ConfigureAwait(false);
            }

            using (var response = await _httpClient.GetAsync($"lotteries?lastSeqNo={_lastSeqNo}").ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ResetToken();
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<DtoLotteriesResponse>(json);
            }
        }

        // POST /token with client_credentials. Sets the Authorization header on the
        // shared HttpClient and records the expiry for IsTokenValid().
        private async Task Authenticate()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            using (var response = await _httpClient.PostAsync("token", content).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var tokenResponseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(tokenResponseJson);

                if (string.IsNullOrWhiteSpace(tokenResponse.access_token))
                {
                    ResetToken();
                    throw new Exception("Invalid token");
                }

                // Fall back to 24h if the server did not include expires_in.
                _tokenExpiresAt = tokenResponse.expires_in > 0
                    ? DateTime.UtcNow.AddSeconds(tokenResponse.expires_in)
                    : DateTime.UtcNow.AddHours(24);

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.access_token);
            }
        }

        private void ResetToken()
        {
            _tokenExpiresAt = DateTime.MinValue;
        }

        private bool IsTokenValid()
        {
            return _tokenExpiresAt > DateTime.UtcNow;
        }

        // Populates the back-references (DtoLotto.Country, DtoLottoDraw.Lotto) that
        // are not serialized by the server. After this call, draw.Lotto.Country is
        // reachable from any DtoLottoDraw in the response.
        private void ConnectHierarchy(DtoLotteriesResponse data)
        {
            foreach (var country in data.Countries)
            {
                foreach (var lotto in country.Lotteries)
                {
                    lotto.SetCountry(country);

                    foreach (var draw in lotto.Draws)
                    {
                        draw.SetLotto(lotto);
                    }
                }
            }
        }

        // Invokes the DataReceived handler. Returns the caller's persist-acknowledgement
        // boolean, or false if there is nothing to deliver or the handler threw.
        private bool OnDataReceived(DtoLotteriesResponse data)
        {
            try
            {
                if (data.Countries.Count == 0 && data.From == data.To) return false;

                return DataReceived?.Invoke(this, data) ?? false;
            }
            catch (Exception ex)
            {
                OnCallbackError(ex);
                return false;
            }
        }

        private void OnError(Exception exception)
        {
            try
            {
                Error?.Invoke(this, exception);
            }
            catch (Exception ex)
            {
                OnCallbackError(ex);
            }
        }

        // Final fallback. If the user's CallbackError handler also throws, there is
        // nowhere left to report it; swallow to keep the polling loop alive.
        private void OnCallbackError(Exception exception)
        {
            try
            {
                CallbackError?.Invoke(this, exception);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }
    }
}
