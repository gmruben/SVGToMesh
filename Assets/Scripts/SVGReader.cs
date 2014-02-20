using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

public class SVGReader : MonoBehaviour
{
    public TextAsset svgFile;
    public Material svgMaterial;

    private float width;
    private float height;

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

        width = float.Parse(svgNode.Attributes.GetNamedItem("width").Value);
        height = float.Parse(svgNode.Attributes.GetNamedItem("height").Value);

        XmlNodeList groupNodeList = svgNode.SelectNodes("g");
        foreach (XmlNode groupNode in groupNodeList)
        {
            string display = groupNode.Attributes.GetNamedItem("style").Value.Split(':')[1];
            if (display != null && display == "inline")
            {
                SVGGroup group = new SVGGroup();
                
                XmlNodeList pathNodeList = groupNode.SelectNodes("path");
                foreach (XmlNode pathNode in pathNodeList)
                {
                    SVGPath svgPath = new SVGPath();

                    string path = pathNode.Attributes.GetNamedItem("d").Value;
                    string id = pathNode.Attributes.GetNamedItem("id").Value;
                    string style = pathNode.Attributes.GetNamedItem("style").Value;

                    svgPath.vertexList = parsePathVertexList(path);
                    svgPath.color = parsePathColor(style);

                    group.pathList.Add(svgPath);
                }

                svg.groupList.Add(group);
            }
        }

        for (int i = 0; i < svg.groupList.Count; i++)
        {
            SVGGroup group = svg.groupList[i];
            for (int j = 0; j < group.pathList.Count; j++)
            {
                SVGPath path = group.pathList[j];
                
                Mesh mesh = createMesh(path.vertexList, path.color);

                string assetpath = "Assets/SVGMeshes";
                AssetDatabase.CreateAsset(mesh, assetpath + "/" + svgFile.name + "_" + j + ".asset");
                AssetDatabase.SaveAssets();

                GameObject gameObject = new GameObject(svgFile.name + "_" + j);
                
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                
                meshFilter.mesh = mesh;
                meshRenderer.sharedMaterial = svgMaterial;
                meshRenderer.castShadows = false;
                meshRenderer.receiveShadows = false;

                //Create Hole Cup Prefab
                string svgpath = "Assets/SVG";
                if (!Directory.Exists(svgpath)) Directory.CreateDirectory(svgpath);
                PrefabUtility.CreatePrefab(svgpath + "/" + svgFile.name + ".prefab", gameObject);
            }
        }
    }

    private List<Vector2> parsePathVertexList(string path)
    {
        //Path Data: M = moveto - L = lineto - H = horizontal lineto - V = vertical lineto - C = curveto - S = smooth curveto
        //Q = quadratic Bézier curve - T = smooth quadratic Bézier curveto - A = elliptical Arc - Z = closepath

        int index = 0;
        List<Vector2> vertexList = new List<Vector2>();
        Vector2 lastVector = new Vector2(0, height);
        bool isAbsolute = false;
        
        string[] vertices = path.Split(' ');
        for (int i = 0; i < vertices.Length; i++)
        {           
            string value = vertices[i];
            if (value == "m" || value == "l")
            {
                isAbsolute = false;
            }
            else if (value == "M" || value == "L" )
            {
                isAbsolute = true;
            }
            else if (value != "z" && value != "Z")
            {
                string[] vertex = value.Split(',');

                float x;
                float y;

                if (isAbsolute)
                {
                    x = float.Parse(vertex[0]);
                    y = -(float.Parse(vertex[1]) - height);
                }
                else
                {
                    x = lastVector.x + float.Parse(vertex[0]);
                    y = lastVector.y - float.Parse(vertex[1]);
                }

                lastVector = new Vector2(x, y);
                vertexList.Add(lastVector);

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                quad.name = index.ToString();
                quad.transform.localScale = Vector3.one * 2.5f;
                quad.transform.position = new Vector3(lastVector.x, lastVector.y, 0);

                index++;
            }
        }

        return vertexList;
    }

    private Color parsePathColor(string style)
    {
        string hexcolor = style.Split(';')[0].Split(':')[1];
        Color color = HexToColor(hexcolor);

        return color;
    }

    private Mesh createMesh(List<Vector2> vertexList, Color color)
    {
        Vector3[] vertices = new Vector3[vertexList.Count];
        Vector2[] uvs = new Vector2[vertexList.Count];
        Color[] colors = new Color[vertexList.Count];

        Triangulator triangulator = new Triangulator(width, height, vertexList);
        int[] triangles = triangulator.Triangulate();

        for (int i = 0; i < vertexList.Count; i++)
        {
            vertices[i] = new Vector3(vertexList[i].x, vertexList[i].y, 0);
            uvs[i] = new Vector2(vertexList[i].x, vertexList[i].y);
            colors[i] = color;
        }

        Mesh mesh = new Mesh();
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        return mesh;
    }

    public static string ColorToHex(Color color)
    {
        string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
        return hex;
    }

    public static Color HexToColor(string hex)
    {
        byte r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, 255);
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

public class Vertex
{
    public float x;
    public float y;
    public Color color;
}