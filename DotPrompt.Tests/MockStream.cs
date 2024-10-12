namespace DotPrompt.Tests;

public class MockStream(Stream sourceStream, bool canRead, bool canWrite) : Stream
{
    public override bool CanRead => canRead;
    public override bool CanWrite => canWrite;
    public override bool CanSeek => sourceStream.CanSeek;

    public override void Flush()
    {
        sourceStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!canRead) throw new NotSupportedException();
        return sourceStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return sourceStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        if (!CanWrite) throw new NotSupportedException();
        sourceStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite) throw new NotSupportedException();
        sourceStream.Write(buffer, offset, count);
    }

    public override long Length => sourceStream.Length;

    public override long Position
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }
}