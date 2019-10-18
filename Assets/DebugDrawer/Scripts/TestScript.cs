using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TestScript : MonoBehaviour
{
    float timer = 0;

    int count = 10;
    
    Vector3[] randomPoints;
    Color[] randomColor;

    List<Vector3[]> randomPointQuad;
    List<Color[]> randomColorQuad;


    // Start is called before the first frame update
    void Start()
    {
        PickNewPoint();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 3.0f)
        {
            timer = 0;
            PickNewPoint();
        }

        for (int i = 0; i < count; ++i)
        {
            DebugDrawer.DrawLine(randomPoints[i*2+0], randomPoints[i*2+1], randomColor[i]);
            DebugDrawer.DrawWireQuad(randomPointQuad[i], randomColorQuad[i][0]);
        }
    }

    void PickNewPoint()
    {
        float range = 10;
        
        randomPoints = new Vector3[count * 2];
        randomColor = new Color[count];
        
        randomPointQuad = new List<Vector3[]>();
        randomColorQuad = new List<Color[]>();

        for (int i = 0; i < count; ++i)
        {
            randomPoints[i * 2 + 0] = Random.insideUnitSphere * range;
            randomPoints[i * 2 + 1] = Random.insideUnitSphere * range;

            randomColor[i] = Random.ColorHSV();
            
            randomPointQuad.Add(new Vector3[4]);
            randomColorQuad.Add(new Color[4]);
            
            randomPointQuad[i][0] = Random.insideUnitSphere * range;
            randomPointQuad[i][1] = Random.insideUnitSphere * range;
            randomPointQuad[i][2] = Random.insideUnitSphere * range;
            randomPointQuad[i][3] = Random.insideUnitSphere * range;
            
            randomColorQuad[i][0] = Random.ColorHSV();
            randomColorQuad[i][1] = Random.ColorHSV();
            randomColorQuad[i][2] = Random.ColorHSV();
            randomColorQuad[i][3] = Random.ColorHSV();
        }
    }
}
