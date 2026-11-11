
namespace MatrixLab;

internal class Matrix
{
    private readonly long[][] _data;

    public int Rows { get; }
    public int Columns { get; }
    public bool IsLowerTriangular { get; }

    public Matrix(int rows, int columns, bool isLowerTriangular = false)
    {
        if (rows <= 0 || columns <= 0)
            throw new ArgumentException("Размеры матрицы должны быть положительными.");

        if (isLowerTriangular && rows != columns)
            throw new ArgumentException("Нижнетреугольная матрица должна быть квадратной.");

        Rows = rows;
        Columns = columns;
        IsLowerTriangular = isLowerTriangular;

        _data = new long[rows][];

        for (int i = 0; i < rows; i++)
        {
            int length = isLowerTriangular ? i + 1 : columns;
            _data[i] = new long[length];
        }
    }

    public long this[int row, int column]
    {
        get
        {
            if (row < 0 || row >= Rows || column < 0 || column >= Columns)
                throw new IndexOutOfRangeException();

            if (IsLowerTriangular && column > row)
                return 0;

            return _data[row][column];
        }
        set
        {
            if (row < 0 || row >= Rows || column < 0 || column >= Columns)
                throw new IndexOutOfRangeException();

            if (IsLowerTriangular && column > row)
            {
                if (value != 0)
                    throw new ArgumentException("Нельзя записать ненулевое значение выше главной диагонали.");

                return;
            }

            _data[row][column] = value;
        }
    }

    public int GetStoredRowLength(int row)
    {
        if (row < 0 || row >= Rows)
            throw new IndexOutOfRangeException();

        return _data[row].Length;
    }

    public long[] GetRow(int row)
    {
        if (row < 0 || row >= Rows)
            throw new IndexOutOfRangeException();

        return _data[row];
    }

    public long Checksum()
    {
        long sum = 0;

        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < GetStoredRowLength(i); j++)
            {
                sum += _data[i][j];
            }
        }

        return sum;
    }
}
