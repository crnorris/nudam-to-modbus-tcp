using NModbus;
using NuDAMBusMaster;
using System.Net;
using System.Net.Sockets;

namespace NuDAMModbusServer
{
    public class NuDAMModbusTCPServer : IModbusLogger
    {
        public const UInt16 DefaultModbusTCPPort = 502;
        private const byte MaxUnitID = 255;
        private readonly IModbusTcpSlaveNetwork network;
        private readonly NuDAMBusMasterClient nudamBusClient;
        private readonly TcpListener tcpListener;
        private readonly IModbusSlave[] virtualModbusSlaves = new IModbusSlave[byte.MaxValue + 1];

        public NuDAMModbusTCPServer(string serialPortIdentifier, NuDAMBaudRate baudRate, bool checksumEnabled, UInt16 tcpPort = DefaultModbusTCPPort)
        {
            nudamBusClient = new(serialPortIdentifier, baudRate, checksumEnabled);

            tcpListener = new(IPAddress.Any, tcpPort);

            ModbusFactory factory = new(logger:this);
            network = factory.CreateSlaveNetwork(tcpListener);

            for (int i = 0; i <= MaxUnitID; i++)
            {
                byte unitId = (byte)i;
                SlaveStorage slaveStorage = new();
                slaveStorage.InputRegisters.StorageOperationOccurred += (s, e) => InputRegisters_StorageOperationOccurred(unitId, e);
                slaveStorage.CoilDiscretes.StorageOperationOccurred += (s, e) => CoilDiscretes_StorageOperationOccurred(unitId, e);
                slaveStorage.CoilInputs.StorageOperationOccurred += (s, e) => CoilInputs_StorageOperationOccurred(unitId, e);
                slaveStorage.HoldingRegisters.StorageOperationOccurred += (s, e) => HoldingRegisters_StorageOperationOccurred(unitId, e);
                IModbusSlave slave = factory.CreateSlave(unitId, slaveStorage);
                virtualModbusSlaves[unitId] = slave;
                network.AddSlave(slave);
            }
        }

        public event EventHandler<ExceptionEventArgs>? OnException;

        public event EventHandler<ActivityEventArgs>? OnInvalidActivity;

        public event EventHandler<ActivityEventArgs>? OnValidActivity;

        public event EventHandler<string>? OnNModbusLog;

        public enum RegisterType
        {
            Input,
            DiscreteCoil,
            DiscreteInput,
            Holding,
        }

        public enum RequestType
        {
            Read,
            Write,
        }

        private enum InputRegisterRange : UInt16
        {
            AnalogDataChannel0_integer_x1 = 0,
            AnalogDataChannel0_integer_x10 = 10,
            AnalogDataChannel0_integer_x100 = 20,
            AnalogDataChannel0_float_32bit = 50,
        }

        public void Log(LoggingLevel level, string message) => OnNModbusLog?.Invoke(this, message);

        public bool ShouldLog(LoggingLevel level)
        {
            // TODO ignore for now
            return true;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            nudamBusClient.Open();

            tcpListener.Start();

            _ = network.ListenAsync(cancellationToken);

            await Task.Run(cancellationToken.WaitHandle.WaitOne);

            nudamBusClient.Close();
        }

        private static RequestType ConvertRequestType(PointOperation o)
        {
            return o switch
            {
                PointOperation.Read => RequestType.Read,
                PointOperation.Write => RequestType.Write,
                _ => throw new ArgumentOutOfRangeException(o.ToString()),
            };
        }

        private static InputRegisterRange LookupInputRegisterRange(UInt16 startingAddress)
        {
            // Beware that this implementation will pick the range based on the range to which the
            // starting address belongs. If you read beyond that range, you will get invalid results.

            InputRegisterRange[] ranges = (InputRegisterRange[])Enum.GetValues(typeof(InputRegisterRange));
            for (int i = ranges.Length - 1; i >= 0; i--)
            {
                if ((UInt16)ranges[i] <= startingAddress + 1)
                {
                    return ranges[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(startingAddress));
        }

        private void CoilDiscretes_StorageOperationOccurred(byte unitId, StorageEventArgs<bool> e)
        {
            OnInvalidActivity?.Invoke(this, new ActivityEventArgs(unitId, ConvertRequestType(e.Operation), RegisterType.DiscreteCoil, e.StartingAddress, e.Points.Length));
            throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalFunction);
        }

        private void CoilInputs_StorageOperationOccurred(byte unitId, StorageEventArgs<bool> e)
        {
            OnInvalidActivity?.Invoke(this, new ActivityEventArgs(unitId, ConvertRequestType(e.Operation), RegisterType.DiscreteInput, e.StartingAddress, e.Points.Length));
            throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalFunction);
        }

