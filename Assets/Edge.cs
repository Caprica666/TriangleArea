using System;
using System.Collections.Generic;
using UnityEngine;

public class Edge
{
    static public int EdgeID = 0;
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
        mEdgeIndex = --EdgeID;
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
        VecCompare veccompare = new VecCompare();

        for (int i = 0; i < mIntersections.Count; ++i)
        {
            VertexEvent ie = mIntersections[i];
            if (evcompare.Compare(ie, ve) == 0)
            {
                return false;
            }
            if (veccompare.Compare(ie.Point, ve.Point) > 0)
            {
                mIntersections.Insert(i, ve);
                return true;
            }
        }
        mIntersections.Add(ve);
        return true;
    }

    public VertexEvent FindPrevIntersection(Vector3 p, ref Vector3 isect)
    {
        VertexEvent prev = null;
        VecCompare vcompare = new VecCompare();

        isect = Line.Start;
        foreach (VertexEvent e in mIntersections)
        {
            if (vcompare.Compare(e.Point, p) >= 0)
            {
                break;
            }
            isect = e.Point;
            prev = e;
        }
        return prev; 
    }
    public VertexEvent FindNextIntersection(Vector3 p, ref Vector3 isect)
    {
        VecCompare vcompare = new VecCompare();

        foreach (VertexEvent e in mIntersections)
        {
            if (vcompare.Compare(e.Point, p) > 0)
            {
                isect = e.Point;
                return e;
            }
        }
        return null;
    }

    public int FindIntersectionIndex(Vector3 p)
    {
        VecCompare vcompare = new VecCompare();

        for (int i = 0; i < mIntersections.Count; ++i)
        {
            VertexEvent e = mIntersections[i];
            if (vcompare.Compare(e.Point, p) == 0)
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
                             Tri.ID, EdgeIndex) +
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
        if (e1 == e2)
        {
            return 0;
        }

        LineSegment s1 = e1.Line;
        LineSegment s2 = e2.Line;
        Vector3 v1 = s1.End - s1.Start;
        Vector3 v2 = s2.End - s2.Start;
        float y1 = s1.CalcY(CurrentX);
        float y2 = s2.CalcY(CurrentX);
        Vector3 sweep = new Vector3(0, -1, 0);
        float dx;

        v1.Normalize();
        v2.Normalize();
        //
        // Check for vertical lines
        //
        if (y1 == float.MaxValue)
        {
            if (y2 == float.MaxValue)
            {
                dx = s1.Start.x - s2.Start.x;
                // both vertical, sort on X
                if (Math.Abs(dx) > LineSegment.EPSILON)
                {
                    return (dx > 0) ? 1 : -1;
                }
                return e1.Tri.ID - e2.Tri.ID;
            }
            //
            // first edge is vertical, not the second
            // if no overlap, sort on starting x
            //
            dx = s1.Start.x - CurrentX;
            if (Math.Abs(dx) < LineSegment.EPSILON)
            {
                y1 = y2;
                v1 = sweep;
            }
            else return (dx < 0) ? 1 : -1;
        }
        //
        // second edge is vertical, not the first
        // if no overlap, sort on starting x
        //
        else if (y2 == float.MaxValue)
        {
            dx = s2.Start.x - CurrentX;
            if (Math.Abs(dx) < LineSegment.EPSILON)
            {
                y2 = y1;
                v2 = sweep;
            }
            else return (dx < 0) ? 1 : -1;
        }
        //
        // Compare Y values at the current X
        //
        float dy = y1 - y2;

        if (Math.Abs(dy) > LineSegment.EPSILON)
        {
            return (dy > 0) ? 1 : -1;
        }
        //
        // sort based on angle around common point
        //
        float a1 = Vector3.Dot(sweep, v1);
        float a2 = Vector3.Dot(sweep, v2);
        float t = a2 - a1;
        if (Math.Abs(t) > LineSegment.EPSILON)
        {
            return (t > 0) ? 1 : -1;
        }
        return e1.Tri.ID - e2.Tri.ID;
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