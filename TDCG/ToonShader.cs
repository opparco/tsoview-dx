using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D11;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace TDCG
{
    /// 定数バッファに書き込む構造体
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PassDescription
    {
        internal float ReferenceAlpha;
        internal float p1;
        internal float p2;
        internal float p3;
    }

    public class ToonShader : IDisposable
    {
        Device device;
        Effect effect;

        Dictionary<string, int> techmap = new Dictionary<string, int>();

        EffectConstantBuffer cb_variable;
        EffectConstantBuffer cb_per_pass_variable;

        EffectShaderResourceVariable ShadeTex_texture_variable;
        EffectShaderResourceVariable ColorTex_texture_variable;

        /// <summary>
        /// Direct3D定数バッファ
        /// </summary>
        public Buffer cb = null;
        public Buffer cb_per_pass = null;

        Shader current_shader = null;

        PassDescription pass_desc = new PassDescription()
        {
            ReferenceAlpha = 0.25f,
            p1 = 0.0f,
            p2 = 0.0f,
            p3 = 0.0f,
        };

        public ToonShader(Device device, Effect effect)
        {
            this.device = device;
            this.effect = effect;

            cb_variable = effect.GetConstantBufferByName("cb");
            cb_per_pass_variable = effect.GetConstantBufferByName("cb_per_pass");

            ShadeTex_texture_variable = effect.GetVariableByName("ShadeTex_texture").AsShaderResource();
            ColorTex_texture_variable = effect.GetVariableByName("ColorTex_texture").AsShaderResource();

            string techmap_file = Path.Combine(Application.StartupPath, @"techmap.txt");
            if (!File.Exists(techmap_file))
            {
                Console.WriteLine("File not found: " + techmap_file);
                return;
            }
            LoadTechmap(techmap_file);

            cb = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = 96,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ConstantBuffer,
            });
            cb_variable.SetConstantBuffer(cb);

            cb_per_pass = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = 16,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ConstantBuffer,
            });
            cb_per_pass_variable.SetConstantBuffer(cb_per_pass);
        }

        /// techmapを読み込みます。
        public void LoadTechmap(string path)
        {
            StreamReader file = new StreamReader(path);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split('\t');
                string name = words[0];
                int idx = int.Parse(words[1]);
                techmap[name] = idx;
            }
            file.Close();
        }

        /// シェーダ設定を解除します。
        public void RemoveShader()
        {
            current_shader = null;
        }

        EffectTechnique technique;
        public EffectTechnique Technique { get { return technique; } }

        /// <summary>
        /// シェーダ設定を切り替えます。
        /// </summary>
        /// <param name="shader">シェーダ設定</param>
        public void SwitchShader(Shader shader, Func<string, ShaderResourceView> fetch_d3d_texture_SR_view)
        {
            if (shader == current_shader)
                return;
            current_shader = shader;

            shader.Sync();

            //
            // rewrite constant buffer
            //
            device.ImmediateContext.UpdateSubresource(ref shader.desc, cb);

            {
                ShaderResourceView d3d_tex_SR_view = fetch_d3d_texture_SR_view(shader.ShadeTexName);
                if (d3d_tex_SR_view != null)
                    ShadeTex_texture_variable.SetResource(d3d_tex_SR_view);
            }

            {
                ShaderResourceView d3d_tex_SR_view = fetch_d3d_texture_SR_view(shader.ColorTexName);
                if (d3d_tex_SR_view != null)
                    ColorTex_texture_variable.SetResource(d3d_tex_SR_view);
            }

            try
            {
                int idx = techmap[shader.technique_name];
                technique = effect.GetTechniqueByIndex(idx);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Error: shader technique not found. " + shader.technique_name);
                return;
            }
        }

        public void UpdateConstantBuffer(EffectPass pass)
        {
            using (EffectVariable annotation = pass.GetAnnotationByName("ReferenceAlpha"))
                pass_desc.ReferenceAlpha = annotation.IsValid ? annotation.AsScalar().GetFloat() : 0.25f;

            //
            // rewrite constant buffer
            //
            device.ImmediateContext.UpdateSubresource(ref pass_desc, cb_per_pass);
        }

        /// <summary>
        /// Direct3Dバッファを破棄します。
        /// </summary>
        public void Dispose()
        {
            if (cb_per_pass != null)
                cb_per_pass.Dispose();

            if (cb != null)
                cb.Dispose();

            this.technique = null;

            ColorTex_texture_variable?.Dispose();
            ShadeTex_texture_variable?.Dispose();
            cb_variable?.Dispose();

            this.effect = null;
            this.device = null;
        }
    }
}
