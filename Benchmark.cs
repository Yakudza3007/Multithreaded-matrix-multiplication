using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace MatrixLab;

internal static class Benchmark
{
    private static readonly int[] Sizes = { 500, 1000, 2000 };

    private const int Measurements = 5;

    public static void Run()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        int[] threadCounts = Enumerable
            .Range(1, 40)
            .Concat(new[] { 80, 160 })
            .ToArray();

        string rawFile = Path.Combine(
            Environment.CurrentDirectory,
            $"benchmark_raw_{timestamp}.csv");

        string summaryFile = Path.Combine(
            Environment.CurrentDirectory,
            $"benchmark_summary_{timestamp}.csv");

        Console.WriteLine("\n=== Автоматические замеры ===");
        Console.WriteLine("Прогрев JIT...");

        Matrix warmupA = MatrixGenerator.Generate(100, 100);
        Matrix warmupB = MatrixGenerator.Generate(100, 100);

        MatrixMultiplier.Multiply(
            warmupA,
            warmupB,
            1,
            printTime: false);

        using StreamWriter rawWriter = new StreamWriter(rawFile);
        using StreamWriter summaryWriter = new StreamWriter(summaryFile);

        rawWriter.WriteLine("Size,Threads,Run,TimeMs,Checksum");

        summaryWriter.WriteLine(
            "Size,Threads,MinMs,MaxMs,AverageTrimmedMs");

        foreach (int size in Sizes)
        {
            Console.WriteLine($"\nГенерация матриц {size} x {size}...");

            Matrix a = MatrixGenerator.Generate(size, size);
            Matrix b = MatrixGenerator.Generate(size, size);

            long? expectedChecksum = null;

            foreach (int threads in threadCounts)
            {
                double[] times = new double[Measurements];

                Console.WriteLine(
                    $"\nРазмер: {size} x {size}, потоков: {threads}");

                for (int run = 0; run < Measurements; run++)
                {
                    double elapsed = 0;

                    Matrix result = MatrixMultiplier.Multiply(
                        a,
                        b,
                        threads,
                        timingCallback: time => elapsed = time,
                        printTime: false);

                    long checksum = result.Checksum();

                    if (expectedChecksum == null)
                    {
                        expectedChecksum = checksum;
                    }
                    else if (checksum != expectedChecksum.Value)
                    {
                        throw new InvalidOperationException(
                            "Контрольные суммы результатов не совпадают.");
                    }

                    times[run] = elapsed;

                    rawWriter.WriteLine(
                        $"{size},{threads},{run + 1}," +
                        $"{elapsed.ToString("F2", CultureInfo.InvariantCulture)}," +
                        $"{checksum}");

                    rawWriter.Flush();

                    Console.WriteLine(
                        $"Замер {run + 1}/{Measurements}: {elapsed:F2} мс");
                }

                double[] sortedTimes = (double[])times.Clone();
                Array.Sort(sortedTimes);

                double average =
                    (sortedTimes[1] + sortedTimes[2] + sortedTimes[3]) / 3.0;

                summaryWriter.WriteLine(
                    $"{size},{threads}," +
                    $"{sortedTimes[0].ToString("F2", CultureInfo.InvariantCulture)}," +
                    $"{sortedTimes[4].ToString("F2", CultureInfo.InvariantCulture)}," +
                    $"{average.ToString("F2", CultureInfo.InvariantCulture)}");

                summaryWriter.Flush();

                Console.WriteLine(
                    $"Среднее после исключения min/max: {average:F2} мс");
            }
        }

