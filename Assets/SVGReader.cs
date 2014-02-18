using UnityEngine;
using UnityEditor;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

public class SVGReader : MonoBehaviour
{
    public TextAsset svgFile;

    void Start()
    {
        read();
    }

    private void read()
    {
        XmlDocument document = new XmlDocument();
        document.LoadXml(svgFile.text);

        SVG svg = new SVG();

        XmlNode svgNode = document.SelectSingleNode("svg");
        XmlNodeList groupNodeList = svgNode.SelectNodes("g");
        foreach (XmlNode groupNode in groupNodeList)
        {
            //string display = groupNode.Attributes.GetNamedItem("style").Value.Split(':')[1];
            //if (display == "inline")
            //{
                SVGGroup group = new SVGGroup();
                
                XmlNodeList pathNodeList = groupNode.SelectNodes("path");
                foreach (XmlNode pathNode in pathNodeList)
                {
                    SVGPath path = new SVGPath();

                    path.vertexList = parsePathVertexList(pathNode.Attributes.GetNamedItem("d").Value);
                    string pathID = pathNode.Attributes.GetNamedItem("id").Value;
                    string pathStyle = pathNode.Attributes.GetNamedItem("style").Value;

                    group.pathList.Add(path);
                }

                svg.groupList.Add(group);
            // }
        }

        for (int i = 0; i < svg.groupList.Count; i++)
        {
            SVGGroup group = svg.groupList[i];
            for (int j = 0; j < group.pathList.Count; j++)
            {
                Debug.Log("PATH");
                SVGPath path = group.pathList[j];
                
                Mesh mesh = createMesh(path.vertexList);

                AssetDatabase.CreateAsset(mesh, Application.dataPath + "/SVGMeshes/mesh" + j + ".asset");
                AssetDatabase.SaveAssets();

                GameObject gameObject = new GameObject("Mesh" + j);
                
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                
                meshFilter.mesh = mesh;
            }
        }
    }

    private List<Vector2> parsePathVertexList(string path)
    {
        //Path Data: M = moveto - L = lineto - H = horizontal lineto - V = vertical lineto - C = curveto - S = smooth curveto
        //Q = quadratic Bézier curve - T = smooth quadratic Bézier curveto - A = elliptical Arc - Z = closepath

        List<Vector2> vertexList = new List<Vector2>();
        
        string[] vertices = path.Split(' ');
        for (int i = 0; i < vertices.Length; i++)
        {
            string value = vertices[i];
            if (value != "m" && value != "z" && value != "L" && value != "l")
            {
                string[] vertex = value.Split(',');
                vertexList.Add(new Vector2(float.Parse(vertex[0]), float.Parse(vertex[1])));
            }
        }

        return vertexList;
    }

    private Mesh createMesh(List<Vector2> vertexList)
    {
        Vector3[] vertices = new Vector3[vertexList.Count];

        Triangulator triangulator = new Triangulator(vertexList.ToArray());
        int[] triangles = triangulator.Triangulate();

        for (int i = 0; i < vertexList.Count; i++)
        {
            vertices[i] = new Vector3(vertexList[i].x, vertexList[i].y, 0);
        }

        for (int i = 0; i < triangles.Length; ++i)
        {
            //Debug.Log(triangles[i]);
        }

        Mesh mesh = new Mesh();
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        return mesh;
    }
}

public class SVG
{
    public List<SVGGroup> groupList;

    public SVG()
    {
        groupList = new List<SVGGroup>();
    }
}

public class SVGGroup
{
    public List<SVGPath> pathList;

    public SVGGroup()
    {
        pathList = new List<SVGPath>();
    }
}

public class SVGPath
{
    public List<Vector2> vertexList;
    public Color color;

    public SVGPath()
    {
        vertexList = new List<Vector2>();
    }
}

public class Triangle
{
    public int v1Index;
    public int v2Index;
    public int v3Index;
}