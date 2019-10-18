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
    static DebugDrawer s_Instance;
    
    CommandBuffer buffer;

    List<Vector3> m_LineVertices = new List<Vector3>();
    List<Color> m_LineColors = new List<Color>();
    List<int> m_LineIndices = new List<int>();
    
    List<Vector3> m_QuadVertices = new List<Vector3>();
    List<Color> m_QuadColors = new List<Color>();
    List<int> m_QuadIndices = new List<int>();

    Mesh m_LinesMesh;
    Mesh m_QuadMesh;
    Material m_DebugMaterial;

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
        m_LineVertices.Clear();
        m_LineColors.Clear();
        m_LineIndices.Clear();
        
        m_QuadVertices.Clear();
        m_QuadColors.Clear();
        m_QuadIndices.Clear();
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
        m_LinesMesh.SetVertices(m_LineVertices);
        m_LinesMesh.SetColors(m_LineColors);
        
        //TODO : making an array is probably bad perf, profile and change if needed
        m_LinesMesh.SetIndices(m_LineIndices.ToArray(), MeshTopology.Lines, 0);
    }

    void BuildQuadMesh()
    {
        m_QuadMesh.Clear();
        m_QuadMesh.SetVertices(m_QuadVertices);
        m_QuadMesh.SetColors(m_QuadColors);
        
        //TODO : making an array is probably bad perf, profile and change if needed
        m_QuadMesh.SetIndices(m_QuadIndices.ToArray(), MeshTopology.Triangles, 0);
    }

    void InternalDrawLine(Vector3 start, Vector3 end, Color color)
    {
        int startIdx = m_LineVertices.Count;
        m_LineVertices.Add(start);
        m_LineVertices.Add(end);
        
        m_LineColors.Add(color);
        m_LineColors.Add(color);
        
        m_LineIndices.Add(startIdx);
        m_LineIndices.Add(startIdx + 1);
    }

    void InternalDrawFilledQuad(Vector3[] corners, Color[] colors)
    {
        Assert.IsTrue(corners.Length == 4);
        Assert.IsTrue(colors.Length == 4 || colors.Length == 1);

        int startIdx = m_QuadVertices.Count;
        m_QuadVertices.AddRange(corners);
        
        if(colors.Length == 4)
            m_QuadColors.AddRange(colors);
        else
            m_QuadColors.AddRange(new Color[4] {colors[0], colors[0], colors[0], colors[0]});
        
        m_QuadIndices.AddRange(new int[]
        {
            startIdx + 0,
            startIdx + 1,
            startIdx + 2,
            startIdx + 0,
            startIdx + 2,
            startIdx + 3,
        });
    }
    
    // ---------- Public Interfaces 

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        s_Instance.InternalDrawLine(start,end,color);
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
}
