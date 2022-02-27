using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

class MaterialUpdater
{
    private static readonly int s_EmissiveID = Shader.PropertyToID("_Emissive");

    private Material[] m_Materials;
    private float[] m_EmissiveState;

    public MaterialUpdater(Material[] input)
    {
        m_Materials = input;
        m_EmissiveState = new float[input.Length];
    }

    public void Update(bool[] keyPressed, float dt)
    {
        for (int key = 0; key < keyPressed.Length; ++key)
        {
            if (keyPressed[key])
                m_EmissiveState[key] = 1.0f;
            else
                m_EmissiveState[key] = Mathf.Lerp(m_EmissiveState[key], 0.0f, dt * 10.0f);

            m_Materials[key].SetFloat(s_EmissiveID, m_EmissiveState[key]);
        }
    }
}

class KeyReader : MonoBehaviour
{
    //Rendering Settings
    public ComputeShader Reduction;
    public RenderTexture Capture;

    //Other settings
    public Material[] Materials;

    private int m_Kernel_Clear;
    private int m_Kernel_Reduce;

    private GraphicsBuffer m_Keys;
    private static readonly int s_KeysID = Shader.PropertyToID("Keys");
    private static readonly int s_CaptureID = Shader.PropertyToID("Capture");

    private static readonly int s_Keys_Count = 8;

    private bool[] m_KeyPressed;

    //Modifier
    private MaterialUpdater m_MaterialUpdater;

    void Start()
    {
        m_Keys = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 8, 4);
        m_KeyPressed = new bool[s_Keys_Count];

        m_Kernel_Clear = Reduction.FindKernel("Clear_Keys");
        m_Kernel_Reduce = Reduction.FindKernel("Compute_Keys");

        m_MaterialUpdater = new MaterialUpdater(Materials);
    }

    private static readonly int s_ThreadGroupSize = 8;

    void Update()
    {
        //Rendering Callback
        Reduction.SetBuffer(m_Kernel_Clear, s_KeysID, m_Keys);
        Reduction.Dispatch(m_Kernel_Clear, 1, 1, 1);

        Reduction.SetBuffer(m_Kernel_Reduce, s_KeysID, m_Keys);
        Reduction.SetTexture(m_Kernel_Reduce, s_CaptureID, Capture);

        //Assuming Capture.width/height is a multiple of s_ThreadGroupSize
        Reduction.Dispatch(m_Kernel_Reduce, Capture.width / s_ThreadGroupSize, Capture.height / s_ThreadGroupSize, 1);
        AsyncGPUReadback.Request(m_Keys, OnCompleteReadback);

        //Update Scene
        m_MaterialUpdater.Update(m_KeyPressed, Time.deltaTime);
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogError("GPU readback error detected.");
            return;
        }

        var data = request.GetData<uint>();
        if (data.Length != s_Keys_Count)
        {
            Debug.LogError("Unexpected Key Count.");
        }

        for (int key = 0; key < s_Keys_Count; ++key)
        {
            m_KeyPressed[key] = data[key] > 16; //Arbitrary treshold
        }
    }

    void OnDestroy()
    {
        AsyncGPUReadback.WaitAllRequests();
        m_Keys.Dispose();
    }
}
