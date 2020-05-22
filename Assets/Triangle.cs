using System;
using UnityEngine;

public class Triangle
{
    public class Edge
    {
        private Triangle mOwner;
        private int mVertIndex;
        private Vector3 mVertex;
        private LineSegment mEdgeLine;

        public Vector3 Vertex
        {
            get { return mVertex; }
            set { mVertex = value; }
        }

        public Triangle Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }

        public LineSegment EdgeLine
        {
            get { return mEdgeLine; }
            set { mEdgeLine = value; }
        }

        public int VertIndex
        {
            get { return mVertIndex; }
            set { mVertIndex = value; }
        }

        public Edge(Triangle tri, int vertIndex, LineSegment edgeline)
        {
            Owner = tri;
            VertIndex = vertIndex;
            EdgeLine = edgeline;
            Vertex = edgeline.Start;
        }

        public Edge(Triangle tri, int vertIndex, Vector3 vtx)
        {
            Owner = tri;
            VertIndex = vertIndex;
            Vertex = vtx;
            EdgeLine = null;
        }

        public Edge(int vertIndex, Vector3 vtx)
        {
            Owner = null;
            VertIndex = vertIndex;
            Vertex = vtx;
            EdgeLine = null;
        }
    }

    public int VertexIndex;
    public Edge[] Edges = new Edge[3];
    public Color TriColor;

    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, int vindex)
    {
        VertexIndex = vindex;
        Vector3[] verts = new Vector3[3];
        verts[0] = v1;
        verts[1] = v2;
        verts[2] = v3;
        if (((v1 - v2).sqrMagnitude < 1e-7) ||
            ((v1 - v3).sqrMagnitude < 1e-7) ||
            ((v3 - v2).sqrMagnitude < 1e-7))
        {
            throw new ArgumentException("degenerate triangle");
        }
        if (v2.x < v1.x)
        {
            if (v3.x < v2.x)
            {
                verts[0] = v3;
                verts[2] = v1;
            }
            else
            {
                verts[0] = v2;
                verts[1] = v1;
            }
        }
        else if (v3.x < v1.x)
        {
            verts[0] = v3;
            verts[2] = v1;
        }
        Edges[0] = new Edge(this, 0, new LineSegment(verts[0], verts[1]));
        Edges[1] = new Edge(this, 1, new LineSegment(verts[1], verts[2]));
        Edges[2] = new Edge(this, 2, new LineSegment(verts[2], verts[0]));
        TriColor = new Color(UnityEngine.Random.value,
                            UnityEngine.Random.value,
                            UnityEngine.Random.value, 0.5f);
    }

    public Triangle(Triangle source) :
        this(source.GetVertex(0),
             source.GetVertex(1),
             source.GetVertex(2),
             source.VertexIndex)
    {
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
        Edges[0].EdgeLine.Start = v1;
        Edges[0].EdgeLine.End = v2;
        Edges[1].EdgeLine.Start = v2;
        Edges[1].EdgeLine.End = v3;
        Edges[2].EdgeLine.Start = v3;
        Edges[2].EdgeLine.End = v1;
    }

    public int Intersects(int edgeindex1, Edge edge2, ref Vector3 isect)
    {
        return Edges[edgeindex1].EdgeLine.FindIntersection(edge2.EdgeLine, ref isect);
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

        if (Mathf.Approximately(alpha + beta + gamma, 0))
        {
            return -1;
        }
        if ((Mathf.Abs(alpha) < 2e-7) ||
            (Mathf.Abs(beta) < 2e-7) ||
            (Mathf.Abs(gamma) < 2e-7))
        {
            return 0;
        }
        return ((alpha > 0) && (beta > 0) && (gamma > 0)) ? 1 : -1;
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
