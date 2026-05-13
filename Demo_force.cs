/*
 *  This is the example code of how to calculate the torques needed to display target force/torque at the handle
 *  
 *  This device need to calculate the torque to drive upper arm and down arm first, then send the torque to the device via serial
 *  
 *  To calculate the torques use the following function
 *  
 *  output.pointTargetUP(6, DeviceVar[0], DeviceVar[1], DeviceVar[2], DeviceVar[3], DeviceVar[4], DeviceVar[5], DeviceVar[6], DeviceVar[7], DeviceVar[8]
 *                       , -thu1, thu2, thu3, -thd1, thd2, thd3, -Fx, -Fz, Fy, -Mx, -Mz, My, k);
 *                       
 *  output.pointTargetDOWN(6, DeviceVar[0], DeviceVar[1], DeviceVar[2], DeviceVar[3], DeviceVar[4], DeviceVar[5], DeviceVar[6], DeviceVar[7], DeviceVar[8]
 *                       , -thu1, thu2, thu3, -thd1, thd2, thd3, -Fx, -Fz, Fy, -Mx, -Mz, My, k);
 *  
 *  
 */
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using HapticDevice;
using HapticDevicePro;

public class Demo_force : MonoBehaviour
{
    public GameObject Cube_UP, Cube_DOWN, HIP;

    static public double thu1, thu2, thu3, thd1, thd2, thd3;
    static public double Fx, Fy, Fz, Mx, My, Mz;
    private float k = 36;   //screw constant

    static public MWArray[] UP_Target;
    static public MWArray[] DOWN_Target;

    static public double[] DeviceVar = { 245, 75, 200, 200, 0, 75, 200, 200, 0 };

    Force output = new Force();

    private void FixedUpdate()
    {
        CalculateMotorTorque();
    }

    public void CalculateMotorTorque()
    {

        //計算馬達所需輸出扭矩
        UP_Target = output.pointTargetUP(6, DeviceVar[0], DeviceVar[1], DeviceVar[2], DeviceVar[3], DeviceVar[4], DeviceVar[5], DeviceVar[6], DeviceVar[7], DeviceVar[8]
        , -thu1, thu2, thu3, -thd1, thd2, thd3, -Fx, -Fz, Fy, -Mx, -Mz, My, k);

        DOWN_Target = output.pointTargetDOWN(6, DeviceVar[0], DeviceVar[1], DeviceVar[2], DeviceVar[3], DeviceVar[4], DeviceVar[5], DeviceVar[6], DeviceVar[7], DeviceVar[8]
        , -thu1, thu2, thu3, -thd1, thd2, thd3, -Fx, -Fz, Fy, -Mx, -Mz, My, k);

    }
}