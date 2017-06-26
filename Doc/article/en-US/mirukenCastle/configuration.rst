=============
Configuration
=============

Miruken does not require that you use Castle Windsor, or even that you use a container, but we have really nice integration with Castle Windsor if you choose to use it. 

This is a basic Castle Windsor Container:

.. literalinclude:: /example/mirukenCastleExamples/basicWindsorContainer.cs

At a conceptual level Miruken embraces concepts and features. Miruken has very few concepts, and these concepts are used to build many features. When configuring a Castle container, concepts are brought in by installers, and features are your specific application assemblies. 

.. literalinclude:: /example/mirukenCastleExamples/featuresFromAssembly.cs

In this example, we have two assemblies that have features for our application.  
We use :code:`Features.FromAssemblies()` :code:`typeof(CreateTeam).Assembly` to target the League.dll and code:`typeof(CreateStudent).Assembly` to target the School.dll. Each FeatureAssembly is installed with the Feature installers. 

