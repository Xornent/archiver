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
        FileSystemWatcher watcherPerc;

        public ActionProcess(string arguments, string title, string description, Guid? specifiedId = null)
        {
            if (specifiedId != null) id = specifiedId.Value;
            this._title = title;
            this._description = description;

            string startup = System.Windows.Forms.Application.StartupPath;
            string zpath = startup + @"\7z\x64\7z-unicode.exe";

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

                watcherPerc = new FileSystemWatcher(wd, "percentile");
                watcherPerc.EnableRaisingEvents = true;
                watcherPerc.Changed += percentilePrompt;

                FileSystemWatcher watcherFc = new FileSystemWatcher(wd, "file-conflict");
                watcherFc.EnableRaisingEvents = true;
                watcherFc.Changed += fileConflictPrompt;

                this._process = process;
                process.Start();
                process.WaitForExit();

                string op = process.StandardError.ReadToEnd().Parse7zUnicode();
                string[] lines = op.Replace("\r", "").Split('\n');
                bool displayNext = false;

                int err = 0;
                foreach (var item in lines) {
                    if (item.StartsWith("System ERROR")) {
                        displayNext = true;
                        this.HasError = true;
                        err++;
                        continue;
                    }

                    if (displayNext) {
                        errorSum += "\n" +item ;
                        displayNext = false;
                    }

                    if (item.StartsWith("ERROR")) {
                        errorSum += "\n" + item;
                        this.HasError = true;
                        err++;
                        continue;
                    }
                }
            };

            worker.RunWorkerCompleted += (s, e) => {
                if (Directory.Exists(wd) && specifiedId == null) {
                    Directory.Delete(wd, true);
                }

                if (errorSum != "") {
                    errorSum = $"In total {errorSum.Split('\n').Count() - 1} errors is caught in the call:" + errorSum;
                    Error er = new Error(errorSum);

                    er.ShowDialog();
                }
            };

            this.loader = new Loader(this.worker, false);
            this.loader.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.loader.btnUserCancel.Visibility = Visibility.Visible;
            this.loader.Height = 140;
            this.loader.btnUserCancel.Click += (s, e) => {
                this.HasError = true;
                errorSum += "\n" + "User has cancelled execution.";
                this._process.Kill();
            };
        }

        public void Run()
        {
            this.loader.ShowDialog();
        }

        int _duplicate_prompt = 0;
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
                            _duplicate_prompt = 0;
                            lastPercentile = s_;
                            try {
                                worker.ReportProgress(0, (_title, _description, s_.Trim()));
                            } catch {
                                // may raise System.InvalidOperationException: 'This operation has
                                // already had OperationCompleted called on it and further calls are illegal.'
                            }
                        } else {
                            _duplicate_prompt++;
                            worker.ReportProgress(0, (_title, _description, $"Duplicate message {_duplicate_prompt}"));
                            // this magic number 10 is set:
                            // when the user gives a wrong password and the error message
                            // is too long (supposed), the process will never end! and the
                            // user get stuck. we should find a way out. on my computer, the
                            // duplicate prompt is stuck at 30-40. the longer the message,
                            // the smaller the count when it gets stucked.
                            if (_duplicate_prompt >= 10) {
                                watcherPerc.Changed -= percentilePrompt;
                                _process.Kill();
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
                                    } else {

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
                                PasswordInput pwd = new PasswordInput();
                                if ((pwd.ShowDialog() ?? false)) {
                                    this._process.StandardInput.WriteLine(pwd.Password);
                                } else {
                                    this.HasError = true;
                                    errorSum += "\n" + "User has cancelled password input.";
                                    this._process.Kill();
                                }

                            });

                            t.SetApartmentState(ApartmentState.STA);
                            t.Start();
                        }
                    }
                }
            } catch { }
        }

        public bool HasError { get; set; } = false;
        private string errorSum = "";
    }
}
