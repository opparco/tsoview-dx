using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

//using OculusWrap;
using TDCG;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.DirectInput;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Effect = SharpDX.Direct3D11.Effect;

namespace OcuView
{
    public class Camera
    {
        public Vector3 Position;
        public Quaternion Rotation;
        float yaw = 0.0f;

        public Camera()
        {
        }
        public Camera(Vector3 position, Quaternion rotation)
        {
            this.Position = position;
            this.Rotation = rotation;
        }

        /// <summary>
        /// カメラの位置を更新します。
        /// </summary>
        /// <param name="x">移動方向</param>
        /// <param name="y">移動方向</param>
        /// <param name="z">移動方向</param>
        public void Move(float x, float y, float z)
        {
            this.Position += Vector3.Transform(new Vector3(x, y, z), this.Rotation);
        }

        /// <summary>
        /// カメラをY軸回転します。
        /// </summary>
        /// <param name="angle">回転角度（ラジアン）</param>
        public void RotY(float angle)
        {
            this.Rotation = Quaternion.RotationYawPitchRoll(yaw += angle, 0, 0);
        }

        /// <summary>
        /// view行列を取得します。
        /// </summary>
        public Matrix GetViewMatrix()
        {
            Vector3 up = Vector3.Transform(new Vector3(0, +1, 0), Rotation);
            Vector3 forward = Vector3.Transform(new Vector3(0, 0, -1), Rotation);
            return Matrix.LookAtRH(Position, Position + forward, up);
        }
    }

    public static class TMONodePath
    {
        public static string Chichi_Right3 = "|W_Hips|W_Spine_Dummy|W_Spine1|W_Spine2|W_Spine3|Chichi_Right1|Chichi_Right2|Chichi_Right3";
        public static string Chichi_Left3 = "|W_Hips|W_Spine_Dummy|W_Spine1|W_Spine2|W_Spine3|Chichi_Left1|Chichi_Left2|Chichi_Left3";
    }

public class OculusViewer : IDisposable
{
    OculusWrap.Wrap oculus;
    OculusWrap.Hmd hmd;

    // Create a set of layers to submit.
    EyeTexture[] eye_texes = new EyeTexture[2];

    SharpDX.Direct3D11.Device device;
    DeviceContext ctx;
    SharpDX.DXGI.Factory dxgi_factory;
    SharpDX.DXGI.SwapChain swap_chain;
    Viewport viewport;

    Texture2D buf0;
    RenderTargetView buf0_view;

    Texture2D ztex;
    DepthStencilView ztex_view;

    /// <summary>
    /// Figure collection
    /// </summary>
    public FigureCollection figures = new FigureCollection();

    OculusWrap.Layers layers;
    OculusWrap.LayerEyeFov layer_eye_fov;

    Texture2D mtex;

    protected Effect effect;
    InputLayout il;

    EffectMatrixVariable World_variable;
    EffectMatrixVariable WorldView_variable;
    EffectMatrixVariable WorldViewProjection_variable;
    /* for HUD */
    EffectMatrixVariable Projection_variable;

    EffectMatrixVariable LocalBoneMats_variable;
    EffectVectorVariable LightDirForced_variable;
    EffectVectorVariable UVSCR_variable;

    EffectConstantBuffer cb_variable;

    EffectShaderResourceVariable ShadeTex_texture_variable;
    EffectShaderResourceVariable ColorTex_texture_variable;

    DirectInput directInput = null;
    Keyboard keyboard = null;
    KeyboardState keyboardState = null;


    /// <summary>
    /// viewerを生成します。
    /// </summary>
    public OculusViewer()
    {
        ScreenColor = SharpDX.Color.LightGray;
    }

