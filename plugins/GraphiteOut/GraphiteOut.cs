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

namespace Metricus.Plugins
{
    public class GraphiteOut : OutputPlugin, IOutputPlugin
    {
        class GraphiteOutConfig
        {
            public String Hostname { get; set; }
            public String Prefix { get; set; }
            public int Port { get; set; }
            public int Protocol { get; set; }
            public int SendBufferSize { get; set; }
        }

        private PluginManager pm;
        private GraphiteOutConfig config;
        private GraphiteTcpClient tcpClient;
        private BlockingCollection<metric> MetricSpool;
        private Task WorkMetricTask;
        private int DefaultSendBufferSize = 1000;

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
            Boolean done = false;
            while (!done)
            {
                foreach (var rawMetric in MetricSpool.GetConsumingEnumerable())
                {
                    ShipMetric(rawMetric);
                }
            }
        }

        private void ShipMetric(metric m)
        {
            bool sent = false;
            while (!sent)
            {
                try
                {
                    tcpClient = tcpClient ?? new GraphiteTcpClient(config.Hostname, config.Port, config.Prefix + "." + pm.Hostname);
                    var theMetric = FormatMetric(m);
                    var path = theMetric.category;
                    path += (theMetric.instance != "") ? "." + theMetric.instance : ".total";
                    path += "." + theMetric.type;
                    path = path.ToLower();
                    tcpClient.Send(path, (int)m.value);
                    Console.WriteLine(path);
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

        private metric FormatMetric(metric m)
        {
            m.category = Regex.Replace(m.category, "(\\s+|\\.|/)", "_");
            m.type = Regex.Replace(m.type, "(\\s+|\\.|/)", "_");
            m.instance = Regex.Replace(m.instance, "(\\s+|\\.|/)", "_");
            return m;
        }

        public class GraphiteTcpClient : IDisposable
        {
            public string Hostname { get; private set; }
            public int Port { get; private set; }
            public string KeyPrefix { get; private set; }

            private readonly TcpClient _tcpClient;

            public GraphiteTcpClient(string hostname, int port = 2003, string keyPrefix = null)
            {
                Hostname = hostname;
                Port = port;
                KeyPrefix = keyPrefix;
                _tcpClient = new TcpClient(Hostname, Port);
            }

            public void Send(string path, int value)
            {
                Send(path, value, DateTime.UtcNow);
            }

            public void Send(string path, int value, DateTime timeStamp)
            {
                    if (!string.IsNullOrWhiteSpace(KeyPrefix))
                        path = KeyPrefix + "." + path;
                    var message = Encoding.UTF8.GetBytes(string.Format("{0} {1} {2}\n", path, value, timeStamp.ToUnixTime()));
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

