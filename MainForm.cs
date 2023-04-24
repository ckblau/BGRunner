using System.Diagnostics;

namespace BGRunner {
    public partial class MainForm : Form {
        private string target = "";
        private string target_name = "";
        private string target_args = "";
        private string target_log = "";
        private Process process = new();
        private StreamWriter? writer;

        public MainForm() {
            InitializeComponent();
            ParseArgs();
        }

        private void ParseArgs() {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length <= 1) return; 
            target = args[1];
            target_name = target.Trim();
            if (args.Length > 2) {
                target_args = string.Join(" ", args[2..]);
                if (target.ToUpper() == "PYTHON" || target.ToUpper() == "PYTHON3") {
                    target_name = args[2].Trim();
                    target_args = "-u " + target_args;
                }
            }
            DateTime now = DateTime.Now;
            target_log = string.Format("BGRunnerLog_{0}_{1}.txt", target_name, now.ToString("yyyyMMdd_HHmmssfff"));
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
                if (writer != null) {
                    writer.WriteLine(line);
                    writer.Flush();
                }
                Output.AppendText(line + Environment.NewLine);
                Output.SelectionStart = Output.TextLength;
                Output.ScrollToCaret();
            }
        }

        private void StartProcess() {
            try {
                writer = new StreamWriter(target_log);
            }
            catch (Exception ex) {
                OutputPrintLine("---------- BGRunner: Failed to create log file ----------");
                OutputPrintLine(ex.Message);
                OutputPrintLine();
                OutputPrintLine("---------- BGRunner: Log disabled ----------");
                OutputPrintLine();
            }
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
            if (writer != null) {
                writer.Close();
                writer = null;
            }
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
                Text = string.Format("BGRunner ({0})", target_name);
                TrayIcon.Text = string.Format("BGRunner ({0})", target_name);
                StartProcess();
            }
        }

        private void HideWindow() {
            TrayIcon.Visible = true;
            Visible = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
        }

        private void ShowWindow() {
            TrayIcon.Visible = false;
            Visible = true;
            ShowInTaskbar = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            if (WindowState == FormWindowState.Minimized) {
                HideWindow();
            }
        }

        private void TrayIcon_Click(object sender, EventArgs e) {
            MouseEventArgs Mouse_e = (MouseEventArgs)e;
            if (Mouse_e.Button == MouseButtons.Left)
                ShowWindow();
        }

        private void Show_Click(object sender, EventArgs e) {
            ShowWindow();
        }

        private bool KillProcess() {
            if (target == "") return false;
            if (!process.HasExited) {
                process.Kill();
                return true;
            }
            return false;
        }

        private void Exit_Click(object sender, EventArgs e) {
            if (target != "") {
                KillProcess();
                process.WaitForExit(100);
            }
            if (writer != null) {
                writer.Close();
                writer = null;
            }
            Application.Exit();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e) {
            if (writer != null) {
                writer.Close();
                writer = null;
            }
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (target != "") {
                //KillProcess();
                if (KillProcess()) e.Cancel = true;
            }
        }
    }
}