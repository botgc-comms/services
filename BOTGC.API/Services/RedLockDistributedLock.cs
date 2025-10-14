﻿using BOTGC.API.Interfaces;
using RedLockNet;

namespace BOTGC.API.Services
{
    public class RedLockDistributedLock : IDistributedLock
    {
        private readonly IRedLock _redLock;

        public RedLockDistributedLock(IRedLock redLock)
        {
            _redLock = redLock;
        }

        public bool IsAcquired => _redLock.IsAcquired;

        public async ValueTask DisposeAsync()
        {
            await _redLock.DisposeAsync();
        }
    }
}
