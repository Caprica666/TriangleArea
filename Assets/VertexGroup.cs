using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class VertexGroup
{
    private RBTree<VertexEvent> mEventQ;
    private RBTree<Edge> mActiveLines;
    private List<Vector3> mIntersections;
    private LineMesh mLineMesh = null;
    private TriangleMesh mClipMesh = null;
    private TriangleMesh mTriMesh = null;
    private EventCompare mCompareEvents = new EventCompare();
    private EdgeCompare mCompareEdges = new EdgeCompare();

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

    public void AddTriangles(List<Triangle> triangles, bool addActive = false)
    {
        try
        {
            foreach (Triangle t in triangles)
            {
                Color c = new Color(Random.value, Random.value, Random.value, 1);
                if (!addActive)
                {
                    t.VertexIndex = -1;
                    t.Edges[0].Line.VertexIndex = -1;
                    t.Edges[1].Line.VertexIndex = -1;
                    t.Edges[2].Line.VertexIndex = -1;
                }
                if (mTriMesh != null)
                {
                    mTriMesh.AddTriangle(t);
                }
                for (int i = 0; i < 3; ++i)
                {
                    AddEdge(t, i, addActive);
                }
            }
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    private void AddEdge(Triangle t, int i, bool addActive = false)
    {
        Color c = new Color(Random.value, Random.value, Random.value, 1);
        VertexEvent p1 = new VertexEvent(t.Vertices[i], t, i);
        VertexEvent p2 = new VertexEvent(t.Vertices[(i + 1) % 3], t, i);

        mEventQ.Add(p1);
        mEventQ.Add(p2);
        if (addActive)
        {
            mActiveLines.Add(t.Edges[i]);
        }
        if (mLineMesh != null)
        {           
            p1.Line.VertexIndex = mLineMesh.Add(p1.Line, c);
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
        if (updateMesh)
        {
            mActiveLines.Remove(e);
            if (mLineMesh != null)
            {
                int vindex = e.Line.VertexIndex;
                mLineMesh.Update(vindex, Color.black);
            }
        }
        else
        {
            mActiveLines.Remove(e);
        }
    }

    public void RemoveTri(Triangle tri, bool updateMesh = false)
    {
        for (int i = 0; i < 3; ++i)
        {
            VertexEvent e1 = new VertexEvent(tri.Vertices[i], tri, i);
            VertexEvent e2 = new VertexEvent(tri.Vertices[(i + 1) % 3], tri, i);

            if (!mEventQ.Remove(e1))
            {
//                throw new ArgumentException("Could not remove " + e1);
            }
            if (!mEventQ.Remove(e2))
            {
//                throw new ArgumentException("Could not remove " + e2);
            }
            RemoveActive(tri.Edges[i]);
        }
        if (updateMesh && (mTriMesh != null))
        {
            mTriMesh.RemoveTriangle(tri);
        }
    }

    public void RemoveActiveEdges(Triangle tri)
    {
        for (int i = 0; i < 3; ++i)
        {
            RemoveActive(tri.Edges[i]);
        }
    }
    public void AddActiveEdges(Triangle tri)
    {
        for (int i = 0; i < 3; ++i)
        {
            AddActive(tri.Edges[i]);
        }
    }

    public void AddActive(Edge e, Color c)
    {
        mActiveLines.Add(e);
        if (mLineMesh != null)
        {
            int vindex = e.Line.VertexIndex;
            mLineMesh.Update(vindex, c);
        }
    }

    public void AddActive(Edge e)
    {
        mActiveLines.Add(e);
        if (mLineMesh != null)
        {
            mLineMesh.Add(e.Line);
        }
    }

    public bool CheckForIntersection(Edge clipperEdge, Edge clippedEdge)
    {
        TriClip clipper = new TriClip(clipperEdge, clippedEdge);
        List<Triangle> trisClipped = new List<Triangle>();
        ClipResult r = clipper.Clip(trisClipped);
        switch (r)
        {
            case ClipResult.CLIPPED:    // tri was clipped
            trisClipped[0].VertexIndex = clippedEdge.Tri.VertexIndex;
            RemoveTri(clippedEdge.Tri, true);
            AddTriangles(trisClipped, true);
            return true;

            case ClipResult.INSIDE:     // clipped inside clipper
            RemoveTri(clippedEdge.Tri, true);
            return true;
        }
        return false;
    }

    public bool CheckForDelete(List<VertexEvent> collected, Vector3 p)
    {
        bool deleted = false;
        foreach (VertexEvent e in collected)
        {
            deleted |= CheckForDelete(e, p);
        }
        return deleted;
    }

    public bool CheckForDelete(VertexEvent e, Vector3 p)
    {
        if (p == e.TriEdge.Tri.GetVertex(2))
        {
            Triangle t = e.TriEdge.Tri;
            RemoveTri(t, true);
            mClipMesh.AddTriangle(new Triangle(t));
            return true;
        }
        if (e.Line.End == p)
        {
            RemoveActive(e.TriEdge);
        }
        mEventQ.Remove(e);
        return false;
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
        return collected;
    }

    public bool Process(LineEnumerator lineiter)
    {
        try
        {
            Vector3 point = new Vector3();
            List<VertexEvent> collected = CollectEdges(ref point);

            if (collected == null)
            {
                return false;
            }
            float curX = point.x;
            Triangle t = null;

            foreach (VertexEvent e in collected)
            {
                if (t != e.TriEdge.Tri)
                {
                    t = e.TriEdge.Tri;
                    RemoveActiveEdges(t);
                }
            }
            mCompareEdges.CurrentX = curX;
            t = null;
            foreach (VertexEvent e in collected)
            {
                if (t != e.TriEdge.Tri)
                {
                    t = e.TriEdge.Tri;
                    AddActiveEdges(t);
                }
            }
            VertexEvent bottom = collected[0];
            VertexEvent top = collected[collected.Count - 1];
            Edge bottomNeighbor = lineiter.FindBottomNeighbor(bottom.TriEdge);
            Edge topNeighbor = lineiter.FindTopNeighbor(top.TriEdge);

            if (bottomNeighbor != null)
            {
                if (CheckForIntersection(bottomNeighbor, bottom.TriEdge))
                {
                    return true;
                }
            }
            if (topNeighbor != null)
            {
                if (CheckForIntersection(topNeighbor, top.TriEdge))
                {
                    return true;
                }
            }
            for (int i = 0; i < (collected.Count - 1); ++i)
            {
                VertexEvent e1 = collected[i];
                VertexEvent e2 = collected[i + 1];

                if (e1.TriEdge.Tri != e2.TriEdge.Tri)
                {
                    if (CheckForIntersection(e2.TriEdge, e1.TriEdge))
                    {
                        return true;
                    }
                }
            }
            CheckForDelete(collected, point);
            return true;
        }
        catch (ArgumentException ex)
        {
            return false;
        }
    }
}
