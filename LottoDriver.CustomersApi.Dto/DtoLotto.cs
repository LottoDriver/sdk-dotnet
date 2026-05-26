using System.Collections.Generic;

namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// A lottery type. A single lottery type schedules many draws over time
    /// (weekly, daily, or several times per day).
    /// </summary>
    public class DtoLotto
    {
        /// <summary>
        /// LottoDriver's lottery identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Country in which this lottery is run. Populated by the SDK after the
        /// response is parsed; never serialized to JSON to avoid cyclic references.
        /// </summary>
        public DtoCountry Country { get; private set; }

        /// <summary>
        /// Lottery name in English. Translate locally; the API does not expose
        /// per-language names because length and character-set constraints vary
        /// between betting platforms.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Total balls in the drum.
        /// For example, "Italy 10e Lotto - 20/90" has <c>NumbersTotal = 90</c>.
        /// </summary>
        public int NumbersTotal { get; set; }

        /// <summary>
        /// Balls drawn per round.
        /// For example, "Italy 10e Lotto - 20/90" has <c>NumbersDrawn = 20</c>.
        /// </summary>
        public int NumbersDrawn { get; set; }

        /// <summary>
        /// Draws for this lottery that have unprocessed changes within the response
        /// window. Unchanged draws are not included.
        /// </summary>
        public List<DtoLottoDraw> Draws { get; set; }

        internal void SetCountry(DtoCountry country)
        {
            Country = country;
        }
    }
}
