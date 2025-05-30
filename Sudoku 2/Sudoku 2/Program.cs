class Program
{
    public static void Main()
    {
        string path = "your file path"; // vul hier het gewenste filepath in
        bool mostConstrained = false; // false = normale backtracking algoritme, true = backtracking met most Constrained variabele eerst

        List<Sudoku> sudokus = SudokuReader.Read(path); // lees bestand in
        Solver solver = new Solver();
        solver.LoadSudokus(sudokus); // laad de sudokus
        List<Sudoku> sudokusSolved = solver.SolveSudokus(mostConstrained); // genereer lijst met opgeloste sudokus

        foreach (Sudoku s in sudokusSolved) // print alle opgeloste sudokus uit
            s.Display();
    }
}

class SudokuReader
{
    public static List<Sudoku> Read(string file) // Reads all the sudokus from a given file 
    {
        List<Sudoku> sudokus = new List<Sudoku>();
        StreamReader SR = new StreamReader(file); // Streamreader om de sudokus in te lezen.

        while (SR.ReadLine() != null)
        {
            string line = SR.ReadLine().TrimStart();
            string[] numbers = line.Split();
            Sudoku sudoku = new Sudoku();
            int index = 0;

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int value = int.Parse(numbers[index]);
                    sudoku.InitialiseDomain(i, j, value); // Give sudoku object a domain
                    index++;
                }
            }
            sudokus.Add(sudoku);
        }
        return sudokus;
    }
}

class Solver
{
    private List<Sudoku> SudokusToBeSolved = new List<Sudoku>(); // list that holds all the sudokus to be solved
    public readonly (int, int)[,][] Constraints = new (int, int)[9, 9][]; // array that holds all the non universal constraints 

    public Solver()
    {
        LoadConstraints();
    }

