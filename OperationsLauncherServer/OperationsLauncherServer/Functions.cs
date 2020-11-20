﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System.Security.Cryptography;

class CustomReadStream : Stream
{
    Stream inner;
    int maxBytes;
    int bytesRead = 0;

    public CustomReadStream(Stream inner, int maxBytes)
    {
        this.inner = inner;
        this.maxBytes = maxBytes;
    }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => inner.CanWrite;

    public override long Length => inner.Length;

    public override long Position { get => inner.Position; set => inner.Position = value; }

    public override void Flush()
    {
        inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var result = inner.Read(buffer, offset, count);

        if (this.bytesRead > this.maxBytes)
        {
            return 0;
        }

        this.bytesRead += count;


        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        inner.Write(buffer, offset, count);
    }
}

namespace OperationsLauncherServer
{
    public partial class Form1 : Form
    {
        public class LauncherSettingsJson
        {          
            public string pathToArma3Exe = Directory.GetCurrentDirectory() + "\\arma3server_x64.exe";
            public string pathToArma3Mods = Directory.GetCurrentDirectory();
            public string[] customMods = new string[0];
            public string[] checkedCustomMods = new string[0];
            public string serverConfig = "";
            public string serverCfg = "";
            public string serverProfiles = "";
            public string serverProfileName = "";
            public bool hideWindow = false;
        }

        public void ReadXmlFile()
        {
            try
            {
                var LauncherSettingsJson = JsonConvert.DeserializeObject<LauncherSettingsJson>(File.ReadAllText(xmlPath_textBox.Text));

                pathToArma3_textBox.Text = LauncherSettingsJson.pathToArma3Exe;
                pathToMods_textBox.Text = LauncherSettingsJson.pathToArma3Mods;
                serverConfig_textBox.Text = LauncherSettingsJson.serverConfig;
                serverCfg_textBox.Text = LauncherSettingsJson.serverCfg;
                serverProfiles_textBox.Text = LauncherSettingsJson.serverProfiles;
                serverProfileName_textBox.Text = LauncherSettingsJson.serverProfileName;
                hideWindow_checkBox.Checked = LauncherSettingsJson.hideWindow;

                foreach (string X in LauncherSettingsJson.customMods)
                {
                    if (!customMods_listView.Items.Cast<ListViewItem>().Select(x => x.Text).Contains(X))
                    {
                        customMods_listView.Items.Add(X);
                    }
                }

                foreach (ListViewItem X in customMods_listView.Items)
                {
                    if (LauncherSettingsJson.checkedCustomMods.Contains(X.Text))
                    {
                        X.Checked = true;
                    }
                }
            }
            catch (Exception error)
            {
                DialogResult dialogResult = MessageBox.Show("Create a new one? " + error.Message, "Settings file is corrupted.", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    SaveXmlFile();
                }
                if (dialogResult == DialogResult.No)
                {
                    System.Environment.Exit(1);
                }
            }
        }

        public void SaveXmlFile()
        {
            try
            {
                var LauncherSettingsJson = new LauncherSettingsJson();

                LauncherSettingsJson.pathToArma3Exe = pathToArma3_textBox.Text;
                LauncherSettingsJson.pathToArma3Mods = pathToMods_textBox.Text;
                LauncherSettingsJson.customMods = customMods_listView.Items.Cast<ListViewItem>().Select(x => x.Text).ToArray();
                LauncherSettingsJson.checkedCustomMods = customMods_listView.CheckedItems.Cast<ListViewItem>().Select(x => x.Text).ToArray();
                LauncherSettingsJson.serverConfig = serverConfig_textBox.Text;
                LauncherSettingsJson.serverCfg = serverCfg_textBox.Text;
                LauncherSettingsJson.serverProfiles = serverProfiles_textBox.Text;
                LauncherSettingsJson.serverProfileName = serverProfileName_textBox.Text;
                LauncherSettingsJson.hideWindow = hideWindow_checkBox.Checked;

                string json = JsonConvert.SerializeObject(LauncherSettingsJson, Formatting.Indented);

                File.WriteAllText(xmlPath_textBox.Text, json);
            }
            catch (Exception error)
            {
                MessageBox.Show("Saving settings failed. " + error.Message);
            }
        }

