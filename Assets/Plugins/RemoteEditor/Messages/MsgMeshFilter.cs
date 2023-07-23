using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
    public class MsgMeshFilter : MsgComponent
    {
        public int meshInstanceID;

        public MsgMeshFilter() { }

        public MsgMeshFilter(MeshFilter meshFilter)
        {
            meshInstanceID = meshFilter.sharedMesh ? meshFilter.sharedMesh.GetInstanceID() : 0;
        }

        public override void ApplyComponent(ServerSide server, Transform t, GameObject go)
        {
            if (server.remoteMeshes.TryGetValue(meshInstanceID, out Mesh mesh))
            {
                if (!go.TryGetComponent(out MeshFilter meshFilter))
                    meshFilter = go.AddComponent<MeshFilter>();

                if (!meshFilter.sharedMesh || !meshFilter.sharedMesh.name.StartsWith(meshInstanceID.ToString()))
                    meshFilter.sharedMesh = mesh;
            }
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
}
