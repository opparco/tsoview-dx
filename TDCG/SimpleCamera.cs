using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SharpDX;
using SharpDX.Direct3D11;

namespace TDCG
{
    /// <summary>
    /// カメラ
    /// </summary>
    public class SimpleCamera
    {
        //角度
        Vector3 angle;
        
        //回転中心
        Vector3 center;
        
        //位置変位
        Vector3 translation;
        
        //更新する必要があるか
        bool needUpdate;
        
        //view行列
        Matrix view;
        
        /// <summary>
        /// 角度
        /// </summary>
        public Vector3 Angle { get { return angle; } set { angle = value; } }

        /// <summary>
        /// 回転中心
        /// </summary>
        public Vector3 Center { get { return center; } set { center = value; } }

        /// <summary>
        /// 位置変位
        /// </summary>
        public Vector3 Translation { get { return translation; } set { translation = value; } }
    
        /// <summary>
        /// 更新する必要があるか
        /// </summary>
        public bool NeedUpdate { get { return needUpdate; } }

        /// <summary>
        /// view行列
        /// </summary>
        public Matrix ViewMatrix { get { return view; } }

        /// <summary>
        /// カメラを生成します。
        /// </summary>
        public SimpleCamera()
        {
            angle = Vector3.Zero;
            center = Vector3.Zero;
            translation = new Vector3(0.0f, 0.0f, +10.0f);
            needUpdate = true;
            view = Matrix.Identity;
        }

        /// <summary>
        /// カメラの位置と姿勢をリセットします。
        /// </summary>
        public void Reset()
        {
            center = Vector3.Zero;
            translation = new Vector3(0.0f, 0.0f, +10.0f);
            angle = Vector3.Zero;
            needUpdate = true;
        }

        /// <summary>
        /// view座標上のカメラの位置をリセットします。
        /// </summary>
        public void ResetTranslation()
        {
            translation = new Vector3(0.0f, 0.0f, +10.0f);
            needUpdate = true;
        }

        /// <summary>
        /// カメラを回転します。
        /// </summary>
        /// <param name="yaw">Y軸回転量</param>
        /// <param name="pitch">X軸回転量</param>
        /// <param name="roll">Z軸回転量</param>
        public void RotateYawPitchRoll(float yaw, float pitch, float roll)
        {
            Vector3 delta = new Vector3(pitch, yaw, roll);
            if (delta != Vector3.Zero)
            {
                angle += delta;
                needUpdate = true;
            }
        }

        /// <summary>
        /// カメラの位置と姿勢を更新します。
        /// </summary>
        public void Update()
        {
            if (!needUpdate)
                return;

            Matrix m = Matrix.RotationYawPitchRoll(angle.Y, angle.X, angle.Z);
            m.M41 = center.X;
            m.M42 = center.Y;
            m.M43 = center.Z;
            m.M44 = 1;

            view = Matrix.Invert(Matrix.Translation(translation) * m);

            needUpdate = false;
        }

        /// <summary>
        /// view行列を取得します。
        /// </summary>
        public Matrix GetViewMatrix()
        {
            return view;
        }

        /// <summary>
        /// 回転中心を設定します。
        /// </summary>
        /// <param name="center">回転中心</param>
        public void SetCenter(Vector3 center)
        {
            this.center = center;
            needUpdate = true;
        }
        /// <summary>
        /// 回転中心を設定します。
        /// </summary>
        /// <param name="x">回転中心x座標</param>
        /// <param name="y">回転中心y座標</param>
        /// <param name="z">回転中心z座標</param>
        public void SetCenter(float x, float y, float z)
        {
            SetCenter(new Vector3(x, y, z));
        }

        /// <summary>
        /// view座標上の位置を設定します。
        /// </summary>
        /// <param name="translation">view座標上の位置</param>
        public void SetTranslation(Vector3 translation)
        {
            this.translation = translation;
            needUpdate = true;
        }
        /// <summary>
        /// 位置変位を設定します。
        /// </summary>
        /// <param name="x">X変位</param>
        /// <param name="y">Y変位</param>
        /// <param name="z">Z変位</param>
        public void SetTranslation(float x, float y, float z)
        {
            SetTranslation(new Vector3(x, y, z));
        }

        /// <summary>
        /// 角度を設定します。
        /// </summary>
        /// <param name="angle">角度</param>
        public void SetAngle(Vector3 angle)
        {
            this.angle = angle;
            needUpdate = true;
        }
        /// <summary>
        /// 角度を設定します。
        /// </summary>
        /// <param name="x">X軸回転角</param>
        /// <param name="y">Y軸回転角</param>
        /// <param name="z">Z軸回転角</param>
        public void SetAngle(float x, float y, float z)
        {
            SetAngle(new Vector3(x, y, z));
        }

        /// <summary>
        /// view座標上で移動します。
        /// </summary>
        /// <param name="x">X軸移動距離</param>
        /// <param name="y">Y軸移動距離</param>
        /// <param name="z">Z軸移動距離</param>
        public void MoveView(float x, float y, float z)
        {
            Vector3 delta = new Vector3(x, y, z);
            if (delta != Vector3.Zero)
            {
                this.translation += delta;
                needUpdate = true;
            }
        }
    }
}
