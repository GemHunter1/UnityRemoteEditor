using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ServerSide : MonoBehaviour
{
    private RouterSocket server;
    internal RemoteEditor remoteEditor;

    private ConcurrentQueue<byte[]> queue = new ConcurrentQueue<byte[]>();

    private Dictionary<int, Transform> remoteObjects = new Dictionary<int, Transform>();
    private Dictionary<int, Mesh> remoteMeshes = new Dictionary<int, Mesh>();
    private Dictionary<int, Texture2D> remoteTextures = new Dictionary<int, Texture2D>();

    internal async Task ServerAsync()
    {
        try
        {
            HashSet<RoutingKey> clients = new HashSet<RoutingKey>();

            using (server = new RouterSocket("tcp://127.0.0.1:5556"))
            {
                while (remoteEditor.isRunning)
                {
                    var (routingKey, more) = await server.ReceiveRoutingKeyAsync();

                    if (!clients.Contains(routingKey))
                    {
                        clients.Add(routingKey);
                        var (message, _) = await server.ReceiveFrameStringAsync();
                        if (message == RemoteEditor.HelloMessage)
                        {
                            Debug.Log($"Received from {routingKey} frame : {message}");
                            server.SendMoreFrame(routingKey);
                            server.SendFrame(RemoteEditor.WelcomeMessage);
                        }
                    }
                    else
                    {
                        var (message, _) = await server.ReceiveFrameBytesAsync();
                        queue.Enqueue(message);
                    }
                }

                //for (int i = 0; i < 1000; i++)
                //{
                //    var (routingKey, more) = await server.ReceiveRoutingKeyAsync();
                //    var (message, _) = await server.ReceiveFrameStringAsync();

                //    // TODO: process message
                //    Debug.Log("Server received: " + message);

                //    await Task.Delay(100);
                //    server.SendMoreFrame(routingKey);
                //    server.SendFrame("Welcome");
                //}
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            throw;
        }
    }

    private void FixedUpdate()
    {
        while (remoteEditor.isRunning && queue.TryDequeue(out byte[] msgJson))
        {
            Message msg = BinaryWriterExtensions.Deserialize<Message>(msgJson);
            HashSet<int> allIds = remoteObjects.Keys.ToHashSet();

            //meshes
            foreach (var msgMesh in msg.meshes)
            {
                if (!remoteMeshes.ContainsKey(msgMesh.instanceID))
                {
                    Mesh mesh = new Mesh();
                    mesh.name = msgMesh.instanceID + " " + msgMesh.name;
                    mesh.vertices = msgMesh.vertices;
                    mesh.triangles = msgMesh.triangles;
                    mesh.normals = msgMesh.normals;
                    mesh.uv = msgMesh.uv;

                    remoteMeshes.Add(msgMesh.instanceID, mesh);
                }
            }

            //textures
            foreach (var msgTex in msg.textures)
            {
                if (!remoteTextures.ContainsKey(msgTex.instanceID))
                {
                    if (msgTex.rawTextureData.Length > 0)
                    {
                        Texture2D tex = new Texture2D(msgTex.width, msgTex.height, msgTex.format, false);
                        tex.name = msgTex.instanceID + " " + msgTex.name;
                        tex.alphaIsTransparency = msgTex.alphaIsTransparency;
                        tex.anisoLevel = msgTex.anisoLevel;
                        tex.wrapMode = msgTex.wrapMode;
                        tex.filterMode = msgTex.filterMode;

                        tex.LoadRawTextureData(msgTex.rawTextureData);
                        tex.Apply();

                        remoteTextures.Add(msgTex.instanceID, tex);
                    }
                    else
                    {
                        //TODO: try to find texture in project by name
                    }
                }
            }

            //game objects
            foreach (var mgo in msg.gameObjects)
            {
                allIds.Remove(mgo.instanceID);

                Transform t;
                GameObject go;
                if (!remoteObjects.TryGetValue(mgo.instanceID, out t))
                {
                    go = new GameObject(mgo.name);
                    t = go.transform;
                    remoteObjects.Add(mgo.instanceID, t);
                }
                else
                {
                    go = t.gameObject;
                }

                {
                    if (mgo.TryGetComponent<Message.MsgMeshFilter>(out var msgMeshFilter))
                    {
                        if (remoteMeshes.TryGetValue(msgMeshFilter.meshInstanceID, out Mesh mesh))
                        {
                            if (!go.TryGetComponent(out MeshFilter meshFilter))
                                meshFilter = go.AddComponent<MeshFilter>();

                            if (!meshFilter.sharedMesh || !meshFilter.sharedMesh.name.StartsWith(msgMeshFilter.meshInstanceID.ToString()))
                                meshFilter.sharedMesh = mesh;
                        }
                    }
                }
                {
                    if (mgo.TryGetComponent<Message.MsgMeshRenderer>(out var msgMeshRenderer))
                    {
                        if (!go.TryGetComponent(out MeshRenderer meshRenderer))
                            meshRenderer = go.AddComponent<MeshRenderer>();

                        Shader shader = Shader.Find(msgMeshRenderer.shader);
                        if (shader == null)
                            shader = Shader.Find("Diffuse");
                        meshRenderer.material = new Material(shader);
                        meshRenderer.sharedMaterial.name = msgMeshRenderer.shader;


                        if (remoteTextures.TryGetValue(msgMeshRenderer.mainTextureInstanceID, out Texture2D tex))
                        {
                            meshRenderer.sharedMaterial.mainTexture = tex;
                        }
                        else if (meshRenderer.sharedMaterial.HasTexture("_MainTex"))
                        {
                            meshRenderer.sharedMaterial.mainTexture = tex;
                        }
                    }
                }
                {
                    if (mgo.TryGetComponent<Message.MsgSkinnedMesh>(out var msgSkinnedMesh))
                    {
                        if (!go.TryGetComponent(out SkinnedMeshRenderer skinnedMesh))
                            skinnedMesh = go.AddComponent<SkinnedMeshRenderer>();

                        Shader shader = Shader.Find(msgSkinnedMesh.shader);
                        if (shader == null)
                            shader = Shader.Find("Diffuse");
                        skinnedMesh.material = new Material(shader);
                        skinnedMesh.sharedMaterial.name = msgSkinnedMesh.shader;

                        if (remoteTextures.TryGetValue(msgSkinnedMesh.mainTextureInstanceID, out Texture2D tex))
                        {
                            skinnedMesh.sharedMaterial.mainTexture = tex;
                        }
                        else if (skinnedMesh.sharedMaterial.HasTexture("_MainTex"))
                        {
                            skinnedMesh.sharedMaterial.mainTexture = tex;
                        }

                        if (msgSkinnedMesh.rootBoneInstanceID != 0 && remoteObjects.ContainsKey(msgSkinnedMesh.rootBoneInstanceID))
                            skinnedMesh.rootBone = remoteObjects[msgSkinnedMesh.rootBoneInstanceID];
                        else
                            skinnedMesh.rootBone = null;
                    }
                }

                if (mgo.parentInstanceID != 0 && remoteObjects.ContainsKey(mgo.parentInstanceID))
                    t.parent = remoteObjects[mgo.parentInstanceID];
                else 
                    t.parent = null;

                if (go.activeSelf != mgo.activeSelf)
                    go.SetActive(mgo.activeSelf);

                t.position = mgo.position;
                t.rotation = mgo.rotation;
                t.localScale = mgo.localScale;
            }

            foreach (int idToRemove in allIds)
            {
                Destroy(remoteObjects[idToRemove].gameObject);
                remoteObjects.Remove(idToRemove);
            }
        }

    }
}
