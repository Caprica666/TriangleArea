using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TriangleArea : MonoBehaviour
{
    public int DebugLevel = 0;
    public bool New;
    public bool PlaneSweep;
    public int Test = 0;
    public int TriangleCount = 2;

    private float mMaxDist = 5;
    private float mMinDist = 1;
    private TriangleMesh mTriMesh;
    private TriangleMesh mClipMesh;
    private List<Triangle> mSaved = null;
    private Rect mBounds = new Rect(0, 0, 6, 6);
    private VertexGroup mVertexGroup;
    private LineMesh mLinesToRender;
    private PointMesh mIntersections;
    private int mRuns = 0;
    public FileLogger DebugLog;

    // Start is called before the first frame update
    void Start()
    {
        DebugLog = new FileLogger(Debug.unityLogger.logHandler);
        Init();
    }

    private void Init()
    {
        GameObject lines = GameObject.Find("Lines");
        GameObject cliplist = GameObject.Find("ClipList");
        mTriMesh = gameObject.GetComponent<TriangleMesh>();
        mClipMesh = cliplist.GetComponent<TriangleMesh>();
        mLinesToRender = new LineMesh(lines.GetComponent<MeshFilter>().mesh);
        GameObject intersections = GameObject.Find("Intersections");
        MeshFilter mf = intersections.GetComponent<MeshFilter>() as MeshFilter;
        mIntersections = new PointMesh(mf.mesh);
        mIntersections.PointSize = 0.1f;
    }
    private void DumpTris()
    {
        string s = "";
        foreach (Triangle t in mSaved)
        {
            s += string.Format("t{0} = new Triangle(new Vector3({1:F2}f, {2:F2}f, 0),\n",
                              t.ID, t.Vertices[0].x, t.Vertices[0].y);
            s += string.Format("       new Vector3({0:F2}f, {1:F2}f, 0),\n", t.Vertices[1].x, t.Vertices[1].y);
            s += string.Format("       new Vector3({0:F2}f, {1:F2}f, 0));\n", t.Vertices[2].x, t.Vertices[2].y);
            s += string.Format("mSaved.Add(t{0});\n", t.ID);
        }
        using (StreamWriter sw = new StreamWriter("Assets/Resources/dumptris.txt", false))
        {
            sw.WriteLine(s);
        }
    }

    private void Update()
    {
        if (New)
        {
            New = false;
            Triangle.NextID = 1;
            DebugLog.DebugLevel = DebugLevel;
            mSaved = NewTriangles(mBounds, TriangleCount * 3);
            mRuns = 0;
            Debug.unityLogger.Log("NEW SET OF " + TriangleCount.ToString() + " TRIANGLES");
            DumpTris();
            mClipMesh.Clear();
            mLinesToRender.Clear();
            mIntersections.Clear();
            mTriMesh.GenerateMesh(mSaved);
        }
        else if (Test > 0)
        {
            int test = Test;
            Triangle t1;
            Triangle t2;
            Triangle t3;
            Triangle t4;
            mSaved = new List<Triangle>();
            DebugLog.DebugLevel = DebugLevel;
            Debug.unityLogger.Log("TEST " + test.ToString());
            mRuns = 0;
            Test = 0;
            Triangle.NextID = 1;
            switch (test)
            {
                default:
                t1 = new Triangle(new Vector3(-1.7f, 3, 0),
                       new Vector3(-1.7f, -0.7f, 0),
                       new Vector3(1, 1.3f, 0));
                mSaved.Add(t1);
                t2 = new Triangle(new Vector3(-1.3f, 2, 0),
                       new Vector3(-1.3f, -1.3f, 0),
                       new Vector3(0, 1, 0));
                mSaved.Add(t2);
                t4 = new Triangle(new Vector3(-2.66f, -1, 0),
                       new Vector3(-1.3f, 0.5f, 0),
                       new Vector3(-0.5f, -1.5f, 0));
                mSaved.Add(t4);
                break;

                case 8:
                t1 = new Triangle(new Vector3(-2.1f, 2.2f, 0),
                                  new Vector3(-1.7f, -1.3f, 0),
                                  new Vector3(2, 0.6f, 0));
                t2 = new Triangle(new Vector3(0.8f, -2.7f, 0),
                                  new Vector3(2.3f, 0.6f, 0),
                                  new Vector3(2.6f, -1.9f, 0));
                t3 = new Triangle(new Vector3(-2.2f, 2.3f, 0),
                                  new Vector3(0.4f, 0.6f, 0),
                                  new Vector3(1.8f, -2.7f, 0));
                t4 = new Triangle(new Vector3(-2.9f, -2.8f, 0),
                                  new Vector3(-1.3f, -1.1f, 0),
                                  new Vector3(1, -2.3f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                mSaved.Add(t3);
                mSaved.Add(t4);
                break;

                case 7:
                t1 = new Triangle(new Vector3(-3, -0.5f, 0),
                                  new Vector3(-1.3f, 3, 0),
                                  new Vector3(-0.8f, -0.5f, 0));
                t2 = new Triangle(new Vector3(-2.4f, 1.5f, 0),
                                  new Vector3(0, 1.5f, 0),
                                  new Vector3(2.5f, -1.5f, 0));
                t3 = new Triangle(new Vector3(-3, -1, 0),
                                  new Vector3(-1.5f, 2, 0),
                                  new Vector3(-0.5f, 0.5f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                mSaved.Add(t3);
                break;

                case 6:
                t1 = new Triangle(new Vector3(-2.6f, 1.8f, 0),
                       new Vector3(-0.25f, -2, 0),
                       new Vector3(1.65f, 0.25f, 0));
                t2 = new Triangle(new Vector3(1, 2.6f, 0),
                       new Vector3(1.5f, 1.3f, 0),
                       new Vector3(2.85f, 2, 0));
                t3 = new Triangle(new Vector3(-1, -0.26f, 0),
                       new Vector3(-0.3f, -2.3f, 0),
                       new Vector3(0.16f, -1.3f, 0));
                t4 = new Triangle(new Vector3(-1.5f, 1, 0),
                       new Vector3(0.6f, 2f, 0),
                       new Vector3(2, -2.75f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                mSaved.Add(t3);
                mSaved.Add(t4);
                break;

                case 5:
                t1 = new Triangle(new Vector3(-1.3f, 1.8f, 0),
                                  new Vector3(-1, -2, 0),
                                  new Vector3(1, -1.4f, 0));
                t2 = new Triangle(new Vector3(-1.7f, 2.5f, 0),
                                  new Vector3(0.5f, -0.1f, 0),
                                  new Vector3(-0.6f, -1, 0));
                t3 = new Triangle(new Vector3(-2.2f, 1.2f, 0),
                                  new Vector3(-0.7f, 2.6f, 0),
                                  new Vector3(2, 0.5f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                mSaved.Add(t3);
                break;

                case 4:
                t1 = new Triangle(new Vector3(-1.5f, -1.5f, 0),
                       new Vector3(0, 1.5f, 0),
                       new Vector3(2.5f, -2, 0));
                mSaved.Add(t1);
                t2 = new Triangle(new Vector3(-1, -2.5f, 0),
                       new Vector3(-1, 2, 0),
                       new Vector3(1, 0, 0));
                mSaved.Add(t2);
                break;

                case 3:
                t1 = new Triangle(new Vector3(-0.7f, 0.4f, 0),
                                  new Vector3(2.2f, -1.2f, 0),
                                  new Vector3(2.5f, 2.9f, 0));
                t2 = new Triangle(new Vector3(-1.7f, 2.1f, 0),
                                  new Vector3(1.1f, -1.9f, 0),
                                  new Vector3(1.4f, 0.2f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                break;

                case 2:
                t1 = new Triangle(new Vector3(0.7f, -0.7f, 0),
                                  new Vector3(2.3f, 2.9f, 0),
                                  new Vector3(2.5f, 0.3f, 0));
                t2 = new Triangle(new Vector3(-2.0f, -2.6f, 0),
                                  new Vector3(-0.8f, 1.7f, 0),
                                  new Vector3(0.9f, -2.4f, 0));
                t3 = new Triangle(new Vector3(-1.9f, -0.6f, 0),
                                  new Vector3(-1.0f, 2.3f, 0),
                                  new Vector3(2.9f, 2.4f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                mSaved.Add(t3);
                break;

                case 1:
                t1 = new Triangle(new Vector3(-1.5f, -1.0f, 0),
                       new Vector3(2.5f, 2f, 0),
                       new Vector3(4f, -1.4f, 0));
                t2 = new Triangle(new Vector3(-2.5f, -2f, 0),
                       new Vector3(0.5f, 2.50f, 0),
                       new Vector3(1.4f, 0.4f, 0));
                mSaved.Add(t1);
                mSaved.Add(t2);
                break;
            }
            mClipMesh.Clear();
            TriangleCount = mSaved.Count;
            mTriMesh.VertexCount = TriangleCount * 3;
            mTriMesh.GenerateMesh(mSaved);
            mClipMesh.VertexCount = TriangleCount * 3;
        }
        else if (PlaneSweep)
        {
            DebugLog.DebugLevel = DebugLevel;
            PlaneSweep = false;
            TriangleCount = mSaved.Count;
            Debug.unityLogger.Log("PLANESWEEEP RUN " + (++mRuns).ToString());
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
        mTriMesh.Clear();
        mClipMesh.Clear();
        mLinesToRender.Clear();
        mIntersections.Clear();
        mTriMesh.VertexCount = TriangleCount * 3;
        mClipMesh.VertexCount = TriangleCount * 3;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Triangle.NextID = 1;
        mVertexGroup = new VertexGroup(mTriMesh, mLinesToRender, mClipMesh, mIntersections);
        mVertexGroup.AddTriangles(mSaved);
        mTriMesh.Display();
        mClipMesh.Display();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        StartCoroutine(mVertexGroup.ShowIntersections());
    }

}

   
