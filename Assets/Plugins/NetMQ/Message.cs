using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public bool activeSelf;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public List<MsgComponent> components = new List<MsgComponent>();

        public MsgGameObject() { }

        public MsgGameObject(Transform t)
        {
            name = t.name;
            instanceID = t.GetInstanceID();
            parentInstanceID = t.parent == null ? 0 : t.parent.GetInstanceID();

            activeSelf = t.gameObject.activeSelf;

            position = t.position;
            rotation = t.rotation;
            localScale = t.localScale;

            if (t.gameObject.TryGetComponent(out MeshFilter meshFilter))
                components.Add(new MsgMeshFilter(meshFilter));

            if (t.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
                components.Add(new MsgMeshRenderer(meshRenderer));
        }

        public T GetComponent<T>() where T : MsgComponent
        {
            return (T)components.FirstOrDefault(x => x is T);
        }

        public bool TryGetComponent<T>(out T component) where T : MsgComponent, new()
        {
            component = (T)components.FirstOrDefault(x => x is T);
            return component != null;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(name);
            writer.Write(instanceID);
            writer.Write(parentInstanceID);

            writer.Write(activeSelf);

            writer.Write(position);
            writer.Write(rotation);
            writer.Write(localScale);

            writer.Write(components);
        }

        public void Deserialize(BinaryReader reader)
        {
            name = reader.ReadString();
            instanceID = reader.ReadInt32();
            parentInstanceID = reader.ReadInt32();

            activeSelf = reader.ReadBoolean();

            position = reader.ReadVector3();
            rotation = reader.ReadQuaternion();
            localScale = reader.ReadVector3();

            components = reader.ReadSerializableList(MsgComponent.Deserializer);
        }
    }

    [Serializable]
    public abstract class MsgComponent : IBinarySerializable
    {
        public MsgComponent() { }

        public static MsgComponent Deserializer(BinaryReader reader)
        {
            string type = reader.ReadString();
            MsgComponent msgComp;
            switch(type)
            {
                case nameof(MsgMeshFilter): 
                    msgComp = new MsgMeshFilter();
                    break;
                case nameof(MsgMeshRenderer): 
                    msgComp = new MsgMeshRenderer(); 
                    break;
                case nameof(MsgSkinnedMesh): 
                    msgComp = new MsgSkinnedMesh(); 
                    break;
                default:
                    throw new Exception("Unknown component type: " + type);
            }
            msgComp.Deserialize(reader);
            return msgComp;
        }

        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);
    }

    public class MsgMeshFilter : MsgComponent
    {
        public int meshInstanceID;

        public MsgMeshFilter() { }

        public MsgMeshFilter(MeshFilter meshFilter) 
        {
            meshInstanceID = meshFilter.sharedMesh ? meshFilter.sharedMesh.GetInstanceID() : 0;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(nameof(MsgMeshFilter));
            writer.Write(meshInstanceID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            meshInstanceID = reader.ReadInt32();
        }
    }

    public class MsgMeshRenderer : MsgComponent
    {
        public int mainTextureInstanceID;
        public string shader;

        public MsgMeshRenderer()
        {

        }

        public MsgMeshRenderer(MeshRenderer meshRenderer)
        {
            if (meshRenderer.sharedMaterial)
            {
                shader = meshRenderer.sharedMaterial.shader.name;
                if (meshRenderer.sharedMaterial.HasTexture("_MainTex") && meshRenderer.sharedMaterial.mainTexture)
                {
                    mainTextureInstanceID = meshRenderer.sharedMaterial.mainTexture.GetInstanceID();
                }
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(nameof(MsgMeshRenderer));
            writer.Write(mainTextureInstanceID);
            writer.Write(shader);
        }

        public override void Deserialize(BinaryReader reader)
        {
            mainTextureInstanceID = reader.ReadInt32();
            shader = reader.ReadString();
        }
    }

    public class MsgSkinnedMesh : MsgComponent
    {
        public int mainTextureInstanceID;
        public int rootBoneInstanceID;
        public string shader;

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(nameof(MsgSkinnedMesh));
            writer.Write(mainTextureInstanceID);
            writer.Write(rootBoneInstanceID);
            writer.Write(shader);
        }

        public override void Deserialize(BinaryReader reader)
        {
            mainTextureInstanceID = reader.ReadInt32();
            rootBoneInstanceID = reader.ReadInt32();
            shader = reader.ReadString();
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