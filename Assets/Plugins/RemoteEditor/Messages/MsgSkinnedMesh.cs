using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
    public class MsgSkinnedMesh : MsgComponent
    {
        public int mainTextureInstanceID;
        public int rootBoneInstanceID;
        public string shaderName;

        public override void ApplyComponent(ServerSide server, Transform t, GameObject go)
        {
            if (!go.TryGetComponent(out SkinnedMeshRenderer skinnedMesh))
                skinnedMesh = go.AddComponent<SkinnedMeshRenderer>();

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                shader = Shader.Find("Diffuse");
            skinnedMesh.material = new Material(shader);
            skinnedMesh.sharedMaterial.name = shaderName;

            if (server.remoteTextures.TryGetValue(mainTextureInstanceID, out Texture2D tex))
            {
                skinnedMesh.sharedMaterial.mainTexture = tex;
            }
            else if (skinnedMesh.sharedMaterial.HasTexture("_MainTex"))
            {
                skinnedMesh.sharedMaterial.mainTexture = tex;
            }

            if (rootBoneInstanceID != 0 && server.remoteObjects.ContainsKey(rootBoneInstanceID))
                skinnedMesh.rootBone = server.remoteObjects[rootBoneInstanceID];
            else
                skinnedMesh.rootBone = null;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(nameof(MsgSkinnedMesh));
            writer.Write(mainTextureInstanceID);
            writer.Write(rootBoneInstanceID);
            writer.Write(shaderName);
        }

        public override void Deserialize(BinaryReader reader)
        {
            mainTextureInstanceID = reader.ReadInt32();
            rootBoneInstanceID = reader.ReadInt32();
            shaderName = reader.ReadString();
        }
    }
}
