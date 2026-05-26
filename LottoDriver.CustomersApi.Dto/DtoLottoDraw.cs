using System;
using System.Collections.Generic;

namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// A single scheduled or completed lottery draw.
    /// </summary>
    public class DtoLottoDraw
    {
        /// <summary>
        /// LottoDriver's draw identifier. Stable for the lifetime of the draw.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Lottery this draw belongs to. Populated by the SDK after the response is
        /// parsed; never serialized to JSON to avoid cyclic references.
        /// </summary>
        public DtoLotto Lotto { get; private set; }

        /// <summary>
        /// When the draw was originally scheduled to take place. Normally stable;
        /// rescheduling is reflected on <see cref="DrawTimeUtc"/>.
        /// </summary>
        public DateTime ScheduledTimeUtc { get; set; }

        /// <summary>
        /// When the draw actually happened. Equal to <see cref="ScheduledTimeUtc"/>
        /// until LottoDriver detects a change (delay, early draw, reschedule). Bets
        /// placed after this time on a cleared draw should be voided.
        /// </summary>
        public DateTime DrawTimeUtc { get; set; }

        /// <summary>
        /// Recommended cutoff for accepting bets on this draw. Each integrator is
        /// free to enforce a stricter cutoff. Treat this as an upper bound, not as
        /// an authoritative betting deadline.
        /// </summary>
        public DateTime RecommendedClosingTimeUtc { get; set; }

        /// <summary>
        /// Draw status as an integer. Kept as <see cref="int"/> rather than
        /// <see cref="DtoLottoDrawStatus"/> so that the server can introduce new
        /// statuses without breaking older SDK clients.
        /// <para>
        /// Use <see cref="DtoLottoDrawExtensions.GetStatusType"/> to map the value
        /// onto the enum, with safe fall-back to <see cref="DtoLottoDrawStatus.Unknown"/>.
        /// See <see cref="DtoLottoDrawStatus"/> for the semantic meaning of each
        /// status.
        /// </para>
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Drawn numbers, in draw order. Empty until the result is known. Length is
        /// typically <see cref="DtoLotto.NumbersDrawn"/>.
        /// </summary>
        public List<int> Result { get; set; }

        /// <summary>
        /// Named groups of extra balls (for example "bonus", "golden", "joker"),
        /// keyed by group name. <c>null</c> if the lottery has no extra balls or
        /// the result is not known yet.
        /// </summary>
        public Dictionary<string, int[]> ExtraResult { get; set; }

        internal void SetLotto(DtoLotto lotto)
        {
            Lotto = lotto;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="DtoLottoDraw"/>.
    /// </summary>
    public static class DtoLottoDrawExtensions
    {
        /// <summary>
        /// Maps <see cref="DtoLottoDraw.Status"/> onto <see cref="DtoLottoDrawStatus"/>.
        /// Returns <see cref="DtoLottoDrawStatus.Unknown"/> if <paramref name="draw"/>
        /// is <c>null</c> or if its <c>Status</c> value is one this SDK version does
        /// not recognise (which can happen when an older SDK talks to a newer API).
        /// </summary>
        public static DtoLottoDrawStatus GetStatusType(this DtoLottoDraw draw)
        {
            if (draw == null) return DtoLottoDrawStatus.Unknown;

            return Enum.IsDefined(typeof(DtoLottoDrawStatus), draw.Status)
                ? (DtoLottoDrawStatus) draw.Status
                : DtoLottoDrawStatus.Unknown;
        }
    }
}
