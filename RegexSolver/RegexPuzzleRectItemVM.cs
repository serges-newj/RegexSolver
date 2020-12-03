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
        internal enum RegexMatchResult
        {
            Unknown,
            NoRegex,
            Disabled,
            Processing,
            NoMatch,
            Partial,
            Full
        }

        internal static class Properties
        {
            public const string Background = "Background";
            public const string Foreground = "Foreground";
            public const string BorderBrush = "BorderBrush";
        }

        public RegexPuzzleRectPatternVM(RegexPuzzleRectVM viewModel, int row, int col) : base(viewModel, row, col)
        {
            updateBackground();
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
                updateBackground();
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
                updateBackground();
            }
        }

        private bool isDisabled = false;
        public bool IsDisabled
        {
            get => isDisabled;
            set
            {
                isDisabled = value;
                if(isDisabled)
                    getNewProcessingId(); // to break any current processing immediately
                updateBackground();
            }
        }

        private async void updateBackground(bool bAsync = true)
        {
            if (isDisabled)
                lastRegexMatchResult = RegexMatchResult.Disabled;
            else if (string.IsNullOrEmpty(Text))
                lastRegexMatchResult = RegexMatchResult.NoRegex;
            else
            {
                if (bAsync)
                {
                    int processingId = getNewProcessingId();
                    if (lastRegexMatchResult != RegexMatchResult.Processing)
                    {
                        lastRegexMatchResult = RegexMatchResult.Processing;
                        OnPropertyChanged(Properties.Background);
                    }

                    RegexMatchResult matchResult = await checkRegexMatchAsync(() => processingId != getLastProcessingId());

                    if (!IsDisabled && (processingId == getLastProcessingId()))
                        lastRegexMatchResult = matchResult;
                }
                else
                    lastRegexMatchResult = checkRegexMatch(() => false);
            }
            if (bAsync)
            {
                OnPropertyChanged(Properties.Background);
            }
        }

        private RegexMatchResult lastRegexMatchResult = RegexMatchResult.Unknown;
        public Brush Background
        {
            get
            {
                if (lastRegexMatchResult == RegexMatchResult.Unknown)
                    updateBackground(false);
                switch (lastRegexMatchResult)
                {
                    case RegexMatchResult.Disabled:
                        return Brushes.LightGray;
                    case RegexMatchResult.Processing:
                        return puzzleVM.BlinkBrush;
                    case RegexMatchResult.NoMatch:
                        return Brushes.Pink;
                    case RegexMatchResult.Partial:
                        return Brushes.LightGreen;
                    case RegexMatchResult.Full:
                        return Brushes.LimeGreen;
                    default:
                        return Brushes.Transparent;
                }
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

        private int processingCounter = 0;
        private int getLastProcessingId()
        {
            return processingCounter;
        }
        private int getNewProcessingId()
        {
            lock (this)
            {
                return ++processingCounter;
            }
        }

        public Brush BorderBrush
        {
            get
            {
                return Brushes.Transparent;
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

        private delegate bool breakCondition();
        private async Task<RegexMatchResult> checkRegexMatchAsync(breakCondition stop)
        {
            return await Task.Run(() => checkRegexMatch(stop));
        }

        private RegexMatchResult checkRegexMatch(breakCondition stop)
        {
            Regex r = pattern?.Regex;
            if (r == null)
                return RegexMatchResult.NoRegex;

            RegexMatchResult res = RegexMatchResult.Unknown;
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
                if (findMatch(cells, 0, "", r, stop) && !stop())
                {
                    if (!multi)
                        res = RegexMatchResult.Full;
                    else
                        res = RegexMatchResult.Partial;

                    for (int i = 0; i <= cells.GetUpperBound(0); i++)
                    {
                        string wrongChars = null;
                        if (cells[i, 0].Length > cells[i, 1].Length)
                            wrongChars = string.Join(string.Empty, cells[i, 0].Where(c => !cells[i, 1].Contains(c)));
                        if (col == 0 || col == model.Cols + 1)
                            puzzleVM.SetCellWrongChars(row, i + 1, Side, wrongChars);
                        else
                            puzzleVM.SetCellWrongChars(i + 1, col, Side, wrongChars);
                    }
                }
                else
                {
                    if (stop())
                        res = RegexMatchResult.Unknown;
                    else
                        res = RegexMatchResult.NoMatch;
                }
            }

            return res;
        }

        private bool findMatch(string[,] cells, int idx, string line, Regex r, breakCondition stop)
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
                    if (stop())
                        return false;
                    if (findMatch(cells, idx + 1, line + c, r, stop))
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

        public void SetWrongChars(Dock side, string chars)
        {
            wrongChars[(int)side + 1] = chars;
            wrongChars[0] = string.Concat( string.Join(string.Empty, wrongChars[1], wrongChars[2], wrongChars[3], wrongChars[4]).Distinct());
            
            if (string.IsNullOrEmpty(wrongChars[0]))
                wrongChars[0] = null;
            OnPropertyChanged(Properties.WrongChars);
            OnPropertyChanged(Properties.Background);
        }

        public void RemoveRepeatedChars()
        {
            if (string.Concat(text.Distinct()).Length < text.Length)
                VisibleText = string.Concat(text.Distinct());
        }

        public void RemoveWrongChars()
        {
            VisibleText = String.Join(string.Empty, text.Where(c => !WrongChars.Contains(c)));
        }

        public Brush Background
        {
            get
            {
                if (String.IsNullOrEmpty(wrongChars[0]))
                    return Brushes.Transparent;
                if (wrongChars[0].Length == text.Length)
                    return IsTemp ? Brushes.MistyRose : Brushes.LightPink;
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
