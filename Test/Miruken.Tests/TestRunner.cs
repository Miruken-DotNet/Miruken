using System;
using System.Threading;

namespace Miruken.Tests
{
    public class TestRunner
    {
        private readonly Action _test;
        private readonly ApartmentState _apartmentState;
        private Exception _exception;

        public TestRunner(Action test, ApartmentState apartmentState)
        {
            _test           = test;
            _apartmentState = apartmentState;
        }

        public void Execute()
        {
            // Setup a worker thread
            var workerThread = new Thread(ExecuteInternal);
            // Set apartment
            workerThread.SetApartmentState(_apartmentState);

            workerThread.Start();
            // Wait until work on the worker thread is done
            workerThread.Join();
            // Probe for unhandled exception
            if (_exception != null)
            {
                // If exception is present, rethrow here on the main thread
                throw _exception;
            }
        }

        private void ExecuteInternal()
        {
            // wrap our original action in the try/catch
            try
            {
                _test();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Don't consider race to happen here, subject to think more
                _exception = ex;
                // Don't rethrow here as that would kill the test host
            }
        }

        public static void STA(Action test)
        {
            new TestRunner(test, ApartmentState.STA).Execute();
        }

        public static void MTA(Action test)
        {
            new TestRunner(test, ApartmentState.MTA).Execute();
        }
    }
}
