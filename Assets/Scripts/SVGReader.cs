using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

public class SVGReader
{
    private TextAsset svgFile;
    private Material svgColorMaterial;
    private Material svgAlphaMaterial;

    private float width;
    private float height;

    public SVGReader(TextAsset svgFile)
    {
        this.svgFile = svgFile;

        svgColorMaterial = Resources.LoadAssetAtPath("Assets/Materials/VertexColor_Material.mat", typeof(UnityEngine.Object)) as Material;
        svgAlphaMaterial = Resources.LoadAssetAtPath("Assets/Materials/VertexColorAlpha_Material.mat", typeof(UnityEngine.Object)) as Material;
        Debug.Log(svgAlphaMaterial);
        //meshRenderer.sharedMaterial = Resources.LoadAssetAtPath("Assets/BaseMaterials/HoleCup_Material.mat", typeof(UnityEngine.Object)) as Material;
    }

    public void export(string name)
    {
        NameTable nameTable = new NameTable();
        XmlNamespaceManager nameSpaceManager = new XmlNamespaceManager(nameTable);

        //Add namespaces
        nameSpaceManager.AddNamespace("svg", "http://www.w3.org/2000/svg");
        nameSpaceManager.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        nameSpaceManager.AddNamespace("cc", "http://creativecommons.org/ns#");
        nameSpaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
        
        XmlParserContext parserContext = new XmlParserContext(null, nameSpaceManager, null, XmlSpace.None);
        XmlTextReader txtReader = new XmlTextReader(svgFile.text, XmlNodeType.Document, parserContext);

        XmlDocument document = new XmlDocument();
        document.Load(txtReader);

        SVG svg = new SVG();

        XmlNode svgNode = document.SelectSingleNode("svg:svg", nameSpaceManager);

        width = float.Parse(svgNode.Attributes.GetNamedItem("width").Value);
        height = float.Parse(svgNode.Attributes.GetNamedItem("height").Value);
        
        XmlNodeList groupNodeList = svgNode.SelectNodes("svg:g", nameSpaceManager);
        foreach (XmlNode groupNode in groupNodeList)
        {
            string display = null;
            XmlNode styleNode = groupNode.Attributes.GetNamedItem("style");
            if (styleNode != null) display = styleNode.Value.Split(':')[1];
            if (styleNode == null || (styleNode != null && display == "inline"))
            {
                SVGGroup group = new SVGGroup();

                XmlNodeList pathNodeList = groupNode.SelectNodes("svg:path", nameSpaceManager);
                foreach (XmlNode pathNode in pathNodeList)
                {
                    SVGPath svgPath = new SVGPath();

                    string path = pathNode.Attributes.GetNamedItem("d").Value;
                    string id = pathNode.Attributes.GetNamedItem("id").Value;
                    string style = pathNode.Attributes.GetNamedItem("style").Value;

                    svgPath.id = id;
                    svgPath.vertexList = parsePathVertexList(path);
                    svgPath.color = parsePathColor(style);

                    group.pathList.Add(svgPath);
                }

                svg.groupList.Add(group);
            }
        }

        GameObject obj = new GameObject(svgFile.name);

        int count = 0;
        for (int i = 0; i < svg.groupList.Count; i++)
        {
            SVGGroup group = svg.groupList[i];
            for (int j = 0; j < group.pathList.Count; j++)
            {
                SVGPath path = group.pathList[j];

                Mesh mesh = createMesh(path.vertexList, path.color);

                string assetpath = "Assets/SVGMeshes";
                if (!Directory.Exists(assetpath)) Directory.CreateDirectory(assetpath);
                assetpath += "/" + name;
                if (!Directory.Exists(assetpath)) Directory.CreateDirectory(assetpath);
                assetpath += "/" + svgFile.name;
                if (!Directory.Exists(assetpath)) Directory.CreateDirectory(assetpath);

                AssetDatabase.CreateAsset(mesh, assetpath + "/" + svgFile.name + "_" + path.id + ".asset");
                AssetDatabase.SaveAssets();

                GameObject gameObject = new GameObject(path.id);
                gameObject.transform.position = new Vector3(0, 0, -(count) * 0.025f - j * 0.025f);
                gameObject.transform.parent = obj.transform;
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

                meshFilter.mesh = mesh;
                meshRenderer.castShadows = false;
                meshRenderer.receiveShadows = false;

                if (path.color.a == 1)
                {
                    meshRenderer.sharedMaterial = svgColorMaterial;
                }
                else
                {
                    meshRenderer.sharedMaterial = svgAlphaMaterial;
                }
            }
            count += group.pathList.Count;
        }

        //Create Hole Cup Prefab
        string svgpath = "Assets/SVGPrefabs";
        if (!Directory.Exists(svgpath)) Directory.CreateDirectory(svgpath);
        svgpath += "/" + name;
        if (!Directory.Exists(svgpath)) Directory.CreateDirectory(svgpath);

        PrefabUtility.CreatePrefab(svgpath + "/" + svgFile.name + ".prefab", obj);
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
            else if (value == "M" || value == "L")
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

                index++;
            }
        }

        return vertexList;
    }

    private Color parsePathColor(string style)
    {
        Color color = Color.black;

        string[] properties = style.Split(';');
        for (int i = 0; i < properties.Length; i++)
        {
            string name = properties[i].Split(':')[0];
            if (name == "fill")
            {
                string hexcolor = properties[i].Split(':')[1];
                color = HexToColor(hexcolor);
            }
            else if (name == "fill-opacity")
            {
                float alpha = float.Parse(properties[i].Split(':')[1]);
                color.a = alpha;
            }
        }

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
        Color32 color;

        if (hex == "none")
        {
            color = new Color(0, 0, 0, 255);
        }
        else
        {
            byte r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);

            color = new Color32(r, g, b, 255);
        }

        return color;
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
    public string id;
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