using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;

struct Particle
{
    public int id;
    public bool active;
    public Vector3 position;
    public Vector3 rotation;
    public float scale;
}

public class ComputeBehaviourScript : MonoBehaviour
{
    const int MAX_VERTEX_NUM = 65534;

    [SerializeField, Tooltip("This cannot be changed while running.")]
    int maxInstanceNum = 10000;
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Shader shader;
    [SerializeField]
    ComputeShader computeShader;

    [SerializeField]
    Vector3 range = new Vector3(100f, 0f, 100f);// 広がる範囲

    Mesh combinedMesh_;
    ComputeBuffer computeBuffer_;// 計算シェーダ用バッファ
    int emitKernel_;
    List<Material> materials_ = new List<Material>();
    int numPerMesh_;// メッシュごとのインスタンス数
    int meshNum_;// 描画メッシュ数

    Mesh CreateCombinedMesh(Mesh mesh, int num)
    {
        Assert.IsTrue(mesh.vertexCount * num <= MAX_VERTEX_NUM);

        var meshIndices = mesh.GetIndices(0);
        var indexNum = meshIndices.Length;

        var vertices = new List<Vector3>();
        var indices = new int[num * indexNum];
        var normals = new List<Vector3>();
        var tangents = new List<Vector4>();
        var uv0 = new List<Vector2>();
        var uv1 = new List<Vector2>();

        for (int id = 0; id < num; ++id)
        {
            vertices.AddRange(mesh.vertices);
            normals.AddRange(mesh.normals);
            tangents.AddRange(mesh.tangents);
            uv0.AddRange(mesh.uv);

            // 各メッシュのインデックスは（1 つのモデルの頂点数 * ID）分ずらす
            for (int n = 0; n < indexNum; ++n)
            {
                indices[id * indexNum + n] = id * mesh.vertexCount + meshIndices[n];
            }

            // 2 番目の UV に ID を格納しておく
            for (int n = 0; n < mesh.uv.Length; ++n)
            {
                uv1.Add(new Vector2(id, id));
            }
        }

        var combinedMesh = new Mesh();
        combinedMesh.SetVertices(vertices);
        combinedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        combinedMesh.SetNormals(normals);
        combinedMesh.RecalculateNormals();
        combinedMesh.SetTangents(tangents);
        combinedMesh.SetUVs(0, uv0);
        combinedMesh.SetUVs(1, uv1);
        combinedMesh.RecalculateBounds();
        combinedMesh.bounds.SetMinMax(Vector3.one * -100f, Vector3.one * 100f);

        return combinedMesh;
    }

    void OnEnable()
    {
        numPerMesh_ = MAX_VERTEX_NUM / mesh.vertexCount;
        meshNum_ = (int)Mathf.Ceil((float)maxInstanceNum / numPerMesh_);

        for (int i = 0; i < meshNum_; ++i)
        {
            var material = new Material(shader);
            material.SetInt("_IdOffset", numPerMesh_ * i);
            materials_.Add(material);
        }

        combinedMesh_ = CreateCombinedMesh(mesh, numPerMesh_);
        computeBuffer_ = new ComputeBuffer(maxInstanceNum, Marshal.SizeOf(typeof(Particle)), ComputeBufferType.Default);

        var initKernel = computeShader.FindKernel("Init");
        emitKernel_ = computeShader.FindKernel("Emit");

        computeShader.SetBuffer(initKernel, "_Particles", computeBuffer_);
        computeShader.SetVector("_Range", range);
        computeShader.Dispatch(initKernel, maxInstanceNum / 8, 1, 1);
    }

    void OnDisable()
    {
        computeBuffer_.Release();
    }

    void Update()
    {
        for (int i = 0; i < meshNum_; ++i)
        {
            var material = materials_[i];
            material.SetInt("_IdOffset", numPerMesh_ * i);
            material.SetBuffer("_Particles", computeBuffer_);
            Graphics.DrawMesh(combinedMesh_, transform.position, transform.rotation, material, 0);
        }
    }
}