﻿using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Painter
{
    enum ToolType
    {
        Brush,
        Point,
        Line,
        EditLine
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Point currentPoint = new System.Windows.Point();
        Line? straightLine = null;
        ToolType selectedTool;

        System.Windows.Media.Effects.Effect dropShadow;
        bool mouseUp = true;

        public MainWindow()
        {
            InitializeComponent();
            this.Width = SystemParameters.PrimaryScreenWidth / 1.45;
            this.Height = SystemParameters.PrimaryScreenHeight / 1.45;
            paintSurface.Width = 1200;
            paintSurface.Height = 600;
            selectedTool = ToolType.Brush;
            brushButton.IsChecked = true;

            // Makes menu items align to right
            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            Action setAlignmentValue = () =>
            {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };
            setAlignmentValue();
            SystemParameters.StaticPropertyChanged += (sender, e) => { setAlignmentValue(); };

            // Save canvas dropshadow for time when we need to remove it during saving to png
            dropShadow = paintSurface.Effect;
        }

        private void resetTools()
        {
            if (selectedTool == ToolType.Line)
            {
                straightLine = null;
                paintSurface.Cursor = Cursors.Cross;
            }
        }

        private void ReattachCanvasEventHandlers(Canvas canvas)
        {
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
        }

        #region Canvas events

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                currentPoint = e.GetPosition(paintSurface);
                mouseUp = false;
            }
            switch (selectedTool) {
                case ToolType.Point: 
                    Ellipse ellipse = new Ellipse();
                    ellipse.Fill = new SolidColorBrush(Colors.Black);
                    ellipse.Width = 20;
                    ellipse.Height = 20;
                    ellipse.Margin = new Thickness(currentPoint.X, currentPoint.Y, 0, 0);
                    paintSurface.Children.Add(ellipse);
                    break;
                case ToolType.Line:
                    if (straightLine is null)
                    {
                        straightLine = new Line();
                        straightLine.Stroke = new SolidColorBrush(Colors.Black);
                        straightLine.StrokeThickness = 4;
                        straightLine.X1 = currentPoint.X;
                        straightLine.Y1 = currentPoint.Y;
                        paintSurface.Cursor = Cursors.Arrow;
                    }
                    else
                    {
                        straightLine.X2 = currentPoint.X;
                        straightLine.Y2 = currentPoint.Y;
                        paintSurface.Children.Add(straightLine);
                        straightLine = null;
                        paintSurface.Cursor = Cursors.Cross;
                    }
                    break;
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            switch(selectedTool) {
                case ToolType.Brush:
                    if (!mouseUp && e.LeftButton == MouseButtonState.Pressed)
                    {
                        Line line = new Line();
                        line.Stroke = new SolidColorBrush(Colors.Black);
                        line.StrokeThickness = 4;
                        line.X1 = currentPoint.X;
                        line.Y1 = currentPoint.Y;
                        line.X2 = e.GetPosition(paintSurface).X;
                        line.Y2 = e.GetPosition(paintSurface).Y;
                        currentPoint = e.GetPosition(paintSurface);
                        paintSurface.Children.Add(line);
                    }
                    break;
            }
        }
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            mouseUp = true;
        }

        #endregion

        #region Toolbar events

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button != null)
            {
                // uncheck all buttons in toolbar
                foreach (var item in toolbar.Items)
                {
                    if (item is ToggleButton)
                    {
                        var toggleButton = item as ToggleButton;
                        toggleButton!.IsChecked = false;
                    }
                }

                switch (button.Name)
                {
                    case "brushButton":
                        selectedTool = ToolType.Brush;
                        paintSurface.Cursor = Cursors.Pen;
                        break;
                    case "pointButton":
                        selectedTool = ToolType.Point;
                        paintSurface.Cursor = Cursors.Cross;
                        break;
                    case "lineButton":
                        selectedTool = ToolType.Line;
                        paintSurface.Cursor = Cursors.Cross;
                        break;
                    case "editLineButton":
                        selectedTool = ToolType.EditLine;
                        paintSurface.Cursor = Cursors.Arrow;
                        break;
                }
                button.IsChecked = true;
                resetTools();
            }
       }

        #endregion

        #region Menu events
        private void SaveCanvas_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.Filter = "Plik painter (*pnt)|*.pnt";
                dialog.RestoreDirectory = true;
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    // Save file
                    using (var fileStream = File.Create(dialog.FileName))
                    {
                        XamlWriter.Save(paintSurface, fileStream);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Wystąpił błąd podczas zapisywania pliku", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadToCanvas_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Filter = "Plik painter (*pnt)|*.pnt";
                dialog.RestoreDirectory = true;
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    Canvas savedCanvas;
                    using (var fileStream = File.OpenRead(dialog.FileName))
                    {
                        savedCanvas = (Canvas)XamlReader.Load(fileStream);
                    }
                    var parent = paintSurface.Parent as ScrollViewer;
                    if (parent != null)
                    {
                        parent.Content = savedCanvas;
                        paintSurface = savedCanvas;
                        ReattachCanvasEventHandlers(paintSurface);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Wystąpił błąd podczas odczytywania pliku", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCanvasToImg_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.Filter = "Plik PNG (*.png)|*.png|Plik JPG (*.jpg)|*.jpg|Plik GIF (*.gif)|*.gif|Plik BMP (*.bmp)|*.bmp|Plik TIFF (*.tiff)|*.tiff";
                dialog.RestoreDirectory = true;
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    // Prepare canvas for saving
                    paintSurface.Effect = null;
                    paintSurface.Measure(new Size((int)paintSurface.ActualWidth, (int)paintSurface.ActualHeight));
                    paintSurface.Arrange(new Rect(new Size((int)paintSurface.ActualWidth, (int)paintSurface.ActualHeight)));

                    // Create bitmap from canvas
                    RenderTargetBitmap rtb = new RenderTargetBitmap((int)paintSurface.ActualWidth, (int)paintSurface.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    VisualBrush sourceBrush = new VisualBrush(paintSurface);
                    DrawingVisual drawingVisual = new DrawingVisual();
                    DrawingContext drawingContext = drawingVisual.RenderOpen();
                    using (drawingContext)
                    {
                        drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(paintSurface.ActualWidth, paintSurface.ActualHeight)));
                    }
                    rtb.Render(drawingVisual);


                    // Encode to correct format
                    var extension = System.IO.Path.GetExtension(dialog.FileName);
                    BitmapEncoder encoder;
                    switch (extension.ToLower())
                    {
                        case ".png":
                            encoder = new PngBitmapEncoder();
                            break;
                        case ".jpg":
                            encoder = new JpegBitmapEncoder();
                            break;
                        case ".gif":
                            encoder = new GifBitmapEncoder();
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        case ".tiff":
                            encoder = new TiffBitmapEncoder();
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    // Save file
                    using (var fileStream = File.Create(dialog.FileName))
                    {
                        encoder.Save(fileStream);
                    }
                    paintSurface.Effect = dropShadow;
                }
            }
            catch
            {
                MessageBox.Show("Wystąpił błąd podczas zapisywania pliku", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadImgToCanvas_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Filter = "Plik PNG (*.png)|*.png|Plik JPG (*.jpg)|*.jpg|Plik GIF (*.gif)|*.gif|Plik BMP (*.bmp)|*.bmp|Plik TIFF (*.tiff)|*.tiff";
                dialog.RestoreDirectory = true;
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    BitmapImage image = new BitmapImage();
                    using (var fileStream = File.OpenRead(dialog.FileName))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = fileStream;

                        image.EndInit();
                    }
                    paintSurface.Width = image.Width;
                    paintSurface.Height = image.Height;
                    paintSurface.Background = new ImageBrush(image);
                }
            }
            catch
            {
                MessageBox.Show("Wystąpił błąd podczas odczytywania pliku", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            if (MessageBox.Show("Wyczyścić zawartość płótna?", "Wyczyść płótno", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                paintSurface.Children.Clear();
                paintSurface.Background = new SolidColorBrush(Colors.White);
            }
        }
        private void ResizeCanvas_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            var dialog = new ResizeCanvasDialog();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                if (MessageBox.Show("Zmiana rozmiaru płótna spowoduje wyczyszczenie go.\nCzy chcesz kontynuować?", "Zmiana rozmiaru płótna",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // Clear and resize canvas
                    paintSurface.Children.Clear();
                    paintSurface.Background = new SolidColorBrush(Colors.White);
                    paintSurface.Width = dialog.CanvasWidth;
                    paintSurface.Height = dialog.CanvasHeight;
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            resetTools();
            MessageBox.Show("Painter - aplikacja do tworzenia grafiki\nProjekt zaliczeniowy z przedmiotu \"Grafika komputerowa i przetwarzanie obrazów\" 2024/2025\n" +
                "Autor: Patryk Gamrat", "O programie", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion
}