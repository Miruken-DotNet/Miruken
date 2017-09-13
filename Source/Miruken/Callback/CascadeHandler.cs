namespace Miruken.Callback
{
	public class CascadeHandler : Handler
	{
		private readonly IHandler _handlerA;
		private readonly IHandler _handlerB;

		public CascadeHandler(object handlerA, object handlerB)
		{
		    _handlerA = ToHandler(handlerA);
		    _handlerB = ToHandler(handlerB);
		}

		protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
		{
		    var handled = base.HandleCallback(callback, ref greedy, composer);
			return greedy
				? handled | _handlerA.Handle(callback, ref greedy, composer) 
                   | _handlerB.Handle(callback, ref greedy, composer)
				: handled || _handlerA.Handle(callback, ref greedy, composer)
                  || _handlerB.Handle(callback, ref greedy, composer);
		}
	}
}
