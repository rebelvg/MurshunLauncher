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
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Serialization;

namespace OperationsUploader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            password_textBox.Text = OperationsUploader.Properties.Settings.Default.password;

            label2.Text = "Version " + version;
        }

        string version = "1.0.9";

        static byte[] TempArrayHex(int bytecount, byte[] importarray, int offsetinarray)
        {
            byte[] tempbytearr = new byte[bytecount];

            for (int i = 0; i <= bytecount - 1; i++)
            {
                tempbytearr[i] = importarray[offsetinarray];
                offsetinarray += 1;
            }

            return tempbytearr;
        }

        static int ParseOffset(byte[] input, int adr)
        {
            string first = input[adr + 3].ToString("X").ToString();
            string second = input[adr + 2].ToString("X").ToString();
            string third = input[adr + 1].ToString("X").ToString();
            string forth = input[adr].ToString("X").ToString();

            int firstint = Convert.ToInt32(input[adr + 3]);
            int secondint = Convert.ToInt32(input[adr + 2]);
            int thirdint = Convert.ToInt32(input[adr + 1]);
            int forthint = Convert.ToInt32(input[adr]);

            string resultparse = first;

            if (secondint < 16) { resultparse = resultparse + 0; resultparse = resultparse + second; } else { resultparse = resultparse + second; }
            if (thirdint < 16) { resultparse = resultparse + 0; resultparse = resultparse + third; } else { resultparse = resultparse + third; }
            if (forthint < 16) { resultparse = resultparse + 0; resultparse = resultparse + forth; } else { resultparse = resultparse + forth; }


            int resultint = int.Parse(resultparse, System.Globalization.NumberStyles.HexNumber);

            return resultint;
        }

        static byte[] ReturnToSender(int dec)
        {
            string Hex = dec.ToString("X");
            string Hex4 = null;

            if (Hex.Length % 2 == 1)
            {
                Hex4 = Hex4 + "0";
                Hex4 = Hex4 + Hex;
                Hex = null;
                Hex = Hex4;
            }

            byte[] HexArr2 = new byte[4];
            byte[] HexArr = new byte[4];
            int m = 0;

            if (Hex.Length < 8)
            {
                m = (8 - Hex.Length) / 2;
            }

            for (int i = 0; i < Hex.Length; i = i + 2)
            {
                HexArr[m] = Convert.ToByte(Hex.Substring(i, 2), 16);
                m += 1;
            }

            for (int i = 0; i < 4; i++)
                HexArr2[i] = HexArr[3 - i];

            return HexArr2;
        }

        public void AppendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public dynamic GetFileAsString(string fileName)
        {
            string file = null;

            for (int i = 0; i < FileList.Count; i++)
            {
                if (FileList[i].Filename == fileName)
                {
                    file = Encoding.UTF8.GetString(TempArrayHex(FileList[i].Filesize, archive, FileList[i].Fileposition));
                    break;
                }
            }

            if (file == null)
                MessageBox.Show(fileName + " not found.");

            return file;
        }

        public void InsertString(string Filename, string FileString)
        {
            int filetoreplaceid = 0;
            for (int i = 0; i < FileList.Count; i++) if (FileList[i].Filename == Filename) filetoreplaceid = i;

            int footersize = archive.Length - FileList[filetoreplaceid].Fileposition - FileList[filetoreplaceid].Filesize;
            FileList[filetoreplaceid].Filesize = Encoding.UTF8.GetBytes(FileString).Length;
            byte[] outputfilebytes = new byte[FileList[filetoreplaceid].Fileposition + FileList[filetoreplaceid].Filesize + footersize];
            for (int i = 0; i < 4; i++) archive[FileList[filetoreplaceid].Descpos + i] = ReturnToSender(FileList[filetoreplaceid].Filesize)[i];

            System.Buffer.BlockCopy(archive, 0, outputfilebytes, 0, FileList[filetoreplaceid].Fileposition);
            System.Buffer.BlockCopy(Encoding.UTF8.GetBytes(FileString), 0, outputfilebytes, FileList[filetoreplaceid].Fileposition, FileList[filetoreplaceid].Filesize);
            System.Buffer.BlockCopy(archive, archive.Length - footersize, outputfilebytes, FileList[filetoreplaceid].Fileposition + FileList[filetoreplaceid].Filesize, footersize);
            archive = new byte[outputfilebytes.Length];
            System.Buffer.BlockCopy(outputfilebytes, 0, archive, 0, outputfilebytes.Length);
        }

        public bool IsBinarizedMissionSQM()
        {
            int? missionsqmid = null;

            for (int i = 0; i < FileList.Count; i++)
            {
                if (FileList[i].Filename == "mission.sqm")
                {
                    missionsqmid = i;
                }
            }

            if (missionsqmid != null)
            {
                if (TempArrayHex(6, archive, FileList[missionsqmid.Value].Fileposition).SequenceEqual(new byte[] { 0x00, 0x72, 0x61, 0x50, 0x00, 0x00 }))
                {
                    MessageBox.Show("Mission is binarized. Disable binarization in the editor.");
                    return true;
                }
            }
            else
            {
                MessageBox.Show("mission.sqm not found.");
                return false;
            }

            return false;
        }

        byte[] archive;
        List<FileArch> FileList = new List<FileArch>();

        public dynamic GetMissionValue(string missionSQM, List<string> classes, string valueName)
        {
            string classLevel = missionSQM;
            int i = 0;

            foreach (string className in classes)
            {
                try
                {
                    string tabs = String.Concat(Enumerable.Repeat("\t", i));
                    classLevel = classLevel.Split(new string[] { "class " + className + "\r\n" + tabs + "{" }, StringSplitOptions.None)[1];
                    classLevel = classLevel.Split(new string[] { "\r\n" + tabs + "};" }, StringSplitOptions.None)[0];
                }
                catch
                {
                    MessageBox.Show(className + " class not found.");
                    return null;
                }

                i++;
            }

            if (classLevel.IndexOf(valueName + "=") == -1)
            {
                return null;
            }

            string value = classLevel.Split(new string[] { valueName + "=\"" }, StringSplitOptions.None)[1];
            value = value.Split(new string[] { "\";" }, StringSplitOptions.None)[0];

            return value;
        }

        public string SetMissionValue(string missionSQM, List<string> classes, string valueName, string value)
        {
            string newMissionSQM = missionSQM;
            string classLevel = missionSQM;
            string newClassLevel;

            int i = 0;

            foreach (string className in classes)
            {
                try
                {
                    string tabs = String.Concat(Enumerable.Repeat("\t", i));
                    classLevel = classLevel.Split(new string[] { "class " + className + "\r\n" + tabs + "{" }, StringSplitOptions.None)[1];
                    classLevel = classLevel.Split(new string[] { "\r\n" + tabs + "};" }, StringSplitOptions.None)[0];
                }
                catch
                {
                    MessageBox.Show(className + " class not found.");
                    return null;
                }

                i++;
            }

            if (classLevel.IndexOf(valueName + "=") == -1)
            {
                string tabs = String.Concat(Enumerable.Repeat("\t", i));
                newClassLevel = classLevel + "\r\n" + tabs + valueName + "=\"" + value + "\";";
                return newMissionSQM.Replace(classLevel, newClassLevel);
            }

            string oldValue = classLevel.Split(new string[] { valueName + "=\"" }, StringSplitOptions.None)[1];
            oldValue = oldValue.Split(new string[] { "\";" }, StringSplitOptions.None)[0];

            newClassLevel = classLevel.Replace(valueName + "=\"" + oldValue + "\";", valueName + "=\"" + value + "\";");

            return newMissionSQM.Replace(classLevel, newClassLevel);
        }

        public string AppendMissionValue(string missionSQM, List<string> classes, string valueName, string value)
        {
            string oldValue = GetMissionValue(missionSQM, classes, valueName);

            string newValue = value;

            if (oldValue != null)
            {
                newValue = oldValue + " " + newValue;
            }

            return SetMissionValue(missionSQM, classes, valueName, newValue);
        }

        public bool EditMission(string path)
        {
            archive = File.ReadAllBytes(path);
            FileList = new List<FileArch> { };

            int fileposition = 0;
            int fileloc = 0;
            int MLdatastart = 0;

            var pattern = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            try
            {
                MLdatastart = archive.Locate(pattern, 0)[0] + 21;
            }
            catch
            {
                MessageBox.Show("Can't read pbo header.");
                return false;
            }

            fileloc = MLdatastart;

            int wordlength = 0;
            while (fileposition < MLdatastart - 21)
            {
                wordlength = 0;

                while (archive[fileposition + wordlength] != 0x00)
                {
                    wordlength++;
                }

                if (wordlength != 0)
                {
                    FileList.Add(new FileArch(Encoding.UTF8.GetString(TempArrayHex(wordlength, archive, fileposition)), ParseOffset(archive, fileposition + wordlength + 17), fileposition + wordlength + 17, fileloc));
                    fileloc += ParseOffset(archive, fileposition + wordlength + 17);
                }

                fileposition += wordlength + 21;
            }

            if (IsBinarizedMissionSQM())
                return false;

            string missionSQM = GetFileAsString("mission.sqm");

            if (missionSQM == null)
            {
                MessageBox.Show("mission.sqm not found.");
                return false;
            }

            string briefingName = GetMissionValue(missionSQM, new List<string> { "Mission", "Intel" }, "briefingName");

            if (briefingName == null)
            {
                MessageBox.Show("briefingName not found.");
                return false;
            }

            string author = GetMissionValue(missionSQM, new List<string> { "ScenarioData" }, "author");

            if (author == null)
            {
                MessageBox.Show("author not found.");
                return false;
            }

            string newOverviewText = "Uploaded at " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " Author - " + author;
            missionSQM = AppendMissionValue(missionSQM, new List<string> { "Mission", "Intel" }, "overviewText", newOverviewText);
            missionSQM = AppendMissionValue(missionSQM, new List<string> { "ScenarioData" }, "overviewText", newOverviewText);

            DialogResult dialogResult = MessageBox.Show("New description:\n" + newOverviewText, "Want to proceed?", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.No)
            {
                return false;
            }

            if (clearDependencies_checkBox.Checked)
            {
                try
                {
                    string addons = missionSQM.Split(new string[] { "addons[]=\r\n{" }, StringSplitOptions.None)[1];
                    addons = addons.Split(new string[] { "};" }, StringSplitOptions.None)[0];
                    missionSQM = missionSQM.Replace("addons[]=\r\n{" + addons + "};", "addons[]={};");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Clearing dependencies failed.\n" + e.Message);
                    return false;
                }
            }

            InsertString("mission.sqm", missionSQM);

            Upload(archive, Path.GetFileNameWithoutExtension(path));

            return true;
        }

        public bool Upload(byte[] mission, string missionName)
        {
            try
            {
                LauncherConfigJson presetFile = ReturnPresetFile();

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("auth", password_textBox.Text);
                    client.Headers.Add("filename", missionName);

                    client.UploadData(new Uri(presetFile.missions_link), mission);
                }

                MessageBox.Show("Upload Done!");
            }
            catch (WebException e)
            {
                dynamic resp = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                dynamic obj = JsonConvert.DeserializeObject(resp);
                MessageBox.Show((string)obj.error[0]);

                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show("Upload error.\n" + e.Message);

                return false;
            }

            return true;
        }

        public class OperationsLauncherXmlSettings
        {
            public string pathToArma3ClientMods_textBox = Directory.GetCurrentDirectory();
        }

        public struct LauncherConfigJson
        {
            public string missions_link;
        }

        public LauncherConfigJson ReturnPresetFile()
        {
            string xmlPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\OperationsLauncher\\OperationsLauncher.xml";

            XmlSerializer serializer = new XmlSerializer(typeof(OperationsLauncherXmlSettings));

            StreamReader reader = new StreamReader(xmlPath);

            OperationsLauncherXmlSettings LauncherSettings = (OperationsLauncherXmlSettings)serializer.Deserialize(reader);

            reader.Close();

            string operationsLauncherFilesPath = LauncherSettings.pathToArma3ClientMods_textBox + "\\OperationsLauncherFiles.json";

            LauncherConfigJson json = JsonConvert.DeserializeObject<LauncherConfigJson>(File.ReadAllText(operationsLauncherFilesPath));

            return json;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OperationsUploader.Properties.Settings.Default.password = password_textBox.Text;
            OperationsUploader.Properties.Settings.Default.Save();

            OpenFileDialog selectFile = new OpenFileDialog();

            selectFile.Title = "Select mission file";
            selectFile.Filter = "PBO File (.pbo) | *.pbo";
            selectFile.RestoreDirectory = true;

            if (selectFile.ShowDialog() == DialogResult.OK)
            {
                if (!directUpload_checkBox.Checked)
                {
                    EditMission(selectFile.FileName);
                }
                else
                {
                    Upload(File.ReadAllBytes(selectFile.FileName), Path.GetFileNameWithoutExtension(selectFile.FileName));
                }
            }
        }
    }
}
