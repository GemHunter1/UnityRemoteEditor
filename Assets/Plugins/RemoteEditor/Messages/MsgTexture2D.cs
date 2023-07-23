using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{

    public class MsgTexture2D : IBinarySerializable
    {
        public string name;
        public int instanceID;
        public int width;
        public int height;
        public TextureFormat format;
        public bool alphaIsTransparency;
        public int anisoLevel;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;

        public NativeArray<byte> rawTextureData;

        public MsgTexture2D() { }

        public MsgTexture2D(Texture2D tex)
        {
            name = tex.name;
            instanceID = tex.GetInstanceID();
            width = tex.width;
            height = tex.height;
            format = tex.format;
            alphaIsTransparency = tex.alphaIsTransparency;
            anisoLevel = tex.anisoLevel;
            filterMode = tex.filterMode;
            wrapMode = tex.wrapMode;

            if (tex.isReadable)
                rawTextureData = tex.GetRawTextureData<byte>();
            else
                Debug.LogWarning($"Texture {tex.name} is not readable!", tex);
        }

        public Texture2D Create()
        {
            if (rawTextureData.Length == 0)
                return FindInProject();

            Texture2D tex = new Texture2D(width, height, format, false);
            tex.name = instanceID + " " + name;
            tex.alphaIsTransparency = alphaIsTransparency;
            tex.anisoLevel = anisoLevel;
            tex.wrapMode = wrapMode;
            tex.filterMode = filterMode;

            tex.LoadRawTextureData(rawTextureData);
            tex.Apply();

            return tex;
        }

        public Texture2D FindInProject()
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:texture2D " + name);
            if (guids.Length > 0)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids.First());
                if (guids.Length > 1)
                {
                    int sameCount = 0;
                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (Path.GetFileNameWithoutExtension(path) == name)
                        {
                            if (sameCount == 0)
                                assetPath = path;
                            sameCount++;
                        }
                    }

                    if (sameCount > 1)
                    {
                        Debug.LogWarning("Multiple textures found with same name: " + name);
                    }
                    else if (sameCount == 0)
                    {
                        Debug.LogError("Multiple textures found but none of them actually match the name: " + name);
                    }
                }
                else if (Path.GetFileNameWithoutExtension(assetPath) != name)
                {
                    Debug.LogError("Texture found but it doesn't actually match the name: " + name);
                }

                Texture2D tex = (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
                return tex;
            }
#endif
            return null;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(name);
            writer.Write(instanceID);
            writer.Write(width);
            writer.Write(height);
            writer.Write((int)format);
            writer.Write(alphaIsTransparency);
            writer.Write(anisoLevel);
            writer.Write((int)filterMode);
            writer.Write((int)wrapMode);

            writer.Write(rawTextureData);
        }

        public void Deserialize(BinaryReader reader)
        {
            name = reader.ReadString();
            instanceID = reader.ReadInt32();
            width = reader.ReadInt32();
            height = reader.ReadInt32();
            format = (TextureFormat)reader.ReadInt32();
            alphaIsTransparency = reader.ReadBoolean();
            anisoLevel = reader.ReadInt32();
            filterMode = (FilterMode)reader.ReadInt32();
            wrapMode = (TextureWrapMode)reader.ReadInt32();

            rawTextureData = reader.ReadNativeByteArray();
        }
    }
}
