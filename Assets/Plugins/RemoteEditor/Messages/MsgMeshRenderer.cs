using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RemoteEditor.Messages
{
    public class MsgMeshRenderer : MsgComponent
    {
        public int mainTextureInstanceID;
        public string shaderName;

        public MsgMeshRenderer() { }

        public MsgMeshRenderer(MeshRenderer meshRenderer)
        {
            if (meshRenderer.sharedMaterial)
            {
                shaderName = meshRenderer.sharedMaterial.shader.name;
                if (meshRenderer.sharedMaterial.HasTexture("_MainTex") && meshRenderer.sharedMaterial.mainTexture)
                {
                    mainTextureInstanceID = meshRenderer.sharedMaterial.mainTexture.GetInstanceID();
                }
            }
        }

        public override void ApplyComponent(ServerSide server, Transform t, GameObject go)
        {
            if (!go.TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer = go.AddComponent<MeshRenderer>();

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                shader = Shader.Find("Diffuse");
            meshRenderer.material = new Material(shader);
            meshRenderer.sharedMaterial.name = shaderName;


            if (server.remoteTextures.TryGetValue(mainTextureInstanceID, out Texture2D tex))
            {
                meshRenderer.sharedMaterial.mainTexture = tex;
            }
            else if (meshRenderer.sharedMaterial.HasTexture("_MainTex"))
            {
                meshRenderer.sharedMaterial.mainTexture = tex;
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(nameof(MsgMeshRenderer));
            writer.Write(mainTextureInstanceID);
            writer.Write(shaderName);
        }

        public override void Deserialize(BinaryReader reader)
        {
            mainTextureInstanceID = reader.ReadInt32();
            shaderName = reader.ReadString();
        }
    }
}
