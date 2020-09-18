using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RegexSolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RegexPuzzleRectVM vm = new RegexPuzzleRectVM();

        public MainWindow()
        {
            InitializeComponent();

            Blink.Fill = new SolidColorBrush(Colors.OrangeRed);
            Blink.Fill.BeginAnimation(SolidColorBrush.ColorProperty, this.Resources["BlinkBrush"] as ColorAnimation);
            vm.BlinkBrush = Blink.Fill;

            DataContext = vm;
            vm.PropertyChanged += vm_PropertyChanged;
            Crossword.Children.Clear();
            vm.IsModified = false;
        }

        private void vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == RegexPuzzleRectVM.Properties.Puzzle)
            {
                generateCrossword();
            }
        }

        private void OnNew_Click(object sender, RoutedEventArgs e)
        {
            vm.New();
        }

        private void OnImport_Click(object sender, RoutedEventArgs e)
        {
            vm.Import();
        }

        private void OnOpen_Click(object sender, RoutedEventArgs e)
        {
            vm.Open();
        }

        private void OnSave_Click(object sender, RoutedEventArgs e)
        {
            vm.Save(sender == btnSaveAs);
        }

        private void OnClearTemp_Click(object sender, RoutedEventArgs e)
        {
            vm.ClearTemp();
        }

        private void OnCleanWrong_Click(object sender, RoutedEventArgs e)
        {
            vm.CleanWrong();
        }

        private void generateCrossword()
        {
            Crossword.Children.Clear();
            Crossword.RowDefinitions.Clear();
            Crossword.ColumnDefinitions.Clear();

            ContentPresenter cp = null;

            Crossword.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            for (int row = 1; row <= vm.Puzzle.Rows; row++)
            {
                Crossword.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });

                cp = new ContentPresenter() { Content = new RegexPuzzleRectPatternVM(vm, row, 0) };
                cp.SetValue(Grid.RowProperty, row);
                cp.SetValue(Grid.ColumnProperty, 0);
                Crossword.Children.Add(cp);

                cp = new ContentPresenter() { Content = new RegexPuzzleRectPatternVM(vm, row, vm.Puzzle.Cols + 1) };
                cp.SetValue(Grid.RowProperty, row);
                cp.SetValue(Grid.ColumnProperty, vm.Puzzle.Cols + 1);
                Crossword.Children.Add(cp);
            }
            Crossword.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            Crossword.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            for (int col = 1; col <= vm.Puzzle.Cols; col++)
            {
                Crossword.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });

                cp = new ContentPresenter() { Content = new RegexPuzzleRectPatternVM(vm, 0, col) };
                cp.SetValue(Grid.RowProperty, 0);
                cp.SetValue(Grid.ColumnProperty, col);
                Crossword.Children.Add(cp);

                cp = new ContentPresenter() { Content = new RegexPuzzleRectPatternVM(vm, vm.Puzzle.Rows + 1, col) };
                cp.SetValue(Grid.RowProperty, vm.Puzzle.Rows + 1);
                cp.SetValue(Grid.ColumnProperty, col);
                Crossword.Children.Add(cp);
            }
            Crossword.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            for (int row = 1; row <= vm.Puzzle.Rows; row++)
                for (int col = 1; col <= vm.Puzzle.Cols; col++)
                {
                    cp = new ContentPresenter() { Content = new RegexPuzzleRectCellVM(vm, row, col) };
                    cp.SetValue(Grid.RowProperty, row);
                    cp.SetValue(Grid.ColumnProperty, col);
                    
                    Crossword.Children.Add(cp);
                }
        }

        private void Crossword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.OriginalSource is TextBox)
            {
                TextBox tb = e.OriginalSource as TextBox;
                if (tb.IsKeyboardFocused && tb.IsKeyboardFocusWithin)
                {
                    vm.IsModified = true;
                }
            }
        }

        private void Cell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.DataContext is RegexPuzzleRectCellVM)
            {
                RegexPuzzleRectCellVM cellVM = tb.DataContext as RegexPuzzleRectCellVM;
                cellVM.IsTemp = !cellVM.IsTemp;
            }
        }

        private void Cell_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.DataContext is RegexPuzzleRectCellVM)
            {
                RegexPuzzleRectCellVM cellVM = tb.DataContext as RegexPuzzleRectCellVM;
                cellVM.RemoveRepeatedChars();
            }
        }

        private void Pattern_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.DataContext is RegexPuzzleRectPatternVM)
            {
                RegexPuzzleRectPatternVM patternVM = tb.DataContext as RegexPuzzleRectPatternVM;
                patternVM.IsDisabled = !patternVM.IsDisabled;
            }
        }
    }
}
