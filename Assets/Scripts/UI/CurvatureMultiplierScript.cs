using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurvatureMultiplierScript : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] TerrainGenerator generator;
    Text text;

    // Start is called before the first frame update
    void Start()
    {
        text = gameObject.GetComponent<Text>();
        text.text = "Curvature cost multiplier: " + slider.value.ToString();
        generator.curvatureCostMultiplier = slider.value;
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Curvature cost multiplier: " + slider.value.ToString();
        generator.curvatureCostMultiplier = slider.value;
    }
}
