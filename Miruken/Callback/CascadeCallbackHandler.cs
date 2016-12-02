namespace Miruken.Callback
{
	public class CascadeCallbackHandler : CallbackHandler
	{
		private readonly ICallbackHandler _handlerA;
		private readonly ICallbackHandler _handlerB;

		internal CascadeCallbackHandler(ICallbackHandler handlerA, ICallbackHandler handlerB)
		{
			_handlerA = handlerA;
			_handlerB = handlerB;
		}

		protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
		{
			var handled = greedy
				? _handlerA.Handle(callback, true, composer) 
                   | _handlerB.Handle(callback, true, composer)
				: _handlerA.Handle(callback, false, composer)
                  || _handlerB.Handle(callback, false, composer);
            if (!handled || greedy) {                                                                                                                                   
                handled = base.HandleCallback(callback, greedy, composer) || handled;                                                                                             
            }
		    return handled;
		}
	}
}
