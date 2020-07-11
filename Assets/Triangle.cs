using System;
using UnityEngine;

public class Triangle
{
    public class Edge
    {
        private Triangle mOwner;
        private int mVertIndex;
        private int mEdgeIndex;
        private LineSegment mEdgeLine;

        public Triangle Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }

        public LineSegment EdgeLine
        {
            get { return mEdgeLine; }
        }

        public int EdgeIndex
        {
            get { return mEdgeIndex; }
        }

        public int VertIndex
        {
            get { return mVertIndex; }
            set { mVertIndex = value; }
        }

        public Edge(Triangle tri, int edgeIndex, int vertIndex, LineSegment line)
        {
            Owner = tri;
            VertIndex = vertIndex;
            mEdgeIndex = edgeIndex;
            mEdgeLine = line;
        }
    }

    public int VertexIndex;
    public Color TriColor;
    public Edge[] Edges = new Edge[3];

    private static readonly float EPSILON = 1e-5f;
    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, int vindex = 0)
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
             source.VertexIndex);
    }

    private void Init(Vector3 v1, Vector3 v2, Vector3 v3, int vindex)
    {
        VertexIndex = vindex;
        Vector3[] verts = new Vector3[3];
        verts[0] = v1;
        verts[1] = v2;
        verts[2] = v3;
        if (((v1 - v2).sqrMagnitude < EPSILON) ||
            ((v1 - v3).sqrMagnitude < EPSILON) ||
            ((v3 - v2).sqrMagnitude < EPSILON))
        {
            throw new ArgumentException("degenerate triangle");
        }
        if (v1.x > v2.x)
        {
            if (v3.x < v2.x)       // v1 > v2 > v3
            {
                verts[0] = v1;
                verts[1] = v2;
                verts[2] = v3;
            }
            else if (v1.x > v3.x)  // v1 > v3 > v2
            {
                verts[0] = v1;
                verts[1] = v3;
                verts[2] = v2;
            }
            else                    // v3 > v1 > v2
            {
                verts[0] = v3;
                verts[1] = v1;
                verts[2] = v2;
            }
        }
        else if (v3.x > v2.x)       // v3 > v2 > v1
        {
            verts[0] = v3;
            verts[1] = v2;
            verts[2] = v1;
        }
        else if (v3.x < v1.x)      // v2 > v1 > v3
        {
            verts[0] = v2;
            verts[1] = v1;
            verts[2] = v3;
        }
        else                        // v2 > v3 > v1
        {
            verts[0] = v2;
            verts[1] = v3;
            verts[2] = v1;
        }
        Edges[0] = new Edge(this, 0, vindex, new LineSegment(verts[0], verts[1]));
        Edges[1] = new Edge(this, 1, vindex + 1, new LineSegment(verts[1], verts[2]));
        Edges[2] = new Edge(this, 2, vindex + 2, new LineSegment(verts[2], verts[0]));
        TriColor = new Color(UnityEngine.Random.value,
                            UnityEngine.Random.value,
                            UnityEngine.Random.value, 0.5f);
    }

    public bool IsDegenerate()
    {
        if (((Edges[0].EdgeLine.Start - Edges[0].EdgeLine.End).sqrMagnitude < EPSILON) ||
            ((Edges[1].EdgeLine.Start - Edges[1].EdgeLine.End).sqrMagnitude < EPSILON) ||
            ((Edges[2].EdgeLine.Start - Edges[2].EdgeLine.End).sqrMagnitude < EPSILON))
        {
            return true;
        }
        return false;
    }

    public float GetArea()
    {
        float area = GetVertex(0).x * (GetVertex(1).y - GetVertex(2).z) +
                     GetVertex(1).x * (GetVertex(2).y - GetVertex(0).z) +
                     GetVertex(2).x * (GetVertex(0).y - GetVertex(1).z);
        return Mathf.Abs(area) / 2;
    }

    public Vector3[] Vertices
    {
        get
        {
            return new Vector3[]
            {
                GetVertex(0),
                GetVertex(1),
                GetVertex(2)
            };
        }
    }

    public Vector3 GetVertex(int vindex)
    {
        return Edges[vindex].EdgeLine.Start;
    }

    public Edge GetEdge(int i)
    {
        return Edges[i];
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

    public int Intersects(int edgeindex1, LineSegment edge2, ref Vector3 isect)
    {
        return Edges[edgeindex1].EdgeLine.FindIntersection(edge2, ref isect);
    }

    /*
     * Determine if a point is inside, outside or on the edge of this triangle
     * @returns -1 = point is outside triangle, 0 = on the edge, 1 = inside
     */
    public int Contains(Vector3 p)
    {
        Vector3 p1 = GetVertex(0);
        Vector3 p2 = GetVertex(1);
        Vector3 p3 = GetVertex(2);
        Vector3 d2 = p3 - p2;
        Vector3 d3 = p1 - p3;
        float denom = (-d2.y * d3.x + d2.x * d3.y);

        if (denom == 0)
        {
            return -1;
        }
        float alpha = (-d2.y * (p.x - p3.x) + d2.x * (p.y - p3.y)) / denom;
        float beta = (-d3.y * (p.x - p3.x) + d3.x * (p.y - p3.y)) / denom;
        float gamma = 1.0f - alpha - beta;

        if ((alpha >= 0) && (gamma >= 0) && (Math.Abs(beta) < 2e-7))
        {
            return 0;
        }
        if ((beta >= 0) && (gamma >= 0) && (Math.Abs(alpha) < 2e-7))
        {
            return 0;
        }
        if ((alpha >= 0) && (beta >= 0) && (Math.Abs(gamma) < 2e-7))
        {
            return 0;
        }
        return (alpha > 0) && (beta > 0) && (gamma > 0) ? 1 : -1;
    }

    public bool Contains(Triangle t)
    {
        int i1 = Contains(t.GetVertex(0));
        int i2 = Contains(t.GetVertex(1));
        int i3 = Contains(t.GetVertex(2));

        if ((i1 + i2 + i3) == 0)
        {
            return false;
        }
        if ((i1 >= 0) &&
            (i2 >= 0) &&
            (i3 >= 0))
        {
            return true;
        }
        return false;
    }
}
