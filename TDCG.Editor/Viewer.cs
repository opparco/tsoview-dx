using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
using System.Globalization;
using System.IO;
//using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
//using System.Text.RegularExpressions;
//using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

using TDCG;

namespace TDCG.Editor
{
    /// <summary>
    /// TSOFileをDirect3D上でレンダリングします。
    /// </summary>
    public class Viewer : IDisposable
    {
        /// <summary>
        /// control
        /// </summary>
        protected Control control;

        //protected Direct3D direct3d;

        /// <summary>
        /// device
        /// </summary>
        protected Device device;
        protected DeviceContext ctx;
        protected SharpDX.DXGI.SwapChain swap_chain;
        protected Viewport viewport;

        /// <summary>
        /// effect
        /// </summary>
        protected Effect effect;
        InputLayout il;

        EffectMatrixVariable World_variable;
        EffectMatrixVariable WorldView_variable;
        EffectMatrixVariable WorldViewProjection_variable;
        /* for normal in view */
        EffectMatrixVariable View_variable;
        /* for HUD */
        EffectMatrixVariable Projection_variable;

        /// <summary>
        /// effect handle for LocalBoneMats
        /// since v0.90
        /// </summary>
        EffectMatrixVariable LocalBoneMats_variable;
        EffectMatrixVariable LocalBoneITMats_variable;

        /// <summary>
        /// effect handle for LightDirForced
        /// since v0.90
        /// </summary>
        EffectVectorVariable LightDirForced_variable;

        /// <summary>
        /// effect handle for UVSCR
        /// since v0.91
        /// </summary>
        EffectVectorVariable UVSCR_variable;

        ToonShader toon_shader;

        /// <summary>
        /// buffer #0 (back buffer)
        /// </summary>
        protected Texture2D buf0 = null;
        /// <summary>
        /// view of buffer #0
        /// </summary>
        protected RenderTargetView buf0_view = null;

        /// <summary>
        /// ztexture
        /// </summary>
        protected Texture2D ztex = null;
        /// <summary>
        /// view of ztexture
        /// </summary>
        protected DepthStencilView ztex_view = null;

        /// <summary>
        /// Figure collection
        /// </summary>
        public FigureCollection figures = new FigureCollection();

        /// <summary>
        /// マウスポイントしているスクリーン座標
        /// </summary>
        protected Point lastScreenPoint = Point.Zero;

        /// <summary>
        /// viewerを生成します。
        /// </summary>
        public Viewer()
        {
            ScreenColor = SharpDX.Color.LightGray;
        }

        /// マウスボタンを押したときに実行するハンドラ
        protected virtual void form_OnMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (Control.ModifierKeys == Keys.Control)
                        figures.SetLightDirection(ScreenToOrientation(e.X, e.Y));
                    break;
            }

