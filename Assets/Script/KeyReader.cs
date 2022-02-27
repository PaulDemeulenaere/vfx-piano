using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
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

    public void Reset()
    {
        foreach (var mat in m_Materials)
        {
            mat.SetFloat(s_EmissiveID, 0.0f);
        }
    }
}

class AudioUpdater
{
    private AudioSource[] m_Sources;

    public static float[] ComputeSine(float len, float freq, uint samplesPerSec)
    {
        var samples = (uint)(samplesPerSec * len);
        var ret = new float[samples];

        for (int i = 0; i < samples; ++i)
            ret[i] = Mathf.Sin(i / (float)samplesPerSec * freq * Mathf.PI * 2.0f);

        return ret;
    }

    public AudioUpdater(int keyCount)
    {
        m_Sources = new AudioSource[keyCount];

        float baseA = 440;
        uint bitrate = 44100;
        for (int key = 0; key < keyCount; ++key)
        {
            var gameObject = new GameObject("Key_" + key);
            gameObject.transform.position = new Vector3(Mathf.Lerp(-4f, 4f, (float) key / keyCount) + 0.5f, 0, 0);

            var audioSource = gameObject.AddComponent<AudioSource>();

            var pcm = ComputeSine(5.0f, baseA * Mathf.Pow(2.0f, key/12.0f), bitrate);
            var baseKey = AudioClip.Create("Key_" + key, pcm.Length, 1, (int)bitrate, false);
            baseKey.SetData(pcm, 0);

            audioSource.clip = baseKey;
            m_Sources[key] = audioSource;
        }
    }

    public void Update(bool[] keyPressed, float dt)
    {
        for (int key = 0; key < keyPressed.Length; ++key)
        {
            if (keyPressed[key])
            {
                m_Sources[key].volume = 1.0f;
                if (!m_Sources[key].isPlaying)
                    m_Sources[key].Play();
            }
            else
            {
                m_Sources[key].volume = Mathf.Lerp(m_Sources[key].volume, 0.0f, dt * 10.0f);
                if (m_Sources[key].volume < 0.01f)
                    m_Sources[key].Stop();
            }
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
    private static readonly int s_Capture_WidthID = Shader.PropertyToID("Capture_Width");

    private static readonly int s_Keys_Count = 8;

    private bool[] m_KeyPressed;

    //Modifiers
    private MaterialUpdater m_MaterialUpdater;
    private AudioUpdater m_AudioUpdater;

    void Start()
    {
        m_Keys = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 8, 4);
        m_KeyPressed = new bool[s_Keys_Count];

        m_Kernel_Clear = Reduction.FindKernel("Clear_Keys");
        m_Kernel_Reduce = Reduction.FindKernel("Compute_Keys");

        m_MaterialUpdater = new MaterialUpdater(Materials);
        m_AudioUpdater = new AudioUpdater(s_Keys_Count);
    }

    private static readonly int s_ThreadGroupSize = 8;

    void Update()
    {
        //Rendering Callback
        Reduction.SetBuffer(m_Kernel_Clear, s_KeysID, m_Keys);
        Reduction.Dispatch(m_Kernel_Clear, 1, 1, 1);

        Reduction.SetBuffer(m_Kernel_Reduce, s_KeysID, m_Keys);
        Reduction.SetTexture(m_Kernel_Reduce, s_CaptureID, Capture);
        Reduction.SetInt( s_Capture_WidthID, Capture.width);
        //Assuming Capture.width/height is a multiple of s_ThreadGroupSize
        Reduction.Dispatch(m_Kernel_Reduce, Capture.width / s_ThreadGroupSize, Capture.height / s_ThreadGroupSize, 1);
        AsyncGPUReadback.Request(m_Keys, OnCompleteReadback);

        //Update Scene
        m_MaterialUpdater.Update(m_KeyPressed, Time.deltaTime);
        m_AudioUpdater.Update(m_KeyPressed, Time.deltaTime);
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
        m_MaterialUpdater.Reset();
    }
}
