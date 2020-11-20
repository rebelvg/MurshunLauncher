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

namespace OperationsLauncher
{
    public partial class Form1 : Form
    {
        string launcherVersion = "0.4.0";

        List<string> presetModsList = new List<string>();

        OperationsLauncherXmlSettings LauncherSettings;

        string server = "";
        string password = "";

        bool debugMode;
    }
}
