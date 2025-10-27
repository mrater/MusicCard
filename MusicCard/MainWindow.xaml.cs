using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicCard
{
    public sealed partial class MainWindow : Window
    {
        private string? _filePath;

        public MainWindow()
        {
            this.InitializeComponent();

            // Pod³¹czamy obs³ugê zdarzeñ do istniej¹cych kontrolek z XAML
            SelectFileButton.Click += SelectFileButton_Click;

            // Dla demonstracji: przypiszemy Start/Stop PlaySound do sekcji "PlaySound" (StartOneButton / StopOneButton)
            StartOneButton.Click += PlayStartButton_Click;
            StopOneButton.Click += PlayStopButton_Click;

            // Na start przyciski wy³¹czone dopóki nie wybierzemy pliku
            StartOneButton.IsEnabled = false;
            StopOneButton.IsEnabled = false;
            // Opcjonalnie wy³¹cz Pause jeœli nieobs³ugiwany
            PauseOneButton.IsEnabled = false;
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".wav");

            // WinUI3: trzeba zainicjalizowaæ picker oknem natywnym
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _filePath = file.Path;
                SelectedFileText.Text = _filePath;
                StartOneButton.IsEnabled = true;
                StopOneButton.IsEnabled = true;
            }
        }

        private void PlayStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath))
                return;

            NativeMethods.PlaySound(_filePath, IntPtr.Zero, NativeMethods.SoundFlags.SND_FILENAME | NativeMethods.SoundFlags.SND_ASYNC);
        }

        private void PlayStopButton_Click(object sender, RoutedEventArgs e)
        {
            NativeMethods.PlaySound(null, IntPtr.Zero, NativeMethods.SoundFlags.SND_PURGE);
        }
    }
}
