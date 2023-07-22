using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class Message : IBinarySerializable
{
    public List<MsgGameObject> gameObjects = new List<MsgGameObject>();
    public List<MsgMesh> meshes = new List<MsgMesh>();
    public List<MsgTexture2D> textures = new List<MsgTexture2D>();

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(gameObjects);
        writer.Write(meshes);
        writer.Write(textures);
    }

    public void Deserialize(BinaryReader reader)
    {
        gameObjects = reader.ReadSerializableList<MsgGameObject>();
        meshes = reader.ReadSerializableList<MsgMesh>();
        textures = reader.ReadSerializableList<MsgTexture2D>();
    }

    [Serializable]
    public class MsgGameObject : IBinarySerializable
    {
        public string name;
        public int instanceID;
        public int parentInstanceID;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public int meshInstanceID;
        public int mainTextureInstanceID;
        public string shaderName;

        public MsgGameObject() { }

        public MsgGameObject(Transform t)
        {
            name = t.name;
            instanceID = t.GetInstanceID();
            parentInstanceID = t.parent == null ? 0 : t.parent.GetInstanceID();
            position = t.position;
            rotation = t.rotation;
            localScale = t.localScale;

            if (t.gameObject.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh)
                meshInstanceID = meshFilter.sharedMesh.GetInstanceID();
            else
                meshInstanceID = 0;

            if (t.gameObject.TryGetComponent(out MeshRenderer meshRenderer) && meshRenderer.sharedMaterial && meshRenderer.sharedMaterial.HasTexture("_MainTex") && meshRenderer.sharedMaterial.mainTexture)
            {
                mainTextureInstanceID = meshRenderer.sharedMaterial.mainTexture.GetInstanceID();
                shaderName = meshRenderer.sharedMaterial.shader.name;
            }
            else
            {
                mainTextureInstanceID = 0;
                shaderName = "";
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(name);
            writer.Write(instanceID);
            writer.Write(parentInstanceID);

            writer.Write(position);
            writer.Write(rotation);
            writer.Write(localScale);

            writer.Write(meshInstanceID);
            writer.Write(mainTextureInstanceID);
            writer.Write(shaderName);
        }

        public void Deserialize(BinaryReader reader)
        {
            name = reader.ReadString();
            instanceID = reader.ReadInt32();
            parentInstanceID = reader.ReadInt32();

            position = reader.ReadVector3();
            rotation = reader.ReadQuaternion();
            localScale = reader.ReadVector3();

            meshInstanceID = reader.ReadInt32();
            mainTextureInstanceID = reader.ReadInt32();
            shaderName = reader.ReadString();
        }
    }

    [Serializable]
    public class MsgMesh : IBinarySerializable
    {
        public string name;
        public int instanceID;

        [NonSerialized] public Vector3[] vertices;
        [NonSerialized] public int[] triangles;
        [NonSerialized] public Vector3[] normals;
        [NonSerialized] public Vector2[] uv;

        public MsgMesh() { }

        public MsgMesh(Mesh mesh)
        {
            name = mesh.name;
            instanceID = mesh.GetInstanceID();
            vertices = mesh.vertices.ToArray();
            triangles = mesh.triangles.ToArray();
            normals = mesh.normals.ToArray();
            uv = mesh.uv.ToArray();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(name);
            writer.Write(instanceID);
            writer.Write(vertices);
            writer.Write(triangles);
            writer.Write(normals);
            writer.Write(uv);
        }

        public void Deserialize(BinaryReader reader)
        {
            name = reader.ReadString();
            instanceID = reader.ReadInt32();
            vertices = reader.ReadVector3Array();
            triangles = reader.ReadInt32Array();
            normals = reader.ReadVector3Array();
            uv = reader.ReadVector2Array();
        }
    }

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