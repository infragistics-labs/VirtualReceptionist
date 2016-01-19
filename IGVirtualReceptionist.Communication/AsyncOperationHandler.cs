﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IGVirtualReceptionist.Communication
{

    /// <summary>
    /// Helper class to expose exceptions thrown by asynch operations
    /// </summary>
    class AsyncOperationHandler
    {
        private Action<IAsyncResult> endOperation;
        public AsyncOperationHandler(Action<IAsyncResult> endOperation)
        {
            this.endOperation = endOperation;
        }

        public void Callback(IAsyncResult ar)
        {
            try
            {
                // Async operations can throw exceptions.
                // Generally, these exceptions should be handled gracefully.
                // For the purpose of illustration, we will simply log any issues.
                endOperation(ar);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }
    }


}
