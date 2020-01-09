using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TDCG;

using SharpDX.Direct3D11;
using SharpDX.Windows;

namespace TSOView
{

static class Program
{
    [STAThread]
    static void Main(string[] args) 
    {
        Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

        var supported = Device.GetSupportedFeatureLevel();
        Console.WriteLine("device supported {0}", supported);

        if (supported < SharpDX.Direct3D.FeatureLevel.Level_10_0)
            return;

        TSOConfig tso_config;

        string tso_config_file = Path.Combine(Application.StartupPath, @"config.xml");
        if (File.Exists(tso_config_file))
            tso_config = TSOConfig.Load(tso_config_file);
        else
            tso_config = new TSOConfig();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        TSOForm form = new TSOForm(tso_config, args);

        form.Show();
        using (RenderLoop loop = new RenderLoop(form))
        {
            while (loop.NextFrame())
            {
                form.FrameMove();
            }
        }
    }
}
}
