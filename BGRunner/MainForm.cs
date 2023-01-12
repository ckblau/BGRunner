using System.Diagnostics;

namespace BGRunner {
    public partial class MainForm : Form {
        private string target = "";
        private string target_args = "";
        private Process process = new();

        public MainForm(string[] args) {
            InitializeComponent();
            ParseArgs(args);
        }

        private void ParseArgs(string[] args) {
            if (args.Length == 0) target = "";
            else target = args[0];
            if (args.Length <= 1) target_args = "";
            else target_args = string.Join(" ", args[1..]);
        }

        private void OutputPrintLine(string line = "") {
            if (InvokeRequired) {
                try {
                    Invoke(OutputPrintLine, line);
                }
                catch {
                    return;
                }
            }
            else {
                Output.AppendText(line + Environment.NewLine);
                Output.SelectionStart = Output.TextLength;
                Output.ScrollToCaret();
            }
        }

        private void StartProcess() {
            OutputPrintLine(string.Format("{0} {1}", target, target_args));
            OutputPrintLine();
            OutputPrintLine("---------- BGRunner: Starting process ----------");
            OutputPrintLine();
            process.StartInfo.FileName = target;
            process.StartInfo.Arguments = target_args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += OutputHandler;
            process.Exited += new EventHandler(ExitHandler);
            try {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex) {
                OutputPrintLine("---------- BGRunner: Failed to start process ----------");
                OutputPrintLine(ex.Message);
            }
        }

        void ExitHandler(object? sendingProcess, EventArgs e) {
            process.WaitForExit();
            OutputPrintLine();
            OutputPrintLine(string.Format("---------- BGRunner: Process exited with code {0} ----------", process.ExitCode));
            OutputPrintLine();
        }

        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
            if (outLine.Data != null) {
                OutputPrintLine(outLine.Data);
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            Output.Text = "";
            if (target == "") {
                Text = "BGRunner";
                TrayIcon.Text = "BGRunner";
                OutputPrintLine("No command line specified.");
                OutputPrintLine("~Major Tom to Ground Control: No runway in sight!~");
                OutputPrintLine("");
                OutputPrintLine("Usage:");
                OutputPrintLine("    BGRunner <command_line>");
                OutputPrintLine("");
                OutputPrintLine("    Start any process with window hidden.");
                OutputPrintLine("    Minimize this window to run in background.");
                OutputPrintLine("    Use the tray icon to bring back.");
            }
            else {
                Text = string.Format("BGRunner ({0})", target);
                TrayIcon.Text = string.Format("BGRunner ({0})", target);
                StartProcess();
            }
        }

        private void HideWindow() {
            TrayIcon.Visible = true;
            Visible = false;
            ShowInTaskbar = false;
        }

        private void ShowWindow() {
            TrayIcon.Visible = false;
            Visible = true;
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            if (WindowState == FormWindowState.Minimized) {
                HideWindow();
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e) {
            ShowWindow();
        }

        private void Show_Click(object sender, EventArgs e) {
            ShowWindow();
        }

        private bool KillProcess() {
            if (!process.HasExited) {
                process.Kill();
                return true;
            }
            return false;
        }

        private void Exit_Click(object sender, EventArgs e) {
            KillProcess();
            process.WaitForExit(100);
            Application.Exit();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e) {
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            //KillProcess();
            if (KillProcess()) e.Cancel = true;
        }
    }
}