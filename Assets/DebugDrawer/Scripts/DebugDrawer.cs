using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class DebugDrawer : MonoBehaviour
{
    const KeyCode k_ToggleKeyCode = KeyCode.F12;
    const int k_StartMaxVertices = UInt16.MaxValue;
    
    static DebugDrawer s_Instance;
    
    CommandBuffer m_Buffer;

    DebugLineRenderer m_WorldSpaceDebugLineRenderer;

    DebugQuadRenderer m_WorldSpaceDebugQuadsRenderer;
    
    DebugQuadRenderer m_PixelScreenSpaceDebugQuadRenderer;
    DebugQuadRenderer m_NormalizedScreenSpaceDebugQuadRenderer;

    DebugTextDrawer m_ScreenSpaceTextRenderer;

    Font m_DefaultFont;
    
    bool m_RuntimeToggle = true;

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
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
        
        Material debugMat = new Material(Shader.Find("Hidden/DebugDrawerShader"));

        if (GraphicsSettings.renderPipelineAsset != null)
        {
            RenderPipelineManager.endCameraRendering += RenderDebugRenderPipeline;
        }
        else
        {
            Camera.onPostRender += RenderDebug;
        }
        
        m_Buffer = new CommandBuffer();
        m_Buffer.name = "DebugDrawing";

        Shader screenspaceShader = Shader.Find("Unlit/DebugDrawerPixelScreenspace");

        Material pixelCoordMat = new Material(screenspaceShader);
        pixelCoordMat.EnableKeyword("PIXEL_COORD");
        
        Material normalizedCoordMat = new Material(screenspaceShader);
        
        m_WorldSpaceDebugLineRenderer = new DebugLineRenderer(debugMat);
        
        m_WorldSpaceDebugQuadsRenderer = new DebugQuadRenderer(debugMat);
        m_PixelScreenSpaceDebugQuadRenderer = new DebugQuadRenderer(pixelCoordMat);
        m_NormalizedScreenSpaceDebugQuadRenderer = new DebugQuadRenderer(normalizedCoordMat);

        m_DefaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        Material textRenderingMaterialScreen = new Material(screenspaceShader);
        textRenderingMaterialScreen.mainTexture = m_DefaultFont.material.mainTexture;
        
        m_ScreenSpaceTextRenderer = new DebugTextDrawer(textRenderingMaterialScreen);
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

    void Update()
    {
        if (Input.GetKeyDown(k_ToggleKeyCode))
            m_RuntimeToggle = !m_RuntimeToggle;
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
        m_WorldSpaceDebugLineRenderer.Clear();
        
        m_WorldSpaceDebugQuadsRenderer.Clear();
        m_PixelScreenSpaceDebugQuadRenderer.Clear();
        m_NormalizedScreenSpaceDebugQuadRenderer.Clear();
    }


    void RenderDebug(Camera cam)
    {
        if(!m_RuntimeToggle)
            return;

        m_WorldSpaceDebugLineRenderer.BuildMesh();
        
        m_WorldSpaceDebugQuadsRenderer.BuildMesh();
        m_PixelScreenSpaceDebugQuadRenderer.BuildMesh();
        m_NormalizedScreenSpaceDebugQuadRenderer.BuildMesh();
        
        m_ScreenSpaceTextRenderer.BuildMesh();
        
        m_Buffer.Clear();
        m_Buffer.ClearRenderTarget(true, false, Color.black);
        m_Buffer.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
        
        m_Buffer.DrawMesh(m_WorldSpaceDebugLineRenderer.Mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_WorldSpaceDebugLineRenderer.Material);
        m_Buffer.DrawMesh(m_WorldSpaceDebugQuadsRenderer.Mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_WorldSpaceDebugQuadsRenderer.Material);
        m_Buffer.DrawMesh(m_PixelScreenSpaceDebugQuadRenderer.Mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_PixelScreenSpaceDebugQuadRenderer.Material);
        m_Buffer.DrawMesh(m_NormalizedScreenSpaceDebugQuadRenderer.Mesh,  Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_NormalizedScreenSpaceDebugQuadRenderer.Material);
        m_Buffer.DrawMesh(m_ScreenSpaceTextRenderer.Mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), m_ScreenSpaceTextRenderer.Material);
        
        Graphics.ExecuteCommandBuffer(m_Buffer);
    }


    void InternalDrawLine(Vector3 start, Vector3 end, Color colorStart, Color colorEnd)
    {
        if(!m_RuntimeToggle)
            return;

        m_WorldSpaceDebugLineRenderer.PushNewLine(start, end, colorStart, colorEnd);
        
    }

    void InternalDrawFilledQuad(Vector3[] corners, Color[] colors)
    {
        if(!m_RuntimeToggle)
            return;
        
        m_WorldSpaceDebugQuadsRenderer.PushNewQuad(corners, colors);
    }
    
    void InternalDrawWireQuad(Vector3[] corners, Color[] colors)
    {
        if(!m_RuntimeToggle)
            return;
        
        int colorLength = colors.Length;
        
        m_WorldSpaceDebugLineRenderer.PushNewLine(corners[0], corners[1], colors[0],colors[ 1 < colorLength - 1 ? 1 : colorLength - 1]);
        m_WorldSpaceDebugLineRenderer.PushNewLine(corners[1], corners[2], colors[ 1 < colorLength - 1 ? 1 : colorLength - 1],colors[ 2 < colorLength - 1 ? 2 : colorLength - 1]);
        m_WorldSpaceDebugLineRenderer.PushNewLine(corners[2], corners[3], colors[ 2 < colorLength - 1 ? 2 : colorLength - 1],colors[ 3 < colorLength - 1 ? 3 : colorLength - 1]);
        m_WorldSpaceDebugLineRenderer.PushNewLine(corners[3], corners[0], colors[ 3 < colorLength - 1 ? 3 : colorLength - 1],colors[0]);
    }

    void InternalDrawFilledQuadPixelCoord(Vector3[] corners, Color[] colors)
    {
        if(!m_RuntimeToggle)
            return;
        
        m_PixelScreenSpaceDebugQuadRenderer.PushNewQuad(corners, colors);
    }
    
    void InternalDrawFilledQuadNormalizedCoord(Vector3[] corners, Color[] colors)
    {
        if(!m_RuntimeToggle)
            return;
        
        m_NormalizedScreenSpaceDebugQuadRenderer.PushNewQuad(corners, colors);
    }

    void InternalPushText(Vector3 position, Color color, string text)
    {
        Text
        TextGenerationSettings settings = new TextGenerationSettings();
        settings.textAnchor = TextAnchor.MiddleCenter;
        settings.color = Color.red;
        settings.generationExtents = new Vector2(500.0F, 200.0F);
        settings.pivot = position;
        settings.richText = false;
        settings.font = m_DefaultFont;
        settings.fontSize = 32;
        settings.fontStyle = FontStyle.Normal;
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        
        TextGenerator generator = new TextGenerator(text.Length);
        generator.PopulateWithErrors(text, settings, s_Instance.gameObject);
        
        Debug.Log($"Generate {generator.vertexCount} vertices");
        
        var vertices = generator.GetVerticesArray();
        m_ScreenSpaceTextRenderer.PushNewCharVertices(vertices, Vector3.zero);
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
    
    
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawPixelScreenQuad(Vector3[] corners, Color[] color)
    { 
        s_Instance.InternalDrawFilledQuadPixelCoord(corners, color);
    }
    
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawNormalizedScreenQuad(Vector3[] corners, Color[] color)
    { 
        s_Instance.InternalDrawFilledQuadNormalizedCoord(corners, color);
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void DrawTextScreenSpace(Vector3 position, Color color, string text)
    {
       
        s_Instance.InternalPushText(position, color, text);
    }
}

public abstract class DebugGeometryRenderer
{
    protected const int k_StartMaxVertices = UInt16.MaxValue;

    public Mesh Mesh => m_Mesh;
    public Material Material => m_DebugMaterial;
    
    protected int m_MaxArraySize = k_StartMaxVertices;
    protected int m_VerticeCount = 0;
    protected Vector3[] m_Vertices = new Vector3[k_StartMaxVertices];
    protected Vector2[] m_UV = null;
    protected Color[] m_Colors = new Color[k_StartMaxVertices];
    protected int m_IndicesCount = 0;
    protected int[] m_Indices = new int[k_StartMaxVertices];
    
        
    protected Mesh m_Mesh;
    protected Material m_DebugMaterial;
    
    public DebugGeometryRenderer(Material materialUsed, bool UseUV = false)
    {
        m_Mesh = new Mesh();
        m_Mesh.MarkDynamic();

        if (UseUV)
            m_UV = new Vector2[k_StartMaxVertices];

        m_DebugMaterial = materialUsed;
    }
    
    public void Clear()
    {
        m_IndicesCount = 0;
        m_VerticeCount = 0;
    }

    public void BuildMesh()
    {
        InternalBuildMesh();
    }

    public void AddVertices(Vector3[] Vertices, Color[] Colors, int[] Indices, Vector3 Offset = default(Vector3))
    {
        for (int i = 0; i < Vertices.Length; ++i)
        {
            m_Vertices[m_VerticeCount + i] = Offset + Vertices[i];
        }

        Array.Copy(Colors, 0, m_Colors, m_VerticeCount, Colors.Length);

        for (int i = 0; i < Indices.Length; ++i)
        {
            m_Indices[m_IndicesCount + i] = m_VerticeCount + Indices[i];
        }

        m_VerticeCount += Vertices.Length;
        m_IndicesCount += Indices.Length;
    }

    protected abstract void InternalBuildMesh();

    protected virtual void IncreaseArraySize()
    {
        int newMaxSize = m_MaxArraySize + k_StartMaxVertices;
            
        var newQuadVertice = new Vector3[newMaxSize];
        var newQuadColor = new Color[newMaxSize];
        var newQuadIndices = new int[newMaxSize];

        Array.Copy(m_Vertices, newQuadVertice, m_VerticeCount);
        Array.Copy(m_Colors, newQuadColor, m_VerticeCount);
        Array.Copy(m_Indices, newQuadIndices, m_IndicesCount);

        if (m_UV != null)
        {
            var newUVs = new Vector2[newMaxSize];
            Array.Copy(m_UV, newUVs, m_VerticeCount);
            m_UV = newUVs;
        }
        
        m_MaxArraySize = newMaxSize;
        m_Vertices = newQuadVertice;
        m_Colors = newQuadColor;
        m_Indices = newQuadIndices;
    }
}

public class DebugLineRenderer : DebugGeometryRenderer
{
    public DebugLineRenderer(Material mat)
        : base(mat) { }
    
    protected override void InternalBuildMesh()
    {
        m_Mesh.Clear();
        m_Mesh.vertices = m_Vertices;
        m_Mesh.colors = m_Colors;
        
        m_Mesh.SetIndices(m_Indices, MeshTopology.Lines, 0);
    }

    public void PushNewLine(Vector3 start, Vector3 end, Color colorStart, Color colorEnd)
    {
        if (m_IndicesCount + 6 >= m_MaxArraySize)
        {
            IncreaseArraySize();
        }
        
        m_Vertices[m_VerticeCount] = start;
        m_Vertices[m_VerticeCount + 1] = end;

        m_Colors[m_VerticeCount] = colorStart;
        m_Colors[m_VerticeCount + 1] = colorEnd;

        m_Indices[m_IndicesCount] = m_VerticeCount;
        m_Indices[m_IndicesCount + 1] = m_VerticeCount + 1;

        m_VerticeCount += 2;
        m_IndicesCount += 2;
    }
}

public class DebugQuadRenderer : DebugGeometryRenderer
{
    public DebugQuadRenderer(Material mat)
        : base(mat) { }

    protected override void InternalBuildMesh()
    {
        m_Mesh.Clear();
        m_Mesh.vertices = m_Vertices;
        m_Mesh.colors = m_Colors;
        
        m_Mesh.SetIndices(m_Indices, MeshTopology.Triangles, 0);
    }
    
    public void PushNewQuad(Vector3[] corners, Color[] colors)
    {
        if (m_IndicesCount + 6 >= m_MaxArraySize)
        {
            IncreaseArraySize();
        }
        
        m_Vertices[m_VerticeCount] = corners[0];
        m_Vertices[m_VerticeCount + 1] = corners[1];
        m_Vertices[m_VerticeCount + 2] = corners[2];
        m_Vertices[m_VerticeCount + 3] = corners[3];

        int colorLength = colors.Length;
        
        m_Colors[m_VerticeCount] = colors[0];
        m_Colors[m_VerticeCount + 1] = colors[ 1 < colorLength - 1 ? 1 : colorLength - 1];
        m_Colors[m_VerticeCount + 2] = colors[ 2 < colorLength - 1 ? 2 : colorLength - 1];
        m_Colors[m_VerticeCount + 3] = colors[ 3 < colorLength - 1 ? 3 : colorLength - 1];

        m_Indices[m_IndicesCount] = m_VerticeCount;
        m_Indices[m_IndicesCount + 1] = m_VerticeCount + 1;
        m_Indices[m_IndicesCount + 2] = m_VerticeCount + 2;
        m_Indices[m_IndicesCount + 3] = m_VerticeCount;
        m_Indices[m_IndicesCount + 4] = m_VerticeCount + 2;
        m_Indices[m_IndicesCount + 5] = m_VerticeCount + 3;

        m_VerticeCount += 4;
        m_IndicesCount += 6;
    }
}

public class DebugTextDrawer : DebugGeometryRenderer
{
    public DebugTextDrawer(Material mat)
        : base(mat, true) { }

    protected override void InternalBuildMesh()
    {
        m_Mesh.Clear();
        m_Mesh.vertices = m_Vertices;
        m_Mesh.uv = m_UV;
        m_Mesh.colors = m_Colors;

        m_Mesh.SetIndices(m_Indices, MeshTopology.Triangles, 0);
    }

    public void PushNewCharVertices(UIVertex[] vertices, Vector3 offset)
    {
        while(vertices.Length * 2 > m_MaxArraySize)
            IncreaseArraySize();
        
        for (int i = 0; i < vertices.Length; ++i)
        {
            m_Vertices[m_VerticeCount + i] = vertices[i].position + offset;
            m_UV[m_VerticeCount + i] = vertices[i].uv0;
            m_Colors[m_VerticeCount + i] = vertices[i].color;
            
            if(i != 0 && i % 3 == 0)
            {
                int startIdx = i / 3 - 1;
                
                m_Indices[m_IndicesCount + startIdx * 6 + 0] = m_VerticeCount + i + 0;
                m_Indices[m_IndicesCount + startIdx * 6 + 1] = m_VerticeCount + i + 1;
                m_Indices[m_IndicesCount + startIdx * 6 + 2] = m_VerticeCount + i + 2;

                m_Indices[m_IndicesCount + startIdx * 6 + 3] = m_VerticeCount + i + 0;
                m_Indices[m_IndicesCount + startIdx * 6 + 4] = m_VerticeCount + i + 2;
                m_Indices[m_IndicesCount + startIdx * 6 + 5] = m_VerticeCount + i + 3;
            }
        }
    }
}