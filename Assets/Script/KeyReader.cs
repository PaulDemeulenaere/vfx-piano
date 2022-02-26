using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class KeyReader : MonoBehaviour
{
    public ComputeShader Reduction;

    private int m_Kernel_Clear;
    private int m_Kernel_Reduce;

    private GraphicsBuffer m_Keys;
    private static readonly int s_KeysID = Shader.PropertyToID("Keys");

    void Start()
    {
        m_Keys = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 8, 4);

        m_Kernel_Clear = Reduction.FindKernel("Clear_Keys");
        m_Kernel_Reduce = Reduction.FindKernel("Compute_Keys");
    }

    void Update()
    {
        Reduction.SetBuffer(m_Kernel_Clear, s_KeysID, m_Keys);
        Reduction.Dispatch(m_Kernel_Clear, 1, 1, 1);

        Reduction.SetBuffer(m_Kernel_Reduce, s_KeysID, m_Keys);
        Reduction.Dispatch(m_Kernel_Reduce, 8, 8, 1); //Arbitrary shoule rely on input texture dimension

        AsyncGPUReadback.Request(m_Keys, OnCompleteReadback);
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }

        Debug.Log(request.GetData<uint>().Select(a => a.ToString()).Aggregate((a, b) => a + ", " + b));
    }

    void OnDestroy()
    {
        AsyncGPUReadback.WaitAllRequests();
        m_Keys.Dispose();
    }
}
