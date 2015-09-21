using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.Windows.Forms;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace TDCG
{
    /// <summary>
    /// フィギュアを管理します。
    /// </summary>
    public class FigureCollection : IDisposable
    {
        /// <summary>
        /// フィギュアリスト
        /// </summary>
        public List<Figure> FigureList = new List<Figure>();

        /// <summary>
        /// 要素数
        /// </summary>
        public int Count
        {
            get { return FigureList.Count; }
        }

        SimpleCamera camera = null;

        /// <summary>
        /// カメラ
        /// </summary>
        public SimpleCamera Camera
        {
            get
            {
                return camera;
            }
            set
            {
                camera = value;
            }
        }

        /// TSOファイルをdevice上で開くときに呼ばれるdelegate型
        public delegate void TSOFileOpenHandler(TSOFile tso);
        /// TSOファイルをdevice上で開くときに呼ばれるハンドラ
        public TSOFileOpenHandler TSOFileOpen;

        // 選択フィギュアindex
        int fig_index = 0;

        /// <summary>
        /// 選択フィギュアの光源方向を設定します。
        /// </summary>
        /// <param name="dir">選択フィギュアの光源方向</param>
        public void SetLightDirection(Vector3 dir)
        {
            foreach (Figure fig in FigureList)
                fig.LightDirection = dir;
        }

        /// <summary>
        /// 指定モーションフレームに進みます。
        /// </summary>
        public void SetFrameIndex(int frame_index)
        {
            foreach (Figure fig in FigureList)
                fig.SetFrameIndex(frame_index);
        }

        /// <summary>
        /// bone行列を更新します。
        /// </summary>
        public void UpdateBoneMatrices()
        {
            foreach (Figure fig in FigureList)
                fig.UpdateBoneMatrices();
        }

        /// <summary>
        /// bone行列を更新します。
        /// </summary>
        public void UpdateBoneMatrices(bool forced)
        {
            foreach (Figure fig in FigureList)
                fig.UpdateBoneMatrices(forced);
        }

        /// <summary>
        /// tmo file中で最大のフレーム長さを得ます。
        /// </summary>
        /// <returns>フレーム長さ</returns>
        public int GetMaxFrameLength()
        {
            int max = 0;
            foreach (Figure fig in FigureList)
                if (fig.Tmo.frames != null && max < fig.Tmo.frames.Length)
                    max = fig.Tmo.frames.Length;
            return max;
        }

        /// <summary>
        /// 任意のファイルを読み込みます。
        /// </summary>
        /// <param name="source_file">任意のパス</param>
        public void LoadAnyFile(string source_file)
        {
            LoadAnyFile(source_file, false);
        }

        /// <summary>
        /// 任意のファイルを読み込みます。
        /// </summary>
        /// <param name="source_file">任意のパス</param>
        /// <param name="append">FigureListを消去せずに追加するか</param>
        public void LoadAnyFile(string source_file, bool append)
        {
            switch (Path.GetExtension(source_file).ToLower())
            {
                case ".tso":
                    if (!append)
                        ClearFigureList();
                    LoadTSOFile(source_file);
                    break;
                case ".tmo":
                    LoadTMOFile(source_file);
                    break;
                case ".png":
                    AddFigureFromPNGFile(source_file, append);
                    break;
                default:
                    if (!append)
                        ClearFigureList();
                    if (Directory.Exists(source_file))
                        AddFigureFromTSODirectory(source_file);
                    break;
            }
        }

        /// <summary>
        /// フィギュア選択時に呼び出されるハンドラ
        /// </summary>
        public event EventHandler FigureEvent;

        /// <summary>
        /// フィギュアを選択します。
        /// </summary>
        /// <param name="fig_index">フィギュア番号</param>
        public void SetFigureIndex(int fig_index)
        {
            if (fig_index < 0)
                fig_index = 0;
            if (fig_index > FigureList.Count - 1)
                fig_index = 0;
            this.fig_index = fig_index;
            if (FigureEvent != null)
                FigureEvent(this, EventArgs.Empty);
        }

        /// <summary>
        /// 指定ディレクトリからフィギュアを作成して追加します。
        /// </summary>
        /// <param name="source_file">TSOFileを含むディレクトリ</param>
        public void AddFigureFromTSODirectory(string source_file)
        {
            List<TSOFile> tso_list = new List<TSOFile>();
            try
            {
                string[] files = Directory.GetFiles(source_file, "*.TSO");
                foreach (string file in files)
                {
                    TSOFile tso = new TSOFile();
                    Debug.WriteLine("loading " + file);
                    tso.Load(file);
                    tso_list.Add(tso);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            Figure fig = new Figure();
            foreach (TSOFile tso in tso_list)
            {
                if (TSOFileOpen != null)
                    TSOFileOpen(tso);
                fig.TSOFileList.Add(tso);
            }
            fig.UpdateNodeMapAndBoneMatrices();
            int idx = FigureList.Count;
            FigureList.Add(fig);
            SetFigureIndex(idx);
            if (FigureEvent != null)
                FigureEvent(this, EventArgs.Empty);
        }

        /// <summary>
        /// 選択フィギュアを得ます。
        /// </summary>
        public Figure GetSelectedFigure()
        {
            Figure fig;
            if (FigureList.Count == 0)
                fig = null;
            else
                fig = FigureList[fig_index];
            return fig;
        }

        /// <summary>
        /// 選択フィギュアを得ます。なければ作成します。
        /// </summary>
        public Figure GetSelectedOrCreateFigure()
        {
            Figure fig;
            if (FigureList.Count == 0)
                fig = new Figure();
            else
                fig = FigureList[fig_index];
            if (FigureList.Count == 0)
            {
                int idx = FigureList.Count;
                FigureList.Add(fig);
                SetFigureIndex(idx);
            }
            return fig;
        }

        /// <summary>
        /// 指定パスからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_file">パス</param>
        public void LoadTSOFile(string source_file)
        {
            Debug.WriteLine("loading " + source_file);
            using (Stream source_stream = File.OpenRead(source_file))
                LoadTSOFile(source_stream, source_file);
        }

        /// <summary>
        /// 指定ストリームからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void LoadTSOFile(Stream source_stream)
        {
            LoadTSOFile(source_stream, null);
        }

        /// <summary>
        /// 指定ストリームからTSOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        /// <param name="file">ファイル名</param>
        public void LoadTSOFile(Stream source_stream, string file)
        {
            List<TSOFile> tso_list = new List<TSOFile>();
            try
            {
                TSOFile tso = new TSOFile();
                tso.Load(source_stream);
                tso.FileName = file != null ? Path.GetFileNameWithoutExtension(file) : null;
                tso_list.Add(tso);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            Figure fig = GetSelectedOrCreateFigure();
            foreach (TSOFile tso in tso_list)
            {
                if (TSOFileOpen != null)
                    TSOFileOpen(tso);
                fig.TSOFileList.Add(tso);
            }
            fig.UpdateNodeMapAndBoneMatrices();
            if (FigureEvent != null)
                FigureEvent(this, EventArgs.Empty);
        }

        /// <summary>
        /// 選択フィギュアを得ます。
        /// </summary>
        public bool TryGetFigure(out Figure fig)
        {
            fig = null;
            if (fig_index < FigureList.Count)
                fig = FigureList[fig_index];
            return fig != null;
        }

        /// 次のフィギュアを選択します。
        public void NextFigure()
        {
            SetFigureIndex(fig_index + 1);
        }

        /// <summary>
        /// 指定パスからTMOFileを読み込みます。
        /// </summary>
        /// <param name="source_file">パス</param>
        public void LoadTMOFile(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                LoadTMOFile(source_stream);
        }

        /// <summary>
        /// 指定ストリームからTMOFileを読み込みます。
        /// </summary>
        /// <param name="source_stream">ストリーム</param>
        public void LoadTMOFile(Stream source_stream)
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                try
                {
                    TMOFile tmo = new TMOFile();
                    tmo.Load(source_stream);
                    fig.Tmo = tmo;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex);
                }
                fig.UpdateNodeMapAndBoneMatrices();
                if (FigureEvent != null)
                    FigureEvent(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 指定パスからPNGFileを読み込みフィギュアを作成して追加します。
        /// </summary>
        /// <param name="source_file">PNGFile のパス</param>
        /// <param name="append">FigureListを消去せずに追加するか</param>
        public void AddFigureFromPNGFile(string source_file, bool append)
        {
            PNGSaveFile sav = new PNGSaveFile();
            if (camera != null)
            {
                sav.CameraUpdate += delegate(Vector3 translation, Vector3 angle)
                {
                    camera.Reset();
                    camera.Translation = translation;
                    camera.Angle = angle;
                };
            }
            sav.Load(source_file);
            if (sav.figures.Count == 0) //POSE png
            {
                Debug.Assert(sav.Tmo != null, "save.Tmo should not be null");
                Figure fig;
                if (TryGetFigure(out fig))
                {
                    if (sav.LightDirection != Vector3.Zero)
                        fig.LightDirection = sav.LightDirection;
                    fig.Tmo = sav.Tmo;
                    //fig.TransformTpo();
                    fig.UpdateNodeMapAndBoneMatrices();
                    if (FigureEvent != null)
                        FigureEvent(this, EventArgs.Empty);
                }
            }
            else
            {
                if (!append)
                    ClearFigureList();

                int idx = FigureList.Count;
                foreach (Figure fig in sav.figures)
                {
                    foreach (TSOFile tso in fig.TSOFileList)
                    {
                        if (TSOFileOpen != null)
                            TSOFileOpen(tso);
                    }
                    fig.UpdateNodeMapAndBoneMatrices();
                    FigureList.Add(fig);
                }
                SetFigureIndex(idx);
            }
        }

        /// <summary>
        /// 全フィギュアを削除します。
        /// </summary>
        public void ClearFigureList()
        {
            foreach (Figure fig in FigureList)
                fig.Dispose();
            FigureList.Clear();
            SetFigureIndex(0);
            // free meshes and textures.
            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
        }

        /// <summary>
        /// 選択フィギュアを削除します。
        /// </summary>
        public void RemoveSelectedFigure()
        {
            Figure fig;
            if (TryGetFigure(out fig))
            {
                fig.Dispose();
                FigureList.Remove(fig);
                SetFigureIndex(fig_index - 1);
            }
            fig = null;
            // free meshes and textures.
            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(true));
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            foreach (Figure fig in FigureList)
                fig.Dispose();
        }
    }
}
