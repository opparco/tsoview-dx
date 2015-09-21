using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
//using OculusWrap;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
//using Buffer = SharpDX.Direct3D11.Buffer;
//using Device = SharpDX.Direct3D11.Device;

namespace OcuView
{

static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string save_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\TechArts3D\TDCG";

        OcuConfig ocu_config;

        string ocu_config_file = Path.Combine(Application.StartupPath, @"ocuconfig.xml");
        if (File.Exists(ocu_config_file))
            ocu_config = OcuConfig.Load(ocu_config_file);
        else
            ocu_config = new OcuConfig();

        RenderForm form = new RenderForm("TSOView for Oculus (SharpDX Direct3D11 net40)");

        OculusViewer viewer = new OculusViewer();
        if (viewer.InitializeApplication(form, ocu_config))
        {
            foreach (string arg in args)
                viewer.figures.LoadAnyFile(arg, true);
            if (viewer.figures.Count == 0)
                viewer.figures.LoadAnyFile(Path.Combine(save_path, @"system.tdcgsav.png"), true);
            viewer.MotionEnabled = true;
            RenderLoop.Run(form, () => { viewer.FrameMove(); viewer.Render(); });
        }

        viewer.Dispose();
    }
}
}
