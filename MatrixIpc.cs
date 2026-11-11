using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;

namespace MatrixLab;

internal static class MatrixIpc
{
    public static (Matrix A, Matrix B) GenerateMatricesInSeparateProcess(
        int rowsA,
        int columnsA,
        int columnsB,
        bool lowerTriangularA = false)
    {
        string pipeName = "MatrixLab_" + Guid.NewGuid().ToString("N");

        using NamedPipeServerStream pipeServer =
            new NamedPipeServerStream(pipeName, PipeDirection.In);
            
        using var bufferedServer = new BufferedStream(pipeServer);

        string executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Не удалось определить путь к программе.");

        ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false
        };

        if (Path.GetFileNameWithoutExtension(executablePath)
            .Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            string assemblyPath = Assembly.GetEntryAssembly()?.Location
                ?? throw new InvalidOperationException("Не удалось определить путь к сборке.");

            startInfo.ArgumentList.Add(assemblyPath);
        }

        startInfo.ArgumentList.Add("--generate");
        startInfo.ArgumentList.Add(pipeName);
        startInfo.ArgumentList.Add(rowsA.ToString());
        startInfo.ArgumentList.Add(columnsA.ToString());
        startInfo.ArgumentList.Add(columnsB.ToString());
        startInfo.ArgumentList.Add(lowerTriangularA.ToString());

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить процесс генерации.");

        pipeServer.WaitForConnection();

        using BinaryReader reader = new BinaryReader(bufferedServer);

        Matrix a = ReadMatrix(reader);
        Matrix b = ReadMatrix(reader);

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException("Процесс генерации завершился с ошибкой.");

        return (a, b);
    }

    public static void GenerateAndSend(string[] args)
    {
        if (args.Length != 6)
            throw new ArgumentException("Неверные аргументы процесса генерации.");

        string pipeName = args[1];
        int rowsA = int.Parse(args[2]);
        int columnsA = int.Parse(args[3]);
        int columnsB = int.Parse(args[4]);
        bool lowerTriangularA = bool.Parse(args[5]);

        Matrix a = lowerTriangularA
            ? MatrixGenerator.GenerateLowerTriangular(rowsA)
            : MatrixGenerator.Generate(rowsA, columnsA);

        Matrix b = MatrixGenerator.Generate(columnsA, columnsB);

        using NamedPipeClientStream pipeClient =
            new NamedPipeClientStream(".", pipeName, PipeDirection.Out);

        pipeClient.Connect();

        using var bufferedClient = new BufferedStream(pipeClient);
        using BinaryWriter writer = new BinaryWriter(bufferedClient);

        WriteMatrix(writer, a);
        WriteMatrix(writer, b);
        
        writer.Flush();
        bufferedClient.Flush();
    }

    private static void WriteMatrix(BinaryWriter writer, Matrix matrix)
    {
        writer.Write(matrix.Rows);
        writer.Write(matrix.Columns);
        writer.Write(matrix.IsLowerTriangular);

        for (int i = 0; i < matrix.Rows; i++)
        {
            for (int j = 0; j < matrix.GetStoredRowLength(i); j++)
            {
                writer.Write(matrix[i, j]);
            }
        }
    }

    private static Matrix ReadMatrix(BinaryReader reader)
    {
        int rows = reader.ReadInt32();
        int columns = reader.ReadInt32();
        bool isLowerTriangular = reader.ReadBoolean();

        Matrix matrix = new Matrix(rows, columns, isLowerTriangular);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < matrix.GetStoredRowLength(i); j++)
            {
                matrix[i, j] = reader.ReadInt64();
            }
        }

        return matrix;
    }
}