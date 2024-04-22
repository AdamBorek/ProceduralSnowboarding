using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreferredAngleSliderScript : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] TerrainGenerator generator;
    Text text;

    // Start is called before the first frame update
    void Start()
    {
        text = gameObject.GetComponent<Text>();
        text.text = "Preferred slope angle: " + slider.value.ToString();
        generator.preferredSlope = (int)slider.value;
        generator.ReconstructSlopeFunction();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Preferred slope angle: " + slider.value.ToString();
        generator.preferredSlope = (int)slider.value;
        generator.ReconstructSlopeFunction();
    }

}
