﻿using System.Linq;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

public class ModLoader : MonoBehaviour {

    public static ModLoader instance;

    public string[] Mods;
    private string ModsFolder;
    private string GameFolder;
    private Transform ImportHolder;
    private Transform PrefabHolder;

    public GameObject defaultModel;
    public GameObject defaultSprite;
    public static Dictionary<string, Material> Materials;
    public static Dictionary<string, GameObject> Models;
    public static Dictionary<string, GameObject> Sprites;
    public static Dictionary<string, AssetInteraction> AssetInteractions;
    public static Dictionary<string, TileAsset> TileAssets;

    void Awake()
    {
        instance = this;

        ModsFolder = Directory.GetParent(Application.dataPath) + "/Mods/";
        GameFolder = Directory.GetParent(Application.dataPath) + "/Game/";

        GameObject ImportHolderGO = new GameObject("ImportHolder");
        ImportHolderGO.transform.SetParent(this.transform);
        ImportHolder = ImportHolderGO.transform;
        ImportHolderGO.SetActive(false);
        GameObject PrefabHolderGO = new GameObject("PrefabHolder");
        PrefabHolderGO.transform.SetParent(this.transform);
        PrefabHolder = PrefabHolderGO.transform;
        PrefabHolderGO.SetActive(false);
    }

    public void LoadMods()
    {
        AssetInteractions = new Dictionary<string, AssetInteraction>();
        Sprites = new Dictionary<string, GameObject>();
        Models = new Dictionary<string, GameObject>();
        TileAssets = new Dictionary<string, TileAsset>();
        Materials = new Dictionary<string, Material>();

        foreach (string Mod in Mods)
        {
            string assetsPath = Path.Combine(Path.Combine(ModsFolder, Mod), Mod.ToLower() + "_models");
            if (File.Exists(assetsPath))
            {
                AssetBundle assets = AssetBundle.LoadFromFile(assetsPath);
                GameObject[] prefabs = assets.LoadAllAssets<GameObject>();
                ImportModels(prefabs);
            }
                //ImportMaterials(ModsFolder + Mod);
            ImportSprites(ModsFolder + Mod);
            ImportAssetInteractions(ModsFolder + Mod);
            ImportTileAssets(ModsFolder + Mod);
        }
    }

    private void ImportTileAssets(string ModPath)
    {
        //Find files in the dir
        DirectoryInfo dir = new DirectoryInfo(ModPath + "/TileAssets");
        FileInfo[] files = dir.GetFiles("*.asset.json", SearchOption.AllDirectories);
        //for every file in that dir parse json :D
        foreach (FileInfo file in files)
        {
            //JSON
            string json = File.ReadAllText(file.FullName);
            JToken jObject = JToken.Parse(json);
            //Name
            string name = "New TileAsset"; ///DEFAULT VALUE
            if (jObject["Name"] != null) name = jObject["Name"].ToObject<string>(); ///IF JSON CONTAINS CHANGE VALUE
            //Model
            GameObject model = null;
            string modelName = "UNDEFINED";
            if (jObject["Model"] != null) modelName = jObject["Model"].ToObject<string>();
            if (modelName != "UNDEFINED") Models.TryGetValue(modelName, out model);
            if (model == null) model = defaultModel;
            //Create 'prefab'
            ///Create TileAsset with model copy as child
            GameObject TileAsset = new GameObject(name);
            TileAsset.transform.SetParent(PrefabHolder);
            GameObject assetModel = Instantiate(model, TileAsset.transform);
            assetModel.name = "Model";
            assetModel.transform.position = new Vector3(0f, 0f, .3f);
            //Chance
            int chance = 0;
            if(jObject["SpawnChance"] != null) chance = jObject["SpawnChance"].ToObject<int>();
            //SizeRange
            float[] ranges = new float[] { 1, 1 };
            if (jObject["SizeRange"] != null) ranges = jObject["SizeRange"].ToObject<float[]>();
            Vector2 sizeRange = new Vector2(ranges[0], ranges[1]);
            //AssetInteractions
            List<string> interactions = new List<string>();
            if (jObject["Interactions"] != null) interactions = jObject["Interactions"].ToObject<List<string>>();
            List<AssetInteraction> assetInteractions = new List<AssetInteraction>();
            foreach (string interaction in interactions)
            {
                AssetInteraction _Interaction;
                if (AssetInteractions.TryGetValue(interaction, out _Interaction))
                    assetInteractions.Add(_Interaction);
            }

            //Create TileAsset
            TileAsset ta = TileAsset.AddComponent<TileAsset>();
            ta.Setup(name, TileAsset, chance, sizeRange, assetInteractions);
            TileAssets.Add(name, ta);
        }
    }

