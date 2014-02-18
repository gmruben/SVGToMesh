using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Triangulator
{
    private List<Vector2> vertexList;
    private List<int> triangleList;

    private List<int> vertexIndexList;
    private List<int> earVertexList;
    private List<int> convexVertexList;
    private List<int> reflexVertexList;

    public Triangulator(List<Vector2> vertexList)
    {
        this.vertexList = vertexList;

        vertexIndexList = new List<int>();
        for (int i = 0; i < vertexList.Count; i++)
        {
            vertexIndexList.Add(i);
        }
    }

    public int[] Triangulate()
    {
        if (vertexList.Count < 3)
        {
            Debug.LogError("Cannot triangulate with less than 3 vertices");
        }

        // Set up phase
        initializeConvexList();
        initializeReflexList();
        initializeEarList();

        // Run the algorithm
        return InnerTriangulate();
    }

    private void initializeConvexList()
    {
        convexVertexList = new List<int>();
        for (int i = 0; i < vertexList.Count; i++)
        {
            if (isConvex(i)) convexVertexList.Add(i);
        }
    }

    private void initializeReflexList()
    {
        reflexVertexList = new List<int>();
        for (int i = 0; i < vertexList.Count; i++)
        {
            if (isReflex(i)) reflexVertexList.Add(i);
        }
    }

    private void initializeEarList()
    {
        earVertexList = new List<int>();
        for (int i = 0; i < vertexList.Count; i++)
        {
            if (isEar(i)) earVertexList.Add(i);
        }
    }

    private bool isConvex(int vertexIndex)
    {
        Vector2 v0 = vertexList[calculatePrevIndex(vertexIndex)];
        Vector2 v1 = vertexList[vertexIndex];
        Vector2 v2 = vertexList[calculateNextIndex(vertexIndex)];

        Vector2 v1v0 = v1 - v0;
        Vector2 v1v2 = v1 - v2;

        float angle = Mathf.Atan2(v1v0.y - v1v2.y, v1v0.x - v1v2.x);
        return angle >= Mathf.PI;
    }

    private bool isReflex(int vertexIndex)
    {
        Vector2 v0 = vertexList[calculatePrevIndex(vertexIndex)];
        Vector2 v1 = vertexList[vertexIndex];
        Vector2 v2 = vertexList[calculateNextIndex(vertexIndex)];

        Vector2 v1v0 = v1 - v0;
        Vector2 v1v2 = v1 - v2;

        float angle = Mathf.Atan2(v1v0.y - v1v2.y, v1v0.x - v1v2.x);
        return angle < Mathf.PI; ;
    }

    private bool isEar(int vertexIndex)
    {
        Vector2 v0 = vertexList[calculatePrevIndex(vertexIndex)];
        Vector2 v1 = vertexList[vertexIndex];
        Vector2 v2 = vertexList[calculateNextIndex(vertexIndex)];

        foreach (Vector2 vertex in vertexList)
        {
            if (isPointInTriangle(vertex, v0, v1, v2)) return true;
        }

        return false;
    }

    private bool isPointOnSameSide(Vector2 point1, Vector2 point2, Vector2 vertex1, Vector2 vertex2)
    {
        Vector3 v2v1 = new Vector3(vertex2.x, 0, vertex2.y) - new Vector3(vertex1.x, 0, vertex1.y);
        Vector3 p1v1 = new Vector3(point1.x, 0, point1.y) - new Vector3(vertex1.x, 0, vertex1.y);
        Vector3 p2v1 = new Vector3(point2.x, 0, point2.y) - new Vector3(vertex1.x, 0, vertex1.y);

        Vector3 cp1 = Vector3.Cross(v2v1, p1v1);
        Vector3 cp2 = Vector3.Cross(v2v1, p2v1);

        return Vector3.Dot(cp1, cp2) >= 0;
    }

    private bool isPointInTriangle(Vector2 point, Vector2 tv1, Vector2 tv2, Vector2 tv3)
    {
        return isPointOnSameSide(point, tv1, tv2, tv3) && isPointOnSameSide(point, tv2, tv3, tv1) && isPointOnSameSide(point, tv3, tv1, tv2);
    }

    /// <summary>
    /// If the vertex was reflex, see if it has become convex
    /// If it is now convex, test for an ear.
    /// If the vertex has become flat (collinear), then it
    /// should be removed. Return true in this case.
    /// If the vertex was convex, it will stay convex, but
    /// we should test to see if it has become an ear.
    /// </summary>
    /// <param name="vertex">The vertex to adjust</param>
    /// <returns>Nothing</returns>
    private void FixUpVertex(int vertexIndex)
    {
        if (!convexVertexList.Contains(vertexIndex) && isConvex(vertexIndex))
        {
            reflexVertexList.Remove(vertexIndex);
            convexVertexList.Add(vertexIndex);
        }

        if (!earVertexList.Contains(vertexIndex) && isEar(vertexIndex))
        {
            reflexVertexList.Remove(vertexIndex);
            earVertexList.Add(vertexIndex);
        }

        return;
    }

    private int[] InnerTriangulate()
    {
        triangleList = new List<int>();

        // Keep clipping ears until only 3 vertices (single triangle) remain
        while (vertexIndexList.Count > 3)
        {
            // Take the first ear in the list
            int earIndex = earVertexList[0];

            int prevIndex = calculatePrevIndex(earIndex);
            int nextIndex = calculateNextIndex(earIndex);

            // Add the ear to our output triangle list
            triangleList.Add(prevIndex);
            triangleList.Add(earIndex);
            triangleList.Add(nextIndex);

            // Remove the ear from the ear list and the vertex from the vertex list
            earVertexList.RemoveAt(0);
            vertexIndexList.Remove(earIndex);

            if (vertexList.Count == 3)
            {
                // Normal early exit
                break;
            }

            // Examine the two remaining vertices.
            FixUpVertex(prevIndex);
            FixUpVertex(nextIndex);
        }

        // Add the remaining triangle
        triangleList.Add(vertexIndexList[0]);
        triangleList.Add(vertexIndexList[1]);
        triangleList.Add(vertexIndexList[2]);

        return triangleList.ToArray();
    }

    private int calculatePrevIndex(int vertexIndex)
    {
        int index = vertexIndexList.IndexOf(vertexIndex);
        int prevIndex = index == 0 ? vertexIndexList.Count - 1 : index - 1;

        return vertexIndexList[prevIndex];
    }

    private int calculateNextIndex(int vertexIndex)
    {
        int index = vertexIndexList.IndexOf(vertexIndex);
        int nextIndex = index == vertexIndexList.Count - 1 ? 0 : index + 1;

        return vertexIndexList[nextIndex];
    }
}