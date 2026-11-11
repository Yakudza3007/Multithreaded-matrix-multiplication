using System.Globalization;

namespace MatrixLab;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--generate")
        {
            try
            {
                MatrixIpc.GenerateAndSend(args);
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка генерации: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }
        }

        while (true)
        {
            Console.WriteLine("1. Многопоточное умножение матриц");
            Console.WriteLine("2. Умножение с нижнетреугольной матрицей");
            Console.WriteLine("3. Возведение матрицы в степень");
            Console.WriteLine("4. Автоматические замеры производительности");
            Console.WriteLine("5. Замеры треугольной матрицы (стат/дин)");
            Console.WriteLine("6. Замеры IPC и умножения");
            Console.WriteLine("7. Замеры возведения в степень");
            Console.WriteLine("0. Выход");

            int choice = ReadInt("Выберите задание: ", 0, 7);
            try
            {
                switch (choice)
                {
                    case 1: RunOrdinaryMultiplication(); break;
                    case 2: RunLowerTriangularMultiplication(); break;
                    case 3: RunMatrixPower(); break;
                    case 4: Benchmark.Run(); break;
                    case 5: Benchmark.RunTriangular(); break;
                    case 6: Benchmark.RunIpcBenchmark(); break;
                    case 7: Benchmark.RunPower(); break;
                    case 0: return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    private static void RunOrdinaryMultiplication()
    {
        Console.WriteLine("\n=== Обычное умножение матриц ===");

        int rowsA = ReadInt("Количество строк A: ", 1);
        int columnsA = ReadInt("Количество столбцов A: ", 1);
        int columnsB = ReadInt("Количество столбцов B: ", 1);
        int threadCount = ReadInt("Количество потоков: ", 1);

        Console.WriteLine("\nГенерация матриц...");

        Matrix a = MatrixGenerator.Generate(rowsA, columnsA);
        Matrix b = MatrixGenerator.Generate(columnsA, columnsB);

        Console.WriteLine("Умножение матриц...");

        Matrix result = MatrixMultiplier.Multiply(
            a,
            b,
            threadCount,
            progressCallback: ShowProgress);

        Console.WriteLine($"Контрольная сумма результата: {result.Checksum()}");
    }

    private static void RunLowerTriangularMultiplication()
    {
        Console.WriteLine("\n=== Нижнетреугольная матрица ===");

        int size = ReadInt("Размер квадратной матрицы A: ", 1);
        int columnsB = ReadInt("Количество столбцов матрицы B: ", 1);
        int threadCount = ReadInt("Количество потоков: ", 1);

        Console.WriteLine("\nВыберите способ распределения строк:");
        Console.WriteLine("1. Статический");
        Console.WriteLine("2. Динамический");

        int schedulingChoice = ReadInt("Ваш выбор: ", 1, 2);

        bool dynamicScheduling = schedulingChoice == 2;

        Console.WriteLine("\nЗапуск отдельного процесса генерации матриц...");

        var (a, b) = MatrixIpc.GenerateMatricesInSeparateProcess(
            size,
            size,
            columnsB,
            lowerTriangularA: true);

        Console.WriteLine("Матрицы получены. Начинаем умножение...");

        Matrix result = MatrixMultiplier.Multiply(
            a,
            b,
            threadCount,
            dynamicScheduling,
            ShowProgress);

        Console.WriteLine($"Контрольная сумма результата: {result.Checksum()}");
    }

    private static void RunMatrixPower()
    {
        Console.WriteLine("\n=== Возведение матрицы в степень ===");

        int size = ReadInt("Размер квадратной матрицы: ", 1);
        int power = ReadInt("Степень матрицы: ", 1);
        int threadCount = ReadInt("Количество потоков: ", 1);

        Console.WriteLine("\nГенерация матрицы...");

        Matrix matrix = MatrixGenerator.Generate(size, size);

        Console.WriteLine("Возведение матрицы в степень...");

        Matrix result = MatrixPower.Calculate(
            matrix,
            power,
            threadCount,
            ShowProgress);

        Console.WriteLine($"Контрольная сумма результата: {result.Checksum()}");
    }

    private static int ReadInt(string message, int min, int max = int.MaxValue)
    {
        while (true)
        {
            Console.Write(message);

            string? input = Console.ReadLine();

            if (int.TryParse(input, out int value) &&
                value >= min &&
                value <= max)
            {
                return value;
            }

            Console.WriteLine($"Введите целое число от {min} до {max}.");
        }
    }

    private static void ShowProgress(int percentage)
    {
        Console.WriteLine($"Прогресс: {percentage}%");
    }
}