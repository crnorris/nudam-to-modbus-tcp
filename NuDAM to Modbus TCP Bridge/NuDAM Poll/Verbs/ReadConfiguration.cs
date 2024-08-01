using CommandLine;
using NuDAMBusMaster;

namespace NuDAM_Poll.Verbs
{
    internal class ReadConfiguration
    {
        internal static void DoWork(Options options)
        {
            NuDAMBusMasterClient client = new(options.SerialPortIdentifier, options.BaudRate, options.ChecksumEnabled);

            client.Open();

            Console.WriteLine("Reading device type... ");
            string moduleName = client.ReadModuleName(options.ModuleAddress);

            Console.Write(moduleName);
            Console.WriteLine();

            if (moduleName.StartsWith("601"))
            {
                ND601xConfiguration configuration = client.ReadND601xConfiguration(options.ModuleAddress);
                PrintConfiguration(configuration);
            }
            else
            {
                Console.Error.WriteLine($"Device is of model ND-{moduleName} but only ND-601x devices are currently supported for this function.");
            }

            client.Close();
        }

        private static string ConvertAnalogInputRangeToFriendlyName(ND601xConfiguration.AnalogInputRange analogInputRange)
        {
            string s = analogInputRange.ToString();
            string[] parts = s.Split("__");
            string result = parts[1];
            result = result.Replace("pm", "±");
            result = result.Replace("_", " ");
            result = result.Replace("TC", "Thermocouple");
            return result;
        }

        private static void PrintConfiguration(ND601xConfiguration c)
        {
            Console.WriteLine($"Address: {c.address}");
            Console.WriteLine($"Analog Input Range: {ConvertAnalogInputRangeToFriendlyName(c.analogInputRange)}");
            Console.WriteLine($"Baud Rate: {(int)c.baudRate}bps");
            Console.WriteLine($"Checksum enabled: {c.ChecksumEnabled}");
        }

        [Verb("readconf", HelpText = "Read NuDAM module's basic configuration")]
        internal class Options : VerbOptionsBase
        {
        }
    }
}