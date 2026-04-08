using System;

namespace StravaAPILibary.Authentication
{
    /// <summary>
    /// Represents the credentials required for authenticating with the Strava API.
    /// </summary>
    /// <remarks>
    /// The <c>Credentials</c> class encapsulates all the necessary information for OAuth 2.0 authentication with the Strava API.
    /// It stores both the application credentials (client ID and secret) and the user's OAuth tokens (access token and refresh token).
    /// 
    /// <para><b>Application Credentials:</b></para>
    /// <list type="bullet">
    /// <item><description><c>ClientId</c> - Unique identifier for your Strava application</description></item>
    /// <item><description><c>ClientSecret</c> - Secret key for your Strava application (keep secure)</description></item>
    /// <item><description><c>Scope</c> - Requested permissions for the application</description></item>
    /// </list>
    /// 
    /// <para><b>OAuth Tokens:</b></para>
    /// <list type="bullet">
    /// <item><description><c>AccessToken</c> - Used for API requests (expires in 6 hours)</description></item>
    /// <item><description><c>RefreshToken</c> - Used to get new access tokens (no expiration)</description></item>
    /// <item><description><c>TokenExpiration</c> - When the access token expires</description></item>
    /// </list>
    /// 
    /// <para><b>Security Considerations:</b></para>
    /// <list type="bullet">
    /// <item><description>Never expose the client secret in client-side code</description></item>
    /// <item><description>Store tokens securely (environment variables, secure storage)</description></item>
    /// <item><description>Request minimal scopes needed for your application</description></item>
    /// <item><description>Implement proper token refresh logic</description></item>
    /// </list>
    /// 
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // Create credentials for a new application
    /// var credentials = new Credentials("your_client_id", "your_client_secret", "read,activity:read_all");
    /// 
    /// // After OAuth flow, tokens will be populated
    /// Console.WriteLine($"Access Token: {credentials.AccessToken}");
    /// Console.WriteLine($"Refresh Token: {credentials.RefreshToken}");
    /// Console.WriteLine($"Expires: {credentials.TokenExpiration}");
    /// 
    /// // Check if token is expired
    /// bool isExpired = credentials.TokenExpiration &lt;= DateTime.UtcNow;
    /// </code>
    /// 
    /// <para><b>Available Scopes:</b></para>
    /// <list type="bullet">
    /// <item><description>read - Basic profile access</description></item>
    /// <item><description>activity:read_all - Read all activities</description></item>
    /// <item><description>activity:write - Upload activities</description></item>
    /// <item><description>profile:read_all - Detailed profile access</description></item>
    /// <item><description>profile:write - Update profile information</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <para>Complete authentication flow with credentials:</para>
    /// <code>
    /// public class StravaClient
    /// {
    ///     private readonly Credentials _credentials;
    ///     
    ///     public StravaClient(string clientId, string clientSecret, string scope)
    ///     {
    ///         _credentials = new Credentials(clientId, clientSecret, scope);
    ///     }
    ///     
    ///     public async Task&lt;string&gt; AuthenticateAsync()
    ///     {
    ///         var userAuth = new UserAuthentication(_credentials, "http://localhost:8080/callback", _credentials.Scope);
    ///         
    ///         userAuth.StartAuthorization();
    ///         string authCode = await userAuth.WaitForAuthCodeAsync();
    ///         
    ///         bool success = await userAuth.ExchangeCodeForTokenAsync(authCode);
    ///         if (success)
    ///         {
    ///             return _credentials.AccessToken;
    ///         }
    ///         
    ///         throw new InvalidOperationException("Authentication failed.");
    ///     }
    ///     
    ///     public bool IsTokenValid()
    ///     {
            ///         return !string.IsNullOrEmpty(_credentials.AccessToken) &amp;&amp;
        ///                _credentials.TokenExpiration &gt; DateTime.UtcNow.AddMinutes(5);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class Credentials
    {
        /// <summary>
        /// Gets or sets the client ID provided by Strava for the application.
        /// </summary>
        /// <remarks>
        /// The client ID is a unique identifier for your Strava application. It is provided when you create
        /// a new application in the Strava API settings.
        /// 
        /// <para>This value is used in:</para>
        /// <list type="bullet">
        /// <item><description>OAuth authorization URL generation</description></item>
        /// <item><description>Token exchange requests</description></item>
        /// <item><description>Token refresh requests</description></item>
        /// </list>
        /// 
        /// <para><b>Security:</b></para>
        /// The client ID is not sensitive and can be safely included in client-side code.
        /// </remarks>
        /// <example>
        /// <code>
        /// var credentials = new Credentials("12345", "secret", "read");
        /// Console.WriteLine(credentials.ClientId); // Output: 12345
        /// </code>
        /// </example>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret associated with the Strava application.
        /// </summary>
        /// <remarks>
        /// The client secret is a sensitive credential that should be kept secure. It is used to authenticate
        /// your application with Strava during the OAuth token exchange process.
        /// 
        /// <para><b>Security Requirements:</b></para>
        /// <list type="bullet">
        /// <item><description>Never expose in client-side code</description></item>
        /// <item><description>Store securely (environment variables, secure storage)</description></item>
        /// <item><description>Don't commit to version control</description></item>
        /// <item><description>Rotate if compromised</description></item>
        /// </list>
        /// 
        /// <para><b>Usage:</b></para>
        /// The client secret is only used server-side during OAuth token exchange and refresh operations.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Load from environment variable (recommended)
        /// string clientSecret = Environment.GetEnvironmentVariable("STRAVA_CLIENT_SECRET");
        /// var credentials = new Credentials("12345", clientSecret, "read");
        /// </code>
        /// </example>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the OAuth access token used for API requests.
        /// </summary>
        /// <remarks>
        /// The access token is obtained through the OAuth 2.0 authorization flow and is used to authenticate
        /// API requests to Strava. It expires after 6 hours and must be refreshed using the refresh token.
        /// 
        /// <para><b>Token Properties:</b></para>
        /// <list type="bullet">
        /// <item><description>Expires after 6 hours</description></item>
        /// <item><description>Used in Authorization header: Bearer {access_token}</description></item>
        /// <item><description>Can be refreshed using RefreshToken</description></item>
        /// <item><description>Should be stored securely</description></item>
        /// </list>
        /// 
        /// <para><b>Validation:</b></para>
        /// Check TokenExpiration to determine if the token is still valid.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Use access token for API requests
        /// using var client = new HttpClient();
        /// client.DefaultRequestHeaders.Authorization = 
        ///     new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
        /// 
        /// var response = await client.GetAsync("https://www.strava.com/api/v3/athlete");
        /// </code>
        /// </example>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OAuth refresh token used to obtain a new access token when it expires.
        /// </summary>
        /// <remarks>
        /// The refresh token is obtained during the initial OAuth authorization and can be used to get new
        /// access tokens without requiring user re-authorization. Refresh tokens do not expire unless revoked.
        /// 
        /// <para><b>Token Properties:</b></para>
        /// <list type="bullet">
        /// <item><description>No expiration (until revoked by user)</description></item>
        /// <item><description>Used only for token refresh operations</description></item>
        /// <item><description>Should be stored securely alongside access token</description></item>
        /// <item><description>Can be revoked by user in Strava settings</description></item>
        /// </list>
        /// 
        /// <para><b>Usage:</b></para>
        /// Use this token with <c>UserAuthentication.RefreshAccessTokenAsync()</c> to get a new access token.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Refresh access token when expired
        /// if (credentials.TokenExpiration &lt;= DateTime.UtcNow.AddMinutes(5))
        /// {
        ///     var userAuth = new UserAuthentication(credentials, redirectUri, scope);
        ///     bool success = await userAuth.RefreshAccessTokenAsync();
        ///     
        ///     if (success)
        ///     {
        ///         // New access token is now available
        ///         Console.WriteLine("Token refreshed successfully");
        ///     }
        /// }
        /// </code>
        /// </example>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the scope of access for the token.
        /// </summary>
        /// <remarks>
        /// The scope defines what permissions your application has been granted by the user. Multiple scopes
        /// can be combined using commas.
        /// 
        /// <para><b>Available Scopes:</b></para>
        /// <list type="bullet">
        /// <item><description>read - Basic profile access (always included)</description></item>
        /// <item><description>activity:read_all - Read all activities</description></item>
        /// <item><description>activity:write - Upload activities</description></item>
        /// <item><description>profile:read_all - Detailed profile access</description></item>
        /// <item><description>profile:write - Update profile information</description></item>
        /// </list>
        /// 
        /// <para><b>Best Practices:</b></para>
        /// <list type="bullet">
        /// <item><description>Request only the scopes you need</description></item>
        /// <item><description>Explain to users why you need each scope</description></item>
        /// <item><description>Handle cases where users deny certain scopes</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Minimal scope for basic functionality
        /// var credentials = new Credentials("client_id", "secret", "read");
        /// 
        /// // Full scope for complete functionality
        /// var credentials = new Credentials("client_id", "secret", "read,activity:read_all,activity:write");
        /// 
        /// // Check if specific scope is granted
        /// bool hasActivityRead = credentials.Scope.Contains("activity:read_all");
        /// </code>
        /// </example>
        public string Scope { get; set; } = "read";

        /// <summary>
        /// Gets or sets the expiration date and time of the current access token.
        /// </summary>
        /// <remarks>
        /// This property indicates when the current access token will expire. Access tokens expire after 6 hours
        /// from the time they were issued. After expiration, you must use the refresh token to get a new access token.
        /// 
        /// <para><b>Token Lifecycle:</b></para>
        /// <list type="number">
        /// <item><description>Token is issued (expires in 6 hours)</description></item>
        /// <item><description>Use token for API requests</description></item>
        /// <item><description>Check expiration before making requests</description></item>
        /// <item><description>Refresh token when close to expiration</description></item>
        /// </list>
        /// 
        /// <para><b>Validation:</b></para>
        /// Check if token is expired: TokenExpiration &lt;= DateTime.UtcNow
        /// Check if token expires soon: TokenExpiration &lt;= DateTime.UtcNow.AddMinutes(5)
        /// </remarks>
        /// <example>
        /// <code>
        /// // Check if token is expired
        /// bool isExpired = credentials.TokenExpiration &lt;= DateTime.UtcNow;
        /// 
        /// // Check if token expires soon (within 5 minutes)
        /// bool expiresSoon = credentials.TokenExpiration &lt;= DateTime.UtcNow.AddMinutes(5);
        /// 
        /// // Calculate time until expiration
        /// TimeSpan timeUntilExpiration = credentials.TokenExpiration - DateTime.UtcNow;
        /// Console.WriteLine($"Token expires in {timeUntilExpiration.TotalMinutes:F0} minutes");
        /// </code>
        /// </example>
        public DateTime TokenExpiration { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Credentials"/> class with the specified client ID, client secret, and scope.
        /// </summary>
        /// <param name="clientId">The client ID provided by Strava for the application. Must not be null or empty.</param>
        /// <param name="clientSecret">The client secret associated with the Strava application. Must not be null or empty.</param>
        /// <param name="scope">The scope of access required for API interactions. Multiple scopes can be separated by commas (e.g., read,activity:read_all).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="clientId"/>, <paramref name="clientSecret"/>, or <paramref name="scope"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="clientId"/>, <paramref name="clientSecret"/>, or <paramref name="scope"/> is empty or whitespace.
        /// </exception>
        /// <remarks>
        /// This constructor initializes a new credentials object with the application's client credentials and requested scope.
        /// The OAuth tokens (access token, refresh token, expiration) will be populated after a successful OAuth flow.
        /// 
        /// <para><b>Parameter Validation:</b></para>
        /// <list type="bullet">
        /// <item><description>Client ID must be a valid Strava application ID</description></item>
        /// <item><description>Client secret must match the client ID</description></item>
        /// <item><description>Scope must be valid Strava OAuth scopes</description></item>
        /// </list>
        /// 
        /// <para><b>Common Scopes:</b></para>
        /// <list type="bullet">
        /// <item><description>read - Basic profile access (recommended minimum)</description></item>
        /// <item><description>read,activity:read_all - Read profile and activities</description></item>
        /// <item><description>read,activity:read_all,activity:write - Full activity access</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic credentials with minimal scope
        /// var credentials = new Credentials("12345", "your_secret", "read");
        /// 
        /// // Credentials with full activity access
        /// var credentials = new Credentials("12345", "your_secret", "read,activity:read_all,activity:write");
        /// 
        /// // Load from environment variables (recommended for production)
        /// string clientId = Environment.GetEnvironmentVariable("STRAVA_CLIENT_ID") 
        ///     ?? throw new InvalidOperationException("STRAVA_CLIENT_ID not set");
        /// string clientSecret = Environment.GetEnvironmentVariable("STRAVA_CLIENT_SECRET") 
        ///     ?? throw new InvalidOperationException("STRAVA_CLIENT_SECRET not set");
        /// var credentials = new Credentials(clientId, clientSecret, "read,activity:read_all");
        /// </code>
        /// </example>
        public Credentials(string clientId, string clientSecret, string scope)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            ClientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be empty or whitespace.", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("Client secret cannot be empty or whitespace.", nameof(clientSecret));
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentException("Scope cannot be empty or whitespace.", nameof(scope));
        }
    }
}