        public struct LauncherConfigJsonFile
        {
            public long size;
            public string date;
            public string md5;
        }

        public struct LauncherConfigJson
        {
            public string server;
            public string password;
            public string verify_link;
            public string missions_link;
            public string[] mods;
            public Dictionary<string, LauncherConfigJsonFile> files;
        }

        public bool ReadPresetFile()
        {
            string operationsLauncherFilesPath = pathToMods_textBox.Text + "\\OperationsLauncherFiles.json";

            presetModsList = new List<string>();

            if (!File.Exists(operationsLauncherFilesPath))
            {
                RefreshPresetModsList(false);

                MessageBox.Show("OperationsLauncherFiles.json not found. Select your BTsync folder as Arma 3 Mods folder.");

                return false;
            }

            LauncherConfigJson json = JsonConvert.DeserializeObject<LauncherConfigJson>(File.ReadAllText(operationsLauncherFilesPath));

            presetModsList = json.mods.ToList();

            RefreshPresetModsList(true);

            return true;
        }

        public dynamic ReturnPresetFile()
        {
            string operationsLauncherFilesPath = pathToMods_textBox.Text + "\\OperationsLauncherFiles.json";

            dynamic json = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(operationsLauncherFilesPath));

            return json;
        }

        public void RefreshPresetModsList(bool btSyncFolderHasSyncFile)
        {
            SetColorOnText(pathToArma3_textBox);
            SetColorOnText(pathToMods_textBox, btSyncFolderHasSyncFile);
            SetColorOnText(serverConfig_textBox);
            SetColorOnText(serverCfg_textBox);
            SetColorOnText(serverProfiles_textBox);

            SetColorOnPresetList(presetMods_listView, pathToMods_textBox.Text);

            SetColorOnCustomList(customMods_listView, columnHeader9);
        }

        public void SetColorOnText(TextBox box)
        {
            if (Directory.Exists(box.Text) || File.Exists(box.Text))
                box.BackColor = Color.Green;
            else
                box.BackColor = Color.Red;
        }

        public void SetColorOnText(TextBox box, bool btSyncFolderHasSyncFile)
        {
            if (btSyncFolderHasSyncFile)
                box.BackColor = Color.Green;
            else
                box.BackColor = Color.Red;
        }

        public void SetColorOnPresetList(ListView list, string path)
        {
            list.Items.Clear();

            foreach (string X in presetModsList)
            {
                list.Items.Add(X);
            }

            foreach (ListViewItem X in list.Items)
            {
                if (Directory.Exists(path + "\\" + X.Text + "\\addons"))
                {
                    if (X.BackColor != Color.Green)
                        X.BackColor = Color.Green;
                }
                else
                {
                    if (X.BackColor != Color.Red)
                        X.BackColor = Color.Red;
                }
            }
        }

        public void SetColorOnCustomList(ListView list, ColumnHeader header)
        {
            foreach (ListViewItem X in list.Items)
            {
                if (Directory.Exists(X.Text + "\\addons"))
                {
                    if (X.BackColor != Color.Green)
                        X.BackColor = Color.Green;
                }
                else
                {
                    if (X.BackColor != Color.Red)
                        X.BackColor = Color.Red;
                }
            }

            header.Width = -2;
        }

