namespace NuDAMBusMaster
{
    public enum NuDAMBaudRate
    {
        /* BPS_600 = 600, */ // This apparently exists but the configuration option to apply it is not documented.
        BPS_1200 = 1200,
        BPS_2400 = 2400,
        BPS_4800 = 4800,
        BPS_9600 = 9600,
        BPS_19200 = 19200,
        BPS_38400 = 38400,
        BPS_57600 = 57600,
        BPS_115200 = 115200,
    }
}