using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;
using SharpDX;
using SharpDX.Direct3D11;

namespace TDCG
{
    /// <summary>
    /// シェーダ設定の型名
    /// </summary>
    public enum ShaderParameterType
    {
        /// <summary>
        /// わからない
        /// </summary>
        Unknown,
        /// <summary>
        /// string
        /// </summary>
        String,
        /// <summary>
        /// float
        /// </summary>
        Float,
        /// <summary>
        /// float3
        /// </summary>
        Float3,
        /// <summary>
        /// float4
        /// </summary>
        Float4,
        /// <summary>
        /// テクスチャ
        /// </summary>
        Texture
    };

    /// <summary>
    /// シェーダ設定パラメータ
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShaderParameter
    {
        internal ShaderParameterType type;
        internal string name;

        private string str;
        private float f1;
        private float f2;
        private float f3;
        private float f4;
        private int dim = 0;

        /// <summary>
        /// パラメータの名称
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        /// <summary>
        /// float値1
        /// </summary>
        public float F1 { get { return f1; } set { f1 = value; } }
        /// <summary>
        /// float値2
        /// </summary>
        public float F2 { get { return f2; } set { f2 = value; } }
        /// <summary>
        /// float値3
        /// </summary>
        public float F3 { get { return f3; } set { f3 = value; } }
        /// <summary>
        /// float値4
        /// </summary>
        public float F4 { get { return f4; } set { f4 = value; } }
        /// <summary>
        /// float次元数
        /// </summary>
        public int Dimension { get { return dim; } }

        /// <summary>
        /// シェーダ設定ファイルの行を解析してシェーダ設定パラメータを生成します。
        /// </summary>
        /// <param name="line">シェーダ設定ファイルの行</param>
        /// <returns>シェーダ設定パラメータ</returns>
        public static ShaderParameter Parse(string line)
        {
            int m = line.IndexOf('='); if (m < 0) throw new ArgumentException();
            string type_name = line.Substring(0,m);
            string value = line.Substring(m+1).Trim();
            m = type_name.IndexOf(' '); if (m < 0) throw new ArgumentException();
            string type = type_name.Substring(0,m);
            string name = type_name.Substring(m+1).Trim();

            return new ShaderParameter(type, name, value);
        }

        /// <summary>
        /// シェーダ設定パラメータを生成します。
        /// </summary>
        public ShaderParameter()
        {
        }

        /// <summary>
        /// シェーダ設定パラメータを生成します。
        /// </summary>
        /// <param name="type_string">型名</param>
        /// <param name="name">名称</param>
        /// <param name="value">値</param>
        public ShaderParameter(string type_string, string name, string value)
        {
            this.name = name;

            switch (type_string)
            {
            case "string":
                type = ShaderParameterType.String;
                SetString(value);
                break;
            case "float":
                type = ShaderParameterType.Float;
                SetFloat(value);
                break;
            case "float3":
                type = ShaderParameterType.Float3;
                SetFloat3(value);
                break;
            case "float4":
                type = ShaderParameterType.Float4;
                SetFloat4(value);
                break;
            case "texture":
                type = ShaderParameterType.Texture;
                SetString(value);
                break;
            default:
                type = ShaderParameterType.Unknown;
                break;
            }
        }

        /// 文字列として表現します。
        public override string ToString()
        {
            return GetTypeName() + " " + name + " = " + GetValueString();
        }

        /// 型名を文字列として得ます。
        public string GetTypeName()
        {
            switch (type)
            {
                case ShaderParameterType.String:
                    return "string";
                case ShaderParameterType.Float:
                    return "float";
                case ShaderParameterType.Float3:
                    return "float3";
                case ShaderParameterType.Float4:
                    return "float4";
                case ShaderParameterType.Texture:
                    return "texture";
            }
            return null;
        }

        /// <summary>
        /// 値を文字列として得ます。
        /// </summary>
        public string GetValueString()
        {
            switch (type)
            {
                case ShaderParameterType.String:
                    return "\"" + str + "\"";
                case ShaderParameterType.Float:
                    return string.Format("[{0}]", f1);
                case ShaderParameterType.Float3:
                    return string.Format("[{0}, {1}, {2}]", f1, f2, f3);
                case ShaderParameterType.Float4:
                    return string.Format("[{0}, {1}, {2}, {3}]", f1, f2, f3, f4);
                case ShaderParameterType.Texture:
                    return str;
            }
            return str;
        }

        /// <summary>
        /// 文字列を取得します。
        /// </summary>
        /// <returns>文字列</returns>
        public string GetString()
        {
            return str;
        }

        /// <summary>
        /// 文字列を設定します。
        /// </summary>
        /// <param name="value">文字列表現</param>
        public void SetString(string value)
        {
            str = value.Trim('"', ' ', '\t');
        }

        static Regex re_float_array = new Regex(@"\s*,\s*|\s+");

        /// <summary>
        /// float値の配列を設定します。
        /// </summary>
        /// <param name="value">float配列値の文字列表現</param>
        /// <param name="dim">次元数</param>
        public void SetFloatDim(string value, int dim)
        {
            string[] token = re_float_array.Split(value.Trim('[', ']', ' ', '\t'));
            this.dim = dim;
            if (token.Length > 0)
                f1 = float.Parse(token[0].Trim());
            if (token.Length > 1)
                f2 = float.Parse(token[1].Trim());
            if (token.Length > 2)
                f3 = float.Parse(token[2].Trim());
            if (token.Length > 3)
                f4 = float.Parse(token[3].Trim());
        }

        /// <summary>
        /// float値を取得します。
        /// </summary>
        /// <returns>float値</returns>
        public float GetFloat()
        {
            return f1;
        }
        /// <summary>
        /// float値を設定します。
        /// </summary>
        /// <param name="value">float値の文字列表現</param>
        public void SetFloat(string value)
        {
            try
            {
                SetFloatDim(value, 1);
            }
            catch (FormatException)
            {
                Console.WriteLine("shader format error (type float): " + value);
            }
        }

        /// <summary>
        /// float3値を取得します。
        /// </summary>
        /// <returns>float3値</returns>
        public Vector3 GetFloat3()
        {
            return new Vector3(f1, f2, f3);
        }
        /// <summary>
        /// float3値を設定します。
        /// </summary>
        /// <param name="value">float3値の文字列表現</param>
        public void SetFloat3(string value)
        {
            try
            {
                SetFloatDim(value, 3);
            }
            catch (FormatException)
            {
                Console.WriteLine("shader format error (type float3): " + value);
            }
        }

        /// <summary>
        /// float4値を取得します。
        /// </summary>
        /// <returns>float4値</returns>
        public Vector4 GetFloat4()
        {
            return new Vector4(f1, f2, f3, f4);
        }
        /// <summary>
        /// float4値を設定します。
        /// </summary>
        /// <param name="value">float4値の文字列表現</param>
        public void SetFloat4(string value)
        {
            try
            {
                SetFloatDim(value, 4);
            }
            catch (FormatException)
            {
                Console.WriteLine("shader format error (type float4): " + value);
            }
        }
    }

