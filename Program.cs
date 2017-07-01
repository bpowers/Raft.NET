// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Collections.Generic;

    class Program
    {
        static void Main(string[] args)
        {
            var config = new Config()
            {
                Peers = new List<PeerId>(),
            };

            Console.WriteLine("Hello World!");
        }
    }
}
