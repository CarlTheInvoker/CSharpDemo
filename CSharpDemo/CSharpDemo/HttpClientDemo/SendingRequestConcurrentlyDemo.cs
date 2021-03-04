namespace CSharpDemo.HttpClientDemo
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public class SendingRequestConcurrentlyDemo : IDemo
    {
        private const string MachineIp = @"";
        private const string RelativeUrl = @"";
        private const string Token = @"";
        private readonly HttpClient _httpClient;

        public SendingRequestConcurrentlyDemo()
        {
            HttpClientHandler handler = new HttpClientHandler();

            // To swallow the self-signed certificate exception
            handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidation;
            this._httpClient = new HttpClient(handler) { BaseAddress = new Uri($"https://{MachineIp}/dsapi/v0.1/") };

            // Add HpaAsApp token header
            this._httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(Token);
        }

        public void RunDemo()
        {
            //Parallel.For(0, 30, i =>
            //{
            //    this.AddNonCompliantDevice();
            //});

            this.AddNonCompliantDevice();
        }

        private void AddNonCompliantDevice()
        {
            Guid deviceId = Guid.NewGuid();
            var res = this._httpClient.SendAsync(this.CreateRequestMessage(deviceId)).GetAwaiter().GetResult();
            if (res.StatusCode == HttpStatusCode.NoContent)
            {
                Logger.LogInfo($"Adding device {deviceId} succeeded");
            }
            else
            {
                Logger.LogError($"Adding device {deviceId} failed, status code is {res.StatusCode}");
            }
        }

        private HttpRequestMessage CreateRequestMessage(Guid deviceId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, RelativeUrl);
            string content = $"{{\"DeviceId\": \"{deviceId}\", \"NonComplianceTime\": \"{DateTime.UtcNow:O}\"}}";
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            // Headers must have
            request.Headers.Add("X-ActivityId", Guid.NewGuid().ToString());
            request.Headers.Add("X-ClientRequestId", Guid.NewGuid().ToString());
            request.Headers.Add("X-ProcessName", "CSharpDemo.exe");
            request.Headers.Add("X-CallerFileNameLine", "SendingRequestConcurrentlyDemo.cs line 30");
            return request;
        }

        private static bool ServerCertificateCustomValidation(HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors)
        {
            return true;
        }
    }
}
