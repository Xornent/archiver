using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Archiver
{
    class ActionProcess
    {
        private Loader loader = null;

        BackgroundWorker worker = new BackgroundWorker();
        private Guid id = Guid.NewGuid();

        string lastPercentile = "";
        string lastFc = "";
        string lastPassword = "";

        string _title = "";
        string _description = "";
        Process _process = null;

        public ActionProcess(string arguments, string title, string description, Guid? specifiedId = null)
        {
            if (specifiedId != null) id = specifiedId.Value;
            this._title = title;
            this._description = description;

            string startup = System.Windows.Forms.Application.StartupPath;
            string zpath = startup + @"\7z\x64\7z.exe";

            string wd = startup + @"\working\" + id.ToString().ToLower() + "\\";
            if (Directory.Exists(wd) && specifiedId == null) {
                Directory.Delete(wd, true);
            }

            if (specifiedId == null)
                Directory.CreateDirectory(wd);

            worker.DoWork += (s, e) => {
                worker.ReportProgress(0, (_title, _description, "0% Preparing ..."));

                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = zpath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.EnableRaisingEvents = true;
                process.StartInfo.WorkingDirectory = wd;

                FileSystemWatcher watcher = new FileSystemWatcher(wd, "password");
                watcher.EnableRaisingEvents = true;
                watcher.Changed += passwordPrompt;

                FileSystemWatcher watcherPerc = new FileSystemWatcher(wd, "percentile");
                watcherPerc.EnableRaisingEvents = true;
                watcherPerc.Changed += percentilePrompt;

                FileSystemWatcher watcherFc = new FileSystemWatcher(wd, "file-conflict");
                watcherFc.EnableRaisingEvents = true;
                watcherFc.Changed += fileConflictPrompt;

                this._process = process;
                process.Start();
                process.WaitForExit();

                string op = process.StandardError.ReadToEnd();
                string[] lines = op.Replace("\r", "").Split('\n');
                bool displayNext = false;
                foreach (var item in lines) {
                    if (item.StartsWith("System ERROR")) {
                        displayNext = true;
                        continue;
                    }

                    if (displayNext) {
                        MessageBox.Show(item);
                        displayNext = false;
                    }
                }
            };

            worker.RunWorkerCompleted += (s, e) => {
                if (Directory.Exists(wd) && specifiedId == null) {
                    Directory.Delete(wd, true);
                }
            };

            this.loader = new Loader(this.worker, false);
            this.loader.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        public void Run()
        {
            this.loader.ShowDialog();
        }

        void percentilePrompt(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(100);

            try {
                if (!File.Exists(e.FullPath)) return;
                using (FileStream stream = new FileStream(e.FullPath, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        string s_ = reader.ReadToEnd();
                        s_ = s_.Replace("\r", "").
                            Replace("\n", "").
                            Replace("\t", "").
                            Replace("\b", "");
                        if (lastPercentile != s_) {
                            lastPercentile = s_;
                            try {
                                worker.ReportProgress(0, (_title, _description, s_.Trim()));
                            } catch {
                                // may raise System.InvalidOperationException: 'This operation has
                                // already had OperationCompleted called on it and further calls are illegal.'
                            }
                        }
                    }
                }
            } catch { }
        }

        void fileConflictPrompt(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(100);

            try {
                if (!File.Exists(e.FullPath)) return;
                using (FileStream stream = new FileStream(e.FullPath, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        string s_ = reader.ReadToEnd();
                        s_ = s_.
                            Replace("\t", "").
                            Replace("\b", "");
                        if (lastFc != s_) {
                            lastFc = s_;

                            Thread t = new Thread(() => {
                                bool cont = true;
                                while (cont) {
                                    FileConflict conflict = new FileConflict(s_);
                                    cont = !(conflict.ShowDialog() ?? false);

                                    if (!cont) {
                                        // respond to the conflict
                                        this._process.StandardInput.WriteLine(conflict.Response);
                                    }
                                }
                            });

                            t.SetApartmentState(ApartmentState.STA);
                            t.Start();
                        }
                    }
                }
            } catch { }
        }

        void passwordPrompt(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(100);
            try {
                if (!File.Exists(e.FullPath)) return;
                using (FileStream stream = new FileStream(e.FullPath, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        string s_ = reader.ReadToEnd();
                        s_ = s_.
                            Replace("\t", "").
                            Replace("\b", "");
                        if (lastPassword != s_) {
                            lastPassword = s_;

                            Thread t = new Thread(() => {
                                bool cont = true;
                                while (cont) {
                                    PasswordInput pwd = new PasswordInput();
                                    cont = !(pwd.ShowDialog() ?? false);

                                    if (!cont) {

                                        this._process.StandardInput.WriteLine(pwd.Password);
                                    }
                                }
                            });

                            t.SetApartmentState(ApartmentState.STA);
                            t.Start();
                        }
                    }
                }
            } catch { }
        }
    }
}
