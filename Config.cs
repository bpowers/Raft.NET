// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    public static class Time
    {
        public static TimeSpan Milliseconds(int ms)
        {
            return new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: ms);
        }
    }

    public class Config
    {
        public IList<PeerId>            Peers               { get; set; }
        public IDictionary<PeerId, int> PrngSeed            { get; set; } = new Dictionary<PeerId, int>();
        public PeerRpcDelegate          PeerRpcDelegate     { get; set; }
        public TimeSpan                 BroadcastTime       { get; set; } = Time.Milliseconds(15);
        public TimeSpan                 ElectionTimeoutMin  { get; set; } = Time.Milliseconds(50);
        public TimeSpan                 ElectionTimeoutSpan { get; set; } = Time.Milliseconds(100);
    }
}
