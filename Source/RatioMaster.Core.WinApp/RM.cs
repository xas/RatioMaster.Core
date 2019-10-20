using Microsoft.Win32;
using RatioMaster.Core.Helpers;
using RatioMaster.Core.NetworkProtocol;
using RatioMaster.Core.TorrentProtocol;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace RatioMaster.Core
{
    internal partial class RM : UserControl
    {
        // Variables
        #region Variables
        private bool getnew = true;
        private readonly Random rand = new Random(((int)DateTime.Now.Ticks));
        private int remWork = 0;
        internal string DefaultDirectory = "";
        private const string DefaultClient = "uTorrent";
        private const string DefaultClientVersion = "3.3.2";

        // internal delegate SocketEx createSocketCallback();
        internal delegate void SetTextCallback(string logLine);

        internal delegate void updateScrapCallback(string seedStr, string leechStr, string finishedStr);

        public NetworkManager networkManager { get; private set; }
        public TorrentManager torrentManager { get; private set; }

        private bool seedMode = false;
        private bool updateProcessStarted = false;
        private bool requestScrap;
        private bool scrapStatsUpdated;
        private int temporaryIntervalCounter = 0;
        bool IsExit = false;
        private readonly string version = "";

        #endregion

        // Methods
        #region Methods
        #region Main Form Events
        internal RM()
        {
            InitializeComponent();
            deployDefaultValues();
            GetPCinfo();
            ReadSettings();
        }

        internal void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (s == null)
            {
                s = (string[])e.Data.GetData("System.String[]", true);
                if (s == null)
                {
                    return;
                }
            }

            loadTorrentFileInfo(s[0]);
        }

        internal void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetFormats().ToString().Equals("System.String[]"))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        internal void ExitRatioMaster()
        {
            IsExit = true;
            if (updateProcessStarted)
            {
                @StopButton_Click(null, null);
            }
        }

        internal void deployDefaultValues()
        {
            torrentManager = new TorrentManager();
            trackerAddress.Text = torrentManager.Info.Tracker;
            shaHash.Text = torrentManager.Info.Hash;
            long num1 = torrentManager.Info.UploadRate / 1024;
            uploadRate.Text = num1.ToString();
            long num2 = torrentManager.Info.DownloadRate / 1024;
            downloadRate.Text = num2.ToString();
            interval.Text = torrentManager.Info.Interval.ToString();
            comboProxyType.SelectedItem = "None";
        }

        #endregion
        #region Log code
        internal void AddClientInfo()
        {
            // Add log info
            AddLogLine("CLIENT EMULATION INFO:");
            AddLogLine("Name: " + torrentManager.Client.Name);
            AddLogLine("HttpProtocol: " + torrentManager.Client.HttpProtocol);
            AddLogLine("HashUpperCase: " + torrentManager.Client.HashUpperCase);
            AddLogLine("Key: " + torrentManager.Client.Key);
            AddLogLine("Headers:......");
            AddLog(torrentManager.Client.Headers);
            AddLogLine("PeerID: " + torrentManager.Client.PeerID);
            AddLogLine("Query: " + torrentManager.Client.Query + "\n" + "\n");
        }

        internal void AddLog(string logLine)
        {
            if (logWindow.InvokeRequired)
            {
                SetTextCallback d = AddLogLine;
                Invoke(d, new object[] { logLine });
            }
            else
            {
                if (checkLogEnabled.Checked && IsExit != true)
                {
                    try
                    {
                        logWindow.AppendText(logLine);
                        logWindow.ScrollToCaret();
                    }
                    catch (Exception) { }
                }
            }
        }

        internal void AddLogLine(string logLine)
        {
            if (logWindow.InvokeRequired && IsExit != true)
            {
                SetTextCallback d = AddLogLine;
                Invoke(d, new object[] { logLine });
            }
            else
            {
                if (checkLogEnabled.Checked)
                {
                    try
                    {
                        DateTime dtNow = DateTime.Now;
                        string dateString;

                        if (!MainForm._24h_format_enabled)
                            dateString = "[" + String.Format("{0:hh:mm:ss}", dtNow) + "]";
                        else
                            dateString = "[" + String.Format("{0:HH:mm:ss}", dtNow) + "]";

                        logWindow.AppendText(dateString + " " + logLine + "\r\n");
                        logWindow.ScrollToCaret();
                    }
                    catch (Exception) { }
                }
            }
        }

        internal void ClearLog()
        {
            logWindow.Clear();
        }

        internal void GetPCinfo()
        {
            try
            {
                AddLogLine("CurrentDirectory: " + Environment.CurrentDirectory);
                AddLogLine("HasShutdownStarted: " + Environment.HasShutdownStarted);
                AddLogLine("MachineName: " + Environment.MachineName);
                AddLogLine("OSVersion: " + Environment.OSVersion);
                AddLogLine("ProcessorCount: " + Environment.ProcessorCount);
                AddLogLine("UserDomainName: " + Environment.UserDomainName);
                AddLogLine("UserInteractive: " + Environment.UserInteractive);
                AddLogLine("UserName: " + Environment.UserName);
                AddLogLine("Version: " + Environment.Version);
                AddLogLine("WorkingSet: " + Environment.WorkingSet);
                AddLogLine("");
            }
            catch (Exception) { }
        }

        internal void SaveLog_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamWriter sw = new StreamWriter(SaveLog.FileName))
            {
                sw.Write(logWindow.Text);
                sw.Close();
            }
        }

        #endregion
        #region Tcp Listener code
        private void OpenTcpListener()
        {
            try
            {
                if (checkTCPListen.Checked && comboProxyType.SelectedIndex == 0)
                {
                    networkManager.CreateTcpListener(torrentManager.Info.Port, torrentManager.Info.PeerID, torrentManager.File.InfoHash, torrentManager.File.InfoHashBytes);
                }
            }
            catch (Exception e)
            {
                AddLogLine("Error in OpenTcpListener(): " + e.Message);

                return;
            }

            AddLogLine("OpenTcpListener() successfully finished!");
        }
        #endregion

        #region Get client
        internal string GetClientName()
        {
            return cmbClient.SelectedItem + " " + cmbVersion.SelectedItem;
        }

        internal void cmbClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbVersion.Items.Clear();
            switch (cmbClient.SelectedItem.ToString())
            {
                case "BitComet":
                    {
                        cmbVersion.Items.Add("1.20");
                        cmbVersion.Items.Add("1.03");
                        cmbVersion.Items.Add("0.98");
                        cmbVersion.Items.Add("0.96");
                        cmbVersion.Items.Add("0.93");
                        cmbVersion.Items.Add("0.92");
                        cmbVersion.SelectedItem = "1.20";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Vuze":
                    {
                        cmbVersion.Items.Add("4.2.0.8");
                        cmbVersion.SelectedItem = "4.2.0.8";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "50";
                        break;
                    }

                case "Azureus":
                    {
                        cmbVersion.Items.Add("3.1.1.0");
                        cmbVersion.Items.Add("3.0.5.0");
                        cmbVersion.Items.Add("3.0.4.2");
                        cmbVersion.Items.Add("3.0.3.4");
                        cmbVersion.Items.Add("3.0.2.2");
                        cmbVersion.Items.Add("2.5.0.4");
                        cmbVersion.SelectedItem = "3.1.1.0";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "50";
                        break;
                    }

                case "uTorrent":
                    {
                        cmbVersion.Items.Add("3.3.2");
                        cmbVersion.Items.Add("3.3.0");
                        cmbVersion.Items.Add("3.2.0");
                        cmbVersion.Items.Add("2.0.1 (build 19078)");
                        cmbVersion.Items.Add("1.8.5 (build 17414)");
                        cmbVersion.Items.Add("1.8.1-beta(11903)");
                        cmbVersion.Items.Add("1.8.0");
                        cmbVersion.Items.Add("1.7.7");
                        cmbVersion.Items.Add("1.7.6");
                        cmbVersion.Items.Add("1.7.5");
                        cmbVersion.Items.Add("1.6.1");
                        cmbVersion.Items.Add("1.6");
                        cmbVersion.SelectedItem = "3.3.2";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitTorrent":
                    {
                        cmbVersion.Items.Add("6.0.3 (8642)");
                        cmbVersion.SelectedItem = "6.0.3 (8642)";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Transmission":
                    {
                        cmbVersion.Items.Add("2.82 (14160)");
                        cmbVersion.Items.Add("2.92 (14714)");
                        cmbVersion.SelectedItem = "2.92 (14714)";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitLord":
                    {
                        cmbVersion.Items.Add("1.1");
                        cmbVersion.SelectedItem = "1.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "ABC":
                    {
                        cmbVersion.Items.Add("3.1");
                        cmbVersion.SelectedItem = "3.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BTuga":
                    {
                        cmbVersion.Items.Add("2.1.8");
                        cmbVersion.SelectedItem = "2.1.8";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitTornado":
                    {
                        cmbVersion.Items.Add("0.3.17");
                        cmbVersion.SelectedItem = "0.3.17";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Burst":
                    {
                        cmbVersion.Items.Add("3.1.0b");
                        cmbVersion.SelectedItem = "3.1.0b";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "BitTyrant":
                    {
                        cmbVersion.Items.Add("1.1");
                        cmbVersion.SelectedItem = "1.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "50";
                        break;
                    }

                case "BitSpirit":
                    {
                        cmbVersion.Items.Add("3.6.0.200");
                        cmbVersion.Items.Add("3.1.0.077");
                        cmbVersion.SelectedItem = "3.6.0.200";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "Deluge":
                    {
                        cmbVersion.Items.Add("1.2.0");
                        cmbVersion.Items.Add("0.5.8.7");
                        cmbVersion.Items.Add("0.5.8.6");
                        cmbVersion.SelectedItem = "1.2.0";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                case "KTorrent":
                    {
                        cmbVersion.Items.Add("2.2.1");
                        cmbVersion.SelectedItem = "2.2.1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "100";
                        break;
                    }

                case "Gnome BT":
                    {
                        cmbVersion.Items.Add("0.0.28-1");
                        cmbVersion.SelectedItem = "0.0.28-1";
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }

                default:
                    {
                        cmbClient.SelectedItem = DefaultClient;
                        if (customPeersNum.Text == "0" || customPeersNum.Text == "") customPeersNum.Text = "200";
                        break;
                    }
            }

            // getCurrentClient(GetClientName());
        }

        private void cmbVersion_SelectedValueChanged(object sender, EventArgs e)
        {
            if (getnew == false)
            {
                getnew = true;
                return;
            }

            if (chkNewValues.Checked)
            {
                SetCustomValues();
            }
        }

        #endregion
        #region Get(open) torrent
        internal void loadTorrentFileInfo(string torrentFilePath)
        {
            try
            {
                torrentManager.CreateTorrentFile(torrentFilePath);
                torrentFile.Text = torrentFilePath;
                trackerAddress.Text = torrentManager.File.Announce;
                shaHash.Text = torrentManager.File.InfoHash;
                txtTorrentSize.Text = FormatFileSize(torrentManager.File.TotalSize);
            }
            catch (Exception ex)
            {
                AddLogLine(ex.ToString());
            }
        }

        private void UpdateTorrentInfo(TorrentInfo torrent)
        {
            torrent.UploadRate = (Int64)(uploadRate.Text.ParseValidFloat(50) * 1024);
            torrent.DownloadRate = (Int64)(downloadRate.Text.ParseValidFloat(10) * 1024);

            torrent.Interval = interval.Text.ParseValidInt(torrent.Interval);
            interval.Text = torrent.Interval.ToString();
            double finishedPercent = fileSize.Text.ParseDouble(0);
            if (finishedPercent < 0 || finishedPercent > 100)
            {
                AddLogLine("Finished value is invalid: " + fileSize.Text + ", assuming 0 as default value");
                finishedPercent = 0;
            }

            if (finishedPercent >= 100)
            {
                seedMode = true;
                finishedPercent = 100;
            }

            fileSize.Text = finishedPercent.ToString();
            long size = torrentManager.File.TotalSize;
            if (torrentManager.File != null)
            {
                if (finishedPercent == 0)
                {
                    torrent.Totalsize = torrentManager.File.TotalSize;
                }
                else if (finishedPercent == 100)
                {
                    torrent.Totalsize = 0;
                }
                else
                {
                    torrent.Totalsize = (long)((torrentManager.File.TotalSize * (100 - finishedPercent)) / 100);
                }
            }
            else
            {
                torrent.Totalsize = 0;
            }

            torrent.Left = torrent.Totalsize;
            torrent.Filename = torrentFile.Text;

            // deploy custom values
            torrent.Port = customPort.Text.ParseValidInt(torrent.Port);
            customPort.Text = torrent.Port.ToString();
            torrent.Key = customKey.Text.GetValueDefault(torrentManager.Client.Key);
            torrent.NumberOfPeers = customPeersNum.Text.GetValueDefault(torrent.NumberOfPeers);
            torrentManager.Client.Key = customKey.Text.GetValueDefault(torrentManager.Client.Key);
            torrent.PeerID = customPeerID.Text.GetValueDefault(torrentManager.Client.PeerID);
            torrentManager.Client.PeerID = customPeerID.Text.GetValueDefault(torrentManager.Client.PeerID);

            // Add log info
            AddLogLine("TORRENT INFO:");
            AddLogLine("Torrent name: " + torrentManager.File.Name);
            AddLogLine("Tracker address: " + torrent.Tracker);
            AddLogLine("Hash code: " + torrent.Hash);
            AddLogLine("Upload rate: " + torrent.UploadRate / 1024);
            AddLogLine("Download rate: " + torrent.DownloadRate / 1024);
            AddLogLine("Update interval: " + torrent.Interval);
            AddLogLine("Size: " + size / 1024);
            AddLogLine("Left: " + torrent.Totalsize / 1024);
            AddLogLine("Finished: " + finishedPercent);
            AddLogLine("Filename: " + torrent.Filename);
            AddLogLine("Number of peers: " + torrent.NumberOfPeers);
            AddLogLine("Port: " + torrent.Port);
            AddLogLine("Key: " + torrent.Key);
            AddLogLine("PeerID: " + torrent.PeerID + "\n" + "\n");
        }

        internal void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                if (openFileDialog1.FileName == "") return;
                loadTorrentFileInfo(openFileDialog1.FileName);
                FileInfo file = new FileInfo(openFileDialog1.FileName);
                DefaultDirectory = file.DirectoryName;
            }
            catch { return; }
        }

        #endregion
        #region Buttons
        internal void closeButton_Click(object sender, EventArgs e)
        {
            ExitRatioMaster();
        }

        internal void StartButton_Click(object sender, EventArgs e)
        {
            if (!StartButton.Enabled) return;
            Seeders = -1;
            Leechers = -1;
            if (trackerAddress.Text == "" || shaHash.Text == "" || txtTorrentSize.Text == "")
            {
                MessageBox.Show("Please select valid torrent file!", "RatioMaster.NET " + version + " - ERROR");
                return;
            }

            // Check rem work
            if ((string)cmbStopAfter.SelectedItem == "After time:")
            {
                int res;
                bool bCheck = int.TryParse(txtStopValue.Text, out res);
                if (bCheck == false)
                {
                    MessageBox.Show("Please select valid number for Remaning Work\n\r- 0 - default - never stop\n\r- positive number (greater than 1000)", "RatioMaster.NET " + version + " - ERROR");
                    return;
                }
                else
                {
                    if (res < 1000 && res != 0)
                    {
                        MessageBox.Show("Please select valid number for Remaning Work\n\r- 0 - default - never stop\n\r- positive number (greater than 1000)", "RatioMaster.NET " + version + " - ERROR");
                        return;
                    }
                }
            }

            updateScrapStats(-1, -1);
            totalRunningTimeCounter = 0;
            timerValue.Text = "updating...";

            // Initialization
            Encoding _usedEnc = Encoding.GetEncoding(0x4e4);
            networkManager = new NetworkManager(comboProxyType.SelectedIndex, textProxyHost.Text, textProxyPort.Text.ParseValidInt(0), _usedEnc.GetBytes(textProxyUser.Text), _usedEnc.GetBytes(textProxyPass.Text));
            torrentManager.CreateTorrentClient(GetClientName());

            // txtStopValue.Text = res.ToString();
            updateProcessStarted = true;
            seedMode = false;
            requestScrap = checkRequestScrap.Checked;
            StartButton.Enabled = false;
            StartButton.BackColor = SystemColors.Control;
            StopButton.Enabled = true;
            StopButton.BackColor = Color.Silver;
            manualUpdateButton.Enabled = true;
            manualUpdateButton.BackColor = Color.Silver;
            btnDefault.Enabled = false;
            interval.ReadOnly = true;
            fileSize.ReadOnly = true;
            cmbClient.Enabled = false;
            cmbVersion.Enabled = false;
            trackerAddress.ReadOnly = true;
            browseButton.Enabled = false;
            txtStopValue.Enabled = false;
            cmbStopAfter.Enabled = false;
            customPeersNum.Enabled = false;
            customPort.Enabled = false;
            UpdateTorrentInfo(torrentManager.Info);
            AddClientInfo();
            OpenTcpListener();
            Thread myThread = new Thread(startProcess);
            myThread.Name = "startProcess() Thread";
            myThread.Start();
            serverUpdateTimer.Start();
            remWork = 0;
            if ((string)cmbStopAfter.SelectedItem == "After time:") RemaningWork.Start();
            requestScrapeFromTracker();
        }

        private void stopTimerAndCounters()
        {
            if (StartButton.InvokeRequired)
            {
                stopTimerAndCountersCallback callback1 = stopTimerAndCounters;
                Invoke(callback1, new object[0]);
            }
            else
            {
                Seeders = -1;
                Leechers = -1;
                totalRunningTimeCounter = 0;
                lblTotalTime.Text = "00:00";
                if (StartButton.Enabled) return;
                StartButton.Enabled = true;
                StopButton.Enabled = false;
                manualUpdateButton.Enabled = false;
                StartButton.BackColor = Color.Silver;
                StopButton.BackColor = SystemColors.Control;
                manualUpdateButton.BackColor = SystemColors.Control;
                btnDefault.Enabled = true;
                interval.ReadOnly = false;
                fileSize.ReadOnly = false;
                cmbClient.Enabled = true;
                cmbVersion.Enabled = true;
                trackerAddress.ReadOnly = false;
                browseButton.Enabled = true;
                txtStopValue.Enabled = true;
                cmbStopAfter.Enabled = true;
                customPeersNum.Enabled = true;
                customPort.Enabled = true;
                serverUpdateTimer.Stop();
                temporaryIntervalCounter = 0;
                timerValue.Text = "stopped";
                torrentManager.Info.NumberOfPeers = "0";
                updateProcessStarted = false;
                RemaningWork.Stop();
                remWork = 0;
                networkManager.Close();
            }
        }

        internal void StopButton_Click(object sender, EventArgs e)
        {
            if (!StopButton.Enabled) return;
            stopTimerAndCounters();
            Thread thread1 = new Thread(stopProcess);
            thread1.Name = "stopProcess() Thread";
            thread1.Start();
        }

        internal void manualUpdateButton_Click(object sender, EventArgs e)
        {
            if (!manualUpdateButton.Enabled) return;
            if (updateProcessStarted)
            {
                OpenTcpListener();
                temporaryIntervalCounter = torrentManager.Info.Interval;
            }
        }

        internal void browseButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = DefaultDirectory;
            openFileDialog1.ShowDialog();
        }

        internal void clearLogButton_Click(object sender, EventArgs e)
        {
            ClearLog();
        }

        internal void btnDefault_Click(object sender, EventArgs e)
        {
            getnew = false;
            cmbClient.SelectedItem = DefaultClient;
            cmbVersion.SelectedItem = DefaultClientVersion;

            // custom
            chkNewValues.Checked = true;
            SetCustomValues();
            customPort.Text = "";
            customPeersNum.Text = "";

            // proxy
            comboProxyType.SelectedItem = "None";
            textProxyHost.Text = "";
            textProxyPass.Text = "";
            textProxyPort.Text = "";
            textProxyUser.Text = "";

            // check
            checkRequestScrap.Checked = true;
            checkTCPListen.Checked = true;

            // Options
            TorrentInfo torrent = new TorrentInfo();
            int defup = (int)(torrent.UploadRate / 1024);
            int defd = (int)(torrent.DownloadRate / 1024);
            uploadRate.Text = defup.ToString();
            downloadRate.Text = defd.ToString();
            fileSize.Text = "0";
            interval.Text = torrent.Interval.ToString();

            // Log
            checkLogEnabled.Checked = true;

            // Random speeds
            chkRandUP.Checked = true;
            chkRandDown.Checked = true;
            txtRandUpMin.Text = "1";
            txtRandUpMax.Text = "10";
            txtRandDownMin.Text = "1";
            txtRandDownMax.Text = "10";

            // Random in next update
            checkRandomDownload.Checked = false;
            checkRandomUpload.Checked = false;
            RandomUploadFrom.Text = "10";
            RandomUploadTo.Text = "50";
            RandomDownloadFrom.Text = "10";
            RandomDownloadTo.Text = "100";

            // Other
            txtStopValue.Text = "0";
        }

        internal void btnSaveLog_Click(object sender, EventArgs e)
        {
            SaveLog.ShowDialog();
        }

        #endregion
        #region Send Event To Tracker
        private delegate void stopTimerAndCountersCallback();

        delegate void SetIntervalCallback(string param);

        internal void updateInterval(string param)
        {
            if (interval.InvokeRequired)
            {
                SetIntervalCallback del = updateInterval;
                Invoke(del, new object[] { param });
            }
            else
            {
                if (updateProcessStarted)
                {
                    int temp;
                    bool bParse = int.TryParse(param, out temp);
                    if (bParse)
                    {
                        if (temp > 3600) temp = 3600;
                        if (temp < 60) temp = 60;
                        torrentManager.Info.Interval = temp;
                        AddLogLine("Updating Interval: " + temp);
                        interval.ReadOnly = false;
                        interval.Text = temp.ToString();
                        interval.ReadOnly = true;
                    }
                }
            }
        }

        #endregion

        #region Scrape
        private void requestScrapeFromTracker()
        {
            Seeders = -1;
            Leechers = -1;
            if (checkRequestScrap.Checked && !scrapStatsUpdated)
            {
                torrentManager.RequestScrape(networkManager);
                if (torrentManager.Leechers == 0 && noLeechers.Checked)
                {
                    AddLogLine("Min number of leechers reached... setting upload speed to 0");
                    updateTextBox(uploadRate, "0");
                    chkRandUP.Checked = false;
                }
            }
        }

        internal string getScrapeUrlString(TorrentInfo torrentInfo)
        {
            UriBuilder url = new UriBuilder(torrentInfo.Tracker);
            if (url.Uri.Segments.Last() != "announce")
            {
                return string.Empty;
            }

            string hash = HashUrlEncode(torrentInfo.Hash, torrentManager.Client.HashUpperCase);

            url.Path = url.Path.Replace("announce", "scrape");
            if (url.Query?.Length > 1)
            {
                url.Query = url.Query.Substring(1) + "&" + $"info_hash={hash}";
            }
            else
            {
                url.Query = $"info_hash={hash}";
            }
            return url.ToString();
        }

        #endregion
        #region Update Counters
        delegate void SetCountersCallback(TorrentInfo torrentInfo);

        // TODO : Check torrentInfo
        private void updateCounters(TorrentInfo torrentInfo)
        {
            try
            {
                // Random random = new Random();
                // modify Upload Rate
                uploadCount.Text = FormatFileSize(torrentInfo.Uploaded);
                Int64 uploadedR = torrentInfo.UploadRate + RandomSP(txtRandUpMin.Text, txtRandUpMax.Text, chkRandUP.Checked);

                // Int64 uploadedR = torrentInfo.uploadRate + (Int64)random.Next(10 * 1024) - 5 * 1024;
                if (uploadedR < 0) { uploadedR = 0; }
                torrentInfo.Uploaded += uploadedR;

                // modify Download Rate
                downloadCount.Text = FormatFileSize(torrentInfo.Downloaded);
                if (!seedMode && torrentInfo.DownloadRate > 0)    // dont update download stats
                {
                    Int64 downloadedR = torrentInfo.DownloadRate + RandomSP(txtRandDownMin.Text, txtRandDownMax.Text, chkRandDown.Checked);

                    // Int64 downloadedR = torrentInfo.downloadRate + (Int64)random.Next(10 * 1024) - 5 * 1024;
                    if (downloadedR < 0) { downloadedR = 0; }
                    torrentInfo.Downloaded += downloadedR;
                    torrentInfo.Left = torrentInfo.Totalsize - torrentInfo.Downloaded;
                }

                if (torrentInfo.Left <= 0) // either seedMode or start seed mode
                {
                    torrentInfo.Downloaded = torrentInfo.Totalsize;
                    torrentInfo.Left = 0;
                    torrentInfo.DownloadRate = 0;
                    if (!seedMode)
                    {
                        seedMode = true;
                        temporaryIntervalCounter = 0;
                        Thread myThread = new Thread(completedProcess);
                        myThread.Name = "completedProcess() Thread";
                        myThread.Start();
                    }
                }

                torrentInfo.Interval = int.Parse(interval.Text);
                double finishedPercent;
                if (torrentInfo.Totalsize == 0)
                {
                    fileSize.Text = "100";
                }
                else
                {
                    // finishedPercent = (((((float)torrentManager.File.totalLength - (float)torrentInfo.totalsize) + (float)torrentInfo.downloaded) / (float)torrentManager.File.totalLength) * 100);
                    finishedPercent = (((torrentManager.File.TotalSize - (float)torrentInfo.Left)) / ((float)torrentManager.File.TotalSize)) * 100.0;
                    fileSize.Text = (finishedPercent >= 100) ? "100" : SetPrecision(finishedPercent.ToString(), 2);
                }

                downloadCount.Text = FormatFileSize(torrentInfo.Downloaded);

                // modify Ratio Lable
                if (torrentInfo.Downloaded / 1024 < 100)
                {
                    lblTorrentRatio.Text = "NaN";
                }
                else
                {
                    float data = torrentInfo.Uploaded / (float)torrentInfo.Downloaded;
                    lblTorrentRatio.Text = SetPrecision(data.ToString(), 2);
                }
            }
            catch (Exception e)
            {
                AddLogLine(e.Message);
                SetCountersCallback d = updateCounters;
                Invoke(d, new object[] { torrentInfo });
            }
        }

        private static string SetPrecision(string data, int prec)
        {
            float pow = (float)Math.Pow(10, prec);
            float wdata = float.Parse(data);
            wdata = wdata * pow;
            int curr = (int)wdata;
            wdata = curr / pow;
            return wdata.ToString();
        }

        int Seeders = -1;
        int Leechers = -1;

        internal void updateScrapStats(int seed, int leech)
        {
            seedLabel.Text = "Seeders: " + seed;
            leechLabel.Text = "Leechers: " + leech;
            scrapStatsUpdated = true;
        }

        internal void StopModule()
        {
            try
            {
                if ((string)cmbStopAfter.SelectedItem == "When seeders <")
                {
                    if (Seeders > -1 && Seeders < int.Parse(txtStopValue.Text)) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When leechers <")
                {
                    if (Leechers > -1 && Leechers < int.Parse(txtStopValue.Text)) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When uploaded >")
                {
                    if (torrentManager.Info.Uploaded > long.Parse(txtStopValue.Text) * 1024 * 1024) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When downloaded >")
                {
                    if (torrentManager.Info.Downloaded > int.Parse(txtStopValue.Text) * 1024 * 1024) StopButton_Click(null, null);
                }

                if ((string)cmbStopAfter.SelectedItem == "When leechers/seeders <")
                {
                    if ((Leechers / (double)Seeders) < double.Parse(txtStopValue.Text)) StopButton_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                AddLogLine("Error in stopping module!!!: " + ex.Message);
                return;
            }
        }

        internal int totalRunningTimeCounter;

        internal void serverUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (updateProcessStarted)
            {
                if (torrentManager.HasInitialPeers)
                {
                    updateCounters(torrentManager.Info);
                }

                int num1 = torrentManager.Info.Interval - temporaryIntervalCounter;
                totalRunningTimeCounter++;
                lblTotalTime.Text = ConvertToTime(totalRunningTimeCounter);
                StopModule();
                if (num1 > 0)
                {
                    temporaryIntervalCounter++;
                    timerValue.Text = ConvertToTime(num1);
                }
                else
                {
                    randomiseSpeeds();
                    OpenTcpListener();
                    Thread thread1 = new Thread(continueProcess);
                    temporaryIntervalCounter = 0;
                    timerValue.Text = "0";
                    thread1.Name = "continueProcess() Thread";
                    thread1.Start();
                }
            }
        }

        internal void randomiseSpeeds()
        {
            try
            {
                if (checkRandomUpload.Checked)
                {
                    uploadRate.Text = (RandomSP(RandomUploadFrom.Text, RandomUploadTo.Text, true) / 1024).ToString();
                }

                if (checkRandomDownload.Checked)
                {
                    downloadRate.Text = (RandomSP(RandomDownloadFrom.Text, RandomDownloadTo.Text, true) / 1024).ToString();
                }
            }
            catch (Exception exception1)
            {
                AddLogLine("Failed to randomise upload/download speeds: " + exception1.Message);
            }
        }

        internal int RandomSP(string min, string max, bool ret)
        {
            if (ret == false) return rand.Next(10);
            int minn = int.Parse(min);
            int maxx = int.Parse(max);
            int rett = rand.Next(GetMin(minn, maxx), GetMax(minn, maxx)) * 1024;
            return rett;
        }

        internal static int GetMin(int p1, int p2)
        {
            if (p1 < p2) return p1;
            else return p2;
        }

        internal static int GetMax(int p1, int p2)
        {
            if (p1 > p2) return p1;
            else return p2;
        }

        #endregion
        #region Help functions
        private delegate void updateTextBoxCallback(TextBox textbox, string text);

        internal void updateTextBox(TextBox textbox, string text)
        {
            if (textbox.InvokeRequired)
            {
                updateTextBoxCallback callback1 = updateTextBox;
                Invoke(callback1, new object[] { textbox, text });
            }
            else
            {
                textbox.Text = text;
            }
        }

        private delegate void updateLabelCallback(Label textbox, string text);

        private void updateTextBox(Label textbox, string text)
        {
            if (textbox.InvokeRequired)
            {
                updateLabelCallback callback1 = updateTextBox;
                Invoke(callback1, new object[] { textbox, text });
            }
            else
            {
                textbox.Text = text;
            }
        }

        internal static string FormatFileSize(long fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }

            if (fileSize >= 0x40000000)
            {
                return string.Format("{0:########0.00} GB", ((double)fileSize) / 1073741824);
            }

            if (fileSize >= 0x100000)
            {
                return string.Format("{0:####0.00} MB", ((double)fileSize) / 1048576);
            }

            if (fileSize >= 0x400)
            {
                return string.Format("{0:####0.00} KB", ((double)fileSize) / 1024);
            }

            return string.Format("{0} bytes", fileSize);
        }

        internal static string ToHexString(byte[] bytes)
        {
            char[] hexDigits = {
                    '0', '1', '2', '3', '4', '5', '6', '7',
                    '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
                    };

            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }

            return new string(chars);
        }

        internal static string ConvertToTime(int seconds)
        {
            string ret;
            if (seconds < 60 * 60)
            {
                ret = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            }
            else
            {
                ret = (seconds / (60 * 60)).ToString("00") + ":" + ((seconds % (60 * 60)) / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            }

            return ret;
        }

        internal string HashUrlEncode(string decoded, bool upperCase)
        {
            StringBuilder ret = new StringBuilder();
            RandomStringGenerator stringGen = new RandomStringGenerator();
            try
            {
                for (int i = 0; i < decoded.Length; i = i + 2)
                {
                    char tempChar;

                    // the only case in which something should not be escaped, is when it is alphanum,
                    // or it's in marks
                    // in all other cases, encode it.
                    tempChar = (char)Convert.ToUInt16(decoded.Substring(i, 2), 16);
                    ret.Append(tempChar);
                }
            }
            catch (Exception ex)
            {
                AddLogLine(ex.ToString());
            }

            return stringGen.Generate(ret.ToString(), upperCase);
        }

        #endregion

        internal void RemaningWork_Tick(object sender, EventArgs e)
        {
            if (txtStopValue.Text == "0")
            {
                return;
            }
            else
            {
                remWork++;
                int RW = int.Parse(txtStopValue.Text);
                int diff = RW - remWork;
                txtRemTime.Text = ConvertToTime(diff);
                if (remWork >= RW)
                {
                    txtRemTime.Text = "0";
                    RemaningWork.Stop();
                    StopButton_Click(null, null);
                }
            }
        }

        #region Process
        internal void stopProcess()
        {
            torrentManager.SendEventToTracker(networkManager, "&event=stopped", !checkIgnoreFailureReason.Checked);
        }

        internal void completedProcess()
        {
            torrentManager.SendEventToTracker(networkManager, "&event=completed", !checkIgnoreFailureReason.Checked);
            requestScrapeFromTracker();
        }

        internal void continueProcess()
        {
            torrentManager.SendEventToTracker(networkManager, string.Empty, !checkIgnoreFailureReason.Checked);
            requestScrapeFromTracker();
        }

        internal void startProcess()
        {
            if (torrentManager.SendEventToTracker(networkManager, "&event=started", !checkIgnoreFailureReason.Checked))
            {
                updateProcessStarted = true;
                requestScrapeFromTracker();
            }
        }

        #endregion
        #region Change Speeds
        internal void uploadRate_TextChanged(object sender, EventArgs e)
        {
            if (uploadRate.Text == "")
            {
                torrentManager.Info.UploadRate = 0;
            }
            else
            {
                TorrentInfo torrent = new TorrentInfo();
                torrentManager.Info.UploadRate = uploadRate.Text.ParseValidInt64(torrent.UploadRate / 1024) * 1024;
            }

            AddLogLine("Upload rate changed to " + (torrentManager.Info.UploadRate / 1024));
        }

        internal void downloadRate_TextChanged(object sender, EventArgs e)
        {
            if (torrentManager?.Info == null)
            {
                return;
            }
            if (downloadRate.Text == "")
            {
                torrentManager.Info.DownloadRate = 0;
            }
            else
            {
                TorrentInfo torrent = new TorrentInfo();
                torrentManager.Info.DownloadRate = downloadRate.Text.ParseValidInt64(torrent.DownloadRate / 1024) * 1024;
            }

            AddLogLine("Download rate changed to " + (torrentManager.Info.DownloadRate / 1024));
        }

        #endregion
        #region Settings
        internal void ReadSettings()
        {
            try
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("Software\\RatioMaster.NET", true);

                // TorrentInfo torrent = new TorrentInfo(0, 0);
                if (reg == null)
                {
                    // The key doesn't exist; create it / open it
                    Registry.CurrentUser.CreateSubKey("Software\\RatioMaster.NET");
                    return;
                }

                string Version = (string)reg.GetValue("Version", "none");
                if (Version == "none")
                {
                    btnDefault_Click(null, null);
                    return;
                }

                chkNewValues.Checked = ItoB((int)reg.GetValue("NewValues", true));
                getnew = false;
                cmbClient.SelectedItem = reg.GetValue("Client", DefaultClient);
                getnew = false;
                cmbVersion.SelectedItem = reg.GetValue("ClientVersion", DefaultClientVersion);
                uploadRate.Text = ((string)reg.GetValue("UploadRate", uploadRate.Text));
                downloadRate.Text = ((string)reg.GetValue("DownloadRate", downloadRate.Text));
                fileSize.Text = (string)reg.GetValue("fileSize", "0");

                // fileSize.Text = "0";
                interval.Text = (reg.GetValue("Interval", interval.Text)).ToString();
                DefaultDirectory = (string)reg.GetValue("Directory", DefaultDirectory);
                checkTCPListen.Checked = ItoB((int)reg.GetValue("TCPlistener", BtoI(checkTCPListen.Checked)));
                checkRequestScrap.Checked = ItoB((int)reg.GetValue("ScrapeInfo", BtoI(checkRequestScrap.Checked)));
                checkLogEnabled.Checked = ItoB((int)reg.GetValue("EnableLog", BtoI(checkLogEnabled.Checked)));

                // Radnom value
                chkRandUP.Checked = ItoB((int)reg.GetValue("GetRandUp", BtoI(chkRandUP.Checked)));
                chkRandDown.Checked = ItoB((int)reg.GetValue("GetRandDown", BtoI(chkRandDown.Checked)));
                txtRandUpMin.Text = (string)reg.GetValue("MinRandUp", txtRandUpMin.Text);
                txtRandUpMax.Text = (string)reg.GetValue("MaxRandUp", txtRandUpMax.Text);
                txtRandDownMin.Text = (string)reg.GetValue("MinRandDown", txtRandDownMin.Text);
                txtRandDownMax.Text = (string)reg.GetValue("MaxRandDown", txtRandDownMax.Text);

                // Custom values
                if (chkNewValues.Checked == false)
                {
                    customKey.Text = (string)reg.GetValue("CustomKey", customKey.Text);
                    customPeerID.Text = (string)reg.GetValue("CustomPeerID", customPeerID.Text);
                    lblGenStatus.Text = "Generation status: " + "using last saved values";
                }
                else
                {
                    SetCustomValues();
                }

                customPort.Text = (string)reg.GetValue("CustomPort", customPort.Text);
                customPeersNum.Text = (string)reg.GetValue("CustomPeers", customPeersNum.Text);

                // Radnom value on next
                checkRandomUpload.Checked = ItoB((int)reg.GetValue("GetRandUpNext", BtoI(checkRandomUpload.Checked)));
                checkRandomDownload.Checked = ItoB((int)reg.GetValue("GetRandDownNext", BtoI(checkRandomDownload.Checked)));
                RandomUploadFrom.Text = (string)reg.GetValue("MinRandUpNext", RandomUploadFrom.Text);
                RandomUploadTo.Text = (string)reg.GetValue("MaxRandUpNext", RandomUploadTo.Text);
                RandomDownloadFrom.Text = (string)reg.GetValue("MinRandDownNext", RandomDownloadFrom.Text);
                RandomDownloadTo.Text = (string)reg.GetValue("MaxRandDownNext", RandomDownloadTo.Text);

                // Stop after...
                cmbStopAfter.SelectedItem = reg.GetValue("StopWhen", "Never");
                txtStopValue.Text = (string)reg.GetValue("StopAfter", txtStopValue.Text);

                // Proxy
                comboProxyType.SelectedItem = reg.GetValue("ProxyType", comboProxyType.SelectedItem);
                textProxyHost.Text = (string)reg.GetValue("ProxyAdress", textProxyHost.Text);
                textProxyUser.Text = (string)reg.GetValue("ProxyUser", textProxyUser.Text);
                textProxyPass.Text = (string)reg.GetValue("ProxyPass", textProxyPass.Text);
                textProxyPort.Text = (string)reg.GetValue("ProxyPort", textProxyPort.Text);
                checkIgnoreFailureReason.Checked = ItoB((int)reg.GetValue("IgnoreFailureReason", BtoI(checkIgnoreFailureReason.Checked)));
            }
            catch (Exception e)
            {
                AddLogLine("Error in ReadSettings(): " + e.Message);
            }
        }

        internal static int BtoI(bool b)
        {
            return b ? 1 : 0;
        }

        internal static bool ItoB(int param)
        {
            return param == 0 ? false : true;
        }

        #endregion
        #region Custom values
        internal void GetRandCustVal()
        {
            //string clientname = GetClientName();
            //currentClient = TorrentClientFactory.GetClient(clientname);
            //customKey.Text = currentClient.Key;
            //customPeerID.Text = currentClient.PeerID;
            //torrentManager.Info.Port = rand.Next(1025, 65535);
            //customPort.Text = torrentManager.Info.Port.ToString();
            //torrentManager.Info.NumberOfPeers = currentClient.DefNumWant.ToString();
            //customPeersNum.Text = torrentManager.Info.NumberOfPeers;
            //lblGenStatus.Text = "Generation status: " + "generated new values for " + clientname;
        }

        internal void SetCustomValues()
        {
            //string clientname = GetClientName();
            //currentClient = TorrentClientFactory.GetClient(clientname);
            //AddLogLine("Client changed: " + clientname);
            //if (!currentClient.Parse) GetRandCustVal();
            //else
            //{
            //    string searchstring = currentClient.SearchString;
            //    long maxoffset = currentClient.MaxOffset;
            //    long startoffset = currentClient.StartOffset;
            //    string process = currentClient.ProcessName;
            //    string pversion = cmbVersion.SelectedItem.ToString();
            //    if (GETDATA(process, pversion, searchstring, startoffset, maxoffset))
            //    {
            //        customKey.Text = currentClient.Key;
            //        customPeerID.Text = currentClient.PeerID;
            //        customPort.Text = torrentManager.Info.Port.ToString();
            //        customPeersNum.Text = torrentManager.Info.NumberOfPeers;
            //        lblGenStatus.Text = "Generation status: " + clientname + " found! Parsed all values!";
            //    }
            //    else
            //    {
            //        GetRandCustVal();
            //    }
            //}
        }

        internal bool GETDATA(string client, string pversion, string SearchString, long startoffset, long maxoffset)
        {
            try
            {
                ProcessMemoryReader pReader;
                long absoluteEndOffset = maxoffset;
                long absoluteStartOffset = startoffset;
                string clientSearchString = SearchString;
                uint bufferSize = 0x10000;
                string currentClientProcessName = client.ToLower();
                long currentOffset;
                Encoding enc = Encoding.ASCII;
                Process process1 = FindProcessByName(currentClientProcessName);
                if (process1 == null)
                {
                    return false;
                }

                currentOffset = absoluteStartOffset;
                pReader = new ProcessMemoryReader();
                pReader.ReadProcess = process1;
                bool flag1 = false;

                // AddLogLine("Debug: before pReader.OpenProcess();");
                pReader.OpenProcess();

                // AddLogLine("Debug: pReader.OpenProcess();");
                while (currentOffset < absoluteEndOffset)
                {
                    long num2;

                    // AddLogLine("Debug: " + currentOffset.ToString());
                    int num1;
                    byte[] buffer1 = pReader.ReadProcessMemory((IntPtr)currentOffset, bufferSize, out num1);

                    // pReader.saveArrayToFile(buffer1, @"D:\Projects\NRPG Ratio\NRPG RatioMaster MULTIPLE\RatioMaster source\bin\Release\tests\test" + currentOffset.ToString() + ".txt");
                    num2 = getStringOffsetInsideArray(buffer1, enc, clientSearchString);
                    if (num2 >= 0)
                    {
                        flag1 = true;
                        string text1 = enc.GetString(buffer1);
                        Match match1 = new Regex("&peer_id=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            torrentManager.Client.PeerID = match1.Groups[1].ToString();
                            AddLogLine("====> PeerID = " + torrentManager.Client.PeerID);
                        }

                        match1 = new Regex("&key=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            torrentManager.Client.Key = match1.Groups[1].ToString();
                            AddLogLine("====> Key = " + torrentManager.Client.Key);
                        }

                        match1 = new Regex("&port=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            torrentManager.Info.Port = int.Parse(match1.Groups[1].ToString());
                            AddLogLine("====> Port = " + torrentManager.Info.Port);
                        }

                        match1 = new Regex("&numwant=(.+?)(&| )", RegexOptions.Compiled).Match(text1);
                        if (match1.Success)
                        {
                            torrentManager.Info.NumberOfPeers = match1.Groups[1].ToString();
                            AddLogLine("====> NumWant = " + torrentManager.Info.NumberOfPeers);
                            int res;
                            if (!int.TryParse(torrentManager.Info.NumberOfPeers, out res)) torrentManager.Info.NumberOfPeers = torrentManager.Client.DefNumWant.ToString();
                        }

                        num2 += currentOffset;
                        AddLogLine("currentOffset = " + currentOffset);
                        break;
                    }

                    currentOffset += (int)bufferSize;
                }

                pReader.CloseHandle();
                if (flag1)
                {
                    AddLogLine("Search finished successfully!");
                    return true;
                }
                else
                {
                    AddLogLine("Search failed. Make sure that torrent client {" + GetClientName() + "} is running and that at least one torrent is working.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddLogLine("Error when parsing: " + ex.Message);
                return false;
            }
        }

        private Process FindProcessByName(string processName)
        {
            AddLogLine("Looking for " + processName + " process...");
            Process[] processArray1 = Process.GetProcessesByName(processName);
            if (processArray1.Length == 0)
            {
                string text1 = "No " + processName + " process found. Make sure that torrent client is running.";
                AddLogLine(text1);
                return null;
            }

            AddLogLine(processName + " process found! ");
            return processArray1[0];
        }

        private static int getStringOffsetInsideArray(byte[] memory, Encoding enc, string clientSearchString)
        {
            return enc.GetString(memory).IndexOf(clientSearchString);
        }

        #endregion
        private void cmbStopAfter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((string)cmbStopAfter.SelectedItem == "Never")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "";
                txtStopValue.Visible = false;
            }

            if ((string)cmbStopAfter.SelectedItem == "After time:")
            {
                lblStopAfter.Text = "seconds";
                txtStopValue.Text = "3600";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When seeders <")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "10";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When leechers <")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "10";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When uploaded >")
            {
                lblStopAfter.Text = "Mb";
                txtStopValue.Text = "1024";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When downloaded >")
            {
                lblStopAfter.Text = "Mb";
                txtStopValue.Text = "1024";
                txtStopValue.Visible = true;
            }

            if ((string)cmbStopAfter.SelectedItem == "When leechers/seeders <")
            {
                lblStopAfter.Text = "";
                txtStopValue.Text = "1,000";
                txtStopValue.Visible = true;
            }
        }

        #endregion
        public override string ToString()
        {
            return "RatioMaster";
        }
    }
}