    private void LoadConstraints()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++) // for each square in the sudoku
            {
                Constraints[i, j] = new (int, int)[20]; // initialise constraint list for that square
                (int, int)[] blockConstraints = LoadBlock(i, j);
                (int, int)[] rowConstraints = LoadRow(i, j);
                (int, int)[] colConstraints = LoadColumn(i, j); // load all constraints
                int count = 0;

                for (int k = 0; k < 24; k++) // combine the row, column and block so there are no doubles
                {
                    if (k < 8)
                    {
                        Constraints[i, j][count] = blockConstraints[k];
                        count++;
                    }
                    else if (k < 16)
                    {
                        if (!blockConstraints.Contains(rowConstraints[k - 8])) // check of de vakjes uit de rij of kolom niet al in het blok staan
                        {
                            Constraints[i, j][count] = rowConstraints[k - 8];
                            count++;
                        }
                    }
                    else
                    {
                        if (!blockConstraints.Contains(colConstraints[k - 16])) // check of de vakjes uit de rij of kolom niet al in het blok staan
                        {
                            Constraints[i, j][count] = colConstraints[k - 16];
                            count++;
                        }
                    }
                }
            }
        }
    }

    private static (int, int)[] LoadRow(int row, int col)
    {
        (int, int)[] rowConstraints = new (int, int)[8];
        int count = 0;

        for (int j = 0; j < 9; j++)
        {
            if ((row, j) != (row, col))
            {
                rowConstraints[count] = (row, j);
                count++;
            }
        }
        return rowConstraints;
    }

    private static (int, int)[] LoadColumn(int row, int col)
    {
        (int, int)[] colConstraints = new (int, int)[8];
        int count = 0;

        for (int i = 0; i < 9; i++)
        {
            if ((i, col) != (row, col))
            {
                colConstraints[count] = (i, col);
                count++;
            }
        }
        return colConstraints;
    }

    private static (int, int)[] LoadBlock(int row, int col)
    {
        (int, int)[] blockConstraints = new (int, int)[8];
        int block = row / 3 * 3 + col / 3;
        int blockRow = block - block % 3;
        int blockCol = block % 3 * 3;
        int count = 0;

        for (int i = blockRow; i < blockRow + 3; i++)
        {
            for (int j = blockCol; j < blockCol + 3; j++)
            {
                if ((i, j) != (row, col))
                {
                    blockConstraints[count] = (i, j);
                    count++;
                }
            }
        }
        return blockConstraints;
    }

    public void LoadSudokus(List<Sudoku> sudokus)
    {
        SudokusToBeSolved = sudokus;
    }

    public List<Sudoku> SolveSudokus(bool mostConstrained = false)
    {
        List<Sudoku> SudokusSolved = new List<Sudoku>();

        foreach (Sudoku sudoku in SudokusToBeSolved)
        {
            MakeNodeConsistent(sudoku);
            if (mostConstrained)
            {
                BackTracking1(sudoku);
            }
            else
            {
                BackTracking(sudoku);
            }
            SudokusSolved.Add(sudoku);
        }
        return SudokusSolved;
    }

    private void MakeNodeConsistent(Sudoku sudoku)
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++) // for each square in the sudoku
            {
                if (sudoku.GetValue(i, j) != 0) // if the square is set to a value 
                {
                    foreach ((int, int) square in Constraints[i, j]) // get al the squares it has a non universal constraint with
                    {
                        int row = square.Item1;
                        int col = square.Item2;
                        int value = sudoku.GetValue(i, j);

                        sudoku.RemoveFromDomain(row, col, value); // remove the value of the square from the domains of all the other squares
                    }
                }
            }
        }
    }

    private void BackTracking(Sudoku sudoku) // backtracking met forward check
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++) // for each square in the sudoku
            {
                if (sudoku.GetValue(i, j) == 0) // if the square is not set 
                {
                    foreach (int k in sudoku.GetValuesFromDomain(i, j)) // loop trough each value in its domain
                    {
                        if (ForwardCheck(sudoku, i, j, k)) // if forward checking doesn't make a domain empty 
                        {
                            sudoku.SetValue(i, j, k); // set the value 
                            BackTracking(sudoku); // continue backtracking
                            if (sudoku.Solved) // if backtracking returns, there are two options either the sudoku is solved or there is no option anymore for the square
                            {
                                return; // if it is solved return 
                            }
                            sudoku.RemoveValue(i, j); // else remove the value 
                            BackTrackDomains(sudoku, i, j, k); // and put the value back in the domains it constrains 
                        }
                    }
                    return;
                }
            }
        }
        return;
    }

    private void BackTracking1(Sudoku sudoku) // back tracking met forward check. Most constrained first.
    {
        (int, int) MostConstrained = GetMostConstrainedVar(sudoku);
        int i = MostConstrained.Item1;
        int j = MostConstrained.Item2;

        if (MostConstrained.Item1 != -1)
        {
            foreach (int k in sudoku.GetValuesFromDomain(i, j)) // loop trough each value in its domain
            {
                if (ForwardCheck(sudoku, i, j, k)) // if forward checking doesn't make a domain empty 
                {
                    sudoku.SetValue(i, j, k); // set the value 
                    BackTracking1(sudoku); // continue backtracking
                    if (sudoku.Solved) // if backtracking returns there are two options either the sudoku is solved or there is no option anymore for the square
                    {
                        return; // if it is solved return 
                    }
                    sudoku.RemoveValue(i, j); // else remove the value 
                    BackTrackDomains(sudoku, i, j, k); // and put the value back in the domains it constrains 
                }
            }
        }
        return;
    }

    private static (int, int) GetMostConstrainedVar(Sudoku sudoku)
    {
        (int, int) mostConstrained = (-1, -1);

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++) // for each square in the sudoku
            {
                if (sudoku.GetValue(i, j) == 0) // if value is not yet set
                {
                    if (mostConstrained == (-1, -1)) // get first value that is not yet set to be the initial most constrained
                    {
                        mostConstrained = (i, j);
                    }
                    else
                    {
                        int mostConstrainedCount = sudoku.GetDomainCount(mostConstrained.Item1, mostConstrained.Item2);
                        int currCount = sudoku.GetDomainCount(i, j);

                        if (mostConstrainedCount == 1) // if count is 1 we can stop
                        {
                            return mostConstrained;
                        }
                        if (currCount < mostConstrainedCount) //get the variable with least domain length as most constrained var
                        {
                            mostConstrained = (i, j);
                        }
                    }
                }
            }
        }
        return mostConstrained;
    }

    private void BackTrackDomains(Sudoku sudoku, int row, int col, int val)
    {
        foreach ((int, int) square in Constraints[row, col])
        {
            int r = square.Item1;
            int c = square.Item2;
            sudoku.InsertInDomain(r, c, val);
        }
    }

    private bool ForwardCheck(Sudoku sudoku, int row, int col, int val)
    {
        bool emptydomain = false;

        foreach ((int, int) square in Constraints[row, col])
        {
            int r = square.Item1;
            int c = square.Item2;

            sudoku.RemoveFromDomain(r, c, val);

            if (sudoku.GetDomainCount(r, c) == 0)
            {
                emptydomain = true;
            }
        }

        if (emptydomain)
        {
            foreach ((int, int) square in Constraints[row, col])
            {
                int r = square.Item1;
                int c = square.Item2;
                sudoku.InsertInDomain(r, c, val);
            }
        }

        return !emptydomain;
    }
}

