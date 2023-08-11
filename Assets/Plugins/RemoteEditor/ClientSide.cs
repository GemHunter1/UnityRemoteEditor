using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using RemoteEditor.Messages;

namespace RemoteEditor
{
    public class ClientSide : MonoBehaviour
    {
        public bool isClientConnected = false;

        public float maxSize = 0;
        public float averageSize = 0;

        private DealerSocket client;
        internal RemoteEditor remoteEditor;
        internal string address;

        private BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

        private int[] sizeAverageBuffer = new int[200];
        private int sizeAverageIdx = 0;
        private bool sizeAverageRolledOver = false;

        private HashSet<int> alreadySentMeshes = new HashSet<int>();
        private HashSet<int> alreadySentTextures = new HashSet<int>();

        private Queue<MsgTexture2D> asyncQueuedTextures = new Queue<MsgTexture2D>();
        private Queue<TransformRequest> queuedTransformRequests = new Queue<TransformRequest>();

        private Dictionary<int, Transform> localTransforms = new Dictionary<int, Transform>();

        internal void Setup(RemoteEditor remoteEditor, string address)
        {
            this.remoteEditor = remoteEditor;
            this.address = address;
        }

        internal async Task ClientAsync()
        {
            try
            {
                using (client = new DealerSocket(address))
                {
                    isClientConnected = false;

                    client.SendFrame(RemoteEditor.HelloMessage);
                    while (remoteEditor.isRunning && !isClientConnected)
                    {
                        var (message, more) = await client.ReceiveFrameStringAsync();
                        Debug.Log("[URE] Received from server: " + message);
                        if (message == RemoteEditor.WelcomeMessage)
                        {
                            isClientConnected = true;
                        }
                    }

                    Task sendTask = Task.Factory.StartNew(() =>
                    {
                        while (remoteEditor.isRunning)
                        {
                            byte[] msgJson = queue.Take();
                            client.SendFrame(msgJson);
                        }
                    }, remoteEditor.cts.Token);

                    isClientConnected = true;
                    while (remoteEditor.isRunning)
                    {
                        var (message, more) = await client.ReceiveFrameBytesAsync();

                        if (message.Length > 0 && message[0] == 42)
                        {
                            TransformRequest tr = BinaryWriterExtensions.Deserialize<TransformRequest>(message);
                            queuedTransformRequests.Enqueue(tr);
                        }
                    }
                    isClientConnected = false;

                    //for (int i = 0; i < 1000; i++)
                    //{
                    //    client.SendFrame("Hello");
                    //    var (message, more) = await client.ReceiveFrameStringAsync();

                    //    // TODO: process reply
                    //    Debug.Log("[URE] Client received: " + message);

                    //    await Task.Delay(100);
                    //}
                }
            }
            catch (Exception ex)
            {
                isClientConnected = false;
                Debug.LogError("[URE] " + ex);
                throw;
            }
        }

        private void FixedUpdate()
        {
            while (remoteEditor.isRunning && queuedTransformRequests.TryDequeue(out TransformRequest trReq))
            {
                if (localTransforms.TryGetValue(trReq.gameObjectInstanceID, out Transform t))
                {
                    if (t)
                        trReq.Apply(t);
                    else 
                        localTransforms.Remove(trReq.gameObjectInstanceID);
                }
            }

            if (remoteEditor.isRunning && isClientConnected && client != null)
            {
                if (queue.Count > 5) return;

                var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                Message msg = new Message();

                foreach (var root in rootObjects)
                {
                    if (root.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                        continue;

                    Stack<Transform> stack = new Stack<Transform>();

                    stack.Push(root.transform);

                    while (stack.Count > 0)
                    {
                        Transform t = stack.Pop();

                        //process game object
                        var mgo = new MsgGameObject(t);
                        msg.gameObjects.Add(mgo);

                        if (!localTransforms.ContainsKey(mgo.instanceID))
                            localTransforms[mgo.instanceID] = t;

                        {
                            if (t.gameObject.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh)
                            {
                                if (!alreadySentMeshes.Contains(meshFilter.sharedMesh.GetInstanceID()))
                                {
                                    var msgMesh = new MsgMesh(meshFilter.sharedMesh);
                                    alreadySentMeshes.Add(msgMesh.instanceID);
                                    msg.meshes.Add(msgMesh);
                                }
                            }
                        }

                        {
                            if (t.gameObject.TryGetComponent(out MeshRenderer meshRenderer)
                                && meshRenderer.sharedMaterial && meshRenderer.sharedMaterial.HasTexture("_MainTex") && meshRenderer.sharedMaterial.mainTexture && meshRenderer.sharedMaterial.mainTexture is Texture2D)
                            {
                                Texture2D tex = (Texture2D)meshRenderer.sharedMaterial.mainTexture;
                                if (!alreadySentTextures.Contains(tex.GetInstanceID()))
                                {
                                    alreadySentTextures.Add(tex.GetInstanceID());

                                    var msgTex = new MsgTexture2D(tex);

                                    if (!tex.isReadable)
                                    {
                                        if (SystemInfo.IsFormatSupported(tex.graphicsFormat, UnityEngine.Experimental.Rendering.FormatUsage.ReadPixels))
                                        {
                                            StartCoroutine(AsyncReadbackTexture(msgTex, tex));
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"[URE] Texture {tex.name} with format {tex.format} does not support GPU readback");
                                            msg.textures.Add(msgTex);
                                        }
                                    }
                                    else
                                    {
                                        //StartCoroutine(DelayedTextureSend(msgTex));
                                        msg.textures.Add(msgTex);
                                    }
                                }
                            }

                            while (asyncQueuedTextures.TryDequeue(out var msgTex))
                            {
                                msg.textures.Add(msgTex);
                            }
                        }

                        foreach (Transform child in t)
                        {
                            if (t.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                                continue;
                            stack.Push(child);
                        }
                    }
                }

                byte[] bytes = BinaryWriterExtensions.Serialize(msg);
                queue.Add(bytes);

                sizeAverageBuffer[sizeAverageIdx++] = bytes.Length;
                if (sizeAverageIdx >= sizeAverageBuffer.Length)
                {
                    sizeAverageIdx = 0;
                    sizeAverageRolledOver = true;
                }

                averageSize = sizeAverageBuffer.Sum() / (sizeAverageRolledOver ? sizeAverageBuffer.Length : sizeAverageIdx) / 1024.0f;
                maxSize = sizeAverageBuffer.Max() / 1024.0f;
            }
        }

        private IEnumerator AsyncReadbackTexture(MsgTexture2D msgTex, Texture2D tex)
        {
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(tex);

            while (!request.hasError && !request.done)
                yield return null;

            if (!request.hasError)
                msgTex.rawTextureData = request.GetData<byte>();

            asyncQueuedTextures.Enqueue(msgTex);
        }

        private IEnumerator DelayedTextureSend(MsgTexture2D msgTex)
        {
            yield return new WaitForSeconds(5);

            asyncQueuedTextures.Enqueue(msgTex);
        }
    }
}