            lastScreenPoint.X = e.X;
            lastScreenPoint.Y = e.Y;
        }

        /// マウスを移動したときに実行するハンドラ
        protected virtual void form_OnMouseMove(object sender, MouseEventArgs e)
        {
            int dx = e.X - lastScreenPoint.X;
            int dy = e.Y - lastScreenPoint.Y;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (Control.ModifierKeys == Keys.Control)
                        figures.SetLightDirection(ScreenToOrientation(e.X, e.Y));
                    else
                        Camera.RotateYawPitchRoll(-dx * 0.01f, -dy * 0.01f, 0.0f);
                    break;
                case MouseButtons.Middle:
                    Camera.MoveView(-dx * 0.125f, dy * 0.125f, 0.0f);
                    break;
                case MouseButtons.Right:
                    Camera.MoveView(0.0f, 0.0f, -dy * 0.125f);
                    break;
            }

            lastScreenPoint.X = e.X;
            lastScreenPoint.Y = e.Y;
        }

        // スクリーンの中心座標
        private float screenCenterX = 800 / 2.0f;
        private float screenCenterY = 600 / 2.0f;

        /// <summary>
        /// controlを保持します。スクリーンの中心座標を更新します。
        /// </summary>
        /// <param name="control">control</param>
        protected void SetControl(Control control)
        {
            this.control = control;
            screenCenterX = control.ClientSize.Width / 2.0f;
            screenCenterY = control.ClientSize.Height / 2.0f;
        }

        /// <summary>
        /// 指定スクリーン座標からスクリーン中心へ向かうベクトルを得ます。
        /// </summary>
        /// <param name="screenPointX">スクリーンX座標</param>
        /// <param name="screenPointY">スクリーンY座標</param>
        /// <returns>方向ベクトル</returns>
        public Vector3 ScreenToOrientation(float screenPointX, float screenPointY)
        {
            float radius = 1.0f;
            float x = -(screenPointX - screenCenterX) / (radius * screenCenterX);
            float y = +(screenPointY - screenCenterY) / (radius * screenCenterY);
            float z = 0.0f;
            float mag = (x * x) + (y * y);

            if (mag > 1.0f)
            {
                float scale = 1.0f / (float)Math.Sqrt(mag);
                x *= scale;
                y *= scale;
            }
            else
                z = (float)-Math.Sqrt(1.0f - mag);

            return new Vector3(x, y, z);
        }

        SimpleCamera camera = new SimpleCamera();

        /// <summary>
        /// カメラ
        /// </summary>
        public SimpleCamera Camera { get { return camera; } set { camera = value; } }

        /// <summary>
        /// world行列
        /// </summary>
        protected Matrix world_matrix = Matrix.Identity;
        /// <summary>
        /// view変換行列
        /// </summary>
        protected Matrix Transform_View = Matrix.Identity;
        /// <summary>
        /// projection変換行列
        /// </summary>
        protected Matrix Transform_Projection = Matrix.Identity;

        Matrix world_view_projection_matrix = Matrix.Identity;

        /// <summary>
        /// deviceを作成します。
        /// </summary>
        /// <param name="control">レンダリング先となるcontrol</param>
        /// <returns>deviceの作成に成功したか</returns>
        public bool InitializeApplication(Control control)
        {
            SetControl(control);

            control.MouseDown += new MouseEventHandler(form_OnMouseDown);
            control.MouseMove += new MouseEventHandler(form_OnMouseMove);

            var desc = new SharpDX.DXGI.SwapChainDescription()
            {
                BufferCount = 1,
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                OutputHandle = control.Handle,
                IsWindowed = true,
                ModeDescription = new SharpDX.DXGI.ModeDescription(0, 0, new SharpDX.DXGI.Rational(60, 1), SharpDX.DXGI.Format.B8G8R8A8_UNorm),
                SampleDescription = new SharpDX.DXGI.SampleDescription(4, 0),
                Flags = SharpDX.DXGI.SwapChainFlags.AllowModeSwitch,
                SwapEffect = SharpDX.DXGI.SwapEffect.Discard
            };
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swap_chain);

            //DetectSampleDescription(device, SharpDX.DXGI.Format.D32_Float);

            ctx = device.ImmediateContext;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string effect_file = Path.Combine(Application.StartupPath, @"toonshader.fx.bin");
            if (!File.Exists(effect_file))
            {
                Console.WriteLine("File not found: " + effect_file);
                return false;
            }
            try
            {
                using (var shader_bytecode = ShaderBytecode.FromFile(effect_file))
                {
                    effect = new Effect(device, shader_bytecode);
                }
            }
            catch (SharpDX.CompilationException e)
            {
                Console.WriteLine(e.Message + ": " + effect_file);
                return false;
            }

            sw.Stop();
            Console.WriteLine("toonshader.fx.bin read time: " + sw.Elapsed);

            World_variable = effect.GetVariableBySemantic("World").AsMatrix();
            WorldView_variable = effect.GetVariableBySemantic("WorldView").AsMatrix();
            WorldViewProjection_variable = effect.GetVariableBySemantic("WorldViewProjection").AsMatrix();
            /* for normal in view */
            View_variable = effect.GetVariableBySemantic("View").AsMatrix();
            /* for HUD */
            Projection_variable = effect.GetVariableBySemantic("Projection").AsMatrix();

            LocalBoneMats_variable = effect.GetVariableByName("LocalBoneMats").AsMatrix();
            LocalBoneITMats_variable = effect.GetVariableByName("LocalBoneITMats").AsMatrix();
            LightDirForced_variable = effect.GetVariableByName("LightDirForced").AsVector();
            UVSCR_variable = effect.GetVariableByName("UVSCR").AsVector();

            toon_shader = new ToonShader(device, effect);

            figures.Camera = camera;
            figures.TSOFileOpen += delegate (TSOFile tso)
            {
                tso.CreateD3DResources(device);
            };

            // Define an input layout to be passed to the vertex shader.
            var technique = effect.GetTechniqueByIndex(0);
            var pass = technique.GetPassByIndex(0);
            using (var signature = pass.Description.Signature)
            {
                il = new InputLayout(device, signature, TSOSubMesh.ie);
            }

            // Setup the immediate context to use the shaders and model we defined.
            ctx.InputAssembler.InputLayout = il;

            camera.Update();

            DefineBlendState();
            DefineDepthStencilState();
            DefineRasterizerState();

            ctx.Rasterizer.State = default_rasterizer_state;

            return true;
        }

        BlendState default_blend_state;

        void DefineBlendState()
        {
            var desc = BlendStateDescription.Default();

            desc.RenderTarget[0].IsBlendEnabled = true;
            desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            desc.RenderTarget[0].BlendOperation = BlendOperation.Add;

            /*
            desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            */

            default_blend_state = new BlendState(device, desc);
        }

        DepthStencilState default_depth_stencil_state;

        // Define how the depth buffer will be used to filter out objects, based on their distance from the viewer.
        void DefineDepthStencilState()
        {
            var desc = DepthStencilStateDescription.Default();

            // NoDepthWriteState
            /*
            desc.IsDepthEnabled = true;
            desc.DepthWriteMask = DepthWriteMask.Zero;
            */

            default_depth_stencil_state = new DepthStencilState(device, desc);
        }

        RasterizerState default_rasterizer_state;
        RasterizerState wireframe_rasterizer_state;

        void DefineRasterizerState()
        {
            var desc = RasterizerStateDescription.Default();

            desc.IsFrontCounterClockwise = true;

            default_rasterizer_state = new RasterizerState(device, desc);

            desc.FillMode = FillMode.Wireframe;

            wireframe_rasterizer_state = new RasterizerState(device, desc);
        }

        bool wired = false;

        public void SwitchFillMode()
        {
            wired = !wired;
        }

        static SharpDX.DXGI.SampleDescription DetectSampleDescription(Device device, SharpDX.DXGI.Format format)
        {
            var desc = new SharpDX.DXGI.SampleDescription();
            for (int multisample_count = Device.MultisampleCountMaximum; multisample_count > 0; --multisample_count)
            {
                int quality_levels = device.CheckMultisampleQualityLevels(format, multisample_count);
                if (quality_levels > 0)
                {
                    desc.Count = multisample_count;
                    desc.Quality = quality_levels - 1;
                    break;
                }
            }
            Console.WriteLine("sample count {0} quality {1}", desc.Count, desc.Quality);
            return desc;
        }

        public void OnUserResized()
        {
            Console.WriteLine("OnUserResized client size {0}x{1}", control.ClientSize.Width, control.ClientSize.Height);

            if (ztex_view != null)
                ztex_view.Dispose();
            if (ztex != null)
                ztex.Dispose();

            if (buf0_view != null)
                buf0_view.Dispose();
            if (buf0 != null)
                buf0.Dispose();

            // Resize the backbuffer
            swap_chain.ResizeBuffers(1 /* desc.BufferCount */, control.ClientSize.Width, control.ClientSize.Height, SharpDX.DXGI.Format.Unknown, SharpDX.DXGI.SwapChainFlags.None);

            // Retrieve the back buffer of the swap chain.
            buf0 = Texture2D.FromSwapChain<Texture2D>(swap_chain, 0);
            buf0_view = new RenderTargetView(device, buf0);

            // Create the depth buffer
            ztex = new Texture2D(device, new Texture2DDescription()
            {
                Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = control.ClientSize.Width,
                Height = control.ClientSize.Height,
                SampleDescription = new SharpDX.DXGI.SampleDescription(4, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
            });

            // Create the depth buffer view
            ztex_view = new DepthStencilView(device, ztex);

            ctx.OutputMerger.SetTargets(ztex_view, buf0_view);

            // Setup targets and viewport for rendering
            viewport = new Viewport(0, 0, control.ClientSize.Width, control.ClientSize.Height, 0.0f, 1.0f);
            ctx.Rasterizer.SetViewport(viewport);

            // Setup new projection matrix with correct aspect ratio
            float aspect = viewport.Width / (float)viewport.Height;
            float d = 1.0f; // zn
            float h = d * (float)Math.Tan(Math.PI / 12.0);
            float w = h * aspect;
            Transform_Projection = Matrix.PerspectiveRH(w * 2.0f, h * 2.0f, 1.0f, 500.0f);
        }

        bool motion_enabled = false;

        long start_ticks = 0;
        int start_frame_index = 0;
        long wait = (long)(10000000.0f / 60.0f);
        int frame_index = 0;

        /// <summary>
        /// モーションの有無
        /// </summary>
        public bool MotionEnabled
        {
            get
            {
                return motion_enabled;
            }
            set
            {
                motion_enabled = value;

                if (motion_enabled)
                {
                    start_ticks = DateTime.Now.Ticks;
                    start_frame_index = frame_index;
                }
            }
        }

        /// <summary>
        /// 次のシーンフレームに進みます。
        /// </summary>
        public void Update()
        {
            if (camera.NeedUpdate)
            {
                camera.Update();
                Transform_View = camera.ViewMatrix;
            }
            if (motion_enabled)
            {
                int frame_len = figures.GetMaxFrameLength();
                if (frame_len > 0)
                {
                    long dt = DateTime.Now.Ticks - start_ticks;
                    int new_frame_index = (int)((start_frame_index + dt / wait) % frame_len);
                    Debug.Assert(new_frame_index >= 0);
                    Debug.Assert(new_frame_index < frame_len);
                    frame_index = new_frame_index;
                }
                figures.SetFrameIndex(frame_index);
                figures.UpdateBoneMatrices(true);
            }
        }

        /// <summary>
        /// レンダリングするのに用いるデリゲート型
        /// </summary>
        public delegate void RenderingHandler();

        /// <summary>
        /// レンダリングするハンドラ
        /// </summary>
        public RenderingHandler Rendering;

        /// <summary>
        /// シーンをレンダリングします。
        /// </summary>
        public void Render()
        {
            ctx.ClearDepthStencilView(ztex_view, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            ctx.ClearRenderTargetView(buf0_view, ScreenColor);

            UVSCR_variable.Set(UVSCR());

            DrawFigure();

            if (Rendering != null)
                Rendering();

            swap_chain.Present(1, SharpDX.DXGI.PresentFlags.None);
        }

        void UpdateTransform(ref Matrix world)
        {
            Matrix world_view_matrix = world * Transform_View;
            world_view_projection_matrix = world_view_matrix * Transform_Projection;

            World_variable.SetMatrix(world);
            WorldView_variable.SetMatrix(world_view_matrix);
            WorldViewProjection_variable.SetMatrix(world_view_projection_matrix);
            /* for normal in view */
            View_variable.SetMatrix(Transform_View);
            /* for HUD */
            Projection_variable.SetMatrix(Transform_Projection);
        }

        /// スクリーン塗りつぶし色
        public SharpDX.Color ScreenColor { get; set; }

        /// <summary>
        /// UVSCR値を得ます。
        /// </summary>
        /// <returns></returns>
        public Vector4 UVSCR()
        {
            float x = Environment.TickCount * 0.000002f;
            return new Vector4(x, 0.0f, 0.0f, 0.0f);
        }

        /// <summary>
        /// フィギュアを描画します。
        /// </summary>
        protected virtual void DrawFigure()
        {
            foreach (Figure fig in figures)
            {
                DrawFigure(fig);
            }
        }

        static void Dump(ref Matrix m)
        {
                Console.WriteLine("{0:F4} {1:F4} {2:F4} {3:F4}", m.M11, m.M12, m.M13, m.M14);
                Console.WriteLine("{0:F4} {1:F4} {2:F4} {3:F4}", m.M21, m.M22, m.M23, m.M24);
                Console.WriteLine("{0:F4} {1:F4} {2:F4} {3:F4}", m.M31, m.M32, m.M33, m.M34);
                Console.WriteLine("{0:F4} {1:F4} {2:F4} {3:F4}", m.M41, m.M42, m.M43, m.M44);
        }

        void DrawFigure(Figure fig)
        {
            {
                Matrix world;
                fig.GetWorldMatrix(out world);
#if false
                Console.WriteLine("-- dump world");
                Dump(ref world);
#endif
                UpdateTransform(ref world);
            }
            LightDirForced_variable.Set(fig.LightDirForced);
            foreach (TSOFile tso in fig.TSOFileList)
            {
                toon_shader.RemoveShader();

                foreach (TSOMesh mesh in tso.meshes)
                    foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                    {
                        TSOSubScript scr = tso.sub_scripts[sub_mesh.spec];

                        toon_shader.SwitchShader(scr.shader, tso.GetD3DTextureSRViewByName);

                        Matrix[] mats = fig.ClipBoneMatrices(sub_mesh);
                        LocalBoneMats_variable.SetMatrix(mats);
                        Matrix[] itmats = new Matrix[mats.Length];
                        for (int i = 0; i < mats.Length; i++)
                        {
                            itmats[i] = mats[i];
                            itmats[i].Invert();
                            itmats[i].Transpose();
                        }
                        LocalBoneITMats_variable.SetMatrix(itmats);

                        ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith3ControlPoints;
                        ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(sub_mesh.vb, 52, 0));
                        ctx.InputAssembler.SetIndexBuffer(sub_mesh.ib, SharpDX.DXGI.Format.R16_UInt, 0);

                        var technique = toon_shader.Technique;

                        for (int i = 0; i < technique.Description.PassCount; i++)
                        {
                            ctx.OutputMerger.SetBlendState(default_blend_state);
                            ctx.OutputMerger.SetDepthStencilState(default_depth_stencil_state);

                            ctx.Rasterizer.State = wired ? wireframe_rasterizer_state : default_rasterizer_state;

                            technique.GetPassByIndex(i).Apply(ctx);
                            ctx.DrawIndexed(sub_mesh.vindices.Length, 0, 0);
                        }
                    }
                toon_shader.RemoveShader();
            }
        }

        /// <summary>
        /// 内部objectを破棄します。
        /// </summary>
        public void Dispose()
        {
            figures.Dispose();

            if (ctx != null)
            {
                ctx.ClearState();
                ctx.Flush();
                ctx.Dispose();
            }

            default_rasterizer_state.Dispose();
            default_depth_stencil_state.Dispose();
            default_blend_state.Dispose();

            if (ztex_view != null)
                ztex_view.Dispose();
            if (ztex != null)
                ztex.Dispose();

            if (buf0_view != null)
                buf0_view.Dispose();
            if (buf0 != null)
                buf0.Dispose();

            if (toon_shader != null)
                toon_shader.Dispose();

            if (swap_chain != null)
                swap_chain.Dispose();

            if (il != null)
                il.Dispose();
            if (effect != null)
                effect.Dispose();
            if (device != null)
                device.Dispose();
        }

        /// <summary>
        /// バックバッファをファイルに保存します。
        /// </summary>
        /// <param name="file">ファイル名</param>
        public void SaveToBitmap(string file)
        {
            var intermediateDesc = buf0.Description;
            intermediateDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

            var desc = buf0.Description;
            desc.BindFlags = SharpDX.Direct3D11.BindFlags.None;
            desc.Usage = SharpDX.Direct3D11.ResourceUsage.Staging;
            desc.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read;
            desc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

            using (Texture2D intermediate = new SharpDX.Direct3D11.Texture2D(device, intermediateDesc))
            {
                ctx.ResolveSubresource(buf0, 0, intermediate, 0, buf0.Description.Format);

                using (Texture2D buf1 = new SharpDX.Direct3D11.Texture2D(device, desc))
                {
                    ctx.CopyResource(intermediate, buf1);

                    DataStream stream;
                    ctx.MapSubresource(buf1, 0, MapMode.Read, MapFlags.None, out stream);
                    IntPtr src = stream.DataPointer;

                    using (System.Drawing.Bitmap bitmap =
                            new System.Drawing.Bitmap(desc.Width, desc.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        // Lock the bitmap's bits.
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        System.Drawing.Imaging.BitmapData bitmapData =
                            bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                            bitmap.PixelFormat);

                        // Get the address of the first line.
                        IntPtr ptr = bitmapData.Scan0;

#if false
                    Utilities.CopyMemory(ptr, src, bitmapData.Stride * bitmapData.Height);
#endif

                        // drop Alpha ch.
                        for (int y = 0; y < bitmapData.Height; y++)
                        {
                            for (int x = 0; x < bitmapData.Width; x++)
                            {
                                Utilities.CopyMemory(ptr, src, 3);
                                ptr = IntPtr.Add(ptr, 3);
                                src = IntPtr.Add(src, 4);
                            }
                        }

                        // Unlock the bits.
                        bitmap.UnlockBits(bitmapData);

                        bitmap.Save(file);
                    }
                    ctx.UnmapSubresource(buf1, 0);
                }
            }
        }

        string GetSaveFileName(string type)
        {
            DateTime ti = DateTime.Now;
            CultureInfo ci = CultureInfo.InvariantCulture;
            string ti_string = ti.ToString("yyyyMMdd-hhmmss-fff", ci);
            return string.Format("{0}-{1}.png", ti_string, type);
        }

        public void SaveToBitmap()
        {
            SaveToBitmap(GetSaveFileName("amb"));
        }

        /// <summary>
        /// 指定スクリーン座標に指定ボーンがあるか。
        /// </summary>
        /// <param name="x">スクリーンX座標</param>
        /// <param name="y">スクリーンY座標</param>
        /// <param name="bone">ボーン</param>
        /// <returns>ボーンを見つけたか</returns>
        public bool FindBoneOnScreenPoint(float x, float y, TMONode bone)
        {
            float collisionTime;
            Vector3 collisionPoint;

            return FindBoneOnScreenPoint(x, y, bone, out collisionPoint, out collisionTime);
        }

        /// <summary>
        /// 指定スクリーン座標に指定ボーンがあるか。
        /// </summary>
        /// <param name="x">スクリーンX座標</param>
        /// <param name="y">スクリーンY座標</param>
        /// <param name="bone">ボーン</param>
        /// <param name="collisionPoint"></param>
        /// <param name="collisionTime"></param>
        /// <returns>ボーンを見つけたか</returns>
        public bool FindBoneOnScreenPoint(float x, float y, TMONode bone, out Vector3 collisionPoint, out float collisionTime)
        {
            collisionTime = 0.0f;
            collisionPoint = Vector3.Zero;

            Figure fig;
            if (figures.TryGetFigure(out fig))
            {
                Matrix m = bone.combined_matrix;

                float sphereRadius = 1.25f;
                Vector3 sphereCenter = new Vector3(m.M41, m.M42, m.M43);
                Vector3 rayStart = ScreenToLocal(x, y, 0.0f);
                Vector3 rayEnd = ScreenToLocal(x, y, 1.0f);
                Vector3 rayOrientation = rayEnd - rayStart;

                return DetectSphereRayCollision(sphereRadius, ref sphereCenter, ref rayStart, ref rayOrientation, out collisionPoint, out collisionTime);
            }
            return false;
        }

        /// <summary>
        /// 球とレイの衝突を見つけます。
        /// </summary>
        /// <param name="sphereRadius">球の半径</param>
        /// <param name="sphereCenter">球の中心位置</param>
        /// <param name="rayStart">光線の発射位置</param>
        /// <param name="rayOrientation">光線の方向</param>
        /// <param name="collisionPoint">衝突位置</param>
        /// <param name="collisionTime">衝突時刻</param>
        /// <returns>衝突したか</returns>
        public static bool DetectSphereRayCollision(float sphereRadius, ref Vector3 sphereCenter, ref Vector3 rayStart, ref Vector3 rayOrientation, out Vector3 collisionPoint, out float collisionTime)
        {
            collisionTime = 0.0f;
            collisionPoint = Vector3.Zero;

            Vector3 u = rayStart - sphereCenter;
            float a = Vector3.Dot(rayOrientation, rayOrientation);
            float b = Vector3.Dot(rayOrientation, u);
            float c = Vector3.Dot(u, u) - sphereRadius * sphereRadius;
            if (a <= float.Epsilon)
                //誤差
                return false;
            float d = b * b - a * c;
            if (d < 0.0f)
                //衝突しない
                return false;
            collisionTime = (-b - (float)Math.Sqrt(d)) / a;
            collisionPoint = rayStart + rayOrientation * collisionTime;
            return true;
        }

        /// <summary>
        /// viewport行列を作成します。
        /// </summary>
        /// <param name="viewport">viewport</param>
        /// <returns>viewport行列</returns>
        public static Matrix CreateViewportMatrix(Viewport viewport)
        {
            Matrix m = Matrix.Identity;
            m.M11 = (float)viewport.Width / 2;
            m.M22 = -1.0f * (float)viewport.Height / 2;
            m.M33 = (float)viewport.MaxDepth - (float)viewport.MinDepth;
            m.M41 = (float)(viewport.X + viewport.Width / 2);
            m.M42 = (float)(viewport.Y + viewport.Height / 2);
            m.M43 = viewport.MinDepth;
            return m;
        }

        /// スクリーン位置をワールド座標へ変換します。
        public static Vector3 ScreenToWorld(float screenX, float screenY, float z, Viewport viewport, Matrix view, Matrix proj)
        {
            //スクリーン位置
            Vector3 v = new Vector3(screenX, screenY, z);

            Matrix inv_m = Matrix.Invert(CreateViewportMatrix(viewport));
            Matrix inv_proj = Matrix.Invert(proj);
            Matrix inv_view = Matrix.Invert(view);

            //スクリーン位置をワールド座標へ変換
            return Vector3.TransformCoordinate(v, inv_m * inv_proj * inv_view);
        }

        /// スクリーン位置をワールド座標へ変換します。
        public Vector3 ScreenToWorld(float screenX, float screenY, float z)
        {
            return ScreenToWorld(screenX, screenY, z, viewport, Transform_View, Transform_Projection);
        }

        /// ワールド座標をスクリーン位置へ変換します。
        public static Vector3 WorldToScreen(Vector3 v, Viewport viewport, Matrix view, Matrix proj)
        {
            return Vector3.TransformCoordinate(v, view * proj * CreateViewportMatrix(viewport));
        }

        /// ワールド座標をスクリーン位置へ変換します。
        public Vector3 WorldToScreen(Vector3 v)
        {
            return WorldToScreen(v, viewport, Transform_View, Transform_Projection);
        }

        /// スクリーン位置をローカル座標へ変換します。
        public static Vector3 ScreenToLocal(float screenX, float screenY, float z, Viewport viewport, Matrix wvp)
        {
            //スクリーン位置
            Vector3 v = new Vector3(screenX, screenY, z);

            Matrix inv = Matrix.Invert(wvp * CreateViewportMatrix(viewport));

            //スクリーン位置をワールド座標へ変換
            return Vector3.TransformCoordinate(v, inv);
        }

        /// スクリーン位置をローカル座標へ変換します。
        public Vector3 ScreenToLocal(float screenX, float screenY, float z)
        {
            return ScreenToLocal(screenX, screenY, z, viewport, world_view_projection_matrix);
        }
    }
}
