Context
=======

The context is one of the three major components of Miruken.  The other two major components being the protocol and the handler.

.. literalinclude:: /example/MirukenExamples/Context/CreatingAContext.cs
   :linenos:

At the simplest level a context is a collection of handlers. You can simply add instances of handlers to a context.

.. literalinclude:: /example/MirukenExamples/Context/AContextWithHandlerInstances.cs
   :linenos:

You can also rely on a container to create the handler instance.  We like to use Castle Windsor,
but as you can see by this example all you need is a handler that implements `IContainer`.
This means you can use any container you choose.

.. literalinclude:: /example/MirukenExamples/Context/RelyingOnAContainerToResolveHandlers.cs
   :linenos:

Hierarchical
------------
Contexts are also hierarchical. They know their parent and can create children.

.. image:: /img/context/Hierarchical.png

.. literalinclude:: /example/MirukenExamples/Context/CreatingAChildContext.cs
   :linenos:

Traversal
---------

Traversal is the concept of finding a handler for a message in the current context.

SelfOrAncestor
^^^^^^^^^^^^^^

By default when Miruken is trying to handle a message it starts with the current context. If the current context cannot handle
the message, the message will be passed to the parent to be handled.

.. image:: /img/context/SelfOrAncestor.png

Lifecycle
---------

- Context.End
- Context.Ended

