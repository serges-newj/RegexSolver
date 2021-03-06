﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;

namespace RegexSolver
{
    public class RegexPuzzleRectVM : ViewModelBase
    {
        internal static class Properties
        {
            public const string Puzzle = "Puzzle";
            public const string PuzzleName = "PuzzleName";
        }

        public bool IsModified = false;

        private RegexPuzzleRect puzzle = null;
        public RegexPuzzleRect Puzzle { get => puzzle; }
        private List<RegexPuzzleRectItemVM> items = new List<RegexPuzzleRectItemVM>();
        private string filePath;
        private string puzzleName;

        public int NewRows { get; set; } = 1;
        public int NewColumns { get; set; } = 1;
        public string RCWID { get; set; } = String.Empty;
        public string PuzzleName => puzzleName;

        internal void AddItem(RegexPuzzleRectItemVM item)
        {
            lock (items)
            {
                items.Add(item);
            }
        }

        public void New()
        {
            if (ensureSaveChanged())
            {
                items.Clear();
                puzzle = new RegexPuzzleRect(NewRows, NewColumns);
                puzzleName = string.Empty;
                OnPropertyChanged(Properties.Puzzle);
                OnPropertyChanged(Properties.PuzzleName);
                IsModified = false;
            }
        }


        public Brush BlinkBrush { get; set; }

        public void Import()
        {
            if (ensureSaveChanged())
            {
                try
                {
                    string json = null;
                    using (var webClient = new System.Net.WebClient())
                    {
                        webClient.Headers.Add("USER-AGENT", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36");
                        string url = "https://regexcrossword.com/api/puzzles/" + RCWID;
                        json = webClient.DownloadString(url);
                    }
                    if (!string.IsNullOrEmpty(json))
                    {
                        PuzzleJSON puzzleJSON = JsonConvert.DeserializeObject<PuzzleJSON>(json);
                        if (puzzleJSON.hexagonal)
                            MessageBox.Show("RegexSolver doesn't support hexagonal puzzles (yet)");
                        else
                        {
                            items.Clear();
                            puzzle = new RegexPuzzleRect(puzzleJSON);
                            puzzleName = puzzleJSON.name;
                            filePath = null;
                            OnPropertyChanged(Properties.Puzzle);
                            OnPropertyChanged(Properties.PuzzleName);
                            IsModified = true;

                            if (puzzleJSON.characters != null)
                            {
                                puzzle.Cells[0,0] = new string(puzzleJSON.characters);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        public void Open()
        {
            if (ensureSaveChanged())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "RegEx Crossword (*.rxc)|*.rxc|Все файлы (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == true)
                {
                    items.Clear();
                    puzzle = new RegexPuzzleRect(openFileDialog.FileName);
                    filePath = openFileDialog.FileName;
                    puzzleName = Path.GetFileNameWithoutExtension(filePath);
                    OnPropertyChanged(Properties.Puzzle);
                    OnPropertyChanged(Properties.PuzzleName);
                    IsModified = false;
                }
            }
        }

        public bool Save(bool saveDialog)
        {
            if (puzzle != null)
            {
                string saveTo = String.Empty;
                if (saveDialog || string.IsNullOrEmpty(filePath))
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "RegEx Crossword (*.rxc)|*.rxc|Все файлы (*.*)|*.*";
                    saveFileDialog.FilterIndex = 1;
                    if (!string.IsNullOrEmpty(filePath))
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(filePath);
                    else
                        saveFileDialog.FileName = puzzleName;

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        saveTo = saveFileDialog.FileName;
                    }
                }
                else
                    saveTo = filePath;

                if (!string.IsNullOrEmpty(saveTo))
                {
                    puzzle.Save(saveTo);
                    filePath = saveTo;
                    puzzleName = Path.GetFileNameWithoutExtension(filePath);
                    OnPropertyChanged(Properties.PuzzleName);
                    IsModified = false;
                    return true;
                }
                return false;
            }
            return true;
        }

        private bool ensureSaveChanged()
        {
            if (!IsModified)
                return true;
            var res = MessageBox.Show("Save changes?", "Regex Solver", MessageBoxButton.YesNoCancel);
            if (res == MessageBoxResult.Yes)
                return Save(false);
            return res == MessageBoxResult.No;
        }

        public void OnCellChanged(int row, int column)
        {
            foreach (var item in items.OfType<RegexPuzzleRectPatternVM>())
                item.OnCellChanged(row, column);
        }

        public void SetCellWrongChars(int row, int column, Dock side, string wrongChars)
        {
            lock (items)
            {
                items.OfType<RegexPuzzleRectCellVM>().SingleOrDefault(c => c.Row == row && c.Col == column)?.SetWrongChars(side, wrongChars);
            }
        }

        public void ClearAll()
        {
            foreach (var cell in items.OfType<RegexPuzzleRectCellVM>())
            {
                cell.IsTemp = false;
                cell.VisibleText = String.Empty;
            }
        }

        public void ClearTemp()
        {
            foreach (var cell in items.OfType<RegexPuzzleRectCellVM>().Where(c => c.IsTemp))
            {
                cell.IsTemp = false;
                cell.VisibleText = String.Empty;
            }
        }

        public void CleanWrong()
        {
            bool cleaned;
            do
            {
                cleaned = false;
                foreach (var cell in items.OfType<RegexPuzzleRectCellVM>().Where(cell => !String.IsNullOrEmpty(cell.WrongChars)))
                {
                    cell.RemoveWrongChars();
                    cleaned = true;
                }
            } while (cleaned);
        }

        public void FillAll(string text)
        {
            text = text.Replace("\\s", " ");
            if (string.Concat(text.Distinct()).Length < text.Length)
                text = string.Concat(text.Distinct());
            foreach (var cell in items.OfType<RegexPuzzleRectCellVM>().Where(cell => String.IsNullOrEmpty(cell.VisibleText)))
            {
                cell.VisibleText = text;
            }
        }
    }
}
