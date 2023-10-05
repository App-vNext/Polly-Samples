namespace PollyTestClient.Samples
{
    public static class HttpClientHelper
    {
        private static readonly HttpClient httpClient = new();
        public static string IssueRequestAndProcessResponse(int totalRequests, CancellationToken cancellationToken = default)
        {
            // Make a request and get a response
            var url = $"{Configuration.WEB_API_ROOT}/api/values/{totalRequests}";
            using var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

            // Read response's body
            using var stream = response.Content.ReadAsStream(cancellationToken);
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}
