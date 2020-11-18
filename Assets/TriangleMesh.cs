using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TriangleMesh : MonoBehaviour
{
    private List<Vector3> mVertices;
    private List<Color> mColors;
    private List<int> mIndices;
    private Mesh mMesh;

    public void Awake()
    {
        mMesh = gameObject.GetComponent<MeshFilter>().mesh;
        mMesh.Clear();
    }

   public int VertexCount
    {
        get { return mVertices.Count; }
        set
        {
            int numverts = value;
            mVertices = new List<Vector3>(numverts);
            mIndices = new List<int>(numverts);
            mColors = new List<Color>(numverts);
        }
    }

    public void Display()
    {
        mMesh.SetVertices(mVertices);
        mMesh.SetColors(mColors);
        mMesh.SetTriangles(mIndices, 0, true);
        mMesh.RecalculateBounds();
    }

    public void Clear()
    {
        mMesh.Clear();
        if (mVertices != null)
        {
            mVertices.Clear();
            mIndices.Clear();
            mColors.Clear();
        }
    }

    public int AddTriangle(Triangle tri)
    {
        int index = mVertices.Count;

        if ((tri.VertexIndex >= 0) &&
           ((tri.VertexIndex + 2) < index))
         {
            Color c = tri.TriColor;
            c.a = 0.5f;
            index = tri.VertexIndex;
            mVertices[index] = tri.GetVertex(0);
            mVertices[index + 1] = tri.GetVertex(1);
            mVertices[index + 2] = tri.GetVertex(2);
            mColors[index] = c;
            mColors[index + 1] = c;
            mColors[index + 2] = c;
            return index;
        }
        tri.VertexIndex = index;
        mVertices.Add(tri.GetVertex(0));
        mVertices.Add(tri.GetVertex(1));
        mVertices.Add(tri.GetVertex(2));
        mIndices.Add(index);
        mIndices.Add(index + 1);
        mIndices.Add(index + 2);
        mColors.Add(tri.TriColor);
        mColors.Add(tri.TriColor);
        mColors.Add(tri.TriColor);
        if (tri.VertexIndex + 2 > mColors.Count)
        {
            return -1;
        }
        return tri.VertexIndex;
    }

    public int RemoveTriangle(Triangle tri)
    {
        Color c = tri.TriColor;
        c.a = 0;
        if ((tri.VertexIndex < 0) || 
            (tri.VertexIndex + 2 > mColors.Count))
        {
            return -1;
        }
        mColors[tri.VertexIndex] = c;
        mColors[tri.VertexIndex + 1] = c;
        mColors[tri.VertexIndex + 2] = c;
        return tri.VertexIndex;
    }

    public void GenerateMesh(List<Triangle> trilist)
    {
        Clear();
        foreach (Triangle t in trilist)
        {
            t.VertexIndex = mVertices.Count;
            mVertices.Add(t.GetVertex(0));
            mVertices.Add(t.GetVertex(1));
            mVertices.Add(t.GetVertex(2));
            mIndices.Add(mIndices.Count);
            mIndices.Add(mIndices.Count);
            mIndices.Add(mIndices.Count);
            mColors.Add(t.TriColor);
            mColors.Add(t.TriColor);
            mColors.Add(t.TriColor);
        }
        Display();
    }


}
