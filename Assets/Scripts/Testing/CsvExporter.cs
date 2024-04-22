using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CsvExporter : MonoBehaviour
{
    string filename = "";

    string mellowFileName = "";
    string steepFileName = "";
    string extremeFileName = "";

    TextWriter tw;

    // Start is called before the first frame update
    void Awake()
    {
        //string topLine = ("Map Type,Stunt type,Normal setup,Steepness,Avg setup curve,Median setup curve,Setup curve uniformity,Board speed");

        ////filename = Application.dataPath + "/testing.csv";
        mellowFileName = Application.dataPath + "/Test results/mellow.csv";
        steepFileName = Application.dataPath + "/Test results/steep.csv";
        extremeFileName = Application.dataPath + "/Test results/extreme.csv";
        //tw = new StreamWriter(mellowFileName, false);
        //tw.WriteLine(topLine);
        //tw.Close();
        //tw = new StreamWriter(steepFileName, false);
        //tw.WriteLine(topLine);
        //tw.Close();
        //tw = new StreamWriter(extremeFileName, false);
        //tw.WriteLine(topLine);
        //tw.Close();
    }

    public void AddNewLine(MountainType mountainType, StuntType stuntType, bool normalSetup, float steepness, float avgSetupCurve, float medianSetupCurve, float uniformity, float boardSpeed)
    {
        //tw = new StreamWriter(filename, true);
        string data = "";

        switch (mountainType)
        {
            case MountainType.Mellow:
                tw = new StreamWriter(mellowFileName, true);
                data += "Mellow";
                break;
            case MountainType.Steep:
                tw = new StreamWriter(steepFileName, true);
                data += "Steep";
                break;
            case MountainType.Extreme:
                tw = new StreamWriter(extremeFileName, true);
                data += "Extreme";
                break;
            default:
                return;
                break;
        }

        switch (stuntType)
        {
            case StuntType.BigRamp:
                data += ",Big ramp";
                break;
            case StuntType.SmallRamp:
                data += ",Small ramp";
                break;
            case StuntType.Rail:
                data += ",Rail";
                break;
            default:
                break;
        }

        switch (normalSetup)
        {
            case true:
                data += ",Yes";
                break;
            case false:
                data += ",No";
                break;
        }

        data += "," + steepness.ToString("0.0");
        data += "," + avgSetupCurve.ToString("0.000");
        data += "," + medianSetupCurve.ToString("0.000");
        data += "," + uniformity.ToString("0.000");
        data += "," + boardSpeed.ToString("0.0");

        tw.WriteLine(data);

        tw.Close();

        Debug.LogWarning("New data added to the dataset");
    }
}
