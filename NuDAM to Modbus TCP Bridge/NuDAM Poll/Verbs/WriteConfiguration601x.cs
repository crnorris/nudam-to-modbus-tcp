using CommandLine;
using NuDAMBusMaster;
using System.Globalization;
using static NuDAMBusMaster.ND601xConfiguration;

namespace NuDAM_Poll.Verbs
{
    internal class WriteConfiguration601x
    {
        internal static void DoWork(Options options)
        {
            NuDAMBusMasterClient client = new(options.SerialPortIdentifier, options.BaudRate, options.ChecksumEnabled);

            client.Open();

            ND601xConfiguration newConfiguration = new();

            if (options.NewModuleAddress is byte newAddress)
            {
                newConfiguration.address = newAddress;
            }
            else
            {
                newConfiguration.address = options.ModuleAddress;
            }

            if (!byte.TryParse(options.AnalogInputRange, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte analogInputRangeValue))
            {
                Console.Error.WriteLine("Invalid analog input range code. Use hexadecimal format, two characters, e.g. \"0F\".");
                return;
            }
            newConfiguration.analogInputRange = (AnalogInputRange)analogInputRangeValue;
            if (!Enum.IsDefined(newConfiguration.analogInputRange))
            {
                Console.Error.WriteLine("Unknown analog input range code.");
                return;
            }

            if (options.NewBaudRate is NuDAMBaudRate newBaudRate)
            {
                newConfiguration.baudRate = newBaudRate;
            }
            else
            {
                newConfiguration.baudRate = options.BaudRate;
            }

            if (options.SetChecksumEnabled is bool setChecksumEnabled)
            {
                newConfiguration.ChecksumEnabled = setChecksumEnabled;
            }
            else
            {
                newConfiguration.ChecksumEnabled = options.ChecksumEnabled;
            }

            client.WriteConfiguration(options.ModuleAddress, newConfiguration);

            client.Close();
        }

        [Verb("setconf601x", HelpText = "Set ND-601x module's basic configuration")]
        internal class Options : VerbOptionsBase
        {
            [Option("ainrange", Required = true, HelpText = "Provide the analog input range code from Table 6-1 in the NuDAM-6000 user manual, or refer to the decal on the device itself.")]
            public required string AnalogInputRange { get; set; }

            [Option("newbaud", Required = false, HelpText = "Provide the new baud rate in bps (e.g. 115200). DEFAULT pin must have been connected to GND while powering the device on to allow this to be changed.")]
            public NuDAMBaudRate? NewBaudRate { get; set; }

            [Option("newaddr", Required = false, HelpText = "Provide the new address.")]
            public byte? NewModuleAddress { get; set; }

            [Option("setchecksum", Required = false, HelpText = "Set true to enable checksum on the target device. DEFAULT pin must have been connected to GND while powering the device on to allow this to be changed.")]
            public bool? SetChecksumEnabled { get; set; }
        }
    }
}