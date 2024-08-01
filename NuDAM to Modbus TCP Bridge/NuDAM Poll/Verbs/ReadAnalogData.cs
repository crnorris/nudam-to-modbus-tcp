using CommandLine;
using NuDAMBusMaster;

namespace NuDAM_Poll.Verbs
{
    internal class ReadAnalogData
    {
        private static bool allChannels;
        private static byte analogChannelIndex;
        private static NuDAMBusMasterClient? client;
        private static byte moduleAddress;

        internal static void DoWork(Options options)
        {
            if (options.AnalogChannelIndex == -1)
            {
                allChannels = true;
            }
            else
            {
                byte channelIndex = (byte)options.AnalogChannelIndex;

                if (!NuDAMBusMasterClient.ValidateAnalogChannelIndex(channelIndex))
                {
                    Console.Error.WriteLine("Invalid channel index.");
                    return;
                }

                analogChannelIndex = channelIndex;
            }

            client = new(options.SerialPortIdentifier, options.BaudRate, options.ChecksumEnabled);
            moduleAddress = options.ModuleAddress;

            client.Open();

            if (options.PollInterval == 0)
            {
                PollOnce();
            }
            else
            {
                PollRepeatedly(options.PollInterval);
            }

            client.Close();
        }

        private static void PollOnce()
        {
            if (client is null)
            {
                throw new NullReferenceException();
            }

            if (allChannels)
            {
                float[] values = client.ReadAllAnalogDataChannels(moduleAddress);

                for (int i = 0; i < values.Length; i++)
                {
                    Console.Write(values[i]);

                    if (i != values.Length - 1)
                    {
                        Console.Write(',');
                    }
                }

                Console.WriteLine();
            }
            else
            {
                float value = client.ReadAnalogDataFromChannelN(moduleAddress, analogChannelIndex);
                Console.WriteLine(value);
            }
        }

        private static void PollRepeatedly(int pollInterval)
        {
            System.Timers.Timer timer = new(pollInterval);
            timer.Elapsed += (s, e) => PollOnce();
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.WriteLine("Press any key to stop...");

            // Loop to keep the application running until a key is pressed
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
            }

            // Clean up
            timer.Stop();
            timer.Dispose();
        }

        [Verb("ranalog", HelpText = "Read analog data")]
        internal class Options : VerbOptionsBase
        {
            [Option('c', "channel", Required = false, HelpText = "Provide the analog channel index, or -1 to read all channels.", Default = -1)]
            public int AnalogChannelIndex { get; set; }

            [Option('i', "interval", Required = false, HelpText = "Provide the poll interval in milliseconds, or 0 to only read once.", Default = 0)]
            public int PollInterval { get; set; }
        }
    }
}