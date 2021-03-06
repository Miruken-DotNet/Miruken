﻿namespace Miruken.Api.Oneway
{
    using Api;
    using Callback;
    using Concurrency;

    public class OnewayHandler : Handler
    {
        [Provides, Singleton]
        public OnewayHandler()
        {
        }

        [Handles]
        public Promise Oneway(Oneway oneway, IHandler composer)
        {
            return composer.Send(oneway.Request);
        }
    }
}
