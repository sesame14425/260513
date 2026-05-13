/*
 * This is the example code of how to get the position of the handle
 * 需要在環境中建置兩個 gameobject 以計算在 Unity 坐標系中的位置與姿態
 * Unity中的
 * (Unity中) H坐標系X軸在世界座標系的向量 = (DH中) H坐標系-X軸在世界座標系的向量
 * (Unity中) H坐標系Y軸在世界座標系的向量 = (DH中) H坐標系 Z軸在世界座標系的向量
 * (Unity中) H坐標系Z軸在世界座標系的向量 = (DH中) H坐標系-Y軸在世界座標系的向量
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HapticDevice;
using MathWorks.MATLAB.NET.Arrays;
using Unity.VisualScripting;

public class Demo_forward_kinematic : MonoBehaviour
{
    public MWArray[] RealhandleRotation;
    transformOperator matlabFunction = new transformOperator();
    public GameObject Cube_UP;
    public GameObject Cube_DOWN;
    public GameObject Handle;
    static public float Hanle_Enc_Rotation;
    static private int CentermetertoMeter = 100;
    public double[] DeviceVar = { 245, 75, 200, 200, 0, 75, 200, 200, 0 };

    private void FixedUpdate()
    {

        //用 matlab 計算出上下末端的位置
        MWArray[] posup = matlabFunction.positionUP(12, DeviceVar[0], DeviceVar[1], DeviceVar[2], DeviceVar[3], DeviceVar[4], -Jacobian.thu1, Jacobian.thu2, Jacobian.thu3);
        MWArray[] posdown = matlabFunction.positionDOWN(12, DeviceVar[5], DeviceVar[6], DeviceVar[7], DeviceVar[8], -Jacobian.thd1, Jacobian.thd2, Jacobian.thd3);

        // 使用上下端點計算手把的位置的位置

        //Up
        Cube_UP.transform.localPosition = new Vector3(-float.Parse(posup[0].ToString()), float.Parse(posup[2].ToString()), -float.Parse(posup[1].ToString())) / CentermetertoMeter;
  
        //Down
        Cube_DOWN.transform.localPosition = new Vector3(-float.Parse(posdown[0].ToString()), float.Parse(posdown[2].ToString()), -float.Parse(posdown[1].ToString())) / CentermetertoMeter;
        
        //把手位置是下端點往上端點 1.19317 單位

        Vector3 dir = (Cube_UP.transform.position - Cube_DOWN.transform.position); // 下往上的方向
        Vector3 mid = Cube_DOWN.transform.position + (dir * (1.19317f) / (dir.magnitude)); // 握持的位置 //1.19317 = 把手下半部的長

        transform.position = new Vector3(mid[0], mid[1], mid[2]);

        // 當手把的姿態太平時，會無法解出手把的姿態，所以建議使用 try catch
        try
        {

            // 計算手把姿態
            RealhandleRotation = matlabFunction.RealhandleRotation(19, DeviceVar[0], DeviceVar[1], DeviceVar[2], DeviceVar[3], DeviceVar[4], DeviceVar[5], DeviceVar[6], DeviceVar[7], DeviceVar[8]
                                                                , -Demo_force.thu1, Demo_force.thu2, Demo_force.thu3, -Demo_force.thd1, Demo_force.thd2, Demo_force.thd3, Hanle_Enc_Rotation * Mathf.PI / 180);


            //更新把手的位置與姿態
            Handle.transform.position = transform.position;

            Handle.transform.eulerAngles = new Vector3(float.Parse(RealhandleRotation[0].ToString()) * 180 / Mathf.PI,
                                                       float.Parse(RealhandleRotation[1].ToString()) * 180 / Mathf.PI,
                                                       float.Parse(RealhandleRotation[2].ToString()) * 180 / Mathf.PI);
        }
        catch
        {
            Debug.Log("error");
        }


    }
}