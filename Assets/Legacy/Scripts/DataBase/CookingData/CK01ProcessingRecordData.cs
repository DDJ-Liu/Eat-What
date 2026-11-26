using System;
using UnityEngine;

[Serializable]
public class CK01ProcessingRecordData : ScriptableObject
{
    public string output_item;
    public string input_01;
    public string input_02;
    public string input_03;
    public string input_04;
    public string input_05;
    public string input_06;
    public string action;
    public string carrier;
    public bool is_final_product;
}
