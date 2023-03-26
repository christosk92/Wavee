namespace Wavee.Playback.Volume.Helper;

public class AtomicDouble
{
    private long value;

    public AtomicDouble(double initialValue)
    {
        value = BitConverter.DoubleToInt64Bits(initialValue);
    }

    public double Load()
    {
        return BitConverter.Int64BitsToDouble(Interlocked.Read(ref value));
    }

    public void Store(double newValue)
    {
        long newBits = BitConverter.DoubleToInt64Bits(newValue);
        Interlocked.Exchange(ref value, newBits);
    }
}