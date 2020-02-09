using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;

namespace TDCG
{
    /// スライダ変形行列
    public class SliderMatrix
    {
        /// 姉妹スライダ0.0でのlocal scaling factor
        public static Vector3 GetMinLocal()
        {
            float scale = 0.9520f;
            return new Vector3(scale, scale, scale);
        }

        /// 姉妹スライダ1.0でのlocal scaling factor
        public static Vector3 GetMaxLocal()
        {
            float scale = 1.05f;
            return new Vector3(scale, scale, scale);
        }

        /// 姉妹スライダ0.0でのface_oya scaling factor
        public static Vector3 GetMinFaceOya()
        {
            return new Vector3(1.224272f, 1.066630f, 1.224272f);
        }

        /// 姉妹スライダ1.0でのface_oya scaling factor
        public static Vector3 GetMaxFaceOya()
        {
            return new Vector3(0.967304f, 1.025342f, 0.967304f);
        }

        /// 胴まわりスライダ0.0でのW_Spine_Dummy scaling factor
        public static Vector3 GetMinSpineDummy()
        {
            return new Vector3(1.0f, 1.0f, 1.0f);
        }

        /// 胴まわりスライダ1.0でのW_Spine_Dummy scaling factor
        public static Vector3 GetMaxSpineDummy()
        {
            return new Vector3(1.0890f, 1.0f, 0.9235f);
        }

        /// 胴まわりスライダ0.0でのW_Spine1 scaling factor
        public static Vector3 GetMinSpine1()
        {
            return new Vector3(1.0f, 1.0f, 1.0f);
        }

        /// 胴まわりスライダ1.0でのW_Spine1 scaling factor
        public static Vector3 GetMaxSpine1()
        {
            return new Vector3(1.1800f, 1.0f, 1.0f);
        }

        /// あしスライダ0.0でのW_(LR)Hips_Dummy scaling factor
        public static Vector3 GetMinHipsDummy()
        {
            return new Vector3(1.0f, 1.0f, 1.0f);
        }

        /// あしスライダ1.0でのW_(LR)Hips_Dummy scaling factor
        public static Vector3 GetMaxHipsDummy()
        {
            return new Vector3(1.2001f, 1.0f, 1.0f);
        }

        /// あしスライダ0.0でのW_(LR)UpLeg scaling factor
        public static Vector3 GetMinUpLeg()
        {
            return new Vector3(0.8091f, 1.0f, 0.8190f);
        }

        /// あしスライダ1.0でのW_(LR)UpLeg scaling factor
        public static Vector3 GetMaxUpLeg()
        {
            return new Vector3(1.2001f, 1.0f, 1.0f);
        }

        /// あしスライダ0.0でのW_(LR)UpLegRoll scaling factor
        public static Vector3 GetMinUpLegRoll()
        {
            return new Vector3(0.8091f, 1.0f, 0.8190f);
        }

        /// あしスライダ1.0でのW_(LR)UpLegRoll scaling factor
        public static Vector3 GetMaxUpLegRoll()
        {
            return new Vector3(1.2012f, 1.0f, 1.0f);
        }

        /// あしスライダ0.0でのW_(LR)LegRoll scaling factor
        public static Vector3 GetMinLegRoll()
        {
            return new Vector3(0.8091f, 1.0f, 0.8190f);
        }

        /// あしスライダ1.0でのW_(LR)LegRoll scaling factor
        public static Vector3 GetMaxLegRoll()
        {
            return new Vector3(0.9878f, 1.0f, 1.0f);
        }

        /// うでスライダ0.0でのW_(LR)Arm_Dummy scaling factor
        public static Vector3 GetMinArmDummy()
        {
            return new Vector3(1.0f, 1.0f, 1.0f);
        }

        /// うでスライダ1.0でのW_(LR)Arm_Dummy scaling factor
        public static Vector3 GetMaxArmDummy()
        {
            return new Vector3(1.0f, 1.1760f, 1.0f);
        }

