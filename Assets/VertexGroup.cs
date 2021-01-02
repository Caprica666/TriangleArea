using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VertexGroup
{
    public int Method = 0;
    private RBTree<VertexEvent> mEventQ;
    private RBTree<VertexEvent> mIsectQ;
    private RBTree<Edge> mActiveLines;
    private LineMesh mLineMesh = null;
    private TriangleMesh mClipMesh = null;
    private TriangleMesh mTriMesh = null;
    private EventCompare mCompareEvents = new EventCompare();
    private EdgeCompare mCompareEdges = new EdgeCompare();
    public int DebugLevel = 0;

    public VertexGroup(TriangleMesh tmesh, LineMesh lmesh = null, TriangleMesh cmesh = null)
    {
        mLineMesh = lmesh;
        mClipMesh = cmesh;
        mTriMesh = tmesh;
        mEventQ = new RBTree<VertexEvent>(mCompareEvents);
        mIsectQ = new RBTree<VertexEvent>(mCompareEvents);
        mActiveLines = new RBTree<Edge>(mCompareEdges);
    }

    public RBTree<VertexEvent> Events
    {
        get { return mEventQ; }
    }

    public RBTree<Edge> ActiveLines
    {
        get { return mActiveLines; }
    }

    public void Clear()
    {
        mEventQ = new RBTree<VertexEvent>(mCompareEvents);
        mActiveLines = new RBTree<Edge>(mCompareEdges);
        if (mTriMesh != null)
        {
            mTriMesh.Clear();
        }
        if (mLineMesh != null)
        {
            mLineMesh.Clear();
        }
        if (mClipMesh != null)
        {
            mClipMesh.Clear();
        }
    }

    public IEnumerator ShowIntersections()
    {
        LineEnumerator iter = new LineEnumerator(this);

        while (AccumulateIntersections(iter))
        {
            Display();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        Display();
        IEnumerator iter2 = AccumulateTriangles();
        while (iter2.MoveNext())
        {
            Display();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        Display();
    }

    public void Display()
    {
        if (mTriMesh != null)
        {
            mTriMesh.Display();
        }
        if (mLineMesh != null)
        {
            mLineMesh.Display();
        }
        if (mClipMesh != null)
        {
            mClipMesh.Display();
        }
    }

    public void RemoveActive(Edge e, bool updateMesh = false)
    {
        int vindex = e.Line.VertexIndex;

        if (vindex < 0)
        {
            return;
        }
        bool removed = mActiveLines.Remove(e);
        if (!removed)
        {
            Debug.LogWarning("ERROR: Cannot Remove Edge " + e + " X = " + mCompareEdges.CurrentX);
            return;
        }
        if (DebugLevel > 0)
        {
            Debug.Log("Removed Edge " + e + " X = " + mCompareEdges.CurrentX);
        }
        if (updateMesh && (mLineMesh != null))
        {
            mLineMesh.Update(vindex, Color.white);
        }
    }

    public void AddActive(Edge e)
    {
        bool added = mActiveLines.Add(e);
        if (!added)
        {
            Debug.LogError("ERROR: Cannot Add Edge " + e + " X = " + mCompareEdges.CurrentX);
            return;
        }
        if (DebugLevel > 0)
        {
            Debug.Log("Added Edge " + e + " X = " + mCompareEdges.CurrentX);
        }
        if (mLineMesh != null)
        {
            mLineMesh.Add(e.Line);
        }
    }

    public void AddTriangles(List<Triangle> triangles, bool clearindices = false)
    {
        try
        {
            foreach (Triangle t in triangles)
            {
                if (clearindices)
                {
                    t.VertexIndex = -1;
                    t.ID = t.GetNextID();
                    t.Edges[0].Line.VertexIndex = -1;
                    t.Edges[1].Line.VertexIndex = -1;
                    t.Edges[2].Line.VertexIndex = -1;
                    t.Edges[0].Intersections.Clear();
                    t.Edges[1].Intersections.Clear();
                    t.Edges[2].Intersections.Clear();
                }
                if (t.Vertices[2].x <= mCompareEdges.CurrentX)
                {
                    if (mClipMesh != null)
                    {
                        t.VertexIndex = -1;
                        mClipMesh.AddTriangle(t);
                    }
                    continue;
                }
                if (mTriMesh != null)
                {
                    mTriMesh.AddTriangle(t);
                }
                if (DebugLevel > 0)
                {
                    Debug.Log("Added Triangle " + t + " X = " + mCompareEdges.CurrentX);
                }
                for (int i = 0; i < 3; ++i)
                {
                    Vector3 v1 = t.Vertices[i];
                    Vector3 v2 = t.Vertices[(i + 1) % 3];
                    if (v1.x > mCompareEdges.CurrentX)
                    {
                        VertexEvent p1 = new VertexEvent(v1, t, i);

                        p1.TriEdge.AddIntersection(p1);
                        AddEvent(p1);
                    }
                    if (v2.x > mCompareEdges.CurrentX)
                    {
                        VertexEvent p2 = new VertexEvent(v2, t, i);

                        p2.TriEdge.AddIntersection(p2);
                        AddEvent(p2);
                    }
                }
            }
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    public bool RemoveTri(Triangle tri, bool removeactive = false)
    {
        if (tri.VertexIndex < 0)
        {
            return false;
        }
        if (DebugLevel > 0)
        {
            Debug.Log("Removed Triangle " + tri + " X = " + mCompareEdges.CurrentX);
        }
        for (int i = 0; i < 3; ++i)
        {
            VertexEvent e1 = new VertexEvent(tri.Vertices[i], tri, i);
            VertexEvent e2 = new VertexEvent(tri.Vertices[(i + 1) % 3], tri, i);

            RemoveEvent(e1);
            RemoveEvent(e2);
        }
        if (removeactive)
        {
            RemoveActive(tri.Edges[0], true);
            RemoveActive(tri.Edges[1], true);
            RemoveActive(tri.Edges[2], true);
        }
        if ((mTriMesh != null) &&
            (mTriMesh.RemoveTriangle(tri) < 0))
        {
            return false;
        }
        return true;
    }

    public void RemoveEvent(VertexEvent e)
    {
        bool removed = mEventQ.Remove(e);
        if (!removed)
        {
            Debug.LogWarning("ERROR: Could not remove event " + e);
            return;
        }
        if (DebugLevel > 1)
        {
            Debug.Log("   Removed event " + e);
        }
    }

    public void AddEvent(VertexEvent e)
    {
        bool added = mEventQ.Add(e);
        if (DebugLevel > 1)
        {
            if (added)
            {
                Debug.Log("   Added event " + e);
            }
            else
            {
                Debug.LogWarning("ERROR: Could not add event " + e);
            }
        }
    }

    public bool CheckStart(Edge edgeA, Edge edgeB)
    {
        int n = edgeB.Intersections.Count - 1;
        VertexEvent ve;

        if ((edgeA.Tri.Contains(edgeB.Line.Start) <= 0) ||
            (n < 0))
        {
            return false;
        }
        int i = edgeB.FindIntersectionIndex(edgeB.Line.Start.x);
        if (i >= 0)
        {
            ve = edgeB.Intersections[0];
            edgeB.Intersections.RemoveAt(0);
            mIsectQ.Remove(ve);
            mEventQ.Remove(ve);
        }
        i = (edgeB.EdgeIndex + 2) % 3;

        Edge adjacent = edgeB.Tri.Edges[i];

        i = adjacent.FindIntersectionIndex(edgeB.Line.Start.x);
        if (i >= 0)
        {
            ve = adjacent.Intersections[i];
            adjacent.Intersections.RemoveAt(i);
            mIsectQ.Remove(ve);
        }
        return true;
    }

    public bool CheckEnd(Edge edgeA, Edge edgeB)
    {
        int n = edgeB.Intersections.Count - 1;
        VertexEvent ve;

        if ((edgeA.Tri.Contains(edgeB.Line.End) <= 0) ||
            (n < 0))
        {
            return false;
        }
        int i = edgeB.FindIntersectionIndex(edgeB.Line.End.x);
        if (i >= 0)
        {
            ve = edgeB.Intersections[i];
            mIsectQ.Remove(ve);
            edgeB.Intersections.RemoveAt(n);
        }
        i = (edgeB.EdgeIndex + 2) % 3;
        Edge adjacent = edgeB.Tri.Edges[i];

        i = adjacent.FindIntersectionIndex(edgeB.Line.End.x);
        if (i >= 0)
        {
            ve = adjacent.Intersections[i];
            adjacent.Intersections.RemoveAt(i);
            mEventQ.Remove(ve);
            mIsectQ.Remove(ve);
        }
        return true;
    }

    public ClipResult CheckForIntersection(Edge edgeA, Edge edgeB)
    {
        Vector3 isect = new Vector3();
        int r = edgeA.FindIntersection(edgeB, ref isect);

        if (edgeA.Tri.Contains(edgeB.Tri))
        {
            RemoveTri(edgeB.Tri);
            return ClipResult.BINSIDEA;
        }
        else if (edgeB.Tri.Contains(edgeA.Tri))
        {
            RemoveTri(edgeA.Tri);
            return ClipResult.AINSIDEB;
        }
        if ((r > 0) && (isect.x > mCompareEdges.CurrentX))
        {
            if (!CheckStart(edgeA, edgeB))
            {
                CheckEnd(edgeA, edgeB);
            }
            if (!CheckStart(edgeB, edgeA))
            {
                CheckEnd(edgeB, edgeA);
            }
            VertexEvent ve1 = new VertexEvent(isect, edgeA, edgeB);
            VertexEvent ve2 = new VertexEvent(isect, edgeB, edgeA);
            bool addedA = edgeA.AddIntersection(ve1);
            bool addedB = edgeB.AddIntersection(ve2);

            if (addedA)
            {
                AddEvent(ve1);
            }
            if (addedB)
            {
                AddEvent(ve2);
            }
            return ClipResult.CLIPPED;
        }
        return ClipResult.COINCIDENT;
    }

    public VertexEvent HandleIntersection(Vector3 vc, VertexEvent eventA, VertexEvent eventB)
    {
        Edge edgeA = eventA.TriEdge;
        Edge edgeB = eventB.TriEdge;
        Vector3 va = eventA.Point;
        Vector3 vb = new Vector3();
        VertexEvent nextB = (edgeB.Line.End.x > vc.x) ?
                             edgeB.FindNextIntersection(vc.x, ref vb) :
                             edgeB.FindPrevIntersection(vc.x, ref vb);
        VertexEvent nextA = (edgeA.Line.End.x > vc.x) ?
                             edgeA.FindNextIntersection(vc.x, ref va) :
                             edgeA.FindPrevIntersection(vc.x, ref va);
        VertexEvent eventC;
        Edge edgeC;

        if (nextB == null)
        {
            return eventA;
        }
        if (eventA.Line.SameDirection(eventB.Line))
        {
            return eventB;
        }
        if (nextA == null)
        {
            return null;
        }
        edgeA.RemoveIntersectionWith(edgeB);
        edgeB.RemoveIntersectionWith(edgeA);
        nextB.TriEdge.RemoveIntersectionWith(edgeB);

        Vector3 mid = (va + vb) / 2;
        bool display = (edgeA.Tri == edgeB.Tri) ||
                        (edgeA.Tri.Contains(mid) >= 0) ||
                        (edgeB.Tri.Contains(mid) >= 0);
        EmitTriangle(va, vb, vc, display);
        if (va.x < vb.x)
        {
            edgeC = new Edge(edgeA.Tri, va, vb);
            eventC = new VertexEvent(va, edgeC, edgeA);
            edgeC.AddIntersection(new VertexEvent(vb, edgeC, edgeB));
            edgeC.AddIntersection(eventC);
        }
        else
        {
            edgeC = new Edge(edgeB.Tri, vb, va);
            eventC = new VertexEvent(vb, edgeC, edgeA);
            edgeC.AddIntersection(new VertexEvent(va, edgeC, edgeB));
            edgeC.AddIntersection(eventC);
        }
        if ((nextB.IntersectingEdge == null) ||
             !edgeC.SameDirection(nextB.IntersectingEdge))
        {
            mIsectQ.Add(eventC);
        }
        return nextB;
    }

    public void EmitTriangle(Vector3 va, Vector3 vb, Vector3 vc, bool display)
    {
        Triangle t = new Triangle(va, vb, vc);

        if (display && (mClipMesh != null))
        {
            mClipMesh.AddTriangle(t);
        }
        if (mTriMesh != null)
        {
            t = new Triangle(va, vb, vc);
            t.TriColor = new Color(1, 1, 1, 1);
            mTriMesh.AddTriangle(t);
        }
    }

    public List<VertexEvent> CollectEdges(ref Vector3 point)
    {
        List<VertexEvent> collected = new List<VertexEvent>();
        VertexEvent nextEvent = mEventQ.Min;
        VecCompare vcompare = new VecCompare();

        if (nextEvent == null)
        {
            return null;
        }
        point = nextEvent.Point;
        foreach (VertexEvent e in mEventQ)
        {
            int order = vcompare.Compare(e.Point, point);
            if (order != 0)
            {
                break;
            }           
            collected.Add(e);
        }
        if (DebugLevel > 0)
        {
            Debug.Log("Process events at " + point + " " + collected.Count + " edges");
        }
        return collected;
    }

   
    public IEnumerator AccumulateTriangles()
    {
        Vector3 curpoint = new Vector3();
        VertexEvent prevEvent = null;
        IComparer<Vector3> veccompare = new VecCompare();
        List<VertexEvent> collected = new List<VertexEvent>();
        while (mIsectQ.Count > 0)
        {
            VertexEvent ve;

            collected.Clear();
            while (((ve = mIsectQ.Min) != null) &&
                   (veccompare.Compare(ve.Point, curpoint) == 0))
            {
                collected.Add(ve);
                mIsectQ.Remove(ve);
            }
            if (ve != null)
            {
                curpoint = ve.Point;
            }
            prevEvent = null;
            foreach (VertexEvent ve2 in collected)
            {
                if (prevEvent != null)
                {
                    prevEvent = HandleIntersection(ve2.Point, prevEvent, ve2);
                    yield return new WaitForEndOfFrame();
                }
                else
                {
                    prevEvent = ve2;
                }
            }
        }
        yield return null;
    }

    public bool AccumulateIntersections(LineEnumerator lineiter)
    {
        try
        {
            Vector3 point = new Vector3();
            List<VertexEvent> collected = CollectEdges(ref point);
            List<Edge> addus = new List<Edge>();
            float prevX = mCompareEdges.CurrentX;
            float curX = point.x;
            Edge bottomNeighbor;
            Edge topNeighbor;
            Edge edge;
            IComparer<VertexEvent> eventcompare = new EventCompare();

            if (collected == null)
            {
                return false;
            }
            edge = collected[0].TriEdge;
            for (int i = 0; i < collected.Count; ++i)
            {
                VertexEvent e1 = collected[i];

                RemoveEvent(e1);
                if (e1.Line.Start == point)
                {
                    addus.Add(e1.TriEdge);
                    mIsectQ.Add(e1);
                }
                else if (e1.Line.End == point)
                {
                    RemoveActive(e1.TriEdge, true);
                    collected.RemoveAt(i--);
                }
                else
                {
                    RemoveActive(e1.TriEdge, false);
                    addus.Add(e1.TriEdge);
                    mIsectQ.Add(e1);
                }
            }
            /*
             * All collected edges end at the event point.
             * Compare top and bottom neighboring edges
             * for possible intersection.
             */
            mCompareEdges.CurrentX = curX;
            if (collected.Count == 0)
            {
                bottomNeighbor = lineiter.FindBottomNeighbor(edge);
                topNeighbor = lineiter.FindTopNeighbor(edge);
                if ((bottomNeighbor != null) &&
                    (topNeighbor != null) &&
                    (bottomNeighbor.Tri != topNeighbor.Tri))
                {
                    CheckForIntersection(topNeighbor, bottomNeighbor);
                }
                return true;
            }
            /*
             * Reorder the active lines with respect to the
             * new X value where the collected lines meet.
             * Take the collected edges out, change the
             * X intercept and add them back.
             * If we removed the last edge of the triangle,
             * add it back.
             */
            VertexEvent bottom = collected[0];
            VertexEvent top = collected[collected.Count - 1];

            foreach (Edge e in addus)
            {
                AddActive(e);
            }
            bottomNeighbor = lineiter.FindBottomNeighbor(bottom.TriEdge);
            topNeighbor = lineiter.FindTopNeighbor(top.TriEdge);

            /*
             * Check for intersection with the bottom neighbor
             */
            if (bottomNeighbor != null)
            {
                CheckForIntersection(bottom.TriEdge, bottomNeighbor);
            }
            /*
              * Check for intersection with the top neighbor
              */
            if (topNeighbor != null)
            {
                CheckForIntersection(top.TriEdge, topNeighbor);
            }
            return true;
        }
        catch (ArgumentException ex)
        {
            return false;
        }
    }
}
