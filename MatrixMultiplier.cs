
using System.Diagnostics;

namespace MatrixLab;

internal static class MatrixMultiplier
{
    public static Matrix Multiply(
        Matrix a,
        Matrix b,
        int threadCount,
        bool dynamicScheduling = false,
        Action<int>? progressCallback = null,
        Action<double>? timingCallback = null,
        bool printTime = true)
    {
        if (a.Columns != b.Rows)
            throw new ArgumentException("Количество столбцов A должно совпадать с количеством строк B.");

        if (threadCount < 1)
            throw new ArgumentException("Количество потоков должно быть положительным.");

        int rows = a.Rows;
        int columns = b.Columns;

        Matrix result = new Matrix(rows, columns);

        int completedRows = 0;
        int nextRow = 0;

        object progressLock = new object();
        object rowLock = new object();

        int lastReportedProgress = 0;

        void CalculateRow(int i)
        {
            int limit = a.IsLowerTriangular
                ? a.GetStoredRowLength(i)
                : a.Columns;

            for (int t = 0; t < limit; t++)
            {
                long left = a[i, t];

                for (int j = 0; j < columns; j++)
                {
                    result[i, j] += left * b[t, j];
                }
            }

            lock (progressLock)
            {
                completedRows++;

                int progress = completedRows * 100 / rows;

                if (progress > lastReportedProgress)
                {
                    lastReportedProgress = progress;
                    progressCallback?.Invoke(progress);
                }
            }
        }

        void Worker(int workerId)
        {
            if (dynamicScheduling)
            {
                while (true)
                {
                    int row;

                    lock (rowLock)
                    {
                        if (nextRow >= rows)
                            break;

                        row = nextRow;
                        nextRow++;
                    }

                    CalculateRow(row);
                }
            }
            else
            {
                int start = workerId * rows / threadCount;
                int end = (workerId + 1) * rows / threadCount;

                for (int i = start; i < end; i++)
                {
                    CalculateRow(i);
                }
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

        double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

        timingCallback?.Invoke(elapsedMilliseconds);

        if (printTime)
        {
            Console.WriteLine($"Время умножения: {elapsedMilliseconds:F2} мс");
        }

        return result;
    }
}
