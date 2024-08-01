using CommandLine;
using NuDAMBusMaster;

namespace NuDAM_Poll.Verbs
{
    internal class ReadModuleName
    {
        internal static void DoWork(Options options)
        {
            NuDAMBusMasterClient client = new(options.SerialPortIdentifier, options.BaudRate, options.ChecksumEnabled);

            client.Open();

            string moduleName = client.ReadModuleName(options.ModuleAddress);

            client.Close();

            Console.WriteLine(moduleName);
        }

        [Verb("model", HelpText = "Read NuDAM module's model name")]
        internal class Options : VerbOptionsBase
        {
        }
    }
}