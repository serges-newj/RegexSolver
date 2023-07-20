using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            NoValue,
            NoMatch,
            Partial,
            Full
        }

        internal struct RegexMatchProgress
        {
            public RegexMatchResult result;
            public double ratio;
        }

        const int MIN_COMBINATION_TO_SHOW_PROGRESS = 1000;
        const double MAX_PARTITION_TO_MOVE_PROGRESS = 0.0005;

        internal static class Properties
        {
            public const string Background = "Background";
            public const string Foreground = "Foreground";
            public const string BorderBrush = "BorderBrush";
            public const string InProcessing = "InProcessing";
            public const string Progress = "Progress";
        }

        internal class ProgressReporter
        {
            IProgress<RegexMatchProgress> progress;
            int idxForProgressChange;
            double minProgressChange;
            double currentProgress;

            public ProgressReporter(string[,] cells, IProgress<RegexMatchProgress> progress)
            {
                this.progress = progress;

                idxForProgressChange = 0;
                minProgressChange = 1;
                currentProgress = 0;
                for (int idx = 0; idx <= cells.GetUpperBound(0) && (minProgressChange / cells[idx, 0].Length > MAX_PARTITION_TO_MOVE_PROGRESS); idx++)
                {
                    minProgressChange /= cells[idx, 0].Length;
                    idxForProgressChange = idx;
                }
            }

            public void SetProgress(int idx)
            {
                if (idx == idxForProgressChange)
                    currentProgress += minProgressChange;
                progress.Report( new RegexMatchProgress { result = RegexMatchResult.Processing, ratio = currentProgress } );
            }
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

        double old_progress = 1;
        public double Progress {
            get => old_progress;
        }

        public bool InProcessing { get { return old_progress < 1; } }
        public void UITimer_Tick(object sender, EventArgs e)
        {
            OnPropertyChanged(Properties.Progress);
        }

        private CancellationTokenSource cancellationTokenSource;
        private void updateMatchResult(IProgress<RegexMatchProgress> progress)
        {
            if (isDisabled)
                progress.Report(new RegexMatchProgress() { result = RegexMatchResult.Disabled, progress = 1 });
            else if (string.IsNullOrEmpty(Text))
                progress.Report(new RegexMatchProgress() { result = RegexMatchResult.NoRegex, progress = 1 });
            else
            {
                if (cancellationTokenSource != null)
                {
                    // cancel previous run
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }

                progress.Report(new RegexMatchProgress() { result = RegexMatchResult.Processing, progress = 0 });

                RegexMatchResult matchResult = await checkRegexMatch(processingId);

                if (!IsDisabled && (processingId == getLastProcessingId()))
                {
                    lastRegexMatchResult = matchResult;
                }
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
                    updateBackground();
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
        //private async Task<RegexMatchResult> checkRegexMatchAsync(breakCondition stop)
        //{
        //    return await Task.Run(() => checkRegexMatch(stop));
        //}

        private async RegexMatchResult checkRegexMatch(int asyncProcessingId)
        {
            Regex r = pattern?.Regex;
            if (r == null)
                return RegexMatchResult.NoRegex;

            RegexMatchResult res = RegexMatchResult.Unknown;
            string[,] cells = null;
            long combination_cnt = 1;
            if (col == 0 || col == model.Cols + 1)
            {
                cells = new string[model.Cols, 2];
                for (int i = 0; i < model.Cols; i++)
                {
                    if (String.IsNullOrEmpty(model.Cells[row - 1, i]))
                    {
                        combination_cnt = 0;
                        break;
                    }
                    else
                    {
                        cells[i, 0] = model.Cells[row - 1, i];
                        cells[i, 1] = String.Empty;
                        if (combination_cnt < MIN_COMBINATION_TO_SHOW_PROGRESS) combination_cnt *= cells[i, 0].Length;
                    }
                }
            }
            else if (row == 0 || row == model.Rows + 1)
            {
                cells = new string[model.Rows, 2];
                for (int i = 0; i < model.Rows; i++)
                {
                    if (String.IsNullOrEmpty(model.Cells[i, col - 1]))
                    {
                        combination_cnt = 0;
                        break;
                    }
                    else
                    {
                        cells[i, 0] = model.Cells[i, col - 1];
                        cells[i, 1] = String.Empty;
                        if (combination_cnt < MIN_COMBINATION_TO_SHOW_PROGRESS) combination_cnt *= cells[i, 0].Length;
                    }
                }
            }

            if (combination_cnt == 0)
                res = RegexMatchResult.NoValue;
            else
            {
                bool match = false;
                bool ignore = false;
                if (asyncProcessingId == 0 || combination_cnt < MIN_COMBINATION_TO_SHOW_PROGRESS)
                {
                    match = findMatch(cells, 0, "", r, () => false, 0);
                }
                else
                {   // асинхронная проверка
                    OnPropertyChanged(Properties.InProcessing);
                    // устанавливаем индикатор прогресса в 0 и запускаем его периодическое обновление
                    old_progress = 0;
                    EventHandler on_ui_timer = (sender, e) => OnPropertyChanged(Properties.Progress);
                    puzzleVM.ActivateTimer(on_ui_timer);

                    match = await findMatchAsync(cells, 0, "", r, () => asyncProcessingId != getLastProcessingId(), 1);

                    puzzleVM.DeactivateTimer(on_ui_timer);
                    if (asyncProcessingId != getLastProcessingId())
                        ignore = true;
                    else if (!IsDisabled)
                        old_progress = 1;
                }

                if (ignore)
                    res = RegexMatchResult.Unknown;
                else if (!match)
                    res = RegexMatchResult.NoMatch;
                else
                {
                    if (combination_cnt == 1)
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
            }

            return res;
        }

        private async Task<bool> findMatchAsync(string[,] cells, int idx, string line, Regex r, breakCondition stop, double partition)
        {
            return await Task.Run(() => findMatch(cells, idx, line, r, stop, partition));
        }

        /// <summary>
        /// Recursive search of match
        /// </summary>
        /// <param name="cells">Two-dimensional array:
        ///   IN:  cells[idx, 0] - characters to be tested on idx position
        ///   OUT: cells[idx, 1] - characters that could appear on idx position
        /// </param>
        /// <param name="idx">Current character position (also depth of recursion)</param>
        /// <param name="line">Current line being constructed for testing</param>
        /// <param name="r">Regex</param>
        /// <param name="progressReporter">Internal progress reporter class</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        private bool findMatchRecursive(string[,] cells, int idx, string line, Regex r, ProgressReporter progressReporter, CancellationToken cancellationToken)
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
                int len = chars.Length;
                foreach (char c in chars)
                {
                    if (findMatchRecursive(cells, idx + 1, line + c, r, progressReporter, cancellationToken))
                    {
                        match = true;
                        if (!cells[idx, 1].Contains(c))
                            cells[idx, 1] = cells[idx, 1] + c;
                    }
                    if (cancellationToken.IsCancellationRequested)
                        return false;
                    progressReporter.SetProgress(idx);
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
            VisibleText = String.Join(string.Empty, text.Where(c => !wrongChars[0].Contains(c)));
        }

        public void PasteUnwrapped()
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                string text = Clipboard.GetText(TextDataFormat.Text);
                Regex regex = new Regex(@"\\U([0-9A-F]{4})", RegexOptions.IgnoreCase);
                text = regex.Replace(text, match => ((char)int.Parse(match.Groups[1].Value, NumberStyles.HexNumber)).ToString());
                text = text.Replace("\\d", "0123456789");
                int pos = 0;
                StringBuilder sb = new StringBuilder();
                while (pos < text.Length)
                {
                    if (text[pos] == '-' && pos > 0 && text[pos - 1] != '\\' && pos + 1 < text.Length)
                    {
                        char c = (char)((int)text[pos - 1] + 1);
                        while (c < text[pos + 1])
                        {
                            if (!(c >= 'a' && c <= 'z'))
                                sb.Append(c);
                            c++;
                        }
                        pos++;
                    }
                    sb.Append(text[pos]);
                    pos++;

                }
                VisibleText = sb.ToString();
            }
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
