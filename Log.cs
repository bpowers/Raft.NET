// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public struct LogIndex
    {
        internal int N;

        public LogIndex(int n)
        {
            N = n;
        }
    }

    public struct Term
    {
        internal int N;

        public Term(int n)
        {
            N = n;
        }
    }

    internal interface ILogEntry<TWriteOp>
    {
        LogIndex Index { get; set; }
        Term Term { get; set; }
        TWriteOp Operation { get; set; }
    }

    internal interface ILog<TWriteOp>
    {
        Task<bool> WriteAsync(ILogEntry<TWriteOp> entry);
        ILogEntry<TWriteOp> Get(LogIndex index);
    }

    internal class Log<TWriteOp> : ILog<TWriteOp>
    {
        Config _config;
        List<ILogEntry<TWriteOp>> _log = new List<ILogEntry<TWriteOp>>();

        public Log(Config config)
        {
            _config = config;
        }

        public Task<bool> WriteAsync(ILogEntry<TWriteOp> entry)
        {
            if (entry.Index.N > _log.Count)
                throw new InvalidOperationException("too far ahead");

            return Task.FromResult(true);
        }

        public ILogEntry<TWriteOp> Get(LogIndex index)
        {
            return _log[index.N];
        }
    }
}
