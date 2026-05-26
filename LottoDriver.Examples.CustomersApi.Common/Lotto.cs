namespace LottoDriver.Examples.CustomersApi.Common
{
    /// <summary>
    /// A lottery as stored in the example app's local SQLite database. Mirrors
    /// <c>LottoDriver.CustomersApi.Dto.DtoLotto</c>, but with the betting company's
    /// own ids. Each instance corresponds to one row in the <c>lotto</c> table.
    /// </summary>
    public class Lotto
    {
        /// <summary>
        /// Local primary key. Autoincrement; <c>0</c> means "not yet inserted".
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to <see cref="Country.Id"/> in the local <c>country</c>
        /// table. This is the local id, not LottoDriver's country id.
        /// </summary>
        public int CountryId { get; set; }

        /// <summary>
        /// Lottery name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Total balls in the drum. Mirrors <c>DtoLotto.NumbersTotal</c>.
        /// </summary>
        public int NumbersTotal { get; set; }

        /// <summary>
        /// Balls drawn per round. Mirrors <c>DtoLotto.NumbersDrawn</c>.
        /// </summary>
        public int NumbersDrawn { get; set; }

        /// <summary>
        /// LottoDriver's lottery id, if this row originated from the LottoDriver
        /// feed. Nullable to allow rows that have no LottoDriver counterpart (a
        /// betting company can run lotteries that LottoDriver does not cover).
        /// </summary>
        public int? LottoDriverLottoId { get; set; }
    }
}
