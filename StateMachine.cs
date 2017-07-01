// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Threading.Tasks;

    internal interface IStateMachine<TReadOp, TWriteOp, TValue>
    {
        // yields a new state machine
        Task<IStateMachine<TReadOp, TWriteOp, TValue>> ApplyAsync(TWriteOp operation);
        Task<TValue> ReadAsync(TReadOp operation);
    }
}
