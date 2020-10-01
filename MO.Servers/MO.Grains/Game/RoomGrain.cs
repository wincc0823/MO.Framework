﻿using Google.Protobuf;
using Microsoft.Extensions.Logging;
using MO.GrainInterfaces;
using MO.GrainInterfaces.Game;
using MO.GrainInterfaces.User;
using MO.Protocol;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MO.Grains.Game
{
    public class RoomInfo
    {
        public Int32 GameId { get; set; }
    }

    public class RoomGrain : Grain, IRoom
    {
        private readonly IPersistentState<RoomInfo> _roomInfo;
        private readonly IPersistentState<Dictionary<long, PlayerData>> _players;
        private readonly ILogger _logger;

        private IAsyncStream<MOMsg> _stream;
        private IDisposable _reminder;

        public RoomGrain(
            [PersistentState("RoomInfo", StorageProviders.DefaultProviderName)] IPersistentState<RoomInfo> roomInfo,
            [PersistentState("Players", StorageProviders.DefaultProviderName)] IPersistentState<Dictionary<long, PlayerData>> players,
            ILogger<RoomGrain> logger)
        {
            _roomInfo = roomInfo;
            _players = players;
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            //自定义加载数据
            await _roomInfo.ReadStateAsync();
            await _players.ReadStateAsync();
            
            //间隔1秒执行一次
            _reminder = RegisterTimer(
                OnTimerCallback,
                this,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));

            var streamProvider = this.GetStreamProvider(StreamProviders.JobsProvider);
            _stream = streamProvider.GetStream<MOMsg>(Guid.NewGuid(), StreamProviders.Namespaces.ChunkSender);

            //var roomFactory = GrainFactory.GetGrain<IRoomFactory>(this.GetPrimaryKeyLong());
            //_roomInfo = await roomFactory.GetRoomInfo((int)this.GetPrimaryKeyLong());
            //for (int i = 0; i < _roomInfo.RoomHeader.PlayerNum; i++)
            //{
            //    _seatDatas.Add(new SeatData(i));
            //}
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            if (_reminder != null)
                _reminder.Dispose();

            //回写数据
            await _roomInfo.WriteStateAsync();
            await _players.WriteStateAsync();
            await base.OnDeactivateAsync();
        }

        private async Task OnTimerCallback(object obj)
        {
            await Task.CompletedTask;
        }

        public async Task RoomNotify(MOMsg msg)
        {
            await _stream.OnNextAsync(msg);
        }

        public async Task Reconnect(IUser user)
        {
            await user.SubscribeRoom(_stream.Guid);
        }

        public async Task PlayerEnterRoom(IUser user)
        {
            if (!_players.State.ContainsKey(user.GetPrimaryKeyLong()))
                _players.State[user.GetPrimaryKeyLong()] = new PlayerData(user);

            await user.SubscribeRoom(_stream.Guid);

            {
                S2C100001 content = new S2C100001();
                content.RoomId = (int)this.GetPrimaryKeyLong();
                foreach (var item in _players.State)
                {
                    PlayerData player = null;
                    if (_players.State.TryGetValue(item.Key, out player))
                    {
                        content.UserPoints.Add(new UserPoint()
                        {
                            UserId = item.Key,
                            UserName = await player.User.GetUserName(),
                            X = player.X,
                            Y = player.Y
                        });
                    }
                }
                MOMsg msg = new MOMsg();
                msg.ActionId = 100001;
                msg.Content = content.ToByteString();
                await user.Notify(msg);
            }
            {
                S2C100002 content = new S2C100002();
                content.UserId = user.GetPrimaryKeyLong();
                content.RoomId = (int)this.GetPrimaryKeyLong();
                content.UserName = await user.GetUserName();
                MOMsg msg = new MOMsg();
                msg.ActionId = 100002;
                msg.Content = content.ToByteString();
                await RoomNotify(msg);
            }
        }

        public async Task PlayerLeaveRoom(IUser user)
        {
            _players.State.Remove(user.GetPrimaryKeyLong());
            await user.UnsubscribeRoom();

            S2C100006 content = new S2C100006();
            content.UserId = user.GetPrimaryKeyLong();
            content.RoomId = (int)this.GetPrimaryKeyLong();
            MOMsg msg = new MOMsg();
            msg.ActionId = 100006;
            msg.Content = content.ToByteString();
            await RoomNotify(msg);
        }

        public Task PlayerReady(IUser user)
        {
            return Task.CompletedTask;
        }

        public async Task PlayerGo(IUser user, float x, float y)
        {
            S2C100004 content = new S2C100004();
            content.UserId = user.GetPrimaryKeyLong();
            content.X = x;
            content.Y = y;
            MOMsg msg = new MOMsg();
            msg.ActionId = 100004;
            msg.Content = content.ToByteString();
            await RoomNotify(msg);
            if (_players.State.ContainsKey(user.GetPrimaryKeyLong()))
            {
                _players.State[user.GetPrimaryKeyLong()].SetPoint(x, y);
            }
        }

        public async Task PlayerSendMsg(IUser user, string msg)
        {
            S2C100008 content = new S2C100008();
            content.UserId = user.GetPrimaryKeyLong();
            content.Content = msg;
            MOMsg notify = new MOMsg();
            notify.ActionId = 100008;
            notify.Content = content.ToByteString();
            await RoomNotify(notify);
        }
    }
}
