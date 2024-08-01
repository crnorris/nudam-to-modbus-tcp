// This file is based on the SlaveStorage sample included with NModbus
using NModbus;

namespace NuDAMModbusServer
{
    public enum PointOperation
    {
        Read,
        Write,
    }

    public class ExternalPointSource<TPoint> : IPointSource<TPoint>
    {
        public event EventHandler<StorageEventArgs<TPoint>>? StorageOperationOccurred;

        public TPoint[] ReadPoints(UInt16 startAddress, UInt16 numberOfPoints)
        {
            TPoint[] points = new TPoint[numberOfPoints];
            StorageOperationOccurred?.Invoke(this, new StorageEventArgs<TPoint>(PointOperation.Read, startAddress, points));
            return points;
        }

        public void WritePoints(UInt16 startAddress, TPoint[] points)
        {
            StorageOperationOccurred?.Invoke(this, new StorageEventArgs<TPoint>(PointOperation.Write, startAddress, points));
        }
    }

    public class SlaveStorage : ISlaveDataStore
    {
        public SlaveStorage()
        {
            CoilDiscretes = new ExternalPointSource<bool>();
            CoilInputs = new ExternalPointSource<bool>();
            HoldingRegisters = new ExternalPointSource<UInt16>();
            InputRegisters = new ExternalPointSource<UInt16>();
        }

        public ExternalPointSource<bool> CoilDiscretes { get; }

        IPointSource<bool> ISlaveDataStore.CoilDiscretes
        {
            get { return CoilDiscretes; }
        }

        public ExternalPointSource<bool> CoilInputs { get; }

        IPointSource<bool> ISlaveDataStore.CoilInputs
        {
            get { return CoilInputs; }
        }

        public ExternalPointSource<UInt16> HoldingRegisters { get; }

        IPointSource<UInt16> ISlaveDataStore.HoldingRegisters
        {
            get { return HoldingRegisters; }
        }

        public ExternalPointSource<UInt16> InputRegisters { get; }

        IPointSource<UInt16> ISlaveDataStore.InputRegisters
        {
            get { return InputRegisters; }
        }
    }

    public class StorageEventArgs<TPoint>(PointOperation operation, ushort startingAddress, TPoint[] points) : EventArgs
    {
        public PointOperation Operation { get; } = operation;

        public TPoint[] Points { get; } = points ?? throw new ArgumentNullException(nameof(points));

        public UInt16 StartingAddress { get; } = startingAddress;
    }
}