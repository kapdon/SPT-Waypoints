﻿using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Components
{
    internal class NavMeshDebugComponent : MonoBehaviour
    {
        private NavMeshTriangulation meshData;
        private static List<UnityEngine.Object> gameObjects = new List<UnityEngine.Object>();

        public void OnDisable()
        {
            Console.WriteLine("NavMeshDebugComponent::Dispose");
            gameObjects.ForEach(Destroy);
            gameObjects.Clear();
        }

        public void OnEnable()
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                Console.WriteLine("Can't create NavMeshDebug with no BotGame");
                return;
            }

            // Setup our gameObject
            gameObjects.Add(gameObject.AddComponent<MeshFilter>());
            gameObjects.Add(gameObject.AddComponent<MeshRenderer>());

            // Build a dictionary of sub areas
            meshData = NavMesh.CalculateTriangulation();
            Console.WriteLine($"NavMeshTriangulation Found. Vertices: {meshData.vertices.Length}");

            gameObject.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);

            // Adjust each vertices up by a margin so we can see the navmesh better
            Vector3[] adjustedVertices = meshData.vertices.Select(v => new Vector3(v.x, v.y + WaypointsPlugin.NavMeshOffset.Value, v.z)).ToArray();

            // Create our new mesh and add all the vertices
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = adjustedVertices;
            mesh.triangles = meshData.indices;

            // Set mesh of our gameObject
            GetComponent<MeshFilter>().mesh = mesh;

            // If dumping is enabled, dump to a JSON file
            if (WaypointsPlugin.ExportNavMesh.Value)
            {
                Directory.CreateDirectory(WaypointsPlugin.MeshFolder);

                var gameWorld = Singleton<GameWorld>.Instance;
                string mapName = gameWorld.MainPlayer.Location.ToLower();
                string meshFilename = $"{WaypointsPlugin.MeshFolder}\\{mapName}.json";
                if (!File.Exists(meshFilename))
                {
                    string jsonString = JsonConvert.SerializeObject(meshData, Formatting.Indented);
                    File.Create(meshFilename).Dispose();
                    StreamWriter streamWriter = new StreamWriter(meshFilename);
                    streamWriter.Write(jsonString);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated && WaypointsPlugin.ShowNavMesh.Value)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameObjects.Add(gameWorld.GetOrAddComponent<NavMeshDebugComponent>());
            }
        }

        public static void Disable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetComponent<NavMeshDebugComponent>()?.OnDisable();
            }
        }
    }
}
