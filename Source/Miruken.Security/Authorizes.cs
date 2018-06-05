﻿namespace Miruken.Security
{
    using System;
    using Callback;
    using Callback.Policy;

    public class Authorizes : CategoryAttribute
    {
        public Authorizes()
        {          
        }

        public Authorizes(object policy)
        {
            InKey = policy;
        }

        public override CallbackPolicy CallbackPolicy => Policy;

        public static void AddFilters(params IFilterProvider[] providers) =>
            Policy.AddFilters(providers);

        public static void AddFilters(params Type[] filterTypes) =>
            Policy.AddFilters(filterTypes);

        public static readonly CallbackPolicy Policy =
             ContravariantPolicy.Create<Authorization>(v => v.Target,
                    x => x.MatchCallbackMethod(
                            Return.Of<bool>(), x.Target, x.Extract(v => v.Principal))
                          .MatchMethod(Return.Of<bool>(), x.Target, x.Callback)
                          .MatchMethod(Return.Of<bool>(), x.Callback)
             );
    }
}