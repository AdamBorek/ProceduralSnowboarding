using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SegmentationSliderScript : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] TerrainGenerator generator;
    Text text;

    // Start is called before the first frame update
    void Start()
    {
        text = gameObject.GetComponent<Text>();
        text.text = "Path Segmentation: " + slider.value.ToString();
        generator.pathSegmentNum = (int)slider.value;
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Path Segmentation: " + slider.value.ToString();
        generator.pathSegmentNum = (int)slider.value;
    }
}
