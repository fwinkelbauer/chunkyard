namespace Chunkyard.Core;

/// <summary>
/// An implementation of a lock-free ring buffer.
/// </summary>
public sealed class RingBuffer
{
    private readonly int[] _buffer;
    private readonly int _mask;

    private int _ticket;

    public RingBuffer(int size)
    {
        if (size < 2 || ((size & (size - 1)) != 0))
        {
            throw new ArgumentException(
                "Size must be a number that is a power of two");
        }

        _buffer = new int[size];
        _mask = size - 1;
        _ticket = 0;
    }

    public int Reserve()
    {
        int temp;

        do
        {
            temp = _ticket;
        }
        while (_buffer[temp & _mask] != 0
            || (Interlocked.CompareExchange(ref _ticket, temp + 1, temp) != temp));

        return temp;
    }

    public void Free(int ticket)
    {
        _buffer[ticket & _mask] = 0;
    }

    public int Read(int ticket)
    {
        return _buffer[ticket & _mask];
    }

    public void Write(int ticket, int number)
    {
        if (number <= 0)
        {
            throw new ArgumentException(
                "Number must be larger than zero",
                nameof(number));
        }

        _buffer[ticket & _mask] = number;
    }
}
