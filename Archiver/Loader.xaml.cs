﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Archiver
{
    /// <summary>
    /// Interaction logic for Loader.xaml
    /// </summary>
    public partial class Loader : Window
    {
        public Loader(BackgroundWorker worker, bool showProgress = false)
        {
            InitializeComponent();
            this.ring.Scale = 0.8f;

            if (showProgress) {
                this.progress.Visibility = Visibility.Visible;
            }

            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (s, e) => {
                (string title, string desc, string progress) loaderInfo =
                    ((string title, string desc, string progress)?)e.UserState ?? ("", "", "");
                this.Title = loaderInfo.title;
                this.lblTitle.Content = loaderInfo.title;
                this.lblDesc.Text = loaderInfo.desc;
                this.lblDetail.Text = loaderInfo.progress;

                if (showProgress) {
                    this.progress.Value = e.ProgressPercentage;
                }
            };

            worker.RunWorkerCompleted += (s, e) => {
                this.Close();
            };

            this.Loaded += (s, e) => {
                worker.RunWorkerAsync();
            };
        }
    }
}
