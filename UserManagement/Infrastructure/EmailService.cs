using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UserManagement.Infrastructure
{
    public class EmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _senderEmail;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;

        public EmailService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _senderEmail = Environment.GetEnvironmentVariable("EMAIL_ADDRESS")
                ?? throw new InvalidOperationException("EMAIL_ADDRESS is not set.");
            _clientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID")
                ?? throw new InvalidOperationException("GMAIL_CLIENT_ID is not set.");
            _clientSecret = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET")
                ?? throw new InvalidOperationException("GMAIL_CLIENT_SECRET is not set.");
            _refreshToken = Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN")
                ?? throw new InvalidOperationException("GMAIL_REFRESH_TOKEN is not set.");
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var values = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "refresh_token", _refreshToken },
                { "grant_type", "refresh_token" }
            };

            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(values));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        public async Task SendVerificationEmailAsync(string userEmail, string code)
        {
            var accessToken = await GetAccessTokenAsync();

            var mime =
                $"To: {userEmail}\r\n" +
                $"From: {_senderEmail}\r\n" +
                "Subject: Verification Code\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n\r\n" +
                $"<p>This is your verification code: <strong>{code}</strong></p>";

            var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(mime))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://gmail.googleapis.com/gmail/v1/users/me/messages/send")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { raw = rawMessage }), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}