        private void HandleInputRegisterRead(byte unitId, UInt16 startingAddress, UInt16[] points)
        {
            InputRegisterRange range = LookupInputRegisterRange(startingAddress);

            // Normalize the starting channel index
            byte startingChannelIndex = (byte)(startingAddress - range);

            switch (range)
            {
                case InputRegisterRange.AnalogDataChannel0_integer_x1:
                    ReadAnalogAndConvertToUInt(unitId, startingChannelIndex, points, 1);
                    break;

                case InputRegisterRange.AnalogDataChannel0_integer_x10:
                    ReadAnalogAndConvertToUInt(unitId, startingChannelIndex, points, 10);
                    break;

                case InputRegisterRange.AnalogDataChannel0_integer_x100:
                    ReadAnalogAndConvertToUInt(unitId, startingChannelIndex, points, 100);
                    break;

                case InputRegisterRange.AnalogDataChannel0_float_32bit:
                    // TODO implement 32 bit float support
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(range));
            }
        }

        private void HoldingRegisters_StorageOperationOccurred(byte unitId, StorageEventArgs<UInt16> e)
        {
            OnInvalidActivity?.Invoke(this, new ActivityEventArgs(unitId, ConvertRequestType(e.Operation), RegisterType.Holding, e.StartingAddress, e.Points.Length));
            throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalFunction);
        }

        private void InputRegisters_StorageOperationOccurred(byte unitId, StorageEventArgs<UInt16> e)
        {
            UInt16[] affectedRegisters = new UInt16[e.Points.Length];

            try
            {
                switch (e.Operation)
                {
                    case PointOperation.Read:
                        HandleInputRegisterRead(unitId, e.StartingAddress, e.Points);
                        break;

                    case PointOperation.Write:
                        // Input registers are read-only!
                        throw new InvalidOperationException();

                    default:
                        throw new InvalidOperationException();
                }

                OnValidActivity?.Invoke(this, new ActivityEventArgs(unitId, ConvertRequestType(e.Operation), RegisterType.Input, e.StartingAddress, e.Points.Length));
            }
            catch (ArgumentOutOfRangeException)
            {
                OnInvalidActivity?.Invoke(this, new ActivityEventArgs(unitId, ConvertRequestType(e.Operation), RegisterType.Input, e.StartingAddress, e.Points.Length));

                throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalDataAddress);
            }
            catch (Exception ex)
            {
                OnException?.Invoke(this, new ExceptionEventArgs(ex, new ActivityEventArgs(unitId, ConvertRequestType(e.Operation), RegisterType.Input, e.StartingAddress, e.Points.Length)));

                if (ex is NotImplementedException)
                {
                    throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalFunction);
                }

                if (ex is TimeoutException)
                {
                    throw new InvalidModbusRequestException(SlaveExceptionCodes.SlaveDeviceFailure);
                }

                if (ex is InvalidOperationException ioe && ioe.Message.Contains("The port is closed"))
                {
                    throw new InvalidModbusRequestException(SlaveExceptionCodes.SlaveDeviceFailure);
                }
            }
        }

        private void ReadAnalogAndConvertToUInt(byte unitId, byte startingChannelIndex, UInt16[] pointsOutput, uint scalar)
        {
            ArgumentOutOfRangeException.ThrowIfZero(scalar);

            int numberOfChannels = pointsOutput.Length;

            float[] data = nudamBusClient.ReadAnalogChannelRange(unitId, startingChannelIndex, numberOfChannels);

            for (int i = 0; i < numberOfChannels; i++)
            {
                pointsOutput[i] = (UInt16)Math.Round(data[i] * scalar);
            }
        }

        public class ActivityEventArgs(byte unitID, RequestType requestType, RegisterType registerType, UInt16 firstRegister, int registerCount)
        {
            public UInt16 FirstRegister { get; } = firstRegister;
            public int RegisterCount { get; } = registerCount;
            public RegisterType RegisterType { get; } = registerType;
            public RequestType RequestType { get; } = requestType;
            public byte UnitID { get; } = unitID;
        }

        public class BusDefinition(string serialPort, NuDAMBaudRate baudRate, bool checksumEnabled)
        {
            public NuDAMBaudRate baudRate = baudRate;
            public bool checksumEnabled = checksumEnabled;
            public string serialPort = serialPort;
        }

        public class ExceptionEventArgs(Exception exception, ActivityEventArgs activityEventArgs) : EventArgs
        {
            public ActivityEventArgs ActivityEventArgs { get; } = activityEventArgs;
            public Exception Exception { get; } = exception;
        }
    }
}