        Console.WriteLine("\n=== Замеры завершены ===");
        Console.WriteLine($"Все результаты: {rawFile}");
        Console.WriteLine($"Средние результаты: {summaryFile}");
    }
    
    public static void RunTriangular()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        int[] threadCounts = Enumerable
            .Range(1, 40)
            .Concat(new[] { 80, 160 })
            .ToArray();

        int[] sizes = { 1000, 2000 };

        string rawFile = Path.Combine(
            Environment.CurrentDirectory,
            $"triangular_raw_{timestamp}.csv");

        string summaryFile = Path.Combine(
            Environment.CurrentDirectory,
            $"triangular_summary_{timestamp}.csv");

        Console.WriteLine("\n=== Замеры треугольной матрицы (стат/дин) ===");
        Console.WriteLine("Прогрев JIT...");

        Matrix warmA = MatrixGenerator.GenerateLowerTriangular(100);
        Matrix warmB = MatrixGenerator.Generate(100, 100);

        MatrixMultiplier.Multiply(warmA, warmB, 1, false, printTime: false);
        MatrixMultiplier.Multiply(warmA, warmB, 1, true,  printTime: false);

        using StreamWriter rawWriter = new StreamWriter(rawFile);
        using StreamWriter summaryWriter = new StreamWriter(summaryFile);

        rawWriter.WriteLine("Size,Threads,Scheduling,Run,TimeMs,Checksum");
        summaryWriter.WriteLine("Size,Threads,Scheduling,MinMs,MaxMs,AverageTrimmedMs");

        string[] schedulings = { "static", "dynamic" };

        foreach (int size in sizes)
        {
            Console.WriteLine($"\nГенерация матриц {size} x {size}...");

            Matrix a = MatrixGenerator.GenerateLowerTriangular(size);
            Matrix b = MatrixGenerator.Generate(size, size);

            long? expectedChecksum = null;

            foreach (string scheduling in schedulings)
            {
                bool dynamicScheduling = scheduling == "dynamic";

                Console.WriteLine($"\nРазмер: {size}, распределение: {scheduling}");

                foreach (int threads in threadCounts)
                {
                    double[] times = new double[Measurements];

                    Console.WriteLine($"  Потоков: {threads}");

                    for (int run = 0; run < Measurements; run++)
                    {
                        double elapsed = 0;

                        Matrix result = MatrixMultiplier.Multiply(
                            a,
                            b,
                            threads,
                            dynamicScheduling,
                            timingCallback: time => elapsed = time,
                            printTime: false);

                        long checksum = result.Checksum();

                        if (expectedChecksum == null)
                        {
                            expectedChecksum = checksum;
                        }
                        else if (checksum != expectedChecksum.Value)
                        {
                            throw new InvalidOperationException(
                                "Контрольные суммы стат/дин не совпадают.");
                        }

                        times[run] = elapsed;

                        rawWriter.WriteLine(
                            $"{size},{threads},{scheduling},{run + 1}," +
                            $"{elapsed.ToString("F2", CultureInfo.InvariantCulture)}," +
                            $"{checksum}");

                        rawWriter.Flush();
                    }

                    double[] sortedTimes = (double[])times.Clone();
                    Array.Sort(sortedTimes);

                    double average =
                        (sortedTimes[1] + sortedTimes[2] + sortedTimes[3]) / 3.0;

                    summaryWriter.WriteLine(
                        $"{size},{threads},{scheduling}," +
                        $"{sortedTimes[0].ToString("F2", CultureInfo.InvariantCulture)}," +
                        $"{sortedTimes[4].ToString("F2", CultureInfo.InvariantCulture)}," +
                        $"{average.ToString("F2", CultureInfo.InvariantCulture)}");

                    summaryWriter.Flush();

                    Console.WriteLine(
                        $"    Среднее: {average:F2} мс");
                }
            }
        }

        Console.WriteLine("\n=== Замеры завершены ===");
        Console.WriteLine($"Сырые результаты: {rawFile}");
        Console.WriteLine($"Средние результаты: {summaryFile}");
    }

    public static void RunIpcBenchmark()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string csvFile = Path.Combine(Environment.CurrentDirectory, $"ipc_benchmark_{timestamp}.csv");

        int[] sizes = { 500, 1000, 2000 };
        int threads = 16;

        Console.WriteLine("\n=== Замеры IPC (передача данных) и умножения ===");

        using StreamWriter writer = new StreamWriter(csvFile);
        writer.WriteLine("Size,IpcTimeMs,MultiplyTimeMs");
        writer.Flush();

        foreach (int size in sizes)
        {
            Console.WriteLine($"\nРазмер: {size} x {size}");
            
            Stopwatch swIpc = Stopwatch.StartNew();
            var (a, b) = MatrixIpc.GenerateMatricesInSeparateProcess(size, size, size);
            swIpc.Stop();
            double ipcTime = swIpc.Elapsed.TotalMilliseconds;
            
            Console.WriteLine($"  Время IPC (генерация + передача): {ipcTime:F2} мс");

            MatrixMultiplier.Multiply(a, b, 1, printTime: false);
            
            Stopwatch swMult = Stopwatch.StartNew();
            MatrixMultiplier.Multiply(a, b, threads, printTime: false);
            swMult.Stop();
            double multTime = swMult.Elapsed.TotalMilliseconds;
            
            Console.WriteLine($"  Время умножения ({threads} потоков): {multTime:F2} мс");

            writer.WriteLine($"{size}," +
                             $"{ipcTime.ToString("F2", CultureInfo.InvariantCulture)}," +
                             $"{multTime.ToString("F2", CultureInfo.InvariantCulture)}");
            writer.Flush();
        }
        
        Console.WriteLine($"\n=== Завершено! Данные сохранены в {csvFile} ===");
    }

    public static void RunPower()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        int[] threadCounts = Enumerable
            .Range(1, 40)
            .Concat(new[] { 80, 160 })
            .ToArray();

        int size = 1000;
        int power = 10;

        string rawFile = Path.Combine(Environment.CurrentDirectory, $"power_raw_{timestamp}.csv");
        string summaryFile = Path.Combine(Environment.CurrentDirectory, $"power_summary_{timestamp}.csv");

        Console.WriteLine("\n=== Замеры возведения матрицы в степень ===");
        Console.WriteLine("Прогрев JIT...");

        Matrix warmup = MatrixGenerator.Generate(100, 100);
        MatrixPower.Calculate(warmup, 2, 1);

        using StreamWriter rawWriter = new StreamWriter(rawFile);
        using StreamWriter summaryWriter = new StreamWriter(summaryFile);

        rawWriter.WriteLine("Size,Power,Threads,Run,TimeMs,Checksum");
        summaryWriter.WriteLine("Size,Power,Threads,MinMs,MaxMs,AverageTrimmedMs");

        Console.WriteLine($"\nГенерация матрицы {size} x {size}...");
        Matrix matrix = MatrixGenerator.Generate(size, size);

        long? expectedChecksum = null;

        foreach (int threads in threadCounts)
        {
            double[] times = new double[Measurements];
            Console.WriteLine($"\nПотоков: {threads}");

            for (int run = 0; run < Measurements; run++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                Matrix result = MatrixPower.Calculate(matrix, power, threads);
                sw.Stop();
                
                double elapsed = sw.Elapsed.TotalMilliseconds;
                long checksum = result.Checksum();

                if (expectedChecksum == null)
                {
                    expectedChecksum = checksum;
                }
                else if (checksum != expectedChecksum.Value)
                {
                    throw new InvalidOperationException("Контрольные суммы степени не совпадают.");
                }

                times[run] = elapsed;

                rawWriter.WriteLine($"{size},{power},{threads},{run + 1}," +
                                    $"{elapsed.ToString("F2", CultureInfo.InvariantCulture)},{checksum}");
                rawWriter.Flush();
                
                Console.WriteLine($"  Замер {run + 1}/{Measurements}: {elapsed:F2} мс");
            }

            double[] sortedTimes = (double[])times.Clone();
            Array.Sort(sortedTimes);

            double average = (sortedTimes[1] + sortedTimes[2] + sortedTimes[3]) / 3.0;

            summaryWriter.WriteLine($"{size},{power},{threads}," +
                                    $"{sortedTimes[0].ToString("F2", CultureInfo.InvariantCulture)}," +
                                    $"{sortedTimes[4].ToString("F2", CultureInfo.InvariantCulture)}," +
                                    $"{average.ToString("F2", CultureInfo.InvariantCulture)}");
            summaryWriter.Flush();

            Console.WriteLine($"  Среднее: {average:F2} мс");
        }

        Console.WriteLine("\n=== Замеры степени завершены ===");
        Console.WriteLine($"Сырые результаты: {rawFile}");
        Console.WriteLine($"Средние результаты: {summaryFile}");
    }
}