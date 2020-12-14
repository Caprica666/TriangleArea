using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class VertexGroup
{
    public int Method = 0;
    private RBTree<VertexEvent> mEventQ;
    private RBTree<Edge> mActiveLines;
    private List<Vector3> mIntersections;
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
        mIntersections = new List<Vector3>();
        LineEnumerator iter = new LineEnumerator(this);

        while (Process(iter))
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

    public void FindIntersections(List<Vector3> intersections)
    {
        mIntersections = intersections;
        LineEnumerator iter = new LineEnumerator(this);

        while (Process(iter))
            ;
    }
    public void RemoveActive(Edge e, bool updateMesh = false)
    {
        int vindex = e.Line.VertexIndex;

        if (vindex < 0)
        {
            return;
        }
        bool removed = mActiveLines.Remove(e);
        if (DebugLevel > 0)
        {
            if (!removed)
            {
                Debug.LogError("ERROR: Cannot Remove Edge " + e + " X = " + mCompareEdges.CurrentX);
                return;
            }
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
        if (DebugLevel > 0)
        {
            if (added)
            {
                Debug.Log("Added Edge " + e + " X = " + mCompareEdges.CurrentX);
            }
            else
            {
                Debug.LogError("ERROR: Cannot Add Edge " + e + " X = " + mCompareEdges.CurrentX);
                return;
            }
        }
        if (mLineMesh != null)
        {
            mLineMesh.Add(e.Line);
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
            if (removeactive)
            {
                Edge edge = tri.Edges[i];
                if ((edge.Line.Start.x >= mCompareEdges.CurrentX) ||
                    (edge.Line.End.x >= mCompareEdges.CurrentX))
                {
                    RemoveActive(edge, true);
                }
            }
        }
        if ((mTriMesh != null) &&
            (mTriMesh.RemoveTriangle(tri) < 0))
        {
            return false;
        }
        return true;
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
                    t.Edges[0].Line.VertexIndex = -1;
                    t.Edges[1].Line.VertexIndex = -1;
                    t.Edges[2].Line.VertexIndex = -1;
                }
                if (t.Vertices[2].x <= mCompareEdges.CurrentX)
                {
                    if (mClipMesh != null)
                    {
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
                    if (v1.x >= mCompareEdges.CurrentX)
                    {
                        VertexEvent p1 = new VertexEvent(v1, t, i);

                        AddEvent(p1);
                    }
                    if (v2.x >= mCompareEdges.CurrentX)
                    {
                        VertexEvent p2 = new VertexEvent(v2, t, i);

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

    public void RemoveEvent(VertexEvent e)
    {
        bool removed = mEventQ.Remove(e);
        if (DebugLevel > 1)
        {
            if (removed)
            {
                Debug.Log("   Removed event " + e);
            }
            else
            {
                Debug.LogWarning("ERROR: Could not remove event " + e);
            }
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

    public ClipResult CheckForIntersection(Edge edgeA, Edge edgeB)
    {
        TriClip clipper = new TriClip();
        List<Triangle> trisClipped = new List<Triangle>();
        ClipResult r = clipper.ClipTri(edgeA, edgeB, trisClipped);

        switch (r)
        {
            case ClipResult.ACLIPSB:    // triB  was clipped
            if (DebugLevel > 0)
            {
                Debug.Log("Edge " + edgeA + " clips Triangle " + edgeB.Tri);
            }
            trisClipped[0].VertexIndex = edgeB.Tri.VertexIndex;
            RemoveTri(edgeB.Tri, true);
            AddTriangles(trisClipped);
            break;

            case ClipResult.BCLIPSA:    // tri A was clipped
            if (DebugLevel > 0)
            {
                Debug.Log("Edge " + edgeB + " clips Triangle " + edgeA.Tri);
            }
            trisClipped[0].VertexIndex = edgeA.Tri.VertexIndex;
            RemoveTri(edgeA.Tri, true);
            AddTriangles(trisClipped);
            break;

            case ClipResult.BINSIDEA:     // tri B inside A
            if (DebugLevel > 0)
            {
                Debug.Log("Triangle " + edgeB.Tri + " inside Triangle " + edgeA.Tri);
            }
            RemoveTri(edgeB.Tri, true);
            break;

            case ClipResult.AINSIDEB:     // tri A inside B
            if (DebugLevel > 0)
            {
                Debug.Log("Triangle " + edgeA.Tri + " inside Triangle " + edgeB.Tri);
            }
            RemoveTri(edgeA.Tri, true);
            break;
        }
        return r;
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

    public bool Process(LineEnumerator lineiter)
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
            ClipResult r;
            Triangle t1;

            if (collected == null)
            {
                return false;
            }
            edge = collected[0].TriEdge;
            for (int i = 0; i < collected.Count; ++i)
            {
                VertexEvent e1 = collected[i];
                t1 = e1.TriEdge.Tri;

                RemoveEvent(e1);
                /*
                 * The line segment ends at this point.
                 * Remove it from the active list,
                 * Dont consider it further.
                 */
                if (e1.Line.End == point)
                {
                    RemoveActive(e1.TriEdge, true);
                    collected.RemoveAt(i--);
                    /*
                     * If this is the last triangle edge,
                     * remove the triangle
                     */
                    if ((point == t1.GetVertex(2)) &&
                        (e1.TriEdge.EdgeIndex == 2))
                    {
                        if (RemoveTri(t1))
                        {
                            mClipMesh.AddTriangle(new Triangle(t1));
                        }
                    }
                }
                else
                {
                    RemoveActive(e1.TriEdge, false);
                    addus.Add(e1.TriEdge);
                    /*
                     * If consecutive edges are from different triangles,
                     * check if the first edge clips or contains the second triangle.
                     */
                    if (i < collected.Count - 1)
                    {
                        VertexEvent e2 = collected[i + 1];

                        if (t1 != e2.TriEdge.Tri)
                        {
                            r = CheckForIntersection(e1.TriEdge, e2.TriEdge);
                            if (r > 0)
                            {
                                return true;
                            }
                            if (e2.TriEdge.Tri.Contains(t1))
                            {
                                RemoveTri(t1, true);
                                return true;
                            }
                        }
                    }
                }
            }
            /*
             * All collected edges end at the event point.
             * Compare top and bottom neighboring edges
             * for possible intersection.
             */
            if (collected.Count == 0)
            {
                mCompareEdges.CurrentX = curX;
                bottomNeighbor = lineiter.FindBottomNeighbor(edge);
                topNeighbor = lineiter.FindTopNeighbor(edge);
                if ((bottomNeighbor != null) &&
                    (topNeighbor != null) &&
                    (bottomNeighbor.Tri != topNeighbor.Tri))
                {
                    CheckForIntersection(bottomNeighbor, topNeighbor);
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

            mCompareEdges.CurrentX = curX;
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
                r = CheckForIntersection(bottomNeighbor, bottom.TriEdge);
                if (r > 0)
                {
                    return true;
                }
            }
            RemoveEvent(top);
            /*
              * Check for intersection with the top neighbor
              */
            if (topNeighbor != null)
            {
                r = CheckForIntersection(topNeighbor, top.TriEdge);
                if (r > 0)
                {
                    return true;
                }
            }
            return true;
        }
        catch (ArgumentException ex)
        {
            return false;
        }
    }
}