    private void ImportAssetInteractions(string ModPath)
    {
        DirectoryInfo dir = new DirectoryInfo(ModPath + "/AssetInteractions");
        FileInfo[] files = dir.GetFiles("*.json", SearchOption.AllDirectories);

        foreach (FileInfo file in files)
        {
            //JSON
            string json = File.ReadAllText(file.FullName);
            JToken jObject = JToken.Parse(json);
            //Name
            string name = "New AssetInteration";
            if (jObject["Name"] != null) name = jObject["Name"].ToObject<string>();
            //Sprite
            GameObject sprite = null;
            string spriteName = "UNDEFINED";
            if (jObject["Sprite"] != null) spriteName = jObject["Sprite"].ToObject<string>();
            if (spriteName != "UNDEFINED") Sprites.TryGetValue(spriteName, out sprite);
            if (sprite == null) sprite = defaultSprite;
            //Create 'prefab'
            ///Create AssetInteraction with sprite copy as child
            GameObject prefab = new GameObject(name);
            prefab.transform.SetParent(PrefabHolder);
            GameObject assetSprite = Instantiate(sprite, prefab.transform);
            assetSprite.name = "Sprite";
            assetSprite.transform.position = new Vector3(0f, 0f, 0f);
            SpriteRenderer renderer = assetSprite.GetComponentInChildren<SpriteRenderer>();
            renderer.sortingLayerName = "Popups";
            renderer.sortingOrder = 1;
            renderer.gameObject.layer = 5;
            //Create AssetInteraction
            AssetInteraction AI = new AssetInteraction(name, prefab);
            AssetInteractions.Add(name, AI);
        }
    }

    private void ImportSprites(string ModPath)
    {
        DirectoryInfo dir = new DirectoryInfo(ModPath + "/Sprites");
        FileInfo[] files = dir.GetFiles("*.png", SearchOption.AllDirectories);

        foreach (FileInfo file in files)
        {
            string spriteName = file.FullName.Replace(@"\", "/").Remove(0, (ModPath + "/Sprites/").Count());
            if (!Sprites.ContainsKey(spriteName))
            {
                byte[] fileData = File.ReadAllBytes(file.FullName);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);

                Sprite spr = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                spr.name = spriteName;

                GameObject sprite = new GameObject("Sprite");
                sprite.transform.SetParent(ImportHolder);
                SpriteRenderer sr = sprite.AddComponent<SpriteRenderer>();
                sr.sprite = spr;

                Sprites.Add(spriteName, sprite);
            }
        }
    }

    private void ImportModels(GameObject[] prefabs)
    {
        foreach(GameObject model in prefabs)
        {
            if (!Models.ContainsKey(model.name))
            {
                //For some reason you have to update the shader to work correctly.
                List<Renderer> renderers = model.GetComponentsInChildren<Renderer>().ToList();
                renderers.AddRange(model.GetComponents<Renderer>().ToList());
                foreach(Renderer renderer in renderers)
                {
                    Material[] mats = renderer.sharedMaterials;
                    foreach(Material mat in mats)
                    {
                        string shader = mat.shader.name;
                        mat.shader = Shader.Find("Standard (Specular Setup)"); //doesnt matter which one
                        mat.shader = Shader.Find(shader);
                    }
                }
                Models.Add(model.name, model);
            }
        }

        /*DirectoryInfo dir = new DirectoryInfo(ModPath + "/Models");
        FileInfo[] files = dir.GetFiles("*.obj", SearchOption.AllDirectories);

        foreach (FileInfo file in files)
        {
            GameObject model = OBJLoader.LoadOBJFile(file.FullName);
            model.transform.SetParent(ImportHolder);
            foreach (Transform child in model.transform) {
                MeshCollider coll = child.gameObject.AddComponent<MeshCollider>();
                coll.convex = true;
            }
            string modelname = file.FullName.Replace(@"\", "/").Remove(0, (ModPath + "/Models/").Count());
            Models.Add(modelname, model);
        }*/
    }

    /*private void ImportMaterials(string ModPath)
    {

        DirectoryInfo dir = new DirectoryInfo(ModPath + "/Materials");
        FileInfo[] files = dir.GetFiles("*.json", SearchOption.AllDirectories);

        foreach (FileInfo file in files)
        {
            //JSON
            string json = File.ReadAllText(file.FullName);
            JToken jObject = JToken.Parse(json);
            //Shader
            string shader = "Standard";
            if (jObject["Shader"] != null) shader = jObject["Shader"].ToObject<string>();
            Material material = new Material(Shader.Find(shader));
            //Color
            float[] gammaColor = new float[4];
            if (jObject["Color"] != null) gammaColor = jObject["Color"].ToObject<float[]>();
            
            double gamma = 1 / 2.2;
            float[] linearColor = new float[4];
            //You might want to not use "Math.Pow" as it is slow compared to multiply operator
            linearColor[0] = (float)Math.Pow(gammaColor[0], gamma);
            linearColor[1] = (float)Math.Pow(gammaColor[1], gamma);
            linearColor[2] = (float)Math.Pow(gammaColor[2], gamma);
            
            material.color = new Color(linearColor[0], linearColor[1], linearColor[2], linearColor[3]);

            //Name
            string name = "New Material";
            if (jObject["Name"] != null) name = jObject["Name"].ToObject<string>();
            else name = name + Materials.Count;
            material.name = name;

            //End
            Materials.Add(name, material);
        }
    }*/
}
