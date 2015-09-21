using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using SharpDX;

namespace OcuView
{

public class OcuConfig
{
    public Vector3 Position; // of main camera
    public float Angle; // of main camera
    public float Speed; // of main camera
    public float Scale; // of world
    public float Znear, Zfar; // of projection

    public struct SneakSpec
    {
        public float Y;
        public float Speed;
    }
    public SneakSpec Sneak; // of main camera

    public OcuConfig()
    {
        this.Position = new Vector3(0.0f, 15.0f, 10.0f);
        this.Angle = 0.02f;
        this.Speed = 0.5f;
        this.Scale = 1.0f;
        this.Znear = 1.0f;
        this.Zfar = 1000.0f;

        this.Sneak = new SneakSpec()
        {
            Y = 7.5f,
            Speed = 0.25f,
        };
    }

    public void Dump()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(OcuConfig));
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        XmlWriter writer = XmlWriter.Create(Console.Out, settings);
        serializer.Serialize(writer, this);
        writer.Close();
    }

    public static OcuConfig Load(string source_file)
    {
        XmlReader reader = XmlReader.Create(source_file);
        XmlSerializer serializer = new XmlSerializer(typeof(OcuConfig));
        OcuConfig config = serializer.Deserialize(reader) as OcuConfig;
        reader.Close();
        return config;
    }
}
}
