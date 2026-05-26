using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LottoDriver.CustomersApi.Dto;

namespace LottoDriver.CustomersApi.Sdk
{
    /// <summary>
    /// Client for the LottoDriver Customers API.
    /// <para>
    /// The client supports two access patterns:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       A change feed. Call <see cref="Connect"/> once on startup with the last
    ///       sequence number you have persisted. The client polls the server every
    ///       15 seconds and raises <see cref="DataReceived"/> when new data arrives.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       On-demand reads. <see cref="GetLotteriesAsync"/>, <see cref="GetDrawsAsync(int,DateTime,DateTime)"/>,
    ///       <see cref="GetDrawsAsync(int,DateTime,TimeZoneInfo)"/>, and <see cref="GetDrawAsync"/>
    ///       can be called at any time. They authenticate on demand.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The client owns the network and reconnect logic. Callers should not implement
    /// their own retry loop; <see cref="Error"/> and <see cref="CallbackError"/> are
    /// informational and meant for logging only.
    /// </para>
    /// </summary>
    public interface ICustomersApiClient
    {
        /// <summary>
        /// Raised when a user-supplied event handler (for <see cref="DataReceived"/>
        /// or <see cref="Error"/>) throws. The exception is swallowed by the client
        /// and forwarded here so that it can be logged. The client is not in a broken
        /// state and the caller does not need to take recovery action.
        /// </summary>
        event ErrorHandler CallbackError;

        /// <summary>
        /// Raised when a poll of the change feed returns new data. The handler must
        /// return <c>true</c> once the data has been persisted, after which the
        /// client advances its internal sequence pointer to <see cref="DtoLotteriesResponse.To"/>.
        /// Returning <c>false</c> causes the same range to be delivered again on the
        /// next poll.
        /// <para>
        /// Empty responses (no countries, <see cref="DtoLotteriesResponse.From"/> == <see cref="DtoLotteriesResponse.To"/>)
        /// do not trigger this event.
        /// </para>
        /// </summary>
        event DataReceivedHandler DataReceived;

        /// <summary>
        /// Raised when the client's internal poll cycle fails (network problem,
        /// non-success HTTP status, deserialization error). The client will retry on
        /// the next tick; callers should log only and not attempt to reconnect.
        /// </summary>
        event ErrorHandler Error;

        /// <summary>
        /// Starts the change-feed polling loop. Call once on application start with
        /// the last value of <see cref="DtoLotteriesResponse.To"/> that was persisted
        /// to durable storage. Pass <c>0</c> on first run.
        /// <para>
        /// This method does not perform I/O itself; it sets internal state that the
        /// background timer reads. <see cref="Disconnect"/> stops further polls.
        /// </para>
        /// </summary>
        /// <param name="lastSeqNo">
        /// The last sequence number observed in a successful <see cref="DataReceived"/>
        /// callback (the <see cref="DtoLotteriesResponse.To"/> property).
        /// </param>
        void Connect(int lastSeqNo);

        /// <summary>
        /// Stops the change-feed polling loop. In-flight requests are not cancelled.
        /// Typically called once on application shutdown.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Returns all currently active lotteries known to LottoDriver.
        /// The <c>Country</c> back-reference on each returned lottery is populated.
        /// </summary>
        Task<List<DtoLotto>> GetLotteriesAsync();

        /// <summary>
        /// Returns draws for a single lottery within a UTC time range. The maximum
        /// span supported by the server is 31 days.
        /// <para>
        /// If <paramref name="dateFrom"/> or <paramref name="dateTo"/> are of
        /// <see cref="DateTimeKind.Local"/> or <see cref="DateTimeKind.Unspecified"/>
        /// kind, they are converted to UTC via <see cref="DateTime.ToUniversalTime"/>
        /// before the request is built.
        /// </para>
        /// </summary>
        /// <param name="lottoId">LottoDriver lottery id.</param>
        /// <param name="dateFrom">Inclusive lower bound.</param>
        /// <param name="dateTo">Exclusive upper bound.</param>
        Task<List<DtoLottoDraw>> GetDrawsAsync(int lottoId, DateTime dateFrom, DateTime dateTo);

        /// <summary>
        /// Returns draws for a single lottery on a single calendar day in the given
        /// timezone. The time component of <paramref name="day"/> is ignored.
        /// </summary>
        /// <param name="lottoId">LottoDriver lottery id.</param>
        /// <param name="day">The calendar day. Time component is dropped.</param>
        /// <param name="timeZoneInfo">
        /// Timezone used to convert the day's midnight boundaries to UTC. Defaults to
        /// <see cref="TimeZoneInfo.Local"/> if <c>null</c>.
        /// </param>
        Task<List<DtoLottoDraw>> GetDrawsAsync(int lottoId, DateTime day, TimeZoneInfo timeZoneInfo = null);

        /// <summary>
        /// Returns a single draw by its LottoDriver id, or <c>null</c> if not found.
        /// The <c>Lotto</c> and <c>Country</c> back-references on the returned draw
        /// are populated.
        /// </summary>
        /// <param name="drawId">LottoDriver draw id.</param>
        Task<DtoLottoDraw> GetDrawAsync(long drawId);
    }
}
