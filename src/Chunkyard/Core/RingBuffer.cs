namespace Chunkyard.Core;

/// <summary>
/// An implementation of a lock-free ring buffer which supports multiple
/// writers.
/// </summary>
internal class RingBuffer
{
    private readonly byte[][] _ringBuffer;
    private readonly int[] _bufferLenghts;

    private int _latestTicket;

    public RingBuffer(int ringLength, int bufferLength)
    {
        if (ringLength < 2)
        {
            throw new ArgumentException(
                "Ring buffer must be able to hold more than a single value");
        }

        _ringBuffer = new byte[ringLength][];
        _bufferLenghts = new int[ringLength];

        for (var i = 0; i < _ringBuffer.Length; i++)
        {
            _ringBuffer[i] = new byte[bufferLength];
        }

        _latestTicket = 0;
    }

    public byte[] GetWriteBuffer(int ticket)
    {
        return _ringBuffer[ToIndex(ticket)];
    }

    public ReadOnlySpan<byte> GetReadBuffer(int ticket)
    {
        var index = ToIndex(ticket);

        return new ReadOnlySpan<byte>(
            _ringBuffer[index],
            0,
            _bufferLenghts[index]);
    }

    public int? ReserveTicket()
    {
        var currentTicket = _latestTicket;
        var newTicket = currentTicket + 1;
        var free = _bufferLenghts[ToIndex(newTicket)] == 0;

        if (!free)
        {
            return null;
        }

        if (Interlocked.CompareExchange(ref _latestTicket, newTicket, currentTicket)
            != currentTicket)
        {
            return null;
        }

        return newTicket;
    }

    public int ReserveTicketBlocking()
    {
        int? ticket;

        while ((ticket = ReserveTicket()) == null)
        {
            Thread.Yield();
        }

        return ticket.Value;
    }

    public void CommitTicketWrite(int ticket, int bytesWritten)
    {
        _bufferLenghts[ToIndex(ticket)] = bytesWritten;
    }

    public void CommitTicketRead(int ticket)
    {
        _bufferLenghts[ToIndex(ticket)] = 0;
    }

    private int ToIndex(int ticket)
    {
        return ticket % _ringBuffer.Length;
    }
}
