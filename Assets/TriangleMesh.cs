using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TriangleMesh : MonoBehaviour
{
    public int TriangleCount = 2;

    private float mMaxDist = 5;
    private float mMinDist = 0.2f;
    private List<Vector3> mVertices;
    private List<Color> mColors;
    private List<int> mIndices;
    private Mesh mMesh;
    private Rect mBounds;

    public void Awake()
    {
        int numverts = TriangleCount * 3;
        mMesh = gameObject.GetComponent<MeshFilter>().mesh;
        mVertices = new List<Vector3>(numverts);
        mIndices = new List<int>(numverts);
        mColors = new List<Color>(numverts);
        mMesh.Clear();
        mMesh.SetVertices(mVertices);
        mMesh.SetColors(mColors);
        mMesh.SetTriangles(mIndices, 0, true);
    }

   public int VertexCount
    {
        get { return mVertices.Count;  }
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
        mVertices.Clear();
        mIndices.Clear();
        mColors.Clear();
    }

    public int AddTriangle(Triangle tri)
    {
        int index = mIndices.Count;
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

    public void UpdateTriangle(Triangle tri)
    {
        int i = tri.VertexIndex;
        mVertices[i].Set(tri.Edges[0].Vertex.x, tri.Edges[0].Vertex.y, 0);
        mVertices[i + 1].Set(tri.Edges[1].Vertex.x, tri.Edges[1].Vertex.y, 0);
        mVertices[i + 2].Set(tri.Edges[2].Vertex.x, tri.Edges[2].Vertex.y, 0);
        mColors[i] = tri.TriColor;
        mColors[i + 1] = tri.TriColor;
        mColors[i + 2] = tri.TriColor;
    }

    public void NewTriangles(Rect bounds)
    {
        Color color = new Color(1, 1, 1, 0.5f);
        float size = bounds.width;
        int numverts = TriangleCount * 3;
        mBounds = bounds;
        for (int i = 0; i < numverts; i += 3)
        {
            /*
             * generate 3 triangles with random vertices
             */
            float x = Random.value - 0.5f;
            float y = Random.value - 0.5f;
            Vector3 v1;
            Vector3 v2;
            Vector3 v3;

            color.r = Random.value;
            color.g = Random.value;
            color.b = Random.value;
            color.a = 0.5f;
            mColors.Add(color);
            mColors.Add(color);
            mColors.Add(color);
            mIndices.Add(i);
            mIndices.Add(i + 1);
            mIndices.Add(i + 2);
            v1 = new Vector3(size * x + bounds.x,
                             size * y + bounds.y,
                             0);
            mVertices.Add(v1);
            do
            {
                x = Random.value - 0.5f;
                y = Random.value - 0.5f;
                v2 = new Vector3(size * x + bounds.x,
                                 size * y + bounds.y,
                                 0);
                x = Random.value - 0.5f;
                y = Random.value - 0.5f;
                v3 = new Vector3(size * x + bounds.x,
                                 size * y + bounds.y,
                                 0);
            }
            while (!CheckTriSize(v1, v2, v3));
            if (Hull.Cross2D(v1, v2, v3) < 0)
            {
                mVertices.Add(v2);
                mVertices.Add(v3);
            }
            else
            {
                mVertices.Add(v3);
                mVertices.Add(v2);
            }
        }
        Display();
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

    public void GenerateMesh(TriangleList trilist)
    {
        mMesh.Clear();
        mIndices.Clear();
        mVertices.Clear();
        mColors.Clear();
        foreach (Triangle t in trilist.Triangles)
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

    bool CheckTriSize(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 v = v1 - v2;
        float m = v.magnitude;
        if ((m > mMaxDist) || (m < mMinDist))
        {
            return false;
        }
        v = v1 - v3;
        m = v.magnitude;
        if ((m > mMaxDist) || (m < mMinDist))
        {
            return false;
        }
        return true;
    }

}
