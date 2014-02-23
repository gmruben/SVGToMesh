using UnityEngine;
using UnityEditor;

using System.IO;

public class SVGExporter
{
    private static string dataPath = Application.streamingAssetsPath + "/";

    [MenuItem("Assets/Export SVG")]
    static void exportSVG()
    {
        //Get the selected directory
        string assetsPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (assetsPath.Length != 0)
        {
            //For each directory in the course folder, create an asset bundle
            DirectoryInfo svgFolder = new DirectoryInfo(assetsPath);
            foreach (FileInfo fileInfo in svgFolder.GetFiles("*.xml"))
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath(assetsPath + "/" + fileInfo.Name, typeof(TextAsset)) as TextAsset;

                SVGReader svgReader = new SVGReader(asset);
                svgReader.export(Selection.activeObject.name);
            }
        }
    }
}