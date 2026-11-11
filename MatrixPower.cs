
using System.Diagnostics;
using System.Threading;

namespace MatrixLab;

internal static class MatrixPower
{
    private const long Mod = 1_000_000_007;

    public static Matrix Calculate(
        Matrix matrix,
        int power,
        int threadCount,
        Action<int>? progressCallback = null)
    {
        if (matrix.Rows != matrix.Columns)
            throw new ArgumentException("Матрица должна быть квадратной.");

        if (power < 1)
            throw new ArgumentException("Степень должна быть положительной.");

        if (threadCount < 1)
            throw new ArgumentException("Количество потоков должно быть положительным.");

        int size = matrix.Rows;

        Matrix current = CopyMatrix(matrix);
        Matrix next = new Matrix(size, size);

        if (power == 1)
        {
            progressCallback?.Invoke(100);
            return current;
        }

        long completedRows = 0;
        long totalRows = (long)(power - 1) * size;

        int lastReportedProgress = 0;
        object progressLock = new object();

        using Barrier barrier = new Barrier(threadCount, _ =>
        {
            Matrix temp = current;
            current = next;
            next = temp;
        });

        void Worker(int workerId)
        {
            int start = workerId * size / threadCount;
            int end = (workerId + 1) * size / threadCount;

            for (int step = 1; step < power; step++)
            {
                for (int i = start; i < end; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        long sum = 0;

                        for (int t = 0; t < size; t++)
                        {
                            long left = current[i, t] % Mod;
                            long right = matrix[t, j] % Mod;

                            sum = (sum + left * right) % Mod;
                        }

                        next[i, j] = sum;
                    }

                    lock (progressLock)
                    {
                        completedRows++;

                        int progress = (int)(completedRows * 100.0 / totalRows);

                        if (progress > lastReportedProgress)
                        {
                            lastReportedProgress = progress;
                            progressCallback?.Invoke(progress);
                        }
                    }
                }

                barrier.SignalAndWait();
            }
        }

        Thread[] threads = new Thread[threadCount];

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < threadCount; i++)
        {
            int workerId = i;
            threads[i] = new Thread(() => Worker(workerId));
            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        stopwatch.Stop();

        Console.WriteLine(
            $"Время возведения в степень: {stopwatch.Elapsed.TotalMilliseconds:F2} мс");

        return current;
    }

    private static Matrix CopyMatrix(Matrix source)
    {
        Matrix copy = new Matrix(source.Rows, source.Columns);

        for (int i = 0; i < source.Rows; i++)
        {
            for (int j = 0; j < source.Columns; j++)
            {
                copy[i, j] = source[i, j];
            }
        }

        return copy;
    }
}
