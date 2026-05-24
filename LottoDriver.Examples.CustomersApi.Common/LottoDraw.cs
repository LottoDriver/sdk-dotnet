using System;

namespace LottoDriver.Examples.CustomersApi.Common
{
    public class LottoDraw
    {
        /// <summary>
        /// Betting company's identifier of the lotto draw which
        /// can be unrelated to LottoDriver's identifiers.
        ///
        /// A betting company should have their own lotto and lotto draw identifiers,
        /// which may be simple autoincrement fields (as shown in this example).
        /// That way, a betting company can have lotteries and lotto draws
        /// that aren't connected to LottoDriver's feed.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// This is the betting company's identifier for the Lotto
        /// (this is not LottoDriver's id).
        /// </summary>
        public int LottoId { get; set; }

        /// <summary>
        /// The time when the draw was originally scheduled.
        /// Normally this does not change.
        /// </summary>
        public DateTime ScheduledTimeUtc { get; set; }

        /// <summary>
        /// This is the actual draw time. When the draw is first
        /// published, it will be the same as <see cref="ScheduledTimeUtc"/>.
        /// But if the draw time change is detected, it will be reflected here.
        /// </summary>
        public DateTime DrawTimeUtc { get; set; }

        /// <summary>
        /// Status of the draw (see <see cref="LottoDrawStatus"/>).
        /// </summary>
        public LottoDrawStatus Status { get; set; }

        /// <summary>
        /// Comma separated list of numbers after the result is known.
        /// This is set to null if the result is not known.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Serialized JSON for extra results (bonus, golden balls)
        /// The JSON represents a dictionary of int array, keyed by bonus name.
        /// </summary>
        public string ExtraResult { get; set; }

        /// <summary>
        /// This is LottoDriver's identifier of the draw.
        /// It is set as nullable here to show that a betting companies can
        /// have lotteries and lotto draws that aren't connected to LottoDriver.
        /// </summary>
        public long? LottoDriverDrawId { get; set; }

        /// <summary>
        /// This is a recommendation from LottoDriver when to close betting for this draw.
        /// Betting companies should decide on their own how long will
        /// they keep the betting open and they should do so differently
        /// per each lottery.
        /// </summary>
        public DateTime RecommendedClosingTimeUtc { get; set; }

        /// <summary>
        /// Betting is allowed only if the status is "Published" and "RecommendedClosingTimeUtc" is in the future.
        /// </summary>
        public bool IsBettingAllowed => Status == LottoDrawStatus.Published
                                        && DateTime.UtcNow < RecommendedClosingTimeUtc;
    }
}