        /// うでスライダ0.0でのW_(LR)Arm scaling factor
        public static Vector3 GetMinArm()
        {
            return new Vector3(1.0f, 0.7350f, 1.0f);
        }

        /// うでスライダ1.0でのW_(LR)Arm scaling factor
        public static Vector3 GetMaxArm()
        {
            return new Vector3(1.0f, 1.1760f, 1.0f);
        }

        /// 変形行列 ChichiR1 0.0
        public static Matrix GetMinChichiR1()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.838979F;
            m.M12 = 0.000000F;
            m.M13 = 0.092851F;
            m.M14 = 0.000000F;
            m.M21 = 0.014229F;
            m.M22 = 0.991598F;
            m.M23 = -0.128573F;
            m.M24 = 0.000000F;
            m.M31 = -0.050885F;
            m.M32 = 0.060347F;
            m.M33 = 0.459783F;
            m.M34 = 0.000000F;
            m.M41 = -0.040490F;
            m.M42 = 2.234810F;
            m.M43 = -0.433890F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiR2 0.0
        public static Matrix GetMinChichiR2()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.931420F;
            m.M12 = -0.000001F;
            m.M13 = -0.164172F;
            m.M14 = 0.000000F;
            m.M21 = 0.018493F;
            m.M22 = 0.921283F;
            m.M23 = 0.343484F;
            m.M24 = 0.000000F;
            m.M31 = 0.060012F;
            m.M32 = -0.091304F;
            m.M33 = 1.114690F;
            m.M34 = 0.000000F;
            m.M41 = -0.398459F;
            m.M42 = -1.191766F;
            m.M43 = 1.387521F;
            m.M44 = 1.000000F;
            m.M43 += 0.2F;
            float scale = 1.125f;
            m.M41 /= scale;
            m.M42 /= scale;
            m.M43 /= scale;
            m *= Matrix.Scaling(scale, scale, scale);
            return m;
        }

