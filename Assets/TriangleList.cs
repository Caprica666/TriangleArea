using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityScript.Steps;

public class TriangleList
{
    private HashSet<Triangle> mTriangles;
    private List<Triangle.Edge> mEdges;
    private TriangleMesh mTriMesh;
    private static readonly float EPSILON = 1e-5f;

    public enum ClipResult
    {
        OUTSIDE = -1,
        INSIDE = 0,
        CLIPPED =  1,
        INTERSECTED = 2
    };
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

    public List<Triangle> SaveTriangles()
    {
        int vindex = 0;
        List<Triangle> triangles = new List<Triangle>();

        foreach (Triangle t in mTriangles)
        {
            triangles.Add(new Triangle(t));
            t.VertexIndex = vindex;
            vindex += 3;
        }
        return triangles;
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

    public void Clear(bool clearmesh = false)
    {
        mTriangles.Clear();
        mEdges.Clear();
        if (clearmesh && (mTriMesh != null))
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
                                               TriangleList toclip,
                                               TriangleList hulltris = null)
    {
        List<Triangle> clipped;
        TriangleList mHullTris = (hulltris != null) ? hulltris : new TriangleList();

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
        System.Collections.IEnumerator iter = mHullTris.ClipAll();
        while (iter.MoveNext())
        {
//            yield return new WaitForEndOfFrame();
//            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        toclip.ClipAgainst(mHullTris.Triangles);
        toclip.Add(mHullTris.Triangles);
        toclip.Display();
        yield return new WaitForEndOfFrame();
        GenerateEdges();
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
                ClipResult r = Clip(tri2, tri1);

                switch (r)
                {
                    case ClipResult.CLIPPED:
                    ++numclipped;     // tri1 clips tri2
                    break;

                    case ClipResult.INSIDE:
                    Remove(tri2);    // tri2 inside tri1
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

    public System.Collections.IEnumerator ClipAll()
    {
        List<Triangle> trilist = null;
        List<Triangle> isolated = new List<Triangle>();

        do
        {
            List<Triangle> cliplist = Triangles;
            int getout = 0;
            trilist = Triangles;

            for (int i = 0; i < trilist.Count - 1; ++i)
            {
                Triangle tri1 = trilist[i];
                bool wasclipped = false;
                int j = i + 1;

                if (tri1.IsDegenerate())
                {
                    Remove(tri1);
                    continue;
                }

                while ((getout == 0) && (j < cliplist.Count))
                {
                    Triangle tri2 = cliplist[j++];

                    Display(true);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    if (tri1 == tri2)   // dont clip against yourself
                    {
                        continue;
                    }
                    if (tri1.Contains(tri2)) // tri2 inside tri1
                    {
                        Remove(tri2);
                        ++getout;
                    }
                    else
                    {
                        ClipResult r = Clip(tri1, tri2);
                        switch (r)
                        {
                            case ClipResult.CLIPPED:
                            wasclipped = true;          // tri2 clips tri1
                            break;

                            case ClipResult.INSIDE:
                            Remove(tri1);               // tri1 inside tri2
                            break;

                            case ClipResult.INTERSECTED:
                            switch (Clip(tri2, tri1))
                            {
                                case ClipResult.CLIPPED: // tri1 clips tri2
                                ++getout;
                                break;

                                case ClipResult.INSIDE: // tri2 inside tri1
                                Remove(tri2);
                                isolated.Add(tri2);
                                ++getout;
                                break;

                                default:
                                continue;
                            }
                            break;

                            default:
                            continue;
                        }
                        ++getout;
                    }
                }
                if (--getout > 0)
                {
                    break;
                }
                if (!wasclipped)
                {
                    isolated.Add(tri1);
                    Remove(tri1);
                }
            }
        }
        while (Count > 1);
        trilist = Triangles;
        if (Count > 0)
        {
            isolated.Add(trilist[0]);
            Display(true);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }
        Clear(true);
        Add(isolated);
        Display();
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
        int edgehit = -1;

        foreach (Triangle t in toclip)
        {
            for (int i = 0; i < hull.Count - 1; ++i)
            {
                Triangle.Edge e = hull[i];
                Vector3 v = new Vector3();

                if (e.Owner == t)
                {
                    continue;
                }
                int i1 = t.Intersects(0, e, ref isect);
                int i2 = t.Intersects(1, e, ref isect);
                int i3 = t.Intersects(2, e, ref isect);

                if (Clip(t, e, inside, ref v, ref edgehit) > 0)
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
     * @returns OUTSIDE = triangle 1 outside triangle 2
     *          INSIDE = triangle 1 inside triangle 2
     *          CLIPPED = triangle 1 was clipped by triangle 2
     *          INTERSECTED = triangle 2 may be clipped by triangle 1
     */
    public ClipResult Clip(Triangle tri1, Triangle tri2)
    {
        if (tri1 == tri2)
        {
            return ClipResult.OUTSIDE;
        }
        int[] inside = new int[3];
        int[] edgeshit = new int[] { -1, -1, -1 };
        int c1 = -1;
        int c2 = -1;
        int c3 = -1;
        int intersections = 0;
        Vector3[] isect = new Vector3[3];
        int sum;

        inside[0] = tri2.Contains(tri1.GetVertex(0));
        inside[1] = tri2.Contains(tri1.GetVertex(1));
        inside[2] = tri2.Contains(tri1.GetVertex(2));
        sum = inside[0] + inside[1] + inside[2];

        if ((sum != 0) &&
            (inside[0] >= 0) &&
            (inside[1] >= 0) &&
            (inside[2] >= 0))
        {
            return ClipResult.INSIDE;  // tri1 inside tri2
        }
        if (sum == -2)
        {
            return ClipResult.OUTSIDE;
        }
        if (inside[0] == 0)
        {
            if (inside[1] == 0)
            {
                if (inside[2] >= 0)
                {
                    return ClipResult.INSIDE;
                }
            }
            else if (inside[2] == 0)
            {
                if (inside[1] >= 0)
                {
                    return ClipResult.INSIDE;
                }
            }
        }
        else if ((inside[1] == 0) &&
                 (inside[2] == 0))
        {
            if (inside[0] >= 0)
            {
                return ClipResult.INSIDE;
            }
        }

        c1 = Clip(tri1, tri2.GetEdge(0), inside, ref isect[0], ref edgeshit[0]);
        switch (c1)
        {
            case (int) ClipResult.INSIDE:
            return ClipResult.INSIDE;

            case 2:           // 2 intersections, was clipped
            return ClipResult.CLIPPED;

            case 1:            // only one intersection
            ++intersections;
            break;
        }
        c2 = Clip(tri1, tri2.GetEdge(1), inside, ref isect[1], ref edgeshit[1]);
        switch (c2)
        {
            case (int) ClipResult.INSIDE:
            return ClipResult.INSIDE;

            case 2:           // 2 intersections, was clipped
            return ClipResult.CLIPPED;

            case 1:             // 2 intersections, different edges
            ++intersections;
            break;
        }
        c3 = Clip(tri1, tri2.GetEdge(2), inside, ref isect[2], ref edgeshit[2]);
        switch (c3)
        {
            case (int) ClipResult.INSIDE:
            return ClipResult.INSIDE;

            case 2:           // 2 intersections, was clipped
            return ClipResult.CLIPPED;

            case 1:             // 2 intersections, different edges
            if (++intersections == 2)
            {
                if (c1 == 1)
                {
                    if (edgeshit[0] == edgeshit[2])
                    {
                        return ClipResult.INTERSECTED;
                    }
                    if (AddTriangles(tri1.GetVertex(0), tri1.GetVertex(1), tri1.GetVertex(2), isect[0], isect[2]))
                    {
                        tri1.Update(tri1.GetVertex(0), isect[0], isect[2]);
                        return ClipResult.CLIPPED;
                    }
                    return ClipResult.OUTSIDE;
                }
                else if (c2 == 1)
                {
                    if (edgeshit[1] == edgeshit[2])
                    {
                        return ClipResult.INTERSECTED;
                    }
                    if (AddTriangles(tri1.GetVertex(2), tri1.GetVertex(1), tri1.GetVertex(0), isect[1], isect[2]))
                    {
                        tri1.Update(isect[1], isect[2], tri1.GetVertex(2));
                        return ClipResult.CLIPPED;
                    }
                    return ClipResult.OUTSIDE;
                }
            }
            break;
        }
        if (intersections == 2)
        {
            if (edgeshit[1] == edgeshit[0])
            {
                return ClipResult.INTERSECTED;
            }
            if (AddTriangles(tri1.GetVertex(1), tri1.GetVertex(0), tri1.GetVertex(2), isect[0], isect[1]))
            {
                tri1.Update(isect[0], tri1.GetVertex(1), isect[1]);
                return ClipResult.CLIPPED;
            }
            return ClipResult.OUTSIDE;
        }
        if (intersections == 1)
        {
            return ClipResult.INTERSECTED;
        }
        return ClipResult.OUTSIDE;
    }

    /*
     * Clips a triangle against a line.
     * @returns 2 = 2 intersections on this edge
     *          1 = only one edge intersection
     *          INSIDE = triangle edge coincident with line
     *          OUTSDIDE = triangle was not clipped by this edge
     *          1 = only one edge intersection
     */
    public int Clip(Triangle tri1, Triangle.Edge edge2, int[] inside, ref Vector3 isect, ref int edgeindex)
    {
        Vector3 isect0 = new Vector3();
        Vector3 isect1 = new Vector3();
        Vector3 isect2 = new Vector3();
        int i0 = tri1.Intersects(0, edge2, ref isect0);
        int i1 = tri1.Intersects(1, edge2, ref isect1);
        int i2 = tri1.Intersects(2, edge2, ref isect2);
        int intersections = 0;

        if ((i0 + i1 + i2) <= -2)
        {
            return (int) ClipResult.OUTSIDE;
        }
        if (i0 > 0)                     // first edge intersects input edge
        {
            isect = isect0;
            ++intersections;
            if (i1 > 0)                 // second edge intersects input edge
            {
                edgeindex = 0;
                if (AddTriangles(tri1.GetVertex(1), tri1.GetVertex(0), tri1.GetVertex(2), isect0, isect1))
                {
                    tri1.Update(isect0, tri1.GetVertex(1), isect1);
                    return 2;
                }
                return (int) ClipResult.OUTSIDE;
            }
            else if (i2 > 0)             // third edge intersects input edge
            {
                isect = isect2;
                edgeindex = 2;
                ++intersections;
                if (AddTriangles(tri1.GetVertex(0), tri1.GetVertex(1), tri1.GetVertex(2), isect0, isect2))
                {
                    tri1.Update(tri1.GetVertex(0), isect0, isect2);
                    return 2;
                }
                return (int) ClipResult.OUTSIDE;
            }
        }
        else if (i1 > 0)        // second edge intersects input edge
        {
            isect = isect1;
            ++intersections;
            if (i2 > 0)
            {
                edgeindex = 1;
                if (AddTriangles(tri1.GetVertex(2), tri1.GetVertex(1), tri1.GetVertex(0), isect1, isect2))
                {
                    tri1.Update(isect1, isect2, tri1.GetVertex(2));
                    return 2;
                }
                return (int) ClipResult.OUTSIDE;
            }
        }
        if (intersections == 1)   // one intersection only
        {
            int temp = i0 & i1 & i2;
            return (temp == 0) ? (int) ClipResult.INSIDE : 1;
        }
        return (int) ClipResult.OUTSIDE; // no intersections
    }

    bool AddTriangles(Vector3 va, Vector3 vb, Vector3 vc, Vector3 isect1, Vector3 isect2)
    {
        int count = (mTriMesh != null) ? mTriMesh.VertexCount : 0;
        Vector3 temp1;
        Vector3 temp2;

        temp1 = isect1 - isect2;
        if (temp1.sqrMagnitude <= EPSILON)
        {
            return false;
        }
        temp1 = va - isect2;
        temp2 = va - isect1;
        if ((temp1.sqrMagnitude <= EPSILON) ||
            (temp2.sqrMagnitude <= EPSILON))
        {
            return false;
        }
        temp1 = vb - isect2;
        temp2 = vb - isect1;
        if ((temp1.sqrMagnitude > EPSILON) &&
            (temp2.sqrMagnitude > EPSILON))
        {
            Triangle tri1 = new Triangle(vb, isect1, isect2, count);
            count += 3;
            Add(tri1, true);
            temp1 = vc - isect2;
            temp2 = vc - isect1;
            if ((temp1.sqrMagnitude > EPSILON) &&
                (temp2.sqrMagnitude > EPSILON))
            {
                Triangle tri2 = new Triangle(vb, isect2, vc, count);
                Add(tri2, true);
            }
            return true;
        }
        return false;
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

            if (v1.x >= v2.x)
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


