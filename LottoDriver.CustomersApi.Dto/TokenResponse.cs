namespace LottoDriver.CustomersApi.Dto
{
    /// <summary>
    /// Payload returned by <c>POST /token</c> when the SDK authenticates with the
    /// <c>client_credentials</c> grant. Properties are lowercase because the
    /// server sends them in the OAuth2 standard snake_case form.
    /// </summary>
    public class TokenResponse
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Bearer token to send on subsequent requests in the
        /// <c>Authorization: Bearer ...</c> header.
        /// </summary>
        public string access_token { get; set; }

        /// <summary>
        /// Always the literal string <c>bearer</c> for this API.
        /// </summary>
        public string token_type { get; set; }

        /// <summary>
        /// Lifetime of <see cref="access_token"/> in seconds. The SDK falls back
        /// to a 24-hour cache window if the server returns zero.
        /// </summary>
        public int expires_in { get; set; }
        // ReSharper restore InconsistentNaming
    }
}