        /// 変形行列 ChichiR3 0.0
        public static Matrix GetMinChichiR3()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.871897F;
            m.M12 = 0.000001F;
            m.M13 = -0.000001F;
            m.M14 = 0.000000F;
            m.M21 = -0.000001F;
            m.M22 = 0.736430F;
            m.M23 = -0.000001F;
            m.M24 = 0.000000F;
            m.M31 = 0.000000F;
            m.M32 = 0.000001F;
            m.M33 = 1.298549F;
            m.M34 = 0.000000F;
            m.M41 = -0.214215F;
            m.M42 = -0.509967F;
            m.M43 = 0.492733F;
            m.M44 = 1.000000F;
            float scale = 1.0f / 1.125f;
            m.M41 /= scale;
            m.M42 /= scale;
            m.M43 /= scale;
            m *= Matrix.Scaling(scale, scale, scale);
            return m;
        }

        /// 変形行列 ChichiR4 0.0
        public static Matrix GetMinChichiR4()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.451919F;
            m.M12 = 0.000000F;
            m.M13 = 0.000000F;
            m.M14 = 0.000000F;
            m.M21 = 0.000000F;
            m.M22 = 1.451918F;
            m.M23 = 0.000000F;
            m.M24 = 0.000000F;
            m.M31 = -0.000001F;
            m.M32 = 0.000000F;
            m.M33 = 1.451921F;
            m.M34 = 0.000000F;
            m.M41 = -0.094427F;
            m.M42 = -0.016221F;
            m.M43 = 0.385749F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiR5 0.0
        public static Matrix GetMinChichiR5()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.000000F;
            m.M12 = 0.000000F;
            m.M13 = 0.000001F;
            m.M14 = 0.000000F;
            m.M21 = 0.000000F;
            m.M22 = 1.000001F;
            m.M23 = 0.000001F;
            m.M24 = 0.000000F;
            m.M31 = 0.000000F;
            m.M32 = 0.000000F;
            m.M33 = 1.000001F;
            m.M34 = 0.000000F;
            m.M41 = -0.064099F;
            m.M42 = 0.044692F;
            m.M43 = 0.238179F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiR5_end 0.0
        public static Matrix GetMinChichiR5E()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.000000F;
            m.M12 = 0.000000F;
            m.M13 = -0.000001F;
            m.M14 = 0.000000F;
            m.M21 = 0.000001F;
            m.M22 = 1.000000F;
            m.M23 = -0.000001F;
            m.M24 = 0.000000F;
            m.M31 = 0.000001F;
            m.M32 = 0.000000F;
            m.M33 = 0.999999F;
            m.M34 = 0.000000F;
            m.M41 = 0.000009F;
            m.M42 = 0.049918F;
            m.M43 = 0.110920F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiL1 0.0
        public static Matrix GetMinChichiL1()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.838980F;
            m.M12 = 0.000000F;
            m.M13 = -0.092846F;
            m.M14 = 0.000000F;
            m.M21 = -0.014228F;
            m.M22 = 0.991598F;
            m.M23 = -0.128574F;
            m.M24 = 0.000000F;
            m.M31 = 0.050883F;
            m.M32 = 0.060347F;
            m.M33 = 0.459783F;
            m.M34 = 0.000000F;
            m.M41 = 0.040490F;
            m.M42 = 2.234810F;
            m.M43 = -0.433890F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiL2 0.0
        public static Matrix GetMinChichiL2()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.931217F;
            m.M12 = 0.000000F;
            m.M13 = 0.164139F;
            m.M14 = 0.000000F;
            m.M21 = -0.018493F;
            m.M22 = 0.921283F;
            m.M23 = 0.343484F;
            m.M24 = 0.000000F;
            m.M31 = -0.060014F;
            m.M32 = -0.091304F;
            m.M33 = 1.114690F;
            m.M34 = 0.000000F;
            m.M41 = 0.398463F;
            m.M42 = -1.191766F;
            m.M43 = 1.387515F;
            m.M44 = 1.000000F;
            m.M43 += 0.2F;
            float scale = 1.125f;
            m.M41 /= scale;
            m.M42 /= scale;
            m.M43 /= scale;
            m *= Matrix.Scaling(scale, scale, scale);
            return m;
        }

        /// 変形行列 ChichiL3 0.0
        public static Matrix GetMinChichiL3()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.872085F;
            m.M12 = 0.000001F;
            m.M13 = -0.000001F;
            m.M14 = 0.000000F;
            m.M21 = 0.000001F;
            m.M22 = 0.736430F;
            m.M23 = -0.000001F;
            m.M24 = 0.000000F;
            m.M31 = 0.000001F;
            m.M32 = 0.000001F;
            m.M33 = 1.298549F;
            m.M34 = 0.000000F;
            m.M41 = 0.214248F;
            m.M42 = -0.511035F;
            m.M43 = 0.492813F;
            m.M44 = 1.000000F;
            float scale = 1.0f / 1.125f;
            m.M41 /= scale;
            m.M42 /= scale;
            m.M43 /= scale;
            m *= Matrix.Scaling(scale, scale, scale);
            return m;
        }

        /// 変形行列 ChichiL4 0.0
        public static Matrix GetMinChichiL4()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.451920F;
            m.M12 = -0.000002F;
            m.M13 = 0.000002F;
            m.M14 = 0.000000F;
            m.M21 = -0.000002F;
            m.M22 = 1.451919F;
            m.M23 = 0.000001F;
            m.M24 = 0.000000F;
            m.M31 = -0.000002F;
            m.M32 = 0.000000F;
            m.M33 = 1.451921F;
            m.M34 = 0.000000F;
            m.M41 = 0.095882F;
            m.M42 = -0.016238F;
            m.M43 = 0.385769F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiL5 0.0
        public static Matrix GetMinChichiL5()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.000001F;
            m.M12 = 0.000000F;
            m.M13 = 0.000000F;
            m.M14 = 0.000000F;
            m.M21 = 0.000001F;
            m.M22 = 1.000000F;
            m.M23 = 0.000001F;
            m.M24 = 0.000000F;
            m.M31 = 0.000001F;
            m.M32 = 0.000000F;
            m.M33 = 1.000001F;
            m.M34 = 0.000000F;
            m.M41 = 0.064114F;
            m.M42 = 0.045690F;
            m.M43 = 0.238135F;
            m.M44 = 1.000000F;
            return m;
        }

        /// 変形行列 ChichiL5_End 0.0
        public static Matrix GetMinChichiL5E()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 0.999999F;
            m.M12 = 0.000000F;
            m.M13 = 0.000001F;
            m.M14 = 0.000000F;
            m.M21 = -0.000001F;
            m.M22 = 0.999999F;
            m.M23 = 0.000000F;
            m.M24 = 0.000000F;
            m.M31 = -0.000001F;
            m.M32 = 0.000000F;
            m.M33 = 1.000000F;
            m.M34 = 0.000000F;
            m.M41 = 0.000006F;
            m.M42 = 0.049874F;
            m.M43 = 0.109921F;
            m.M44 = 1.000000F;
            return m;
        }

        /// おっぱいスライダ0.225でのscaling factor
        public static Vector3 GetMinChichi()
        {
            return new Vector3(0.8350f, 0.8240f, 0.7800f);
        }

        /// おっぱいスライダ1.0でのscaling factor
        public static Vector3 GetMaxChichi()
        {
            return new Vector3(1.2500f, 1.3000f, 1.1800f);
        }

        /// たれ目つり目スライダ0.0での変形
        public static Matrix GetMinEyeR()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.04776F; m.M12 = -0.165705F; m.M13 = -0.042457F; m.M14 = 0;
            m.M21 = 0.169162F; m.M22 = 1.012264F; m.M23 = -0.011601F; m.M24 = 0;
            m.M31 = 0.036979F; m.M32 = 0.024401F; m.M33 = 1.100661F; m.M34 = 0;
            m.M41 = 0.004252F; m.M42 = 0.124786F; m.M43 = 0.025256F; m.M44 = 1;
            return m;
        }

        /// たれ目つり目スライダ1.0での変形
        public static Matrix GetMaxEyeR()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.039604F; m.M12 = 0.255108F; m.M13 = -0.15971F; m.M14 = 0;
            m.M21 = -0.27475F; m.M22 = 1.011007F; m.M23 = -0.040911F; m.M24 = 0;
            m.M31 = 0.139395F; m.M32 = 0.085025F; m.M33 = 1.090691F; m.M34 = 0;
            m.M41 = 0.180933F; m.M42 = -0.058389F; m.M43 = 0.094482F; m.M44 = 1;
            return m;
        }

        /// たれ目つり目スライダ0.0での変形
        public static Matrix GetMinEyeL()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.04776F; m.M12 = 0.165707F; m.M13 = 0.042456F; m.M14 = 0;
            m.M21 = -0.169164F; m.M22 = 1.012264F; m.M23 = -0.01111F; m.M24 = 0;
            m.M31 = -0.036979F; m.M32 = 0.0244F; m.M33 = 1.100662F; m.M34 = 0;
            m.M41 = -0.004275F; m.M42 = 0.124808F; m.M43 = 0.025122F; m.M44 = 1;
            return m;
        }

        /// たれ目つり目スライダ1.0での変形
        public static Matrix GetMaxEyeL()
        {
            Matrix m = Matrix.Identity;
            m.M11 = 1.039607F; m.M12 = -0.255101F; m.M13 = 0.159708F; m.M14 = 0;
            m.M21 = 0.274743F; m.M22 = 1.01101F; m.M23 = -0.040846F; m.M24 = 0;
            m.M31 = -0.139394F; m.M32 = 0.085025F; m.M33 = 1.090691F; m.M34 = 0;
            m.M41 = -0.181016F; m.M42 = -0.058312F; m.M43 = 0.094464F; m.M44 = 1;
            return m;
        }

        /// 指定割合に比例するscaling factorを得ます。
        public static Vector3 GetVector3Rate(Vector3 min, Vector3 max, float rate)
        {
            return Vector3.Lerp(min, max, rate);
        }

        /// 指定割合に比例する変形行列を得ます。
        public static Matrix GetMatrixRate(Vector3 min, Vector3 max, float rate)
        {
            return Matrix.Scaling(GetVector3Rate(min, max, rate));
        }

        /// 指定割合に比例する変形行列を得ます。
        public static Matrix GetMatrixRate(Matrix min, Matrix max, float rate)
        {
            Matrix m;

            m.M11 = Helper.Lerp(min.M11, max.M11, rate);
            m.M12 = Helper.Lerp(min.M12, max.M12, rate);
            m.M13 = Helper.Lerp(min.M13, max.M13, rate);
            m.M14 = Helper.Lerp(min.M14, max.M14, rate);

            m.M21 = Helper.Lerp(min.M21, max.M21, rate);
            m.M22 = Helper.Lerp(min.M22, max.M22, rate);
            m.M23 = Helper.Lerp(min.M23, max.M23, rate);
            m.M24 = Helper.Lerp(min.M24, max.M24, rate);

            m.M31 = Helper.Lerp(min.M31, max.M31, rate);
            m.M32 = Helper.Lerp(min.M32, max.M32, rate);
            m.M33 = Helper.Lerp(min.M33, max.M33, rate);
            m.M34 = Helper.Lerp(min.M34, max.M34, rate);

            m.M41 = Helper.Lerp(min.M41, max.M41, rate);
            m.M42 = Helper.Lerp(min.M42, max.M42, rate);
            m.M43 = Helper.Lerp(min.M43, max.M43, rate);
            m.M44 = Helper.Lerp(min.M44, max.M44, rate);

            return m;
        }

        /// face_oyaの変形行列
        public static Matrix FaceOyaDefault;

        static SliderMatrix()
        {
            FaceOyaDefault = Matrix.Scaling(1.1045F, 1.064401F, 1.1045F);
        }

        /// 拡大変位
        public Vector3 Local;
        /// 拡大変位
        public Vector3 FaceOya;

        /// 拡大変位
        public Vector3 SpineDummy;
        /// 拡大変位
        public Vector3 Spine1;

        /// 拡大変位
        public Vector3 HipsDummy;
        /// 拡大変位
        public Vector3 UpLeg;
        /// 拡大変位
        public Vector3 UpLegRoll;
        /// 拡大変位
        public Vector3 LegRoll;

        /// 拡大変位
        public Vector3 ArmDummy;
        /// 拡大変位
        public Vector3 Arm;

        /// 拡大変位
        public Vector3 Chichi;

        /// 変形行列
        public Matrix EyeR;
        /// 変形行列
        public Matrix EyeL;

        /// スライダ変形行列を生成します。
        public SliderMatrix()
        {
            ArmRate = 0.5f;
            LegRate = 0.5f;
            WaistRate = 0.0f; //scaling factorから見て胴まわりの基準は0.0である
            OppaiRate = 0.5f;
            AgeRate = 0.5f;
            EyeRate = 0.5f;
        }

        float arm_rate;
        /// うでスライダ割合
        public float ArmRate
        {
            get { return arm_rate; }
            set
            {
                arm_rate = value;
                ArmDummy = GetVector3Rate(GetMinArmDummy(), GetMaxArmDummy(), arm_rate);
                Arm = GetVector3Rate(GetMinArm(), GetMaxArm(), arm_rate);
            }
        }

        float leg_rate;
        /// あしスライダ割合
        public float LegRate
        {
            get { return leg_rate; }
            set
            {
                leg_rate = value;
                HipsDummy = GetVector3Rate(GetMinHipsDummy(), GetMaxHipsDummy(), leg_rate);
                UpLeg = GetVector3Rate(GetMinUpLeg(), GetMaxUpLeg(), leg_rate);
                UpLegRoll = GetVector3Rate(GetMinUpLegRoll(), GetMaxUpLegRoll(), leg_rate);
                LegRoll = GetVector3Rate(GetMinLegRoll(), GetMaxLegRoll(), leg_rate);
            }
        }

        float waist_rate;
        /// 胴まわりスライダ割合
        public float WaistRate
        {
            get { return waist_rate; }
            set
            {
                waist_rate = value;
                SpineDummy = GetVector3Rate(GetMinSpineDummy(), GetMaxSpineDummy(), waist_rate);
                Spine1 = GetVector3Rate(GetMinSpine1(), GetMaxSpine1(), waist_rate);
            }
        }

        float oppai_rate;
        /// おっぱいスライダ割合
        public float OppaiRate
        {
            get { return oppai_rate; }
            set
            {
                oppai_rate = value;

                if (Flat())
                {
                    float rate = oppai_rate / oppai_flat_rate;
                    float scale = 1.0f;

                    Chichi = GetVector3Rate(new Vector3(scale, scale, scale), GetMinChichi(), rate);
                }
                else
                    Chichi = GetVector3Rate(GetMinChichi(), GetMaxChichi(), (oppai_rate - oppai_flat_rate) / (1.0f - oppai_flat_rate));
            }
        }

        /// 貧乳であるか
        public bool Flat()
        {
            return oppai_rate < oppai_flat_rate;
        }

        /// 貧乳境界割合
        public static float oppai_flat_rate = 0.2250F;

        float age_rate;
        /// 姉妹スライダ割合
        public float AgeRate
        {
            get { return age_rate; }
            set
            {
                age_rate = value;
                Local = GetVector3Rate(GetMinLocal(), GetMaxLocal(), age_rate);
                FaceOya = GetVector3Rate(GetMinFaceOya(), GetMaxFaceOya(), age_rate);
            }
        }

        float eye_rate;
        /// たれ目つり目スライダ割合
        public float EyeRate
        {
            get { return eye_rate; }
            set
            {
                eye_rate = value;
                EyeR = GetMatrixRate(GetMinEyeR(), GetMaxEyeR(), eye_rate) * Matrix.Invert(FaceOyaDefault);
                EyeL = GetMatrixRate(GetMinEyeL(), GetMaxEyeL(), eye_rate) * Matrix.Invert(FaceOyaDefault);
            }
        }

        void Scale1(ref Matrix m, Vector3 scaling)
        {
            m.M11 *= scaling.X;
            m.M12 *= scaling.X;
            m.M13 *= scaling.X;
            m.M21 *= scaling.Y;
            m.M22 *= scaling.Y;
            m.M23 *= scaling.Y;
            m.M31 *= scaling.Z;
            m.M32 *= scaling.Z;
            m.M33 *= scaling.Z;
        }

        void Scale1(ref Matrix m, Matrix scaling)
        {
            m.M11 *= scaling.M11;
            m.M12 *= scaling.M11;
            m.M13 *= scaling.M11;
            m.M21 *= scaling.M22;
            m.M22 *= scaling.M22;
            m.M23 *= scaling.M22;
            m.M31 *= scaling.M33;
            m.M32 *= scaling.M33;
            m.M33 *= scaling.M33;
        }

        /// おっぱい変形：貧乳を行います。
        public void TransformChichiFlat(TMONode tmo_node, ref Matrix m)
        {
            float rate = oppai_rate / oppai_flat_rate;

            switch (tmo_node.Name)
            {
                case "Chichi_Right1":
                    m = GetMatrixRate(GetMinChichiR1(), m, rate);
                    break;
                case "Chichi_Right2":
                    m = GetMatrixRate(GetMinChichiR2(), m, rate);
                    break;
                case "Chichi_Right3":
                    m = GetMatrixRate(GetMinChichiR3(), m, rate);
                    break;
                case "Chichi_Right4":
                    m = GetMatrixRate(GetMinChichiR4(), m, rate);
                    break;
                case "Chichi_Right5":
                    m = GetMatrixRate(GetMinChichiR5(), m, rate);
                    break;
                case "Chichi_Right5_end":
                    m = GetMatrixRate(GetMinChichiR5E(), m, rate);
                    break;
                case "Chichi_Left1":
                    m = GetMatrixRate(GetMinChichiL1(), m, rate);
                    break;
                case "Chichi_Left2":
                    m = GetMatrixRate(GetMinChichiL2(), m, rate);
                    break;
                case "Chichi_Left3":
                    m = GetMatrixRate(GetMinChichiL3(), m, rate);
                    break;
                case "Chichi_Left4":
                    m = GetMatrixRate(GetMinChichiL4(), m, rate);
                    break;
                case "Chichi_Left5":
                    m = GetMatrixRate(GetMinChichiL5(), m, rate);
                    break;
                case "Chichi_Left5_End":
                    m = GetMatrixRate(GetMinChichiL5E(), m, rate);
                    break;
            }

            // translationを維持する必要があるため
            // translationに対してscalingを打ち消す演算を行う。
            Vector3 scaling = this.Chichi;

            m.M41 /= scaling.X;
            m.M42 /= scaling.Y;
            m.M43 /= scaling.Z;

            switch (tmo_node.Name)
            {
                case "Chichi_Right1":
                case "Chichi_Left1":
                    m *= Matrix.Scaling(scaling);
                    break;
            }
        }

        /// 表情変形を行います。
        public void TransformFace(TMONode tmo_node, ref Matrix m)
        {
            switch (tmo_node.Name)
            {
                case "face_oya":
                    Scale1(ref m, this.FaceOya);
                    break;
                case "eyeline_sita_L":
                case "L_eyeline_oya_L":
                case "Me_Right_Futi":
                    m *= this.EyeR;
                    break;
                case "eyeline_sita_R":
                case "R_eyeline_oya_R":
                case "Me_Left_Futi":
                    m *= this.EyeL;
                    break;

            }
        }

        /// 体型変形を行います。
        public void Scale(TMONode tmo_node, ref Matrix m)
        {
            switch (tmo_node.Name)
            {
                case "W_Spine_Dummy":
                    Scale1(ref m, this.SpineDummy);
                    break;
                case "W_Spine1":
                case "W_Spine2":
                    Scale1(ref m, this.Spine1);
                    break;

                case "W_LeftHips_Dummy":
                case "W_RightHips_Dummy":
                    Scale1(ref m, this.HipsDummy);
                    break;
                case "W_LeftUpLeg":
                case "W_RightUpLeg":
                    Scale1(ref m, this.UpLeg);
                    break;
                case "W_LeftUpLegRoll":
                case "W_RightUpLegRoll":
                case "W_LeftLeg":
                case "W_RightLeg":
                    Scale1(ref m, this.UpLegRoll);
                    break;
                case "W_LeftLegRoll":
                case "W_RightLegRoll":
                case "W_LeftFoot":
                case "W_RightFoot":
                case "W_LeftToeBase":
                case "W_RightToeBase":
                    Scale1(ref m, this.LegRoll);
                    break;

                case "W_LeftArm_Dummy":
                case "W_RightArm_Dummy":
                    Scale1(ref m, this.ArmDummy);
                    break;
                case "W_LeftArm":
                case "W_RightArm":
                case "W_LeftArmRoll":
                case "W_RightArmRoll":
                case "W_LeftForeArm":
                case "W_RightForeArm":
                case "W_LeftForeArmRoll":
                case "W_RightForeArmRoll":
                    Scale1(ref m, this.Arm);
                    break;
            }
        }

        /// おっぱい変形を行います。
        public void ScaleChichi(ref Matrix m)
        {
            Scale1(ref m, this.Chichi);
        }
    }
}
