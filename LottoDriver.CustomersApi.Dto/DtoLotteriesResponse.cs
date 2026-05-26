using System.Collections.Generic;

namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// Response envelope returned by every endpoint of the Customers API.
    /// Carries a window of changes, identified by the sequence-number pair
    /// <see cref="From"/> / <see cref="To"/>, plus the affected countries and
    /// their nested lotteries and draws.
    /// </summary>
    public class DtoLotteriesResponse
    {
        /// <summary>
        /// Lower bound (inclusive) of the sequence-number range that was scanned by
        /// the server to produce this response. Informational; clients normally do
        /// not need to read this value.
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// Upper bound (inclusive) of the sequence-number range that was scanned.
        /// This is the value the client must persist together with the data, and
        /// pass back as <c>lastSeqNo</c> on the next session via
        /// <c>ICustomersApiClient.Connect</c> (in <c>LottoDriver.CustomersApi.Sdk</c>).
        /// <para>
        /// <see cref="To"/> can advance even when <see cref="Countries"/> is empty.
        /// This happens when changes occurred that are not visible to clients.
        /// Persist the new <see cref="To"/> anyway.
        /// </para>
        /// </summary>
        public int To { get; set; }

        /// <summary>
        /// Countries whose lotteries or draws changed within the
        /// <see cref="From"/> - <see cref="To"/> range. Unchanged countries are not
        /// returned.
        /// </summary>
        public List<DtoCountry> Countries { get; set; }
    }
}
