using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicCard
{
    public sealed partial class MainWindow : Window
    {
        private string? _filePath;
        private StorageFile? file;

        // Playback fields (moved to instance scope so StopWave can access them)
        private IntPtr hWaveOut = IntPtr.Zero;
        private GCHandle? audioHandle;
        private bool isPlaying = false;

        // Header state for unprepare
        private WaveHeader waveHeader;
        private bool headerPrepared = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Pod³¹czamy obs³ugê zdarzeñ do istniej¹cych kontrolek z XAML
            SelectFileButton.Click += SelectFileButton_Click;

            // Dla demonstracji: przypiszemy Start/Stop PlaySound do sekcji "PlaySound" (StartOneButton / StopOneButton)
            StartOneButton.Click += PlayStartFirstButton_Click;
            StopOneButton.Click += PlayStopFirstButton_Click;

            // MCI Recording
            RecordStartButton.Click += RecordStartButton_Click;
            RecordStopButton.Click += RecordStopButton_Click;


            // Na start przyciski wy³¹czone dopóki nie wybierzemy pliku
            StartOneButton.IsEnabled = false;
            StopOneButton.IsEnabled = false;
            RecordStopButton.IsEnabled = false; // Przycisk stopu jest wy³¹czony na starcie
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".wav");

            // WinUI3: trzeba zainicjalizowaæ picker oknem natywnym
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _filePath = file.Path;
                SelectedFileText.Text = _filePath;
                StartOneButton.IsEnabled = true;
                StopOneButton.IsEnabled = true;

                mediaPlayerControl.Source = MediaSource.CreateFromStorageFile(file);
            }
        }

        private void PlayStartFirstButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath))
                return;

            NativeMethods.PlaySound(_filePath, IntPtr.Zero, NativeMethods.SoundFlags.SND_FILENAME | NativeMethods.SoundFlags.SND_ASYNC);
        }

        private void PlayStopFirstButton_Click(object sender, RoutedEventArgs e)
        {
            NativeMethods.PlaySound(null, IntPtr.Zero, NativeMethods.SoundFlags.SND_PURGE);
        }

        // --- P/Invoke definicje ---
        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WaveFormat lpFormat, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [StructLayout(LayoutKind.Sequential)]
        struct WaveFormat
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WaveHeader
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        private async void PlayWaveAsync(object sender, RoutedEventArgs e)
        {
            if (file == null)
                return;

            byte[] data = await File.ReadAllBytesAsync(file.Path);
            if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "RIFF")
            {
                return;
            }

            // Proste parsowanie WAV headera (PCM)
            int fmtPos = BitConverter.ToInt32(data, 12) == 0x20746D66 ? 12 : 20;
            int sampleRate = BitConverter.ToInt32(data, fmtPos + 12);
            short bits = BitConverter.ToInt16(data, fmtPos + 22);
            short channels = BitConverter.ToInt16(data, fmtPos + 10);

            int dataPos = Array.IndexOf(data, (byte)'d', 36); // znajdŸ "data"
            while (dataPos < data.Length - 4 && System.Text.Encoding.ASCII.GetString(data, dataPos, 4) != "data")
                dataPos++;
            int dataSize = BitConverter.ToInt32(data, dataPos + 4);
            int dataOffset = dataPos + 8;

            var fmt = new WaveFormat
            {
                wFormatTag = 1, // PCM
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = (ushort)bits,
                nBlockAlign = (ushort)((bits / 8) * channels),
                nAvgBytesPerSec = (uint)(sampleRate * channels * bits / 8),
                cbSize = 0
            };

            IntPtr localWaveOut;
            int result = waveOutOpen(out localWaveOut, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
            if (result != 0)
            {
                return;
            }

            // Pin audio buffer and store state in instance fields so StopWave can access them
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            this.audioHandle = handle;

            this.waveHeader = new WaveHeader
            {
                lpData = handle.AddrOfPinnedObject() + dataOffset,
                dwBufferLength = (uint)dataSize,
                dwFlags = 0,
                dwLoops = 0
            };

            // assign device handle to instance
            this.hWaveOut = localWaveOut;

            int sz = Marshal.SizeOf<WaveHeader>();
            waveOutPrepareHeader(this.hWaveOut, ref this.waveHeader, sz);
            headerPrepared = true;

            waveOutWrite(this.hWaveOut, ref this.waveHeader, sz);

            isPlaying = true;

            // Poczekaj a¿ dŸwiêk siê odtworzy (nieblokuj¹co)
            await System.Threading.Tasks.Task.Delay(dataSize / (int)fmt.nAvgBytesPerSec * 1000 + 500);

            // After playback completes, if not stopped externally, clean up
            try
            {
                if (isPlaying && hWaveOut != IntPtr.Zero)
                {
                    if (headerPrepared)
                    {
                        waveOutUnprepareHeader(this.hWaveOut, ref this.waveHeader, sz);
                        headerPrepared = false;
                    }
                    waveOutClose(this.hWaveOut);
                    this.hWaveOut = IntPtr.Zero;
                }
            }
            finally
            {
                if (this.audioHandle.HasValue && this.audioHandle.Value.IsAllocated)
                {
                    this.audioHandle.Value.Free();
                    this.audioHandle = null;
                }
                isPlaying = false;
            }
        }
        private async void StopButtonThree_Click(object sender, RoutedEventArgs e)
        {
            await StopThreePlaybackAsync();
        }
        private async Task StopThreePlaybackAsync()
        {
            if (!isPlaying || hWaveOut == IntPtr.Zero) return;

            isPlaying = false; // Prevent cleanup timer from running

            waveOutReset(hWaveOut); // Stop playback immediately

            int sz = Marshal.SizeOf<WaveHeader>();
            if (headerPrepared)
            {
                waveOutUnprepareHeader(hWaveOut, ref waveHeader, sz);
                headerPrepared = false;
            }

            waveOutClose(hWaveOut);
            hWaveOut = IntPtr.Zero;

            if (audioHandle.HasValue && audioHandle.Value.IsAllocated)
            {
                audioHandle.Value.Free();
                audioHandle = null;
            }
        }

        // Metoda 4: MCI
        private void MciStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath))
                return;

            // Zamknij poprzedni, jeœli by³ otwarty, i otwórz nowy
            NativeMethods.mciSendString("close MyMciSound", null, 0, IntPtr.Zero);
            NativeMethods.mciSendString($"open \"{_filePath}\" alias MyMciSound", null, 0, IntPtr.Zero);
            // Odtwórz od pocz¹tku
            NativeMethods.mciSendString("play MyMciSound from 0", null, 0, IntPtr.Zero);
        }

        private void MciStopButton_Click(object sender, RoutedEventArgs e)
        {
            // Zatrzymaj i zamknij
            NativeMethods.mciSendString("stop MyMciSound", null, 0, IntPtr.Zero);
            NativeMethods.mciSendString("close MyMciSound", null, 0, IntPtr.Zero);
        }


        /// TODO: fix: pause works like stop
        private void MciPauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Wstrzymaj odtwarzanie
            NativeMethods.mciSendString("pause MyMciSound", null, 0, IntPtr.Zero);
        }

        // Metoda 6: MCI Recording
        private void RecordStartButton_Click(object sender, RoutedEventArgs e)
        {
            NativeMethods.mciSendString("open new type waveaudio alias MyRecording", null, 0, IntPtr.Zero);
            NativeMethods.mciSendString("record MyRecording", null, 0, IntPtr.Zero);

            RecordStartButton.IsEnabled = false;
            RecordStopButton.IsEnabled = true;
        }

        private async void RecordStopButton_Click(object sender, RoutedEventArgs e)
        {
            NativeMethods.mciSendString("stop MyRecording", null, 0, IntPtr.Zero);

            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            savePicker.FileTypeChoices.Add("WAV file", new System.Collections.Generic.List<string>() { ".wav" });
            savePicker.SuggestedFileName = "recording";

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                NativeMethods.mciSendString($"save MyRecording \"{file.Path}\"", null, 0, IntPtr.Zero);
            }

            NativeMethods.mciSendString("close MyRecording", null, 0, IntPtr.Zero);

            RecordStartButton.IsEnabled = true;
            RecordStopButton.IsEnabled = false;
        }
    }
}
