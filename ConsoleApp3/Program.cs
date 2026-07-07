using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Symbols;
using SevenZip;

namespace ConsoleApp3;

[InProcess]
[MemoryDiagnoser(displayGenColumns: false)]
[DisassemblyDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public partial class Program
{
    static void Main(string[] args)
    {

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        //const string sourceFile = @"c:\tmp\1.db";
        //const string archiveFile = @"c:\tmp\1.7z";
        //const string archiveFile1 = @"c:\tmp\2.7z";
        //const string extractedFile = @"c:\tmp\2.db";
        //Compress(sourceFile, archiveFile, CompressionMethod.Lzma);
        //Compress(sourceFile, archiveFile1, CompressionMethod.Lzma2);
        //Extract(archiveFile, extractedFile);
        //SevenZipBase.SetLibraryPath(@"c:\tmp\7z64.dll");
        Console.WriteLine("Hello, World!");
    }
    [Benchmark]
    public void BenchmarkLzma2()
    {
        Compress(@"c:\tmp\1.db", @"c:\tmp\1.7z");
    }
    [Benchmark]
    public void BenchmarkDefault()
    {
        Compress(@"c:\tmp\1.db", @"c:\tmp\1.7z", CompressionMethod.Lzma2,true);
    }
    public static void Extract(string inputFileName, string outputFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFileName);

        var inputPath = Path.GetFullPath(inputFileName);
        var outputPath = Path.GetFullPath(outputFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var inputStream = new FileStream(
            inputPath,
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            });

        using var outputStream = new FileStream(
            outputPath,
            new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Share = FileShare.None,
                Options = FileOptions.SequentialScan
            });

        var extractor = new SevenZipExtractor(inputStream);
        extractor.ExtractFile(0, outputStream);

        outputStream.Flush(flushToDisk: true);
    }

    public static void Compress(
        string inputFileName,
        string outputFileName,
        CompressionMethod compressionMethod = CompressionMethod.Default, bool useMultithreading = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFileName);

        var inputPath = Path.GetFullPath(inputFileName);
        var outputPath = Path.GetFullPath(outputFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var inputStream = new FileStream(
            inputPath,
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            });

        using var outputStream = new FileStream(
            outputPath,
            new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Share = FileShare.None,
                Options = FileOptions.SequentialScan
            });

        var compressor = new SevenZipCompressor
        {
            
            CompressionMethod = compressionMethod
        };
        if (useMultithreading)
        {
            compressor.CustomParameters.Add("mt", "on");
        }

        compressor.CompressStream(inputStream, outputStream);

        outputStream.Flush(flushToDisk: true);
    }
}