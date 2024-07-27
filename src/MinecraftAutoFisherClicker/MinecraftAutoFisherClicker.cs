using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftAutoFisherClicker
{
    public partial class MinecraftAutoFisherClicker : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, StringBuilder lParam);

        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_RBUTTONUP = 0x205;

        private CancellationTokenSource cts;
        private Process minecraftProcess;

        public MinecraftAutoFisherClicker()
        {
            InitializeComponent();

            RefreshProcessesList();
        }

        private void RefreshProcessesList()
        {
            Process[] processes = Process.GetProcesses()
                .Where(p => p.ProcessName.IndexOf("javaw", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            p.ProcessName.IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            processesComboBox.Items.Clear();
            processesComboBox.DisplayMember = "Value";
            processesComboBox.ValueMember = "Key";

            foreach (Process process in processes)
            {
                processesComboBox.Items.Add(new KeyValuePair<int, string>(process.Id, GetProcessName(process)));
            }
        }

        private static string GetProcessName(Process process)
        {
            if (string.IsNullOrWhiteSpace(process.MainWindowTitle))
            {
                return $"{process.ProcessName}.exe (PID {process.Id})";
            }

            return $"{process.MainWindowTitle} ({process.ProcessName}.exe, PID {process.Id})";
        }

        private void RefreshProcessesButton_Click(object sender, EventArgs e)
        {
            RefreshProcessesList();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            KeyValuePair<int, string>? selectedProcess = (KeyValuePair<int, string>?)processesComboBox.SelectedItem;

            if (!selectedProcess.HasValue)
            {
                MessageBox.Show($"Please select the Minecraft process", "Process not selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            minecraftProcess = Process.GetProcessById(selectedProcess.Value.Key);
            cts = new CancellationTokenSource();
            startButton.Enabled = false;
            stopButton.Enabled = true;

            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    SendMessage(minecraftProcess.MainWindowHandle, WM_RBUTTONDOWN, IntPtr.Zero, null);
                    await Task.Delay(1000);
                }
            }, cts.Token);
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            if (minecraftProcess != null)
            {
                cts.Cancel();
                SendMessage(minecraftProcess.MainWindowHandle, WM_RBUTTONUP, IntPtr.Zero, null);
                startButton.Enabled = true;
                stopButton.Enabled = false;
            }
        }
    }
}
