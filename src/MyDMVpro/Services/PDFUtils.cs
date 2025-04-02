using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace MyDMVpro.Services
{
    /// <summary>
    /// This class wraps calls to the PDFUtils functions 
    /// we have deployed in Azure.  PDF generation can be resource intensive
    /// and requires additional libraries on Linux that are not installed by default.
    /// </summary>
    public static class PdfUtils
    {
        private static readonly HttpClient _httpClient;

        static PdfUtils()
        {
            _httpClient = new HttpClient(new SocketsHttpHandler()
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

        public static Stream ConvertHtmlToPdf(string html)
        {
            // tbd: call azure function 
            return null;
        }
        public static List<byte[]> SplitPdf(Stream stream)
        {
            List<byte[]> pdfs = new();


            return pdfs;
        }
        public static async Task<List<byte[]>> SplitPdfAsync(Stream stream)
        {
            string url = "";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Content = new StreamContent(stream),
                Method = HttpMethod.Post,
                RequestUri = new System.Uri(url)
            };

            var response = await _httpClient.SendAsync(request);
            
            return null;
        }
        class SplitPdfResponse
        {
            public string[] PdfBase64 { get; set; }
        }
    }
}
