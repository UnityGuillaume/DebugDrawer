using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class DebugDrawer : MonoBehaviour
{
    const int k_StartMaxVertices = UInt16.MaxValue;
    static DebugDrawer s_Instance;
    
    CommandBuffer buffer;

    int m_LineMaxArraySize = k_StartMaxVertices;
    //TODO : Maybe can merge vertices and indices count for lines, as we never reuse vertices?
    int m_LineVerticeCount = 0;
    Vector3[] m_LineVertices = new Vector3[k_StartMaxVertices];
    Color[] m_LineColors = new Color[k_StartMaxVertices];
    int m_LineIndicesCount = 0;
    int[] m_LineIndices = new int[k_StartMaxVertices];

    int m_QuadMaxArraySize = k_StartMaxVertices;
    int m_QuadVerticeCount = 0;
    Vector3[] m_QuadVertices = new Vector3[k_StartMaxVertices];
    Color[] m_QuadColors = new Color[k_StartMaxVertices];
    int m_QuadIndicesCount = 0;
    int[] m_QuadIndices = new int[k_StartMaxVertices];

    Mesh m_LinesMesh;
    Mesh m_QuadMesh;
    Material m_DebugMaterial;

    bool m_MaxLineVerticeReached = false;
    bool m_MaxQuadVerticeReached = false;

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        GameObject drawerInstance = new GameObject();
        drawerInstance.AddComponent<DebugDrawer>();
    }

    void Awake()
    {
        if (s_Instance != null)
        {
            Destroy(this);
            return;
        }

        s_Instance = this;
        gameObject.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(gameObject);

        //we clean all vertice list in the init phase of the player loop, so any subsequent update will push to clean list
        UpdateLoopForCleanup();
        
        m_DebugMaterial = new Material(Shader.Find("Hidden/DebugDrawerShader"));

        if (GraphicsSettings.renderPipelineAsset != null)
        {
            RenderPipelineManager.endCameraRendering += RenderDebugRenderPipeline;
        }
        else
        {
            Camera.onPostRender += RenderDebug;
        }
        
        buffer = new CommandBuffer();
        buffer.name = "DebugDrawing";

        m_LinesMesh = new Mesh();
        m_LinesMesh.MarkDynamic();
        
        m_QuadMesh = new Mesh();
        m_QuadMesh.MarkDynamic();
    }

    //will only be called on game stop as the object is tagged as don't destroy on load
    void OnDestroy()
    {
        if (GraphicsSettings.renderPipelineAsset != null)
        {
            RenderPipelineManager.endCameraRendering -= RenderDebugRenderPipeline;
        }
        else
        {
            Camera.onPostRender = cam => { };
        }
        
        PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
    }

    void UpdateLoopForCleanup()
    {
        var playerLoop = PlayerLoop.GetDefaultPlayerLoop();

        for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
        {
            if (playerLoop.subSystemList[i].type == typeof(UnityEngine.Experimental.PlayerLoop.Initialization))
            {
                int length = playerLoop.subSystemList[i].subSystemList.Length;
                PlayerLoopSystem[] newSubsystem = new PlayerLoopSystem[length + 1];
                for (int j = 0; j < length; ++j)
                {
                    newSubsystem[j] = playerLoop.subSystemList[i].subSystemList[j];
                }

                newSubsystem[length].type = typeof(DebugDrawer);
                newSubsystem[length].updateDelegate += CleanupPostFrame;

                playerLoop.subSystemList[i].subSystemList = newSubsystem;
            }
        }
        
        PlayerLoop.SetPlayerLoop(playerLoop);
    }

    void RenderDebugRenderPipeline(ScriptableRenderContext context, Camera cam)
    {
        RenderDebug(cam);   
    }

    void CleanupPostFrame()
    {
        m_LineVerticeCount = 0;
        m_LineIndicesCount = 0;

        m_QuadVerticeCount = 0;
        m_QuadIndicesCount = 0;

        if (m_MaxLineVerticeReached)
        {
            Debug.LogError($"Reached max Line vertices on frame {Time.frameCount - 1}");
        }
        if (m_MaxQuadVerticeReached)
        {
            Debug.LogError($"Reached max Quad vertices on frame {Time.frameCount - 1}");
        }

        m_MaxLineVerticeReached = false;
        m_MaxQuadVerticeReached = false;
    }


    void RenderDebug(Camera cam)
    {
        BuildLineMesh();
        BuildQuadMesh();
        
        buffer.Clear();
        buffer.ClearRenderTarget(true, false, Color.black);
        buffer.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
        
        buffer.DrawMesh(m_LinesMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_DebugMaterial);
        buffer.DrawMesh(m_QuadMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_DebugMaterial);
        
        Graphics.ExecuteCommandBuffer(buffer);
    }

    void BuildLineMesh()
    {
        m_LinesMesh.Clear();
        m_LinesMesh.vertices = m_LineVertices;
        m_LinesMesh.colors = m_LineColors;

        m_LinesMesh.SetIndices(m_LineIndices, MeshTopology.Lines, 0);
    }

    void BuildQuadMesh()
    {
        m_QuadMesh.Clear();
        m_QuadMesh.vertices = m_QuadVertices;
        m_QuadMesh.colors = m_QuadColors;
        
        m_QuadMesh.SetIndices(m_QuadIndices, MeshTopology.Triangles, 0);
    }

    void InternalDrawLine(Vector3 start, Vector3 end, Color colorStart, Color colorEnd)
    {
        if (m_LineVerticeCount + 2 >= m_LineMaxArraySize)
        {
            int newMaxSize = m_LineMaxArraySize + k_StartMaxVertices;
            
            var newLineVertice = new Vector3[newMaxSize];
            var newLineColor = new Color[newMaxSize];
            var newLineIndices = new int[newMaxSize];
            
            Array.Copy(m_LineVertices, newLineVertice, m_LineVerticeCount);
            Array.Copy(m_LineColors, newLineColor, m_LineVerticeCount);
            Array.Copy(m_LineIndices, newLineIndices, m_LineIndicesCount);

            m_LineMaxArraySize = newMaxSize;
            m_LineVertices = newLineVertice;
            m_LineColors = newLineColor;
            m_LineIndices = newLineIndices;
        }
        
        m_LineVertices[m_LineVerticeCount] = start;
        m_LineVertices[m_LineVerticeCount + 1] = end;

        m_LineColors[m_LineVerticeCount] = colorStart;
        m_LineColors[m_LineVerticeCount + 1] = colorEnd;

        m_LineIndices[m_LineIndicesCount] = m_LineVerticeCount;
        m_LineIndices[m_LineIndicesCount + 1] = m_LineVerticeCount + 1;

        m_LineVerticeCount += 2;
        m_LineIndicesCount += 2;
    }

    void InternalDrawFilledQuad(Vector3[] corners, Color[] colors)
    {
        if (m_QuadIndicesCount + 6 >= m_QuadMaxArraySize)
        {
            int newMaxSize = m_QuadMaxArraySize + k_StartMaxVertices;
            
            var newQuadVertice = new Vector3[newMaxSize];
            var newQuadColor = new Color[newMaxSize];
            var newQuadIndices = new int[newMaxSize];
            
            Array.Copy(m_QuadVertices, newQuadVertice, m_QuadVerticeCount);
            Array.Copy(m_QuadColors, newQuadColor, m_QuadVerticeCount);
            Array.Copy(m_QuadIndices, newQuadIndices, m_QuadIndicesCount);

            m_QuadMaxArraySize = newMaxSize;
            m_QuadVertices = newQuadVertice;
            m_QuadColors = newQuadColor;
            m_QuadIndices = newQuadIndices;
        }
        
        Assert.IsTrue(corners.Length == 4, "Drawing a quad require 4 corners");

        m_QuadVertices[m_QuadVerticeCount] = corners[0];
        m_QuadVertices[m_QuadVerticeCount + 1] = corners[1];
        m_QuadVertices[m_QuadVerticeCount + 2] = corners[2];
        m_QuadVertices[m_QuadVerticeCount + 3] = corners[3];

        int colorLength = colors.Length;
        
        m_QuadColors[m_QuadVerticeCount] = colors[0];
        m_QuadColors[m_QuadVerticeCount + 1] = colors[ 1 < colorLength - 1 ? 1 : colorLength - 1];
        m_QuadColors[m_QuadVerticeCount + 1] = colors[ 2 < colorLength - 1 ? 2 : colorLength - 1];
        m_QuadColors[m_QuadVerticeCount + 1] = colors[ 3 < colorLength - 1 ? 3 : colorLength - 1];

        m_QuadIndices[m_QuadIndicesCount] = m_QuadVerticeCount;
        m_QuadIndices[m_QuadIndicesCount + 1] = m_QuadVerticeCount + 1;
        m_QuadIndices[m_QuadIndicesCount + 2] = m_QuadVerticeCount + 2;
        m_QuadIndices[m_QuadIndicesCount + 3] = m_QuadVerticeCount;
        m_QuadIndices[m_QuadIndicesCount + 4] = m_QuadVerticeCount + 2;
        m_QuadIndices[m_QuadIndicesCount + 5] = m_QuadVerticeCount + 3;

        m_QuadVerticeCount += 4;
        m_QuadIndicesCount += 6;
    }
    
    void InternalDrawWireQuad(Vector3[] corners, Color[] colors)
    {
        Assert.IsTrue(corners.Length == 4, "Drawing a wire quad require 4 corners");
        
        int colorLength = colors.Length;
        
        InternalDrawLine(corners[0], corners[1], colors[0],colors[ 1 < colorLength - 1 ? 1 : colorLength - 1]);
        InternalDrawLine(corners[1], corners[2], colors[ 1 < colorLength - 1 ? 1 : colorLength - 1],colors[ 2 < colorLength - 1 ? 2 : colorLength - 1]);
        InternalDrawLine(corners[2], corners[3], colors[ 2 < colorLength - 1 ? 2 : colorLength - 1],colors[ 3 < colorLength - 1 ? 3 : colorLength - 1]);
        InternalDrawLine(corners[3], corners[0], colors[ 3 < colorLength - 1 ? 3 : colorLength - 1],colors[0]);
    }
    
    // ---------- Public Interfaces 

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        s_Instance.InternalDrawLine(start,end,color, color);
    }
    
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end, Color colorStart, Color colorEnd)
    {
        s_Instance.InternalDrawLine(start,end,colorStart, colorEnd);
    }

    /// <summary>
    /// Draw a filled quad (2 triangles) using the 4 corners provided. If a single color is given in the array, then it
    /// is used for the 4 corners. 
    /// </summary>
    /// <param name="corners">4 Vector3 that are the 4 corner of the quad, made of 2 triangle (0,1,2 and 0,2,3)</param>
    /// <param name="colors">4 Colors for the 4 corner, if this is a single entry array, it will be used for the 4 corner</param>
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawFilledQuad(Vector3[] corners, Color[] colors)
    {
        s_Instance.InternalDrawFilledQuad(corners, colors);
    }

    /// <summary>
    /// Draw a wire quad (2 triangles) using the 4 corners provided.
    /// </summary>
    /// <param name="corners">4 Vector3 that are the 4 corner of the quad</param>
    /// <param name="color">A color for the wire</param>
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawWireQuad(Vector3[] corners, Color[] color)
    { 
        s_Instance.InternalDrawWireQuad(corners, color);
    }
}
