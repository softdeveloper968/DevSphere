using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyDMVpro.Common;

public static class  ChampRequestTypes
{
    public const string RepossessionWithTitle = "RPO";
    public const string RepossessionWithoutTitle = "NTR";
}

public class ChampHelper
{
    private static readonly HttpClient s_httpClient;
    public static string HttpTriggerInitiateSubmissionUrl { get { return ConfigurationHelper.Configuration["Champ:InitiateRequestUrl"]; } }

    static ChampHelper()
    {
        s_httpClient = new HttpClient(new SocketsHttpHandler()
        {
            // The maximum idle time for a connection in the pool. When there is no request in
            // the provided delay, the connection is released.
            // Default value in .NET 6: 1 minute
            //PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),

            // This property defines maximal connection lifetime in the pool regardless
            // of whether the connection is idle or active. The connection is reestablished
            // periodically to reflect the DNS or other network changes.
            // ⚠️ Default value in .NET 6: never
            //    Set a timeout to reflect the DNS or other network changes
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        });
    }
    public class ChampInitiateRequest
    {
        public Guid RequestId { get; set; }
    }
    public async static Task<ResultCounts> InitiateChampRequest_RepossessionWithTitle(List<ChampInitiateRequest> requests, ILogger logger)
    {
        return await TriggerInitiateSubmission(requests, ChampRequestTypes.RepossessionWithTitle, logger);
    }

    public static async Task<ResultCounts> TriggerInitiateSubmission(List<ChampInitiateRequest> requests, string transactionType, ILogger logger)
    {
        List<Task<HttpResponseMessage>> tasks = new();

        foreach (var request in requests)
        {
            tasks.Add(HttpTriggerInitiateSubmission(request, transactionType, logger));
        }
        await Task.WhenAll(tasks);

        return LogHttpResponseMessages(tasks, logger);
    }

    public static Task<HttpResponseMessage> HttpTriggerInitiateSubmission(ChampInitiateRequest request, string transactionType, ILogger logger)
    {
        string rootUrl = HttpTriggerInitiateSubmissionUrl;
        if (string.IsNullOrEmpty(rootUrl))
            rootUrl = "";
        if (!rootUrl.Contains('?'))
            rootUrl += "?";
        string url = $"{rootUrl}&RequestId={request.RequestId}";

        return s_httpClient.PostAsync(url, null);
    }
    public class ResultCounts
    {
        public ResultCounts(int successCount, int errorCount)
        {
            SuccessCount = successCount;
            ErrorCount = errorCount;
        }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }
    public static ResultCounts LogHttpResponseMessages(List<Task<HttpResponseMessage>> tasks, ILogger logger)
    {
        int successCount = 0;
        int errorCount = 0;

        foreach (var t in tasks)
        {
            var result = t.Result;
            if (result.IsSuccessStatusCode) 
                successCount++;
            else 
                errorCount++;
            logger?.LogInformation("HttpChampApi:  {StatusCode} - {ReasonPhrase}", result.StatusCode, result.ReasonPhrase);
        }
        return new ResultCounts(successCount, errorCount);
    }
}
