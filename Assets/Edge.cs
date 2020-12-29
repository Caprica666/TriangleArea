using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Edge
{
    private Triangle mTriangle;
    private LineSegment mLine;
    private int mEdgeIndex;
    private List<VertexEvent> mIntersections = new List<VertexEvent>();

    public LineSegment Line
    {
        get { return mLine; }
    }

    public Triangle Tri
    {
        get { return mTriangle; }
    }

    public List<VertexEvent> Intersections
    {
        get { return mIntersections; }
        set { mIntersections = value; }
    }

    public int EdgeIndex
    {
        get { return mEdgeIndex; }
        set { mEdgeIndex = value; }
    }

    public Edge(Triangle tri, int edgeIndex)
    {
        mTriangle = tri;
        mEdgeIndex = edgeIndex;
        mLine = new LineSegment(tri.GetVertex(edgeIndex),
                                tri.GetVertex((edgeIndex + 1) % 3));
    }

    public Edge(Triangle tri, Vector3 v1, Vector3 v2)
    {
        mTriangle = tri;
        mEdgeIndex = -1;
        mLine = new LineSegment(v1, v2);
    }

    public Edge(Edge src, int edgeIndex)
    {
        mTriangle = src.mTriangle;
        mEdgeIndex = edgeIndex;
        mLine = src.Line;
    }

    public bool SameDirection(Edge e2)
    {
        return Line.SameDirection(e2.Line);
    }

    public int FindIntersection(Edge e2, ref Vector3 intersection)
    {
        LineSegment line2 = e2.Line;
        if (Line.Start.x == Line.End.x)
        {
            if (line2.Start.x == line2.End.x)
            {
                return -1;
            }
            intersection.x = Line.Start.x;
            intersection.y = line2.CalcY(intersection.x);
            if ((intersection.y < Line.Start.y) || (intersection.y > Line.End.y))
            {
                return -1;
            }
            if ((intersection.y == Line.Start.y) || (intersection.y == Line.End.y))
            {
                return 0;
            }
            return 1;
        }
        return Line.FindIntersection(line2, ref intersection);
    }

    public bool AddIntersection(VertexEvent ve)
    {
        IComparer<VertexEvent> evcompare = new EventCompare();

        for (int i = 0; i < mIntersections.Count; ++i)
        {
            VertexEvent ie = mIntersections[i];
            if (evcompare.Compare(ie, ve) == 0)
            {
                return false;
            }
            if (ie.Point.x > ve.Point.x)
            {
                mIntersections.Insert(i, ve);
                return true;
            }
        }
        mIntersections.Add(ve);
        return true;
    }

    public VertexEvent FindPrevIntersection(float x, ref Vector3 isect)
    {
        VertexEvent prev = null;
        isect = Line.Start;
        foreach (VertexEvent e in mIntersections)
        {
            float t = e.Point.x - x;

            if (t >= 0)
            {
                break;
            }
            isect = e.Point;
            prev = e;
        }
        return prev; 
    }
    public VertexEvent FindNextIntersection(float x, ref Vector3 isect)
    {
        foreach (VertexEvent e in mIntersections)
        {
            float t = e.Point.x - x;

            if (t > 0)
            {
                isect = e.Point;
                return e;
            }
        }
        return null;
    }

    public int FindIntersectionIndex(float x)
    {
        for (int i = 0; i < mIntersections.Count; ++i)
        {
            VertexEvent e = mIntersections[i];
            if (Math.Abs(e.Point.x - x) < LineSegment.EPSILON)
            {
                return i;
            }
        }
        return -1;
    }

    public void RemoveIntersectionWith(Edge edge)
    {
        for (int i = 0; i < mIntersections.Count; ++i)
        {
            VertexEvent e = mIntersections[i];
            if (e.IntersectingEdge == edge)
            {
                mIntersections.RemoveAt(i);
                return;
            }
        }
    }

    public override string ToString()
    {
        return String.Format("T: {0:0} E: {1:0} ",
                             (Tri.VertexIndex / 3), EdgeIndex) +
               Line.ToString();
    }
}

public class EdgeCompare : Comparer<Edge>
{
    public float CurrentX = -100000;

    public EdgeCompare() { }

    public EdgeCompare(float X) { CurrentX = X; }

    public override int Compare(Edge e1, Edge e2)
    {
        LineSegment s1 = e1.Line;
        LineSegment s2 = e2.Line;
        float y1 = s1.CalcY(CurrentX);
        float y2 = s2.CalcY(CurrentX);
        float t = y1 - y2;

        if (e1 == e2)
        {
            return 0;
        }
        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }

        Vector3 v1 = s1.End - s1.Start;
        Vector3 v2 = s2.End - s2.Start;
        Vector3 sweep = new Vector3(0, -1, 0);
        float a1, a2;

        v1.Normalize();
        v2.Normalize();
        a1 = Vector3.Dot(sweep, v1);
        a2 = Vector3.Dot(sweep, v2);
        t = a2 - a1;
        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }
        return e1.Tri.GetHashCode() - e2.Tri.GetHashCode();
    }
}

public class VecCompare : Comparer<Vector3>
{
    public override int Compare(Vector3 v1, Vector3 v2)
    {
        float epsilon = 1e-6f;
        float t = v1.x - v2.x;

        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        t = v1.y - v2.y;
        if (Math.Abs(t) > epsilon)
        {
            return (t > 0) ? 1 : -1;
        }
        return 0;
    }
}