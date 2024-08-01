namespace NuDAMBusMaster
{
    public class ND601xConfiguration : NuDAMModuleConfiguration
    {
        public static readonly Dictionary<NuDAMBaudRate, byte> LookupBaudRateToSend = new()
        {
            { NuDAMBaudRate.BPS_1200  , 0x03 },
            { NuDAMBaudRate.BPS_2400  , 0x04 },
            { NuDAMBaudRate.BPS_4800  , 0x05 },
            { NuDAMBaudRate.BPS_9600  , 0x06 },
            { NuDAMBaudRate.BPS_19200 , 0x07 },
            { NuDAMBaudRate.BPS_38400 , 0x08 },
            { NuDAMBaudRate.BPS_115200 , 0x09 },
            { NuDAMBaudRate.BPS_57600, 0x0A },
        };

        public static readonly Dictionary<byte, NuDAMBaudRate> LookupReceivedBaudRate = new()
        {
            { 0x03, NuDAMBaudRate.BPS_1200   },
            { 0x04, NuDAMBaudRate.BPS_2400   },
            { 0x05, NuDAMBaudRate.BPS_4800   },
            { 0x06, NuDAMBaudRate.BPS_9600   },
            { 0x07, NuDAMBaudRate.BPS_19200  },
            { 0x08, NuDAMBaudRate.BPS_38400  },
            { 0x09, NuDAMBaudRate.BPS_115200   },
            { 0x0A, NuDAMBaudRate.BPS_57600},
        };

        public AnalogInputDataFormat analogInputDataFormat;
        public AnalogInputRange analogInputRange;
        public bool ChecksumEnabled;

        /// <summary>
        /// This is taken from Figure 6-1, page 139, of the NuDAM-6000 User's Guide.
        /// </summary>
        public enum AnalogInputDataFormat
        {
            Engineering_Units = 0x00,
            PC_of_Full_Scale = 0x01,
            Twos_Complement_of_Hexadecimal = 0x10,
            Ohms = 0x11,
        }

        /// <summary>
        /// This is taken from Table 6-1, page 136, of the NuDAM-6000 User's Guide.
        /// </summary>
        public enum AnalogInputRange : byte
        {
            ND6018__pm_15mV = 0x00,
            ND6018__pm_50mV = 0x01,
            ND6018__pm_100mV = 0x02,
            ND6018__pm_500mV = 0x03,
            ND6018__pm_1V = 0x04,
            ND6018__pm_2V5 = 0x05,
            ND6018__pm_20mA = 0x06,
            ND6018__TC_Type_J = 0x0E,
            ND6018__TC_Type_K = 0x0F,
            ND6018__TC_Type_T = 0x10,
            ND6018__TC_Type_E = 0x11,
            ND6018__TC_Type_R = 0x12,
            ND6018__TC_Type_S = 0x13,
            ND6018__TC_Type_B = 0x14,
            ND6018__TC_Type_N = 0x15,
            ND6018__TC_Type_C = 0x16,

            ND6017__pm_10V = 0x08,
            ND6017__pm_5V = 0x09,
            ND6017__pm_1V = 0x0A,
            ND6017__pm_500mV = 0x0B,
            ND6017__pm_150mV = 0x0C,
            ND6017__pm_20mA = 0x0D,

            ND6013__Pt100_A = 0x21,
            ND6013__Pt100_B = 0x22,
            ND6013__Pt100_C = 0x23,
            ND6013__Pt100_D = 0x24,
            ND6013__Pt100_E = 0x25,
            ND6013__Pt100_F = 0x26,
            ND6013__Pt100_G = 0x27,
            ND6013__Ni_100 = 0x28,
            ND6013__Ni_120 = 0x29,
            ND6013__0_60_Ohms = 0x2A,
        }
    }
}