﻿using System;
using System.Collections.Generic;
using UnityEngine;

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
        Color c;

        tri.TriColor.a = 0.5f;
        c = tri.TriColor;
        if ((tri.VertexIndex >= 0) &&
           ((tri.VertexIndex + 2) < index))
         {
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
        mColors.Add(c);
        mColors.Add(c);
        mColors.Add(c);
        return index;
    }

    public int RemoveTriangle(Triangle tri)
    {
        Color c = tri.TriColor;
        if ((c.a == 0) ||
            (tri.VertexIndex < 0) || 
            (tri.VertexIndex + 2 > mColors.Count))
        {
            return -1;
        }
        c.a = 0;
        mColors[tri.VertexIndex] = c;
        mColors[tri.VertexIndex + 1] = c;
        mColors[tri.VertexIndex + 2] = c;
        tri.TriColor = c;
        return tri.VertexIndex;
    }

    public void GenerateMesh(List<Triangle> trilist)
    {
        Clear();
        VertexCount = trilist.Count * 3;
        foreach (Triangle t in trilist)
        {
            AddTriangle(t);
        }
        Display();
    }


}
