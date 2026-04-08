using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace StravaAPILibary.Authentication
{
    /// <summary>
    /// Handles the OAuth 2.0 authentication flow for Strava users, including authorization, token exchange, and token refresh.
    /// </summary>
    /// <remarks>
    /// The <c>UserAuthentication</c> class implements the complete OAuth 2.0 authorization code flow for Strava API integration.
    /// 
    /// <para><b>OAuth 2.0 Flow:</b></para>
    /// <list type="number">
    /// <item><description>User is redirected to Strava's authorization page</description></item>
    /// <item><description>User authorizes the application and grants requested scopes</description></item>
    /// <item><description>Strava redirects back with an authorization code</description></item>
    /// <item><description>Application exchanges the code for access and refresh tokens</description></item>
    /// <item><description>Access token is used for API requests (expires in 6 hours)</description></item>
    /// <item><description>Refresh token is used to get new access tokens when needed</description></item>
    /// </list>
    /// 
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // Create credentials
    /// var credentials = new Credentials("your_client_id", "your_client_secret", "read,activity:read_all");
    /// 
    /// // Initialize authentication
    /// var userAuth = new UserAuthentication(credentials, "http://localhost:8080/callback", "read,activity:read_all");
    /// 
    /// // Start authorization process
    /// userAuth.StartAuthorization();
    /// 
    /// // Wait for user to complete authorization
    /// string authCode = await userAuth.WaitForAuthCodeAsync();
    /// 
    /// // Exchange code for tokens
    /// bool success = await userAuth.ExchangeCodeForTokenAsync(authCode);
    /// 
    /// if (success)
    /// {
    ///     string accessToken = credentials.AccessToken;
    ///     // Use accessToken for API calls
    /// }
    /// </code>
    /// 
    /// <para><b>Token Management:</b></para>
    /// <list type="bullet">
    /// <item><description>Access tokens expire after 6 hours</description></item>
    /// <item><description>Refresh tokens have no expiration (until revoked)</description></item>
    /// <item><description>Use <see cref="RefreshAccessTokenAsync"/> to get new access tokens</description></item>
    /// <item><description>Store tokens securely (environment variables, secure storage)</description></item>
    /// </list>
    /// 
    /// <para><b>Security Considerations:</b></para>
    /// <list type="bullet">
    /// <item><description>Never expose client secret in client-side code</description></item>
    /// <item><description>Use HTTPS for redirect URIs in production</description></item>
    /// <item><description>Request minimal scopes needed for your application</description></item>
    /// <item><description>Implement proper error handling for token refresh failures</description></item>
    /// </list>
    /// 
    /// <para><b>API Documentation:</b></para>
    /// Refer to the official Strava API documentation for authentication:
    /// <see href="https://developers.strava.com/docs/authentication/">Strava Authentication Guide</see>.
    /// </remarks>
    /// <example>
    /// <para>Complete authentication flow with error handling:</para>
    /// <code>
    /// public async Task&lt;string&gt; AuthenticateAsync()
    /// {
    ///     var credentials = new Credentials("client_id", "client_secret", "read,activity:read_all");
    ///     var userAuth = new UserAuthentication(credentials, "http://localhost:8080/callback", "read,activity:read_all");
    ///     
    ///     try
    ///     {
    ///         userAuth.StartAuthorization();
    ///         string authCode = await userAuth.WaitForAuthCodeAsync();
    ///         
    ///         bool success = await userAuth.ExchangeCodeForTokenAsync(authCode);
    ///         if (success)
    ///         {
    ///             return credentials.AccessToken;
    ///         }
    ///         else
    ///         {
    ///             throw new InvalidOperationException("Failed to exchange authorization code for tokens.");
    ///         }
    ///     }
    ///     catch (TimeoutException)
    ///     {
    ///         throw new InvalidOperationException("Authorization timed out. Please try again.");
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         throw new InvalidOperationException($"Authentication failed: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class UserAuthentication
    {
        private readonly Credentials _creds;
        private readonly string _redirectUri;
        private readonly string _scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAuthentication"/> class with the specified credentials and configuration.
        /// </summary>
        /// <param name="creds">The Strava API credentials containing client ID and secret. Must not be null.</param>
        /// <param name="redirectUri">The redirect URI registered for the Strava application. Must match the URI configured in your Strava app settings.</param>
        /// <param name="scope">The requested OAuth scope(s). Multiple scopes can be separated by commas (e.g., <c>read,activity:read_all</c>).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="creds"/>, <paramref name="redirectUri"/>, or <paramref name="scope"/> is null.
        /// </exception>
        /// <remarks>
        /// <para>The redirect URI must exactly match what is configured in your Strava application settings.</para>
        /// <para>Common redirect URIs for development:</para>
        /// <list type="bullet">
        /// <item><description><c>http://localhost:8080/callback</c></description></item>
        /// <item><description><c>http://localhost:3000/callback</c></description></item>
        /// <item><description><c>http://localhost:5000/callback</c></description></item>
        /// </list>
        /// </remarks>
        public UserAuthentication(Credentials creds, string redirectUri, string scope)
        {
            _creds = creds ?? throw new ArgumentNullException(nameof(creds));
            _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        /// <summary>
        /// Starts the authorization process by opening the browser with the Strava OAuth URL.
        /// </summary>
        /// <remarks>
        /// This method opens the user's default browser and navigates to Strava's authorization page.
        /// The user will be prompted to log in to Strava (if not already logged in) and authorize your application.
        /// 
        /// <para>After authorization, Strava will redirect the user back to your redirect URI with an authorization code.</para>
        /// <para>Use <see cref="WaitForAuthCodeAsync"/> to automatically capture the authorization code, or handle the redirect manually.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var userAuth = new UserAuthentication(credentials, "http://localhost:8080/callback", "read,activity:read_all");
        /// userAuth.StartAuthorization();
        /// 
        /// // Option 1: Wait for automatic code capture
        /// string authCode = await userAuth.WaitForAuthCodeAsync();
        /// 
        /// // Option 2: Handle redirect manually
        /// // User will be redirected to http://localhost:8080/callback?code=YOUR_AUTH_CODE
        /// // Extract the code parameter from the URL
        /// </code>
        /// </example>
        public void StartAuthorization()
        {
            string url = BuildAuthorizationUrl();
            OpenBrowser(url);
        }

        /// <summary>
        /// Opens the specified URL in the default browser.
        /// </summary>
        /// <param name="url">The URL to open in the browser.</param>
        /// <remarks>
        /// This method uses the system's default browser to open the provided URL.
        /// On Windows, it uses <c>Process.Start</c> with <c>UseShellExecute = true</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OpenBrowser(string url)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>
        /// Builds the Strava OAuth authorization URL with the configured parameters.
        /// </summary>
        /// <returns>The complete authorization URL for redirecting the user to Strava's authorization page.</returns>
        /// <remarks>
        /// The generated URL includes:
        /// <list type="bullet">
        /// <item><description>Client ID from credentials</description></item>
        /// <item><description>Redirect URI (URL-encoded)</description></item>
        /// <item><description>Response type set to "code"</description></item>
        /// <item><description>Requested scope (URL-encoded)</description></item>
        /// <item><description>Approval prompt set to "force" to always show authorization page</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var userAuth = new UserAuthentication(credentials, "http://localhost:8080/callback", "read,activity:read_all");
        /// string authUrl = userAuth.BuildAuthorizationUrl();
        /// Console.WriteLine(authUrl);
        /// // Output: https://www.strava.com/oauth/authorize?client_id=12345&amp;redirect_uri=http%3A%2F%2Flocalhost%3A8080%2Fcallback&amp;response_type=code&amp;scope=read%2Cactivity%3Aread_all&amp;approval_prompt=force
        /// </code>
        /// </example>
        public string BuildAuthorizationUrl()
        {
            return $"https://www.strava.com/oauth/authorize?client_id={_creds.ClientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope={Uri.EscapeDataString(_scope)}&approval_prompt=force";
        }

        /// <summary>
        /// Waits for the user to authorize the application and returns the authorization code.
        /// </summary>
        /// <returns>The authorization code from the OAuth redirect.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the redirect URI is not set or the authorization code is missing in the response.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown if no authorization is received within 5 minutes.
        /// </exception>
        /// <remarks>
        /// This method starts an HTTP listener on the redirect URI port and waits for the OAuth callback.
        /// It automatically handles the redirect response and extracts the authorization code.
        /// 
        /// <para><b>Important:</b></para>
        /// <list type="bullet">
        /// <item><description>The redirect URI must be accessible (localhost for development)</description></item>
        /// <item><description>Port must be available and not blocked by firewall</description></item>
        /// <item><description>Method times out after 5 minutes</description></item>
        /// <item><description>Returns a success page to the user's browser</description></item>
        /// </list>
        /// 
        /// <para><b>Alternative:</b></para>
        /// For production applications, consider handling the redirect manually instead of using this method.
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     userAuth.StartAuthorization();
        ///     string authCode = await userAuth.WaitForAuthCodeAsync();
        ///     Console.WriteLine($"Authorization code: {authCode}");
        /// }
        /// catch (TimeoutException)
        /// {
        ///     Console.WriteLine("Authorization timed out. Please try again.");
        /// }
        /// </code>
        /// </example>
        public async Task<string> WaitForAuthCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(_redirectUri))
                throw new InvalidOperationException("Redirect URI is not set.");

            var uri = new Uri(_redirectUri);
            int port = uri.Port;

            using var listener = new HttpListener();
            listener.Prefixes.Add($"{uri.Scheme}://{uri.Host}:{port}/");
            listener.Start();

            var contextTask = listener.GetContextAsync();
            if (await Task.WhenAny(contextTask, Task.Delay(TimeSpan.FromMinutes(5))) != contextTask)
            {
                throw new TimeoutException("Timed out waiting for authorization response.");
            }

            var context = contextTask.Result;
            var code = context.Request.QueryString["code"];

            byte[] buffer = Encoding.UTF8.GetBytes("Authorization successful. You can close this window.");
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();

            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Authorization code not found in the request.");

            return code;
        }

        /// <summary>
        /// Exchanges an authorization code for an access token and refresh token.
        /// </summary>
        /// <param name="authCode">The authorization code received from the OAuth redirect.</param>
        /// <returns><c>true</c> if the token exchange was successful; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="authCode"/> is null or empty.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the token exchange request fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the response JSON cannot be parsed.
        /// </exception>
        /// <remarks>
        /// This method performs the OAuth 2.0 token exchange by sending a POST request to Strava's token endpoint.
        /// If successful, the access token, refresh token, and expiration time are stored in the credentials object.
        /// 
        /// <para><b>Token Exchange Process:</b></para>
        /// <list type="number">
        /// <item><description>Send POST request to <c>https://www.strava.com/oauth/token</c></description></item>
        /// <item><description>Include client ID, client secret, authorization code, and grant type</description></item>
        /// <item><description>Parse response to extract tokens and expiration</description></item>
        /// <item><description>Store tokens in the credentials object</description></item>
        /// </list>
        /// 
        /// <para><b>Response Fields:</b></para>
        /// <list type="bullet">
        /// <item><description><c>access_token</c> - Used for API requests (expires in 6 hours)</description></item>
        /// <item><description><c>refresh_token</c> - Used to get new access tokens</description></item>
        /// <item><description><c>expires_at</c> - Unix timestamp when access token expires</description></item>
        /// <item><description><c>expires_in</c> - Seconds until access token expires</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// string authCode = "received_authorization_code";
        /// bool success = await userAuth.ExchangeCodeForTokenAsync(authCode);
        /// 
        /// if (success)
        /// {
        ///     Console.WriteLine($"Access Token: {credentials.AccessToken}");
        ///     Console.WriteLine($"Refresh Token: {credentials.RefreshToken}");
        ///     Console.WriteLine($"Expires: {credentials.TokenExpiration}");
        /// }
        /// else
        /// {
        ///     Console.WriteLine("Token exchange failed.");
        /// }
        /// </code>
        /// </example>
        public async Task<bool> ExchangeCodeForTokenAsync(string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode))
                throw new ArgumentException("Authorization code cannot be null or empty.", nameof(authCode));

            var data = new Dictionary<string, string>
            {
                ["client_id"] = _creds.ClientId,
                ["client_secret"] = _creds.ClientSecret,
                ["code"] = authCode,
                ["grant_type"] = "authorization_code"
            };

            var response = await PostForTokenAsync(data);
            return await ParseTokenResponse(response);
        }

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <returns><c>true</c> if the token refresh was successful; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no refresh token is available.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the refresh request fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the response JSON cannot be parsed.
        /// </exception>
        /// <remarks>
        /// This method uses the refresh token to obtain a new access token without requiring user re-authorization.
        /// The new access token and expiration time are stored in the credentials object.
        /// 
        /// <para><b>When to Use:</b></para>
        /// <list type="bullet">
        /// <item><description>Access token has expired (6 hours)</description></item>
        /// <item><description>Access token will expire soon (within 5 minutes)</description></item>
        /// <item><description>API calls return 401 Unauthorized errors</description></item>
        /// </list>
        /// 
        /// <para><b>Error Handling:</b></para>
        /// If the refresh token is invalid or has been revoked, this method will return false.
        /// In such cases, the user will need to re-authenticate using the full OAuth flow.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Check if token needs refresh
        /// if (credentials.TokenExpiration &lt;= DateTime.UtcNow.AddMinutes(5))
        /// {
        ///     bool refreshSuccess = await userAuth.RefreshAccessTokenAsync();
        ///     if (refreshSuccess)
        ///     {
        ///         Console.WriteLine("Token refreshed successfully.");
        ///     }
        ///     else
        ///     {
        ///         Console.WriteLine("Token refresh failed. User needs to re-authenticate.");
        ///     }
        /// }
        /// </code>
        /// </example>
        public async Task<bool> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_creds.RefreshToken))
                throw new InvalidOperationException("No refresh token available.");

            var data = new Dictionary<string, string>
            {
                ["client_id"] = _creds.ClientId,
                ["client_secret"] = _creds.ClientSecret,
                ["refresh_token"] = _creds.RefreshToken,
                ["grant_type"] = "refresh_token"
            };

            var response = await PostForTokenAsync(data);
            return await ParseTokenResponse(response);
        }

        /// <summary>
        /// Posts token exchange/refresh data to Strava's OAuth endpoint.
        /// </summary>
        /// <param name="data">The form data to send in the request.</param>
        /// <returns>The HTTP response from Strava's OAuth endpoint.</returns>
        /// <exception cref="HttpRequestException">
        /// Thrown when the HTTP request fails.
        /// </exception>
        private async Task<HttpResponseMessage> PostForTokenAsync(Dictionary<string, string> data)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var content = new FormUrlEncodedContent(data);
            return await client.PostAsync("https://www.strava.com/oauth/token", content);
        }

        /// <summary>
        /// Parses the token response and updates the credentials object.
        /// </summary>
        /// <param name="response">The HTTP response from the token endpoint.</param>
        /// <returns>true if parsing was successful; otherwise, false.</returns>
        /// <exception cref="HttpRequestException">
        /// Thrown when the response indicates an error.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the response JSON cannot be parsed.
        /// </exception>
        private async Task<bool> ParseTokenResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Token exchange failed. Status: {response.StatusCode}, Response: {responseContent}");
            }

            var jsonResponse = JsonNode.Parse(responseContent) as JsonObject;
            if (jsonResponse == null)
                throw new JsonException("Failed to parse token response JSON.");

            if (jsonResponse.TryGetPropertyValue("access_token", out var accessToken) &&
                jsonResponse.TryGetPropertyValue("refresh_token", out var refreshToken) &&
                jsonResponse.TryGetPropertyValue("expires_at", out var expiresAt))
            {
                _creds.AccessToken = accessToken?.ToString() ?? string.Empty;
                _creds.RefreshToken = refreshToken?.ToString() ?? string.Empty;
                _creds.TokenExpiration = DateTimeOffset.FromUnixTimeSeconds(expiresAt?.GetValue<long>() ?? 0).DateTime;
                return true;
            }

            return false;
        }
    }
}
