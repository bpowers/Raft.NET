// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

    public struct LogIndex
    {
        int N;
    }

    public struct Term
    {
        int N;
    }

    internal interface ILogEntry<TWriteOp>
    {
        LogIndex Index { get; set; }
        Term Term { get; set; }
        TWriteOp Operation { get; set; }
    }

    internal interface ILog<TWriteOp>
    {
        Task WriteAsync(ILogEntry<TWriteOp> entry);
    }
}
