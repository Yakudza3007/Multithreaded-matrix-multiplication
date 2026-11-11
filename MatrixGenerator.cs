namespace MatrixLab;

internal static class MatrixGenerator
{
    private static readonly Random Random = new Random();

    public static Matrix Generate(int rows, int columns)
    {
        Matrix matrix = new Matrix(rows, columns);

        Fill(matrix);

        return matrix;
    }

    public static Matrix GenerateLowerTriangular(int size)
    {
        Matrix matrix = new Matrix(size, size, true);

        Fill(matrix);

        return matrix;
    }

    private static void Fill(Matrix matrix)
    {
        for (int i = 0; i < matrix.Rows; i++)
        {
            for (int j = 0; j < matrix.GetStoredRowLength(i); j++)
            {
                matrix[i, j] = Random.Next(0, 10);
            }
        }
    }
}
