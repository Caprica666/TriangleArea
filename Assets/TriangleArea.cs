using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleArea : MonoBehaviour
{
    public bool New;
    public bool PlaneSweep;
    public int TriangleCount = 2;

    private float mMaxDist = 5;
    private float mMinDist = 1;
    private TriangleMesh mTriMesh;
    private TriangleMesh mClipMesh;
    private List<Triangle> mSaved = null;
    private Rect mBounds = new Rect(0, 0, 6, 6);
    private VertexGroup mVertexGroup;
    private LineMesh mLinesToRender;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    private void Init()
    {
        GameObject lines = GameObject.Find("Lines");
        GameObject cliplist = GameObject.Find("ClipList");
        mTriMesh = gameObject.GetComponent<TriangleMesh>();
        mClipMesh = cliplist.GetComponent<TriangleMesh>();
        mLinesToRender = new LineMesh(lines.GetComponent<MeshFilter>().mesh);
        mVertexGroup = new VertexGroup(mTriMesh, mLinesToRender, mClipMesh);
    }

    private void Update()
    {
        if (New)
        {
            New = false;
            mSaved = NewTriangles(mBounds, TriangleCount * 3);
            mTriMesh.VertexCount = TriangleCount * 3;
            mTriMesh.GenerateMesh(mSaved);
            mClipMesh.VertexCount = TriangleCount * 3;
        }
        else if (PlaneSweep)
        {
            PlaneSweep = false;
            StartCoroutine(PlaneSweepArea());
        }
    }

    public List<Triangle> NewTriangles(Rect bounds, int numverts)
    {
        List<Triangle> trilist = new List<Triangle>();
        Color color = new Color(1, 1, 1, 0.5f);
        float size = bounds.width;

        mBounds = bounds;
        for (int i = 0; i < numverts; i += 3)
        {
            /*
             * generate 3 triangles with random vertices
             */
            float x = Random.value - 0.5f;
            float y = Random.value - 0.5f;
            Vector3 v1;
            Vector3 v2;
            Vector3 v3;

            color.r = Random.value;
            color.g = Random.value;
            color.b = Random.value;
            color.a = 0.5f;
            v1 = new Vector3(size * x + bounds.x,
                             size * y + bounds.y,
                             0);
            do
            {
                x = Random.value - 0.5f;
                y = Random.value - 0.5f;
                v2 = new Vector3(size * x + bounds.x,
                                 size * y + bounds.y,
                                 0);
                x = Random.value - 0.5f;
                y = Random.value - 0.5f;
                v3 = new Vector3(size * x + bounds.x,
                                 size * y + bounds.y,
                                 0);
            }
            while (!CheckTriSize(v1, v2, v3));
            Triangle t = new Triangle(v1, v2, v3);
            trilist.Add(t);
        }
        return trilist;
    }

    public bool CheckTriSize(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 v = v1 - v2;
        float m = v.magnitude;
        if ((m > mMaxDist) || (m < mMinDist))
        {
            return false;
        }
        v = v1 - v3;
        m = v.magnitude;
        if ((m > mMaxDist) || (m < mMinDist))
        {
            return false;
        }
        return true;
    }

    IEnumerator PlaneSweepArea()
    {
        mTriMesh.VertexCount = TriangleCount * 3;
        mClipMesh.VertexCount = TriangleCount * 3;
        mTriMesh.Clear();
        mClipMesh.Clear();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        foreach (Triangle t in mSaved)
        {
            t.VertexIndex = -1;
        }
        mVertexGroup.AddTriangles(mSaved);
        mLinesToRender.Clear();
        mTriMesh.Display();
        mClipMesh.Display();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        StartCoroutine(mVertexGroup.ShowIntersections());
    }

}

   