        public Int32 GetUnixTime(DateTime date)
        {
            return (Int32)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public async Task<bool> VerifyMods(bool fullVerify)
        {
            if (!ReadPresetFile())
                return false;

            string operationsLauncherFilesPath = pathToMods_textBox.Text + "\\OperationsLauncherFiles.json";

            LauncherConfigJson json = JsonConvert.DeserializeObject<LauncherConfigJson>(File.ReadAllText(operationsLauncherFilesPath));

            if (!await Task.Run(() => CheckLauncherFiles(json.verify_link, GetMD5(operationsLauncherFilesPath, true))))
                return false;

            List<string> folderFiles = Directory.GetFiles(pathToMods_textBox.Text, "*", SearchOption.AllDirectories).ToList();

            folderFiles = folderFiles.Select(a => a.Replace(pathToMods_textBox.Text, "")).Select(b => b.ToLower()).ToList();

            folderFiles = folderFiles.Where(a => presetModsList.Any(b => a.StartsWith("\\" + b.ToLower() + "\\"))).Where(c => c.EndsWith(".pbo") || c.EndsWith(".dll")).ToList();

            modsFiles_listView.Items.Clear();
            launcherFiles_listView.Items.Clear();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = folderFiles.Count();
            progressBar1.Value = 0;
            progressBar1.Step = 1;

            List<string> clientFiles = await Task.Run(() => GetVerifyList(folderFiles, fullVerify));

            foreach (string X in clientFiles)
            {
                modsFiles_listView.Items.Add(X);
            }

            foreach (KeyValuePair<string, LauncherConfigJsonFile> X in json.files)
            {
                long size = X.Value.size;
                string date = X.Value.date;
                string md5 = X.Value.md5;

                if (!fullVerify)
                    launcherFiles_listView.Items.Add(X.Key + ":" + size + ":" + date);
                else
                    launcherFiles_listView.Items.Add(X.Key + ":" + md5);
            }

            folderFiles = modsFiles_listView.Items.Cast<ListViewItem>().Select(x => x.Text).ToList();

            List<string> jsonFiles = launcherFiles_listView.Items.Cast<ListViewItem>().Select(x => x.Text).ToList();

            List<string> missingFilesList = jsonFiles.Where(x => !folderFiles.Contains(x)).ToList();
            List<string> excessFilesList = folderFiles.Where(x => !jsonFiles.Contains(x)).ToList();

            missingFiles_listView.Items.Clear();
            excessFiles_listView.Items.Clear();

            foreach (string X in missingFilesList)
            {
                missingFiles_listView.Items.Add(X);
            }

            foreach (string X in excessFilesList)
            {
                excessFiles_listView.Items.Add(X);
            }

            modsFiles_textBox.Text = pathToMods_textBox.Text + " (" + modsFiles_listView.Items.Count + " files / " + missingFiles_listView.Items.Count + " missing)";
            launcherFiles_textBox.Text = "OperationsLauncherFiles.json (" + launcherFiles_listView.Items.Count + " files / " + excessFiles_listView.Items.Count + " excess)";

            if (missingFiles_listView.Items.Count != 0 || excessFiles_listView.Items.Count != 0)
            {
                if (tabControl1.SelectedTab != tabPage4)
                {
                    MessageBox.Show("You have missing or excess files.");
                }

                return false;
            }

            return true;
        }

        public List<string> GetVerifyList(List<string> folderFiles, bool fullVerify)
        {
            LockInterface("Verifying...");

            List<string> clientFiles = new List<string>();

            var chunkedList = new List<List<string>>();

            for (int i = 0; i < folderFiles.Count; i += 4)
            {
                chunkedList.Add(folderFiles.GetRange(i, Math.Min(4, folderFiles.Count - i)));
            }

            var tasks = new List<Task>();

            foreach (List<string> chunkedFolderFiles in chunkedList)
            {
                var task = Task.Run(() =>
                {
                    foreach (string X in chunkedFolderFiles)
                    {
                        FileInfo file = new FileInfo(pathToMods_textBox.Text + X);

                        ChangeHeader("Verifying... (" + progressBar1.Value + "/" + progressBar1.Maximum + ") - " + file.Name + "/" + file.Length / 1024 / 1024 + "mb");

                        if (!fullVerify)
                            clientFiles.Add(X + ":" + file.Length + ":" + GetUnixTime(file.LastWriteTimeUtc));
                        else
                            clientFiles.Add(X + ":" + GetMD5(pathToMods_textBox.Text + X, false));

                        Invoke(new Action(() => progressBar1.PerformStep()));

                        ChangeHeader("Verifying... (" + progressBar1.Value + "/" + progressBar1.Maximum + ") - " + file.Name + "/" + file.Length / 1024 / 1024 + "mb");
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            UnlockInterface();

            return clientFiles;
        }

        public bool CheckLauncherFiles(string link, string localJsonMD5)
        {
            if (string.IsNullOrEmpty(link))
                return true;

            ChangeHeader("Connecting to the server... " + link);

            WebClient client = new WebClient();

            try
            {
                string modLineString = client.DownloadString(link);

                if (modLineString != localJsonMD5)
                {
                    Invoke(new Action(() => MessageBox.Show("Your OperationsLauncherFiles.json is not up-to-date. Launch BTsync to update.")));

                    ChangeHeader("Verify failed, hashes do not match.");

                    return false;
                }
            }
            catch (Exception error)
            {
                Invoke(new Action(() => MessageBox.Show("Couldn't connect to the server to check the integrity of your files. " + link + "\n\nError: " + error.Message)));

                ChangeHeader("Verify failed " + error.Message);

                return false;
            }

            return true;
        }

        public void CheckSyncFolderSize()
        {
            string archivePath = pathToMods_textBox.Text + "\\.sync\\Archive";

            if (Directory.Exists(archivePath))
            {
                string[] archiveFilesArray = Directory.GetFiles(archivePath, "*", SearchOption.AllDirectories).ToArray();

                long bytes = 0;
                foreach (string name in archiveFilesArray)
                {
                    FileInfo file = new FileInfo(name);
                    bytes += file.Length;
                }

                if ((bytes / 1024 / 1024 / 1024) >= 1)
                {
                    MessageBox.Show("Your BTsync archive folder is too large. It's size is over " + (bytes / 1024 / 1024 / 1024) + " GB. You can clear it and disable archiving in the BTsync client.");
                    System.Diagnostics.Process.Start(archivePath);
                }
            }
        }

        public string GetMD5(string filename, bool getFullHash)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    if (getFullHash)
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    }

                    long fileSize = new FileInfo(filename).Length;

                    var shortStream = new CustomReadStream(stream, Convert.ToInt32(fileSize * 0.1));

                    return BitConverter.ToString(md5.ComputeHash(shortStream)).Replace("-", "").ToLower();
                }
            }
        }

        public void LockInterface(string text)
        {
            Invoke(new Action(() =>
            {
                tabControl1.Enabled = false;
                ChangeHeader(text);
            }));
        }

        public void DownloadMissions()
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        dynamic presetFile = ReturnPresetFile();

                        WebClient client = new WebClient();

                        string response = client.DownloadString((string)presetFile["missions_link"]);

                        List<dynamic> missions = JsonConvert.DeserializeObject<List<dynamic>>(response);

                        foreach (dynamic mission in missions)
                        {
                            string missionPath = Path.GetDirectoryName(pathToArma3_textBox.Text) + "/mpmissions/" + (string)mission["file"];

                            if (!File.Exists(missionPath) || GetMD5(missionPath, true) != (string)mission["hash"])
                            {
                                using (client = new WebClient())
                                {
                                    try
                                    {
                                        client.DownloadFile((string)presetFile["missions_link"] + "/" + (string)mission["file"], missionPath);

                                    }
                                    catch (Exception error)
                                    {
                                        Console.WriteLine(error.Message);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception error) {
                        Console.WriteLine(error.Message);
                    }

                    Thread.Sleep(30000);
                }
            });

            thread.IsBackground = true;

            thread.Start();
        }

        public void UnlockInterface()
        {
            Invoke(new Action(() =>
            {
                tabControl1.Enabled = true;
                ChangeHeader("Operations Launcher Server");
            }));
        }

        public void ChangeHeader(string text)
        {
            Invoke(new Action(() =>
            {
                Text = text;
            }));
        }
    }
}