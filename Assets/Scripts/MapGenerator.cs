﻿using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

[System.Serializable]
public struct TerrainType {

    public string name;
    public float height;
    public Color color;

}

public struct MapData {

    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {

        this.heightMap = heightMap;
        this.colorMap = colorMap;

    }

}

public enum DrawMode { NoiseMap, ColorMap, Mesh};

public class MapGenerator : MonoBehaviour {

    public DrawMode drawMode;

    public const int mapChunkSize = 241;
    [Range(0, 6)]
    public int simplificationLevel;

    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThraedInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThraedInfo<MapData>>();
    Queue<MapThraedInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThraedInfo<MeshData>>();

    struct MapThraedInfo<T> {

        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThraedInfo(Action<T> callback, T parameter) {

            this.callback = callback;
            this.parameter = parameter;

        }

    }

    public void DrawMapInEditor() {

        MapDisplay display = FindObjectOfType<MapDisplay>();

        MapData mapData = GenerateMapData();

        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColorMap)
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, simplificationLevel), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));

    }

    public void RequestMapData(Action<MapData> callback) {

        ThreadStart threadStart = delegate {
            MapDataThread(callback);
        };

        new Thread(threadStart).Start(); 

    }

    void MapDataThread(Action<MapData> callback) {

        MapData mapData = GenerateMapData();

        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThraedInfo<MapData>(callback, mapData));
        }

    }

    public void RequestMeshData(MapData mapData, Action<MeshData> callback) {

        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, callback);
        };

        new Thread(threadStart).Start();

    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback) {

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, simplificationLevel);

        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThraedInfo<MeshData>(callback, meshData));
        }

    }

    void Update() {

        if (mapDataThreadInfoQueue.Count > 0) 
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {

                MapThraedInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();

                threadInfo.callback(threadInfo.parameter);

            }

        if (meshDataThreadInfoQueue.Count > 0)
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {

                MapThraedInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();

                threadInfo.callback(threadInfo.parameter);

            }

    }

    MapData GenerateMapData() {

        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
            for (int x = 0; x < mapChunkSize; x++) {

                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height) {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }

            }

        return new MapData(noiseMap, colorMap);
       
    }

    void OnValidate() {

        if (octaves < 0)
            octaves = 0;

        if (lacunarity < 1)
            lacunarity = 1;

    }
	
}