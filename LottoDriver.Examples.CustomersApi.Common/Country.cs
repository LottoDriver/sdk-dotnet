namespace LottoDriver.Examples.CustomersApi.Common
{
    /// <summary>
    /// A country as stored in the example app's local SQLite database. Mirrors
    /// <c>LottoDriver.CustomersApi.Dto.DtoCountry</c>, with the betting company's
    /// own ids.
    /// </summary>
    public class Country
    {
        /// <summary>
        /// Local primary key. Autoincrement; <c>0</c> means "not yet inserted".
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Country name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// LottoDriver's country id, if this row originated from the LottoDriver
        /// feed. Nullable, for the same reason as <c>Lotto.LottoDriverLottoId</c>.
        /// </summary>
        public string LottoDriverCountryId { get; set; }
    }
}
