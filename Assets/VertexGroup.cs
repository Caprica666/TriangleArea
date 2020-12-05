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
        mActiveLines.Remove(e);
        if (updateMesh && (mLineMesh != null))
        {
            int vindex = e.Line.VertexIndex;
            mLineMesh.Update(vindex, Color.white);
        }
    }

    public bool RemoveTri(Triangle tri, bool updateMesh = false)
    {
        if (tri.VertexIndex < 0)
        {
            return false;
        }
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
            RemoveActive(tri.Edges[i], true);
        }
        if (updateMesh && (mTriMesh != null))
        {
            if (mTriMesh.RemoveTriangle(tri) < 0)
            {
                return false;
            }
        }
        return true;
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

    public ClipResult CheckForIntersection(Edge edgeA, Edge edgeB, bool clipone = false)
    {
        TriClip clipper = new TriClip(edgeA, edgeB);
        List<Triangle> trisClipped = new List<Triangle>();
        ClipResult r = clipper.ClipTri(trisClipped);
        switch (r)
        {
            case ClipResult.ACLIPSB:    // triB  was clipped
            trisClipped[0].VertexIndex = edgeB.Tri.VertexIndex;
            RemoveTri(edgeB.Tri, true);
            AddTriangles(trisClipped, true);
            break;

            case ClipResult.BCLIPSA:    // tri A was clipped
            trisClipped[0].VertexIndex = edgeA.Tri.VertexIndex;
            RemoveTri(edgeA.Tri, true);
            AddTriangles(trisClipped, true);
            break;

            case ClipResult.BINSIDEA:     // tri B inside A
            RemoveTri(edgeB.Tri, true);
            break;

            case ClipResult.AINSIDEB:     // tri A inside B
            RemoveTri(edgeB.Tri, true);
            break;
        }
        return r;
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
            if (RemoveTri(t, true))
            {
                mClipMesh.AddTriangle(new Triangle(t));
            }
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
            float curX = point.x;
            Edge bottomNeighbor;
            Edge topNeighbor;
            Edge edge;
            ClipResult r;

            if (collected == null)
            {
                return false;
            }
            edge = collected[0].TriEdge;
            foreach (VertexEvent e in collected)
            {
                RemoveActive(e.TriEdge);
            }
            for (int i = 0; i < collected.Count; ++i)
            {
                VertexEvent e = collected[i];

                if (CheckForDelete(e, point))
                {
                    collected.RemoveAt(i);
                    --i;
                }
            }
            if (collected.Count == 0)
            {
                bottomNeighbor = lineiter.FindBottomNeighbor(edge);
                topNeighbor = lineiter.FindTopNeighbor(edge);

                if ((bottomNeighbor != null) &&
                    (topNeighbor != null))
                {
                    CheckForIntersection(bottomNeighbor, topNeighbor);
                }
                return true;
            }

            VertexEvent bottom = collected[0];
            VertexEvent top = collected[collected.Count - 1];

            mCompareEdges.CurrentX = curX;
            foreach (VertexEvent e in collected)
            {
                AddActive(e.TriEdge);
            }
            bottomNeighbor = lineiter.FindBottomNeighbor(bottom.TriEdge);
            topNeighbor = lineiter.FindTopNeighbor(top.TriEdge);
            if (bottomNeighbor != null)
            {
                r = CheckForIntersection(bottomNeighbor, bottom.TriEdge);
                if (r > 0)
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
                    r = CheckForIntersection(e1.TriEdge, e2.TriEdge, true);
                    if (r > 0)
                    {
                        return true;
                    }
                }
            }
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