    /// 定数バッファに書き込む構造体
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ShaderDescription
    {
        internal float Ambient;
        internal float Thickness;
        internal float ColorBlend;
        internal float ShadeBlend;

        internal float HighLight;
        internal float HighLightBlend;
        internal float HighLightPower;
        /* for Tessellation */
        internal float TessFactor;

        /* for HLMap */
        internal float FrontLightPower;
        internal float BackLightPower;
        /* for UVScroll */
        internal float UVScrollX;
        internal float UVScrollY;

        internal Vector4 PenColor;
        //internal Vector4 ShadowColor = new Vector4(0, 0, 0, 1);
        //internal Vector4 ManColor = new Vector4(0, 1, 1, 0.4f);

        /* for HLMap */
        internal Vector4 FrontLight;
        internal Vector4 BackLight;
    }

    /// <summary>
    /// シェーダ設定
    /// </summary>
    public class Shader
    {
        /// <summary>
        /// シェーダ設定パラメータの配列
        /// </summary>
        public ShaderParameter[] shader_parameters;

        //internal string     description;     // = "TA ToonShader v0.50"
        //internal string     shader;          // = "TAToonshade_050.cgfx"

        /// テクニック名
        public string technique_name;

        /// テクニック名に対応するindex
        /// techmapにより代入されます。
        public int technique_idx = 0;

