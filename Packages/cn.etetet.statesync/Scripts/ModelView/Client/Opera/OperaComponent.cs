using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
	[ComponentOf(typeof(Scene))]
	public class OperaComponent: Entity, IAwake, IUpdate
    {
        public Vector3 ClickPoint;

	    public int mapMask;

	    // WASD 移动状态
	    public bool IsMoving;
	    public float3 LastDirection;
    }
}
