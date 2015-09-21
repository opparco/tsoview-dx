using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

namespace TDCG
{
    /// <summary>
    /// セーブファイルの内容を保持します。
    /// </summary>
public class PNGSaveFile
{
    /// <summary>
    /// タイプ
    /// </summary>
    public string type = null;
    /// <summary>
    /// 最後に読み込んだライト方向
    /// </summary>
    public Vector3 LightDirection;
    /// <summary>
    /// 最後に読み込んだtmo
    /// </summary>
    public TMOFile Tmo;
    /// <summary>
    /// フィギュアリスト
    /// </summary>
    public List<Figure> figures = new List<Figure>();

    /// カメラ姿勢を更新するときに呼ばれるdelegate型
    public delegate void CameraUpdateHandler(Vector3 translation, Vector3 angle);
    /// カメラ姿勢を更新するときに呼ばれるハンドラ
    public CameraUpdateHandler CameraUpdate;

    /// <summary>
    /// 指定パスからPNGFileを読み込みフィギュアを作成します。
    /// </summary>
    /// <param name="source_file">PNGFileのパス</param>
    public void Load(string source_file)
    {
        if (File.Exists(source_file))
        try
        {
            PNGFile png = new PNGFile();
            Figure fig = null;

            png.Hsav += delegate(string type)
            {
                this.type = type;
                fig = new Figure();
                this.figures.Add(fig);
            };
            png.Pose += delegate(string type)
            {
                this.type = type;
            };
            png.Scne += delegate(string type)
            {
                this.type = type;
            };
            png.Cami += delegate(Stream dest, int extract_length)
            {
                byte[] buf = new byte[extract_length];
                dest.Read(buf, 0, extract_length);

                List<float> factor = new List<float>();
                for (int offset = 0; offset < extract_length; offset += sizeof(float))
                {
                    float flo = BitConverter.ToSingle(buf, offset);
                    factor.Add(flo);
                }
                if (CameraUpdate != null)
                {
                    Vector3 translation = new Vector3(-factor[0], -factor[1], -factor[2]);
                    Vector3 angle = new Vector3(-factor[5], -factor[4], -factor[6]);
                    CameraUpdate(translation, angle);
                }
            };
            png.Lgta += delegate(Stream dest, int extract_length)
            {
                byte[] buf = new byte[extract_length];
                dest.Read(buf, 0, extract_length);

                List<float> factor = new List<float>();
                for (int offset = 0; offset < extract_length; offset += sizeof(float))
                {
                    float flo = BitConverter.ToSingle(buf, offset);
                    factor.Add(flo);
                }

                Matrix m;
                m.M11 = factor[0];
                m.M12 = factor[1];
                m.M13 = factor[2];
                m.M14 = factor[3];

                m.M21 = factor[4];
                m.M22 = factor[5];
                m.M23 = factor[6];
                m.M24 = factor[7];

                m.M31 = factor[8];
                m.M32 = factor[9];
                m.M33 = factor[10];
                m.M34 = factor[11];

                m.M41 = factor[12];
                m.M42 = factor[13];
                m.M43 = factor[14];
                m.M44 = factor[15];

                this.LightDirection = Vector3.TransformCoordinate(new Vector3(0.0f, 0.0f, -1.0f), m);
            };
            png.Ftmo += delegate(Stream dest, int extract_length)
            {
                this.Tmo = new TMOFile();
                this.Tmo.Load(dest);
            };
            png.Figu += delegate(Stream dest, int extract_length)
            {
                fig = new Figure();
                fig.LightDirection = this.LightDirection;
                fig.Tmo = this.Tmo;
                this.figures.Add(fig);

                byte[] buf = new byte[extract_length];
                dest.Read(buf, 0, extract_length);

                List<float> ratios = new List<float>();
                for (int offset = 0; offset < extract_length; offset += sizeof(float))
                {
                    float flo = BitConverter.ToSingle(buf, offset);
                    ratios.Add(flo);
                }
                /*
                ◆FIGU
                スライダの位置。値は float型で 0.0 .. 1.0
                    0: 姉妹
                    1: うで
                    2: あし
                    3: 胴まわり
                    4: おっぱい
                    5: つり目たれ目
                    6: やわらか
                 */
                fig.slider_matrix.TallRatio = ratios[0];
                fig.slider_matrix.ArmRatio = ratios[1];
                fig.slider_matrix.LegRatio = ratios[2];
                fig.slider_matrix.WaistRatio = ratios[3];
                fig.slider_matrix.BustRatio = ratios[4];
                fig.slider_matrix.EyeRatio = ratios[5];

                //fig.TransformTpo();
            };
            png.Ftso += delegate(Stream dest, int extract_length, byte[] opt1)
            {
                TSOFile tso = new TSOFile();
                tso.Load(dest);
                tso.Row = opt1[0];
                fig.TSOFileList.Add(tso);
            };
            Debug.WriteLine("loading " + source_file);
            png.Load(source_file);

            if (this.type == "HSAV")
            {
                BMPSaveData data = new BMPSaveData();

                using (Stream stream = File.OpenRead(source_file))
                    data.Read(stream);

                fig.slider_matrix.TallRatio = data.GetSliderValue(4);
                fig.slider_matrix.ArmRatio = data.GetSliderValue(5);
                fig.slider_matrix.LegRatio = data.GetSliderValue(6);
                fig.slider_matrix.WaistRatio = data.GetSliderValue(7);
                fig.slider_matrix.BustRatio = data.GetSliderValue(0);
                fig.slider_matrix.EyeRatio = data.GetSliderValue(8);

                for (int i = 0; i < fig.TSOFileList.Count; i++)
                {
                    TSOFile tso = fig.TSOFileList[i];
                    string file = data.GetFileName(tso.Row);
                    if (file != "")
                        tso.FileName = Path.GetFileName(file);
                    else
                        tso.FileName = string.Format("{0:X2}", tso.Row);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
}
