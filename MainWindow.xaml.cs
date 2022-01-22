using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace Mario_Santorini_Installer {

    // This is simply an enum that is used for keeping track of different status's for the launcher throughout
    // the running process. It allows the GUI to correctly display the state of the process for starting the game

    enum LauncherStaus {
        READY,
        FAILED,
        DOWNLOADING_GAME,
        DOWNLOADING_UPDATE
    }

    public partial class MainWindow : Window {

        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStaus _status;

        internal LauncherStaus Status {
            get => _status;
            set {
                _status = value;
                switch (_status) {
                    case LauncherStaus.READY:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStaus.FAILED:
                        PlayButton.Content = "Update FAILED - Retry";
                        break;
                    case LauncherStaus.DOWNLOADING_GAME:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStaus.DOWNLOADING_UPDATE:
                        PlayButton.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow() {

            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "Place holder for Game Exectuable");
        }

        private void CheckForUpdates() {

            if (File.Exists(versionFile)) {

                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try {

                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("Version File Link"));

                    if (onlineVersion.IsDifferentThan(localVersion)) 
                        InstallGameFiles(true, onlineVersion);                  
                    else
                        Status = LauncherStaus.READY;
                }
                catch (Exception ex) {

                    Status = LauncherStaus.FAILED;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
                InstallGameFiles(false, Version.zero);
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion) {

            try {

                WebClient webClient = new WebClient();

                if (_isUpdate)
                    Status = LauncherStaus.DOWNLOADING_UPDATE;
                else {

                    Status = LauncherStaus.DOWNLOADING_GAME;
                    _onlineVersion = new Version(webClient.DownloadString("Version File Link"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("Game Zip Link"), gameZip, _onlineVersion);
            }
            catch (Exception ex) {

                Status = LauncherStaus.FAILED;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e) {

            try {

                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStaus.READY;
            }
            catch (Exception ex) {

                Status = LauncherStaus.FAILED;
                MessageBox.Show($"Error installing game files: {ex}");
            } 
        }

        private void Window_ContentRendered(object sender, EventArgs e) {

            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) {

            if (File.Exists(gameExe) && Status == LauncherStaus.READY) {

                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStaus.FAILED) {

                CheckForUpdates();
            }
        }
    } 

    struct Version {

        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor) {

            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version (string _version) {

            string[] _versionStrings = _version.Split('.');

            if (_versionStrings.Length != 3) {

                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion) {

            if (major != _otherVersion.major)
                return true;
            else
            {
                if (minor != _otherVersion.minor)
                    return true;
                else
                    if (subMinor != _otherVersion.subMinor)
                        return true;
            }
            return false;
        }

        public override string ToString() {

            return $"{major}.{minor}.{subMinor}";
        }
    }
}
