using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace TraceRoute
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine($"Please, use '{AppDomain.CurrentDomain.FriendlyName} <domain or ip-address>'");
            else
                foreach (var address in TraceRoute.GetTraceRoute(args[0]))
                    Console.WriteLine(address);
        }
    }

    public class TraceRoute
    {
        private static readonly string[] ipInfoParams = {"ip", "hostname", "org", "country", "region", "city"};
        private const int Timeout = 100;
        private const int MaxTtl = 30;
        private const int BufferSize = 32;

        public static IEnumerable<string> GetTraceRoute(string hostname)
        {
            var buffer = new byte[BufferSize];
            new Random().NextBytes(buffer);
            var ping = new Ping();

            for (var ttl = 1; ttl <= MaxTtl; ttl++)
            {
                var options = new PingOptions(ttl, true);
                var reply = ping.Send(hostname, Timeout, buffer, options);

                switch (reply.Status)
                {
                    case IPStatus.TtlExpired:
                        yield return GetIpInfo(ttl, reply.Address.ToString());
                        continue;
                    case IPStatus.TimedOut:
                        yield return $"{ttl} * * *";
                        continue;
                    case IPStatus.Success:
                        yield return GetIpInfo(ttl, reply.Address.ToString());
                        break;
                }

                break;
            }
        }

        private static string GetIpInfo(int ttl, string address)
        {
            var ipInfo = new List<string> {ttl.ToString()};
            var hostname = $"http://ipinfo.io/{address}/json";
            using (var wc = new WebClient())
            {
                var json = wc.DownloadString(hostname);
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                ipInfo.AddRange(ipInfoParams
                    .Where(x => values.TryGetValue(x, out var value) && value != string.Empty)
                    .Select(x => $"[{values[x]}]"));

                return string.Join(" ", ipInfo);
            }
        }
    }
}