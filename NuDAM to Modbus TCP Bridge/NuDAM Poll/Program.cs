using CommandLine;
using NuDAM_Poll.Verbs;
using static NuDAMBusMaster.NuDAMBusMasterClient;

namespace NuDAM_Poll
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<ReadModuleName.Options, ReadAnalogData.Options, ReadConfiguration.Options, WriteConfiguration601x.Options>(args)
                    .WithParsed<ReadModuleName.Options>(ReadModuleName.DoWork)
                    .WithParsed<ReadAnalogData.Options>(ReadAnalogData.DoWork)
                    .WithParsed<ReadConfiguration.Options>(ReadConfiguration.DoWork)
                    .WithParsed<WriteConfiguration601x.Options>(WriteConfiguration601x.DoWork)
                    .WithNotParsed(OnNotParsed);
            }
            catch (TimeoutException)
            {
                Console.Error.WriteLine("Error: The request timed out.");
            }
            catch (ChecksumMismatchException)
            {
                Console.Error.WriteLine("Error: Checksum mismatch in response. Check cabling and configuration.");
            }
            catch (DeviceClaimsCommandisInvalidException)
            {
                Console.Error.WriteLine("Error: Target device claims the command is invalid. It may not support this type of command.");
            }
            catch (InvalidBaudRateException)
            {
                Console.Error.WriteLine("Error: The specified baud rate is not supported.");
            }
            catch (PortDoesNotExistException e)
            {
                Console.Error.WriteLine($"Error: The specified serial port \"{e.Message}\" does not exist.");
            }
            catch (UnrecognizedResponseException)
            {
                Console.Error.WriteLine("Error: The device's response is not recognized.");
            }
            catch (WrongDeviceRepliedException)
            {
                Console.Error.WriteLine("Error: The address in the response differs from the address specified in the request.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        private static void OnNotParsed(IEnumerable<Error> errors)
        {
            // Do nothing.
        }
    }
}