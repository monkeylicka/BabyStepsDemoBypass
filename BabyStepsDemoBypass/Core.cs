using Il2CppSteamAudio;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(BabyStepsDemoBypass.Core), "BabyStepsDemoBypass", "1.0.0", "monkeylicker", null)]
[assembly: MelonGame("DefaultCompany", "BabySteps")]

namespace BabyStepsDemoBypass
{
    public class Core : MelonMod
    {
        private bool demoBlockRemoved = false;
        private bool isFlying = false;
        private object lockCoroutine = null;

        private GameObject playerGlobal = null;
        float flySpeed = 1f;

        private struct TransformState
        {
            public UnityEngine.Vector3 position { get; set; }
            public UnityEngine.Quaternion rotation;
            public bool hadRigidbody;
            public bool originalKinematic;
        }

        private Dictionary<Transform, TransformState> originalStates = new Dictionary<Transform, TransformState>();

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Toggle flight (duh)
            if(Input.GetKeyDown(KeyCode.F1))
            {
                playerGlobal = GameObject.Find("Dudest");
                ToggleFlyingMode(playerGlobal);
            }

            if (isFlying)
            {
                // Change flying speed by 0.1
                if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    flySpeed -= 0.1f;
                    MelonLogger.Msg("Flight speed decreased to " + flySpeed);
                }
                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    flySpeed += 0.1f;
                    MelonLogger.Msg("Flight speed increased to " + flySpeed);
                }

                // Regular flight controls
                if (Input.GetKey(KeyCode.W))
                {
                    MoveAllByOffset(new UnityEngine.Vector3(0, 0, flySpeed), playerGlobal);
                }
                if (Input.GetKey(KeyCode.S))
                {
                    MoveAllByOffset(new UnityEngine.Vector3(0, 0, -flySpeed), playerGlobal);
                }
                if (Input.GetKey(KeyCode.A))
                {
                    MoveAllByOffset(new UnityEngine.Vector3(-flySpeed, 0, 0), playerGlobal);
                }
                if (Input.GetKey(KeyCode.D))
                {
                    MoveAllByOffset(new UnityEngine.Vector3(flySpeed, 0, 0), playerGlobal);
                }

                // Vertical movement
                if (Input.GetKey(KeyCode.Q))
                {
                    MoveAllByOffset(new UnityEngine.Vector3(0, -flySpeed, 0), playerGlobal);
                }
                if (Input.GetKey(KeyCode.E))
                {
                    MoveAllByOffset(new UnityEngine.Vector3(0, flySpeed, 0), playerGlobal);
                }
            }
        }

        private void ToggleFlyingMode(GameObject player)
        {
            isFlying = !isFlying;

            if (player == null) { return; }

            if (isFlying)
            {
                MelonLogger.Msg("Flying mode ON. Storing positions and starting lock.");
                originalStates.Clear();
                StoreChildTransforms(player.transform);
                MelonLogger.Msg(originalStates.Count + " transforms stored.");

                lockCoroutine = MelonCoroutines.Start(LockObjectTransform());
            }
            else
            {
                MelonLogger.Msg("Flying mode OFF. Stopping lock.");
                if (lockCoroutine != null)
                {
                    MelonCoroutines.Stop(lockCoroutine);
                    lockCoroutine = null;
                }
                RestoreChildTransforms();
            }
        }

        private void MoveAllByOffset(UnityEngine.Vector3 offset, GameObject player)
        {
            GameObject camera = GameObject.Find("Dudest/GameCam");
            if (camera == null) return;
            UnityEngine.Vector3 cameraAdjustedPosition = camera.transform.rotation * offset;
            MelonLogger.Msg(offset);
            foreach (var pair in originalStates)
            {
                originalStates[pair.Key] = new TransformState
                {
                    position = pair.Value.position + cameraAdjustedPosition,
                    rotation = pair.Value.rotation,
                    hadRigidbody = pair.Value.hadRigidbody,
                    originalKinematic = pair.Value.originalKinematic
                };
            }

            ToggleFlyingMode(player);
            ToggleFlyingMode(player);
        }

        private void StoreChildTransforms(Transform parent)
        {
            Rigidbody rb = parent.GetComponent<Rigidbody>();
            originalStates[parent] = new TransformState
            {
                position = parent.position,
                rotation = parent.rotation,
                hadRigidbody = (rb != null),
                originalKinematic = (rb != null ? rb.isKinematic : false)
            };

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = UnityEngine.Vector3.zero;
                rb.angularVelocity = UnityEngine.Vector3.zero;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                StoreChildTransforms(parent.GetChild(i));
            }
        }

        private void RestoreChildTransforms()
        {
            foreach (var pair in originalStates)
            {
                Transform tf = pair.Key;
                if (tf == null) continue;

                tf.position = pair.Value.position;
                tf.rotation = pair.Value.rotation;

                if (pair.Value.hadRigidbody)
                {
                    Rigidbody rb = tf.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = pair.Value.originalKinematic;
                    }
                }
            }

            originalStates.Clear();
        }

        private System.Collections.IEnumerator LockObjectTransform()
        {
            while (true)
            {
                foreach (var pair in originalStates)
                {
                    if(pair.Key != null)
                    {
                        pair.Key.position = pair.Value.position;
                        pair.Key.rotation = pair.Value.rotation;
                    }
                }
                yield return null;
            }
        }

        private void LockTransformAndChildren(Transform parent, UnityEngine.Vector3 position)
        {
            parent.position = position;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                LockTransformAndChildren(child, position);
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MelonLogger.Msg("Scene loaded: " + sceneName);

            if(!demoBlockRemoved)
            {
                GameObject target = GameObject.Find("DemoStopDad(Clone)");

                if (target != null)
                {
                    MelonLogger.Msg("Found DemoStopDad. Disabling.");
                    target.SetActive(false);
                    demoBlockRemoved = true;
                } 
                else
                {
                    MelonLogger.Msg("DemoStopDad not found in the scene.");
                }
            }
        }
    }
}
