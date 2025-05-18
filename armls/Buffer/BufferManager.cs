using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Armls.Buffer;

// Class to manage all buffers of a project
public class BufferManager
{
    private readonly IDictionary<string, Buffer> buffers;

    public BufferManager()
    {
        buffers = new ConcurrentDictionary<string, Buffer>();
    }

    public void Add(DocumentUri uri, Buffer buf)
    {
        Add(uri.GetFileSystemPath(), buf);
    }

    public void Add(string path, Buffer buf)
    {
        buffers[path] = buf;
    }

    public IReadOnlyDictionary<string, Buffer> GetBuffers()
    {
        return buffers.AsReadOnly();
    }
}
