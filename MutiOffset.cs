/*
 * This code is use to reset the handle the reading of encoder, can reset the handle position to the standard position
 */
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Demo_mutioffset : MonoBehaviour
{
    //用來重設六個馬達的讀取角度

    static public float OffsetA1, OffsetA2, OffsetA3, OffsetA4;
    static public float OffsetB1, OffsetB2, OffsetB3;
    public Text highScore;
    void Start()
    {
        OffsetA1 = PlayerPrefs.GetFloat("u1", 30.03f);
        OffsetA2 = PlayerPrefs.GetFloat("u2", -15.25f);
        OffsetA3 = PlayerPrefs.GetFloat("u3", -21.36f);
        OffsetA4 = PlayerPrefs.GetFloat("u4", -79.02f); // offset of the handle encoder
        OffsetB1 = PlayerPrefs.GetFloat("d1", 30.33f);
        OffsetB2 = PlayerPrefs.GetFloat("d2", -32.06f);
        OffsetB3 = PlayerPrefs.GetFloat("d3", -2.58f);
    }

    // Start is called before the first frame update
    public void storeOffset()
    {
        PlayerPrefs.SetFloat("u1", Demo_Serial_V2_1.rotation_up[0]);
        PlayerPrefs.SetFloat("u2", Demo_Serial_V2_1.rotation_up[1]);
        PlayerPrefs.SetFloat("u3", Demo_Serial_V2_1.rotation_up[2]);
        PlayerPrefs.SetFloat("u4", Demo_Serial_V2_1.rotation_up[3]);
        PlayerPrefs.SetFloat("d1", Demo_Serial_V2_1.rotation_down[0]);
        PlayerPrefs.SetFloat("d2", Demo_Serial_V2_1.rotation_down[1]);
        PlayerPrefs.SetFloat("d3", Demo_Serial_V2_1.rotation_down[2]);

        OffsetA1 = PlayerPrefs.GetFloat("u1", 0);
        OffsetA2 = PlayerPrefs.GetFloat("u2", 0);
        OffsetA3 = PlayerPrefs.GetFloat("u3", 0);
        OffsetA4 = PlayerPrefs.GetFloat("u4", 0);
        OffsetB1 = PlayerPrefs.GetFloat("d1", 0);
        OffsetB2 = PlayerPrefs.GetFloat("d2", 0);
        OffsetB3 = PlayerPrefs.GetFloat("d3", 0);
        Debug.Log("U:" + OffsetA1.ToString() + "," + OffsetA2.ToString() + "," + OffsetA3.ToString() + ",D:" + OffsetB1.ToString() + "," + OffsetB2.ToString() + "," + OffsetB3.ToString());
    }
}
