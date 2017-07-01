// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System.Collections.Generic;

    public class Config
    {
        public IList<PeerId> Peers { get; set; }
    }
}
