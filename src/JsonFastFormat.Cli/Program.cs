using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

var options = CliOptions.Parse(args);
if (options.ShowHelp)
{
    CliOptions.PrintHelp();
    return options.Error is null ? 0 : 2;
}

if (options.Error is not null)
{
    Console.Error.WriteLine(options.Error);
    Console.Error.WriteLine();
    CliOptions.PrintHelp();
    return 2;
}

var inputPath = Path.GetFullPath(options.InputPath!);
var outputPath = options.ResolveOutputPath(inputPath);
var tempPath = options.InPlace
    ? Path.Combine(Path.GetDirectoryName(inputPath)!, $".{Path.GetFileName(inputPath)}.{Guid.NewGuid():N}.tmp")
    : outputPath;

var stopwatch = Stopwatch.StartNew();

try
{
    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

    await using var input = new FileStream(
        inputPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: options.BufferSize,
        FileOptions.SequentialScan);

    await using var output = new FileStream(
        tempPath,
        FileMode.Create,
        FileAccess.Write,
        FileShare.None,
        bufferSize: options.BufferSize,
        FileOptions.SequentialScan);

    var writerOptions = new JsonWriterOptions
    {
        Indented = !options.Compact,
        IndentCharacter = ' ',
        IndentSize = options.IndentSize,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        SkipValidation = false
    };

    await FormatAsync(input, output, writerOptions, options.BufferSize);

    if (options.InPlace)
    {
        File.Copy(tempPath, inputPath, overwrite: true);
        File.Delete(tempPath);
        outputPath = inputPath;
    }

    stopwatch.Stop();
    var inputBytes = new FileInfo(inputPath).Length;
    var outputBytes = new FileInfo(outputPath).Length;
    Console.WriteLine($"OK: {outputPath}");
    Console.WriteLine($"Input:  {FormatBytes(inputBytes)}");
    Console.WriteLine($"Output: {FormatBytes(outputBytes)}");
    Console.WriteLine($"Time:   {stopwatch.Elapsed.TotalSeconds:N2}s");
    return 0;
}
catch
{
    if (options.InPlace && File.Exists(tempPath))
    {
        File.Delete(tempPath);
    }

    throw;
}