class Sudoku
{
    private readonly Domain[,] grid = new Domain[9, 9];
    private int emptySquares = 81;
    public bool Solved { get { return emptySquares == 0; } }

    public int GetValue(int row, int col) 
    {
        return grid[row, col].value;
    }

    public int GetDomainCount(int row, int col) 
    {
        return grid[row, col].count;
    }

    public void InsertInDomain(int row, int col, int val)
    {
        if (grid[row, col].domain[val - 1] == 0)
        {
            grid[row, col].count++;
        }
        grid[row, col].domain[val - 1] += 1;
    }

    public void RemoveFromDomain(int row, int col, int val)
    {
        if (grid[row, col].domain[val - 1] == 1)
        {
            grid[row, col].count--;
        }
        grid[row, col].domain[val - 1] -= 1;
    }

    public void SetValue(int row, int col, int val)
    {
        grid[row, col].value = val;
        emptySquares--;
    }

    public void RemoveValue(int row, int col)
    {
        grid[row, col].value = 0;
        emptySquares++;
    }

    public List<int> GetValuesFromDomain(int row, int col)
    {
        List<int> values = new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (grid[row, col].domain[i] == 1)
            {
                values.Add(i + 1);
            }
        }
        return values;
    }

    public void InitialiseDomain(int row, int col, int val) // initialise a domain to one number 1-9 or all numbers when given a 0
    {
        grid[row, col] = new Domain(val);
        if (val != 0)
        {
            emptySquares--;
        }
    }

    public void Display() // Prints the grid array in a clear way
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                Console.Write(grid[i, j].ToString() + " " + "  ");
                if (j == 2 || j == 5)
                {
                    Console.Write("|  ");
                }
            }
            Console.WriteLine();
            if (i == 2 || i == 5)
            {
                for (int k = 0; k < 14; k++)
                    Console.Write("-" + "  ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("\n");
    }

    class Domain
    {
        public int[] domain = new int[9];
        public int count;
        public int value;

        public Domain(int initialiseval) // initialises a domain to one or all numbers
        {
            if (initialiseval == 0)
            {
                for (int i = 1; i < 10; i++)
                {
                    domain[i - 1] = 1;
                }
                count = 9;
                value = 0;
            }
            else
            {
                domain[initialiseval - 1] = 1;
                count = 1;
                value = initialiseval;
            }
        }

        public override string ToString()
        {
            if (value == 0)
            {
                return String.Format(".{0}", count);
            }

            return value.ToString();
        }
    }
}