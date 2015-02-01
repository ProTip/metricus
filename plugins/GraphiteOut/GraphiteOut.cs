using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Metricus.Plugin;
using ServiceStack.Text;
using Graphite;

namespace Metricus.Plugins
{
    public class GraphiteOut : OutputPlugin, IOutputPlugin
    {
        class GraphiteOutConfig
        {
            public String Hostname { get; set; }
            public String Prefix { get; set; }
            public int Port { get; set; }
            public string Protocol { get; set; }
            public int SendBufferSize { get; set; }
        }

        private PluginManager pm;
        private GraphiteOutConfig config;
        private MetricusGraphiteTcpClient tcpClient;
        private GraphiteUdpClient udpClient;
        private BlockingCollection<metric> MetricSpool;
        private Task WorkMetricTask;
        private int DefaultSendBufferSize = 1000;
        public static readonly string FormatReplacementMatch = "(\\s+|\\.|/|\\(|\\))";
        public static readonly string FormatReplacementString = "_";

        public GraphiteOut(PluginManager pm)
            : base(pm)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            config = JsonSerializer.DeserializeFromString<GraphiteOutConfig>(File.ReadAllText(path + "/config.json"));
            Console.WriteLine("Loaded config : {0}", config.Dump());
            this.pm = pm;
            if (config.SendBufferSize == 0) config.SendBufferSize = DefaultSendBufferSize;
            MetricSpool = new BlockingCollection<metric>(config.SendBufferSize);
            WorkMetricTask = Task.Factory.StartNew(() => WorkMetrics(), TaskCreationOptions.LongRunning);
        }

        public override void Work(List<metric> m)
        {
            foreach (var rawMetric in m) { MetricSpool.TryAdd(rawMetric); }
        }

        private void WorkMetrics()
        {
            Action<metric> shipMethod;
            switch(config.Protocol.ToLower())
            {
                case "tcp":
                    shipMethod = (m) => ShipMetricTCP(m); break;
                case "udp":
                    shipMethod = (m) => ShipMetricUDP(m); break;
                default:
                    shipMethod = (m) => ShipMetricUDP(m); break;
            }

            Boolean done = false;
            while (!done)
            {
                foreach (var rawMetric in MetricSpool.GetConsumingEnumerable())
                {
                    shipMethod(rawMetric);
                }
            }
        }

        private void ShipMetricUDP(metric m)
        {
            udpClient = udpClient ?? new GraphiteUdpClient(config.Hostname, config.Port, config.Prefix + "." + pm.Hostname);
            var theMetric = FormatMetric(m);
            var path = MetricPath(theMetric);
            udpClient.Send(path, (int)m.value);
        }

        private void ShipMetricTCP(metric m)
        {
            bool sent = false;
            while (!sent)
            {
                try
                {
                    tcpClient = tcpClient ?? new MetricusGraphiteTcpClient(config.Hostname, config.Port, config.Prefix + "." + pm.Hostname);
                    var theMetric = FormatMetric(m);
                    var path = MetricPath(theMetric);
                    tcpClient.Send(path, m.value);
                    sent = true;
                }
                catch (Exception e) //There has been some sort of error with the client
                {
                    Console.WriteLine(e);
                    if (tcpClient != null) tcpClient.Dispose();
                    tcpClient = null;
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        private string MetricPath(metric m)
        {
            var path = m.category;
            path += (m.instance != "") ? "." + m.instance : ".total";
            path += "." + m.type;
            return path.ToLower();
        }

        private metric FormatMetric(metric m)
        {
            m.category = Regex.Replace(m.category, FormatReplacementMatch, FormatReplacementString);
            m.type = Regex.Replace(m.type, FormatReplacementMatch, FormatReplacementString);
            m.instance = Regex.Replace(m.instance, FormatReplacementMatch, FormatReplacementString);
            return m;
        }

        public class MetricusGraphiteTcpClient : IDisposable
        {
            public string Hostname { get; private set; }
            public int Port { get; private set; }
            public string KeyPrefix { get; private set; }

            private readonly TcpClient _tcpClient;

            public MetricusGraphiteTcpClient(string hostname, int port = 2003, string keyPrefix = null)
            {
                Hostname = hostname;
                Port = port;
                KeyPrefix = keyPrefix;
                _tcpClient = new TcpClient(Hostname, Port);
            }

            public void Send(string path, float value)
            {
                Send(path, value, DateTime.UtcNow);
            }

            public void Send(string path, float value, DateTime timeStamp)
            {
                    if (!string.IsNullOrWhiteSpace(KeyPrefix))
                        path = KeyPrefix + "." + path;
                    var message = Encoding.UTF8.GetBytes(string.Format("{0} {1} {2}\n", path, value, ServiceStack.Text.DateTimeExtensions.ToUnixTime(timeStamp.ToUniversalTime())));
                    _tcpClient.GetStream().Write(message, 0, message.Length);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing) return;
                if (_tcpClient != null)
                    _tcpClient.Close();
            }

        }

    }
}

