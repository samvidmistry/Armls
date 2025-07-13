using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Armls.Buffer;

/// <summary>
/// Class responsible to manage all <see cref="Buffer" /> of the
/// project. This class is required to have the latest text of all
/// files in the project at any time.
/// </summary>
public class BufferManager
{
    private readonly IDictionary<string, Buffer> buffers;

    public BufferManager()
    {
        buffers = new ConcurrentDictionary<string, Buffer>();
    }

    /// <summary>
    /// Add a <see cref="Buffer" /> to manager. If the buffer already
    /// exists, its text will be overwritten by the one provided in
    /// this call.
    /// </summary>
    public void Add(DocumentUri uri, Buffer buf)
    {
        Add(uri.GetFileSystemPath(), buf);
    }

    /// <summary>
    /// Add a <see cref="Buffer" /> to manager. If the buffer already
    /// exists, its text will be overwritten by the one provided in
    /// this call.
    /// </summary>
    public void Add(string path, Buffer buf)
    {
        buffers[path] = buf;
    }

    /// <summary>
    /// Get all <see cref=Buffer /> of the project as a read only
    /// dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, Buffer> GetBuffers()
    {
        return buffers.AsReadOnly();
    }

    /// <summary>
    /// Get a list of buffers that are yet to be analyzed for errors.
    /// </summary>
    public IReadOnlyDictionary<string, Buffer> GetNotAnalyzedBuffers()
    {
        return buffers
            .Where(b => b.Value.RequiresAnalysis)
            .ToDictionary(b => b.Key, b => b.Value)
            .AsReadOnly();
    }

    /// <summary>
    /// Mark all buffers as analyzed.
    /// </summary>
    public void MarkAnalyzed()
    {
        foreach (var b in buffers.Values)
        {
            b.RequiresAnalysis = false;
        }
    }

    /// <summary>
    /// Get a <see cref="Buffer" /> pointed to by the `uri`. If the
    /// buffer is not available with the manager (which shouldn't
    /// happen unless the buffer is not a part of the project), it
    /// will return `null`.
    /// </summary>
    public Buffer? GetBuffer(DocumentUri uri)
    {
        return GetBuffer(uri.GetFileSystemPath());
    }

    /// <summary>
    /// Get a <see cref="Buffer" /> pointed to by the `uri`. If the
    /// buffer is not available with the manager (which shouldn't
    /// happen unless the buffer is not a part of the project), it
    /// will return `null`.
    /// </summary>
    public Buffer? GetBuffer(string path)
    {
        if (buffers.ContainsKey(path))
        {
            return buffers[path];
        }

        return null;
    }
}
