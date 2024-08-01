using CommandLine;
using NuDAMBusMaster;

namespace NuDAM_Poll
{
    internal abstract class VerbOptionsBase
    {
        [Option('b', "baud", Required = false, HelpText = "Provide baud rate. Must be one of the supported values.", Default = (int)NuDAMBusMasterClient.DefaultBaudRate)]
        public NuDAMBaudRate BaudRate { get; set; }

        [Option('s', "checksum", Required = false, HelpText = "Set true if checksum is enabled on the target.", Default = false)]
        public bool ChecksumEnabled { get; set; }

        [Option('a', "address", Required = false, HelpText = "Provide the module's address.", Default = NuDAMBusMasterClient.DefaultAddress)]
        public byte ModuleAddress { get; set; }

        [Option('p', "port", Required = true, HelpText = "Provide serial port name.")]
        public required string SerialPortIdentifier { get; set; }
    }
}