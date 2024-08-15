using CommandLine;
using NuDAMBusMaster;
using NuDAMModbusServer;

namespace NuDAM_to_Modbus_TCP_Bridge
{
    internal class Program
    {
        private static bool registersStartFrom0;

        private static void DoWork(Options o)
        {
            Console.WriteLine($"Starting server at port {o.TCPPort}, using serial port {o.SerialPortIdentifier} @ {(int)o.BaudRate:N0}bps (Checksum {(o.ChecksumEnabled ? "Enabled" : "Disabled")})");

            NuDAMModbusTCPServer server = new(o.SerialPortIdentifier, o.BaudRate, o.ChecksumEnabled, o.TCPPort);

            if (o.Verbose)
            {
                server.OnValidActivity += Server_OnValidActivity;
                server.OnNModbusLog += Server_OnNModbusLog;
            }

            server.OnInvalidActivity += Server_OnInvalidActivity;

            server.OnException += Server_OnException;

            registersStartFrom0 = o.RegistersStartFrom0;

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            _ = server.Start(cancellationTokenSource.Token);


            Console.WriteLine("Press any key followed by enter to stop...");
            Console.Read();

            cancellationTokenSource.Cancel();
        }

        private static void GetFirstAndLastRegister(NuDAMModbusTCPServer.ActivityEventArgs e, out int first, out int last)
        {
            first = e.FirstRegister + (registersStartFrom0 ? 0 : 1);
            last = first + e.RegisterCount - 1;
        }

        private static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(DoWork)
                    .WithNotParsed(OnNotParsed);
            }
            catch (NuDAMBusMasterClient.PortDoesNotExistException e)
            {
                Console.Error.WriteLine($"Error: The specified serial port \"{e.Message}\" does not exist.");
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

        private static void Server_OnException(object? sender, NuDAMModbusTCPServer.ExceptionEventArgs ex)
        {
            NuDAMModbusTCPServer.ActivityEventArgs e = ex.ActivityEventArgs;
            GetFirstAndLastRegister(e, out int first, out int last);
            Console.Error.WriteLine($"EXCEPTION OCURRED: \"{ex.Exception.Message}\". WHILE CARRYING OUT REQUEST: {e.RequestType}: Unit={e.UnitID}, {e.RegisterType} Register, {first}{(e.RegisterCount > 1 ? $" to {last} inclusive" : "")}");
        }

        private static void Server_OnNModbusLog(object? sender, string msg)
        {
            Console.WriteLine($"NModbus: {msg}");
        }

        private static void Server_OnInvalidActivity(object? sender, NuDAMModbusTCPServer.ActivityEventArgs e)
        {
            GetFirstAndLastRegister(e, out int first, out int last);
            Console.Error.WriteLine($"ONE OR MORE REGISTERS INVALID IN REQUEST: {e.RequestType}: Unit={e.UnitID}, {e.RegisterType} Register, {first}{(e.RegisterCount > 1 ? $" to {last} inclusive" : "")}");
        }

        private static void Server_OnValidActivity(object? sender, NuDAMModbusTCPServer.ActivityEventArgs e)
        {
            GetFirstAndLastRegister(e, out int first, out int last);
            Console.WriteLine($"{e.RequestType}: Unit={e.UnitID}, {e.RegisterType} Register, {first}{(e.RegisterCount > 1 ? $" to {last} inclusive" : "")}");
        }

        private class Options
        {
            [Option('b', "baud", Required = false, HelpText = "Provide baud rate. Must be one of the supported values.", Default = (int)NuDAMBusMasterClient.DefaultBaudRate)]
            public NuDAMBaudRate BaudRate { get; set; }

            [Option('s', "checksum", Required = false, HelpText = "Set true if checksum is enabled on the target.", Default = false)]
            public bool ChecksumEnabled { get; set; }

            [Option('z', Required = false, Default = false, HelpText = "Prints register addresses as if they started from zero. Default Modbus behavior is that addresses start from 1. This only affects printed output from this program and not how it receives or responds to requests from clients.")]
            public bool RegistersStartFrom0 { get; set; }

            [Option('p', "serialport", Required = true, HelpText = "Provide serial port name.")]
            public required string SerialPortIdentifier { get; set; }

            [Option('t', "tcpport", Required = false, Default = NuDAMModbusTCPServer.DefaultModbusTCPPort, HelpText = "Provide TCP port number.")]
            public required UInt16 TCPPort { get; set; }

            [Option('v', Default = false, HelpText = "Prints activity to standard output.")]
            public bool Verbose { get; set; }
        }
    }
}