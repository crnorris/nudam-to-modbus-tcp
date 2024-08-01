using NuDAMBusMaster;
using System.Text.Json.Serialization;

namespace NuDAM_to_Modbus_TCP_Bridge
{
    public class BusConfig
    {
        [JsonPropertyName("baudRate")]
        public NuDAMBaudRate BaudRate { get; set; }

        [JsonPropertyName("busName")]
        public required string BusName { get; set; }

        [JsonPropertyName("checksumEnabled")]
        public bool ChecksumEnabled { get; set; }

        [JsonPropertyName("serialPort")]
        public required string SerialPort { get; set; }
    }

    public class ServerConfig
    {
        [JsonPropertyName("buses")]
        public required List<BusConfig> Buses { get; set; }

        [JsonPropertyName("serverName")]
        public required string ServerName { get; set; }

        [JsonPropertyName("tcpPort")]
        public required UInt16 TCPPort { get; set; }
    }
}