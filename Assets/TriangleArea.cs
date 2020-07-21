using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleArea : MonoBehaviour
{
    public bool Test;
    public bool New;
    public int TriangleCount = 2;

    private float mMaxDist = 5;
    private float mMinDist = 1;
    private TriangleMesh mTriMesh;
    private TriangleMesh mClipMesh;
    private EdgeGroup mEdgeGroup;
    private List<Triangle> mSaved = null;
    private Rect mBounds = new Rect(0, 0, 6, 6);

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    private void Init()
    {
        GameObject cliplist = GameObject.Find("ClipList");
        mTriMesh = gameObject.GetComponent<TriangleMesh>() as TriangleMesh;
        mClipMesh = cliplist.GetComponent<TriangleMesh>() as TriangleMesh;
        Triangle t1 = new Triangle(new Vector3(2, 0, 0),
                                  new Vector3(0, 2, 0),
                                  new Vector3(-2, 0, 0), 0);
        Triangle t2 = new Triangle(new Vector3(0, -1, 0),
                                  new Vector3(1.5f, 3, 0),
                                  new Vector3(3, -1, 0), 3);
/*
        Triangle t3 = new Triangle(new Vector3(-0.6508294f, -2.309343f, 0),
                                   new Vector3(1.754553f, 0.9481241f, 0),
                                   new Vector3(0.4796473f, 0.6368812f, 0), 6);
                                   */
        mSaved = new List<Triangle> { t1, t2 };
        mClipMesh.VertexCount = TriangleCount * 3;
        mClipMesh.GenerateMesh(mSaved);
    }

    private void Update()
    {
        if (Test)
        {
            Test = false;
            mTriMesh.Clear();
            mClipMesh.Clear();
            StartCoroutine("PlaneSweepTest");
        }
        else if (New)
        {
            New = false;
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

    IEnumerator PlaneSweepTest()
    {
        mTriMesh.Clear();
        mTriMesh.VertexCount = TriangleCount * 3;
        mClipMesh.VertexCount = TriangleCount * 3;
        mClipMesh.GenerateMesh(mSaved);
        mEdgeGroup = new EdgeGroup(mSaved[0], mTriMesh, mClipMesh);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        for (int i = 1; i < mSaved.Count; ++i)
        {
            Triangle t = mSaved[i];
            yield return mEdgeGroup.Add(t);
        }
        mTriMesh.Display();
    }

    IEnumerator PlaneSweepArea()
    {
        mSaved = NewTriangles(mBounds, TriangleCount * 3);
        mTriMesh.VertexCount = TriangleCount * 3;
        mClipMesh.VertexCount = TriangleCount * 3;
        mTriMesh.Clear();
        mClipMesh.Clear();
        mClipMesh.GenerateMesh(mSaved);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        mEdgeGroup = new EdgeGroup(mSaved[0], mTriMesh, mClipMesh);
        for (int i = 1; i < mSaved.Count; ++i)
        {
            Triangle t = mSaved[i];
            yield return mEdgeGroup.Add(t);
        }
        mTriMesh.Display();
        mClipMesh.Display();
    }

}

   
