using System.Globalization;
using System.IO.Ports;
using System.Text;

namespace NuDAMBusMaster
{
    public class NuDAMBusMasterClient
    {
        public const byte DefaultAddress = 1;
        public const NuDAMBaudRate DefaultBaudRate = NuDAMBaudRate.BPS_9600;
        public const int MaximumAnalogChannelIndex = 7;

        /// <summary>
        /// Length of what Adlink call "Engineering units", e.g. "+100.88".
        /// </summary>
        private const int AdlinkEngineeringUnitLength = 7;

        private const char commandNAckLeadingCode = '?';
        private const int maxResponseLength = 128;
        private const char packetEndMarker = '\r';
        private readonly bool checksumEnabled;
        private readonly SerialPort serialPort = new();
        private readonly object serialPortLock = new();

        public NuDAMBusMasterClient(string serialPortIdentifier, NuDAMBaudRate baudRate, bool checksumEnabled)
        {
            if (!SerialPort.GetPortNames().Contains(serialPortIdentifier))
            {
                throw new PortDoesNotExistException(serialPortIdentifier);
            }

            serialPort.PortName = serialPortIdentifier;

            if (!ValidateBaudRate(baudRate))
            {
                throw new InvalidBaudRateException();
            }

            serialPort.BaudRate = (int)baudRate;

            this.checksumEnabled = checksumEnabled;

            serialPort.ReadBufferSize = 4096;
            serialPort.ReadTimeout = 500;
        }

        public static bool ValidateAnalogChannelIndex(byte analogChannelIndex)
        {
            return analogChannelIndex <= MaximumAnalogChannelIndex;
        }

        public static bool ValidateBaudRate(NuDAMBaudRate baudRate) => Enum.IsDefined(baudRate);

        public void Close()
        {
            serialPort.Close();
        }

        public void Open()
        {
            serialPort.Open();
        }

        /// <summary>
        /// Read analog data from all channels of a NuDAM module.
        ///
        /// Defined in section 6.3.5, page 154, of NuDAM-6000 User's Guide.
        /// </summary>
        /// <param name="address">NuDAM module's address</param>
        /// <returns></returns>
        /// <exception cref="UnrecognizedResponseException"></exception>
        /// <exception cref="DeviceClaimsCommandisInvalidException"></exception>
        public float[] ReadAllAnalogDataChannels(byte address)
        {
            char[] response = WriteAndRead(BuildCommandString('#', address, "A"));

            if (!CommandIsAcknowledged(response[0], '>'))
            {
                throw new DeviceClaimsCommandisInvalidException();
            }

            string valuesString = new(response[1..]);

            List<string> valueStrings;

            try
            {
                valueStrings = DivideString(valuesString, AdlinkEngineeringUnitLength);
            }
            catch (InvalidDataException)
            {
                // Could not divide evenly
                throw new UnrecognizedResponseException();
            }

            List<float> values = [];

            foreach (string valueString in valueStrings)
            {
                bool success = float.TryParse(valueString, out float value);
                if (!success)
                {
                    throw new UnrecognizedResponseException();
                }

                values.Add(value);
            }

            return values.ToArray();
        }

        /// <summary>
        /// Read data for a range of analog channels. This function automatically chooses between
        /// <see cref="ReadAnalogDataFromChannelN(byte, byte)"/> and <see cref="ReadAllAnalogDataChannels(byte)"/>.
        /// </summary>
        /// <param name="address">NuDAM module's address</param>
        /// <param name="firstChannelIndex">Index of first channel to read.</param>
        /// <param name="numberOfChannelsToRead">Number of channels to read.</param>
        /// <returns>
        /// Array of analog channel data. This contains only the range requested, so beware that the
        /// index in the array will not match the channel index. For example, If you requested
        /// channels 2 to 4, the first index of the array will be the data for channel 2 and the
        /// array will have length 3.
        /// </returns>
        public float[] ReadAnalogChannelRange(byte address, byte firstChannelIndex, int numberOfChannelsToRead)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(firstChannelIndex, MaximumAnalogChannelIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(firstChannelIndex + numberOfChannelsToRead - 1, MaximumAnalogChannelIndex);
            ArgumentOutOfRangeException.ThrowIfLessThan(numberOfChannelsToRead, 1);

            float[] output = new float[numberOfChannelsToRead];

            if (numberOfChannelsToRead == 1)
            {
                // Just one channel so read Channel 'N', as it's more efficient in terms of serial
                // traffic than reading all of them just to discard most of the data.
                output[0] = ReadAnalogDataFromChannelN(address, firstChannelIndex);
            }
            else
            {
                // Multiple channels to read, so just read all of them and discard the data we don't
                // need in this case. It might actually be more efficient to do multiple discrete
                // Channel 'N' reads up to a certain number of channels, but the difference will
                // probably be negligible when you consider the extra overhead.
                float[] allChannelData = ReadAllAnalogDataChannels(address);
                Array.Copy(allChannelData, firstChannelIndex, output, 0, numberOfChannelsToRead);
            }
            return output;
        }

