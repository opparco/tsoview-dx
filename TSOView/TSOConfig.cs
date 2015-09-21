using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using SharpDX;

namespace TSOView
{

public class TSOConfig
{
    public Size ClientSize { get; set; }
    SharpDX.Color screen_color;
    public SharpDX.Color ScreenColor { get { return screen_color; } }
#if false
    public string ScreenColorName
    {
        get { return ColorTranslator.ToHtml(screen_color); }
        set { screen_color = ColorTranslator.FromHtml(value); }
    }
#endif
    public Vector3 Position; // of camera
    public float Fov, Znear, Zfar; // of projection

    public TSOConfig()
    {
        this.ClientSize = new Size(800, 600);
        screen_color = SharpDX.Color.LightGray;
        this.Position = new Vector3(0.0f, +10.0f, +44.0f);
        this.Fov = 30.0f;
        this.Znear = 1.0f;
        this.Zfar = 1000.0f;
    }

    public void Dump()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(TSOConfig));
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        XmlWriter writer = XmlWriter.Create(Console.Out, settings);
        serializer.Serialize(writer, this);
        writer.Close();
    }

    public static TSOConfig Load(string source_file)
    {
        XmlReader reader = XmlReader.Create(source_file);
        XmlSerializer serializer = new XmlSerializer(typeof(TSOConfig));
        TSOConfig config = serializer.Deserialize(reader) as TSOConfig;
        reader.Close();
        return config;
    }
}
}
