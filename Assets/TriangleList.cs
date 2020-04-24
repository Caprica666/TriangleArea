using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TriangleList
{
    private HashSet<Triangle> mTriangles;
    private List<Triangle.Edge> mEdges;
    private TriangleMesh mTriMesh;

    public List<Triangle> Triangles
    {   get { return mTriangles.ToList(); } }

    public List<Triangle.Edge> Edges
    { get { return mEdges; } }

    public TriangleMesh TriMesh
    {
        get { return mTriMesh; }
        set { mTriMesh = value; } 
    }

    public TriangleList()
    {
        mTriangles = new HashSet<Triangle>();
        mEdges = new List<Triangle.Edge>();
    }

    public TriangleList(List<Triangle> triangles)
    {
        mTriangles = new HashSet<Triangle>();
        mEdges = new List<Triangle.Edge>();
        Add(triangles);
    }

    public Triangle.Edge GetEdge(int i)
    {
        return mEdges[i];
    }

    public int Count
    {
        get { return mTriangles.Count; }
    }

    public Vector3 GetVertex(int vertIndex)
    {
        return mEdges[vertIndex].Vertex;
    }

    public void Add(Triangle t, bool addtomesh = false)
    {
        if (!mTriangles.Add(t))
        {
            return;
        }
        mEdges.Add(t.GetEdge(0));
        mEdges.Add(t.GetEdge(1));
        mEdges.Add(t.GetEdge(2));
        if (addtomesh && (mTriMesh != null))
        {
            mTriMesh.AddTriangle(t);
        }
    }

    public void Update(Triangle t)
    {
        if (mTriMesh != null)
        {
            mTriMesh.UpdateTriangle(t);
        }
    }

    public void Add(List<Triangle> triangles, bool addtomesh = true)
    {
        foreach (Triangle t in triangles)
        {
            Add(t, addtomesh);
        }
    }

    public bool Remove(Triangle t)
    {
        return mTriangles.Remove(t);
    }

    public void Remove(List<Triangle> triangles)
    {
        foreach (Triangle t in triangles)
        {
            mTriangles.Remove(t);
            mEdges.Remove(t.GetEdge(0));
            mEdges.Remove(t.GetEdge(1));
            mEdges.Remove(t.GetEdge(2));
        }
    }

    public void Clear()
    {
        mTriangles.Clear();
        mEdges.Clear();
        if (mTriMesh != null)
        {
            mTriMesh.Clear();
        }
    }

    public List<Triangle.Edge> GetEdges()
    {
        return mEdges;
    }

    public void GenerateEdges()
    {
        mEdges.Clear();
        foreach (Triangle t in mTriangles)
        {
            mEdges.Add(t.GetEdge(0));
            mEdges.Add(t.GetEdge(1));
            mEdges.Add(t.GetEdge(2));
        }
        if (mTriMesh != null)
        {
            mTriMesh.GenerateMesh(this);
        }
        SortByX();
    }

    public void Display(bool makemesh = false)
    {
        if (mTriMesh != null)
        {
            if (makemesh)
            {
                mTriMesh.GenerateMesh(this);
            }
            else
            {
                mTriMesh.Display();
            }
        }
    }

    public System.Collections.IEnumerator Clip(List<Triangle.Edge> newhull,
                                               TriangleList toclip)
    {
        List<Triangle> clipped;
        TriangleList mHullTris = new TriangleList();

        /*
         * Make a triangle list from the hull vertices
         * containing only triangles from the hull
         */
        foreach (Triangle.Edge e in newhull)
        {
            mHullTris.Add(e.Owner);
        }
        List<Triangle> clipagainst = mHullTris.Triangles;

        Remove(clipagainst);
        /*
         * Scan triangles added from the last iteration
         * and remove the ones outside the new hull
         */
        if (toclip.Count > 0)
        {
            clipped = toclip.ClipHull(newhull);
            Remove(clipped);
        }
        /*
         * Clip triangles in new hull against one another
         */
        mHullTris.ClipAgainst(clipagainst);
        yield return new WaitForEndOfFrame();
        toclip.ClipAgainst(mHullTris.Triangles);
        toclip.Add(mHullTris.Triangles);
        toclip.Display();
        yield return new WaitForEndOfFrame();
        GenerateEdges();
        yield return new WaitForEndOfFrame();
    }

    /*
     * Clip all triangles against a given list.
     * Upon return, the triangle list will contain
     * independent, non-intersection triangles.
     */
    public void ClipAgainst(List<Triangle> clipagainst)
    {
        for (int i = 0; i < clipagainst.Count; ++i)
        {
            Triangle tri1 = clipagainst[i];
            int numclipped = 0;
            List<Triangle> cliplist = Triangles;

            for (int j = 0; j < cliplist.Count; ++j)
            {
                Triangle tri2 = cliplist[j];

                if (tri1 == tri2)
                {
                    continue;
                }
                int r = Clip(tri2, tri1);

                switch (r)
                {
                    case 1:     // tri1 clips tri2
                    ++numclipped;
                    break;

                    case 0:    // tri2 inside tri1
                    Remove(tri2);
                    break;
                }
            }
            if (Count <= 1)
            {
                return;
            }
            if (numclipped == 0)
            {
                clipagainst.RemoveAt(i--);
                if (clipagainst.Count == 0)
                {
                    return;
                }
            }
        }
    }

    public System.Collections.IEnumerator ClipAll(TriangleList result)
    {
        List<Triangle> trilist = null;
        do
        {
            List<Triangle> cliplist = Triangles;
            trilist = Triangles;

            for (int i = 0; i < trilist.Count - 1; ++i)
            {
                Triangle        tri1 = trilist[i];
                int             numclipped = 0;

                for (int j = i + 1; j < cliplist.Count; ++j)
                {
                    Triangle tri2 = cliplist[j];

                    if (tri1 == tri2)   // dont clip against yourself
                    {
                        continue;
                    }
                    int r = Clip(tri1, tri2);

                    switch (r)
                    {
                        case 1:     // tri2 clips tri1
                        ++numclipped;
                        break;

                        case 0:    // tri1 inside tri2
                        Remove(tri1);
                        break;

                        default:
                        continue;
                    }
                    Display();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                }
                if (numclipped == 0)
                {
                    result.Add(tri1);
                    Remove(tri1);
                    Display(true);
                    result.Display(true);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        while (Count > 1);
        trilist = Triangles;
        result.Add(trilist[0], false);
        result.Display(true);
        Clear();
    }

    /*
     * Discard the triangles in the list that are outside of the convex hull
     */
    public List<Triangle> ClipHull(List<Triangle.Edge> hull)
    {
        Vector3 isect = new Vector3();
        List<Triangle> clipped = new List<Triangle>();
        int numremoved = 0;
        List<Triangle> toclip = mTriangles.ToList();
        int[] inside = new int[3];

        foreach (Triangle t in toclip)
        {
            for (int i = 0; i < hull.Count - 1; ++i)
            {
                Triangle.Edge e = hull[i];

                if (e.Owner == t)
                {
                    continue;
                }
                int i1 = t.Intersects(0, e, ref isect);
                int i2 = t.Intersects(1, e, ref isect);
                int i3 = t.Intersects(2, e, ref isect);

                if (Clip(t, e, inside) > 0)
                {
                    clipped.Add(t);
                    Remove(t);
                    ++numremoved;
                    break;
                }
            }
        }
        return clipped;
    }


    /* 
     * Clips the first triangle against the second.
     * @returns -1 = triangle 1 outside triangle 2
     *          0 = triangle 1 inside triangle 2
     *          1 = triangle 1 was clipped by triangle 2
     *
     */
    public int Clip(Triangle tri1, Triangle tri2)
    {
        if (tri1 == tri2)
        {
            return -1;
        }
        int[] inside = new int[3];
        int c1 = -1;
        int c2 = -1;
        int c3 = -1;

        inside[0] = tri2.Contains(tri1.GetVertex(0));
        inside[1] = tri2.Contains(tri1.GetVertex(1));
        inside[2] = tri2.Contains(tri1.GetVertex(2));

        if ((inside[0] | inside[1] | inside[2]) == 1)
        {
            return 0;           // tri1 inside tri2
        }
        c1 = Clip(tri1, tri2.GetEdge(0), inside);
        if (c1 >= 0)      // vertex on the edge
        {
            return 1;
        }
        c2 = Clip(tri1, tri2.GetEdge(1), inside);
        if (c2 >= 0)      // vertex on the edge
        {
            return 1;
        }
        c3 = Clip(tri1, tri2.GetEdge(2), inside);
        return (c3 >= 0) ? 1 : -1;
    }

    /*
     * Clips a triangle against a line.
     * @returns 1 = triangle was clipped
     *          0 = triangle was clipped, no further clipping needed
     *          -1 = triangle was not clipped by this edge
     */
    public int Clip(Triangle tri1, Triangle.Edge edge2, int[] inside)
    {
        Vector3 isect0 = new Vector3();
        Vector3 isect1 = new Vector3();
        Vector3 isect2 = new Vector3();
        int i0;
        int i1;
        int i2;

        i0 = tri1.Intersects(0, edge2, ref isect0);
        if (inside[0] == 0)
        {
            i0 = 0;
        }
        i1 = tri1.Intersects(1, edge2, ref isect1);
        if (inside[1] == 0)
        {
            i1 = 0;
        }
        i2 = tri1.Intersects(2, edge2, ref isect2);
        if (inside[2] == 0)
        {
            i2 = 0;
        }
        if ((i0 + i1 + i2) < 1)
        {
            return -1;
        }
        if (i0 > 0)                     // first edge intersects input edge
        {
            if (i1 > 0)                 // second edge intersects input edge
            {
                if (((isect0 - isect1).sqrMagnitude) < 1e-7)
                {
                    return -1;
                }
                if (AddTriangles(tri1, isect0, 0, isect1, 2, inside))
                {
                    tri1.Update(isect0, tri1.GetVertex(1), isect1);
                    Update(tri1);
                    return 1;
                }
                return -1;
            }
            else if (i2 > 0)             // third edge intersects input edge
            {
                if (((isect0 - isect2).sqrMagnitude) < 1e-7)
                {
                    return -1;
                }
                if (AddTriangles(tri1, isect0, 1, isect2, 2, inside))
                {
                    tri1.Update(isect0, tri1.GetVertex(0), isect2);
                    Update(tri1);
                    return 1;
                }
                return -1;
            }
        }
        else if (i1 > 0)        // second edge intersects input edge
        {
            if (i2 > 0)
            {
                if (((isect1 - isect2).sqrMagnitude) < 1e-7)
                {
                    return -1;
                }
                if (AddTriangles(tri1, isect1, 1, isect2, 0, inside))
                {
                    tri1.Update(isect1, tri1.GetVertex(2), isect2);
                    Update(tri1);
                    return 1;
                }
                return -1;
            }
        }
        return -1;              // no intersections
    }

    bool AddTriangles(Triangle tri, Vector3 isect1, int vindex1, Vector3 isect2, int vindex2, int[] inside)
    {
        bool result = false;

        if ((inside[vindex1] >= 0) &&
            (inside[vindex2] >= 0))
        {
            return false;
        }
        Vector3 va = tri.GetVertex(vindex1);
        Vector3 vb = tri.GetVertex(vindex2);
        int count = (mTriMesh != null) ? mTriMesh.VertexCount : 0;

        if (((va - isect1).sqrMagnitude > 1e-7) &&
            ((va - isect2).sqrMagnitude > 1e-7))
        {
            Triangle tri1 = new Triangle(va, isect1, isect2, count);
            count += 3;
            Add(tri1, true);
            result = true;
        }
        if ((vb - isect2).sqrMagnitude > 1e-7)
        {
            Triangle tri2 = new Triangle(va, isect2, vb, count);
            Add(tri2, true);
            result = true;
        }
        return result;
    }

    public void SortByX()
    {
        mEdges = SortByX(mEdges);
    }

    /*
     * Sort the triangle vertices based on X coordinate of
     * leftmost vertex
     */
    public static List<Triangle.Edge> SortByX(List<Triangle.Edge> verts)
    {
        int n = verts.Count;
        int m = n / 2;
        if (n <= 1)
        {
            return verts;
        }
        List<Triangle.Edge> left = verts.GetRange(0, m);
        List<Triangle.Edge> right = verts.GetRange(m, n - m);

        if (left.Count > 1)
        {
            left = SortByX(left);
        }
        if (right.Count > 1)
        {
            right = SortByX(right);
        }
        return Merge(left, right);
    }

    /*
     * Merge two indexed triangle lists based on the X
     * coordinate of the first vertex (which will have
     * the smallest X in that triangle.
     */
    public static List<Triangle.Edge> Merge(List<Triangle.Edge> left, List<Triangle.Edge> right)
    {
        List<Triangle.Edge> result = new List<Triangle.Edge>();
        int lofs = 0;
        int rofs = 0;

        while ((lofs < left.Count) && (rofs < right.Count))
        {
            Triangle.Edge tvleft = left[lofs];
            Triangle.Edge tvright = right[rofs];
            Vector3 v1 = tvleft.Vertex;
            Vector3 v2 = tvright.Vertex;

            if (v1.x <= v2.x)
            {
                result.Add(left[lofs++]);
            }
            else
            {
                result.Add(right[rofs++]);
            }
        }

        // Either left or right may have elements left; consume them.
        // (Only one of the following loops will actually be entered.)
        while (lofs < left.Count)
        {
            result.Add(left[lofs++]);
        }
        while (rofs < right.Count)
        {
            result.Add(right[rofs++]);
        }
        return result;
    }

}


