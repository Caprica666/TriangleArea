using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class VertexEvent
{
    private Vector3 mPoint;
    private Edge mEdge;

    public Vector3 Start
    {
        get { return mEdge.Line.Start; }
    }

    public Vector3 End
    {
        get { return mEdge.Line.End; }
    }

    public LineSegment Line
    {
        get { return mEdge.Line; }
    }

    public Edge TriEdge
    {
        get { return mEdge; }
    }

    public Vector3 Point
    {
        get { return mPoint; }
        set { mPoint = value; }
    }

    public int FindIntersection(Edge e2, ref Vector3 intersection)
    {
        return mEdge.FindIntersection(e2, ref intersection);
    }

    public override string ToString()
    {
        if (mPoint == mEdge.Line.Start)
        {
            return mPoint + " " + mEdge.ToString();
        }
        else
        {
            return mPoint + " T: " + (mEdge.Tri.VertexIndex / 3) +
                   " E: " + mEdge.EdgeIndex + " " +
                   End + " <- " + Start;
        }
    }

    public VertexEvent(Vector3 point, Triangle tri, int vertexIndex)
    {
        mPoint = point;
        mEdge = tri.Edges[vertexIndex];
    }

    public VertexEvent(Vector3 point, Edge edge)
    {
        mPoint = point;
        mEdge = edge;
    }
}

public class EventCompare : IComparer<VertexEvent>
{
    int IComparer<VertexEvent>.Compare(VertexEvent p1, VertexEvent p2)
    {
        VecCompare vcompare = new VecCompare();

        int order = vcompare.Compare(p1.Point, p2.Point);
        if (order != 0)
        {
            return order;
        }
        EdgeCompare lcompare = new EdgeCompare(p1.Point.x + 4 * LineSegment.EPSILON);
        order = lcompare.Compare(p1.TriEdge, p2.TriEdge);
        return order;
     }
}
