==========================
Configuring Castle Windsor
==========================

.. note:: Miruken has first class integration with Castle Windsor, but Miruken does not require you to use Castle Windsor for your container. Miruken does not even require you to use a container.  All of that being said, we love Castle Windsor and use it in our own projects.

One of the main ways of configuring a Castle Windsor container is the :code:`Container.Install()` method.  It accepts a comma seperated list of :code:`IWindsorInstaller` instances.  These installers do all the work of registering objects and configuring the container.

In this very basic Castle Windsor Container all the :code:`IWindsorInstaller` classes in the current assembly will be run. :code:`FromAssembly.This()` returns an :code:`IWindsorInstaller`.

.. literalinclude:: /example/mirukenCastleExamples/basicWindsorContainer.cs

We used to use this simple form of configuration, but found that we had to list assemblies multiple times. Features solves this problem. 

Features
========

At a high level a feature is an implementation of a Miruken concept. It may be a Protocol, Handler, Validator, or Mediator, etc.  On a very practical level features are application code implemented across multiple assemblies. The :code:`Features` object has several ways to specify your application assemblies so that they can be installed in the container.

FromAssemblies(params Assembly[] assemblies)
--------------------------------------------

In this example we pass a comma seperated list of application assemblies into :code:`Features.FromAssemblies()`.
:code:`typeof(CreateTeam).Assembly` targets the Example.League assembly and 
:code:`typeof(CreateStudent).Assembly` targets the Example.School assembly. 
Using Features allows you to specify your assemblies in one place, and it guaranties that your assemblies are only scanned once during installation.

Next, you specify which Miruken installers you want to run. These installers inherit from 
:code:`FeatureInstaller`, and do all the work of registering objects and configuring the container for that specific feature across all your feature assemblies.  This example configures the ConfigurationFactory using the ConfigurationFactoryInstaller, and Validation using the ValidationInstaller.

.. literalinclude:: /example/mirukenCastleExamples/featuresFromAssemblies.cs
   :emphasize-lines: 21-25

FromAssembliesNamed(params string[] assemblyNames)
--------------------------------------------------

The FromAssembliesNamed() method allows you to specify the assembly name of the feature assemblies you want installed into the container.

.. literalinclude:: /example/mirukenCastleExamples/featuresFromAssembliesNamed.cs
   :emphasize-lines: 19-21

InDirectory(AssemblyFilter filter)
----------------------------------

The InDirectory() method allows you to specify an AssemblyFilter. An AssemblyFilter takes the string name of a directory and a filter predicate to allow only the assemblies you intend.

.. literalinclude:: /example/mirukenCastleExamples/featuresInDirectory.cs
   :emphasize-lines: 20-21



