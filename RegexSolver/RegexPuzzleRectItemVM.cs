using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace RegexSolver
{
    internal class RegexPuzzleRectItemVM : ViewModelBase
    {
        protected int row;
        public int Row { get => row; }
        protected int col;
        public int Col { get => col; }
        protected RegexPuzzleRectVM puzzleVM;
        protected RegexPuzzleRect model { get => puzzleVM.Puzzle; }

        public RegexPuzzleRectItemVM(RegexPuzzleRectVM viewModel, int row, int col)
        {
            this.puzzleVM = viewModel;
            this.row = row;
            this.col = col;
            viewModel.AddItem(this);
        }
    }

    internal class RegexPuzzleRectPatternVM : RegexPuzzleRectItemVM
    {
        internal static class Properties
        {
            public const string Background = "Background";
            public const string Foreground = "Foreground";
        }

        public RegexPuzzleRectPatternVM(RegexPuzzleRectVM viewModel, int row, int col) : base(viewModel, row, col)
        {
        }

        private Pattern pattern
        {
            get
            {
                if (col == 0)
                    return model.RPattern[row - 1, 0];
                else if (col == model.Cols + 1)
                    return model.RPattern[row - 1, 1];
                else if (row == 0)
                    return model.CPattern[col - 1, 0];
                else if (row == model.Rows + 1)
                    return model.CPattern[col - 1, 1];
                return null;
            }
            set
            {
                if (col == 0)
                    model.RPattern[row - 1, 0] = value;
                else if (col == model.Cols + 1)
                    model.RPattern[row - 1, 1] = value;
                else if (row == 0)
                    model.CPattern[col - 1, 0] = value;
                else if (row == model.Rows + 1)
                    model.CPattern[col - 1, 1] = value;
            }
        }

        public void OnCellChanged(int row, int column)
        {
            if (row == this.row || column == this.col)
                OnPropertyChanged(Properties.Background);
        }

        public string Text
        {
            get => pattern?.ToString();
            set
            {
                if (pattern == null)
                    pattern = new Pattern(value);
                pattern.Text = value;
                OnPropertyChanged(Properties.Foreground);
                OnPropertyChanged(Properties.Background);
            }
        }

        private bool isDisabled = false;
        public bool IsDisabled
        {
            get => isDisabled;
            set
            {
                isDisabled = value;
                OnPropertyChanged(Properties.Background);
            }
        }

        public Brush Background
        {
            get
            {
                if (isDisabled)
                    return Brushes.LightGray;
                if (!String.IsNullOrEmpty(Text))
                {
                    switch (checkRegexMatch())
                    {
                        case 0: return Brushes.Transparent;
                        case 1: return Brushes.Pink;
                        case 2: return Brushes.LightGreen;
                        case 3: return Brushes.LimeGreen;
                    }

                }
                return Brushes.Transparent;
            }
        }

        public Brush Foreground
        {
            get
            {
                if (pattern?.IsError == true)
                    return Brushes.Red;
                return Brushes.DarkBlue;
            }
        }

        public Dock Side
        {
            get
            {
                if (col == 0)
                    return Dock.Left;
                else if (col == model.Cols + 1)
                    return Dock.Right;
                else if (row == 0)
                    return Dock.Top;
                else if (row == model.Rows + 1)
                    return Dock.Bottom;
                return Dock.Left;

            }
        }

        private int checkRegexMatch()
        {
            Regex r = pattern?.Regex;
            if (r == null)
                return 0;

            int res = 0;
            string[,] cells = null;
            bool empty = false;
            bool multi = false;
            if (col == 0 || col == model.Cols + 1)
            {
                cells = new string[model.Cols, 2];
                for (int i = 0; i < model.Cols; i++)
                {
                    cells[i, 0] = model.Cells[row - 1, i];
                    cells[i, 1] = String.Empty;
                    empty = empty || (String.IsNullOrEmpty(cells[i, 0]));
                    multi = multi || cells[i, 0].Length > 1;
                }
            }
            else if (row == 0 || row == model.Rows + 1)
            {
                cells = new string[model.Rows, 2];
                for (int i = 0; i < model.Rows; i++)
                {
                    cells[i, 0] = model.Cells[i, col - 1];
                    cells[i, 1] = String.Empty;
                    empty = empty || (String.IsNullOrEmpty(cells[i, 0]));
                    multi = multi || cells[i, 0].Length > 1;
                }
            }
            if (!empty)
            {
                if (findMatch(cells, 0, "", r))
                {
                    if (!multi)
                        res = 3;
                    else
                        res = 2;

                    for (int i = 0; i <= cells.GetUpperBound(0); i++)
                    {
                        string wrongChars = null;
                        if (cells[i, 0].Length > cells[i, 1].Length)
                            wrongChars = string.Join(string.Empty, cells[i, 0].Where(c => !cells[i, 1].Contains(c)));
                        if (col == 0 || col == model.Cols + 1)
                            puzzleVM.AddCellWrongChars(row, i + 1, Side, wrongChars);
                        else
                            puzzleVM.AddCellWrongChars(i + 1, col, Side, wrongChars);
                    }
                }
                else
                    res = 1;
            }
            return res;
        }

        private bool findMatch(string[,] cells, int idx, string line, Regex r)
        {
            if (idx > cells.GetUpperBound(0))
            {
                //var match = r.Match(line);
                //return match.Success && match.Length == line.Length;
                return r.IsMatch(line);
            }
            else
            {
                bool match = false;
                string chars = cells[idx, 0];
                foreach (char c in chars)
                {
                    if (findMatch(cells, idx + 1, line + c, r))
                    {
                        match = true;
                        if (!cells[idx, 1].Contains(c))
                            cells[idx, 1] = cells[idx, 1] + c;
                    }
                }
                return match;
            }
        }
    }

    internal class RegexPuzzleRectCellVM : RegexPuzzleRectItemVM
    {
        internal static class Properties
        {
            public const string VisibleText = "VisibleText";
            public const string FontSize = "FontSize";
            public const string Background = "Background";
            public const string BorderBrush = "BorderBrush";
            public const string WrongChars = "WrongChars";
        }

        public RegexPuzzleRectCellVM(RegexPuzzleRectVM viewModel, int row, int col) : base(viewModel, row, col)
        {
        }

        private string text
        {
            get { return model.Cells[row - 1, col - 1]; }
            set { model.Cells[row - 1, col - 1] = value; }
        }

        public string VisibleText
        {
            get
            {
                return text.Replace(" ", "\\s");
            }
            set
            {
                text = value.Replace("\\s", " ");
                OnPropertyChanged(Properties.VisibleText);
                OnPropertyChanged(Properties.FontSize);
                ClearWrongChars();
                puzzleVM.OnCellChanged(Row, Col);
            }
        }

        public int FontSize
        {
            get
            {
                int len = text == null ? 0 : text.Length;
                if (len <= 1) return 30;
                if (len <= 2) return 18;
                if (len <= 4) return 14;
                if (len <= 6) return 12;
                if (len <= 12) return 10;
                return 8;
            }
        }

        private string[] wrongChars = new string[5];
        public string WrongChars { get => wrongChars[0]?.Replace(" ", "\\s"); }

        public void ClearWrongChars()
        {
            Array.Clear(wrongChars, 0, 5);
            OnPropertyChanged(Properties.WrongChars);
            OnPropertyChanged(Properties.Background);
        }

        public void AddWrongChars(Dock side, string chars)
        {
            wrongChars[(int)side + 1] = chars;
            wrongChars[0] = string.Join(string.Empty, (wrongChars[1] + wrongChars[2] + wrongChars[3] + wrongChars[4]).Distinct());
            OnPropertyChanged(Properties.WrongChars);
            OnPropertyChanged(Properties.Background);
        }

        public void RemoveRepeatedChars()
        {
            if (string.Concat(text.Distinct()).Length < text.Length)
                VisibleText = string.Concat(text.Distinct());
        }

        public Brush Background
        {
            get
            {
                if (String.IsNullOrEmpty(wrongChars[0]))
                    return Brushes.Transparent;
                if (!IsTemp && wrongChars[0].Length == text.Length)
                    return Brushes.MistyRose;
                return Brushes.LightCyan;
            }
        }

        public Brush BorderBrush
        {
            get
            {
                return IsTemp ? Brushes.HotPink : Brushes.LightGray;
            }
        }

        public bool IsTemp
        {
            get { return model.IsTemp[row - 1, col - 1]; }
            set
            {
                if (value != IsTemp)
                {
                    model.IsTemp[row - 1, col - 1] = value;
                    OnPropertyChanged(Properties.BorderBrush);
                }
            }
        }
    }
}
