using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeSliderScript : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] TerrainGenerator generator;
    Text text;


    // Start is called before the first frame update
    void Start()
    {
        text = gameObject.GetComponent<Text>();
        text.text = "Node map size: " + slider.value.ToString();
        generator.nodeMapSize = (int)slider.value;
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Node map size: " + slider.value.ToString();
        generator.nodeMapSize = (int)slider.value;
    }
}
