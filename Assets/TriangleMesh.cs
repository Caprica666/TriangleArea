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
    private Rect mBounds;

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
        tri.VertexIndex = index;
        mVertices.Add(tri.GetVertex(0));
        mVertices.Add(tri.GetVertex(1));
        mVertices.Add(tri.GetVertex(2));
        mIndices.Add(index++);
        mIndices.Add(index++);
        mIndices.Add(index++);
        mColors.Add(tri.TriColor);
        mColors.Add(tri.TriColor);
        mColors.Add(tri.TriColor);
        return index;
    }

    public int RemoveTriangle(Triangle tri)
    {
        Color c = tri.TriColor;
        c.a = 0;
        mColors[tri.VertexIndex] = c;
        mColors[tri.VertexIndex + 1] = c;
        mColors[tri.VertexIndex + 2] = c;
        return tri.VertexIndex;
    }

    public void GenerateMesh(List<Triangle> trilist)
    {
        mMesh.Clear();
        mIndices.Clear();
        mVertices.Clear();
        mColors.Clear();
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

    public void UpdateTriangle(Triangle tri)
    {
        int i = tri.VertexIndex;
        mVertices[i].Set(tri.Edges[0].EdgeLine.Start.x, tri.Edges[0].EdgeLine.Start.y, 0);
        mVertices[i + 1].Set(tri.Edges[1].EdgeLine.Start.x, tri.Edges[1].EdgeLine.Start.y, 0);
        mVertices[i + 2].Set(tri.Edges[2].EdgeLine.Start.x, tri.Edges[2].EdgeLine.Start.y, 0);
        mColors[i] = tri.TriColor;
        mColors[i + 1] = tri.TriColor;
        mColors[i + 2] = tri.TriColor;
    }

    public List<Triangle.Edge> PrepareTriangles(TriangleList trilist, bool isnew = false)
    {
        trilist.Clear();
        for (int t = 0; t < mIndices.Count; t += 3)
        {
            int i1 = mIndices[t];
            int i2 = mIndices[t + 1];
            int i3 = mIndices[t + 2];
            Vector3 v1 = mVertices[i1];
            Vector3 v2 = mVertices[i2];
            Vector3 v3 = mVertices[i3];
            Triangle tri = new Triangle(v1, v2, v3, t);

            if (isnew)
            {
                tri.TriColor = mColors[t];
            }
            else
            {
                Color color = new Color(Random.value, Random.value, Random.value, 0.5f);
                mColors[t] = color;
                mColors[t + 1] = color;
                mColors[t + 2] = color;
                tri.TriColor = color;
            }
            trilist.Add(tri);
        }
        trilist.SortByX();
        return trilist.GetEdges();
    }


}
