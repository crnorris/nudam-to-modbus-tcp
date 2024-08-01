# nudam-to-modbus-tcp
Utility that allows access to NuDAM serial devices through Modbus TCP.

Adlink's NuDAM remote I/O modules use a proprietary serial protocol over RS-485, rather than the common Modbus RTU, and is not compatible with Modbus RTU. However, the protocol is similar enough that translation is possible. This software runs on a PC and allows software designed to work with Modbus TCP to access the proprietary NuDAM bus. I originally wrote this so that my ND-6018 modules can be used with ModbusScope (https://github.com/ModbusScope/) which is very convenient.

Important notes:

- You still can't have NuDAM modules and Modbus RTU devices on the same RS485 bus. This software doesn't change how the modules communicate, it just adds a man in the middle to translate between Modbus TCP and the NuDAM protocol.
- Your software must support Modbus TCP, so if it only lets you select a COM port (serial port) then it's not going to work with this as it stands. Please submit an issue if this is something you need, as it should be possible to implement it.
- Only ND-6018 is currently supported, but support for other modules should be trivial to add. Please create an issue if you want support for a specific module.
- This work is not developed, authorized, supported, sponsored, or endorsed by Adlink.

I've included a small utility I wrote to aid my development called "nudampoll", which just lets you interact with the NuDAM bus. This is useful on its own. If all you need is the ability to log data from a NuDAM module, then you can achieve that using nudampoll and don't actually need to involve Modbus TCP at all.

Communication with the NuDAM bus is handled by a project that both the Modbus TCP server and nudampoll share. You could use that if you want to write your own program that directly supports NuDAM modules.
