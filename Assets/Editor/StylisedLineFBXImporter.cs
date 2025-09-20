using UnityEngine;
using UnityEditor;
using System.Collections;

class StylisedLineFBXImporter : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (assetPath.Contains("Assets/Models/fbx/") && assetPath.Contains(".fbx"))
        {
            ModelImporter modelImporter = assetImporter as ModelImporter;

            modelImporter.meshCompression = ModelImporterMeshCompression.Off;       
            modelImporter.isReadable = true;                                        
            modelImporter.optimizeMeshPolygons = true;                              
            modelImporter.optimizeMeshVertices = false;                             
            modelImporter.keepQuads = false;                                        
            modelImporter.weldVertices = false;                                     

            modelImporter.importNormals = ModelImporterNormals.Import;              
            modelImporter.importBlendShapeNormals = ModelImporterNormals.Import;    
            modelImporter.normalCalculationMode = ModelImporterNormalCalculationMode.Unweighted; 
            modelImporter.normalSmoothingSource = ModelImporterNormalSmoothingSource.None;
            modelImporter.importTangents = ModelImporterTangents.None;
            modelImporter.swapUVChannels = false;

        }
    }
}
