using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using WinRT; // for InitializeWithWindow

namespace MusicCard
{
    public sealed partial class MainWindow : Window
    {
        private string? _filePath;

        [Flags]
        private enum PlaySoundFlags : int
        {
            SND_SYNC = 0x0000,
            SND_ASYNC = 0x0001,
            SND_NODEFAULT = 0x0002,
            SND_MEMORY = 0x0004,
            SND_LOOP = 0x0008,
            SND_NOSTOP = 0x0010,
            SND_NOWAIT = 0x00002000,
            SND_ALIAS = 0x00010000,
            SND_ALIAS_ID = 0x00110000,
            SND_FILENAME = 0x00020000,
            SND_RESOURCE = 0x00040004
        }

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern bool PlaySound(string? pszSound, IntPtr hmod, PlaySoundFlags fdwSound);

        public MainWindow()
        {
            this.InitializeComponent();

            // Pod³¹czamy obs³ugê zdarzeñ do istniej¹cych kontrolek z XAML
            SelectFileButton.Click += ChooseButton_Click;

            // Dla demonstracji: przypiszemy Start/Stop PlaySound do sekcji "PlaySound" (StartOneButton / StopOneButton)
            StartOneButton.Click += PlayStartButton_Click;
            StopOneButton.Click += PlayStopButton_Click;

            // Na start przyciski wy³¹czone dopóki nie wybierzemy pliku
            StartOneButton.IsEnabled = false;
            StopOneButton.IsEnabled = false;
            // Opcjonalnie wy³¹cz Pause jeœli nieobs³ugiwany
            PauseOneButton.IsEnabled = false;
        }

        private async void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".wav");

            // WinUI3: trzeba zainicjalizowaæ picker oknem natywnym
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

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

            // Asynchroniczne odtwarzanie pliku
            PlaySound(_filePath, IntPtr.Zero, PlaySoundFlags.SND_FILENAME | PlaySoundFlags.SND_ASYNC);
        }

        private void PlayStopButton_Click(object sender, RoutedEventArgs e)
        {
            // Zatrzymuje bie¿¹ce PlaySound
            PlaySound(null, IntPtr.Zero, 0);
        }
    }
}
