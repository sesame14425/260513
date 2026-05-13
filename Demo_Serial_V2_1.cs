/*
 *  This is the example code of how to communicate to the device through serial port
 *  
 *  To send the torque to motor the packet is 
 *   "t,DT1,DT2,DT3,UT1,UT2,UT3"
 *   use serialPort.WriteLine() to send the packet
 */
using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;
using HapticDevicePro;
using MathWorks.MATLAB.NET.Arrays;

public class Demo_Serial_V2_1 : MonoBehaviour
{
    public bool execute_On_Other_Project = false;
    public string port_number = "COM16";
    SerialPort serialPort = null;
    static public float[] rotation_up, rotation_down;
    static public float[] output_up, output_down;



    static public double UT1, UT2, UT3, DT1, DT2, DT3;

    private int smoothWindowSize = 10;
    private Queue<double> UT1_History = new Queue<double>();
    private Queue<double> UT2_History = new Queue<double>();
    private Queue<double> UT3_History = new Queue<double>();
    private Queue<double> DT1_History = new Queue<double>();
    private Queue<double> DT2_History = new Queue<double>();
    private Queue<double> DT3_History = new Queue<double>();

    // 上次數值儲存
    private double prevUT1, prevUT2, prevUT3, prevDT1, prevDT2, prevDT3;
    private double changeLimitThreshold = 15;
    private double maxChange = 0.2;


    // Start is called before the first frame update
    void Start()
    {
            //UP teensy port
            serialPort = new SerialPort(port_number, 115200);
            serialPort.DtrEnable = true;
            serialPort.ReadTimeout = -1;
            serialPort.WriteTimeout = -1;
            serialPort.Open();
    }

    void FixedUpdate()
    {
        ReadData();
        WriteData();
    }

    void Update()
    {

    }

    public void ReadData()
    {
        if (serialPort.IsOpen)
        {
            serialPort.DiscardInBuffer();
            string UP = serialPort.ReadLine();

            //Split different Information
            string[] UP_sp = UP.Split(char.Parse(";"));
            string UP_Degree = UP_sp[0];

            if (UP_Degree != null)
            {
                string[] splitArray_deg = UP_Degree.Split(char.Parse(","));
                //remove offset from here
                rotation_down = new float[] { float.Parse(splitArray_deg[1]), float.Parse(splitArray_deg[2]), float.Parse(splitArray_deg[3]) };
                rotation_up = new float[] { float.Parse(splitArray_deg[4]), float.Parse(splitArray_deg[5]), float.Parse(splitArray_deg[6]), float.Parse(splitArray_deg[7]) };
                Demo_forward_kinematic.Hanle_Enc_Rotation = (float.Parse(splitArray_deg[7]) - MultiOffset.OffsetA4);
                updateDegree_UP(rotation_up[0] - MultiOffset.OffsetA1, rotation_up[1] - MultiOffset.OffsetA2, rotation_up[2] - MultiOffset.OffsetA3);
                updateDegree_DOWN(rotation_down[0] - MultiOffset.OffsetB1, rotation_down[1] - MultiOffset.OffsetB2, rotation_down[2] - MultiOffset.OffsetB3);
            }
        }
    }
    private void updateDegree_UP(float a, float b, float c)
    {
        Demo_force.thu1 = (double)a * Mathf.PI / 180;
        Demo_force.thu2 = (double)b * Mathf.PI / 180;
        Demo_force.thu3 = (double)c * Mathf.PI / 180;
    }
    private void updateDegree_DOWN(float a, float b, float c)
    {
        Demo_force.thd1 = (double)a * Mathf.PI / 180;
        Demo_force.thd2 = (double)b * Mathf.PI / 180;
        Demo_force.thd3 = (double)c * Mathf.PI / 180;
    }

    public void WriteData()
    {

            if (serialPort.IsOpen)
            {
                serialPort.DiscardOutBuffer();

                // unsmoothed version
                //UT1 = Convert.ToDouble(Jacobian.UP_Target[3].ToString(), CultureInfo.InvariantCulture) * 0.03; 
                //UT2 = Convert.ToDouble(Jacobian.UP_Target[4].ToString(), CultureInfo.InvariantCulture) * 0.03;
                //UT3 = Convert.ToDouble(Jacobian.UP_Target[5].ToString(), CultureInfo.InvariantCulture) * 0.03;
                //DT1 = Convert.ToDouble(Jacobian.DOWN_Target[3].ToString(), CultureInfo.InvariantCulture) * 0.03;
                //DT2 = Convert.ToDouble(Jacobian.DOWN_Target[4].ToString(), CultureInfo.InvariantCulture) * 0.03;
                //DT3 = Convert.ToDouble(Jacobian.DOWN_Target[5].ToString(), CultureInfo.InvariantCulture) * 0.03;

                // smooth version
                DT1 = SmoothValue(DT1_History, Convert.ToDouble(Jacobian.DOWN_Target[3].ToString(), CultureInfo.InvariantCulture) * 0.03, ref prevDT1);
                DT2 = SmoothValue(DT2_History, Convert.ToDouble(Jacobian.DOWN_Target[4].ToString(), CultureInfo.InvariantCulture) * 0.03, ref prevDT2);
                DT3 = SmoothValue(DT3_History, Convert.ToDouble(Jacobian.DOWN_Target[5].ToString(), CultureInfo.InvariantCulture) * 0.03, ref prevDT3);
                UT1 = SmoothValue(UT1_History, Convert.ToDouble(Jacobian.UP_Target[3].ToString(), CultureInfo.InvariantCulture) * 0.03, ref prevUT1);
                UT2 = SmoothValue(UT2_History, Convert.ToDouble(Jacobian.UP_Target[4].ToString(), CultureInfo.InvariantCulture) * 0.03, ref prevUT2);
                UT3 = SmoothValue(UT3_History, Convert.ToDouble(Jacobian.UP_Target[5].ToString(), CultureInfo.InvariantCulture) * 0.03, ref prevUT3);

                // send packet to the device 
                serialPort.WriteLine("t," + Math.Round(DT1, 1).ToString()
                                        + "," + Math.Round(DT2, 1).ToString()
                                        + "," + Math.Round(DT3, 1).ToString()
                                        + "," + Math.Round(UT1, 1).ToString()
                                        + "," + Math.Round(UT2, 1).ToString()
                                        + "," + Math.Round(UT3, 1).ToString());
            }
        else
        {
            serialPort.WriteLine("t,0,0,0,0,0,0");
            Debug.Log("1");
        }
    }

    private double SmoothValue(Queue<double> history, double newValue, ref double previousValue)
    {
        history.Enqueue(newValue);
        if (history.Count > smoothWindowSize)
        {
            history.Dequeue();
        }


        // 手動計算平均值
        double sum = 0;
        foreach (double val in history)
        {
            sum += val;
        }
        double smoothedValue = sum / history.Count;
        if (newValue == 0)
        {
            smoothedValue = 0;
            previousValue = 0;
            return 0;
        }

        // 限制變化量
        if (Math.Abs(previousValue) > changeLimitThreshold)
        {
            double delta = smoothedValue - previousValue;
            if (Math.Abs(delta) > maxChange)
            {
                smoothedValue = previousValue + Math.Sign(delta) * maxChange;
            }
        }

        previousValue = smoothedValue;

        return smoothedValue;
    }

    void OnApplicationQuit()
    {
        if (serialPort != null)
        {
            serialPort.WriteLine("t,0,0,0,0,0,0");
        }
    }
}


