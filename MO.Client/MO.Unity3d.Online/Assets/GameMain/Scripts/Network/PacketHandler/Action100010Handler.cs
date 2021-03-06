﻿using GameFramework.Network;
using MO.Algorithm.OnlineDemo;
using MO.Protocol;
using MO.Unity3d.Data;
using MO.Unity3d.UIExtension;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MO.Unity3d.Network.PacketHandler
{
    public class Action100010Handler : IPacketHandler
    {
        public int Id
        {
            get { return 100010; }
        }

        public void Handle(object sender, Packet packet)
        {
            S2C100010 rep = S2C100010.Parser.ParseFrom(((MOPacket)packet).Packet.Content);
            if (GlobalGame.FrameCount != 0)
            {
                var nextFrameCount = GlobalGame.FrameCount + 1;
                if (nextFrameCount != rep.FrameCount)
                {
                    Log.Info("丢帧");
                }
            }
            GlobalGame.FrameCount = rep.FrameCount;
            PlayerData player;
            foreach (var command in rep.Commands)
            {
                if (GameUser.Instance.Players.TryGetValue(command.UserId, out player))
                {
                    if (command.UserId == GameUser.Instance.UserId)
                        continue;

                    switch (command.CommandId)
                    {
                        case (int)CommandType.BigSkill:
                            player.ShowBigSkill();
                            break;
                        case (int)CommandType.Jump:
                            player.Jump();
                            break;
                        case (int)CommandType.SkillC:
                            player.ShowSkillC();
                            break;
                        case (int)CommandType.SkillX:
                            player.ShowSkillX();
                            break;
                        case (int)CommandType.SkillZ:
                            player.ShowSkillZ();
                            break;
                    }
                }
            }

            var stateInfoList = StateInfoList.Parser.ParseFrom(rep.CommandResult);
            foreach (var stateInfo in stateInfoList.StateInfos)
            {
                if (GameUser.Instance.Players.TryGetValue(stateInfo.UserId, out player))
                {
                    player.CurBlood = stateInfo.BloodValue;
                    if (player.CurBlood == 0)
                    {
                        player.Reset();
                    }
                    player.KillCount = stateInfo.KillCount;
                    player.DeadCount = stateInfo.DeadCount;

                    player.Position = new Vector3(
                        stateInfo.Transform.Position.X,
                        stateInfo.Transform.Position.Y,
                        stateInfo.Transform.Position.Z);
                    player.Rotate = new Vector3(
                        stateInfo.Transform.Rotation.X,
                        stateInfo.Transform.Rotation.Y,
                        stateInfo.Transform.Rotation.Z);
                }
            }
        }
    }
}
