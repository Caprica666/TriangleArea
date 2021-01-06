using System;
using System.Collections.Generic;
using UnityEngine;


public class Triangle
{
    static public int NextID = 0;
    public int ID = 0;
    public int VertexIndex = -1;
    public Color TriColor;
    public Vector3[] Vertices = new Vector3[3];
    public Edge[] Edges = new Edge[3];
    public Bounds BoundBox = new Bounds();

    private static readonly float EPSILON = 2e-6f;
    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, int vindex = -1)
    {
        Init(v1, v2, v3, vindex);
    }

    public Triangle(Triangle source)
    {
        Vector3 v1 = source.GetVertex(0);
        Vector3 v2 = source.GetVertex(1);
        Vector3 v3 = source.GetVertex(2);
        Init(new Vector3(v1.x, v1.y, v1.z),
             new Vector3(v2.x, v2.y, v2.z),
             new Vector3(v3.x, v3.y, v3.z),
             -1);
    }

    public int GetNextID()
    {
        return NextID++;
    }

    private void Init(Vector3 v1, Vector3 v2, Vector3 v3, int vindex)
    {
        VertexIndex = vindex;
        ID = GetNextID();
        if (v1.x > v2.x)
        {
            if (v3.x < v2.x)       // v1 > v2 > v3
            {
                Vertices[0] = v3;
                Vertices[1] = v2;
                Vertices[2] = v1;
            }
            else if (v1.x > v3.x)  // v1 > v3 > v2
            {
                Vertices[0] = v2;
                Vertices[1] = v3;
                Vertices[2] = v1;
            }
            else                    // v3 > v1 > v2
            {
                Vertices[0] = v2;
                Vertices[1] = v1;
                Vertices[2] = v3;
            }
        }
        else if (v3.x > v2.x)       // v3 > v2 > v1
        {
            Vertices[0] = v1;
            Vertices[1] = v2;
            Vertices[2] = v3;
        }
        else if (v3.x < v1.x)      // v2 > v1 > v3
        {
            Vertices[0] = v3;
            Vertices[1] = v1;
            Vertices[2] = v2;
        }
        else                        // v2 > v3 > v1
        {
            Vertices[0] = v1;
            Vertices[1] = v3;
            Vertices[2] = v2;
        }
        TriColor = new Color(UnityEngine.Random.value,
                            UnityEngine.Random.value,
                            UnityEngine.Random.value, 0.5f);
        Edges[0] = new Edge(this, 0);
        Edges[1] = new Edge(this, 1);
        Edges[2] = new Edge(this, 2);
        BoundBox.Encapsulate(v1);
        BoundBox.Encapsulate(v2);
        BoundBox.Encapsulate(v3);
    }

    public float GetArea()
    {
        float area = GetVertex(0).x * (GetVertex(1).y - GetVertex(2).z) +
                     GetVertex(1).x * (GetVertex(2).y - GetVertex(0).z) +
                     GetVertex(2).x * (GetVertex(0).y - GetVertex(1).z);
        return Mathf.Abs(area) / 2;
    }

    public Vector3 GetVertex(int vindex)
    {
        return Vertices[vindex];
    }

    public static float GetArea(Vector3 v1,
                                Vector3 v2,
                                Vector3 v3)
    {
        float area = v1.x * (v2.y - v3.y) +
                     v2.x * (v3.y - v1.y) +
                     v3.x * (v1.y - v2.y);
        return Mathf.Abs(area) / 2;
    }

    public void Update(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        if (((v1 - v2).sqrMagnitude < 1e-7) ||
            ((v1 - v3).sqrMagnitude < 1e-7) ||
            ((v3 - v2).sqrMagnitude < 1e-7))
        {
            throw new ArgumentException("degenerate triangle");
        }
        Init(v1, v2, v3, VertexIndex);
    }

    /*
     * Determine if a point is inside, outside or on the edge of this triangle
     * @returns -1 = point is outside triangle, 0 = on the edge, 1 = inside
     */
    public int Contains(Vector3 p)
    {
        Vector3 bc = new Vector3();
        if (!Bary(p, ref bc))
        {
            return -1;
        }
        float alpha = bc[0];
        float beta = bc[1];
        float gamma = bc[2];
        bool alphais0 = Math.Abs(alpha) < EPSILON;
        bool betais0 = Math.Abs(beta) < EPSILON;
        bool gammais0 = Math.Abs(gamma) < EPSILON;

        if ((alphais0 && betais0) ||
            (alphais0 && gammais0) ||
            (betais0 && gammais0))
        {
            return 0;
        }
        if ((alpha >= 0) && (gamma >= 0) && betais0)
        {
            return 0;
        }
        if ((beta >= 0) && (gamma >= 0) && alphais0)
        {
            return 0;
        }
        if ((alpha >= 0) && (beta >= 0) && gammais0)
        {
            return 0;
        }
        return (alpha > EPSILON) && (beta > EPSILON) && (gamma > EPSILON) ? 1 : -1;
    }

    public bool Bary(Vector3 p, ref Vector3 bc)
    {
        Vector3 p1 = GetVertex(0);
        Vector3 p2 = GetVertex(1);
        Vector3 p3 = GetVertex(2);
        Vector3 d2 = p3 - p2;
        Vector3 d3 = p1 - p3;
        float denom = (-d2.y * d3.x + d2.x * d3.y);

        if (denom == 0)
        {
            return false;
        }
        bc[0] = (-d2.y * (p.x - p3.x) + d2.x * (p.y - p3.y)) / denom;
        bc[1] = (-d3.y * (p.x - p3.x) + d3.x * (p.y - p3.y)) / denom;
        bc[2] = 1.0f - bc[0] - bc[1];
        return true;
    }

    public Vector3 Bary(Vector3 p)
    {
        Vector3 bc = new Vector3();
        Bary(p, ref bc);
        return bc;
    }

    public bool Contains(Triangle t)
    {
        int i1 = Contains(t.GetVertex(0));
        int i2 = Contains(t.GetVertex(1));
        int i3 = Contains(t.GetVertex(2));

        if ((i1 | i2 | i3) == 0)
        {
            return true;
        }
        if ((i1 >= 0) &&
            (i2 >= 0) &&
            (i3 >= 0))
        {
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        return String.Format("T: {0:0} ({1:0.#}, {2:0.#}, {3:0.#})",
                            ID, TriColor.r, TriColor.g, TriColor.b);
    }
}
