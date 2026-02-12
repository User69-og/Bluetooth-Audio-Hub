using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading.Tasks;
using NAudio.Wave;

namespace BluetoothAudioHub {
    static class Program {
        private static NotifyIcon trayIcon;
        private static BufferedWaveProvider waveProvider;
        private static WasapiOut waveOut;
        private static UdpClient udpServer;
        private static bool isRunning = true;

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Setup Tray Icon
            trayIcon = new NotifyIcon() {
                Icon = System.Drawing.SystemIcons.AudioVolumeHigh,
                Text = "Bluetooth Audio Hub (Active)",
                ContextMenuStrip = new ContextMenuStrip()
            };
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => ExitApp());
            trayIcon.Visible = true;

            // 2. Setup Audio Engine (16kHz, 16-bit, Mono)
            var format = new WaveFormat(16000, 16, 1);
            waveProvider = new BufferedWaveProvider(format) {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(500) // 500ms safety buffer
            };
            
            waveOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);
            waveOut.Init(waveProvider);
            waveOut.Play();

            // 3. Start Background Listening
            Task.Run(() => StartListening());

            Application.Run(); // Keeps the app alive in the tray
        }

        static async Task StartListening() {
            udpServer = new UdpClient(50005);
            while (isRunning) {
                try {
                    var result = await udpServer.ReceiveAsync();
                    waveProvider.AddSamples(result.Buffer, 0, result.Buffer.Length);
                } catch { /* Handle socket closing */ }
            }
        }

        static void ExitApp() {
            isRunning = false;
            udpServer?.Close();
            waveOut?.Stop();
            waveOut?.Dispose();
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}