        /// <summary>
        /// Read analog data from channel 'N' (provided as argument) of a NuDAM module.
        ///
        /// Defined in section 6.3.4, page 153, of NuDAM-6000 User's Guide.
        ///
        /// TODO Currently only supports "Engineering Units" format.
        /// </summary>
        /// <param name="address">NuDAM module's address</param>
        /// <param name="channelIndex">Analog channel index</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="UnrecognizedResponseException"></exception>
        /// <exception cref="DeviceClaimsCommandisInvalidException"></exception>
        public float ReadAnalogDataFromChannelN(byte address, byte channelIndex)
        {
            if (channelIndex > 7)
            {
                throw new ArgumentOutOfRangeException(channelIndex.ToString());
            }

            char[] response = WriteAndRead(BuildCommandString('#', address, channelIndex.ToString()));

            if (CommandIsAcknowledged(response[0], '>'))
            {
                string valueString = new(response[1..]);
                bool success = float.TryParse(valueString, out float value);
                if (!success)
                {
                    throw new UnrecognizedResponseException();
                }

                return value;
            }
            else
            {
                throw new DeviceClaimsCommandisInvalidException();
            }
        }

        /// <summary>
        /// Read NuDAM module's name. This is its model name, and is read-only.
        ///
        /// Defined in section 6.2.3, page 146, of NuDAM-6000 User's Guide.
        /// </summary>
        /// <param name="address">NuDAM module's address</param>
        /// <returns>NuDAM module's name</returns>
        public string ReadModuleName(byte address)
        {
            char[] response = WriteAndRead(BuildCommandString('$', address, "M"));

            string addressOfResponderString = new(response[1..3]);
            byte addressOfResponder = ParseHexByte(addressOfResponderString);
            if (addressOfResponder != address)
            {
                throw new WrongDeviceRepliedException();
            }

            if (CommandIsAcknowledged(response[0], '!'))
            {
                string moduleName = new(response[3..]);
                return moduleName;
            }
            else
            {
                throw new DeviceClaimsCommandisInvalidException();
            }
        }

        /// <summary>
        /// Read am ND-601x module's basic configuration.
        ///
        /// Defined in section 6.2.2, page 143, of NuDAM-6000 User's Guide.
        /// </summary>
        /// <param name="address">NuDAM module's address</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ND601xConfiguration ReadND601xConfiguration(byte address)
        {
            ND601xConfiguration configuration = new();

            char[] response = WriteAndRead(BuildCommandString('$', address, "2"));

            if (!CommandIsAcknowledged(response[0], '!'))
            {
                throw new DeviceClaimsCommandisInvalidException();
            }

            string addressOfResponderString = new(response[1..3]);
            configuration.address = ParseHexByte(addressOfResponderString);

            string analogInputRangeString = new(response[3..5]);
            configuration.analogInputRange = (ND601xConfiguration.AnalogInputRange)ParseHexByte(analogInputRangeString);
            if (!Enum.IsDefined(configuration.analogInputRange))
            {
                throw new UnrecognizedResponseException();
            }

            string baudRateString = new(response[5..7]);
            byte baudRateValue = ParseHexByte(baudRateString);
            if (!ND601xConfiguration.LookupReceivedBaudRate.TryGetValue(baudRateValue, out configuration.baudRate))
            {
                throw new UnrecognizedResponseException();
            }

            string dataFormatString = new(response[7..9]);
            byte dataFormatValue = ParseHexByte(dataFormatString);

            configuration.ChecksumEnabled = (dataFormatValue & 0b01000000) != 0;

            configuration.analogInputDataFormat = (ND601xConfiguration.AnalogInputDataFormat)(dataFormatValue & 0b00000011);
            if (!Enum.IsDefined(configuration.analogInputDataFormat))
            {
                throw new UnrecognizedResponseException();
            }

            return configuration;
        }

