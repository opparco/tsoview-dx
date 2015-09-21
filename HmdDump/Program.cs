using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HmdDump
{
    class Program
    {
        OculusWrap.Wrap oculus;
        OculusWrap.Hmd hmd;

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Process();
        }
        bool Process()
        {
            oculus = new OculusWrap.Wrap();

            // Initialize the Oculus runtime.
            bool success = oculus.Initialize();
            if (!success)
            {
                Console.WriteLine("Failed to initialize the Oculus runtime library.");
                return false;
            }
            int numberOfHeadMountedDisplays = oculus.Hmd_Detect();
            if (numberOfHeadMountedDisplays > 0)
                hmd = oculus.Hmd_Create(0);
            else
                hmd = oculus.Hmd_CreateDebug(OculusWrap.OVR.HmdType.DK2);

            if (hmd == null)
            {
                Console.WriteLine("Oculus Rift not detected.");
                return false;
            }

            if (hmd.ProductName == string.Empty)
                Console.WriteLine("The HMD is not enabled.");

            // Specify which head tracking capabilities to enable.
            hmd.SetEnabledCaps(OculusWrap.OVR.HmdCaps.LowPersistence | OculusWrap.OVR.HmdCaps.DynamicPrediction);

            // Start the sensor which informs of the Rift's pose and motion
            hmd.ConfigureTracking(OculusWrap.OVR.TrackingCaps.ovrTrackingCap_Orientation | OculusWrap.OVR.TrackingCaps.ovrTrackingCap_MagYawCorrection | OculusWrap.OVR.TrackingCaps.ovrTrackingCap_Position, OculusWrap.OVR.TrackingCaps.None);

            Console.WriteLine("-- HmdDump --");
            Console.WriteLine("EyeHeight {0}", hmd.GetFloat(OculusWrap.OVR.OVR_KEY_EYE_HEIGHT, 0));

            var eyeToNoseDistance = new float[] { 0, 0 };
            hmd.GetFloatArray(OculusWrap.OVR.OVR_KEY_EYE_TO_NOSE_DISTANCE, ref eyeToNoseDistance);
            Console.WriteLine("EyeToNoseDist {0} {1}", eyeToNoseDistance[0], eyeToNoseDistance[1]);

            Console.WriteLine("IPD {0}", hmd.GetFloat(OculusWrap.OVR.OVR_KEY_IPD, 0));

            var neckToEyeDistance = new float[] { 0, 0 };
            hmd.GetFloatArray(OculusWrap.OVR.OVR_KEY_NECK_TO_EYE_DISTANCE, ref neckToEyeDistance);
            Console.WriteLine("NeckEyeDistance {0} {1}", neckToEyeDistance[0], neckToEyeDistance[1]);

            Console.WriteLine("PlayerHeight {0}", hmd.GetFloat(OculusWrap.OVR.OVR_KEY_PLAYER_HEIGHT, 0));

            hmd.Dispose();
            oculus.Dispose();

            return true;
        }
    }
}
