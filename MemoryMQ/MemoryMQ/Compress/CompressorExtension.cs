using System.Text;
using EasyCompressor;

namespace MemoryMQ.Compress;

public static class CompressorExtension
{
    public static string Compress(this ICompressor compressor, string str)
    {
        var bytes           = Encoding.UTF8.GetBytes(str);
        var compressedBytes = compressor.Compress(bytes);

        return Convert.ToBase64String(compressedBytes);
    }

    public static string Decompress(this ICompressor compressor, string str)
    {
        var bytes           = Convert.FromBase64String(str);
        var compressedBytes = compressor.Decompress(bytes);
        var convertedString = Encoding.UTF8.GetString(compressedBytes);

        return convertedString;
    }
}