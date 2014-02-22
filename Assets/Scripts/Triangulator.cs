using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Triangulator
{
    private List<Vector2> vertices;

    private List<Vector2> vertexList;
    private List<int> triangleList;

    private List<int> vertexIndexList;
    private List<int> earVertexList;
    private List<int> convexVertexList;
    private List<int> reflexVertexList;

    private bool isClockwise = true;

    public Triangulator(float width, float height, List<Vector2> vertices)
    {
        this.vertices = vertices;

        vertexList = vertices;
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector2 vertex = new Vector2(vertexList[i].x - width / 2, vertexList[i].y - height / 2);
            vertexList[i] = new Vector2(vertex.x, vertex.y);
        }

        vertexIndexList = new List<int>();
        for (int i = 0; i < vertices.Count; i++)
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


        isClockwise = !(convexVertexList.Count > reflexVertexList.Count);

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
        return !isReflex(vertexIndex);
    }

    private bool isReflex(int vertexIndex)
    {
        Vector2 v0 = vertexList[calculatePrevIndex(vertexIndex)];
        Vector2 v1 = vertexList[vertexIndex];
        Vector2 v2 = vertexList[calculateNextIndex(vertexIndex)];

        Vector2 v1v0 = v1 - v0;
        Vector2 v1v2 = v1 - v2;

        if (!isClockwise)
        {
            return angleSign(v1v0, v1v2) > 0;
        }
        else
        {
            return angleSign(v1v0, v1v2) < 0;
        }
    }

    private float angleSign(Vector2 v1, Vector2 v2)
    {
        // the vector that we want to measure an angle from
        Vector3 referenceForward = new Vector3(v1.x, 0, v1.y);
 
        // the vector perpendicular to referenceForward (90 degrees clockwise)
        // (used to determine if angle is positive or negative)
        Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
 
        // the vector of interest
        Vector3 newDirection = new Vector3(v2.x, 0, v2.y);
 
        // Get the angle in degrees between 0 and 180
        float angle = Vector3.Angle(newDirection, referenceForward);
 
        // Determine if the degree value should be negative.  Here, a positive value
        // from the dot product means that our vector is on the right of the reference vector   
        // whereas a negative value means we're on the left.
        float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
 
        float finalAngle = sign * angle;
        return finalAngle;
    }

    private bool isEar(int vertexIndex)
    {
        Vector2 v0 = vertexList[calculatePrevIndex(vertexIndex)];
        Vector2 v1 = vertexList[vertexIndex];
        Vector2 v2 = vertexList[calculateNextIndex(vertexIndex)];

        foreach (Vector2 vertex in vertexList)
        {
            if (vertex != v0 && vertex != v1 && vertex != v2)
            {
                if (isPointInTriangle(vertex, v0, v1, v2))
                {
                    return false;
                }
            }
        }

        return isReflex(vertexIndex);
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

        
        if (earVertexList.Contains(vertexIndex) && !isEar(vertexIndex))
        {
            if (isConvex(vertexIndex)) convexVertexList.Add(vertexIndex);
            else if (isReflex(vertexIndex)) reflexVertexList.Add(vertexIndex);

            earVertexList.Remove(vertexIndex);
        }

        return;
    }

    private int[] InnerTriangulate()
    {
        triangleList = new List<int>();

        // Keep clipping ears until only 3 vertices (single triangle) remain
        while (vertexIndexList.Count > 3)
        {
            if (earVertexList.Count == 0) break;

            // Take the first ear in the list
            int earIndex = earVertexList[0];

            int prevIndex = calculatePrevIndex(earIndex);
            int nextIndex = calculateNextIndex(earIndex);

            // Add the ear to our output triangle list
            if (isClockwise)
            {
                triangleList.Add(prevIndex);
                triangleList.Add(earIndex);
                triangleList.Add(nextIndex);
            }
            else
            { 
                triangleList.Add(nextIndex);
                triangleList.Add(earIndex);
                triangleList.Add(prevIndex);
            }

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
        if (isClockwise)
        {
            triangleList.Add(vertexIndexList[0]);
            triangleList.Add(vertexIndexList[1]);
            triangleList.Add(vertexIndexList[2]);
        }
        else
        {
            triangleList.Add(vertexIndexList[2]);
            triangleList.Add(vertexIndexList[1]);
            triangleList.Add(vertexIndexList[0]);
        }

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

    private void printVertexInfo()
    {
        string ears = "";
        foreach (int index in earVertexList)
        {
            ears += index + " - ";
        }
        Debug.Log("EARS: " + ears);
        string convex = "";
        foreach (int index in convexVertexList)
        {
            convex += index + " - ";
        }
        Debug.Log("CONVEX: " + convex);
    }
}