        internal ShaderDescription desc = new ShaderDescription()
        {
            Ambient = 0.0f,
            Thickness = 0.001f,
            ColorBlend = 10.0f,
            ShadeBlend = 10.0f,

            HighLight = 0.0f,
            HighLightBlend = 10.0f,
            HighLightPower = 100.0f,
            TessFactor = 1.0f,

            FrontLightPower = 0.1f,
            BackLightPower = 0.4f,
            UVScrollX = 0.0f,
            UVScrollY = 0.0f,

            PenColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),

            FrontLight = new Vector4(0.9f, 0.9f, 0.9f, 0.2f),
            BackLight = new Vector4(0.2f, 0.4f, 0.5f, 0.6f),
        };

        /// 陰テクスチャ名
        public string ShadeTexName;
        /// 色テクスチャ名
        public string ColorTexName;

        /// <summary>
        /// シェーダ設定を読み込みます。
        /// </summary>
        /// <param name="lines">テキスト行配列</param>
        public void Load(string[] lines)
        {
            shader_parameters = new ShaderParameter[lines.Length];
            int i = 0;
            foreach (string line in lines)
            {
                ShaderParameter p = ShaderParameter.Parse(line);
                switch (p.name)
                {
                    case "description":
                        break;
                    case "shader":
                        break;
                    case "technique":
                        technique_name = p.GetString();
                        break;
                    case "Ambient":
                        desc.Ambient = p.GetFloat();
                        break;
                    case "Thickness":
                        desc.Thickness = p.GetFloat();
                        break;
                    case "ColorBlend":
                        desc.ColorBlend = p.GetFloat();
                        break;
                    case "ShadeBlend":
                        desc.ShadeBlend = p.GetFloat();
                        break;
                    case "HighLight":
                        desc.HighLight = p.GetFloat();
                        break;
                    case "HighLightBlend":
                        desc.HighLightBlend = p.GetFloat();
                        break;
                    case "HighLightPower":
                        desc.HighLightPower = p.GetFloat();
                        break;
                    case "TessFactor":
                        desc.TessFactor = p.GetFloat();
                        break;
                    case "PenColor":
                        desc.PenColor = p.GetFloat4();
                        break;
                    case "FrontLight":
                        desc.FrontLight = p.GetFloat4();
                        break;
                    case "FrontLightPower":
                        desc.FrontLightPower = p.GetFloat();
                        break;
                    case "BackLight":
                        desc.BackLight = p.GetFloat4();
                        break;
                    case "BackLightPower":
                        desc.BackLightPower = p.GetFloat();
                        break;
                    case "UVScrollX":
                        desc.UVScrollX = p.GetFloat();
                        break;
                    case "UVScrollY":
                        desc.UVScrollY = p.GetFloat();
                        break;
                    case "ShadeTex":
                        ShadeTexName = p.GetString();
                        break;
                    case "ColorTex":
                        ColorTexName = p.GetString();
                        break;
                    case "LightDirX":
                    case "LightDirY":
                    case "LightDirZ":
                    case "LightDirW":
                    case "ShadowColor":
                        break;
                }
                shader_parameters[i++] = p;
            }
            Array.Resize(ref shader_parameters, i);
        }

        // Set shader parameters to description.
        public void Sync()
        {
            foreach (ShaderParameter p in shader_parameters)
            {
                if (p.Name == "TessFactor")
                    desc.TessFactor = p.GetFloat();
            }
        }

        /// <summary>
        /// シェーダ設定を文字列の配列として得ます。
        /// </summary>
        public string[] GetLines()
        {
            string[] lines = new string[shader_parameters.Length];
            int i = 0;
            foreach (ShaderParameter p in shader_parameters)
            {
                lines[i++] = p.ToString();
            }
            Array.Resize(ref lines, i);
            return lines;
        }
    }
}