static async Task FormatAsync(Stream input, Stream output, JsonWriterOptions writerOptions, int initialBufferSize)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(initialBufferSize);
    var readerState = new JsonReaderState(new JsonReaderOptions
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 0
    });

    await using var writer = new Utf8JsonWriter(output, writerOptions);
    int bufferedBytes = 0;
    bool isFirstRead = true;

    try
    {
        while (true)
        {
            if (bufferedBytes == buffer.Length)
            {
                buffer = GrowBuffer(buffer, bufferedBytes);
            }

            int bytesRead = await input.ReadAsync(buffer.AsMemory(bufferedBytes, buffer.Length - bufferedBytes));
            int totalBytes = bufferedBytes + bytesRead;
            bool isFinalBlock = bytesRead == 0;
            int inputOffset = 0;

            if (isFirstRead)
            {
                isFirstRead = false;
                if (totalBytes >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                {
                    inputOffset = 3;
                }
            }

            var reader = new Utf8JsonReader(buffer.AsSpan(inputOffset, totalBytes - inputOffset), isFinalBlock, readerState);
            while (reader.Read())
            {
                WriteCurrentToken(ref reader, writer);
            }

            readerState = reader.CurrentState;
            int consumedBytes = checked((int)reader.BytesConsumed) + inputOffset;
            bufferedBytes = totalBytes - consumedBytes;

            if (bufferedBytes > 0)
            {
                Buffer.BlockCopy(buffer, consumedBytes, buffer, 0, bufferedBytes);
            }

            if (isFinalBlock)
            {
                break;
            }
        }

        await writer.FlushAsync();
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

static byte[] GrowBuffer(byte[] oldBuffer, int bytesToKeep)
{
    int newSize = oldBuffer.Length * 2;
    if (newSize < 0)
    {
        throw new InvalidOperationException("A single JSON token is too large to buffer.");
    }

    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
    Buffer.BlockCopy(oldBuffer, 0, newBuffer, 0, bytesToKeep);
    ArrayPool<byte>.Shared.Return(oldBuffer);
    return newBuffer;
}

static void WriteCurrentToken(ref Utf8JsonReader reader, Utf8JsonWriter writer)
{
    switch (reader.TokenType)
    {
        case JsonTokenType.StartObject:
            writer.WriteStartObject();
            break;
        case JsonTokenType.EndObject:
            writer.WriteEndObject();
            break;
        case JsonTokenType.StartArray:
            writer.WriteStartArray();
            break;
        case JsonTokenType.EndArray:
            writer.WriteEndArray();
            break;
        case JsonTokenType.PropertyName:
            writer.WritePropertyName(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
            break;
        case JsonTokenType.String:
            writer.WriteStringValue(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
            break;
        case JsonTokenType.Number:
            writer.WriteRawValue(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan, skipInputValidation: false);
            break;
        case JsonTokenType.True:
            writer.WriteBooleanValue(true);
            break;
        case JsonTokenType.False:
            writer.WriteBooleanValue(false);
            break;
        case JsonTokenType.Null:
            writer.WriteNullValue();
            break;
        case JsonTokenType.Comment:
        case JsonTokenType.None:
        default:
            throw new JsonException($"Unsupported JSON token: {reader.TokenType}");
    }
}

static string FormatBytes(long bytes)
{
    string[] units = ["B", "KB", "MB", "GB", "TB"];
    double value = bytes;
    int unit = 0;
    while (value >= 1024 && unit < units.Length - 1)
    {
        value /= 1024;
        unit++;
    }

    return $"{value:N2} {units[unit]}";
}

sealed class CliOptions
{
    public string? InputPath { get; private init; }
    public string? OutputPath { get; private init; }
    public bool InPlace { get; private init; }
    public bool Compact { get; private init; }
    public int IndentSize { get; private init; } = 2;
    public int BufferSize { get; private init; } = 1024 * 1024;
    public bool ShowHelp { get; private init; }
    public string? Error { get; private init; }

    public string ResolveOutputPath(string inputPath)
    {
        if (InPlace)
        {
            return inputPath;
        }

        if (!string.IsNullOrWhiteSpace(OutputPath))
        {
            return Path.GetFullPath(OutputPath);
        }

        var directory = Path.GetDirectoryName(inputPath)!;
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        return Path.Combine(directory, $"{name}.formatted{extension}");
    }

    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            return new CliOptions { ShowHelp = true };
        }

        var paths = new List<string>(capacity: 2);
        bool inPlace = false;
        bool compact = false;
        int indentSize = 2;
        int bufferSize = 1024 * 1024;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--in-place":
                case "-i":
                    inPlace = true;
                    break;
                case "--compact":
                    compact = true;
                    break;
                case "--indent":
                    if (!TryReadInt(args, ref i, "--indent", out indentSize, out var indentError) || indentSize is < 0 or > 127)
                    {
                        return new CliOptions { ShowHelp = true, Error = indentError ?? "--indent must be between 0 and 127." };
                    }
                    break;
                case "--buffer-mb":
                    if (!TryReadInt(args, ref i, "--buffer-mb", out var bufferMb, out var bufferError) || bufferMb is < 1 or > 1024)
                    {
                        return new CliOptions { ShowHelp = true, Error = bufferError ?? "--buffer-mb must be between 1 and 1024." };
                    }
                    bufferSize = bufferMb * 1024 * 1024;
                    break;
                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        return new CliOptions { ShowHelp = true, Error = $"Unknown argument: {arg}" };
                    }

                    paths.Add(arg);
                    break;
            }
        }

        if (paths.Count is < 1 or > 2)
        {
            return new CliOptions { ShowHelp = true, Error = "Provide an input JSON file and optionally an output file." };
        }

        if (inPlace && paths.Count == 2)
        {
            return new CliOptions { ShowHelp = true, Error = "--in-place cannot be used with an output file." };
        }

        return new CliOptions
        {
            InputPath = paths[0],
            OutputPath = paths.Count == 2 ? paths[1] : null,
            InPlace = inPlace,
            Compact = compact,
            IndentSize = indentSize,
            BufferSize = bufferSize
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
        jsonfmt - local streaming JSON formatter

        Usage:
          jsonfmt <input.json> [output.json]
          jsonfmt <input.json> --in-place

        Options:
          --in-place, -i       overwrite the source file through a temporary file
          --indent <n>         indentation spaces, default 2
          --compact            compact JSON by removing whitespace
          --buffer-mb <n>      initial buffer size in MB, default 1
          --help, -h           show help

        Examples:
          jsonfmt big.json
          jsonfmt big.json pretty.json --indent 4
          jsonfmt big.json --in-place
          jsonfmt big.json compact.json --compact
        """);
    }

    static bool TryReadInt(string[] args, ref int index, string name, out int value, out string? error)
    {
        value = 0;
        error = null;

        if (index + 1 >= args.Length)
        {
            error = $"{name} needs a number.";
            return false;
        }

        index++;
        if (!int.TryParse(args[index], NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            error = $"{name} is not a valid number: {args[index]}";
            return false;
        }

        return true;
    }
}
