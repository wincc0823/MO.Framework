﻿using MO.Protocol;
using MO.Unity3d.Data;
using MO.Unity3d.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections;

namespace MO.Unity3d.Entities
{
    public class SelfEntity : EntityLogic
    {
		private Vector3 _offset;
		private float _positionSpeed = 2.0f;
		private float _rotateSpeed = 8.0f;
		private float currY = 1.0f;
		private PlayerData _playerData;

		protected internal override void OnInit(object userData)
		{
			base.OnInit(userData);
			//UIJoystickControl.Instance.joystickDragDelegate = OnJoystickDrag;
			_playerData = (PlayerData)userData;
			GetComponent<Renderer>().material.color = Color.blue;
			transform.position = new Vector3(_playerData.X, _playerData.Y, _playerData.Z);
			Vector3 eulerAngles = new Vector3(
				_playerData.RX,
				_playerData.RY,
				_playerData.RZ);
			transform.Rotate(eulerAngles);

			_offset = Camera.main.transform.position;
			var position = transform.position + _offset;
			Camera.main.transform.position = position;
		}

		protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
		{
			//if (GameUser.Instance.JumpState)
			//{
			//	transform.position += transform.forward * _positionSpeed * 2;
			//	GameUser.Instance.IsJump = false;
			//}

			if (GameUser.Instance.JumpState > 0)
			{
				transform.position += transform.forward * Time.deltaTime * _positionSpeed * 4;

				if (GameUser.Instance.JumpState == 1)
				{
					currY += 0.05f;

					if (currY > 2.0f)
						GameUser.Instance.JumpState = 2;
				}
				else
				{
					currY -= 0.05f;
					if (currY < 1.0f)
					{
						GameUser.Instance.JumpState = 0;
						currY = 1.0f;
					}
				}

				Vector3 pos = transform.position;
				pos.y = currY;
				transform.position = pos;
			}


			var eulerAngles = JoystickControl.GetDestination();
			if (eulerAngles != new Vector3())
			{
				Vector3 destDirection = new Vector3(eulerAngles.x, 0, eulerAngles.y);
				Quaternion quaternion = Quaternion.LookRotation(destDirection);
				transform.rotation = quaternion;
				transform.position += transform.forward * Time.deltaTime * _positionSpeed;
			}
			var position = transform.position + _offset;
			Camera.main.transform.position = position;
			//FixedState();
			base.OnUpdate(elapseSeconds, realElapseSeconds);
		}

		private void FixedState()
		{
			//500ms误差修正玩家位置
			var destDirection = new Vector3(_playerData.RX, _playerData.RY, _playerData.RZ);
			if (Vector3.Distance(transform.eulerAngles, destDirection) > _rotateSpeed / 2)
			{
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(destDirection), _rotateSpeed * Time.deltaTime);
			}

			float distance = 0.0f;
			float deltaSpeed = (_positionSpeed * Time.deltaTime);

			var destPosition = new Vector3(_playerData.X, _playerData.Y, _playerData.Z);
			distance = Vector3.Distance(destPosition, transform.position);

			if (distance > _positionSpeed / 2)
			{
				Vector3 pos = transform.position;

				Vector3 movement = destPosition - pos;
				movement.y = 0f;
				movement.Normalize();

				movement *= deltaSpeed;

				if (distance > deltaSpeed || movement.magnitude > deltaSpeed)
					pos += movement;
				else
					pos = destPosition;

				transform.position = pos;
			}
		}

		void FixedUpdate()
		{
			GameUser.Instance.Channel.Send(new C2S100003()
			{
				Vector = new MsgVector3()
				{
					X = transform.position.x,
					Y = transform.position.y,
					Z = transform.position.z
				},
				Rotation = new MsgRotation()
				{
					X = transform.rotation.eulerAngles.x,
					Y = transform.rotation.eulerAngles.y,
					Z = transform.rotation.eulerAngles.z
				}
			}.BuildPacket());
		}
	}
}
