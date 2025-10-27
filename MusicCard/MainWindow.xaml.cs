using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Windows.Storage;
using WinRT; // for InitializeWithWindow
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicCard
{
    public sealed partial class MainWindow : Window
    {
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

            
        }

    }
}