        public void WriteConfiguration(byte address, ND601xConfiguration c)
        {
            StringBuilder commandSB = new();

            commandSB.Append('%');
            commandSB.Append(ByteToHexString(address));
            commandSB.Append(ByteToHexString(c.address));
            commandSB.Append(ByteToHexString((byte)c.analogInputRange));
            commandSB.Append(ByteToHexString(ND601xConfiguration.LookupBaudRateToSend[c.baudRate]));

            byte dataFormat = (byte)c.analogInputDataFormat;

            if (c.ChecksumEnabled)
            {
                dataFormat |= 0b01000000;
            }

            commandSB.Append(ByteToHexString(dataFormat));

            if (checksumEnabled)
            {
                commandSB.Append(ByteToHexString(CalculateChecksum(commandSB)));
            }

            commandSB.Append(packetEndMarker);

            char[] response = WriteAndRead(commandSB.ToString());

            if (!CommandIsAcknowledged(response[0], '!'))
            {
                throw new DeviceClaimsCommandisInvalidException();
            }

            string addressOfResponderString = new(response[1..3]);
            byte addressOfResponder = ParseHexByte(addressOfResponderString);
            if (addressOfResponder != c.address)
            {
                throw new WrongDeviceRepliedException();
            }
        }

        private static string ByteToHexString(byte b) => b.ToString("X2");

        private static byte CalculateChecksum(StringBuilder sb)
        {
            int calculatedChecksum = 0;

            for (int i = 0; i < sb.Length; i++)
            {
                calculatedChecksum += (byte)sb[i];
            }

            calculatedChecksum %= 0x100;

            return (byte)calculatedChecksum;
        }

        private static bool CommandIsAcknowledged(char responseLeadingCode, char ackCode)
        {
            bool ackd;

            if (responseLeadingCode == ackCode)
            {
                ackd = true;
            }
            else if (responseLeadingCode == commandNAckLeadingCode)
            {
                ackd = false;
            }
            else
            {
                throw new UnrecognizedResponseException();
            }

            return ackd;
        }

        private static List<string> DivideString(string str, int chunkSize)
        {
            if ((str.Length % chunkSize) != 0)
            {
                throw new InvalidDataException();
            }

            List<string> chunks = [];

            int numberOfChunks = str.Length / chunkSize;

            for (int i = 0; i < numberOfChunks; i++)
            {
                chunks.Add(str.Substring(i * chunkSize, chunkSize));
            }

            return chunks;
        }

        private static byte ParseHexByte(string hexByte) => byte.Parse(hexByte, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        private string BuildCommandString(char leadingCode, byte address, string argument)
        {
            StringBuilder commandSB = new();

            commandSB.Append(leadingCode);
            commandSB.Append(ByteToHexString(address));
            commandSB.Append(argument);

            if (checksumEnabled)
            {
                commandSB.Append(ByteToHexString(CalculateChecksum(commandSB)));
            }

            commandSB.Append(packetEndMarker);

            return commandSB.ToString();
        }

        private char[] ReadResponse()
        {
            StringBuilder responseSB = new();

            while (responseSB.Length <= maxResponseLength)
            {
                int newValue = serialPort.ReadChar();

                if (newValue == -1)
                {
                    throw new EndOfStreamException();
                }
                else
                {
                    char newChar = (char)newValue;

                    if (newChar == packetEndMarker)
                    {
                        break;
                    }
                    else
                    {
                        responseSB.Append(newChar);
                    }
                }
            }

            if (checksumEnabled)
            {
                try
                {
                    int providedChecksum = ParseHexByte(responseSB.ToString(responseSB.Length - 2, 2));

                    responseSB.Remove(responseSB.Length - 2, 2);

                    int calculatedChecksum = CalculateChecksum(responseSB);

                    if (calculatedChecksum != providedChecksum)
                    {
                        throw new ChecksumMismatchException();
                    }
                }
                catch (FormatException)
                {
                    throw new UnrecognizedResponseException();
                }
            }

            return responseSB.ToString().ToCharArray();
        }

        private char[] WriteAndRead(string command)
        {
            char[] response;
            lock (serialPortLock)
            {
                WriteCommand(command);
                response = ReadResponse();
            }
            return response;
        }

        private void WriteCommand(string command)
        {
            // Clear the receive buffer before sending a command, just in case something has ended up in there
            serialPort.DiscardInBuffer();
            serialPort.Write(command);
        }

        public class ChecksumMismatchException : Exception
        {
        }

        public class DeviceClaimsCommandisInvalidException : Exception
        {
        }

        public class InvalidBaudRateException : Exception
        {
        }

        public class PortDoesNotExistException(string portName) : Exception(portName)
        {
        }

        public class UnrecognizedResponseException : Exception
        {
        }

        public class WrongDeviceRepliedException : Exception
        {
        }
    }
}