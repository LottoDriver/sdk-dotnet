using System.Collections.Generic;

namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// A country in which one or more lotteries are operated.
    /// </summary>
    public class DtoCountry
    {
        /// <summary>
        /// LottoDriver's country identifier. This is a short string (for example a
        /// country code), not an integer.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Country name in English.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Lotteries in this country that have unprocessed changes. Lotteries with
        /// no changes in the current response are not included.
        /// </summary>
        public List<DtoLotto> Lotteries { get; set; }
    }
}
