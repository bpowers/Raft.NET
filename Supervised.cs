// Copyright 2017 Bobby Powers. All rights reserved.
// Use of this source code is governed by the ISC
// license that can be found in the LICENSE file.

namespace Raft
{
    using System;
    using System.Threading.Tasks;

    internal static class Supervised
    {
        // Wraps a Taks.Run function in a try catch to diagnose any
        // Exceptions that are thrown (without this, the .NET runtime
        // silently swallows them).
        internal static void Run(Func<Task> function)
        {
            Task.Run(async () => {
                try
                {
                    await function();
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Exception in supervised task: " + exception);
                }
            });
        }
    }
}
