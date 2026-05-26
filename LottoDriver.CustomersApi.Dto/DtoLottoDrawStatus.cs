namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// Lifecycle statuses recognised by this SDK version. Compare against
    /// <see cref="DtoLottoDraw.Status"/> using
    /// <see cref="DtoLottoDrawExtensions.GetStatusType"/>, which falls back to
    /// <see cref="Unknown"/> for any value the SDK does not know.
    /// </summary>
    public enum DtoLottoDrawStatus
    {
        /// <summary>
        /// The server returned a status this SDK version does not know. Treat as
        /// <see cref="UndoCleared"/>: stop accepting new bets and, if the draw was
        /// already settled, freeze payouts until a known status appears.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// The draw is announced and results are not yet known. Bets are allowed
        /// only if the integrator's own cutoff also permits it; "published" does
        /// not by itself authorise betting.
        /// </summary>
        Published = 0,

        /// <summary>
        /// Results are not known, but LottoDriver has detected a scheduling
        /// discrepancy. Do not accept bets until the status returns to
        /// <see cref="Published"/> or <see cref="Cleared"/>.
        /// </summary>
        Unpublished = 1,

        /// <summary>
        /// Results are final. Bets can be settled. Void any bet placed after
        /// <see cref="DtoLottoDraw.DrawTimeUtc"/>.
        /// </summary>
        Cleared = 2,

        /// <summary>
        /// A draw that was previously <see cref="Cleared"/> is in dispute. If bets
        /// have already been settled, reverse them. If reversal is impossible, at
        /// minimum block payouts on winning bets until the status moves back to
        /// <see cref="Cleared"/> or to <see cref="Canceled"/>.
        /// </summary>
        UndoCleared = 3,

        /// <summary>
        /// The draw did not take place. All bets must be voided and the stakes
        /// returned to the players.
        /// </summary>
        Canceled = 4
    }
}
