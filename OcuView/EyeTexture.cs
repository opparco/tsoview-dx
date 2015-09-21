using System;
using System.Diagnostics;
using System.Windows.Forms;
//using OculusWrap;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace OcuView
{
    /// <summary>
    /// Contains all the fields used by each eye.
    /// </summary>
    public class EyeTexture : IDisposable
    {
        #region IDisposable Members
        /// <summary>
        /// Dispose contained fields.
        /// </summary>
        public void Dispose()
        {
            if (SwapTextureSet != null)
            {
                SwapTextureSet.Dispose();
                SwapTextureSet = null;
            }

            if (Textures != null)
            {
                foreach (Texture2D texture in Textures)
                    texture.Dispose();

                Textures = null;
            }

            if (RenderTargetViews != null)
            {
                foreach (RenderTargetView renderTargetView in RenderTargetViews)
                    renderTargetView.Dispose();

                RenderTargetViews = null;
            }

            if (DepthBuffer != null)
            {
                DepthBuffer.Dispose();
                DepthBuffer = null;
            }

            if (DepthStencilView != null)
            {
                DepthStencilView.Dispose();
                DepthStencilView = null;
            }
        }
        #endregion

        public Texture2DDescription Texture2DDescription;
        public OculusWrap.D3D11.SwapTextureSet SwapTextureSet;
        public Texture2D[] Textures;
        public RenderTargetView[] RenderTargetViews;
        public Texture2DDescription DepthBufferDescription;
        public Texture2D DepthBuffer;
        public Viewport Viewport;
        public DepthStencilView DepthStencilView;
        public OculusWrap.OVR.FovPort FieldOfView;
        public OculusWrap.OVR.Sizei TextureSize;
        public OculusWrap.OVR.Recti ViewportSize;
        public OculusWrap.OVR.EyeRenderDesc RenderDescription;
        public OculusWrap.OVR.Vector3f HmdToEyeViewOffset;
    }
}
