using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TestScript : MonoBehaviour
{
    float timer = 0;

    int count = 50;
    
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
        
//        DebugDrawer.DrawPixelScreenQuad(new Vector3[]
//        {
//            new Vector3(10,10, 0),
//            new Vector3(300, 10, 0),
//            new Vector3( 500, 500, 0),
//            new Vector3( 10, 300, 0) 
//        }, new Color[]
//        {
//            Color.red, Color.green, Color.blue, Color.yellow 
//        });
//        
//        DebugDrawer.DrawNormalizedScreenQuad(new Vector3[]
//        {
//            new Vector3(0.8f,0.8f, 0),
//            new Vector3(0.8f, 0.9f, 0),
//            new Vector3( 0.9f, 0.9f, 0),
//            new Vector3( 0.9f, 0.8f, 0) 
//        }, new Color[]
//        {
//            Color.green, Color.green, Color.blue, Color.blue 
//        });

        for(int i = 0; i < 300; ++i)
            DebugDrawer.DrawTextScreenSpace(new Vector3(Random.Range(200, Screen.width - 200), Random.Range(200, Screen.height - 200), 0), Random.ColorHSV(), "This IS a TEST string");

//        for (int i = 0; i < count; ++i)
//        {
//            DebugDrawer.DrawLine(randomPoints[i*2+0], randomPoints[i*2+1], randomColor[i]);
//            DebugDrawer.DrawFilledQuad(randomPointQuad[i], randomColorQuad[i]);
//        }
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
            
            randomColorQuad[i][0] = Random.ColorHSV(0,1,0,1,0,1,1,1);
            randomColorQuad[i][1] = Random.ColorHSV(0,1,0,1,0,1,1,1);
            randomColorQuad[i][2] = Random.ColorHSV(0,1,0,1,0,1,1,1);
            randomColorQuad[i][3] = Random.ColorHSV(0,1,0,1,0,1,1,1);
        }
    }
}
