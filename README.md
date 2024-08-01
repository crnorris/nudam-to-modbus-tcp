# nudam-to-modbus-tcp
Utility that allows access to NuDAM serial devices through Modbus TCP.

Adlink's NuDAM remote I/O modules use a proprietary serial protocol over RS-485, rather than the common Modbus RTU, and is not compatible with Modbus RTU. However, the protocol is similar enough that translation is possible. This software runs on a PC and allows software designed to work with Modbus TCP to access the proprietary NuDAM bus. I originally wrote this so that my ND-6018 modules can be used with ModbusScope (https://github.com/ModbusScope/) which is very convenient (this project is not affiliated with ModbusScope).

Important notes:

- You still can't have NuDAM modules and Modbus RTU devices on the same RS485 bus. This software doesn't change how the modules communicate, it just adds a man in the middle to translate between Modbus TCP and the NuDAM protocol.
- Your software must support Modbus TCP, so if it only lets you select a COM port (serial port) then it's not going to work with this as it stands. Please submit an issue if this is something you need, as it should be possible to implement it.
- Only ND-6018 is currently supported, but support for other modules should be trivial to add. Please create an issue if you want support for a specific module.
- This work is not developed, authorized, supported, sponsored, or endorsed by Adlink.

I've included a small utility I wrote to aid my development called "nudampoll", which just lets you interact with the NuDAM bus. This is useful on its own. If all you need is the ability to log data from a NuDAM module, then you can achieve that using nudampoll and don't actually need to involve Modbus TCP at all. Some configuration is also possible via this utility (configuration changes are not implemented via Modbus TCP).

Communication with the NuDAM bus is handled by a project that both the Modbus TCP server and nudampoll share. You could use that if you want to write your own program that directly supports NuDAM modules.

# Getting Started with NuDAM to Modbus TCP

1. Download the latest release (see the Releases list).
2. Open a command terminal (e.g. cmd).
3. Run the command (see below).

The command you need to run will depend on your setup. If you run `nudammodbustcp.exe -h`, the program will output the built-in help, documenting the command line options.

I have my NuDAM modules set to run at 115,200bps, and checksums enabled. My NuDAM bus is connected to a USB Serial adapter named COM5. So I run:

`nudammodbustcp.exe -b 115200 -s -p COM5`

Note that I don't need to specify the addresses of my NuDAM modules. This is because Modbus TCP still includes the unit ID. The unit ID included in the Modbus TCP packet will be used as the address for the NuDAM module.

# Floating Point Values

Rather than trying to support "real" floating point values, I have simply provided extra input registers. Reading input register 1 will give you the value x1, reading input register 11 will give you the value x10, and reading input register 21 will give you the value x100. ModbusScope allows you to divide this back down again to have the data displayed as floating point. For example, I have an expression in my "Register Settings" window of `${30021@3}/100`, which represents a T type thermocouple connected to one of my ND-6018 modules. The data is received as an integer x100 larger than it should be, and then ModbusScope divides it back to its proper value.
