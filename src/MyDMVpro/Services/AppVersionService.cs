using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace MyDMVpro.Services
{
    public interface IAppVersionService
    {
        string Version { get; }
    }
    public class AppVersionService : IAppVersionService
    {
        public string Version =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }

}
