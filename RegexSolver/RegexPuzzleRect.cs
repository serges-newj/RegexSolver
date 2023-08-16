using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RegexSolver
{
    public class Pattern
    {
        public Regex Regex = null;
        private string text;

        public Pattern(string text)
        {
            this.Text = text;
        }

        public string Text
        {
            get => text;
            set
            {
                this.text = value;
                Regex = null;
                if (!String.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        new Regex(text, RegexOptions.ECMAScript);
                        string pattern = text;
                        //if (!pattern.StartsWith("^")) pattern = "^" + pattern;
                        //if (!pattern.EndsWith("$")) pattern = pattern + "$";
                        pattern = "^(?:" + pattern + ")$";
                        Regex = new Regex(pattern, RegexOptions.ECMAScript);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public bool IsError { get => !String.IsNullOrWhiteSpace(text) && Regex == null; }

        public override string ToString()
        {
            return text;
        }
    }

    public class Cell
    {
        public Line[] lines;
        public string text;
        public bool isTemp;

        public Cell(Line[] lines)
        {
            this.lines = lines;
        }
    }

    public class Line
    {
        public int axis;
        public int pos;
        public Pattern[] patterns;
        public Cell[] cells;

        public Line(int axis, int pos)
        {
            this.axis = axis;
            this.pos = pos;
            this.patterns = new Pattern[2];
        }

        internal void SetCells(Cell[] cells)
        {
            this.cells = cells;
        }
    }

    public class RegexPuzzle
    {
        public int dimension;
        public int[] size;
        public Line[][] lines;

        private void _initLines()
        {
            lines = new Line[dimension][];
            Dictionary<Line, List<Cell>> matrix = new Dictionary<Line, List<Cell>>();

            for (int axis = 0; axis < dimension; axis++)
            {
                lines[axis] = new Line[size[axis]];
                for (int pos = 0; pos < size[axis]; pos++)
                {
                    lines[axis][pos] = new Line(axis, pos);
                    matrix.Add(lines[axis][pos], new List<Cell>());
                }
            }

            Cell[] cells = null;
            if (dimension == 2)
                cells = _initCellsRect();
            else
                throw new NotImplementedException("Hex puzzles not supported yet");

            foreach (var c in cells)
                foreach (var l in c.lines)
                    matrix[l].Add(c);

            foreach (var lc in matrix)
                lc.Key.SetCells(lc.Value.ToArray());
        }

        private Cell[] _initCellsRect()
        {
            int total = size[0] * size[1];
            Cell[] cells = new Cell[total];
            for (int i = 0; i < total; i++)
            {
                Line line1 = lines[0][i / size[0]];
                Line line2 = lines[1][i / size[1]];
                cells[i] = new Cell(new Line[] { line1, line2 });
            }
            return cells;
        }

        public RegexPuzzle(int[] size)
        {
            this.size = size;
            this.dimension = size.Length;

            _initLines();
            ClearCells();
        }

        public static RegexPuzzle CreateFromJSON(PuzzleJSON puzzleJSON)
        {
            if (puzzleJSON.hexagonal)
                throw new NotImplementedException("Hex puzzles not supported yet");

            RegexPuzzle puzzle = new RegexPuzzle(new int[] { puzzleJSON.patternsX.GetUpperBound(0) + 1, puzzleJSON.patternsY.GetUpperBound(0) + 1 });

            for (int pos = 0; pos < puzzle.size[0]; pos++)
                for (int side = 0; side <= 1; side++)
                {
                    string r = puzzleJSON.patternsX[pos][side];
                    if (!String.IsNullOrEmpty(r))
                    {
                        puzzle.lines[0][pos].patterns[side] = new Pattern(r);
                    }
                }

            for (int pos = 0; pos < puzzle.size[0]; pos++)
                for (int side = 0; side <= 1; side++)
                {
                    string r = puzzleJSON.patternsY[pos][side];
                    if (!String.IsNullOrEmpty(r))
                    {
                        puzzle.lines[1][pos].patterns[side] = new Pattern(r);
                    }
                }

            return puzzle;
        }

        public static RegexPuzzle CreateFromFile(string fileName)
        {
            var input = File.OpenText(fileName);
            int[] size = input.ReadLine().Split().Select(s => int.Parse(s)).ToArray();

            RegexPuzzle puzzle = new RegexPuzzle(size);

            for (int axis = 0; axis < puzzle.dimension; axis++)
                for (int pos = 0; pos < size[axis]; pos++)
                    for (int side = 0; side <= 1; side++)
                    {
                        string r = input.ReadLine();
                        if (!String.IsNullOrEmpty(r))
                        {
                            puzzle.lines[axis][pos].patterns[side] = new Pattern(r);
                        }
                    }

            for (int row = 0; row < size[0]; row++)
            {
                string[] cellValues = input.ReadLine().Split((char)7);
                for (int pos = 0; pos < puzzle.lines[0][row].cells.Length; pos++)
                {
                    if (!String.IsNullOrEmpty(cellValues[pos]) && cellValues[pos][0] == 6)
                    {
                        puzzle.lines[0][row].cells[pos].isTemp = true;
                        cellValues[pos] = cellValues[pos].Substring(1);
                    }
                    puzzle.lines[0][row].cells[pos].text = cellValues[pos];
                }
            }

            return puzzle;
        }

        public void ClearCells()
        {
            foreach (var line in lines[0])
                foreach (var cell in line.cells)
                    cell.text = String.Empty;
        }

        public void Save(string fileName)
        {
            var output = File.CreateText(fileName);

            output.WriteLine(string.Join(" ", size));

            for (int axis = 0; axis < dimension; axis++)
                for (int pos = 0; pos < size[axis]; pos++)
                    for (int side = 0; side <= 1; side++)
                        output.WriteLine(lines[axis][pos].patterns[side]?.ToString());

            foreach (Line line in lines[0])
            {
                foreach (Cell cell in line.cells)
                {
                    if (cell.isTemp)
                        output.Write((char)6);
                    output.Write(cell.text);
                    output.Write((char)7);
                }
                output.WriteLine();
            }

            output.Flush();
            output.Close();
        }
    }

    public class RegexPuzzle2D : RegexPuzzle
    {
        public RegexPuzzle2D(int x, int y) : base(new int[2] { x, y })
        {
        }
    }

    public class RegexPuzzleRect
    {
        public int Rows, Cols;
        public Pattern[,] RPattern, CPattern;
        public string[,] Cells;
        public bool[,] IsTemp;

        private void _initArrays()
        {
            RPattern = new Pattern[Rows, 2];
            CPattern = new Pattern[Cols, 2];
            Cells = new string[Rows, Cols];
            IsTemp = new bool[Rows, Cols];
        }

        public RegexPuzzleRect(int rows, int columns)
        {
            this.Rows = rows;
            this.Cols = columns;

            _initArrays();
            ClearCells();
        }

        public RegexPuzzleRect(string fileName)
        {
            var input = File.OpenText(fileName);
            string[] inp = input.ReadLine().Split();
            Rows = int.Parse(inp[0]);
            Cols = int.Parse(inp[1]);
            _initArrays();

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j <= 1; j++)
                {
                    string r = input.ReadLine();
                    if (!String.IsNullOrEmpty(r))
                    {
                        RPattern[i, j] = new Pattern(r);
                    }
                }

            for (int i = 0; i < Cols; i++)
                for (int j = 0; j <= 1; j++)
                {
                    string r = input.ReadLine();
                    if (!String.IsNullOrEmpty(r))
                    {
                        CPattern[i, j] = new Pattern(r);
                    }
                }

            for (int i = 0; i < Rows; i++)
            {
                string[] v = input.ReadLine().Split((char)7);
                for (int j = 0; j < Cols; j++)
                {
                    if (!String.IsNullOrEmpty(v[j]) && v[j][0] == 6)
                    {
                        IsTemp[i, j] = true;
                        v[j] = v[j].Substring(1);
                    }
                    Cells[i, j] = v[j];
                }
            }
        }

        public RegexPuzzleRect(PuzzleJSON puzzleJSON)
        {
            Rows = puzzleJSON.patternsY.GetUpperBound(0) + 1;
            Cols = puzzleJSON.patternsX.GetUpperBound(0) + 1;
            _initArrays();
            ClearCells();

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j <= 1; j++)
                {
                    string r = puzzleJSON.patternsY[i][j];
                    if (!String.IsNullOrEmpty(r))
                    {
                        RPattern[i, j] = new Pattern(r);
                    }
                }

            for (int i = 0; i < Cols; i++)
                for (int j = 0; j <= 1; j++)
                {
                    string r = puzzleJSON.patternsX[i][j];
                    if (!String.IsNullOrEmpty(r))
                    {
                        CPattern[i, j] = new Pattern(r);
                    }
                }

        }

        public void ClearCells()
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    Cells[i, j] = String.Empty;
        }

        public void Save(string fileName)
        {
            var output = File.CreateText(fileName);

            output.WriteLine("{0} {1}", Rows, Cols);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j <= 1; j++)
                    output.WriteLine(RPattern[i, j]?.ToString());
            for (int i = 0; i < Cols; i++)
                for (int j = 0; j <= 1; j++)
                    output.WriteLine(CPattern[i, j]?.ToString());
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (IsTemp[i, j])
                        output.Write((char)6);
                    output.Write(Cells[i, j]);
                    output.Write((char)7);
                }
                output.WriteLine();
            }
            output.Flush();
            output.Close();
        }
    }
}