    /// マウスボタンを押したときに実行するハンドラ
    protected virtual void form_OnMouseDown(object sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
        case MouseButtons.Left:
            {
                //おっぱいを触っているか判定する
                bool touched = false;

                //注視点 on screen
                float x = (float)(viewport.Width / 2);
                float y = (float)(viewport.Height / 2);

                Figure fig;
                if (figures.TryGetFigure(out fig))
                {
                    // todo: figures.TryGetFigure again
                    touched = touched || FindBoneOnScreenPoint(x, y, fig.Tmo.nodemap[TMONodePath.Chichi_Right3]);
                    touched = touched || FindBoneOnScreenPoint(x, y, fig.Tmo.nodemap[TMONodePath.Chichi_Left3]);
                }
                if (touched)
                    fig.ResetSpring();
            }
            break;
        }
    }

    TechniqueMap techmap = new TechniqueMap();
    OcuConfig ocu_config;

    /// <summary>
    /// deviceを作成します。
    /// </summary>
    /// <param name="control">レンダリング先となるcontrol</param>
    /// <param name="ocu_config">設定</param>
    /// <returns>deviceの作成に成功したか</returns>
    public bool InitializeApplication(Control control, OcuConfig ocu_config)
    {
        this.ocu_config = ocu_config;
        oculus = new OculusWrap.Wrap();

        // Initialize the Oculus runtime.
        bool success = oculus.Initialize();
        if (!success)
        {
            MessageBox.Show("Failed to initialize the Oculus runtime library.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        // Use the head mounted display, if it's available, otherwise use the debug HMD.
        int numberOfHeadMountedDisplays = oculus.Hmd_Detect();
        if (numberOfHeadMountedDisplays > 0)
            hmd = oculus.Hmd_Create(0);
        else
            hmd = oculus.Hmd_CreateDebug(OculusWrap.OVR.HmdType.DK2);

        if (hmd == null)
        {
            MessageBox.Show("Oculus Rift not detected.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (hmd.ProductName == string.Empty)
            MessageBox.Show("The HMD is not enabled.", "There's a tear in the Rift", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Specify which head tracking capabilities to enable.
        hmd.SetEnabledCaps(OculusWrap.OVR.HmdCaps.LowPersistence | OculusWrap.OVR.HmdCaps.DynamicPrediction);

        // Start the sensor which informs of the Rift's pose and motion
        hmd.ConfigureTracking(OculusWrap.OVR.TrackingCaps.ovrTrackingCap_Orientation | OculusWrap.OVR.TrackingCaps.ovrTrackingCap_MagYawCorrection | OculusWrap.OVR.TrackingCaps.ovrTrackingCap_Position, OculusWrap.OVR.TrackingCaps.None);

        // Create DirectX drawing device.
        device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None);

        ctx = device.ImmediateContext;

        Stopwatch sw = new Stopwatch();
        sw.Start();

        string effect_file = Path.Combine(Application.StartupPath, @"toonshader.fx.bin");
        if (! File.Exists(effect_file))
        {
            Console.WriteLine("File not found: " + effect_file);
            return false;
        }
        try
        {
            var shader_bytecode = ShaderBytecode.FromFile(effect_file);
            effect = new Effect(device, shader_bytecode);
        }
        catch (SharpDX.CompilationException e)
        {
            Console.WriteLine(e.Message + ": " + effect_file);
            return false;
        }

        sw.Stop();
        Console.WriteLine("toonshader.fx.bin read time: " + sw.Elapsed);

        string techmap_file = Path.Combine(Application.StartupPath, @"techmap.txt");
        if (!File.Exists(techmap_file))
        {
            Console.WriteLine("File not found: " + techmap_file);
            return false;
        }
        techmap.Load(techmap_file);

        control.MouseDown += new MouseEventHandler(form_OnMouseDown);

        // Define the properties of the swap chain.
        SwapChainDescription swapChainDescription = DefineSwapChainDescription(control);

        // Create DirectX Graphics Interface factory, used to create the swap chain.
        dxgi_factory = new SharpDX.DXGI.Factory();
        // Create the swap chain.
        swap_chain = new SwapChain(dxgi_factory, device, swapChainDescription);

        // Retrieve the back buffer of the swap chain.
        buf0 = swap_chain.GetBackBuffer<Texture2D>(0);
        buf0_view = new RenderTargetView(device, buf0);

        // Create a depth buffer, using the same width and height as the back buffer.
        Texture2DDescription depthBufferDescription = DefineDepthBufferDescription(control);

        // Create the depth buffer.
        ztex = new Texture2D(device, depthBufferDescription);
        ztex_view = new DepthStencilView(device, ztex);

        ctx.OutputMerger.SetRenderTargets(ztex_view, buf0_view);

        viewport = new Viewport(0, 0, hmd.Resolution.Width, hmd.Resolution.Height, 0.0f, 1.0f);
        ctx.Rasterizer.SetViewport(viewport);

        // Retrieve the DXGI device, in order to set the maximum frame latency.
        using (SharpDX.DXGI.Device1 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device1>())
        {
            dxgiDevice.MaximumFrameLatency = 1;
        }

        layers = new OculusWrap.Layers();
        layer_eye_fov = layers.AddLayerEyeFov();

        CreateEyeTextures();

        CreateMirrorTexture(control);

        World_variable = effect.GetVariableBySemantic("World").AsMatrix();
        WorldView_variable = effect.GetVariableBySemantic("WorldView").AsMatrix();
        WorldViewProjection_variable = effect.GetVariableBySemantic("WorldViewProjection").AsMatrix();
        /* for HUD */
        Projection_variable = effect.GetVariableBySemantic("Projection").AsMatrix();

        LocalBoneMats_variable = effect.GetVariableByName("LocalBoneMats").AsMatrix();
        LightDirForced_variable = effect.GetVariableByName("LightDirForced").AsVector();
        UVSCR_variable = effect.GetVariableByName("UVSCR").AsVector();

        cb_variable = effect.GetConstantBufferByName("cb");

        ShadeTex_texture_variable = effect.GetVariableByName("ShadeTex_texture").AsShaderResource();
        ColorTex_texture_variable = effect.GetVariableByName("ColorTex_texture").AsShaderResource();

        //figures.Camera = camera;
        figures.TSOFileOpen += delegate(TSOFile tso)
        {
            tso.Open(device, effect);
            techmap.AssignTechniqueIndices(tso);
        };

        // Define an input layout to be passed to the vertex shader.
        var technique = effect.GetTechniqueByIndex(0);
        il = new InputLayout(device, technique.GetPassByIndex(0).Description.Signature, TSOSubMesh.ie);

        // Setup the immediate context to use the shaders and model we defined.
        ctx.InputAssembler.InputLayout = il;

        DefineBlendState();
        DefineDepthStencilState();
        DefineRasterizerState();

        main_camera = new Camera()
        {
            Position = ocu_config.Position,
            Rotation = Quaternion.Identity,
        };

        directInput = new DirectInput();
        keyboard = new Keyboard(directInput);
        keyboard.Acquire();

        keyboardState = keyboard.GetCurrentState();

        return true;
    }

    void CreateEyeTextures()
    {
        for (int eye_idx = 0; eye_idx < 2; eye_idx++)
        {
            OculusWrap.OVR.EyeType eye = (OculusWrap.OVR.EyeType)eye_idx;
            EyeTexture eye_tex = new EyeTexture();
            eye_texes[eye_idx] = eye_tex;

            // Retrieve size and position of the texture for the current eye.
            eye_tex.FieldOfView = hmd.DefaultEyeFov[eye_idx];
            eye_tex.TextureSize = hmd.GetFovTextureSize(eye, hmd.DefaultEyeFov[eye_idx], 1.0f);
            eye_tex.RenderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eye_idx]);
            eye_tex.HmdToEyeViewOffset = eye_tex.RenderDescription.HmdToEyeViewOffset;
            eye_tex.ViewportSize.Position = new OculusWrap.OVR.Vector2i(0, 0);
            eye_tex.ViewportSize.Size = eye_tex.TextureSize;
            eye_tex.Viewport = new Viewport(0, 0, eye_tex.TextureSize.Width, eye_tex.TextureSize.Height, 0.0f, 1.0f);

            // Define a texture at the size recommended for the eye texture.
            eye_tex.Texture2DDescription = DefineEyeTextureDescription(eye_tex);

            // Convert the SharpDX texture description to the native Direct3D texture description.
            OculusWrap.OVR.D3D11.D3D11_TEXTURE2D_DESC swapTextureDescriptionD3D11 = SharpDXHelpers.CreateTexture2DDescription(eye_tex.Texture2DDescription);

            // Create a SwapTextureSet, which will contain the textures to render to, for the current eye.
            var result = hmd.CreateSwapTextureSetD3D11(device.NativePointer, ref swapTextureDescriptionD3D11, out eye_tex.SwapTextureSet);
            WriteErrorDetails(oculus, result, "Failed to create swap texture set.");

            // Create room for each DirectX texture in the SwapTextureSet.
            eye_tex.Textures = new Texture2D[eye_tex.SwapTextureSet.TextureCount];
            eye_tex.RenderTargetViews = new RenderTargetView[eye_tex.SwapTextureSet.TextureCount];

            // Create a texture 2D and a render target view, for each unmanaged texture contained in the SwapTextureSet.
            for (int tex_idx = 0; tex_idx < eye_tex.SwapTextureSet.TextureCount; tex_idx++)
            {
                // Retrieve the current textureData object.
                OculusWrap.OVR.D3D11.D3D11TextureData textureData = eye_tex.SwapTextureSet.Textures[tex_idx];

                // Create a managed Texture2D, based on the unmanaged texture pointer.
                eye_tex.Textures[tex_idx] = new Texture2D(textureData.Texture);

                // Create a render target view for the current Texture2D.
                eye_tex.RenderTargetViews[tex_idx] = new RenderTargetView(device, eye_tex.Textures[tex_idx]);
            }

            // Define the depth buffer, at the size recommended for the eye texture.
            eye_tex.DepthBufferDescription = DefineEyeDepthBufferDescription(eye_tex);

            // Create the depth buffer.
            eye_tex.DepthBuffer = new Texture2D(device, eye_tex.DepthBufferDescription);
            eye_tex.DepthStencilView = new DepthStencilView(device, eye_tex.DepthBuffer);

            // Specify the texture to show on the HMD.
            layer_eye_fov.ColorTexture[eye_idx] = eye_tex.SwapTextureSet.SwapTextureSetPtr;
            layer_eye_fov.Viewport[eye_idx].Position = new OculusWrap.OVR.Vector2i(0, 0);
            layer_eye_fov.Viewport[eye_idx].Size = eye_tex.TextureSize;
            layer_eye_fov.Fov[eye_idx] = eye_tex.FieldOfView;
            layer_eye_fov.Header.Flags = OculusWrap.OVR.LayerFlags.None;
        }
    }

    void CreateMirrorTexture(Control control)
    {
        OculusWrap.OVR.ovrResult result;

        // Define the texture used to display the rendered result on the computer monitor.
        Texture2DDescription mirrorTextureDescription = DefineMirrorTextureDescription(control);

        // Convert the SharpDX texture description to the native Direct3D texture description.
        OculusWrap.OVR.D3D11.D3D11_TEXTURE2D_DESC mirrorTextureDescriptionD3D11 = SharpDXHelpers.CreateTexture2DDescription(mirrorTextureDescription);

        OculusWrap.D3D11.MirrorTexture mirrorTexture;

        // Create the texture used to display the rendered result on the computer monitor.
        result = hmd.CreateMirrorTextureD3D11(device.NativePointer, ref mirrorTextureDescriptionD3D11, out mirrorTexture);
        WriteErrorDetails(oculus, result, "Failed to create mirror texture.");

        mtex = new Texture2D(mirrorTexture.Texture.Texture);
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
        desc.DepthComparison = Comparison.LessEqual;

        default_depth_stencil_state = new DepthStencilState(device, desc);
    }

    RasterizerState default_rasterizer_state;

    void DefineRasterizerState()
    {
        var desc = RasterizerStateDescription.Default();

        desc.IsFrontCounterClockwise = true;

        default_rasterizer_state = new RasterizerState(device, desc);
    }

    public void OnUserResized()
    {
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

    //隠密であるか
    bool sneaked = false;

    /// <summary>
    /// 次のシーンフレームに進みます。
    /// </summary>
    public void FrameMove()
    {
        if (motion_enabled)
        {
            int frame_len = figures.GetMaxFrameLength();
            if (frame_len > 0)
            {
                long dt = DateTime.Now.Ticks - start_ticks;
                int new_frame_index = (int)((start_frame_index + dt / wait) % frame_len);
                Debug.Assert(new_frame_index >= 0);
                Debug.Assert(new_frame_index < frame_len);
                this.frame_index = new_frame_index;
            }
            figures.SetFrameIndex(this.frame_index);
            figures.UpdateBoneMatrices(true);
        }

        KeyboardState currentKeyboardState = keyboard.GetCurrentState();

        if (!keyboardState.IsPressed(Key.LeftControl) && currentKeyboardState.IsPressed(Key.LeftControl))
            sneaked = !sneaked;
        if (!keyboardState.IsPressed(Key.RightControl) && currentKeyboardState.IsPressed(Key.RightControl))
            sneaked = !sneaked;

        if (currentKeyboardState.IsPressed(Key.Left))
            main_camera.RotY(+ocu_config.Angle);
        if (currentKeyboardState.IsPressed(Key.Right))
            main_camera.RotY(-ocu_config.Angle);

        {
            float speed = sneaked ? ocu_config.Sneak.Speed : ocu_config.Speed;

            if (currentKeyboardState.IsPressed(Key.W))
                main_camera.Move(0, 0, -speed);
            if (currentKeyboardState.IsPressed(Key.S))
                main_camera.Move(0, 0, +speed);
            if (currentKeyboardState.IsPressed(Key.A))
                main_camera.Move(-speed, 0, 0);
            if (currentKeyboardState.IsPressed(Key.D))
                main_camera.Move(+speed, 0, 0);
        }

        {
            float y = sneaked ? ocu_config.Sneak.Y : ocu_config.Position.Y;

            main_camera.Position.Y = hmd.GetFloat(OculusWrap.OVR.OVR_KEY_EYE_HEIGHT, 0) + y;
        }

        keyboardState = currentKeyboardState;
    }

    public void Render()
    {
        if (hmd == null)
            return;

        OculusWrap.OVR.Vector3f[] hmdToEyeViewOffsets = { eye_texes[0].HmdToEyeViewOffset, eye_texes[1].HmdToEyeViewOffset };
        OculusWrap.OVR.FrameTiming frameTiming = hmd.GetFrameTiming(0);
        OculusWrap.OVR.TrackingState trackingState = hmd.GetTrackingState(frameTiming.DisplayMidpointSeconds);
        OculusWrap.OVR.Posef[] exe_poses = new OculusWrap.OVR.Posef[2];

        // Calculate the position and orientation of each eye.
        oculus.CalcEyePoses(trackingState.HeadPose.ThePose, hmdToEyeViewOffsets, ref exe_poses);

        UVSCR_variable.Set(UVSCR());

        for (int eye_idx = 0; eye_idx < 2; eye_idx++)
        {
            OculusWrap.OVR.EyeType eye = (OculusWrap.OVR.EyeType)eye_idx;
            EyeTexture eye_tex = eye_texes[eye_idx];

            layer_eye_fov.RenderPose[eye_idx] = exe_poses[eye_idx];

            // Retrieve the index of the active texture and select the next texture as being active next.
            int tex_idx = eye_tex.SwapTextureSet.CurrentIndex++;

            ctx.OutputMerger.SetRenderTargets(eye_tex.DepthStencilView, eye_tex.RenderTargetViews[tex_idx]);

            ctx.ClearDepthStencilView(eye_tex.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            ctx.ClearRenderTargetView(eye_tex.RenderTargetViews[tex_idx], ScreenColor);

            ctx.Rasterizer.SetViewport(eye_tex.Viewport);

            UpdateTransform(exe_poses[eye_idx], eye_tex.FieldOfView);

            DrawFigure();
        }

        hmd.SubmitFrame(0, layers);

        ctx.CopyResource(mtex, buf0);
        swap_chain.Present(0, PresentFlags.None);
    }

    Camera main_camera;
    public Camera Camera
    {
        get
        {
            return main_camera;
        }
    }

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

    void UpdateTransform(OculusWrap.OVR.Posef eye_pose, OculusWrap.OVR.FovPort fov)
    {
        // Retrieve the eye rotation quaternion and use it to calculate the LookAt direction and the LookUp direction.
        Camera camera = new Camera()
        {
            Position = main_camera.Position + Vector3.Transform(eye_pose.Position.ToVector3(), main_camera.Rotation),
            Rotation = main_camera.Rotation * SharpDXHelpers.ToQuaternion(eye_pose.Orientation),
        };

        world_matrix = Matrix.Scaling(ocu_config.Scale);
        Transform_View = camera.GetViewMatrix();
        Transform_Projection = OculusWrap.OVR.ovrMatrix4f_Projection(fov, ocu_config.Znear, ocu_config.Zfar, OculusWrap.OVR.ProjectionModifier.RightHanded).ToMatrix();
        Transform_Projection.Transpose();

        Matrix world_view_matrix = world_matrix * Transform_View;
        world_view_projection_matrix = world_view_matrix * Transform_Projection;

        World_variable.SetMatrix(world_matrix);
        WorldView_variable.SetMatrix(world_view_matrix);
        WorldViewProjection_variable.SetMatrix(world_view_projection_matrix);
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

    void DrawFigure()
    {
        foreach (Figure fig in figures.FigureList)
        {
            DrawFigure(fig);
        }
    }

    void DrawFigure(Figure fig)
    {
        LightDirForced_variable.Set(fig.LightDirForced());
        foreach (TSOFile tso in fig.TSOFileList)
        {
            int current_spec = -1;

            foreach (TSOMesh mesh in tso.meshes)
                foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
                {
                    TSOSubScript scr = tso.sub_scripts[sub_mesh.spec];

                    if (sub_mesh.spec != current_spec)
                    {
                        current_spec = sub_mesh.spec;

                        cb_variable.SetConstantBuffer(scr.cb);

                        TSOTex shadeTex;
                        if (tso.texmap.TryGetValue(scr.shader.ShadeTexName, out shadeTex))
                            ShadeTex_texture_variable.SetResource(shadeTex.d3d_tex_view);

                        TSOTex colorTex;
                        if (tso.texmap.TryGetValue(scr.shader.ColorTexName, out colorTex))
                            ColorTex_texture_variable.SetResource(colorTex.d3d_tex_view);
                    }

                    int technique_idx = scr.shader.technique_idx;

                    var technique = effect.GetTechniqueByIndex(technique_idx);
                    if (!technique.IsValid)
                    {
                        string technique_name = scr.shader.technique_name;
                        Console.WriteLine("technique {0} is not valid", technique_name);
                        continue;
                    }

                    LocalBoneMats_variable.SetMatrix(fig.ClipBoneMatrices(sub_mesh));

                    if (!technique.GetPassByIndex(0).IsValid)
                    {
                        Console.WriteLine("pass #0 is not valid");
                        continue;
                    }

                    ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
                    ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(sub_mesh.vb, 52, 0));

                    for (int i = 0; i < technique.Description.PassCount; i++)
                    {
                        ctx.OutputMerger.SetBlendState(default_blend_state);
                        ctx.OutputMerger.SetDepthStencilState(default_depth_stencil_state);
                        ctx.Rasterizer.State = default_rasterizer_state;

                        technique.GetPassByIndex(i).Apply(ctx);
                        ctx.Draw(sub_mesh.vertices.Length, 0);
                    }
                }
        }
    }

    public void Dispose()
    {
        // Release all resources

        figures.Dispose();

        if (ctx != null)
        {
            ctx.ClearState();
            ctx.Flush();
            ctx.Dispose();
        }

        if (il != null)
            il.Dispose();
        if (effect != null)
            effect.Dispose();

        if (hmd != null)
        {
            keyboard.Dispose();
            directInput.Dispose();

            mtex.Dispose();
            layers.Dispose();

            eye_texes[0].Dispose();
            eye_texes[1].Dispose();

            default_rasterizer_state.Dispose();
            default_depth_stencil_state.Dispose();
            default_blend_state.Dispose();

            ztex_view.Dispose();
            ztex.Dispose();

            buf0_view.Dispose();
            buf0.Dispose();

            swap_chain.Dispose();
            dxgi_factory.Dispose();

            // Disposing the device, before the hmd, will cause the hmd to fail when disposing.
            // Disposing the device, after the hmd, will cause the dispose of the device to fail.
            // It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
            // device.Dispose();

            hmd.Dispose();
        }
        oculus.Dispose();
    }

    SwapChainDescription DefineSwapChainDescription(Control control)
    {
        return new SwapChainDescription()
        {
            BufferCount = 1,
            IsWindowed = true,
            OutputHandle = control.Handle,
            SampleDescription = new SampleDescription(1, 0),
            Usage = Usage.RenderTargetOutput | Usage.ShaderInput,
            SwapEffect = SwapEffect.Sequential,
            Flags = SwapChainFlags.AllowModeSwitch,
            ModeDescription = new ModeDescription()
            {
                Width = control.Width,
                Height = control.Height,
                Format = Format.R8G8B8A8_UNorm,
                RefreshRate = new Rational(0, 1),
            },
        };
    }

    Texture2DDescription DefineDepthBufferDescription(Control control)
    {
        return new Texture2DDescription()
        {
            Format = Format.D32_Float,
            ArraySize = 1,
            MipLevels = 1,
            Width = control.Width,
            Height = control.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
        };
    }

    Texture2DDescription DefineEyeTextureDescription(EyeTexture eye_tex)
    {
        return new Texture2DDescription()
        {
            Width = eye_tex.TextureSize.Width,
            Height = eye_tex.TextureSize.Height,
            ArraySize = 1,
            MipLevels = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            CpuAccessFlags = CpuAccessFlags.None,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
        };
    }

    Texture2DDescription DefineEyeDepthBufferDescription(EyeTexture eye_tex)
    {
        return new Texture2DDescription()
        {
            Format = Format.D32_Float,
            Width = eye_tex.TextureSize.Width,
            Height = eye_tex.TextureSize.Height,
            ArraySize = 1,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
        };
    }

    Texture2DDescription DefineMirrorTextureDescription(Control control)
    {
        return new Texture2DDescription()
        {
            Width = control.Width,
            Height = control.Height,
            ArraySize = 1,
            MipLevels = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            CpuAccessFlags = CpuAccessFlags.None,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
        };
    }

    /// <summary>
    /// Write out any error details received from the Oculus SDK, into the debug output window.
    /// 
    /// Please note that writing text to the debug output window is a slow operation and will affect performance,
    /// if too many messages are written in a short timespan.
    /// </summary>
    /// <param name="oculus">OculusWrap object for which the error occurred.</param>
    /// <param name="result">Error code to write in the debug text.</param>
    /// <param name="message">Error message to include in the debug text.</param>
    public void WriteErrorDetails(OculusWrap.Wrap oculus, OculusWrap.OVR.ovrResult result, string message)
    {
        if (result >= OculusWrap.OVR.ovrResult.Success)
            return;

        // Retrieve the error message from the last occurring error.
        OculusWrap.OVR.ovrErrorInfo errorInformation = oculus.GetLastError();

        string formattedMessage = string.Format("{0}. Message: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
        Trace.WriteLine(formattedMessage);

        throw new Exception(formattedMessage);
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
