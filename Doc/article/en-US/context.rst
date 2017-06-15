Context
=======

The context is one of the three major components of Miruken.  The other two major components being the protocol and the handler.

.. literalinclude:: /example/mirukenExamples/context/creatingAContext.cs
   :linenos:

At the simplest level a context is a collection of handlers. You can simply add instances of handlers to a context.

.. image:: /img/context/collectionOfHandlers.png

.. literalinclude:: /example/mirukenExamples/context/aContextWithHandlerInstances.cs
   :linenos:

You can also rely on a container to create the handler instance.  We like to use Castle Windsor,
but as you can see by this example all you need is a handler that implements `IContainer`.
This means you can use any container you choose.

.. literalinclude:: /example/mirukenExamples/context/relyingOnAContainerToResolveHandlers.cs
   :linenos:
   :end-before: //end
   :append: ...


Hierarchical
------------
Contexts are also hierarchical. They know their parent and can create children.

.. image:: /img/context/hierarchical.png

.. literalinclude:: /example/mirukenExamples/context/creatingAChildContext.cs
   :linenos:

Traversal
---------

Traversal is the concept of finding a handler for a message in the current context.

SelfOrAncestor
^^^^^^^^^^^^^^

SelfOrAncestor is the default. When Miruken is trying to handle a message it starts with the current context. If the current context cannot handle the message, the message will be passed to the parent to be handled.

.. image:: /img/context/selfOrAncestor.png

There are several other TraversingAxis

Ancestor
^^^^^^^^

.. image:: /img/context/ancestor.png

Child
^^^^^^^^

.. image:: /img/context/child.png

.. image:: /img/context/descendant.png

.. image:: /img/context/descendantReversed.png

.. image:: /img/context/root.png

.. image:: /img/context/self.png

.. image:: /img/context/selfOrChild.png

.. image:: /img/context/selfOrDescendant.png

.. image:: /img/context/selfOrDescendantReversed.png

.. image:: /img/context/selfOrSibling.png

.. image:: /img/context/selfSiblingOrAncestor.png

.. image:: /img/context/sibling.png


Lifecycle
---------

- Context.End
- Context.Ended

