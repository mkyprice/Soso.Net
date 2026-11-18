using System;

namespace Soso.Net.Stream;

public class ByteBuffer
{
    public readonly byte[] Buffer;

    public int Position
    {
        get => _position;
        set => _position = value;
    }

    public int Count => _count;
    private int _count;
    private int _position;
    
    public ByteBuffer(int size)
    {
        Buffer = new byte[size];
    }

    public void Receive(int count)
    {
        _position += count;
        _count += count;
    }

    public void Flush()
    {
        _position = 0;
        _count = 0;
    }
    
    public void Consume(int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (count >= _count)
        {
            Flush();
            return;
        }

        int remaining = _count - count;
        System.Buffer.BlockCopy(Buffer, count, Buffer, 0, remaining);
        _position = remaining;
        _count = remaining;
    }
}