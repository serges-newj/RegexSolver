using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

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
            Rows = puzzleJSON.patternsY.GetUpperBound(0)+1;
            Cols = puzzleJSON.patternsX.GetUpperBound(0)+1;
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
                    if(IsTemp[i, j])
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
