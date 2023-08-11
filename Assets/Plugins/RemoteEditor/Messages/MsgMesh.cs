using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
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
            if (mesh.isReadable)
            {
                vertices = mesh.vertices.ToArray();
                triangles = mesh.triangles.ToArray();
                normals = mesh.normals.ToArray();
                uv = mesh.uv.ToArray();
            }
        }

        public Mesh Create()
        {
            Mesh mesh = new Mesh();
            mesh.name = instanceID + " " + name;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;

            return mesh;
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
}
