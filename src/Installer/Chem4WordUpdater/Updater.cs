// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Shared;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Chem4WordUpdater
{
    public partial class Updater : Form
    {
        private string _downloadTarget = "";
        private WebClient _webClient;

        private bool _downloadCompleted;
        private string _downloadedFile;
        private string _msiOriginalFileName = string.Empty;
        private bool _userCancelledUpdate = false;

        private int _retryCount = 0;
        private Stopwatch _sw = new Stopwatch();

        public Updater(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                var ok = true;
                var parts = args[0].ToLower().Split(new char[] { '/', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);

                // Check 1 ensure that website is the source
                if (!(parts[0].Equals("https:") || parts[0].Equals("http:")))
                {
                    ok = false;
                }

                if (ok)
                {
                    // Check 2 ensure that chem4word occurs twice
                    var x = parts.Where(p => p.Contains("chem4word")).Count();
                    if (x != 2)
                    {
                        ok = false;
                    }

                    // Check 3 ensure that file is exe or msi
                    if (!(parts.Last().Equals("msi") || parts.Last().Equals("exe")))
                    {
                        ok = false;
                    }
                }

                if (ok)
                {
                    RegistryHelper.WriteAction("Update Started");
                    _downloadTarget = args[0];
                    if (DownloadFile(_downloadTarget))
                    {
                        StartTimer();
                    }
                }
                else
                {
                    RegistryHelper.WriteAction($"Declined to download {args[0]}");
                    Information.Text = "Error staring download of Chem4Word update";
                }
            }
            else
            {
                RegistryHelper.WriteAction("arg[0] is missing");
            }
        }

        private void OnClick_Cancel(object sender, EventArgs e)
        {
            RegistryHelper.WriteAction("Update was cancelled by User");
            timer1.Enabled = false;
            _userCancelledUpdate = true;
            try
            {
                _webClient.CancelAsync();
            }
            catch
            {
                // Do Nothing
            }
            Close();
        }

        private void OnTick_timer1(object sender, EventArgs e)
        {
            var wordCount = ShowWordProcesses();
            if (wordCount == 0 && _downloadCompleted)
            {
                RegistryHelper.WriteAction("Conditions right to run Update");
                timer1.Enabled = false;

                Application.DoEvents();
                Thread.Sleep(1000);

                RegistryHelper.WriteAction("Running Chem4Word Update");
                const int retryLimit = 3;
                var exitCode = -1;
                var retries = 0;
                while (exitCode != 0 && retries < retryLimit)
                {
                    exitCode = ProcessHelper.RunMsi(_downloadedFile);
                    RegistryHelper.WriteAction($"Chem4Word ExitCode: {exitCode} Attempt: {retries}");
                    if (exitCode == 0)
                    {
                        break;
                    }

                    if (exitCode == 1602 || exitCode == 1603)
                    {
                        if (retries < retryLimit - 1)
                        {
                            Information.Text = $"ExitCode: {exitCode}; Waiting for a few seconds before trying again!";
                            Application.DoEvents();
                            Thread.Sleep(3000);
                        }
                        retries++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (exitCode == 0)
                {
                    _userCancelledUpdate = true;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Error {exitCode} while installing {_downloadedFile}", "Chem4Word Updater", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Information.Text = $"Error {exitCode} while installing {_msiOriginalFileName}";
                    UpdateNow.Enabled = true;
                }
            }
        }

        private int ShowWordProcesses()
        {
            var processes = Process.GetProcessesByName("winword");

            WordInstances.Items.Clear();
            foreach (var proc in processes)
            {
                WordInstances.Items.Add($"[{proc.Id}] - {proc.ProcessName}");
            }

            return processes.Length;
        }

        private void StartTimer()
        {
            timer1.Enabled = true;
        }

        private bool DownloadFile(string url)
        {
            var started = false;

            var securityProtocol = ServicePointManager.SecurityProtocol;

            _sw = new Stopwatch();
            _sw.Start();

            try
            {
                progressBar1.Value = 0;

                RegistryHelper.WriteAction($"Downloading {url}");

                var parts = url.Split('/');
                var filename = parts[parts.Length - 1];

                _msiOriginalFileName = filename;
                var guid = Guid.NewGuid().ToString("N");

                var downloadPath = FolderHelper.GetPath(KnownFolder.Downloads);
                if (!Directory.Exists(downloadPath))
                {
                    downloadPath = Path.GetTempPath();
                }

                _downloadedFile = Path.Combine(downloadPath, filename);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                _webClient = new WebClient();
                _webClient.Headers.Add("user-agent", "Chem4Word Updater");

                _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                _webClient.DownloadFileCompleted += OnDownloadComplete;

                _webClient.DownloadFileAsync(new Uri(url), _downloadedFile);

                started = true;
            }
            catch (Exception ex)
            {
                RegistryHelper.WriteAction(ex.Message);
                Information.Text = ex.Message;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = securityProtocol;
            }

            return started;
        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            RegistryHelper.WriteAction("Download complete");
            progressBar1.Value = 100;
            _sw.Stop();

            if (e.Cancelled)
            {
                RegistryHelper.WriteAction($"Downloading of {_downloadTarget} was Cancelled");
                Information.Text = $"Downloading of {_msiOriginalFileName} was Cancelled";
            }
            else if (e.Error != null)
            {
                _retryCount++;
                if (_retryCount > 3)
                {
                    Information.Text = $"Too many errors downloading {_msiOriginalFileName}, please check your internet connection and try again!";
                }
                else
                {
                    DownloadFile(_downloadTarget);
                }
            }
            else
            {
                var fi = new FileInfo(_downloadedFile);
                if (fi.Length == 0)
                {
                    _retryCount++;
                    if (_retryCount > 3)
                    {
                        Information.Text = $"Too many errors downloading {_msiOriginalFileName}, please check your internet connection and try again!";
                    }
                    else
                    {
                        DownloadFile(_downloadTarget);
                    }
                }
                else
                {
                    _downloadCompleted = true;
                    _webClient.Dispose();

                    UpdateNow.Enabled = false;
                    Information.Text = "Your update has been downloaded.  It will be automatically installed once all Microsoft Word processes are closed.";
                    RegistryHelper.WriteAction($"Downloading of {_downloadTarget} took {_sw.ElapsedMilliseconds.ToString("#,##0", CultureInfo.InvariantCulture)}ms");
                    var seconds = _sw.ElapsedMilliseconds / 1000.0;
                    var kiloBytes = fi.Length / 1024.0;
                    var speed = kiloBytes / seconds / 1000.0;
                    RegistryHelper.WriteAction($"Download speed {speed.ToString("#,##0.000", CultureInfo.InvariantCulture)}MiB/s");
                }
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void OnFormClosing_Updater(object sender, FormClosingEventArgs e)
        {
            if (!_userCancelledUpdate)
            {
                e.Cancel = true;
            }
        }

        private void OnLoad_Updater(object sender, EventArgs e)
        {
            // Move up and left by half the form size
            Left -= Width / 2;
            Top -= Height / 2;
        }
    }
}