using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;

namespace MyDMVpro
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            Common.CurrencyHelper.Test();
#endif
            CultureInfo.DefaultThreadCurrentCulture = new("en-US");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
