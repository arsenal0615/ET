using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(OperaComponent))]
    [FriendOf(typeof(OperaComponent))]
    public static partial class OperaComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OperaComponent self)
        {
            self.mapMask = LayerMask.GetMask("Map");
        }

        [EntitySystem]
        private static void Update(this OperaComponent self)
        {
            // 鼠标右键点击移动（保持原有功能）
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000, self.mapMask))
                {
                    // 右键点击时取消 WASD 移动状态
                    self.IsMoving = false;
                    self.LastDirection = float3.zero;

                    C2M_PathfindingResult c2MPathfindingResult = C2M_PathfindingResult.Create();
                    c2MPathfindingResult.Position = hit.point;
                    self.Root().GetComponent<ClientSenderComponent>().Send(c2MPathfindingResult);
                }
            }

            // WASD 方向移动
            self.HandleWASDInput();

            if (Input.GetKeyDown(KeyCode.R))
            {
                CodeLoader.Instance.Reload();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                C2M_TransferMap c2MTransferMap = C2M_TransferMap.Create();
                self.Root().GetComponent<ClientSenderComponent>().Call(c2MTransferMap).NoContext();
            }
        }

        private static void HandleWASDInput(this OperaComponent self)
        {
            float h = 0, v = 0;
            if (Input.GetKey(KeyCode.W)) v += 1;
            if (Input.GetKey(KeyCode.S)) v -= 1;
            if (Input.GetKey(KeyCode.A)) h -= 1;
            if (Input.GetKey(KeyCode.D)) h += 1;

            if (h != 0 || v != 0)
            {
                // 计算相机相对方向（投影到 XZ 平面）
                Transform cameraTransform = Camera.main.transform;
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 dir = (forward * v + right * h).normalized;
                float3 direction = new float3(dir.x, 0, dir.z);

                // 仅在方向变化时发送（首次按下 或 方向改变）
                if (!self.IsMoving || !direction.Equals(self.LastDirection))
                {
                    self.IsMoving = true;
                    self.LastDirection = direction;

                    Log.Debug($"WASD: sending direction ({direction.x:F2}, {direction.z:F2}), IsMoving={self.IsMoving}");

                    C2M_MoveForward msg = C2M_MoveForward.Create();
                    msg.Direction = direction;
                    self.Root().GetComponent<ClientSenderComponent>().Send(msg);
                }
            }
            else if (self.IsMoving)
            {
                // 松开所有 WASD 键 → 立即发送停止
                self.IsMoving = false;
                self.LastDirection = float3.zero;

                Log.Debug("WASD: sending stop");

                C2M_Stop c2MStop = C2M_Stop.Create();
                self.Root().GetComponent<ClientSenderComponent>().Send(c2MStop);
            }
        }
    }
}